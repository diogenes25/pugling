using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// Calculates a child's progress metrics server-side from the existing tables
/// (practice sessions, reviews, tests, daily rewards). A shared source for missions
/// (time-slot related) and awards (lifetime or current streak). Read-only queries only.
/// </summary>
public class MetricsService(PuglingDbContext db, PositionProgressService progress)
{
    /// <summary>
    /// Value of a metric for a child within the half-open day window [<paramref name="from"/>, <paramref name="to"/>]
    /// (both inclusive; null = unbounded). <paramref name="today"/> is used for the streak calculation.
    /// </summary>
    public async Task<int> ValueAsync(int childId, ProgressMetric metric, DateOnly? from, DateOnly? to, DateOnly today,
        CancellationToken ct = default)
    {
        var lo = from ?? DateOnly.MinValue;
        var hi = to ?? DateOnly.MaxValue;

        return metric switch
        {
            ProgressMetric.NewWords => await NewWordsAsync(childId, lo, hi, ct),

            ProgressMetric.CorrectReviews => await db.ReviewEvents
                .Where(r => r.WasCorrect && r.PracticeSession!.StudyPlan!.ChildId == childId
                    && r.PracticeSession!.Day >= lo && r.PracticeSession!.Day <= hi)
                .CountAsync(ct),

            ProgressMetric.TestsPassed => await db.TestAttempts
                .Where(t => t.Passed && t.CompletedAt != null && t.StudyPlan!.ChildId == childId
                    && t.Day >= lo && t.Day <= hi)
                .CountAsync(ct),

            ProgressMetric.MinutesPracticed => (await db.PracticeSessions
                .Where(s => s.StudyPlan!.ChildId == childId && s.Day >= lo && s.Day <= hi)
                .SumAsync(s => (int?)s.ActiveSeconds, ct) ?? 0) / 60,

            ProgressMetric.DaysComplete => (await CompleteDaysAsync(childId, today, ct))
                .Count(d => d >= lo && d <= hi),

            ProgressMetric.StreakDays => CurrentStreak(await CompleteDaysAsync(childId, today, ct), today),

            _ => 0,
        };
    }

    /// <summary>
    /// Contents the child met for the first time within [<paramref name="lo"/>, <paramref name="hi"/>]. A position on a
    /// build path keeps one progress row per exercise it played, so the same word is introduced again on every level;
    /// it counts only on the exercise the position introduced first. Otherwise a mission for new words would pay the
    /// path's words once per level. Contents only a later level brings are not counted - the cautious side for a
    /// metric that pays coins.
    /// </summary>
    private async Task<int> NewWordsAsync(int childId, DateOnly lo, DateOnly hi, CancellationToken ct)
    {
        var introduced = db.PositionItemProgress.AsNoTracking()
            .Where(p => p.PlanPosition!.StudyPlan!.ChildId == childId && p.IntroducedAt != null);
        var plain = await introduced
            .Where(p => p.PlanPosition!.CurrentBuildLevel == null && p.IntroducedAt >= lo && p.IntroducedAt <= hi)
            .CountAsync(ct);
        // Which exercise came first is a question per position; answered in memory over the few staged positions.
        var staged = await introduced
            .Where(p => p.PlanPosition!.CurrentBuildLevel != null)
            .Select(p => new { p.Id, p.PlanPositionId, p.ExerciseId, Day = p.IntroducedAt!.Value })
            .ToListAsync(ct);
        return plain + staged.GroupBy(p => p.PlanPositionId).Sum(rows =>
        {
            var first = rows.OrderBy(p => p.Day).ThenBy(p => p.Id).First().ExerciseId;
            return rows.Count(p => p.ExerciseId == first && p.Day >= lo && p.Day <= hi);
        });
    }

    /// <summary>
    /// Days up to and including <paramref name="until"/> that are complete for the child – <b>the same</b> rule
    /// (<see cref="PositionProgressService.CompleteChildDaysAsync"/>, B-606) as the streak of the daily box and the
    /// day view: one plan done while another active plan is open is no complete day. Deliberately routed through the
    /// progress service rather than a plain reward query: "at least one goal booked" ≠ "day complete", and
    /// missions/awards must not fire on only partially completed days.
    /// </summary>
    private Task<IReadOnlySet<DateOnly>> CompleteDaysAsync(int childId, DateOnly until, CancellationToken ct) =>
        progress.CompleteChildDaysAsync(childId, until, ct);

    /// <summary>Length of the current streak of complete days up to and including today or yesterday.</summary>
    private static int CurrentStreak(IReadOnlySet<DateOnly> set, DateOnly today)
    {
        if (set.Count == 0) return 0;

        // The streak may still be open today: count from today if today is already complete, otherwise from yesterday.
        var cursor = set.Contains(today) ? today : today.AddDays(-1);
        var streak = 0;
        while (set.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }
        return streak;
    }
}
