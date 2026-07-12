using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;
using Pugling.Api.Errors;
using Pugling.Api.Models;
using Pugling.Api.Services;
using Pugling.Api.Services.Supervisor;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// „Große Ziele" (Objectives, der kindgerechte OKR-Kern) eines Kindes: eine terminierte, motivierende Klammer
/// über mehreren messbaren Etappen (<see cref="KeyResult"/>s, eigener Controller darunter). Live gegen den
/// Lernstand + die Klassenarbeits-Noten ausgewertet (Status offen/erreicht/überfällig). Abgrenzung: das
/// plan-gebundene Pflicht-Ziel der Position (Tag/Woche, mit Malus) und aktivitätsbasierte Missionen sind etwas
/// anderes; ein Objective misst den Ergebnis-Fortschritt und wird ohne Malus, dafür mit Etappen-Häppchen belohnt.
/// Eigentum über <see cref="ChildOwnershipFilter"/>; Lesen darf Vater <b>und</b> Kind, Schreiben nur der Vater.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/children/{childId:int}/objectives")]
[Tags("Supervisor – Objectives")]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class ObjectivesController(ObjectiveService objectives) : ControllerBase
{
    /// <summary>
    /// Alle großen Ziele des Kindes, live ausgewertet. Filter: <paramref name="status"/>
    /// (<c>open</c>/<c>achieved</c>/<c>overdue</c>), <paramref name="kind"/> (<c>Committed</c>/<c>Stretch</c>).
    /// Gesamtzahl im Header <c>X-Total-Count</c>.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ObjectiveService.ObjectiveResponse>>> List(
        int childId, [FromQuery] string? status = null, [FromQuery] ObjectiveKind? kind = null,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake, CancellationToken ct = default) =>
        (await objectives.ListAsync(childId, status, kind, ct)).ToPagedList(Response, skip, take);

    /// <summary>Ein einzelnes großes Ziel, live ausgewertet (404, wenn es zu diesem Kind nicht existiert).</summary>
    [HttpGet("{objectiveId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ObjectiveService.ObjectiveResponse>> Get(int childId, int objectiveId, CancellationToken ct = default) =>
        await objectives.GetAsync(childId, objectiveId, ct) is { } o ? o : NotFound();

    /// <summary>Legt ein großes Ziel an (nur Vater); Etappen können inline mitgegeben werden. 400 bei ungültigem Scope/Zielwert.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Supervisor)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ObjectiveService.ObjectiveResponse>> Create(
        int childId, [FromBody] ObjectiveService.CreateObjectiveRequest request, CancellationToken ct = default)
    {
        var (value, error) = await objectives.CreateAsync(childId, request, ct);
        if (error is not null) return this.ProblemWithCode(error.Value);
        return CreatedAtAction(nameof(Get), new { childId, objectiveId = value!.Id }, value);
    }

    /// <summary>Ändert Kopf-Felder eines Ziels (Titel/Motivation/Art/Zeitraum/Belohnung/aktiv); nur Vater.</summary>
    [HttpPatch("{objectiveId:int}")]
    [Authorize(Roles = Roles.Supervisor)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ObjectiveService.ObjectiveResponse>> Update(
        int childId, int objectiveId, [FromBody] ObjectiveService.UpdateObjectiveRequest request, CancellationToken ct = default)
    {
        var (value, error) = await objectives.UpdateAsync(childId, objectiveId, request, ct);
        if (error is not null) return this.ProblemWithCode(error.Value);
        return value is not null ? value : NotFound();
    }

    /// <summary>Löscht ein großes Ziel samt Etappen (nur Vater).</summary>
    [HttpDelete("{objectiveId:int}")]
    [Authorize(Roles = Roles.Supervisor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int childId, int objectiveId, CancellationToken ct = default) =>
        await objectives.DeleteAsync(childId, objectiveId, ct) ? NoContent() : NotFound();
}
