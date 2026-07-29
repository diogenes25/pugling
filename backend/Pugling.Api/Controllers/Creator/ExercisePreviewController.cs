using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// Testmodus („Ausprobieren"): Der Vater/Lehrer spielt eine einzelne Katalog-Übung selbst durch, genau wie sie
/// das Kind im Abschlusstest erlebt – aber <b>nebenwirkungsfrei</b> (keine Punkte, kein Leitner-Fortschritt, kein
/// <c>TestAttempt</c>, keine Gamification, kein Lehrplan/Kind nötig). So kann er eine frisch erstellte oder
/// ausgewählte Übung verifizieren und sich mit ihr vertraut machen, ohne aufs Feedback des Kindes zu warten.
/// <para>
/// Kein Ownership-Filter: konsistent mit dem global lesbaren Katalog (<see cref="ExerciseCatalogController"/>) –
/// der Vater soll auch übernommene/fremde Übungen testen können. Beide Endpunkte sind seiteneffektfrei.
/// </para>
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/exercises/{id:int}/preview")]
[Tags("Creator – Exercise Preview")]
[Produces("application/json")]
[Authorize(Roles = Roles.Creator)]
public class ExercisePreviewController(PuglingDbContext db, ExercisePreviewService preview, ExerciseTypeRegistry types) : ControllerBase
{
    /// <summary>
    /// Warum es nichts zu spielen gibt – und das ist der Unterschied, der dem Autor die Arbeit spart:
    /// Bei einem Aufsatz ist „keine prüfbaren Aufgaben" die <b>Eigenschaft des Typs</b>, bei einer
    /// Vokabelübung ohne Items ein <b>unfertiger Datenstand</b>. Vorher trugen beide Fälle dieselbe Meldung,
    /// und ein leeres „Ausprobieren" sah aus wie ein Fehler der App statt wie eine Übung ohne Wörter.
    /// </summary>
    private async Task<ObjectResult> NoContentProblemAsync(Exercise exercise, CancellationToken ct)
    {
        var unfilled = types.ByKey(exercise.Type)?.StoreResolution == StoreResolution.ItemTable
            && !await db.ExerciseItems.AnyAsync(i => i.ExerciseId == exercise.Id, ct);
        return unfilled
            ? this.ProblemWithCode(ApiErrors.ExerciseEmpty, "This exercise has no items yet. Add its content first.")
            : this.ProblemWithCode(ApiErrors.NoCheckableContent, "The exercise contains no checkable content.");
    }

    /// <summary>
    /// Liefert die spielbaren Aufgaben der Übung (ohne Lösung, wenn getippt wird), damit der Vater sie durchspielen kann.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PreviewData>> Get(int id, CancellationToken ct, [FromQuery] int? stage = null)
    {
        var exercise = await db.Exercises.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
        if (exercise is null) return NotFound();

        // Optionaler stage-Parameter: der Vater probiert eine bestimmte Abfrageform durch (sonst Übungs-Standard).
        var data = await preview.BuildAsync(exercise, stage);
        if (data is null) return await NoContentProblemAsync(exercise, ct);
        return data;
    }

    /// <summary>
    /// Bewertet die Antworten wie im echten Test (server-autoritativ), aber ohne jede Persistenz oder Punktevergabe.
    /// Die Stufe muss dieselbe sein wie beim Laden (<see cref="Get"/>), damit „getippt" nicht auseinanderdriftet.
    /// </summary>
    [HttpPost("check")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PreviewResult>> Check(int id, PreviewCheckDto dto, CancellationToken ct)
    {
        var exercise = await db.Exercises.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
        if (exercise is null) return NotFound();

        var result = await preview.CheckAsync(exercise, dto.Answers ?? [], dto.Stage);
        if (result is null) return await NoContentProblemAsync(exercise, ct);
        return result;
    }
}
