using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>Configuration of the preparation duty before a class test (B-752, section <c>ClassTestPrep</c>).</summary>
public sealed class ClassTestPrepOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "ClassTestPrep";

    /// <summary>Days before the test in which the preparation duty runs, unless the class test sets its own window.</summary>
    public int DefaultWindowDays { get; set; } = 7;

    /// <summary>
    /// The penalty one class test costs per missed day, spread over its preparation positions (decision 8). Same value as
    /// the wizard's "normal" penalty (<c>VaterWizard.tsx</c>), which is still a client constant.
    /// </summary>
    public int PenaltyCoinsPerDay { get; set; } = 5;

    /// <summary>
    /// Upper bound of the daily dose of one preparation position (decision 6): new cards per day for a Leitner exercise,
    /// the contents used for a type without a daily dose. Not measured against real bundles (the real database is off
    /// limits for a night run); 10 is the daily dose the real plans use for vocabulary.
    /// </summary>
    public int MaxCardsPerDay { get; set; } = 10;
}

/// <summary>
/// The preparation duty before a class test (B-752): in the window before a planned test, every exercise of its bundle
/// becomes a daily duty in the child's active plan of the test's subject, ending the day before the test. Reconciled on
/// the child's login (and before a purchase), right before the penalty settlement, and when the supervisor plans or changes
/// the test or its bundle - never on a GET and never by a job.
/// Idempotent: a test is started once (<see cref="Klassenarbeit.PrepStartedOn"/>, the bundle is frozen then), and a
/// position knows the test it was made for (<see cref="PlanPosition.SourceKlassenarbeitId"/>).
/// </summary>
public class ClassTestPrepService(PuglingDbContext db, ExerciseContentResolver content, ExerciseTypeRegistry types,
    TimeProvider time, LearningDay learningDay, IOptions<ClassTestPrepOptions> options, PositionProgressService progress,
    PositionPlayService play)
{
    /// <summary>Upper bound of a test's own window - beyond it a typo, not a setting (the supervisor's form uses the same).</summary>
    public const int MaxWindowDays = 60;

    private ClassTestPrepOptions Options => options.Value;

    /// <summary>The window of the test in days: its own, or the configured default. 0 switches the duty off.</summary>
    public int WindowOf(Klassenarbeit k) => k.PrepWindowDays ?? Options.DefaultWindowDays;

    /// <summary>The configured default window - the value an empty field in the supervisor's form stands for.</summary>
    public int DefaultWindowDays => Options.DefaultWindowDays;

    /// <summary>
    /// Is <paramref name="today"/> in the test's preparation window? Planned, a window above 0, the test still to come
    /// (on its day nothing is owed any more) and the window begun.
    /// </summary>
    public bool InWindow(Klassenarbeit k, DateOnly today) =>
        k.Status == KlassenarbeitStatus.Planned && WindowOf(k) > 0
        && today < k.ScheduledDate && today >= k.ScheduledDate.AddDays(-WindowOf(k));

    /// <summary>The child's active plan in the test's subject, or null (no subject, no active plan in it).</summary>
    private Task<StudyPlan?> PlanForAsync(Klassenarbeit k, CancellationToken ct) =>
        k.SubjectId is not { } subjectId
            ? Task.FromResult<StudyPlan?>(null)
            : db.StudyPlans.Where(p => p.ChildId == k.ChildId && p.Active && p.SubjectId == subjectId)
                .OrderByDescending(p => p.ActivatedAt).ThenByDescending(p => p.Id).FirstOrDefaultAsync(ct);

    /// <summary>
    /// Why no preparation duty arises for a planned test with a window (decision 4): no subject, or no active plan in it.
    /// <c>null</c> when it can arise - and for tests that are not planned or have the duty switched off.
    /// </summary>
    public async Task<string?> BlockReasonAsync(Klassenarbeit k, CancellationToken ct)
    {
        if (k.Status != KlassenarbeitStatus.Planned || WindowOf(k) <= 0) return null;
        if (k.SubjectId is null) return ClassTestPrepBlockReasons.NoSubject;
        return await PlanForAsync(k, ct) is null ? ClassTestPrepBlockReasons.NoActivePlan : null;
    }

    // ---- Dose ----

    /// <summary>A bundle exercise as the duty plays it: its card count and whether it takes a daily dose.</summary>
    private sealed record Candidate(Exercise Exercise, int Cards, bool TakesDose);

    private async Task<Candidate> CandidateOfAsync(Exercise exercise, CancellationToken ct)
    {
        var cards = (await content.ItemsOfAsync(exercise, null, ct)).Count;
        // A daily dose needs the Leitner box and an order of the cards; a set-graded exercise accepts any entry of the round.
        var takesDose = exercise.DefaultUseLeitner
            && types.ByKey(exercise.Type)?.GradesAsSet(PositionPlayService.ConfigOf(exercise)) != true;
        return new Candidate(exercise, cards, takesDose);
    }

    /// <summary>
    /// The daily cards of one exercise over <paramref name="days"/> days (decision 6): with a daily dose
    /// <c>ceil(cards / days)</c>, without one every content under the cap every day - both at most the configured cap.
    /// </summary>
    private int DailyCards(Candidate c, int days) =>
        Math.Min(c.TakesDose ? (int)Math.Ceiling(c.Cards / (double)Math.Max(1, days)) : c.Cards, Options.MaxCardsPerDay);

    /// <summary>
    /// "About N cards a day" for the supervisor's list (B-757): what the child plays daily for this test - every position of
    /// the subject's active plan that is a daily duty today and plays a bundle exercise or was made for the test (B-948: the
    /// load already in the plan counts too, else a test whose exercises are all assigned reads "0"), plus, before the start,
    /// the positions the duty would still make over its window. <c>null</c> when no duty arises (not planned, window 0,
    /// blocked, the test day reached).
    /// </summary>
    public async Task<int?> CardsPerDayAsync(Klassenarbeit k, DateOnly today, CancellationToken ct)
    {
        if (k.Status != KlassenarbeitStatus.Planned || WindowOf(k) <= 0 || today >= k.ScheduledDate) return null;
        if (await PlanForAsync(k, ct) is not { } plan) return null;
        var bundle = await ClassTestBundle.ExerciseIdsAsync(db, [k.Id], ct);
        var positions = await db.PlanPositions.AsNoTracking()
            .Where(p => p.StudyPlanId == plan.Id && (p.SourceKlassenarbeitId == k.Id || bundle.Contains(p.ExerciseId)))
            .Include(p => p.Exercise).ThenInclude(e => e!.Prerequisites).ToListAsync(ct);
        var sum = 0;
        foreach (var p in positions.Where(p => AsksDailyOn(p, today)))
        {
            if (p.NewItemsPerDay > 0) { sum += p.NewItemsPerDay; continue; }
            // Without a dose the position's ItemCount is only a cap: the list shows the round the child really plays
            // today - the played exercise's cards under the cap, through the play service's one pool rule (B-946).
            sum += play.PoolSize(p, today, (await play.ItemsOfAsync(p, today, ct: ct)).Count);
        }
        if (k.PrepStartedOn is null)
            foreach (var c in await NewCandidatesAsync(k, plan, ct)) sum += DailyCards(c, WindowOf(k));
        return sum;
    }

    /// <summary>
    /// Does the position ask something of the child on <paramref name="day"/> (B-948)? A duty on that day - begun, not ended -
    /// with a daily cadence; a free or weekly position asks nothing of a given day.
    /// </summary>
    private bool AsksDailyOn(PlanPosition p, DateOnly day) =>
        progress.IsObligationOn(p, day) && PositionProgressService.IsDailyDuty(PositionProgressService.DutyCadenceOn(p, day));

    /// <summary>
    /// The bundle exercises a supervisor of the test's child may assign (B-941): publicly executable, or granted to one of
    /// the child's supervisors. The rule of <c>ExercisePermissionService.CanExecuteAsync</c>, without a principal - the duty
    /// starts on the child's login. A grant of any other adult does not count: it would let a stranger's material in.
    /// </summary>
    private IQueryable<Exercise> ExecutableFor(int childId) =>
        db.Exercises.Where(e => e.ExecutePublic || db.ExerciseGrants.Any(g => g.ExerciseId == e.Id
            && db.SupervisorLinks.Any(l => l.StudentId == childId && l.SupervisorId == g.CreatorId)));

    /// <summary>
    /// How many bundle exercises the duty leaves out for lack of an execute permission (B-941) - withdrawn material stops
    /// new assignments, and starting the duty is one. Shown to the supervisor, so a silently shorter duty has a reason. An
    /// exercise already in the plan is not left out (it is played there), and from the test day on nothing is owed.
    /// </summary>
    public async Task<int> NotExecutableCountAsync(Klassenarbeit k, DateOnly today, CancellationToken ct)
    {
        // No duty, nothing left out of it - the same bounds as CardsPerDayAsync.
        if (k.Status != KlassenarbeitStatus.Planned || WindowOf(k) <= 0 || today >= k.ScheduledDate) return 0;
        var leftOut = await LeftOutAsync(k, ct);
        // After the start the hint counts only what a grant still pulls in (B-947): the start's snapshot. Anything that
        // joined the frozen bundle later would never become a duty, so the hint must not advise a grant for it.
        return k.PrepStartedOn is null ? leftOut.Count : leftOut.Count(id => k.PrepLeftOutExerciseIds?.Contains(id) == true);
    }

    /// <summary>The bundle exercises the duty leaves out right now: not executable for the child's supervisors and not in the plan.</summary>
    private async Task<List<int>> LeftOutAsync(Klassenarbeit k, CancellationToken ct)
    {
        var bundle = await ClassTestBundle.ExerciseIdsAsync(db, [k.Id], ct);
        if (bundle.Count == 0) return [];
        var executable = await ExecutableFor(k.ChildId).Where(e => bundle.Contains(e.Id)).Select(e => e.Id).ToListAsync(ct);
        var inPlan = await PlanForAsync(k, ct) is { } plan
            ? await db.PlanPositions.Where(p => p.StudyPlanId == plan.Id).Select(p => p.ExerciseId).ToListAsync(ct)
            : [];
        return bundle.Where(id => !executable.Contains(id) && !inPlan.Contains(id)).ToList();
    }

    /// <summary>
    /// The bundle exercises that would become a new position in <paramref name="plan"/>: executable for the child's
    /// supervisors (B-941), not already in it (decision 3, AK 4) and with something to play - an empty exercise would be a
    /// duty nobody can meet. The one sink for both the start and the supervisor's "about N cards a day".
    /// </summary>
    private async Task<List<Candidate>> NewCandidatesAsync(Klassenarbeit k, StudyPlan plan, CancellationToken ct)
    {
        var bundle = await ClassTestBundle.ExerciseIdsAsync(db, [k.Id], ct);
        var inPlan = await db.PlanPositions.Where(p => p.StudyPlanId == plan.Id).Select(p => p.ExerciseId).ToListAsync(ct);
        var exercises = await ExecutableFor(k.ChildId).Include(e => e.Prerequisites)
            .Where(e => bundle.Contains(e.Id) && !inPlan.Contains(e.Id))
            .OrderBy(e => e.Id).ToListAsync(ct);
        var candidates = new List<Candidate>();
        foreach (var exercise in exercises)
            if (await CandidateOfAsync(exercise, ct) is { Cards: > 0 } c) candidates.Add(c);
        return candidates;
    }

    // ---- Reconcile ----

    /// <summary>
    /// Brings the child's preparation duties in line with its planned class tests on <paramref name="today"/>: starts the
    /// tests whose window has begun (once each) and lets every preparation position run to the latest test it prepares.
    /// Saves itself; call it before the penalty settlement, or the first period lacks the position.
    /// </summary>
    public async Task ReconcileAsync(int childId, DateOnly today, CancellationToken ct)
    {
        var inWindow = (await db.Klassenarbeiten
                .Where(k => k.ChildId == childId && k.Status == KlassenarbeitStatus.Planned && k.ScheduledDate > today)
                .OrderBy(k => k.ScheduledDate).ThenBy(k => k.Id)
                .ToListAsync(ct))
            .Where(k => InWindow(k, today)).ToList();

        foreach (var k in inWindow.Where(k => k.PrepStartedOn is null))
        {
            // No plan in the subject: nothing arises, and the test is not frozen - a plan switched on later starts it then.
            if (await PlanForAsync(k, ct) is not { } plan) continue;
            // An empty bundle is not frozen either: the father plans the test first and assigns its exercises afterwards,
            // and a start in between would leave the test without a duty for good. "Empty" counts only what may be assigned
            // (B-941): a bundle of withdrawn material stays unstarted, so a grant given later still starts the duty. A bundle
            // already in the plan is started on purpose - it then lets those positions run to this test (AlignSpansAsync).
            var bundle = await ClassTestBundle.ExerciseIdsAsync(db, [k.Id], ct);
            if (!await ExecutableFor(k.ChildId).AnyAsync(e => bundle.Contains(e.Id), ct)) continue;
            // Claim and positions in one transaction (the caller's, if it holds one): a start that fails or is cancelled
            // half-way must not leave the test claimed without its positions - it would never be started again.
            await using var tx = db.Database.CurrentTransaction is null ? await db.Database.BeginTransactionAsync(ct) : null;
            // Claim the start in the database first: two logins at once (or a login and the father's change) both read the
            // test as not started, and only the one whose update lands may add the positions - else they come twice.
            var claimed = await db.Klassenarbeiten.Where(x => x.Id == k.Id && x.PrepStartedOn == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.PrepStartedOn, today), ct);
            if (claimed == 0) continue;
            await StartAsync(k, plan, today, ct);
            // Saved per test: the next test's "already in the plan?" reads the database, and must see these positions.
            await db.SaveChangesAsync(ct);
            if (tx is not null) await tx.CommitAsync(ct);
        }
        foreach (var k in inWindow.Where(k => k.PrepStartedOn is not null && k.PrepLeftOutExerciseIds is { Count: > 0 }))
            if (await PlanForAsync(k, ct) is { } plan) await PullInAsync(k, plan, today, ct);
        if (await AlignSpansAsync(childId, today, inWindow, ct)) await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Makes a duty of the exercises the start left out for lack of an execute permission once a grant arrives (B-947):
    /// only those of the start's snapshot, only while still in the bundle, executable and not in the plan - the sink
    /// <see cref="NewCandidatesAsync"/> decides the last three. Owing from today, like a start on this day.
    /// </summary>
    private async Task PullInAsync(Klassenarbeit k, StudyPlan plan, DateOnly today, CancellationToken ct)
    {
        var snapshot = k.PrepLeftOutExerciseIds!;
        var candidates = (await NewCandidatesAsync(k, plan, ct)).Where(c => snapshot.Contains(c.Exercise.Id)).ToList();
        if (candidates.Count == 0) return;
        var remaining = snapshot.Where(id => candidates.All(c => c.Exercise.Id != id)).ToList();

        await using var tx = db.Database.CurrentTransaction is null ? await db.Database.BeginTransactionAsync(ct) : null;
        // Claim like the start: of two logins at once only the one whose update lands adds the positions.
        var claimed = await db.Klassenarbeiten.Where(x => x.Id == k.Id && x.PrepLeftOutExerciseIds == snapshot)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.PrepLeftOutExerciseIds, remaining), ct);
        if (claimed == 0) return;
        k.PrepLeftOutExerciseIds = remaining;
        // The running positions keep their penalty: a lowered share would change days not yet settled. The new ones bear
        // the share the test's daily penalty gives every position now.
        var made = await db.PlanPositions.CountAsync(p => p.SourceKlassenarbeitId == k.Id, ct);
        var share = Math.Max(1, Options.PenaltyCoinsPerDay / (made + candidates.Count));
        await AddPositionsAsync(k, plan, today, candidates, _ => share, ct);
        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);
    }

    /// <summary>
    /// Starts the duty of one test: freezes its bundle (<see cref="Klassenarbeit.PrepStartedOn"/>) and adds a daily
    /// position for every bundle exercise not yet in the plan, owing from today on (no retroactive penalty), ending the
    /// day before the test, the test's daily penalty spread over them (rest on the first) and no goal reward (decision 8).
    /// </summary>
    private async Task StartAsync(Klassenarbeit k, StudyPlan plan, DateOnly today, CancellationToken ct)
    {
        k.PrepStartedOn = today;
        // What the start leaves out for lack of permission - a later grant pulls exactly these in (B-947).
        k.PrepLeftOutExerciseIds = await LeftOutAsync(k, ct);
        var candidates = await NewCandidatesAsync(k, plan, ct);
        if (candidates.Count == 0) return;

        var share = Options.PenaltyCoinsPerDay / candidates.Count;
        var rest = Options.PenaltyCoinsPerDay - share * candidates.Count;
        await AddPositionsAsync(k, plan, today, candidates, i => share + (i == 0 ? rest : 0), ct);
    }

    /// <summary>
    /// Adds a daily preparation position for each candidate, owing from today on (no retroactive penalty), ending the day
    /// before the test, dosed over the days left and with no goal reward - the one place that makes them (start and pull-in).
    /// </summary>
    private async Task AddPositionsAsync(Klassenarbeit k, StudyPlan plan, DateOnly today, List<Candidate> candidates,
        Func<int, int> penaltyOf, CancellationToken ct)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var days = k.ScheduledDate.DayNumber - today.DayNumber;
        var order = ((await db.PlanPositions.Where(p => p.StudyPlanId == plan.Id).MaxAsync(p => (int?)p.Order, ct)) ?? -1) + 1;
        for (var i = 0; i < candidates.Count; i++)
        {
            var c = candidates[i];
            var dose = DailyCards(c, days);
            db.PlanPositions.Add(new PlanPosition
            {
                StudyPlanId = plan.Id,
                ExerciseId = c.Exercise.Id,
                SourceKlassenarbeitId = k.Id,
                Order = order + i,
                CreatedAt = now,
                DutySince = learningDay.DayOf(now),
                Cadence = GoalCadence.Daily,
                DutyUntil = k.ScheduledDate.AddDays(-1),
                CadenceUntil = GoalCadence.Daily,
                UseLeitner = c.Exercise.DefaultUseLeitner,
                NewItemsPerDay = c.TakesDose ? dose : 0,
                ItemCount = c.TakesDose ? null : Options.MaxCardsPerDay,
                PenaltyCoins = penaltyOf(i),
                PointsGoalMet = 0,
            });
        }
    }

    /// <summary>
    /// Lets every running preparation position of the child end the day before the latest test it prepares (decision 5):
    /// its own test, and every other started test in its window whose bundle holds the exercise. A position the father
    /// freed or switched to weekly is his and stays as it is.
    /// <para>
    /// B-758: when no test covers a running position any more (cancelled, written, moved out of its window, window 0), its
    /// duty is closed by the rule of B-659 - a met day counts, an open one does not, nothing is owed afterwards. A closed
    /// position that a test covers again (moved back into its window) owes from today on, never for the days in between.
    /// </para>
    /// </summary>
    private async Task<bool> AlignSpansAsync(int childId, DateOnly today, List<Klassenarbeit> inWindow, CancellationToken ct)
    {
        // Exercise and its prerequisites: closing judges "met today?" on the exercise the position plays (B-848).
        var positions = await db.PlanPositions.Include(p => p.StudyPlan)
            .Include(p => p.Exercise).ThenInclude(e => e!.Prerequisites)
            .Where(p => p.SourceKlassenarbeitId != null && p.StudyPlan!.ChildId == childId && p.Cadence == GoalCadence.Daily)
            .ToListAsync(ct);
        if (positions.Count == 0) return false;

        var started = inWindow.Where(k => k.PrepStartedOn is not null).ToList();
        var bundles = new Dictionary<int, List<int>>();
        foreach (var k in started) bundles[k.Id] = await ClassTestBundle.ExerciseIdsAsync(db, [k.Id], ct);

        var changed = false;
        foreach (var pos in positions)
        {
            // Not filtered by execute permission (B-941): the position exists, and withdrawing stops only NEW assignments -
            // letting a running duty reach a later test of the same bundle assigns nothing new.
            var covering = started.Where(k => k.Id == pos.SourceKlassenarbeitId
                || (k.SubjectId == pos.StudyPlan!.SubjectId && bundles[k.Id].Contains(pos.ExerciseId))).ToList();
            if (covering.Count == 0)
            {
                // Already over. A preparation duty without an end (its span cleared by a cadence switch) is still running and closes.
                if (pos.DutyUntil is { } end && end < today) continue;
                await progress.CloseDutyAsync(pos, pos.StudyPlan!, today, ct);
                changed = true;
                continue;
            }
            var until = covering.Max(k => k.ScheduledDate).AddDays(-1);
            // Reopened after a close: the duty begins again today - the days it was closed cannot have been missed.
            if (pos.DutyUntil is { } closed && closed < today) pos.DutySince = today;
            if (pos.DutyUntil == until && pos.CadenceUntil == GoalCadence.Daily) continue;
            pos.DutyUntil = until;
            pos.CadenceUntil = GoalCadence.Daily;
            changed = true;
        }
        return changed;
    }
}
