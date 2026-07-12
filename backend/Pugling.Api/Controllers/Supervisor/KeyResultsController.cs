using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;
using Pugling.Api.Errors;
using Pugling.Api.Services.Supervisor;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// Die messbaren Etappen (<see cref="Pugling.Api.Models.KeyResult"/>s) eines großen Ziels. Jede hängt an einem
/// Katalog-Scope (Fach/Kapitel/Übung) und einer tricksicheren Metrik; der Scope ist nach Anlage fix (zum Umhängen
/// neu anlegen). Live über den Lernstand bzw. die Klassenarbeits-Note ausgewertet. Eigentum über
/// <see cref="ChildOwnershipFilter"/> (Kette Kind → Objective → Etappe wird im Service geprüft); Schreiben nur der Vater.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/children/{childId:int}/objectives/{objectiveId:int}/key-results")]
[Tags("Supervisor – Objectives")]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class KeyResultsController(ObjectiveService objectives) : ControllerBase
{
    /// <summary>Fügt dem Ziel eine Etappe hinzu (nur Vater). 400 bei ungültigem Scope/Zielwert, 404 wenn das Ziel fehlt.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Supervisor)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ObjectiveService.KeyResultResponse>> Create(
        int childId, int objectiveId, [FromBody] ObjectiveService.CreateKeyResultRequest request, CancellationToken ct = default)
    {
        var (value, error) = await objectives.AddKeyResultAsync(childId, objectiveId, request, ct);
        if (error is not null) return this.ProblemWithCode(error.Value);
        return value is not null
            ? CreatedAtAction(nameof(ObjectivesController.Get), "Objectives", new { childId, objectiveId }, value)
            : NotFound();
    }

    /// <summary>Ändert Metrik/Zielwert/Titel einer Etappe (nur Vater); der Scope bleibt fix.</summary>
    [HttpPatch("{keyResultId:int}")]
    [Authorize(Roles = Roles.Supervisor)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ObjectiveService.KeyResultResponse>> Update(
        int childId, int objectiveId, int keyResultId, [FromBody] ObjectiveService.UpdateKeyResultRequest request, CancellationToken ct = default)
    {
        var (value, error) = await objectives.UpdateKeyResultAsync(childId, objectiveId, keyResultId, request, ct);
        if (error is not null) return this.ProblemWithCode(error.Value);
        return value is not null ? value : NotFound();
    }

    /// <summary>Löscht eine Etappe des Ziels (nur Vater).</summary>
    [HttpDelete("{keyResultId:int}")]
    [Authorize(Roles = Roles.Supervisor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int childId, int objectiveId, int keyResultId, CancellationToken ct = default) =>
        await objectives.DeleteKeyResultAsync(childId, objectiveId, keyResultId, ct) ? NoContent() : NotFound();
}
