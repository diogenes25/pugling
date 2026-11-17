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
            rows.Add(new ChildDay(child.Id, child.Name, day.Plans.Count, day.GoalsTotal, day.GoalsMet, points,
                day.DutyDone, day.GoalsMet > 0 || points > 0));
        }

        return new Dashboard(date, rows);
    }

    /// <summary>Longest range one timeline request may cover (B-39, decision 9): the cost grows with days × plans × children.</summary>
    public const int MaxRangeDays = 92;

    /// <summary>
    /// The timeline of the supervisor's children (B-39): per child the days from <paramref name="from"/> to
    /// <paramref name="to"/>, computed with the same daily rollup as <see cref="BuildAsync"/> and summed per day, week
    /// (Monday to Sunday) or calendar month. Pages over the children, ordered by name; the total is the number of children.
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
            // The same plans as the daily overview: active ones, each counted only on the days it runs.
            var plans = await db.StudyPlans.AsNoTracking()
                .Where(p => p.ChildId == child.Id && p.Active && p.StartDate <= to && p.EndDate >= from)
                .ToListAsync(ct);
            var days = new List<(DateOnly Day, int GoalsTotal, int GoalsMet, int Points, bool? DutyDone)>();
            for (var day = from; day <= to; day = day.AddDays(1))
            {
                int goalsTotal = 0, goalsMet = 0, points = 0, withDuty = 0, done = 0;
                foreach (var plan in plans.Where(p => p.StartDate <= day && p.EndDate >= day))
                {
                    var d = await progress.ComputeDayAsync(plan, day, ct);
                    goalsTotal += d.GoalsTotal;
                    goalsMet += d.GoalsMet;
                    points += d.PointsAwarded;
                    if (d.GoalsTotal > 0) { withDuty++; if (d.DutyDone) done++; }
                }
                // null = no daily goal that day: such a day neither fulfils nor breaks a period's duty.
                days.Add((day, goalsTotal, goalsMet, points, withDuty == 0 ? null : done == withDuty));
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

    // The first day of the period a day belongs to - in UTC like all day logic, the week from Monday like the weekly goals.
    private static DateOnly PeriodKey(OverviewGranularity granularity, DateOnly day) => granularity switch
    {
        OverviewGranularity.Week => PositionProgressService.WeekMonday(day),
        OverviewGranularity.Month => new DateOnly(day.Year, day.Month, 1),
        _ => day,
    };
}
