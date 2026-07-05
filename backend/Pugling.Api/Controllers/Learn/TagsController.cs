using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Learn;

/// <summary>
/// Schlagwörter (Tags) pro Kind zum Markieren von Katalog-Übungen. Vater UND Sohn dürfen taggen
/// (z. B. „relevant für die nächste Klassenarbeit"); die Zugehörigkeit läuft über das Kind.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.V1 + "/tags")]
[Tags("Learn – Tags")]
[Produces("application/json")]
[Authorize]
public class TagsController(PuglingDbContext db, AuthAccess access) : ControllerBase
{
    /// <summary>Tag in der Antwort inkl. Anzahl markierter Übungen und Vokabeln.</summary>
    public record TagResponse(int Id, int ChildId, string Name, string? Color, TaggedBy CreatedBy,
        int ExerciseCount, int VocabularyCount, DateTime CreatedAt);

    private TaggedBy CurrentRole() => User.IsFather() ? TaggedBy.Vater : TaggedBy.Sohn;

    private static TagResponse Map(Tag t) =>
        new(t.Id, t.ChildId, t.Name, t.Color, t.CreatedBy, t.ExerciseTags.Count, t.VocabularyTags.Count, t.CreatedAt);

    /// <summary>Lädt einen Tag samt Links (Übungen + Vokabeln), sofern der Nutzer auf das zugehörige Kind zugreifen darf.</summary>
    private async Task<Tag?> FindOwnedAsync(int tagId)
    {
        var tag = await db.Tags.Include(t => t.ExerciseTags).Include(t => t.VocabularyTags)
            .FirstOrDefaultAsync(t => t.Id == tagId);
        if (tag is null) return null;
        return await access.OwnsChildAsync(User, tag.ChildId) ? tag : null;
    }

    /// <summary>Alle Tags eines Kindes (Sohn: nur eigene, Vater: nur eigene Kinder).</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<TagResponse>>> List([FromQuery] int childId)
    {
        if (!await access.OwnsChildAsync(User, childId)) return Forbid();
        var tags = await db.Tags.Include(t => t.ExerciseTags).Include(t => t.VocabularyTags)
            .Where(t => t.ChildId == childId)
            .OrderBy(t => t.Name)
            .ToListAsync();
        return tags.Select(Map).ToList();
    }

    public record CreateTagDto(int ChildId, string Name, string? Color);

    /// <summary>Legt einen Tag für ein Kind an (Name je Kind eindeutig).</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TagResponse>> Create(CreateTagDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return Problem(statusCode: 400, detail: "Name ist erforderlich.");
        if (!await access.OwnsChildAsync(User, dto.ChildId)) return Forbid();

        var name = dto.Name.Trim();
        if (await db.Tags.AnyAsync(t => t.ChildId == dto.ChildId && t.Name == name))
            return Problem(statusCode: 400, detail: "Ein Tag mit diesem Namen existiert für dieses Kind bereits.");

