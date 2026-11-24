using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;

namespace Pugling.Api.Controllers.Student;

/// <summary>
/// The child's own grade level per vocabulary subject (B-742). Reading is the lazy daily evaluation: the first read of a
/// learning day re-evaluates the level, every further read that day only reads (<see cref="SubjectLevelService"/>).
/// The supervisor reads the same numbers under <c>supervisor/children/{childId}/subject-levels</c>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Student + "/me/subject-levels")]
[Tags("Student – Subject Levels")]
[Produces("application/json")]
[Authorize(Roles = Roles.Student)]
public class MySubjectLevelsController(SubjectLevelService levels, LearningDay learningDay) : ControllerBase
{
    /// <summary>
    /// Own grade levels, one per vocabulary subject of the active plans (sorted by subject name). A subject without
    /// vocabulary in an active plan has no measure and is missing.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<SubjectLevelResponse>>> List(CancellationToken ct)
    {
        if (User.ChildId() is not { } childId) return Forbid();
        return await levels.ReadAsync(childId, learningDay.Today, ct);
    }
}
