using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// What children reported about an exercise, for the people who can fix it (B-628, B-623 decision 7): counted reasons per
/// card and in total - no child and no text, because a teacher account must not read what other people's children wrote.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/exercises/{exerciseId:int}/feedback")]
[Tags("Creator – Exercise Feedback")]
[Produces("application/json")]
[Authorize(Roles = Roles.Creator)]
public class ExerciseFeedbackSummaryController(PuglingDbContext db, ExercisePermissionService perms,
    ExerciseFeedbackService feedback) : ControllerBase
{
    /// <summary>
    /// The counted reasons of the exercise. Only for a creator who may change it (write permission): the counts are a
    /// repair list, and repairing is what write permission is for.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseFeedbackSummary>> Get(int exerciseId, CancellationToken ct)
    {
        if (!await db.Exercises.AnyAsync(e => e.Id == exerciseId, ct))
            return this.ProblemWithCode(ApiErrors.NotFound, "Exercise not found.");
        if (!await perms.CanWriteAsync(User, exerciseId, ct))
            return this.ProblemWithCode(ApiErrors.NotOwner, "Only a creator who may change this exercise sees its feedback.");
        return await feedback.SummaryAsync(exerciseId, ct);
    }
}
