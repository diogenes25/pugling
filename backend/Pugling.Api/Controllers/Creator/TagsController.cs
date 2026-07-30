using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// Tags per child for marking catalog exercises. Both adult AND student may tag
/// (e.g. "relevant for the next class test"); ownership runs through the child.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/tags")]
[Tags("Creator – Tags")]
[Produces("application/json")]
[Authorize]
public class TagsController(PuglingDbContext db, AuthAccess access) : ControllerBase
{
    // Attribution „wer hat getaggt": Student → Sohn, jeder Erwachsene (Creator und/oder Supervisor) → Vater.
    // Nicht auf IsSupervisor allein prüfen – ein reiner Creator (künftige Lehrer-Konten) ist ebenfalls Erwachsener.
    private TaggedBy CurrentRole() => User.IsStudent() ? TaggedBy.Sohn : TaggedBy.Vater;

    private static TagResponse Map(Tag t) =>
        new(t.Id, t.ChildId, t.Name, t.Color, t.CreatedBy, t.ExerciseTags.Count, t.VocabularyTags.Count, t.CreatedAt);

    /// <summary>
    /// Projects tags directly into the response. The two counters are computed as <c>COUNT</c> in the
    /// database – the list endpoints previously loaded *all* link rows via two <c>Include</c>s,
    /// only to count them in memory (and got them back tracked on top of that).
    /// </summary>
    private static IQueryable<TagResponse> Project(IQueryable<Tag> q) =>
        q.Select(t => new TagResponse(t.Id, t.ChildId, t.Name, t.Color, t.CreatedBy,
            t.ExerciseTags.Count, t.VocabularyTags.Count, t.CreatedAt));

    /// <summary>Loads a tag along with its links (exercises + vocabulary), provided the user may access the associated child.</summary>
    private async Task<Tag?> FindOwnedAsync(int tagId, CancellationToken ct)
    {
        var tag = await db.Tags.Include(t => t.ExerciseTags).Include(t => t.VocabularyTags)
            .FirstOrDefaultAsync(t => t.Id == tagId, ct);
        if (tag is null) return null;
        return await access.OwnsChildAsync(User, tag.ChildId, ct) ? tag : null;
    }

    /// <summary>All tags of a child (student: own only, adult: own children only).</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<TagResponse>>> List([FromQuery] int childId, CancellationToken ct = default)
    {
        if (!await access.OwnsChildAsync(User, childId, ct)) return Forbid();
        return await Project(db.Tags.Where(t => t.ChildId == childId).OrderBy(t => t.Name)).ToListAsync(ct);
    }

    /// <summary>Creates a tag for a child (name unique per child).</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TagResponse>> Create(CreateTagDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return this.ProblemWithCode(ApiErrors.ValidationError, "Name is required.");
        if (!await access.OwnsChildAsync(User, dto.ChildId, ct)) return Forbid();

        var name = dto.Name.Trim();
        if (await db.Tags.AnyAsync(t => t.ChildId == dto.ChildId && t.Name == name, ct))
            return this.ProblemWithCode(ApiErrors.DuplicateTagName, "A tag with this name already exists for this child.");

