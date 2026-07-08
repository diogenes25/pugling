using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;
using Pugling.Api.Services;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// Kindübergreifender Tagesüberblick für den Vater („wer hat heute/gestern was geschafft?"). Aggregiert
/// den Tagesstand aller eigenen Kinder; die Berechnung liegt im <see cref="ChildrenDashboardService"/>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/children/daily-overview")]
[Tags("Admin – Children")]
[Produces("application/json")]
[Authorize(Roles = Roles.Vater)]
public class ChildrenDashboardController(ChildrenDashboardService dashboard) : ControllerBase
{
    /// <summary>Tagesstand aller eigenen Kinder; <paramref name="date"/> optional (Standard: heute, UTC).</summary>
    [HttpGet]
    public async Task<ActionResult<ChildrenDashboardService.Dashboard>> Get([FromQuery] DateOnly? date, CancellationToken ct)
    {
        var day = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return await dashboard.BuildAsync(User.FatherId()!.Value, day, ct);
    }
}
