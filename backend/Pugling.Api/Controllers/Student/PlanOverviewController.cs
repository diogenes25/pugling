using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Student;

/// <summary>
/// Daily/history view of a study plan via its <see cref="PlanPosition"/>s (new model).
/// Here the child fetches their daily mission (which exercises are due today, what's done, streak),
/// the father the day-by-day history. Replaces the plan-wide today/progress view of the old model.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Student + "/study-plans/{planId:int}/overview")]
[Tags("Student – Plan Overview")]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(PlanOwnershipFilter))]
public class PlanOverviewController(PuglingDbContext db, PositionProgressService progress, DailyBoxService dailyBox) : ControllerBase
{
    // No default for `ct`: it would make the call site look correct while the client's cancellation fizzles out.
    private Task<StudyPlan?> GetPlan(int planId, CancellationToken ct) =>
        db.StudyPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == planId, ct);

    /// <summary>Daily mission: positions due today with status, mandatory goal completion and current streak.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OverviewResponse>> Get(int planId, CancellationToken ct = default)
    {
        var plan = await GetPlan(planId, ct);
        if (plan is null) return NotFound();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var days = await progress.ProgressAsync(plan, today, ct);
        var todayOverview = await progress.ComputeDayAsync(plan, today, ct);
        var boxStatus = await dailyBox.StatusAsync(plan.ChildId, today, ct);
        return new OverviewResponse(plan.Id, plan.Title, plan.StartDate, plan.EndDate, plan.Active,
            PositionProgressService.Streak(days, today), todayOverview, boxStatus);
    }

    /// <summary>
    /// Day-by-day history over the entire run time (completed days, goals reached, points).
    /// The metrics (<c>DaysComplete</c>/<c>TotalPoints</c>/<c>CurrentStreak</c>) deliberately refer
    /// to the <b>entire</b> run time; filter/sort/paging only affect <c>Days</c>. The filtered
    /// total number of days is in the <c>X-Total-Count</c> header.
    /// </summary>
    /// <param name="planId">Study plan whose history is being read.</param>
    /// <param name="from">Only days from this date (inclusive).</param>
    /// <param name="to">Only days up to this date (inclusive).</param>
    /// <param name="dutyDone">Only days with completed (<c>true</c>) or open (<c>false</c>) mandatory goal.</param>
    /// <param name="sort">Sort: <c>day</c> (default), <c>-day</c>, <c>points</c>, <c>-points</c>.</param>
    /// <param name="skip">Number of days to skip (paging).</param>
    /// <param name="take">Maximum number of days (1..500). Total count in the <c>X-Total-Count</c> header.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("progress")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProgressResponse>> Progress(int planId,
        [FromQuery] DateOnly? from = null, [FromQuery] DateOnly? to = null, [FromQuery] bool? dutyDone = null,
        [FromQuery] string? sort = null,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake, CancellationToken ct = default)
    {
        var plan = await GetPlan(planId, ct);
        if (plan is null) return NotFound();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var view = await progress.ProgressViewAsync(plan, today, from, to, dutyDone, sort, ct);
        // Paging (an HTTP concern) on the already filtered/sorted day list; X-Total-Count = the filtered total.
        var page = view.Days.ToPagedList(Response, skip, take);

        return new ProgressResponse(plan.Id, plan.StartDate, plan.EndDate,
            view.DaysComplete, view.TotalDays, view.TotalPoints, view.CurrentStreak, page);
    }
}
