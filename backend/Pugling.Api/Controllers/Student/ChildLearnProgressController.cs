using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;
using Pugling.Api.Services;

namespace Pugling.Api.Controllers.Student;

/// <summary>
/// Kind-zentrische Drill-down-Sicht auf den Vokabel-Lernstand entlang der Katalog-Hierarchie
/// (Fach → Kapitel → Übung → Item). Spiegelt den globalen Katalogpfad <c>learn/subjects/{}/chapters/{}/vocabulary</c>,
/// liefert aber je Ebene den <b>aggregierten Lernstand des Kindes</b> statt der Übungsdarstellung. Ergänzt die
/// flache <see cref="ChildVocabularyProgressController"/>-Sicht (schwächste Wörter übergreifend) um die Hierarchie.
/// Eigentum über <see cref="ChildOwnershipFilter"/> (Vater = eigenes Kind, Sohn = er selbst). Angezeigt wird die
/// relevante Menge (zugewiesen ∪ mit Fortschritt); das Flag <c>active</c> unterscheidet aktuell zugewiesene von
/// nur noch historischen (abgehängten/deaktivierten) Übungen – Logik im <see cref="ChildLearnProgressService"/>.
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
    /// Relevante Fächer des Kindes mit aggregiertem Vokabel-Fortschritt. <paramref name="search"/> filtert nach
    /// Fachname, <paramref name="active"/> auf (in)aktive Fächer. Sortierung: <c>name</c> (Standard), <c>mastery</c>,
    /// <c>coverage</c>, <c>weak</c>, <c>activity</c> (Kurzform <c>-name</c> = absteigend). Gesamtzahl im Header <c>X-Total-Count</c>.
    /// </summary>
    [HttpGet("subjects")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ChildLearnProgressService.SubjectProgressResponse>>> Subjects(
        int childId, [FromQuery] string? search = null, [FromQuery] bool? active = null,
        [FromQuery] string? sort = null, [FromQuery] string? dir = null,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake, CancellationToken ct = default) =>
        (await progress.SubjectsAsync(childId, search, SortingExtensions.ParseSort(sort, dir), active, ct))
            .ToPagedList(Response, skip, take);

    /// <summary>Ein einzelnes relevantes Fach (404, wenn dem Kind darin nichts zugewiesen ist und kein Fortschritt existiert).</summary>
    [HttpGet("subjects/{subjectId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChildLearnProgressService.SubjectProgressResponse>> Subject(
        int childId, int subjectId, CancellationToken ct = default) =>
        await progress.SubjectAsync(childId, subjectId, ct) is { } s ? s : NotFound();

    /// <summary>
    /// Kapitel eines Fachs mit Fortschritt (404, wenn das Fach nicht relevant ist). Filter wie bei den Fächern;
    /// Sortierung zusätzlich <c>order</c> (Standard, Kapitelreihenfolge). Gesamtzahl im Header <c>X-Total-Count</c>.
    /// </summary>
    [HttpGet("subjects/{subjectId:int}/chapters")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ChildLearnProgressService.ChapterProgressResponse>>> Chapters(
        int childId, int subjectId, [FromQuery] string? search = null, [FromQuery] bool? active = null,
        [FromQuery] string? sort = null, [FromQuery] string? dir = null,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake, CancellationToken ct = default) =>
        await progress.ChaptersAsync(childId, subjectId, search, SortingExtensions.ParseSort(sort, dir), active, ct) is { } list
            ? list.ToPagedList(Response, skip, take)
            : NotFound();

    /// <summary>
    /// Relevante Vokabelübungen eines Kapitels mit Fortschritt je Übung (404, wenn das Kapitel nicht relevant ist).
    /// Filter wie bei Kapiteln; Sortierung zusätzlich <c>title</c>, <c>active</c> (Standard <c>order</c>).
    /// Gesamtzahl im Header <c>X-Total-Count</c>.
    /// </summary>
    [HttpGet("subjects/{subjectId:int}/chapters/{chapterId:int}/vocabulary")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ChildLearnProgressService.ExerciseProgressResponse>>> Vocabulary(
        int childId, int subjectId, int chapterId, [FromQuery] string? search = null, [FromQuery] bool? active = null,
        [FromQuery] string? sort = null, [FromQuery] string? dir = null,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake, CancellationToken ct = default) =>
        await progress.ExercisesAsync(childId, subjectId, chapterId, search, SortingExtensions.ParseSort(sort, dir), active, ct) is { } list
            ? list.ToPagedList(Response, skip, take)
            : NotFound();

    /// <summary>
    /// Item-Lernstand des Kindes für eine relevante Vokabelübung, schwächste zuerst
    /// (404, wenn die Übung dem Kind unter diesem Fach/Kapitel weder zugewiesen ist noch Fortschritt trägt).
    /// <paramref name="search"/> filtert nach Wort/Übersetzung; Sortierung: <c>word</c>, <c>mastery</c>, <c>box</c>,
    /// <c>seen</c>, <c>activity</c>. Gesamtzahl im Header <c>X-Total-Count</c>.
    /// </summary>
    [HttpGet("subjects/{subjectId:int}/chapters/{chapterId:int}/vocabulary/{exerciseId:int}/items")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ChildLearnProgressService.ItemProgressResponse>>> Items(
        int childId, int subjectId, int chapterId, int exerciseId,
        [FromQuery] string? search = null, [FromQuery] string? sort = null, [FromQuery] string? dir = null,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake, CancellationToken ct = default)
    {
        if (!await progress.IsRelevantExerciseAsync(childId, subjectId, chapterId, exerciseId, ct))
            return NotFound();

        return await progress.ItemsAsync(childId, exerciseId, search, SortingExtensions.ParseSort(sort, dir), Response, skip, take, ct);
    }
}
