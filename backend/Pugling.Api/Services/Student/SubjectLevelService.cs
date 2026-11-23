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
/// (child login, reading the levels). A child that does not show up for days is not re-evaluated - deliberately: no
/// lowering without learning. Once a daily planner exists (B-644), the call moves there.
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
    public async Task EvaluateAsync(int childId, DateOnly day, CancellationToken ct)
    {
        var measures = await progress.ActiveSubjectMeasuresAsync(childId, ct);
        if (measures.Count == 0) return;

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
        if (!changed) return;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteUniqueViolation })
        {
            // Two first accesses of the same day raced to create the row; the other one won with the same data and the
            // same day, so its row is this one's result. Detach ours so a later SaveChanges of the request does not
            // trip over it again.
            foreach (var entry in db.ChangeTracker.Entries<ChildSubjectLevel>().ToList())
                entry.State = EntityState.Detached;
        }
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
