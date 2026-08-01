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
            // Only plans active and running on that day count towards the day's obligation.
            var plans = await db.StudyPlans.AsNoTracking()
                .Where(p => p.ChildId == child.Id && p.Active && p.StartDate <= date && p.EndDate >= date)
                .ToListAsync(ct);

            int goalsTotal = 0, goalsMet = 0, points = 0, plansWithDuty = 0, plansDone = 0;
            foreach (var plan in plans)
            {
                var day = await progress.ComputeDayAsync(plan, date, ct);
                goalsTotal += day.GoalsTotal;
                goalsMet += day.GoalsMet;
                points += day.PointsAwarded;
                if (day.GoalsTotal > 0) { plansWithDuty++; if (day.DutyDone) plansDone++; }
            }

            // Obligation met if there is a day's target AND all such plans reached it.
            var dutyDone = plansWithDuty > 0 && plansDone == plansWithDuty;
            rows.Add(new ChildDay(child.Id, child.Name, plans.Count, goalsTotal, goalsMet, points,
                dutyDone, goalsMet > 0 || points > 0));
        }

        return new Dashboard(date, rows);
    }
}
