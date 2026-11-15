using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;
using Pugling.Api.Errors;

namespace Pugling.Api.Controllers.Student;

/// <summary>
/// The daily reward chest of a child (B-527): earned when today's reward goals are met, opened by the child itself.
/// Opening is the child's own moment, so only the child itself opens it - not a supervisor, not another child; the booked
/// draw shows up in the ledger.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Student + "/children/{childId:int}/daily-box")]
[Tags("Student – Daily Box")]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class DailyBoxController(DailyBoxService dailyBox) : ControllerBase
{
    /// <summary>
    /// Opens the oldest waiting chest and books its coins and gems - the draw fixed on the day it was earned.
    /// <c>409 daily_box_none</c> when no chest is waiting (or a parallel tap opened it first), <c>409
    /// concurrency_conflict</c> when another wallet write collided.
    /// </summary>
    [HttpPost("open")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DailyBoxOpened>> Open(int childId, CancellationToken ct)
    {
        // The child itself, not just any student: an account that is the student of one child and the supervisor of
        // another passes the role check and the (OR-combined) ownership filter for the other child.
        if (!User.IsStudent() || User.ChildId() != childId) return Forbid();
        var outcome = await dailyBox.OpenAsync(childId, ct);
        return outcome.Value is { } opened ? opened : this.ProblemWithCode(outcome.Error!.Value, outcome.Detail!);
    }
}
