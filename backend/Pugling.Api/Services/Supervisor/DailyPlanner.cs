using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Supervisor;

/// <summary>
/// The day planner (B-734): makes each child's daily plan once per learning day from 6 a.m. and freezes it. The sink every
/// trigger passes - the timer, the child's login, the child's day view and the supervisor's list all call
/// <see cref="EnsureTodayAsync"/>, so there is one way a plan comes into being. The decisions are
/// <see cref="DailyPlanRules"/>; this class only gathers their inputs and stores the result. In <c>Shadow</c> mode a plan
/// has no effect anywhere: nothing here books points or touches the child's app.
/// </summary>
public class DailyPlanner(PuglingDbContext db, LearningDay learningDay, DutyCalendar calendar, PositionProgressService progress,
    PositionPlayService play, DailyEvaluationService evaluations, TimeProvider time, ILogger<DailyPlanner> logger)
{
    private const int SqliteUniqueViolation = 2067;

    /// <summary>The local hour from which today's plan is made (B-644, decision 9): the plan stands before school.</summary>
    public const int ReadyHour = 6;

    /// <summary>
    /// Today's plan of the child: the stored one, or composed and stored now when the planner is on, it is 6 a.m. or later
    /// and today owes a duty (never on a Sunday or in a pause, B-734 acceptance 9). <c>null</c> when no plan is due. Idempotent
    /// and safe to race: a second caller that loses the unique index reads the winner's plan.
    /// </summary>
    public virtual async Task<DailyPlan?> EnsureTodayAsync(int childId, CancellationToken ct)
    {
        var today = learningDay.Today;
        var existing = await LoadAsync(childId, today, ct);
        if (existing is not null) return existing;

        var child = await db.Children.AsNoTracking().FirstOrDefaultAsync(c => c.Id == childId, ct);
        if (child is not
            {
                PlannerMode: not PlannerMode.Off, DailyTasksMin: { } tasksMin, DailyTasksMax: { } tasksMax,
                DailyNewWordsMin: { } wordsMin, DailyNewWordsMax: { } wordsMax
            })
            return null;
        if (learningDay.LocalNow.Hour < ReadyHour) return null;
        var dutyDays = await calendar.ForChildAsync(childId, ct);
        if (!dutyDays.IsDutyDay(today)) return null;

        var candidates = await CandidatesAsync(childId, today, dutyDays, ct);
        var result = DailyPlanRules.Compose(candidates, new PlannerLimits(tasksMin, tasksMax, wordsMin, wordsMax),
            await TempoAsync(childId, today, ct));
        var plan = new DailyPlan
        {
            ChildId = childId,
            Day = today,
            Mode = child.PlannerMode,
            NewWordsBudget = result.NewWordsBudget,
            Warnings = [.. result.Warnings],
            CreatedAt = time.GetUtcNow().UtcDateTime,
            Tasks = [.. result.Tasks.Select(t => new DailyTask
            {
                PlanPositionId = t.Candidate.PositionId, ExerciseId = t.Candidate.ExerciseId,
                ExerciseTitle = t.Candidate.ExerciseTitle, SubjectId = t.Candidate.SubjectId, Order = t.Order,
                DueReviews = t.Candidate.DueReviews, NewWords = t.NewWords, Stage = t.Stage, Reason = t.Reason,
            })],
        };
        return await SaveOrLoadAsync(plan, ct);
    }

    /// <summary>
    /// <see cref="EnsureTodayAsync"/> for the child's own paths (login, day view): a failure is logged and swallowed. In shadow
    /// mode the plan has no effect, so it must never cost the child the app; the timer or the next access tries again.
    /// </summary>
    public async Task TryEnsureTodayAsync(int childId, CancellationToken ct)
    {
        try
        {
            await EnsureTodayAsync(childId, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Tagesplan für Kind {ChildId} ließ sich nicht anlegen; der nächste Zugriff versucht es erneut.", childId);
        }
    }

    /// <summary>
    /// Stores a composed plan, or - when another caller stored the plan of that child and day first - drops it and returns
    /// the stored one. The unique index decides the race, not a check before the insert.
    /// </summary>
    public async Task<DailyPlan> SaveOrLoadAsync(DailyPlan plan, CancellationToken ct)
    {
        db.DailyPlans.Add(plan);
        try
        {
            await db.SaveChangesAsync(ct);
            return plan;
        }
        catch (DbUpdateException e) when (e.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteUniqueViolation })
        {
            // Only a lost race lands here; any other failure (a position deleted meanwhile) is no plan to read back.
            db.ChangeTracker.Clear();
            return await LoadAsync(plan.ChildId, plan.Day, ct)
                ?? throw new InvalidOperationException($"Tagesplan für Kind {plan.ChildId} am {plan.Day} ließ sich nicht speichern.");
        }
    }

    /// <summary>The stored plans of the child from <paramref name="from"/> to <paramref name="to"/>, newest first.</summary>
    public Task<List<DailyPlan>> ListAsync(int childId, DateOnly from, DateOnly to, CancellationToken ct) =>
        db.DailyPlans.AsNoTracking().Include(p => p.Tasks).ThenInclude(t => t.Subject)
            .Where(p => p.ChildId == childId && p.Day >= from && p.Day <= to)
            .OrderByDescending(p => p.Day)
            .ToListAsync(ct);

    /// <summary>
    /// Duty days from <paramref name="from"/> to <paramref name="to"/> on which the planner was on and still no plan exists - the
    /// server did not run that day (B-644, decision 19). Such a day costs nothing; the supervisor is told about it. Only closed
    /// days count: today's plan is made by any access, so a running server can never have missed it. A day before the planner
    /// was switched on never counts.
    /// </summary>
    public async Task<List<DateOnly>> MissingDaysAsync(int childId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var since = await db.Children.AsNoTracking().Where(c => c.Id == childId && c.PlannerMode != PlannerMode.Off)
            .Select(c => c.PlannerSince).FirstOrDefaultAsync(ct);
        if (since is not { } start) return [];
        var last = learningDay.Today.AddDays(-1);
        if (to < last) last = to;
        var first = from > start ? from : start;
        if (first > last) return [];

        var planned = (await db.DailyPlans.AsNoTracking()
            .Where(p => p.ChildId == childId && p.Day >= first && p.Day <= last)
            .Select(p => p.Day).ToListAsync(ct)).ToHashSet();
        var dutyDays = await calendar.ForChildAsync(childId, ct);
        var missing = new List<DateOnly>();
        for (var d = last; d >= first; d = d.AddDays(-1))
            if (dutyDays.IsDutyDay(d) && !planned.Contains(d)) missing.Add(d);
        return missing;
    }

    private Task<DailyPlan?> LoadAsync(int childId, DateOnly day, CancellationToken ct) =>
        db.DailyPlans.AsNoTracking().Include(p => p.Tasks).ThenInclude(t => t.Subject)
            .FirstOrDefaultAsync(p => p.ChildId == childId && p.Day == day, ct);

    // The positions of the child's active, running plans, each with what the rules read about it on the planned day.
    private async Task<List<PlannerCandidate>> CandidatesAsync(int childId, DateOnly day, ChildDutyDays dutyDays,
        CancellationToken ct)
    {
        var lastLearningDay = LastDutyDayBefore(day, dutyDays);
        var exams = await ExamsAsync(childId, day, ct);
        var lessonSubjects = await LessonSubjectsAsync(childId, day, ct);
        var weakness = await WeaknessAsync(childId, day, ct);
        var plans = await db.StudyPlans.AsNoTracking()
            .Where(p => p.ChildId == childId && p.Active && p.StartDate <= day && (p.EndDate == null || p.EndDate >= day))
            .OrderBy(p => p.StartDate).ThenBy(p => p.Id)
            .ToListAsync(ct);
        var candidates = new List<PlannerCandidate>();
        var order = 0;
        foreach (var plan in plans)
        {
            // A remediation that ended has left the plan's day (B-636); the planner sees what the child's day shows.
            foreach (var pos in (await progress.LoadPositionsAsync(plan.Id, ct))
                         .Where(p => p.Cadence != GoalCadence.UntilDone || PositionProgressService.HasDutyOn(p, day)))
            {
                order++;
                if (PositionPlayService.PlayedExercise(pos, day) is not { } played || play.TypeOf(played) is not { } type)
                    continue;
                var pool = play.PoolSize(pos, day, (await play.ItemsOfAsync(pos, day, ct: ct)).Count);
                var introduced = await db.PositionItemProgress.AsNoTracking()
                    .Where(p => p.PlanPositionId == pos.Id && p.ExerciseId == played.Id && p.ItemIndex < pool
                        && p.IntroducedAt != null)
                    .Select(p => p.DueOn).ToListAsync(ct);
                var due = pos.UseLeitner ? introduced.Count(d => d is null || d <= day) : 0;
                // New words exist only where a type introduces its cards (vocabulary); an unplayed cloze task is no new word.
                var newAvailable = PositionPlayService.IntroductionStage(type) is null ? 0 : pool - introduced.Count;
                var answers = lastLearningDay is { } y
                    ? await db.ItemReviewEvents.AsNoTracking()
                        .Where(e => e.PlanPositionId == pos.Id && e.ExerciseId == played.Id && e.Day == y && e.BoxBefore != null)
                        .GroupBy(e => e.WasCorrect).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(ct)
                    : [];
                var failedTest = lastLearningDay is { } yt && await db.TestAttempts.AsNoTracking()
                    .AnyAsync(t => t.PlanPositionId == pos.Id && t.Day == yt && t.CompletedAt != null && t.Graded
                        && !t.BySupervisor && !(t.PassedAfterCorrection ?? t.Passed), ct);
                var stage = PositionPlayService.StageForDay(pos, plan, day, type);
                var subjectId = plan.SubjectId ?? played.SeriesUnit?.Series?.SubjectId;
                var examDate = exams.Where(e => e.Covers(played.Id, subjectId)).Select(e => (DateOnly?)e.On).Min();
                candidates.Add(new PlannerCandidate(pos.Id, played.Id, played.Title, subjectId, order, due, newAvailable,
                    answers.Where(a => !a.Key).Sum(a => a.Count), answers.Where(a => a.Key).Sum(a => a.Count), failedTest,
                    stage, EasierStage(type, stage), examDate, examDate is { } ed ? LearningDaysUntil(day, ed, dutyDays) : 0,
                    subjectId is { } sid && lessonSubjects.Contains(sid), subjectId is { } wid ? weakness.GetValueOrDefault(wid) : 0));
            }
        }
        return candidates;
    }

    /// <summary>An upcoming class test with the exercises its bundle covers (B-751): none = the whole subject.</summary>
    private sealed record Exam(DateOnly On, int? SubjectId, HashSet<int> Exercises)
    {
        public bool Covers(int exerciseId, int? subjectId) =>
            Exercises.Count > 0 ? Exercises.Contains(exerciseId) : SubjectId is not null && SubjectId == subjectId;
    }

    // The child's class tests not yet written, from the planned day on. A test on the planned day itself still counts: the day is
    // its last chance. Several per subject are all read, so a near partial test pulls its exercises before the later one.
    private async Task<List<Exam>> ExamsAsync(int childId, DateOnly day, CancellationToken ct)
    {
        var tests = await db.Klassenarbeiten.AsNoTracking()
            .Where(k => k.ChildId == childId && k.Status == KlassenarbeitStatus.Planned && k.ScheduledDate >= day)
            .Select(k => new { k.Id, k.ScheduledDate, k.SubjectId }).ToListAsync(ct);
        var exams = new List<Exam>(tests.Count);
        foreach (var t in tests)
            exams.Add(new Exam(t.ScheduledDate, t.SubjectId, [.. await ClassTestBundle.ExerciseIdsAsync(db, [t.Id], ct)]));
        return exams;
    }

    // Learning days from the planned day up to the day before the test, at least 1: the test day itself learns nothing new.
    private static int LearningDaysUntil(DateOnly day, DateOnly exam, ChildDutyDays dutyDays)
    {
        var days = 0;
        for (var d = day; d < exam; d = d.AddDays(1))
            if (dutyDays.IsDutyDay(d)) days++;
        return Math.Max(1, days);
    }

    // Subjects with a timetable lesson on the planned day or the day after (the day before a lesson is its preparation).
    private async Task<HashSet<int>> LessonSubjectsAsync(int childId, DateOnly day, CancellationToken ct)
    {
        var weekdays = new[] { day.DayOfWeek, day.AddDays(1).DayOfWeek };
        return [.. await db.TimetableEntries.AsNoTracking()
            .Where(t => t.ChildId == childId && weekdays.Contains(t.DayOfWeek)).Select(t => t.SubjectId).Distinct().ToListAsync(ct)];
    }

    // The subject's share of wrong answers over the last 28 days before the planned day (B-733 evaluation rows); no answers = 0 (no signal).
    private async Task<Dictionary<int, double>> WeaknessAsync(int childId, DateOnly day, CancellationToken ct)
    {
        var from = day.AddDays(-28);
        var rows = await db.DailyEvaluations.AsNoTracking()
            .Where(d => d.ChildId == childId && d.SubjectId != null && d.Day >= from && d.Day < day)
            .GroupBy(d => d.SubjectId!.Value)
            .Select(g => new { Subject = g.Key, Right = g.Sum(d => d.CorrectAnswers), Wrong = g.Sum(d => d.WrongAnswers) })
            .ToListAsync(ct);
        return rows.Where(x => x.Right + x.Wrong > 0).ToDictionary(x => x.Subject, x => x.Wrong / (double)(x.Right + x.Wrong));
    }

    // The next rung down the type's ease ladder; a stage off the ladder or at its end has no easier form.
    private static int? EasierStage(IExerciseType type, int stage)
    {
        var ladder = type.EaseLadder;
        for (var i = 0; i < ladder.Count - 1; i++)
            if (ladder[i] == stage) return ladder[i + 1];
        return null;
    }

    // "Yesterday" for rule 2 is the last day that owed a duty: what failed on Saturday comes back on Monday.
    private static DateOnly? LastDutyDayBefore(DateOnly day, ChildDutyDays dutyDays)
    {
        for (var d = day.AddDays(-1); d >= day.AddDays(-14); d = d.AddDays(-1))
            if (dutyDays.IsDutyDay(d)) return d;
        return null;
    }

    // Consolidated words per learning day over the last seven closed days, all subjects (B-733); null without a learning day.
    private async Task<double?> TempoAsync(int childId, DateOnly today, CancellationToken ct)
    {
        await evaluations.EnsureClosedDaysAsync(childId, ct);
        var from = today.AddDays(-7);
        var days = await db.DailyEvaluations.AsNoTracking()
            .Where(d => d.ChildId == childId && d.Day >= from && d.Day < today)
            .GroupBy(d => d.Day)
            .Select(g => new
            {
                Active = g.Sum(d => d.CorrectAnswers + d.WrongAnswers + d.NewWords + d.PracticedSeconds) > 0,
                Consolidated = g.Sum(d => d.ConsolidatedWords),
            })
            .ToListAsync(ct);
        var learningDays = days.Count(d => d.Active);
        return learningDays == 0 ? null : (double)days.Sum(d => d.Consolidated) / learningDays;
    }
}
