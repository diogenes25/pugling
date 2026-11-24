using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// The grade level of one child per vocabulary subject, for its supervisor (B-742) - the same numbers the child reads
/// under <c>student/me/subject-levels</c>, one child per call and never side by side with a sibling.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/children/{childId:int}/subject-levels")]
[Tags("Supervisor – Subject Levels")]
[Produces("application/json")]
[Authorize(Roles = Roles.Supervisor)]
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class ChildSubjectLevelsController(SubjectLevelService levels, LearningDay learningDay) : ControllerBase
{
    /// <summary>
    /// The child's grade levels with best level and the words to the next level, one per vocabulary subject of its
    /// active plans (sorted by subject name). Reading evaluates the day if nobody did yet. 404 for a child you do not
    /// supervise.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<SubjectLevelResponse>>> List(int childId, CancellationToken ct) =>
        await levels.ReadAsync(childId, learningDay.Today, ct);
}
