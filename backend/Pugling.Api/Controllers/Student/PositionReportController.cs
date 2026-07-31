using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;

namespace Pugling.Api.Controllers.Student;

/// <summary>
/// Learning report of a single study plan position (new model): shows the father for each content item of the
/// exercise how well it "sits" – Leitner box/mastery, introduction/due date and test hit rate. Read-only
/// view; the aggregation lives in the <see cref="PositionReportService"/>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Student + "/study-plans/{planId:int}/positions/{positionId:int}/report")]
[Tags("Student – Position Report")]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(PlanOwnershipFilter))]
public class PositionReportController(PositionReportService report) : ControllerBase
{
    /// <summary>Report of the position: per content item box/mastery, introduction/due date and test hit rate.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Report>> Get(int planId, int positionId, CancellationToken ct)
    {
        var result = await report.BuildAsync(planId, positionId, ct);
        return result is null ? NotFound() : result;
    }
}