        var tag = new Tag
        {
            ChildId = dto.ChildId,
            Name = name,
            Color = dto.Color?.Trim(),
            CreatedBy = CurrentRole(),
        };
        db.Tags.Add(tag);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetExercises), new { tagId = tag.Id }, Map(tag));
    }

    /// <summary>Renames a tag or changes its color.</summary>
    [HttpPatch("{tagId:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TagResponse>> Update(int tagId, UpdateTagDto dto, CancellationToken ct = default)
    {
        var tag = await FindOwnedAsync(tagId, ct);
        if (tag is null) return NotFound();

        if (dto.Name is not null)
        {
            var name = dto.Name.Trim();
            if (name.Length == 0) return this.ProblemWithCode(ApiErrors.ValidationError, "Name must not be empty.");
            if (name != tag.Name && await db.Tags.AnyAsync(t => t.ChildId == tag.ChildId && t.Name == name, ct))
                return this.ProblemWithCode(ApiErrors.DuplicateTagName, "A tag with this name already exists for this child.");
            tag.Name = name;
        }
        if (dto.Color is not null) tag.Color = dto.Color.Trim() is { Length: > 0 } c ? c : null;

        await db.SaveChangesAsync(ct);
        return Map(tag);
    }

    /// <summary>Deletes a tag (automatically removes all markings and class test links).</summary>
    [HttpDelete("{tagId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int tagId, CancellationToken ct = default)
    {
        var tag = await FindOwnedAsync(tagId, ct);
        if (tag is null) return NotFound();
        db.Tags.Remove(tag);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Marks one or more catalog exercises with this tag (already marked ones are skipped).</summary>
    [HttpPost("{tagId:int}/exercises")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TagResponse>> TagExercises(int tagId, TagExercisesDto dto, CancellationToken ct = default)
    {
        var tag = await FindOwnedAsync(tagId, ct);
        if (tag is null) return NotFound();
        if (dto.ExerciseIds is not { Count: > 0 }) return this.ProblemWithCode(ApiErrors.ValidationError, "At least one exercise is required.");

        var ids = dto.ExerciseIds.Distinct().ToList();
        var existing = await db.Exercises.Where(e => ids.Contains(e.Id)).Select(e => e.Id).ToListAsync(ct);
        var missing = ids.Except(existing).ToList();
        if (missing.Count > 0) return this.ProblemWithCode(ApiErrors.InvalidReference, $"Unknown exercise IDs: {string.Join(", ", missing)}");

        var already = tag.ExerciseTags.Select(x => x.ExerciseId).ToHashSet();
        foreach (var id in ids.Where(id => !already.Contains(id)))
            tag.ExerciseTags.Add(new ExerciseTag { ExerciseId = id });

        await db.SaveChangesAsync(ct);
        return Map(tag);
    }

    /// <summary>Removes the marking of an exercise with this tag.</summary>
    [HttpDelete("{tagId:int}/exercises/{exerciseId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UntagExercise(int tagId, int exerciseId, CancellationToken ct = default)
    {
        var tag = await FindOwnedAsync(tagId, ct);
        if (tag is null) return NotFound();
        var link = tag.ExerciseTags.FirstOrDefault(x => x.ExerciseId == exerciseId);
        if (link is null) return NotFound();
        db.ExerciseTags.Remove(link);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>All exercises marked with this tag.</summary>
    /// <param name="tagId">Tag whose exercises are read.</param>
    /// <param name="skip">Number of entries to skip (paging).</param>
    /// <param name="take">Maximum number of hits (1..500). Total count in the <c>X-Total-Count</c> header.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("{tagId:int}/exercises")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ExerciseBrief>>> GetExercises(
        int tagId, [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake,
        CancellationToken ct = default)
    {
        var tag = await FindOwnedAsync(tagId, ct);
        if (tag is null) return NotFound();

        // Von `Exercises` aus filtern, **nicht** von `ExerciseTags` über `.Select(x => x.Exercise!)`:
        // EF Core lässt `Include` nur an einer Entity-Wurzel zu, nach einer Projektion wirft es zur Laufzeit.
        // Die Route lieferte darum bei jedem Aufruf 500 – aufgefallen erst, als C3 sie zum ersten Mal aufrief.
        var exercises = await db.Exercises
            .Where(e => db.ExerciseTags.Any(x => x.TagId == tagId && x.ExerciseId == e.Id))
            .Include(e => e.Chapter!).ThenInclude(c => c.Subject)
            .OrderBy(e => e.Chapter!.SubjectId).ThenBy(e => e.ChapterId).ThenBy(e => e.OrderIndex).ThenBy(e => e.Id)
            .AsNoTracking()
            .ToPagedListAsync(Response, skip, take, ct);
        return exercises.Select(ExerciseBriefMapping.From).ToList();
    }

    /// <summary>The tags with which a specific exercise is marked in the context of a child.</summary>
    [HttpGet("for-exercise/{exerciseId:int}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<TagResponse>>> ForExercise(
        int exerciseId, [FromQuery] int childId, CancellationToken ct = default)
    {
        if (!await access.OwnsChildAsync(User, childId, ct)) return Forbid();
        return await Project(db.Tags
            .Where(t => t.ChildId == childId && t.ExerciseTags.Any(x => x.ExerciseId == exerciseId))
            .OrderBy(t => t.Name)).ToListAsync(ct);
    }

    // ---- Vokabeln taggen (kind-skopiert) -----------------------------------------------------------

    /// <summary>Marks one or more store vocabulary entries with this tag (already marked ones are skipped).</summary>
    [HttpPost("{tagId:int}/vocabulary")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TagResponse>> TagVocabulary(int tagId, TagVocabularyDto dto, CancellationToken ct = default)
    {
        var tag = await FindOwnedAsync(tagId, ct);
        if (tag is null) return NotFound();
        if (dto.VocabularyIds is not { Count: > 0 }) return this.ProblemWithCode(ApiErrors.ValidationError, "At least one vocabulary item is required.");

        var ids = dto.VocabularyIds.Distinct().ToList();
        var existing = await db.Vocabulary.Where(v => ids.Contains(v.Id)).Select(v => v.Id).ToListAsync(ct);
        var missing = ids.Except(existing).ToList();
        if (missing.Count > 0) return this.ProblemWithCode(ApiErrors.InvalidReference, $"Unknown vocabulary item IDs: {string.Join(", ", missing)}");

        var already = tag.VocabularyTags.Select(x => x.VocabularyId).ToHashSet();
        foreach (var id in ids.Where(id => !already.Contains(id)))
            tag.VocabularyTags.Add(new VocabularyTag { VocabularyId = id });

        await db.SaveChangesAsync(ct);
        return Map(tag);
    }

    /// <summary>Removes the marking of a vocabulary entry with this tag (the tag itself remains).</summary>
    [HttpDelete("{tagId:int}/vocabulary/{vocabularyId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UntagVocabulary(int tagId, int vocabularyId, CancellationToken ct = default)
    {
        var tag = await FindOwnedAsync(tagId, ct);
        if (tag is null) return NotFound();
        var link = tag.VocabularyTags.FirstOrDefault(x => x.VocabularyId == vocabularyId);
        if (link is null) return NotFound();
        db.VocabularyTags.Remove(link);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>All vocabulary entries marked with this tag (alphabetically by key).</summary>
    /// <param name="tagId">Tag whose vocabulary entries are read.</param>
    /// <param name="skip">Number of entries to skip (paging).</param>
    /// <param name="take">Maximum number of hits (1..500). Total count in the <c>X-Total-Count</c> header.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("{tagId:int}/vocabulary")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<TaggedVocabularyDto>>> GetVocabulary(
        int tagId, [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake,
        CancellationToken ct = default)
    {
        var tag = await FindOwnedAsync(tagId, ct);
        if (tag is null) return NotFound();

        return await db.VocabularyTags
            .Where(x => x.TagId == tagId)
            .Select(x => x.Vocabulary!)
            .OrderBy(v => v.Key).ThenBy(v => v.Id)
            .Select(v => new TaggedVocabularyDto(v.Id, v.Key, v.Word, v.Translation))
            .AsNoTracking()
            .ToPagedListAsync(Response, skip, take, ct);
    }

    /// <summary>The tags with which a specific vocabulary entry is marked in the context of a child.</summary>
    [HttpGet("for-vocabulary/{vocabularyId:int}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<TagResponse>>> ForVocabulary(
        int vocabularyId, [FromQuery] int childId, CancellationToken ct = default)
    {
        if (!await access.OwnsChildAsync(User, childId, ct)) return Forbid();
        return await Project(db.Tags
            .Where(t => t.ChildId == childId && t.VocabularyTags.Any(x => x.VocabularyId == vocabularyId))
            .OrderBy(t => t.Name)).ToListAsync(ct);
    }
}
