using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;
using Pugling.Api.Errors;

namespace Pugling.Api.Controllers.Student;

/// <summary>
/// "Vokabel-Türme", the native tower game (B-851): levels built from the vocabulary of a plan position,
/// server-side grading of every token-to-tower assignment, play time and coins (<see cref="TowerGameService"/>). The supervisor may read the
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
    /// Starts a level from the position's words: towers (meanings) and tokens (words) with opaque ids and no mapping
    /// between them; the words are frozen on the run. <c>404</c> when the position is not a playable vocabulary position
    /// of the child with at least five words, <c>409 tower_game_time_limit</c> once today's minutes are used up.
    /// </summary>
    [HttpPost("runs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TowerRunResponse>> Start(int childId, StartTowerRunDto dto, CancellationToken ct)
    {
        if (!User.IsStudent()) return Forbid();
        var outcome = await game.StartAsync(childId, dto.PlanPositionId, ct);
        return outcome.Value is { } run ? run : this.ProblemWithCode(outcome.Error!.Value, outcome.Detail!);
    }

    /// <summary>
    /// Grades the assignment of a token to a tower and records it in the child's learning progress (source
    /// <c>Game</c>). <c>404</c> for an unknown or foreign id, <c>409 tower_game_run_closed</c> once the level ended or
    /// its grace time after the daily limit is over.
    /// </summary>
    [HttpPost("assign")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TowerAssignResult>> Assign(int childId, AssignTowerTokenDto dto, CancellationToken ct)
    {
        if (!User.IsStudent()) return Forbid();
        var outcome = await game.AssignAsync(childId, dto, ct);
        return outcome.Value is { } result ? result : this.ProblemWithCode(outcome.Error!.Value, outcome.Detail!);
    }

    /// <summary>
    /// Counts play time while the level is in the foreground (the app beats about every 15 seconds; each beat adds at most
    /// a few seconds, so a forged or delayed beat cannot add more). <c>timeUp</c> tells the app to end the level.
    /// </summary>
    [HttpPost("runs/{runId:int}/heartbeat")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TowerHeartbeatResult>> Heartbeat(int childId, int runId, CancellationToken ct)
    {
        if (!User.IsStudent()) return Forbid();
        return await game.HeartbeatAsync(childId, runId, ct) is { } beat
            ? beat
            : this.ProblemWithCode(ApiErrors.NotFound, "Tower-game run not found for this child.");
    }

    /// <summary>
    /// Ends the level. The server decides by its own count of solved towers whether it is won and pays the coins, cut
    /// to today's tower-game cap, and books the run's trophies; ending twice returns the same result. The body is
    /// optional: without it the run ends with no wave reported, which earns the words' trophies only.
    /// </summary>
    [HttpPost("runs/{runId:int}/end")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TowerRunEndResult>> End(int childId, int runId, [FromBody] EndTowerRunDto? dto,
        CancellationToken ct)
    {
        if (!User.IsStudent()) return Forbid();
        var outcome = await game.EndAsync(childId, runId, dto?.Wave ?? 0, ct);
        return outcome.Value is { } result ? result : this.ProblemWithCode(outcome.Error!.Value, outcome.Detail!);
    }

    /// <summary>The hero skins of the game with their prices in gems and what the child owns.</summary>
    [HttpGet("skins")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TowerSkinState>> Skins(int childId, CancellationToken ct) =>
        await game.SkinsAsync(childId, ct);

    /// <summary>
    /// Buys a hero skin for gems - the child itself only, like every purchase from its wallet. <c>404</c> for an
    /// unknown skin, <c>409 skin_already_unlocked</c> for one the child owns, <c>400 insufficient_gems</c> when the
    /// balance does not cover the price, <c>409 concurrency_conflict</c> when another wallet write collided.
    /// </summary>
    [HttpPost("skins/{skinId}/purchase")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TowerSkinState>> BuySkin(int childId, string skinId, CancellationToken ct)
    {
        if (!User.IsStudent() || User.ChildId() != childId) return Forbid();
        var outcome = await game.BuySkinAsync(childId, skinId, ct);
        return outcome.Value is { } state ? state : this.ProblemWithCode(outcome.Error!.Value, outcome.Detail!);
    }

    /// <summary>
    /// Opens today's game chest and books its gems. Opening is the child's own moment, so only the child itself opens
    /// it. <c>409 tower_game_chest_none</c> when the chest is not full yet or already opened today.
    /// </summary>
    [HttpPost("chest/open")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TowerGameChestOpened>> OpenChest(int childId, CancellationToken ct)
    {
        // The child itself, not just any student (see DailyBoxController.Open).
        if (!User.IsStudent() || User.ChildId() != childId) return Forbid();
        var outcome = await game.OpenChestAsync(childId, ct);
        return outcome.Value is { } opened ? opened : this.ProblemWithCode(outcome.Error!.Value, outcome.Detail!);
    }
}
