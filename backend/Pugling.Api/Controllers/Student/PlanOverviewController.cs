using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Models;
using Pugling.Api.Services;

namespace Pugling.Api.Controllers.Student;

/// <summary>
/// Tages-/Verlaufs-Sicht eines Lehrplans über seine <see cref="PlanPosition"/>en (neues Modell).
/// Der Sohn holt hier seine Tagesmission (welche Übungen sind heute dran, was ist erledigt, Streak),
/// der Vater den Tag-für-Tag-Verlauf. Ersetzt die plan-weite Today/Progress-Sicht des alten Modells.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Student + "/study-plans/{planId:int}/overview")]
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

    /// <summary>
    /// Tag-für-Tag-Verlauf über die gesamte Laufzeit (erledigte Tage, erreichte Ziele, Punkte).
    /// Die Kennzahlen (<c>DaysComplete</c>/<c>TotalPoints</c>/<c>CurrentStreak</c>) beziehen sich bewusst
    /// auf die <b>gesamte</b> Laufzeit; Filter/Sortierung/Paging wirken nur auf <c>Days</c>. Die gefilterte
    /// Gesamtzahl der Tage steht im Header <c>X-Total-Count</c>.
    /// </summary>
    /// <param name="planId">Lehrplan, dessen Verlauf gelesen wird.</param>
    /// <param name="from">Nur Tage ab diesem Datum (inklusive).</param>
    /// <param name="to">Nur Tage bis zu diesem Datum (inklusive).</param>
    /// <param name="dutyDone">Nur Tage mit erledigter (<c>true</c>) bzw. offener (<c>false</c>) Pflicht.</param>
    /// <param name="sort">Sortierung: <c>day</c> (Standard), <c>-day</c>, <c>points</c>, <c>-points</c>.</param>
    /// <param name="skip">Anzahl zu überspringender Tage (Paging).</param>
    /// <param name="take">Maximale Tages-Zahl (1..500). Gesamtzahl im Header <c>X-Total-Count</c>.</param>
    [HttpGet("progress")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProgressResponse>> Progress(int planId,
        [FromQuery] DateOnly? from = null, [FromQuery] DateOnly? to = null, [FromQuery] bool? dutyDone = null,
        [FromQuery] string? sort = null,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake)
    {
        var plan = await GetPlan(planId);
        if (plan is null) return NotFound();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var view = await progress.ProgressViewAsync(plan, today, from, to, dutyDone, sort);
        // Paging (HTTP-Belang) auf die bereits gefilterte/sortierte Tagesliste; X-Total-Count = gefilterte Gesamtzahl.
        var page = view.Days.ToPagedList(Response, skip, take);

        return new ProgressResponse(plan.Id, plan.StartDate, plan.EndDate,
            view.DaysComplete, view.TotalDays, view.TotalPoints, view.CurrentStreak, page);
    }
}