        var tag = new Tag
        {
            ChildId = dto.ChildId,
            Name = name,
            Color = dto.Color?.Trim(),
            CreatedBy = CurrentRole(),
        };
        db.Tags.Add(tag);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetExercises), new { tagId = tag.Id }, Map(tag));
    }

    public record UpdateTagDto(string? Name, string? Color);

    /// <summary>Benennt einen Tag um oder ändert seine Farbe.</summary>
    [HttpPatch("{tagId:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TagResponse>> Update(int tagId, UpdateTagDto dto)
    {
        var tag = await FindOwnedAsync(tagId);
        if (tag is null) return NotFound();

        if (dto.Name is not null)
        {
            var name = dto.Name.Trim();
            if (name.Length == 0) return Problem(statusCode: 400, detail: "Name darf nicht leer sein.");
            if (name != tag.Name && await db.Tags.AnyAsync(t => t.ChildId == tag.ChildId && t.Name == name))
                return Problem(statusCode: 400, detail: "Ein Tag mit diesem Namen existiert für dieses Kind bereits.");
            tag.Name = name;
        }
        if (dto.Color is not null) tag.Color = dto.Color.Trim() is { Length: > 0 } c ? c : null;

        await db.SaveChangesAsync();
        return Map(tag);
    }

    /// <summary>Löscht einen Tag (entfernt automatisch alle Markierungen und Klassenarbeits-Verknüpfungen).</summary>
    [HttpDelete("{tagId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int tagId)
    {
        var tag = await FindOwnedAsync(tagId);
        if (tag is null) return NotFound();
        db.Tags.Remove(tag);
        await db.SaveChangesAsync();
        return NoContent();
    }

    public record TagExercisesDto(List<int> ExerciseIds);

    /// <summary>Markiert eine oder mehrere Katalog-Übungen mit diesem Tag (bereits markierte werden übersprungen).</summary>
    [HttpPost("{tagId:int}/exercises")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TagResponse>> TagExercises(int tagId, TagExercisesDto dto)
    {
        var tag = await FindOwnedAsync(tagId);
        if (tag is null) return NotFound();
        if (dto.ExerciseIds is not { Count: > 0 }) return Problem(statusCode: 400, detail: "Mindestens eine Übung ist erforderlich.");

        var ids = dto.ExerciseIds.Distinct().ToList();
        var existing = await db.Exercises.Where(e => ids.Contains(e.Id)).Select(e => e.Id).ToListAsync();
        var missing = ids.Except(existing).ToList();
        if (missing.Count > 0) return Problem(statusCode: 400, detail: $"Unbekannte Übungs-Ids: {string.Join(", ", missing)}");

        var already = tag.ExerciseTags.Select(x => x.ExerciseId).ToHashSet();
        foreach (var id in ids.Where(id => !already.Contains(id)))
            tag.ExerciseTags.Add(new ExerciseTag { ExerciseId = id, TaggedByRole = CurrentRole() });

        await db.SaveChangesAsync();
        return Map(tag);
    }

    /// <summary>Entfernt die Markierung einer Übung mit diesem Tag.</summary>
    [HttpDelete("{tagId:int}/exercises/{exerciseId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UntagExercise(int tagId, int exerciseId)
    {
        var tag = await FindOwnedAsync(tagId);
        if (tag is null) return NotFound();
        var link = tag.ExerciseTags.FirstOrDefault(x => x.ExerciseId == exerciseId);
        if (link is null) return NotFound();
        db.ExerciseTags.Remove(link);
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Alle Übungen, die mit diesem Tag markiert sind.</summary>
    [HttpGet("{tagId:int}/exercises")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ExerciseBrief>>> GetExercises(int tagId)
    {
        var tag = await FindOwnedAsync(tagId);
        if (tag is null) return NotFound();

        var exercises = await db.ExerciseTags
            .Where(x => x.TagId == tagId)
            .Select(x => x.Exercise!)
            .Include(e => e.Chapter!).ThenInclude(c => c.Subject)
            .OrderBy(e => e.Chapter!.SubjectId).ThenBy(e => e.ChapterId).ThenBy(e => e.OrderIndex)
            .AsNoTracking()
            .ToListAsync();
        return exercises.Select(ExerciseBrief.From).ToList();
    }

    /// <summary>Die Tags, mit denen eine bestimmte Übung im Kontext eines Kindes markiert ist.</summary>
    [HttpGet("for-exercise/{exerciseId:int}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<TagResponse>>> ForExercise(int exerciseId, [FromQuery] int childId)
    {
        if (!await access.OwnsChildAsync(User, childId)) return Forbid();
        var tags = await db.Tags.Include(t => t.ExerciseTags).Include(t => t.VocabularyTags)
            .Where(t => t.ChildId == childId && t.ExerciseTags.Any(x => x.ExerciseId == exerciseId))
            .OrderBy(t => t.Name)
            .ToListAsync();
        return tags.Select(Map).ToList();
    }

    // ---- Vokabeln taggen (kind-skopiert) -----------------------------------------------------------

    /// <summary>Schlanke Vokabel-Sicht für die Tag-Zuordnung (ohne die kindneutralen Store-Details).</summary>
    public record TaggedVocabularyDto(int Id, string Key, string Word, string Translation);

    public record TagVocabularyDto(List<int> VocabularyIds);

    /// <summary>Markiert eine oder mehrere Store-Vokabeln mit diesem Tag (bereits markierte werden übersprungen).</summary>
    [HttpPost("{tagId:int}/vocabulary")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TagResponse>> TagVocabulary(int tagId, TagVocabularyDto dto)
    {
        var tag = await FindOwnedAsync(tagId);
        if (tag is null) return NotFound();
        if (dto.VocabularyIds is not { Count: > 0 }) return Problem(statusCode: 400, detail: "Mindestens eine Vokabel ist erforderlich.");

        var ids = dto.VocabularyIds.Distinct().ToList();
        var existing = await db.Vocabulary.Where(v => ids.Contains(v.Id)).Select(v => v.Id).ToListAsync();
        var missing = ids.Except(existing).ToList();
        if (missing.Count > 0) return Problem(statusCode: 400, detail: $"Unbekannte Vokabel-Ids: {string.Join(", ", missing)}");

        var already = tag.VocabularyTags.Select(x => x.VocabularyId).ToHashSet();
        foreach (var id in ids.Where(id => !already.Contains(id)))
            tag.VocabularyTags.Add(new VocabularyTag { VocabularyId = id, TaggedByRole = CurrentRole() });

        await db.SaveChangesAsync();
        return Map(tag);
    }

    /// <summary>Entfernt die Markierung einer Vokabel mit diesem Tag (der Tag selbst bleibt bestehen).</summary>
    [HttpDelete("{tagId:int}/vocabulary/{vocabularyId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UntagVocabulary(int tagId, int vocabularyId)
    {
        var tag = await FindOwnedAsync(tagId);
        if (tag is null) return NotFound();
        var link = tag.VocabularyTags.FirstOrDefault(x => x.VocabularyId == vocabularyId);
        if (link is null) return NotFound();
        db.VocabularyTags.Remove(link);
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Alle Vokabeln, die mit diesem Tag markiert sind (alphabetisch nach Key).</summary>
    [HttpGet("{tagId:int}/vocabulary")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<TaggedVocabularyDto>>> GetVocabulary(int tagId)
    {
        var tag = await FindOwnedAsync(tagId);
        if (tag is null) return NotFound();

        return await db.VocabularyTags
            .Where(x => x.TagId == tagId)
            .Select(x => x.Vocabulary!)
            .OrderBy(v => v.Key)
            .Select(v => new TaggedVocabularyDto(v.Id, v.Key, v.Word, v.Translation))
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>Die Tags, mit denen eine bestimmte Vokabel im Kontext eines Kindes markiert ist.</summary>
    [HttpGet("for-vocabulary/{vocabularyId:int}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<TagResponse>>> ForVocabulary(int vocabularyId, [FromQuery] int childId)
    {
        if (!await access.OwnsChildAsync(User, childId)) return Forbid();
        var tags = await db.Tags.Include(t => t.ExerciseTags).Include(t => t.VocabularyTags)
            .Where(t => t.ChildId == childId && t.VocabularyTags.Any(x => x.VocabularyId == vocabularyId))
            .OrderBy(t => t.Name)
            .ToListAsync();
        return tags.Select(Map).ToList();
    }
}
