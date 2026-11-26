using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;
using Pugling.Api.Errors;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// The supervisor praises a child (B-377): a short plain text the child sees once, with his name, the next time it
/// opens the app. Each supervisor sends and lists only his own praise; "seen" closes the loop.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/children/{childId:int}/praise")]
[Tags("Supervisor – Praise")]
[Produces("application/json")]
[Authorize(Roles = Roles.Supervisor)]
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class PraiseController(PraiseService praise, TimeProvider time) : ControllerBase
{
    /// <summary>
    /// Sends a praise: plain text, 1..140 characters after trimming (400 <c>validation_error</c> otherwise). Books no
    /// coins. 404 for a child you do not supervise.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PraiseResponse>> Create(int childId, CreatePraiseDto dto, CancellationToken ct)
    {
        var text = dto.Text?.Trim() ?? "";
        if (text.Length is 0 or > PraiseService.MaxTextLength)
            return this.ProblemWithCode(ApiErrors.ValidationError,
                $"The praise must have 1 to {PraiseService.MaxTextLength} characters.");

        var p = await praise.SendAsync(childId, User.SupervisorId()!.Value, text, time.GetUtcNow().UtcDateTime, ct);
        return StatusCode(StatusCodes.Status201Created, new PraiseResponse(p.Id, p.ChildId, p.Text, p.CreatedAt, p.SeenAt));
    }

    /// <summary>
    /// Your own praise to this child, newest first, with <c>seenAt</c>; another supervisor's praise is not listed.
    /// </summary>
    /// <param name="childId">Id of the child.</param>
    /// <param name="skip">Number of entries skipped (paging).</param>
    /// <param name="take">Maximum entries (1..500). Total count in the <c>X-Total-Count</c> header.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PraiseResponse>>> List(int childId,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake, CancellationToken ct = default) =>
        await praise.SentBy(childId, User.SupervisorId()!.Value).ToPagedListAsync(Response, skip, take, ct);
}
