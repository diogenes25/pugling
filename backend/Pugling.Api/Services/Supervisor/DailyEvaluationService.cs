using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Supervisor;

/// <summary>
/// The supervisor's evaluation of a child's learning (B-733): one row per closed day and subject, written once, plus the
/// profile over the stored days and the problem words. The evaluation reads only; it changes nothing
/// the child sees or is charged for.
/// <para>
/// There is no scheduler: the closed days of the last <see cref="WindowDays"/> days are evaluated lazily on the first read
/// after they closed (<see cref="EvaluatedDay"/>). A stored day is computed again only when evidence for it arrived after it
/// was stored - a supervisor's catch-up for that day (B-1028). A day further back than the window stays a gap.
/// </para>
/// </summary>
public class DailyEvaluationService(PuglingDbContext db, PositionProgressService progress, DutyCalendar calendar,
    LearningDay learningDay, TimeProvider time)
{
    /// <summary>How far back closed days are evaluated, and the longer profile window.</summary>
    public const int WindowDays = 28;
    private const int ShortWindowDays = 7;

    /// <summary>
    /// Evaluates every closed day of the last <see cref="WindowDays"/> days that has not been evaluated yet, and stores its
    /// rows. Idempotent: an evaluated day is not computed again unless new evidence for it arrived since (see
    /// <see cref="ReopenedDaysAsync"/>), so its stored figures stay as they were read. Requests for the same child wait for
    /// each other; a second process would lose on the unique index and keep the winner's rows.
    /// </summary>
    public async Task EnsureClosedDaysAsync(int childId, CancellationToken ct)
    {
        // The supervisor page reads the list and the profile at once; both would evaluate the same days. One gate per child
        // lets the second wait and then find the days done, instead of losing on the unique index (a logged database error).
        var gate = Gates.GetOrAdd(childId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            await EvaluateMissingDaysAsync(childId, ct);
        }
        finally
        {
            gate.Release();
        }
    }

    // One server process holds the database (SQLite); a second process would still be caught by the unique index.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, SemaphoreSlim> Gates = new();

    private async Task EvaluateMissingDaysAsync(int childId, CancellationToken ct)
    {
        var today = learningDay.Today;
        var first = today.AddDays(-WindowDays);
        var done = (await db.EvaluatedDays.AsNoTracking()
                .Where(d => d.ChildId == childId && d.Day >= first)
                .Select(d => d.Day).ToListAsync(ct))
            .ToHashSet();
        var reopened = await ReopenedDaysAsync(childId, first, ct);
        // The closed days only: today is still running and is computed live on reading, never stored.
        var missing = Enumerable.Range(0, WindowDays).Select(first.AddDays)
            .Where(d => !done.Contains(d) || reopened.Contains(d)).ToList();
        if (missing.Count == 0) return;

        // Taken before computing: evidence written while the days are computed is newer and reopens them on the next read.
        var now = time.GetUtcNow().UtcDateTime;
        // Computed outside the transaction: SQLite takes its write lock at the transaction's start, and computing up to 28
        // days under it would stall the child's answers.
        var rows = new List<DailyEvaluation>();
        foreach (var day in missing) rows.AddRange(await ComputeDayAsync(childId, day, ct));

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (reopened.Count > 0)
        {
            // The reopened days are replaced as a whole, in the same transaction as their new rows.
            await db.DailyEvaluations.Where(d => d.ChildId == childId && reopened.Contains(d.Day)).ExecuteDeleteAsync(ct);
            await db.EvaluatedDays.Where(d => d.ChildId == childId && reopened.Contains(d.Day)).ExecuteDeleteAsync(ct);
        }
        var evaluated = missing.Select(day => new EvaluatedDay { ChildId = childId, Day = day, CreatedAt = now }).ToList();
        db.DailyEvaluations.AddRange(rows);
        db.EvaluatedDays.AddRange(evaluated);
        try
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Another process evaluated the same days first; its rows stand, ours are dropped. A compensating step: a
            // cancelled request must not decide whether it runs.
            await tx.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
        }
        catch
        {
            // Any other failure (a cancelled request included) leaves the rows unsaved. They must not stay queued in the
            // request's context, or the caller's next SaveChanges would insert them behind its back.
            Detach(rows);
            Detach(evaluated);
            throw;
        }
    }

    private void Detach(IEnumerable<object> entities)
    {
        foreach (var entity in entities) db.Entry(entity).State = EntityState.Detached;
    }

    /// <summary>
    /// The stored days since <paramref name="first"/> for which evidence arrived after they were stored (B-1028): an answer of
    /// that learning day written later (a vocabulary answer, or any card answered in a round of that day), a round of that day
    /// started or ended later, or a test of that day handed in later.
    /// That is a supervisor's catch-up for a past day; checked here, where every day is read, so no write path can miss it.
    /// </summary>
    private async Task<HashSet<DateOnly>> ReopenedDaysAsync(int childId, DateOnly first, CancellationToken ct)
    {
        var stored = db.EvaluatedDays.Where(d => d.ChildId == childId && d.Day >= first);
        var byAnswer = await stored
            .Where(d => db.ItemReviewEvents.Any(e => e.ChildId == childId && e.Day == d.Day && e.At > d.CreatedAt))
            .Select(d => d.Day).ToListAsync(ct);
        // Cards of other exercise types write no item history; their round's review rows carry the time instead.
        var byCard = await stored
            .Where(d => db.ReviewEvents.Any(r => r.PracticeSession!.StudyPlan!.ChildId == childId && r.PracticeSession.Day == d.Day
                && r.At > d.CreatedAt))
            .Select(d => d.Day).ToListAsync(ct);
        var byRound = await stored
            .Where(d => db.PracticeSessions.Any(s => s.StudyPlan!.ChildId == childId && s.Day == d.Day
                && (s.StartedAt > d.CreatedAt || s.EndedAt > d.CreatedAt)))
            .Select(d => d.Day).ToListAsync(ct);
        var byTest = await stored
            .Where(d => db.TestAttempts.Any(t => t.StudyPlan!.ChildId == childId && t.Day == d.Day && t.CompletedAt > d.CreatedAt))
            .Select(d => d.Day).ToListAsync(ct);
        return [.. byAnswer, .. byCard, .. byRound, .. byTest];
    }

    /// <summary>
    /// The stored days in [<paramref name="from"/>, <paramref name="to"/>], newest first, plus today's live row when today
    /// lies in the range. Reads stored figures only - a closed day is computed again only for late evidence (B-1028).
    /// </summary>
    public async Task<List<DailyEvaluationResponse>> ListAsync(int childId, DateOnly from, DateOnly to, int? subjectId,
        CancellationToken ct)
    {
        await EnsureClosedDaysAsync(childId, ct);
        var stored = await db.DailyEvaluations.AsNoTracking().Include(d => d.Subject)
            .Where(d => d.ChildId == childId && d.Day >= from && d.Day <= to
                && (subjectId == null || d.SubjectId == subjectId))
            .ToListAsync(ct);
        var rows = stored.Select(d => Map(d, live: false)).ToList();

        var today = learningDay.Today;
        if (from <= today && today <= to)
        {
            var names = await SubjectNamesAsync(ct);
            rows.AddRange((await ComputeDayAsync(childId, today, ct))
                .Where(d => subjectId == null || d.SubjectId == subjectId)
                .Select(d => Map(d, live: true, d.SubjectId is { } id ? names.GetValueOrDefault(id, "") : "")));
        }
        // Sorted in memory: SQLite cannot order by DateOnly in every provider version, and a page is small.
        return rows.OrderByDescending(r => r.Day).ThenBy(r => r.SubjectName, StringComparer.Ordinal)
            .ThenBy(r => r.SubjectId).ToList();
    }

    /// <summary>
    /// The profile per subject over the last 7 and 28 closed days, from the stored rows only (decision 2 of B-733, AK 4).
    /// </summary>
    public async Task<EvaluationProfileResponse> ProfileAsync(int childId, CancellationToken ct)
    {
        await EnsureClosedDaysAsync(childId, ct);
        var until = learningDay.Today.AddDays(-1);
        var from28 = until.AddDays(-(WindowDays - 1));
        var from7 = until.AddDays(-(ShortWindowDays - 1));
        var rows = await db.DailyEvaluations.AsNoTracking().Include(d => d.Subject)
            .Where(d => d.ChildId == childId && d.Day >= from28 && d.Day <= until)
            .ToListAsync(ct);
        var subjects = rows.GroupBy(d => d.SubjectId)
            .Select(g => new SubjectProfile(g.Key, g.First().Subject?.Name ?? "",
                Window(g.Where(d => d.Day >= from7).ToList(), ShortWindowDays),
                Window(g.ToList(), WindowDays)))
            .OrderBy(s => s.SubjectName, StringComparer.Ordinal).ThenBy(s => s.SubjectId)
            .ToList();
        return new EvaluationProfileResponse(until, subjects);
    }

    /// <summary>
    /// The words the child got wrong in [<paramref name="from"/>, <paramref name="to"/>], most wrong answers first, with the
    /// kinds of the wrong answers and the word's current box. Read live from the answers: the per-day rows hold counts, not
    /// words, and a problem word is about what is wrong now.
    /// </summary>
    public async Task<List<ProblemWordResponse>> ProblemWordsAsync(int childId, DateOnly from, DateOnly to, int? subjectId,
        CancellationToken ct)
    {
        var legacyUntil = await LegacyUntilAsync(childId, ct);
        // By learning day, not timestamp (B-1028): a catch-up for a past day belongs to that day.
        var events = (await db.ItemReviewEvents.AsNoTracking()
                .Where(e => e.ChildId == childId && e.Day >= from && e.Day <= to)
                .Select(e => new
                {
                    e.VocabularyId,
                    e.ItemId,
                    e.ExerciseId,
                    e.PlanPositionId,
                    e.WasCorrect,
                    e.GivenAnswer,
                    e.ErrorKind,
                    e.At,
                    e.BoxBefore,
                    e.StageValue
                })
                .ToListAsync(ct))
            .Where(e => Counts(e.At, e.BoxBefore, e.GivenAnswer, e.StageValue, legacyUntil))
            .ToList();
        var subjectOf = await SubjectResolverAsync(events.Select(e => e.ExerciseId), events.Select(e => e.PlanPositionId), ct);
        var withSubject = events.Select(e => (e, Subject: subjectOf(e.ExerciseId, e.PlanPositionId)))
            .Where(x => subjectId == null || x.Subject == subjectId)
            .ToList();
        var wrongWords = withSubject.Where(x => !x.e.WasCorrect).Select(x => x.e.VocabularyId).ToHashSet();
        if (wrongWords.Count == 0) return [];

        var words = await db.Vocabularies.AsNoTracking().Where(v => wrongWords.Contains(v.Id))
            .Select(v => new { v.Id, v.Word, v.Translation }).ToDictionaryAsync(v => v.Id, ct);
        var states = await db.ItemProgress.AsNoTracking()
            .Where(p => p.ChildId == childId && wrongWords.Contains(p.VocabularyId))
            .Select(p => new { p.ItemId, p.Box, p.MasteryPercent }).ToListAsync(ct);
        var names = await SubjectNamesAsync(ct);

        return withSubject.Where(x => wrongWords.Contains(x.e.VocabularyId))
            .GroupBy(x => x.e.VocabularyId)
            .Select(g =>
            {
                var wrong = g.Where(x => !x.e.WasCorrect).OrderByDescending(x => x.e.At).ToList();
                var last = wrong[0];
                var state = states.FirstOrDefault(s => s.ItemId == last.e.ItemId);
                var word = words.GetValueOrDefault(g.Key);
                return new ProblemWordResponse(g.Key, last.Subject,
                    last.Subject is { } id ? names.GetValueOrDefault(id, "") : "",
                    word?.Word ?? "", word?.Translation ?? "", wrong.Count, g.Count() - wrong.Count,
                    CountKinds(wrong.Select(x => x.e.ErrorKind)),
                    wrong.Select(x => x.e.GivenAnswer).FirstOrDefault(a => !string.IsNullOrEmpty(a)), last.e.At,
                    state?.Box ?? 1, state?.MasteryPercent ?? 0);
            })
            .OrderByDescending(p => p.WrongAnswers).ThenByDescending(p => p.LastWrongAt).ThenBy(p => p.VocabularyId)
            .ToList();
    }

    /// <summary>
    /// Computes one day's rows per subject without storing them (B-733, estimation assumption 3). A subject gets a row when
    /// the child practised in it that day or a daily duty of it was owed; a free day without practice gets none.
    /// </summary>
    internal async Task<List<DailyEvaluation>> ComputeDayAsync(int childId, DateOnly day, CancellationToken ct)
    {
        var legacyUntil = await LegacyUntilAsync(childId, ct);
        // By learning day, not timestamp (B-1028): a catch-up for a past day belongs to that day.
        var answers = (await db.ItemReviewEvents.AsNoTracking()
                .Where(e => e.ChildId == childId && e.Day == day)
                .Select(e => new { e.ExerciseId, e.PlanPositionId, e.WasCorrect, e.ErrorKind, e.BoxBefore, e.At, e.GivenAnswer, e.StageValue })
                .ToListAsync(ct))
            .Where(e => Counts(e.At, e.BoxBefore, e.GivenAnswer, e.StageValue, legacyUntil))
            .ToList();
        // New = introduced to the child at a position that day: the stamp the day view and the daily dose read, set only on
        // the child's own first contact (not on a supervisor's preview).
        var introduced = await db.PositionItemProgress.AsNoTracking()
            .Where(p => p.PlanPosition!.StudyPlan!.ChildId == childId && p.IntroducedAt == day)
            .Select(p => new { p.ExerciseId, p.PlanPositionId }).ToListAsync(ct);
        var sessions = await db.PracticeSessions.AsNoTracking()
            .Where(s => s.StudyPlan!.ChildId == childId && s.Day == day && s.ActiveSeconds > 0)
            .Select(s => new { ExerciseId = (int?)s.PlanPosition!.ExerciseId, s.PlanPositionId, s.StudyPlan!.SubjectId, s.ActiveSeconds })
            .ToListAsync(ct);
        var subjectOf = await SubjectResolverAsync(answers.Select(a => a.ExerciseId).Concat(introduced.Select(i => i.ExerciseId))
            .Concat(sessions.Where(s => s.ExerciseId != null).Select(s => s.ExerciseId!.Value)),
            answers.Select(a => a.PlanPositionId).Concat(introduced.Select(i => (int?)i.PlanPositionId))
                .Concat(sessions.Select(s => s.PlanPositionId)), ct);

        // Keyed by subject id, 0 standing for "no subject" (ids start at 1).
        var rows = new Dictionary<int, DailyEvaluation>();
        DailyEvaluation Row(int? subjectId) =>
            rows.TryGetValue(subjectId ?? 0, out var row) ? row
                : rows[subjectId ?? 0] = new DailyEvaluation { ChildId = childId, Day = day, SubjectId = subjectId };

        foreach (var a in answers)
        {
            var row = Row(subjectOf(a.ExerciseId, a.PlanPositionId));
            if (a.WasCorrect)
            {
                row.CorrectAnswers++;
                // Consolidated: this answer moved the word into the top box.
                if (a.BoxBefore is { } before && before < ItemProgress.MaxBox && before + 1 >= ItemProgress.MaxBox)
                    row.ConsolidatedWords++;
            }
            else
            {
                row.WrongAnswers++;
                if (a.BoxBefore is > 1) row.FellBackWords++;
                if (a.ErrorKind is { } kind)
                    row.ErrorKinds[kind.ToString()] = row.ErrorKinds.GetValueOrDefault(kind.ToString()) + 1;
            }
        }
        foreach (var i in introduced) Row(subjectOf(i.ExerciseId, i.PlanPositionId)).NewWords++;
        foreach (var s in sessions)
            Row(s.ExerciseId is { } id ? subjectOf(id, s.PlanPositionId) : s.SubjectId).PracticedSeconds += s.ActiveSeconds;

        await AddDutiesAsync(childId, day, Row, ct);
        return rows.Values.ToList();
    }

    /// <summary>
    /// The day's daily duties per plan subject: owed (<see cref="PositionStatus.OwesDuty"/>) daily goals of every plan running
    /// that day, met or left open. Weekly goals stay out - on a weekday their period is not closed yet. A Sunday or a pause
    /// owes nothing (<see cref="DutyCalendar"/>), so it adds no row. Known limit: only plans active now are read - a plan
    /// switched off since contributes no duty to a day evaluated afterwards (no activation history is stored).
    /// </summary>
    private async Task AddDutiesAsync(int childId, DateOnly day, Func<int?, DailyEvaluation> row, CancellationToken ct)
    {
        var dutyDays = await calendar.ForChildAsync(childId, ct);
        if (!dutyDays.IsDutyDay(day)) return;
        var plans = await db.StudyPlans.AsNoTracking()
            .Where(p => p.ChildId == childId && p.Active && p.StartDate <= day && (p.EndDate == null || p.EndDate >= day))
            .ToListAsync(ct);
        foreach (var plan in plans)
        {
            var owed = (await progress.ComputeDayAsync(plan, day, dutyDays, ct)).Positions
                .Where(s => s.OwesDuty && PositionProgressService.IsDailyDuty(s.Cadence)).ToList();
            if (owed.Count == 0) continue;
            var target = row(plan.SubjectId);
            target.DutiesMet += owed.Count(s => s.GoalMet);
            target.DutiesMissed += owed.Count(s => !s.GoalMet);
        }
    }

    /// <summary>
    /// Does an answer row count as a right or wrong answer of the child? Since B-732 exactly the rows that moved the word's
    /// box (<see cref="ItemReviewEvent.BoxBefore"/> set): a shown Kennenlernen card, a learn-card claim or recall and an eased
    /// recall carry no verdict and stay out. Rows written before B-732 have no box at all; for them a typed answer outside
    /// the learn card counts - the closest the old rows allow (a self-assessment of that time stays out).
    /// </summary>
    internal static bool Counts(DateTime at, int? boxBefore, string? givenAnswer, int stage, DateTime legacyUntil) =>
        boxBefore is not null
        || (at < legacyUntil && !string.IsNullOrEmpty(givenAnswer) && stage != (int)TestStage.LearnCard);

    /// <summary>When the child's first row with a box was written - the end of its rows from before B-732.</summary>
    private async Task<DateTime> LegacyUntilAsync(int childId, CancellationToken ct) =>
        await db.ItemReviewEvents.AsNoTracking().Where(e => e.ChildId == childId && e.BoxBefore != null)
            .MinAsync(e => (DateTime?)e.At, ct) ?? DateTime.MaxValue;

    /// <summary>
    /// The subject of an answer: the series subject of its exercise (catalog truth), else the subject of the plan it was
    /// played in. Loaded once for all given exercises and positions.
    /// </summary>
    private async Task<Func<int, int?, int?>> SubjectResolverAsync(IEnumerable<int> exerciseIds, IEnumerable<int?> positionIds,
        CancellationToken ct)
    {
        var exercises = exerciseIds.Distinct().ToList();
        var positions = positionIds.OfType<int>().Distinct().ToList();
        var bySeries = await db.Exercises.AsNoTracking().Where(e => exercises.Contains(e.Id))
            .Select(e => new { e.Id, e.SeriesUnit!.Series!.SubjectId }).ToDictionaryAsync(e => e.Id, e => e.SubjectId, ct);
        var byPlan = await db.PlanPositions.AsNoTracking().Where(p => positions.Contains(p.Id))
            .Select(p => new { p.Id, p.StudyPlan!.SubjectId }).ToDictionaryAsync(p => p.Id, p => p.SubjectId, ct);
        return (exerciseId, positionId) => bySeries.GetValueOrDefault(exerciseId)
            ?? (positionId is { } p ? byPlan.GetValueOrDefault(p) : null);
    }

    private async Task<Dictionary<int, string>> SubjectNamesAsync(CancellationToken ct) =>
        await db.Subjects.AsNoTracking().ToDictionaryAsync(s => s.Id, s => s.Name, ct);

    private static ProfileWindow Window(IReadOnlyList<DailyEvaluation> days, int length)
    {
        var correct = days.Sum(d => d.CorrectAnswers);
        var wrong = days.Sum(d => d.WrongAnswers);
        var learningDays = days.Count(d => d.CorrectAnswers + d.WrongAnswers + d.NewWords + d.PracticedSeconds > 0);
        var consolidated = days.Sum(d => d.ConsolidatedWords);
        var dutyDays = days.Where(d => d.DutiesMet + d.DutiesMissed > 0).ToList();
        var dutyDaysMet = dutyDays.Count(d => d.DutiesMissed == 0);
        var kinds = days.SelectMany(d => d.ErrorKinds)
            .GroupBy(k => k.Key).Select(g => new ErrorKindCount(g.Key, g.Sum(k => k.Value)))
            .OrderByDescending(k => k.Count).ThenBy(k => k.Kind, StringComparer.Ordinal).ToList();
        return new ProfileWindow(length, learningDays, correct, wrong,
            correct + wrong == 0 ? 0 : (int)Math.Round(100.0 * correct / (correct + wrong)),
            days.Sum(d => d.NewWords), consolidated,
            learningDays == 0 ? 0 : Math.Round((double)consolidated / learningDays, 1),
            kinds, dutyDays.Count, dutyDaysMet,
            dutyDays.Count == 0 ? null : (int)Math.Round(100.0 * dutyDaysMet / dutyDays.Count));
    }

    private static Dictionary<string, int> CountKinds(IEnumerable<AnswerErrorKind?> kinds) =>
        kinds.OfType<AnswerErrorKind>().GroupBy(k => k.ToString()).ToDictionary(g => g.Key, g => g.Count());

    private static DailyEvaluationResponse Map(DailyEvaluation d, bool live, string? subjectName = null) =>
        new(d.Day, d.SubjectId, subjectName ?? d.Subject?.Name ?? "", d.NewWords, d.ConsolidatedWords, d.FellBackWords,
            d.CorrectAnswers, d.WrongAnswers, d.PracticedSeconds, d.ErrorKinds, d.DutiesMet, d.DutiesMissed, live);
}
