using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// Cross-child daily overview for the father ("who accomplished what today/yesterday?"). Aggregates
/// the daily status of all own children; the computation lives in <see cref="ChildrenDashboardService"/>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/children/daily-overview")]
[Tags("Supervisor – Children")]
[Produces("application/json")]
[Authorize(Roles = Roles.Supervisor)]
public class ChildrenDashboardController(ChildrenDashboardService dashboard, LearningDay learningDay) : ControllerBase
{
    /// <summary>Daily status of all own children; <paramref name="date"/> optional (default: today's learning day in the family's zone, <c>LearningDay:TimeZone</c>).</summary>
    [HttpGet]
    public async Task<ActionResult<Dashboard>> Get([FromQuery] DateOnly? date, CancellationToken ct)
    {
        var day = date ?? learningDay.Today;
        return await dashboard.BuildAsync(User.AdultId()!.Value, day, ct);
    }
}
