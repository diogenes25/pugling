using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Models;
using Pugling.Api.Services;

namespace Pugling.Api.Controllers.Learn;

/// <summary>
/// Tages-/Verlaufs-Sicht eines Lehrplans über seine <see cref="PlanPosition"/>en (neues Modell).
/// Der Sohn holt hier seine Tagesmission (welche Übungen sind heute dran, was ist erledigt, Streak),
/// der Vater den Tag-für-Tag-Verlauf. Ersetzt die plan-weite Today/Progress-Sicht des alten Modells.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.V1 + "/study-plans/{planId:int}/overview")]
[Tags("Study – Plan Overview")]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(PlanOwnershipFilter))]
public class PlanOverviewController(PuglingDbContext db, PositionProgressService progress) : ControllerBase
{
    public record OverviewResponse(int PlanId, string Title, DateOnly StartDate, DateOnly EndDate, bool Active,
        int CurrentStreak, PositionProgressService.DayOverview Today);

    private Task<StudyPlan?> GetPlan(int planId) =>
        db.StudyPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == planId);

    /// <summary>Tagesmission: heute fällige Positionen mit Status, erledigte Pflicht und aktuelle Streak.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OverviewResponse>> Get(int planId)
    {
        var plan = await GetPlan(planId);
        if (plan is null) return NotFound();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var days = await progress.ProgressAsync(plan, today);
        var todayOverview = await progress.ComputeDayAsync(plan, today);
        return new OverviewResponse(plan.Id, plan.Title, plan.StartDate, plan.EndDate, plan.Active,
            PositionProgressService.Streak(days, today), todayOverview);
    }

    public record ProgressResponse(int PlanId, DateOnly StartDate, DateOnly EndDate, int DaysComplete,
        int TotalDays, int TotalPoints, int CurrentStreak, IReadOnlyList<PositionProgressService.ProgressDay> Days);

    /// <summary>Tag-für-Tag-Verlauf über die gesamte Laufzeit (erledigte Tage, erreichte Ziele, Punkte).</summary>
    [HttpGet("progress")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProgressResponse>> Progress(int planId)
    {
        var plan = await GetPlan(planId);
        if (plan is null) return NotFound();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var days = await progress.ProgressAsync(plan, plan.EndDate);
        var totalDays = plan.EndDate.DayNumber - plan.StartDate.DayNumber + 1;
        return new ProgressResponse(plan.Id, plan.StartDate, plan.EndDate,
            days.Count(d => d.DutyDone), totalDays, days.Sum(d => d.PointsAwarded),
            PositionProgressService.Streak(days, today), days);
    }
}
