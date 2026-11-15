using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;
using Pugling.Api.Errors;

namespace Pugling.Api.Controllers.Student;

/// <summary>
/// The bonus training of a child: extra exercises on its current topic that pay coins beyond the day's duty
/// (<see cref="ExtraTrainingService"/>). It opens once today's daily goals are met (B-535), pays only for checked hits,
/// is capped per day, and halves an exercise's worth with every further run of it on the same day (B-538) - a vocabulary
/// exercise also asks harder with every completed run.
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
    /// Starts a bonus run of the exercise - or resumes its open run, which keeps its form and cards unless the exercise
    /// changed since: then the open run is discarded and a fresh one starts, with a new <c>runId</c> (B-545). <c>409
    /// extra_training_locked</c> before today's daily goals are done, <c>409 extra_exercise_done</c> for a fixed-form
    /// (non-vocabulary) exercise worth nothing more today, <c>409 extra_cap_reached</c> for a fixed-form exercise - new or
    /// open - once today's coin cap is full, so it does not show its solutions for nothing.
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
    /// <c>409 extra_run_completed</c> on a second submit. A fixed-form run handed in at a full cap is neither graded nor
    /// completed (<c>409 extra_cap_reached</c>) and can be handed in again once coins are left. <c>409 extra_run_stale</c>
    /// when the exercise changed since the start: the run is discarded unpaid and the exercise is open again (B-545).
    /// <c>409 extra_training_locked</c> when today's daily goals are not met (any more).
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
