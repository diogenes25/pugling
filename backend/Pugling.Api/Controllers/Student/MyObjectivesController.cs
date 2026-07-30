using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;

namespace Pugling.Api.Controllers.Student;

/// <summary>
/// The child's "big goals" from their own perspective (read-only view): the big goal, its key results with
/// progress and the reward preview. The progress is computed live; the <c>rewarded</c> flag shows
/// whether the completion payout has already been credited. Earned rewards are credited idempotently
/// on <b>child login</b> (<c>AuthController</c> → <c>ObjectiveRewardService</c>, there is no scheduler) – this
/// endpoint deliberately stays side-effect-free (GET). Only <b>active</b> goals are shown. Management lies
/// with the father (<c>supervisor/children/{childId}/objectives</c>).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Student + "/me/objectives")]
[Tags("Student – Objectives")]
[Produces("application/json")]
[Authorize]
public class MyObjectivesController(ObjectiveService objectives) : ControllerBase
{
    /// <summary>
    /// Own active big goals with key-result progress (open/overdue first), paged.
    /// Total count in the <c>X-Total-Count</c> header.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ObjectiveResponse>>> List(
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake, CancellationToken ct = default)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();

        var all = (await objectives.ListAsync(cid.Value, status: null, kind: null, ct)).Where(o => o.Active).ToList();
        return all.ToPagedList(Response, skip, take);
    }

    /// <summary>A single own big goal (detail view for the list). 404 if it does not exist (for you).</summary>
    [HttpGet("{objectiveId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ObjectiveResponse>> Get(int objectiveId, CancellationToken ct = default)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();

        // Nur aktive Ziele sind für den Sohn sichtbar (deckungsgleich zur Liste); ein deaktiviertes → 404.
        return await objectives.GetAsync(cid.Value, objectiveId, ct) is { Active: true } o ? o : NotFound();
    }
}
