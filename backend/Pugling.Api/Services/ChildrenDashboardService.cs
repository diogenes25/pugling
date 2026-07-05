using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;

namespace Pugling.Api.Services;

/// <summary>
/// Kindübergreifendes Tages-Dashboard für den Vater: fasst je Kind den Stand eines Tages über alle aktiven
/// Lehrpläne zusammen (Tagesziele erfüllt?, erreichte Punkte, überhaupt geübt?) – beantwortet „wer hat heute
/// bzw. gestern was geschafft/verpasst". Baut auf dem plan-weiten Tages-Rollup des
/// <see cref="PositionProgressService"/> auf.
/// </summary>
public class ChildrenDashboardService(PuglingDbContext db, PositionProgressService progress)
{
    /// <summary>Tagesstand eines Kindes, aggregiert über seine aktiven Lehrpläne.</summary>
    public record ChildDay(int ChildId, string Name, int ActivePlans, int GoalsTotal, int GoalsMet,
        int PointsToday, bool DutyDone, bool Practiced);

    /// <summary>Der Tagesüberblick über alle Kinder eines Vaters.</summary>
    public record Dashboard(DateOnly Date, IReadOnlyList<ChildDay> Children);

    /// <summary>Baut den Tagesüberblick für alle Kinder des Vaters am angegebenen Tag.</summary>
    public async Task<Dashboard> BuildAsync(int fatherId, DateOnly date, CancellationToken ct = default)
    {
        var children = await db.Children.AsNoTracking()
            .Where(c => c.FatherId == fatherId)
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync(ct);

        var rows = new List<ChildDay>(children.Count);
        foreach (var child in children)
        {
            // Nur an dem Tag laufende, aktive Pläne zählen zum Tagessoll.
            var plans = await db.StudyPlans.AsNoTracking()
                .Where(p => p.ChildId == child.Id && p.Active && p.StartDate <= date && p.EndDate >= date)
                .ToListAsync(ct);

            int goalsTotal = 0, goalsMet = 0, points = 0, plansWithDuty = 0, plansDone = 0;
            foreach (var plan in plans)
            {
                var day = await progress.ComputeDayAsync(plan, date);
                goalsTotal += day.GoalsTotal;
                goalsMet += day.GoalsMet;
                points += day.PointsAwarded;
                if (day.GoalsTotal > 0) { plansWithDuty++; if (day.DutyDone) plansDone++; }
            }

            // Pflicht erfüllt, wenn es ein Tagessoll gibt UND alle solchen Pläne es geschafft haben.
            var dutyDone = plansWithDuty > 0 && plansDone == plansWithDuty;
            rows.Add(new ChildDay(child.Id, child.Name, plans.Count, goalsTotal, goalsMet, points,
                dutyDone, goalsMet > 0 || points > 0));
        }

        return new Dashboard(date, rows);
    }
}
