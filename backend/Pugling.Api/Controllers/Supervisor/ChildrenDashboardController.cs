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
public class ChildrenDashboardController(ChildrenDashboardService dashboard) : ControllerBase
{
    /// <summary>Daily status of all own children; <paramref name="date"/> optional (default: today, UTC).</summary>
    [HttpGet]
    public async Task<ActionResult<Dashboard>> Get([FromQuery] DateOnly? date, CancellationToken ct)
    {
        var day = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return await dashboard.BuildAsync(User.AdultId()!.Value, day, ct);
    }
}
