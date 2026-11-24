using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;
using Pugling.Api.Errors;

namespace Pugling.Api.Controllers.Student;

/// <summary>
/// The child's own homework (B-760): read it and hand in. Only the child's token has a homework list here; a supervisor reads
/// it under <c>supervisor/children/{childId}/homework</c>. A hand-in by the end of the due day averts the penalty, the
/// supervisor's acceptance brings the coins.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Student + "/homework")]
[Tags("Student – Homework")]
[Produces("application/json")]
[Authorize]
public class MyHomeworkController(HomeworkService homework) : ControllerBase
{
    /// <summary>The own homework with its hand-ins, latest due day first; <c>take</c> bounds the list (no default).</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<HomeworkResponse>>> List([FromQuery] int? take = null, CancellationToken ct = default)
    {
        if (!User.IsStudent() || User.ChildId() is not { } childId)
            return this.ProblemWithCode(ApiErrors.Forbidden, "Only the child reads its homework here.");
        if (take is < 1) return this.ProblemWithCode(ApiErrors.ValidationError, "take must be at least 1.");
        return Ok(await homework.ListAsync(childId, take, ct));
    }

    /// <summary>
    /// Hands the homework in. Another hand-in after a rework note is the way back; after acceptance there is nothing to hand
    /// in. Somebody else's homework answers like an unknown one.
    /// </summary>
    [HttpPost("{homeworkId:int}/submissions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HomeworkResponse>> Submit(int homeworkId, CancellationToken ct)
    {
        if (!User.IsStudent() || User.ChildId() is not { } childId)
            return this.ProblemWithCode(ApiErrors.Forbidden, "Only the child hands in its homework.");
        var outcome = await homework.SubmitAsync(childId, homeworkId, ct);
        return outcome.Error is { } error ? this.ProblemWithCode(error, outcome.Detail) : outcome.Value!;
    }
}
