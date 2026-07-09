using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;
using Pugling.Api.Services;

namespace Pugling.Api.Controllers.Student;

/// <summary>
/// Lern-Report einer einzelnen Lehrplan-Position (neues Modell): zeigt dem Vater je Inhalt der Übung, wie
/// gut er „sitzt" – Karteikasten-Box/Beherrschung, Einführung/Fälligkeit und Test-Trefferquote. Reine
/// Lese-Sicht; die Aggregation liegt im <see cref="PositionReportService"/>.
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
    /// <summary>Report der Position: je Inhalt Box/Beherrschung, Einführung/Fälligkeit und Test-Trefferquote.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PositionReportService.Report>> Get(int planId, int positionId, CancellationToken ct)
    {
        var result = await report.BuildAsync(planId, positionId, ct);
        return result is null ? NotFound() : result;
    }
}
