using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Student;

/// <summary>
/// Evaluates and stores the grade level of a child per vocabulary subject (B-741). The measure is the share of
/// mastered words in the material of the child's active plans (<see cref="ChildLearnProgressService"/>), converted by
/// the one grading key (<see cref="GradeKey"/>).
/// <para>
/// <b>Once per learning day, lazily.</b> There is no scheduler; the evaluation runs on the first access of the day
/// (child login, the child reading its levels, and - B-742 decision 3 - the supervisor reading them). So a level can
/// sink while the child stays away, if its material grew or its words fell back; without any read nothing moves.
/// Once a daily planner exists (B-644), the call moves there.
/// </para>
/// <para>
/// A rise applies at once and without limit; a lowering through the learning state moves at most one step and at most
/// once per <see cref="LoweringIntervalDays"/> days, so one bad day does not topple the level. The best level never
/// gets worse. Nothing here books to the ledger - the level is honour, not coins (B-682 decision 9).
/// </para>
/// </summary>
public class SubjectLevelService(PuglingDbContext db, ChildLearnProgressService progress, IOptions<GradingOptions> grading)
{
    /// <summary>Days that must pass after a lowering before the next one.</summary>
    public const int LoweringIntervalDays = 7;

    // SQLITE_CONSTRAINT_UNIQUE (extended result code).
    private const int SqliteUniqueViolation = 2067;

    /// <summary>
    /// Evaluates every measured subject of the child for <paramref name="day"/>, creating missing rows. Idempotent per
    /// day: a row already evaluated on that day stays as it is.
    /// </summary>
    public async Task EvaluateAsync(int childId, DateOnly day, CancellationToken ct) =>
        await EvaluateAndMeasureAsync(childId, day, ct);

    // Evaluates and hands back the measures it used, so a read shows the levels and the words to the next level from
    // one and the same snapshot instead of computing the rollup twice.
    private async Task<Dictionary<int, (int Mastered, int Total)>> EvaluateAndMeasureAsync(int childId, DateOnly day, CancellationToken ct)
    {
        var measures = await progress.ActiveSubjectMeasuresAsync(childId, ct);
        if (measures.Count == 0) return measures;

        var rows = await db.ChildSubjectLevels.Where(l => l.ChildId == childId).ToDictionaryAsync(l => l.SubjectId, ct);
        var changed = false;
        foreach (var (subjectId, (mastered, total)) in measures)
        {
            var measured = GradeKey.ShareToGrade(mastered, total, grading.Value.Thresholds);
            if (!rows.TryGetValue(subjectId, out var row))
            {
                db.ChildSubjectLevels.Add(new ChildSubjectLevel
                {
                    ChildId = childId,
                    SubjectId = subjectId,
                    Level = measured,
                    BestLevel = measured,
                    EvaluatedOn = day,
                });
                changed = true;
            }
            else if (row.EvaluatedOn < day)
            {
                Apply(row, measured, day);
                changed = true;
            }
        }
        if (changed)
        {
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException e) when (e.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteUniqueViolation })
            {
                // Two first accesses of the same day raced to create the row and the other one won - from nearly the same
                // data (an answer committed in between may differ), which the next day's evaluation evens out anyway.
                // Detach ours so a later SaveChanges of the request does not trip over it again.
                foreach (var entry in db.ChangeTracker.Entries<ChildSubjectLevel>().ToList())
                    entry.State = EntityState.Detached;
            }
        }
        await SnapshotTestDayAsync(childId, day, measures.Keys.ToList(), ct);
        return measures;
    }

    /// <summary>
    /// B-744: writes the evaluated level of each measured subject onto the child's class tests of <paramref name="day"/>
    /// that have a subject and no snapshot yet. Runs on every access of the test day, not only the first, so a level that
    /// first comes into being later that day still fills it (AK 4); a subject without a measure leaves it empty instead
    /// of writing a stand-in value. One conditional UPDATE: of two racing requests the loser finds the column set and
    /// writes nothing, and no later day - nor a moved test date - ever replaces a snapshot. No ledger booking.
    /// </summary>
    private async Task SnapshotTestDayAsync(int childId, DateOnly day, List<int> measuredSubjectIds, CancellationToken ct)
    {
        // One filter for the probe and the write, so the two can never disagree about which test is due.
        var due = db.Klassenarbeiten
            .Where(k => k.ChildId == childId)
            .Where(k => k.ScheduledDate == day)
            .Where(k => k.PredictedLevel == null)
            .Where(k => k.SubjectId != null && measuredSubjectIds.Contains(k.SubjectId.Value))
            .Where(k => db.ChildSubjectLevels.Any(l => l.ChildId == childId && l.SubjectId == k.SubjectId));
        // A read first: an UPDATE takes SQLite's write lock even when it matches nothing, and nearly every access is on
        // a day without a due test.
        if (!await due.AnyAsync(ct)) return;
        await due
            .ExecuteUpdateAsync(s => s.SetProperty(
                k => k.PredictedLevel,
                k => db.ChildSubjectLevels.Where(l => l.ChildId == childId && l.SubjectId == k.SubjectId)
                    .Select(l => (int?)l.Level).FirstOrDefault()), ct);
    }

    /// <summary>
    /// Evaluates the day (if not done yet) and returns the child's levels, one per measured subject, sorted by subject
    /// name. A stored level whose subject has no measure any more (plan deactivated) is kept for its best value but not
    /// listed - without material there is no goal to show. <see cref="SubjectLevelResponse.WordsToNextLevel"/> is counted
    /// live from today's data against the bound of the next better grade (<see cref="GradeKey.MinPartForGrade"/>), so it
    /// drops with every mastered word while the level itself waits for the next day's evaluation.
    /// </summary>
    public async Task<List<SubjectLevelResponse>> ReadAsync(int childId, DateOnly day, CancellationToken ct)
    {
        var measures = await EvaluateAndMeasureAsync(childId, day, ct);
        if (measures.Count == 0) return [];
        var subjectIds = measures.Keys.ToList();
        var rows = await db.ChildSubjectLevels.AsNoTracking()
            .Where(l => l.ChildId == childId && subjectIds.Contains(l.SubjectId))
            .ToListAsync(ct);
        var names = await db.Subjects.AsNoTracking().Where(s => subjectIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

        var thresholds = grading.Value.Thresholds;
        return rows
            .Select(r =>
            {
                var (mastered, total) = measures[r.SubjectId];
                int? toNext = r.Level == GradeKey.BestGrade
                    ? null
                    : Math.Max(0, GradeKey.MinPartForGrade(r.Level - 1, total, thresholds) - mastered);
                return new SubjectLevelResponse(r.SubjectId, names.GetValueOrDefault(r.SubjectId, ""), r.Level, r.BestLevel, toNext);
            })
            .OrderBy(r => r.SubjectName, StringComparer.CurrentCulture).ThenBy(r => r.SubjectId)
            .ToList();
    }

    /// <summary>
    /// Moves a stored level towards the measured grade for <paramref name="day"/>: a better measure lifts it at once, a
    /// worse one lowers it by one step if the last lowering is at least <see cref="LoweringIntervalDays"/> days back.
    /// </summary>
    internal static void Apply(ChildSubjectLevel row, int measured, DateOnly day)
    {
        if (measured < row.Level)
            row.Level = measured;
        else if (measured > row.Level
                 && (row.LastLoweredOn is not { } last || day.DayNumber - last.DayNumber >= LoweringIntervalDays))
        {
            row.Level += 1;
            row.LastLoweredOn = day;
        }
        row.BestLevel = Math.Min(row.BestLevel, row.Level);
        row.EvaluatedOn = day;
    }
}
