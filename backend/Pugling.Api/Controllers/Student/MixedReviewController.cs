using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;
using Pugling.Api.Errors;

namespace Pugling.Api.Controllers.Student;

/// <summary>
/// The mixed review of a child (<see cref="MixedReviewService"/>, B-764): one short round of the cards due across all of
/// its playable Leitner positions. Voluntary and behind the bonus training's lock; it creates no practice session and so
/// meets, misses and costs no goal. The supervisor may read the overview; playing is the child's own, because the round
/// moves the child's boxes and books the child's coins.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Student + "/children/{childId:int}/mixed-review")]
[Tags("Student – Mixed Review")]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class MixedReviewController(MixedReviewService mixed) : ControllerBase
{
    /// <summary>Today's mixed review: the bonus lock, the cards due for it, an open round and the shared bonus coins.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MixedReviewOverview>> Overview(int childId, CancellationToken ct) =>
        await mixed.OverviewAsync(childId, ct);

    /// <summary>
    /// Starts a mixed review round - or resumes today's open one with the same cards. <c>409 extra_training_locked</c>
    /// before the goals the bonus training waits for are done, <c>409 mixed_review_empty</c> when no card is due in a
    /// position the round may draw from.
    /// </summary>
    [HttpPost("runs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MixedReviewRunResponse>> Start(int childId, CancellationToken ct)
    {
        if (!User.IsStudent()) return Forbid();
        var outcome = await mixed.StartAsync(childId, ct);
        return outcome.Value is { } run ? run : this.ProblemWithCode(outcome.Error!.Value, outcome.Detail!);
    }

    /// <summary>
    /// Grades a mixed review round and books its coins (B-765): each card moves the box of its own position; checked hits
    /// pay within the bonus cap the bonus training shares. A round is graded once: <c>409 mixed_review_completed</c> on a
    /// second submit. <c>409 mixed_review_stale</c> for a round drawn on an earlier day - it is discarded unpaid.
    /// <c>409 extra_training_locked</c> when the goals the bonus training waits for are not met (any more).
    /// </summary>
    [HttpPost("runs/{runId:int}/submit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MixedReviewResult>> Submit(int childId, int runId, SubmitMixedReviewDto dto, CancellationToken ct)
    {
        if (!User.IsStudent()) return Forbid();
        var outcome = await mixed.SubmitAsync(childId, runId, dto.Answers, ct);
        return outcome.Value is { } result ? result : this.ProblemWithCode(outcome.Error!.Value, outcome.Detail!);
    }
}
