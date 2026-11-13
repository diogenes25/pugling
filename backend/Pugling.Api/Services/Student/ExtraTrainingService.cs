using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Student;

/// <summary>
/// The bonus training: extra exercises on the child's current topic that pay coins beyond the day's duty.
/// <para>
/// Three rules keep it from becoming a coin farm. It opens only once today's duty is met - the path comes first.
/// Coins are paid for checked hits only (choice, typed), never for a self-assessment, and capped per day. And an
/// exercise pays only once: a non-vocabulary exercise has exactly one run (started is used up), while a vocabulary
/// exercise may be played again but asks harder with every completed run - self-assessment, then choice, then typed
/// for good. Typing a word from memory again and again is work the child may be paid for.
/// </para>
/// <para>
/// The topic is the set of textbook units the child's playable plans draw from, plus the current unit of its
/// textbooks. A non-vocabulary exercise that is a position of one of the child's plans is left out: the child knows
/// it from the path, and replaying it would pay for memory, not for learning.
/// </para>
/// </summary>
public class ExtraTrainingService(PuglingDbContext db, PositionProgressService progress, ExerciseContentResolver content,
    ExercisePreviewService preview, ExerciseTypeRegistry registry, IOptions<ExtraTrainingOptions> options,
    TimeProvider time)
{
    private readonly ExtraTrainingOptions opts = options.Value;

    /// <summary>A choice needs distractors: with fewer atoms the options would give the answer away.</summary>
    private const int MinItemsForChoice = 4;

    /// <summary>Lock reason: the child has no playable plan, so there is neither a duty nor a topic.</summary>
    public const string LockNoPlan = "no_plan";
    /// <summary>Lock reason: today's duty is not met yet.</summary>
    public const string LockDutyOpen = "duty_open";

    /// <summary>Outcome of a start or submit: either the value or the error to answer with.</summary>
    public record Outcome<T>(T? Value, ApiError? Error = null, string? Detail = null) where T : class
    {
        /// <summary>A failed outcome answered with <paramref name="error"/>.</summary>
        public static Outcome<T> Fail(ApiError error, string detail) => new(null, error, detail);
    }

    private DateOnly Today => DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);

    /// <summary>The child's bonus training for today: whether it is open, the coins so far and the exercise pool.</summary>
    public async Task<ExtraTrainingOverview> OverviewAsync(int childId, CancellationToken ct)
    {
        var today = Today;
        var gate = await GateAsync(childId, today, ct);
        var coinsToday = await CoinsTodayAsync(childId, today, ct);
        var cap = CapOf(await db.Children.AsNoTracking().Where(c => c.Id == childId).Select(c => c.BonusDailyCoinCap).FirstAsync(ct));
        var pool = gate.Plans.Count == 0 ? [] : await PoolAsync(childId, gate.Plans, ct);
        return new ExtraTrainingOverview(gate.Unlocked, gate.LockReason, gate.Outstanding, coinsToday, cap,
            [.. pool.Select(p => p.Row)]);
    }

    /// <summary>
    /// Starts a run of <paramref name="exerciseId"/>, or resumes the open one. The form is frozen on the run, so a
    /// resumed run asks the same questions in the same form.
    /// </summary>
    public async Task<Outcome<ExtraRunResponse>> StartAsync(int childId, int exerciseId, CancellationToken ct)
    {
        var today = Today;
        var gate = await GateAsync(childId, today, ct);
        if (!gate.Unlocked)
            return Outcome<ExtraRunResponse>.Fail(ApiErrors.ExtraTrainingLocked,
                "The bonus training opens once today's duty is done.");

        var pool = await PoolAsync(childId, gate.Plans, ct);
        if (pool.FirstOrDefault(p => p.Row.ExerciseId == exerciseId) is not { } entry)
            return Outcome<ExtraRunResponse>.Fail(ApiErrors.NotFound, "The exercise is not part of the bonus training.");

        var run = entry.Row.OpenRunId is { } openId
            ? await db.ExtraTrainingRuns.FirstAsync(r => r.Id == openId, ct)
            : null;
        if (run is null)
        {
            if (entry.Row.Done)
                return Outcome<ExtraRunResponse>.Fail(ApiErrors.ExtraExerciseDone,
                    "This bonus exercise has already been played.");
            run = new ExtraTrainingRun
            {
                ChildId = childId,
                ExerciseId = exerciseId,
                Day = today,
                Pass = entry.Row.CompletedRuns,
                Form = entry.Row.NextForm,
                Stage = entry.Stage,
                StartedAt = time.GetUtcNow().UtcDateTime,
                ItemCount = entry.Items.Count,
            };
            db.ExtraTrainingRuns.Add(run);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // A parallel start (double tap) won the filtered unique index: resume its run instead of failing.
                // Any other cause (the exercise deleted meanwhile) has no open run to resume and stays an error.
                db.ChangeTracker.Clear();
                run = await db.ExtraTrainingRuns.FirstOrDefaultAsync(r => r.ChildId == childId && r.ExerciseId == exerciseId
                    && r.CompletedAt == null, ct);
                if (run is null) throw;
            }
        }

        return new Outcome<ExtraRunResponse>(BuildRun(run, entry.Exercise, entry.Type, entry.Items));
    }

    /// <summary>
    /// Grades a run and books its coins: per checked hit, never above what is left of today's cap. A run is graded
    /// once, and only while today's duty is met - a run started on an open day and kept back must not pay out on a day
    /// the duty was skipped. The child's concurrency stamp serializes the submit against every other one.
    /// </summary>
    public async Task<Outcome<ExtraRunResult>> SubmitAsync(int childId, int runId, IReadOnlyList<ExtraAnswer> answers,
        CancellationToken ct)
    {
        // The child first: its stamp is the serialization point, and it only protects what is read AFTER it. Loaded
        // last, a second submit of the same run (double tap, two tabs) would read the run as still open, see the
        // stamp the first one had just written, and book the coins a second time.
        var child = await db.Children.FirstAsync(c => c.Id == childId, ct);

        var run = await db.ExtraTrainingRuns.Include(r => r.Exercise)
            .FirstOrDefaultAsync(r => r.Id == runId && r.ChildId == childId, ct);
        if (run?.Exercise is null) return Outcome<ExtraRunResult>.Fail(ApiErrors.NotFound, "Bonus run not found.");
        if (run.CompletedAt is not null)
            return Outcome<ExtraRunResult>.Fail(ApiErrors.ExtraRunCompleted, "This bonus run has already been submitted.");

        var today = Today;
        if (!(await GateAsync(childId, today, ct)).Unlocked)
            return Outcome<ExtraRunResult>.Fail(ApiErrors.ExtraTrainingLocked,
                "The bonus training opens once today's duty is done.");

        var graded = await preview.CheckAsync(run.Exercise,
            [.. answers.Select(a => new PreviewAnswer(a.ItemIndex, a.GivenAnswer, a.WasKnown))], run.Stage, ct);
        if (graded is null)
            return Outcome<ExtraRunResult>.Fail(ApiErrors.NotFound, "The exercise has no content to grade any more.");

        var coinsBefore = await CoinsTodayAsync(childId, today, ct);
        var earned = graded.Correct * CoinsPerHit(run.Form);
        var dailyCap = CapOf(child.BonusDailyCoinCap);
        var left = dailyCap is { } cap ? Math.Max(0, cap - coinsBefore) : int.MaxValue;
        var coins = Math.Min(earned, left);

        var now = time.GetUtcNow().UtcDateTime;
        // The coins count against the day they are booked on, not the day the run was started.
        run.Day = today;
        run.CompletedAt = now;
        run.CorrectCount = graded.Correct;
        run.CoinsAwarded = coins;
        if (coins > 0)
            db.ChildPointsEntries.Add(new ChildPointsEntry
            {
                ChildId = childId,
                Kind = PointKind.ExtraTrainingCoins,
                Amount = coins,
                Reason = $"Bonus-Training: {run.Exercise.Title}",
                CreatedAt = now,
            });

        child.ConcurrencyStamp = Guid.NewGuid();
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            return Outcome<ExtraRunResult>.Fail(ApiErrors.ConcurrencyConflict, "Another bonus run was saved at the same time. Please try again.");
        }

        return new Outcome<ExtraRunResult>(new ExtraRunResult(run.Id, run.Form, graded.Total, graded.Correct, coins,
            coins < earned, coinsBefore + coins, dailyCap,
            [.. graded.Items.Select(i => new ExtraItemOutcome(i.ItemIndex, i.Prompt, i.Expected, i.GivenAnswer, i.WasCorrect))]));
    }

    private record Gate(bool Unlocked, string? LockReason, IReadOnlyList<string> Outstanding, IReadOnlyList<StudyPlan> Plans);

    // Open once every playable plan has met today's duty. A plan without any duty has nothing to come first.
    private async Task<Gate> GateAsync(int childId, DateOnly today, CancellationToken ct)
    {
        var plans = await db.StudyPlans.AsNoTracking()
            .Where(p => p.ChildId == childId && p.Active && p.StartDate <= today && p.EndDate >= today)
            .ToListAsync(ct);
        if (plans.Count == 0) return new Gate(false, LockNoPlan, [], plans);

        var outstanding = new List<string>();
        foreach (var plan in plans)
        {
            var day = await progress.ComputeDayAsync(plan, today, ct);
            if (day.GoalsTotal > 0 && !day.DutyDone) outstanding.AddRange(day.Outstanding);
        }
        return outstanding.Count == 0
            ? new Gate(true, null, [], plans)
            : new Gate(false, LockDutyOpen, outstanding, plans);
    }

    // The supervisor's cap for this child wins over the server default.
    private int? CapOf(int? childCap) => childCap ?? opts.DailyCoinCap;

    private async Task<int> CoinsTodayAsync(int childId, DateOnly day, CancellationToken ct) =>
        await db.ExtraTrainingRuns.Where(r => r.ChildId == childId && r.Day == day).SumAsync(r => (int?)r.CoinsAwarded, ct) ?? 0;

    private record PoolEntry(ExtraExercise Row, Exercise Exercise, IExerciseType Type, int Stage, IReadOnlyList<ContentItem> Items);

    // The exercises of the current topic the child can play for a bonus, each with the form of its next run.
    private async Task<List<PoolEntry>> PoolAsync(int childId, IReadOnlyList<StudyPlan> plans, CancellationToken ct)
    {
        var planIds = plans.Select(p => p.Id).ToList();
        var positionExercises = await db.PlanPositions.AsNoTracking()
            .Where(p => planIds.Contains(p.StudyPlanId))
            .Select(p => new { p.ExerciseId, p.Exercise!.SeriesUnitId })
            .ToListAsync(ct);
        var units = positionExercises.Select(p => p.SeriesUnitId).ToHashSet();
        units.UnionWith(await db.Textbooks.AsNoTracking()
            .Where(t => t.ChildId == childId && t.CurrentUnitId != null)
            .Select(t => t.CurrentUnitId!.Value)
            .ToListAsync(ct));
        if (units.Count == 0) return [];

        // Every plan of the child, not only the playable ones: an exercise known from an earlier path is known too.
        var knownFromPath = (await db.PlanPositions.AsNoTracking()
            .Where(p => p.StudyPlan!.ChildId == childId)
            .Select(p => p.ExerciseId)
            .ToListAsync(ct)).ToHashSet();
        var assigned = positionExercises.Select(p => p.ExerciseId).ToHashSet();

        var exercises = await db.Exercises.AsNoTracking().Include(e => e.SeriesUnit)
            .Where(e => units.Contains(e.SeriesUnitId) && (e.ExecutePublic || assigned.Contains(e.Id)))
            .OrderBy(e => e.SeriesUnitId).ThenBy(e => e.OrderIndex).ThenBy(e => e.Id)
            .ToListAsync(ct);

        var runs = await db.ExtraTrainingRuns.AsNoTracking()
            .Where(r => r.ChildId == childId)
            .Select(r => new { r.ExerciseId, r.Id, r.CompletedAt })
            .ToListAsync(ct);
        var completed = runs.Where(r => r.CompletedAt != null).GroupBy(r => r.ExerciseId).ToDictionary(g => g.Key, g => g.Count());
        var open = runs.Where(r => r.CompletedAt == null).ToDictionary(r => r.ExerciseId, r => r.Id);

        var pool = new List<PoolEntry>();
        foreach (var exercise in exercises)
        {
            if (registry.ByKey(exercise.Type) is not { } type) continue;
            var repeatable = exercise.Type == ExerciseTypeKeys.Vocabulary;
            if (!repeatable && knownFromPath.Contains(exercise.Id)) continue;

            var items = await content.ItemsOfAsync(exercise, ct: ct);
            if (items.Count == 0) continue;

            var done = completed.GetValueOrDefault(exercise.Id);
            if (FormFor(exercise, type, repeatable, items, done) is not var (form, stage)) continue;
            // A one-shot exercise is used up once started: an open run is resumed, a finished one ends it.
            var finished = !repeatable && (done > 0 || open.ContainsKey(exercise.Id)) && !open.ContainsKey(exercise.Id);

            pool.Add(new PoolEntry(
                new ExtraExercise(exercise.Id, exercise.Title, exercise.Type, exercise.SeriesUnit?.Label ?? "", items.Count,
                    repeatable, done, finished, form, CoinsPerHit(form), open.TryGetValue(exercise.Id, out var runId) ? runId : null),
                exercise, type, stage, items));
        }
        return pool;
    }

    // The form of the next run, or null when the exercise has no checked form to be paid for. Vocabulary climbs with
    // its completed runs; everything else plays its author's stage, or the type's most telling one, once - and only
    // where that stage is checked, because a self-assessed one-shot would be the cheapest coins there are.
    private (ExtraForm Form, int Stage)? FormFor(Exercise exercise, IExerciseType type, bool repeatable,
        IReadOnlyList<ContentItem> items, int completedRuns)
    {
        if (repeatable)
        {
            if (completedRuns < opts.SelfAssessRuns) return (ExtraForm.SelfAssess, (int)TestStage.SelfAssess);
            if (completedRuns < opts.SelfAssessRuns + opts.ChoiceRuns && items.Count >= MinItemsForChoice)
                return (ExtraForm.Choice, (int)TestStage.MultipleChoice);
            return (ExtraForm.Typed, (int)TestStage.FreeText);
        }

        int? stage = exercise.DefaultStage is { } own && type.IsTypedStage(own) ? own
            : type.IsTypedStage(type.PreviewStage) ? type.PreviewStage : null;
        if (stage is not { } checkedStage) return null;
        // A checked stage that offers options (a cloze word bank) is a pick, not a recall, and pays as one.
        return (items.Any(i => type.Choices(exercise.ConfigJson, items, i, checkedStage) is not null) ? ExtraForm.Choice : ExtraForm.Typed,
            checkedStage);
    }

    private int CoinsPerHit(ExtraForm form) => form switch
    {
        ExtraForm.Choice => opts.CoinsPerChoiceHit,
        ExtraForm.Typed => opts.CoinsPerTypedHit,
        _ => 0,
    };

    // The cards in an order of their own per run, and the options shuffled per run as well: the shared projection
    // places the right option by the atom's index, which a child replaying the same words would learn as a position.
    private ExtraRunResponse BuildRun(ExtraTrainingRun run, Exercise exercise, IExerciseType type,
        IReadOnlyList<ContentItem> items)
    {
        var typed = type.IsTypedStage(run.Stage);
        var rng = new Random(run.Id);
        var ordered = exercise.Type == ExerciseTypeKeys.Vocabulary ? items.OrderBy(_ => rng.Next()).ToList() : [.. items];
        var cards = ordered.Select(item =>
        {
            var f = PositionPlayService.CardFacets(exercise.ConfigJson, items, item, type, run.Stage, typed, run.Id);
            var choices = f.Choices is { } c ? c.OrderBy(_ => rng.Next()).ToList() : null;
            return new ExtraCard(item.Index, f.Prompt, f.GapIndex, f.Hint, f.AnswerLength, f.Reveal, f.RevealAlternatives,
                choices, f.AudioUrl, f.Passage, f.AnyOrder, f.Decoding, f.AnswerPattern, f.LetterTiles, f.CueBox);
        }).ToList();
        return new ExtraRunResponse(run.Id, exercise.Id, exercise.Title, exercise.Type, run.Form, run.Stage, typed,
            typed ? CoinsPerHit(run.Form) : 0, cards);
    }
}
