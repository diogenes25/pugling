using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// Kindneutrale Schlagworte für den gemeinsamen Vokabel-Katalog (z. B. „Kapitel 5", „Klasse 7",
/// „unregelmäßige Verben"). Sie machen Vokabeln such- und gruppierbar – das Filtern nach Tags läuft über
/// den Store-Endpunkt (<c>GET learn/vocabulary?tag=…</c>). Bewusst getrennt vom kind-skopierten
/// <see cref="Tag"/> (Klassenarbeits-Relevanz), weil der Vokabel-Store selbst kindneutral ist.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/vocabulary")]
[Tags("Creator – Vocabulary Store")]
[Produces("application/json")]
[Authorize(Roles = Roles.Creator)]
public class VocabularyTagsController(PuglingDbContext db) : ControllerBase
{
    /// <summary>Tag inkl. Anzahl verknüpfter Vokabeln.</summary>
    public record VocabTagResponse(int Id, string Name, string? Color, int VocabCount, DateTime CreatedAt);

    /// <summary>Alle Vokabel-Tags (alphabetisch), jeweils mit Anzahl verknüpfter Vokabeln.</summary>
    [HttpGet("tags")]
    public async Task<IEnumerable<VocabTagResponse>> List() =>
        await db.VocabTags.AsNoTracking().OrderBy(t => t.Name)
            .Select(t => new VocabTagResponse(t.Id, t.Name, t.Color, t.Links.Count, t.CreatedAt))
            .ToListAsync();

    public record CreateVocabTagDto(string Name, string? Color);

    /// <summary>Legt einen Tag an (Name global eindeutig). Existiert er bereits, wird der bestehende zurückgegeben (idempotent).</summary>
    [HttpPost("tags")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VocabTagResponse>> Create(CreateVocabTagDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return this.ProblemWithCode(ApiErrors.ValidationError, "Name is required.");
        var name = dto.Name.Trim();

        var existing = await db.VocabTags.FirstOrDefaultAsync(t => t.Name == name);
        if (existing is not null)
            return Ok(new VocabTagResponse(existing.Id, existing.Name, existing.Color, existing.Links.Count, existing.CreatedAt));

        var tag = new VocabTag { Name = name, Color = dto.Color?.Trim() is { Length: > 0 } c ? c : null };
        db.VocabTags.Add(tag);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(List), new VocabTagResponse(tag.Id, tag.Name, tag.Color, 0, tag.CreatedAt));
    }

    public record UpdateVocabTagDto(string? Name, string? Color);

    /// <summary>Benennt einen Tag um oder ändert seine Farbe.</summary>
    [HttpPatch("tags/{id:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VocabTagResponse>> Update(int id, UpdateVocabTagDto dto)
    {
        var tag = await db.VocabTags.FirstOrDefaultAsync(t => t.Id == id);
        if (tag is null) return NotFound();

        if (dto.Name is not null)
        {
            var name = dto.Name.Trim();
            if (name.Length == 0) return this.ProblemWithCode(ApiErrors.ValidationError, "Name must not be empty.");
            if (name != tag.Name && await db.VocabTags.AnyAsync(t => t.Name == name))
                return this.ProblemWithCode(ApiErrors.DuplicateTagName, "A tag with this name already exists.");
            tag.Name = name;
        }
        if (dto.Color is not null) tag.Color = dto.Color.Trim() is { Length: > 0 } c ? c : null;

        await db.SaveChangesAsync();
        var count = await db.VocabTagLinks.CountAsync(l => l.VocabTagId == id);
        return new VocabTagResponse(tag.Id, tag.Name, tag.Color, count, tag.CreatedAt);
    }

    /// <summary>Löscht einen Tag (entfernt automatisch alle Vokabel-Verknüpfungen).</summary>
    [HttpDelete("tags/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var tag = await db.VocabTags.FindAsync(id);
        if (tag is null) return NotFound();
        db.VocabTags.Remove(tag);
        await db.SaveChangesAsync();
        return NoContent();
    }

    public record TagVocabDto(List<string> Tags);

    /// <summary>Verknüpft eine Vokabel mit einem oder mehreren Tags (create-if-missing; bereits verknüpfte werden übersprungen). Liefert die aktuellen Tags der Vokabel.</summary>
    [HttpPost("{vocabularyId:int}/tags")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<VocabTagResponse>>> AttachTags(int vocabularyId, TagVocabDto dto)
    {
        var vocab = await db.Vocabulary.Include(v => v.TagLinks).ThenInclude(l => l.VocabTag)
            .FirstOrDefaultAsync(v => v.Id == vocabularyId);
        if (vocab is null) return NotFound();

        var names = (dto.Tags ?? []).Select(n => n.Trim()).Where(n => n.Length > 0).Distinct(StringComparer.Ordinal).ToList();
        if (names.Count == 0) return this.ProblemWithCode(ApiErrors.ValidationError, "At least one tag is required.");

        var existing = await db.VocabTags.Where(t => names.Contains(t.Name)).ToListAsync();
        var byName = existing.ToDictionary(t => t.Name, StringComparer.Ordinal);
        var already = vocab.TagLinks.Where(l => l.VocabTag is not null).Select(l => l.VocabTag!.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var name in names.Where(n => !already.Contains(n)))
        {
            if (!byName.TryGetValue(name, out var tag))
            {
                tag = new VocabTag { Name = name };
                db.VocabTags.Add(tag);
                byName[name] = tag;
            }
            vocab.TagLinks.Add(new VocabTagLink { VocabTag = tag, Vocabulary = vocab });
        }
        await db.SaveChangesAsync();

        return vocab.TagLinks.Select(l => l.VocabTag!)
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .Select(t => new VocabTagResponse(t.Id, t.Name, t.Color, 0, t.CreatedAt)).ToList();
    }

    /// <summary>Löst die Verknüpfung einer Vokabel mit einem Tag (der Tag selbst bleibt bestehen).</summary>
    [HttpDelete("{vocabularyId:int}/tags/{tagId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DetachTag(int vocabularyId, int tagId)
    {
        var link = await db.VocabTagLinks.FirstOrDefaultAsync(l => l.VocabularyId == vocabularyId && l.VocabTagId == tagId);
        if (link is null) return NotFound();
        db.VocabTagLinks.Remove(link);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
