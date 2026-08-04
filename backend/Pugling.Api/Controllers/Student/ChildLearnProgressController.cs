using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;

namespace Pugling.Api.Controllers.Student;

/// <summary>
/// Child-centric drill-down view of vocabulary learning progress along the catalog hierarchy
/// (subject → series unit → exercise → item). Mirrors the global catalog path
/// <c>creator/textbook-series/{}/units/{}/vocabulary</c>, but returns at each level the
/// <b>aggregated learning progress of the child</b> instead of the exercise representation. Complements the
/// flat <see cref="ChildVocabularyProgressController"/> view (weakest words across the board) with the hierarchy.
/// Ownership via <see cref="ChildOwnershipFilter"/> (father = own child, child = themself). What's shown is the
/// relevant set (assigned ∪ with progress); the <c>active</c> flag distinguishes currently assigned from
/// merely historical (unlinked/deactivated) exercises – logic in the <see cref="ChildLearnProgressService"/>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Student + "/children/{childId:int}/learn")]
[Tags("Student – Child Progress")]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class ChildLearnProgressController(ChildLearnProgressService progress) : ControllerBase
{
    /// <summary>
    /// Relevant subjects of the child with aggregated vocabulary progress. <paramref name="search"/> filters by
    /// subject name, <paramref name="active"/> by (in)active subjects. Sort: <c>name</c> (default), <c>mastery</c>,
    /// <c>coverage</c>, <c>weak</c>, <c>activity</c> (short form <c>-name</c> = descending). Total count in the <c>X-Total-Count</c> header.
    /// </summary>
    [HttpGet("subjects")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<SubjectProgressResponse>>> Subjects(
        int childId, [FromQuery] string? search = null, [FromQuery] bool? active = null,
        [FromQuery] string? sort = null, [FromQuery] string? dir = null,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake, CancellationToken ct = default) =>
        (await progress.SubjectsAsync(childId, search, SortingExtensions.ParseSort(sort, dir), active, ct))
            .ToPagedList(Response, skip, take);

    /// <summary>A single relevant subject (404 if the child has nothing assigned in it and no progress exists).</summary>
    [HttpGet("subjects/{subjectId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectProgressResponse>> Subject(
        int childId, int subjectId, CancellationToken ct = default) =>
        await progress.SubjectAsync(childId, subjectId, ct) is { } s ? s : NotFound();

    /// <summary>
    /// Series units of a subject with progress (404 if the subject is not relevant). Filters as for subjects;
    /// sort additionally <c>order</c> (default, unit order). Total count in the <c>X-Total-Count</c> header.
    /// </summary>
    [HttpGet("subjects/{subjectId:int}/series-units")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<SeriesUnitProgressResponse>>> SeriesUnits(
        int childId, int subjectId, [FromQuery] string? search = null, [FromQuery] bool? active = null,
        [FromQuery] string? sort = null, [FromQuery] string? dir = null,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake, CancellationToken ct = default) =>
        await progress.SeriesUnitsAsync(childId, subjectId, search, SortingExtensions.ParseSort(sort, dir), active, ct) is { } list
            ? list.ToPagedList(Response, skip, take)
            : NotFound();

    /// <summary>
    /// Relevant vocabulary exercises of a series unit with progress per exercise (404 if the unit is not relevant).
    /// Filters as for series units; sort additionally <c>title</c>, <c>active</c> (default <c>order</c>).
    /// Total count in the <c>X-Total-Count</c> header.
    /// </summary>
    [HttpGet("subjects/{subjectId:int}/series-units/{seriesUnitId:int}/vocabulary")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ExerciseProgressResponse>>> Vocabulary(
        int childId, int subjectId, int seriesUnitId, [FromQuery] string? search = null, [FromQuery] bool? active = null,
        [FromQuery] string? sort = null, [FromQuery] string? dir = null,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake, CancellationToken ct = default) =>
        await progress.ExercisesAsync(childId, subjectId, seriesUnitId, search, SortingExtensions.ParseSort(sort, dir), active, ct) is { } list
            ? list.ToPagedList(Response, skip, take)
            : NotFound();

    /// <summary>
    /// Item learning progress of the child for a relevant vocabulary exercise, weakest first
    /// (404 if the exercise is neither assigned to the child under this subject/series unit nor carries progress).
    /// <paramref name="search"/> filters by word/translation; sort: <c>word</c>, <c>mastery</c>, <c>box</c>,
    /// <c>seen</c>, <c>activity</c>. Total count in the <c>X-Total-Count</c> header.
    /// </summary>
    [HttpGet("subjects/{subjectId:int}/series-units/{seriesUnitId:int}/vocabulary/{exerciseId:int}/items")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ItemProgressResponse>>> Items(
        int childId, int subjectId, int seriesUnitId, int exerciseId,
        [FromQuery] string? search = null, [FromQuery] string? sort = null, [FromQuery] string? dir = null,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake, CancellationToken ct = default)
    {
        if (!await progress.IsRelevantExerciseAsync(childId, subjectId, seriesUnitId, exerciseId, ct))
            return NotFound();

        return await progress.ItemsAsync(childId, exerciseId, search, SortingExtensions.ParseSort(sort, dir), Response, skip, take, ct);
    }
}
