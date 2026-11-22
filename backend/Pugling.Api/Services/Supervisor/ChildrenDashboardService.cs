using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;

namespace Pugling.Api.Services.Supervisor;

/// <summary>
/// Cross-child daily dashboard for the supervisor: summarizes, per child, the status of a day across
/// all active study plans (daily goals met?, points earned, practiced at all?) – answers "who
/// achieved/missed what today or yesterday". Builds on the plan-wide daily rollup of
/// <see cref="PositionProgressService"/>.
/// </summary>
public class ChildrenDashboardService(PuglingDbContext db, PositionProgressService progress)
{
    // ChildDay/Dashboard live in the contract project (Pugling.Contracts.Supervisor).

    /// <summary>Builds the daily overview for all of the supervisor's children on the given day.</summary>
    public async Task<Dashboard> BuildAsync(int supervisorId, DateOnly date, CancellationToken ct = default)
    {
        var children = await db.Children.AsNoTracking()
            .Where(c => c.SupervisorLinks.Any(l => l.SupervisorId == supervisorId))
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync(ct);

        var rows = new List<ChildDay>(children.Count);
        foreach (var child in children)
        {
            // The child's one day view (B-606): only plans active and running on that day count, and the obligation is
            // met when there is a goal AND every goal of every such plan is met.
            var day = await progress.ComputeChildDayAsync(child.Id, date, ct);
            var points = day.Plans.Sum(p => p.Overview.PointsAwarded);
            var answers = await CountAnswersAsync(child.Id, date, ct);
            // B-777: an answer is practice even before a goal is met - points only come with a met goal.
            rows.Add(new ChildDay(child.Id, child.Name, day.Plans.Count, day.GoalsTotal, day.GoalsMet, points,
                day.DutyDone, day.GoalsMet > 0 || points > 0 || answers > 0, answers));
        }

        return new Dashboard(date, rows);
    }

    /// <summary>
    /// Answers the child gave on <paramref name="date"/> across all of its plans (B-770): one <see cref="Models.ReviewEvent"/>
    /// per practice card and one <see cref="Models.TestItemResult"/> per test answer. Not <see cref="Models.ItemReviewEvent"/>:
    /// a vocabulary test answer writes one of those next to its TestItemResult (counted twice), and non-vocabulary
    /// practice writes none (missing). Not derived from points either - bonus points are no work done.
    /// <para>
    /// A test row counts only once answered: the start pre-creates one row per question, so an abandoned test would
    /// otherwise count in full. A supervisor's test attempt (preview, catch-up) is not the child's work. Known limits:
    /// a self-assessed "not known" test answer leaves no trace and is missed, a supervisor's practice session cannot be
    /// told apart (no actor column), and bonus training is plan-independent and not counted.
    /// </para>
    /// </summary>
    private async Task<int> CountAnswersAsync(int childId, DateOnly date, CancellationToken ct)
    {
        var practice = await db.ReviewEvents.AsNoTracking()
            .CountAsync(e => e.PracticeSession!.Day == date && e.PracticeSession.StudyPlan!.ChildId == childId, ct);
        var tests = await db.TestAttempts.AsNoTracking()
            .Where(a => a.Day == date && a.StudyPlan!.ChildId == childId && !a.BySupervisor)
            .SumAsync(a => a.Results.Count(r => r.GivenAnswer != null || r.WasCorrect), ct);
        return practice + tests;
    }

    /// <summary>Longest range one timeline request may cover (B-39, decision 9): the cost grows with days × plans × children.</summary>
    public const int MaxRangeDays = 92;

    /// <summary>
    /// The timeline of the supervisor's children (B-39): per child the days from <paramref name="from"/> to
    /// <paramref name="to"/>, computed with the same per-plan rollup as <see cref="BuildAsync"/> and summed per day, week
    /// (Monday to Sunday) or calendar month. Pages over the children, ordered by name; the total is the number of children.
    /// <para>
    /// Unlike the daily overview it also reads plans switched off since (B-558, the fairness rule of B-606 decision 6):
    /// an active plan counts in full; an inactive one adds its met goals and points, its goals total only on days it
    /// met a goal or earned points, and its duty only when met - its open duty never makes a day open.
    /// </para>
    /// </summary>
    public async Task<(IReadOnlyList<ChildTimeline> Rows, int Total)> TimelineAsync(int supervisorId, DateOnly from, DateOnly to,
        OverviewGranularity granularity, int? childId, int skip, int take, CancellationToken ct)
    {
        var query = db.Children.AsNoTracking()
            .Where(c => c.SupervisorLinks.Any(l => l.SupervisorId == supervisorId))
            .Where(c => childId == null || c.Id == childId)
            .OrderBy(c => c.Name).ThenBy(c => c.Id);
        var total = await query.CountAsync(ct);
        var children = await query.Skip(Math.Max(skip, 0)).Take(take).Select(c => new { c.Id, c.Name }).ToListAsync(ct);

        var rows = new List<ChildTimeline>(children.Count);
        foreach (var child in children)
        {
            // Every plan running in the range, also one switched off since (B-558): a chapter change must not erase the
            // weeks before it. A plan off on a day (off now, or switched on only later - B-613) counts in the child's favour.
            // The subject rides along: the day rollup would otherwise look it up once per day (B-614).
            var plans = await db.StudyPlans.AsNoTracking().Include(p => p.Subject)
                .Where(p => p.ChildId == child.Id && p.StartDate <= to && p.EndDate >= from)
                .ToListAsync(ct);
            var days = new List<(DateOnly Day, int GoalsTotal, int GoalsMet, int Points, bool? DutyDone)>();
            for (var day = from; day <= to; day = day.AddDays(1))
            {
                int goalsTotal = 0, goalsMet = 0, points = 0;
                var duties = new List<PositionProgressService.PlanDutyState>();
                foreach (var plan in plans.Where(p => p.StartDate <= day && p.EndDate >= day))
                {
                    var d = await progress.ComputeDayAsync(plan, day, ct);
                    goalsMet += d.GoalsMet;
                    points += d.PointsAwarded;
                    var active = PositionProgressService.ActiveOn(plan, day);
                    // An inactive plan's goals only enter the total on a day it was used, or a draft would read "0 of n".
                    if (active || d.GoalsMet > 0 || d.PointsAwarded > 0) goalsTotal += d.GoalsTotal;
                    duties.Add(new(active, d.GoalsTotal > 0, d.DutyDone));
                }
                // null = no duty that day: such a day neither fulfils nor breaks a period's duty.
                days.Add((day, goalsTotal, goalsMet, points, PositionProgressService.TimelineDutyDone(duties)));
            }

            var periods = days
                .GroupBy(d => PeriodKey(granularity, d.Day))
                .Select(g => new ChildPeriod(g.First().Day, g.Last().Day, g.Sum(d => d.GoalsTotal), g.Sum(d => d.GoalsMet),
                    g.Sum(d => d.Points), g.Any(d => d.DutyDone is not null) && g.All(d => d.DutyDone != false)))
                .ToList();
            rows.Add(new ChildTimeline(child.Id, child.Name, periods));
        }
        return (rows, total);
    }

    // The first day of the period a day belongs to - on the learning-day calendar like all day logic (B-730), the week from Monday like the weekly goals.
    private static DateOnly PeriodKey(OverviewGranularity granularity, DateOnly day) => granularity switch
    {
        OverviewGranularity.Week => PositionProgressService.WeekMonday(day),
        OverviewGranularity.Month => new DateOnly(day.Year, day.Month, 1),
        _ => day,
    };
}
