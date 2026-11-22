using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;
using Pugling.Api.Errors;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// The father's children over time (B-39): goals met, points and the daily duty per day, week or month - next to the
/// one-day <see cref="ChildrenDashboardController"/>. The computation lives in <see cref="ChildrenDashboardService"/>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/children/overview")]
[Tags("Supervisor – Children")]
[Produces("application/json")]
[Authorize(Roles = Roles.Supervisor)]
public class ChildrenOverviewController(ChildrenDashboardService dashboard, LearningDay learningDay) : ControllerBase
{
    /// <summary>
    /// The timeline of the own children from <paramref name="from"/> to <paramref name="to"/> (default: the last 28 days up
    /// to today's learning day in the family's zone), grouped by <paramref name="granularity"/>. A <c>to</c> after today ends today: a day still to come
    /// would read as a missed duty. At most 92 days and <c>from</c> not after <c>to</c>, otherwise
    /// <c>400 validation_error</c>. Pages over the children (ordered by name); <c>X-Total-Count</c> is the number
    /// of children. <paramref name="childId"/> narrows it to one own child - another one simply does not appear.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChildrenOverview>> Get([FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] OverviewGranularity granularity = OverviewGranularity.Day, [FromQuery] int? childId = null,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(granularity))
            return this.ProblemWithCode(ApiErrors.ValidationError, "granularity must be Day, Week or Month.");
        var today = learningDay.Today;
        var end = to is { } requested && requested < today ? requested : today;
        var start = from ?? end.AddDays(-27);
        if (start > end)
            return this.ProblemWithCode(ApiErrors.ValidationError, "from must not be after to.");
        if (end.DayNumber - start.DayNumber + 1 > ChildrenDashboardService.MaxRangeDays)
            return this.ProblemWithCode(ApiErrors.ValidationError,
                $"The range covers at most {ChildrenDashboardService.MaxRangeDays} days; move from or to closer together.");

        var (rows, total) = await dashboard.TimelineAsync(User.AdultId()!.Value, start, end, granularity, childId,
            skip, Math.Clamp(take, 0, PagingExtensions.MaxTake), ct);
        Response.Headers["X-Total-Count"] = total.ToString();
        return new ChildrenOverview(start, end, granularity, rows);
    }
}
