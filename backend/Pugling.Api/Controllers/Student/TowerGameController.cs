using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;
using Pugling.Api.Errors;

namespace Pugling.Api.Controllers.Student;

/// <summary>
/// "Vokabel-Türme", the native tower game (B-851 core): runs built from the vocabulary of a plan position, and
/// server-side grading of every token-to-tower assignment (<see cref="TowerGameService"/>). The supervisor may read the
/// overview; playing writes the child's learning progress and is the child's own.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Student + "/children/{childId:int}/tower-game")]
[Tags("Student – Tower Game")]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class TowerGameController(TowerGameService game) : ControllerBase
{
    /// <summary>The vocabulary positions of the child's playable plans a run can be built from.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TowerGameOverview>> Overview(int childId, CancellationToken ct) =>
        await game.OverviewAsync(childId, ct);

    /// <summary>
    /// Starts a run from the position's words: towers (meanings) and tokens (words) with opaque ids and no mapping
    /// between them. <c>404</c> when the position is not a playable vocabulary position of the child with at least five
    /// words.
    /// </summary>
    [HttpPost("runs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TowerRunResponse>> Start(int childId, StartTowerRunDto dto, CancellationToken ct)
    {
        if (!User.IsStudent()) return Forbid();
        return await game.StartAsync(childId, dto.PlanPositionId, ct) is { } run
            ? run
            : this.ProblemWithCode(ApiErrors.NotFound, "No playable vocabulary position with at least five words.");
    }

    /// <summary>
    /// Grades the assignment of a token to a tower and records it in the child's learning progress (source
    /// <c>Game</c>). <c>404</c> for an unknown or foreign id, or a position that is no longer playable.
    /// </summary>
    [HttpPost("assign")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TowerAssignResult>> Assign(int childId, AssignTowerTokenDto dto, CancellationToken ct)
    {
        if (!User.IsStudent()) return Forbid();
        return await game.AssignAsync(childId, dto, ct) is { } result
            ? result
            : this.ProblemWithCode(ApiErrors.NotFound, "Tower or token not found in a playable run of this child.");
    }
}
