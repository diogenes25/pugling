using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// Learning report of a single study plan position: shows the father for each content item of the exercise
/// how well it "sits" – Leitner box/mastery, introduction/due date and test hit rate. Read-only view; the
/// aggregation lives in the <see cref="PositionReportService"/>.
/// <para>
/// Supervisor-only, and that is the anti-cheat assurance, not a formality: every row carries the item's
/// solution, also for cards the child has never been shown (<c>introduced: false</c>). The child reads its
/// own progress through <c>student/children/{childId}/…</c>, which never names an answer.
/// </para>
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/study-plans/{planId:int}/positions/{positionId:int}/report")]
[Tags("Supervisor – Position Report")]
[Produces("application/json")]
[Authorize(Roles = Roles.Supervisor)]
[ServiceFilter(typeof(PlanOwnershipFilter))]
public class PositionReportController(PositionReportService report) : ControllerBase
{
    /// <summary>Report of the position: per content item box/mastery, introduction/due date and test hit rate.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Report>> Get(int planId, int positionId, CancellationToken ct = default)
    {
        var result = await report.BuildAsync(planId, positionId, ct);
        return result is null ? NotFound() : result;
    }
}
