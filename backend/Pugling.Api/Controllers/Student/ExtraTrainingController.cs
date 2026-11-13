using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;
using Pugling.Api.Errors;

namespace Pugling.Api.Controllers.Student;

/// <summary>
/// The bonus training of a child: extra exercises on its current topic that pay coins beyond the day's duty
/// (<see cref="ExtraTrainingService"/>). It opens once today's duty is met, pays only for checked hits, is capped
/// per day, and pays for an exercise once - a vocabulary exercise asks harder with every completed run instead.
/// The supervisor may read the pool; playing is the child's own, because the coins are booked to the child.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Student + "/children/{childId:int}/extra-training")]
[Tags("Student – Bonus Training")]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class ExtraTrainingController(ExtraTrainingService extras) : ControllerBase
{
    /// <summary>Today's bonus training: whether it is open, the bonus coins so far against the cap, and the exercise pool.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExtraTrainingOverview>> Overview(int childId, CancellationToken ct) =>
        await extras.OverviewAsync(childId, ct);

    /// <summary>
    /// Starts a bonus run of the exercise - or resumes its open run, which keeps its form and cards. <c>409
    /// extra_training_locked</c> before today's duty is done, <c>409 extra_exercise_done</c> for a one-shot exercise
    /// already played, <c>409 extra_cap_reached</c> for a one-shot exercise - new or open - once today's coin cap is
    /// full, so it is not used up for nothing.
    /// </summary>
    [HttpPost("{exerciseId:int}/runs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExtraRunResponse>> Start(int childId, int exerciseId, CancellationToken ct)
    {
        if (!User.IsStudent()) return Forbid();
        var outcome = await extras.StartAsync(childId, exerciseId, ct);
        return outcome.Value is { } run ? run : this.ProblemWithCode(outcome.Error!.Value, outcome.Detail!);
    }

    /// <summary>
    /// Grades a bonus run and books its coins (checked hits only, within the daily cap). A run is graded once:
    /// <c>409 extra_run_completed</c> on a second submit. A one-shot run handed in at a full cap is neither graded nor
    /// used up (<c>409 extra_cap_reached</c>) and can be handed in again once coins are left.
    /// </summary>
    [HttpPost("runs/{runId:int}/submit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExtraRunResult>> Submit(int childId, int runId, SubmitExtraRunDto dto, CancellationToken ct)
    {
        if (!User.IsStudent()) return Forbid();
        var outcome = await extras.SubmitAsync(childId, runId, dto.Answers, ct);
        return outcome.Value is { } result ? result : this.ProblemWithCode(outcome.Error!.Value, outcome.Detail!);
    }
}
