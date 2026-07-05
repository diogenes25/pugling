using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Services;

namespace Pugling.Api.Controllers.Learn;

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
[Route(ApiRoutes.V1 + "/learn/exercises/{id:int}/preview")]
[Tags("Learn – Exercise Preview")]
[Produces("application/json")]
[Authorize(Roles = Roles.Vater)]
public class ExercisePreviewController(PuglingDbContext db, ExercisePreviewService preview) : ControllerBase
{
    /// <summary>
    /// Liefert die spielbaren Aufgaben der Übung (ohne Lösung, wenn getippt wird), damit der Vater sie durchspielen kann.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExercisePreviewService.PreviewData>> Get(int id)
    {
        var exercise = await db.Exercises.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (exercise is null) return NotFound();

        var data = await preview.BuildAsync(exercise);
        if (data is null) return Problem(statusCode: 400, detail: "Die Übung enthält keine prüfbaren Inhalte.");
        return data;
    }

    /// <summary>Body des Testmodus-Checks: die abgegebenen Antworten.</summary>
    public record CheckDto(List<ExercisePreviewService.PreviewAnswer> Answers);

    /// <summary>
    /// Bewertet die Antworten wie im echten Test (server-autoritativ), aber ohne jede Persistenz oder Punktevergabe.
    /// </summary>
    [HttpPost("check")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExercisePreviewService.PreviewResult>> Check(int id, CheckDto dto)
    {
        var exercise = await db.Exercises.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (exercise is null) return NotFound();

        var result = await preview.CheckAsync(exercise, dto.Answers ?? []);
        if (result is null) return Problem(statusCode: 400, detail: "Die Übung enthält keine prüfbaren Inhalte.");
        return result;
    }
}
