using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;
using Pugling.Api.Errors;
using Pugling.Api.Services;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// Lernziele eines Kindes: vom Vater gesetzte Ergebnis-/Beherrschungsziele auf einem Katalog-Scope
/// (Fach/Kapitel/Übung), live gegen den aggregierten Lernstand ausgewertet (Status offen/erreicht/überfällig).
/// Abgrenzung: das plan-gebundene Pflicht-Ziel der Position (Tag/Woche) und aktivitätsbasierte Missionen sind
/// etwas anderes – siehe <see cref="ChildLearnProgressController"/> für die zugrundeliegende Auswertung.
/// Eigentum über <see cref="ChildOwnershipFilter"/>; Lesen darf Vater <b>und</b> Kind, Schreiben nur der Vater.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/children/{childId:int}/learn-goals")]
[Tags("Learn – Learning Goals")]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class LearnGoalsController(LearnGoalService goals) : ControllerBase
{
    /// <summary>
    /// Alle Lernziele des Kindes, live ausgewertet. Filter: <paramref name="subjectId"/> (nur ein Fach),
    /// <paramref name="status"/> (<c>open</c>/<c>achieved</c>/<c>overdue</c>). Gesamtzahl im Header <c>X-Total-Count</c>.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<LearnGoalService.LearnGoalResponse>>> List(
        int childId, [FromQuery] int? subjectId = null, [FromQuery] string? status = null,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake, CancellationToken ct = default) =>
        (await goals.ListAsync(childId, subjectId, status, ct)).ToPagedList(Response, skip, take);

    /// <summary>Ein einzelnes Lernziel, live ausgewertet (404, wenn es zu diesem Kind nicht existiert).</summary>
    [HttpGet("{goalId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LearnGoalService.LearnGoalResponse>> Get(int childId, int goalId, CancellationToken ct = default) =>
        await goals.GetAsync(childId, goalId, ct) is { } g ? g : NotFound();

    /// <summary>Legt ein Lernziel an (nur Vater). 400 bei ungültigem Scope/Zielwert.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Vater)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LearnGoalService.LearnGoalResponse>> Create(
        int childId, [FromBody] LearnGoalService.CreateLearnGoalRequest request, CancellationToken ct = default)
    {
        var (value, error) = await goals.CreateAsync(childId, request, ct);
        if (error is not null) return this.ProblemWithCode(error.Value);
        return CreatedAtAction(nameof(Get), new { childId, goalId = value!.Id }, value);
    }

    /// <summary>Ändert Metrik/Zielwert/Stichtag/Titel eines Ziels (nur Vater); der Scope bleibt fix.</summary>
    [HttpPatch("{goalId:int}")]
    [Authorize(Roles = Roles.Vater)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LearnGoalService.LearnGoalResponse>> Update(
        int childId, int goalId, [FromBody] LearnGoalService.UpdateLearnGoalRequest request, CancellationToken ct = default)
    {
        var (value, error) = await goals.UpdateAsync(childId, goalId, request, ct);
        if (error is not null) return this.ProblemWithCode(error.Value);
        return value is not null ? value : NotFound();
    }

    /// <summary>Löscht ein Lernziel (nur Vater).</summary>
    [HttpDelete("{goalId:int}")]
    [Authorize(Roles = Roles.Vater)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int childId, int goalId, CancellationToken ct = default) =>
        await goals.DeleteAsync(childId, goalId, ct) ? NoContent() : NotFound();
}
