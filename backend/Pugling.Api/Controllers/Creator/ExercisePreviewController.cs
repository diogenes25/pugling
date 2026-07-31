using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// Test mode ("try it out"): the adult/teacher plays through a single catalog exercise themselves, exactly as
/// the child experiences it in the final test – but <b>side-effect-free</b> (no points, no Leitner progress, no
/// <c>TestAttempt</c>, no gamification, no study plan/child required). This lets them verify a freshly created or
/// selected exercise and get familiar with it, without waiting for the child's feedback.
/// <para>
/// No ownership filter: consistent with the globally readable catalog (<see cref="ExerciseCatalogController"/>) –
/// the adult should be able to test adopted/foreign exercises too. Both endpoints are free of side effects.
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
    /// Why there is nothing to play – and this is the distinction that saves the author work:
    /// for an essay, "no checkable tasks" is a <b>property of the type</b>, for a vocabulary exercise
    /// without items it is an <b>unfinished data state</b>. Both cases used to carry the same message,
    /// and an empty "try it out" looked like a bug in the app instead of an exercise without words.
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
    /// Returns the playable tasks of the exercise (without the solution, when typed), so the adult can play it through.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PreviewData>> Get(int id, CancellationToken ct, [FromQuery] int? stage = null)
    {
        var exercise = await db.Exercises.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
        if (exercise is null) return NotFound();

        // Optionaler stage-Parameter: der Vater probiert eine bestimmte Abfrageform durch (sonst Übungs-Standard).
        var data = await preview.BuildAsync(exercise, stage, ct);
        if (data is null) return await NoContentProblemAsync(exercise, ct);
        return data;
    }

    /// <summary>
    /// Evaluates the answers like in the real test (server-authoritative), but without any persistence or scoring.
    /// The stage must be the same as when loading (<see cref="Get"/>), so that "typed" does not drift apart.
    /// </summary>
    [HttpPost("check")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PreviewResult>> Check(int id, PreviewCheckDto dto, CancellationToken ct)
    {
        var exercise = await db.Exercises.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
        if (exercise is null) return NotFound();

        var result = await preview.CheckAsync(exercise, dto.Answers ?? [], dto.Stage, ct);
        if (result is null) return await NoContentProblemAsync(exercise, ct);
        return result;
    }
}
