using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// Child-neutral tags for the shared vocabulary catalog (e.g. "chapter 5", "grade 7",
/// "irregular verbs"). They make vocabulary entries searchable and groupable – filtering by tags runs via
/// the store endpoint (<c>GET learn/vocabulary?tag=…</c>). Deliberately separate from the child-scoped
/// <see cref="Tag"/> (class test relevance), because the vocabulary store itself is child-neutral.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/vocabulary")]
[Tags("Creator – Vocabulary Store")]
[Produces("application/json")]
[Authorize(Roles = Roles.Creator)]
public class VocabularyTagsController(PuglingDbContext db) : ControllerBase
{
    /// <summary>All vocabulary tags (alphabetically), each with the count of linked vocabulary entries.</summary>
    [HttpGet("tags")]
    public async Task<IEnumerable<VocabTagResponse>> List(CancellationToken ct = default) =>
        await db.VocabTags.AsNoTracking().OrderBy(t => t.Name)
            .Select(t => new VocabTagResponse(t.Id, t.Name, t.Color, t.Links.Count, t.CreatedAt))
            .ToListAsync(ct);

    /// <summary>Creates a tag (name globally unique). If it already exists, the existing one is returned (idempotent).</summary>
    [HttpPost("tags")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VocabTagResponse>> Create(CreateVocabTagDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return this.ProblemWithCode(ApiErrors.ValidationError, "Name is required.");
        var name = dto.Name.Trim();

        var existing = await db.VocabTags.FirstOrDefaultAsync(t => t.Name == name, ct);
        if (existing is not null)
            return Ok(new VocabTagResponse(existing.Id, existing.Name, existing.Color, existing.Links.Count, existing.CreatedAt));

        var tag = new VocabTag { Name = name, Color = dto.Color?.Trim() is { Length: > 0 } c ? c : null };
        db.VocabTags.Add(tag);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new VocabTagResponse(tag.Id, tag.Name, tag.Color, 0, tag.CreatedAt));
    }

    /// <summary>Renames a tag or changes its color.</summary>
    [HttpPatch("tags/{id:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VocabTagResponse>> Update(int id, UpdateVocabTagDto dto, CancellationToken ct = default)
    {
        var tag = await db.VocabTags.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tag is null) return NotFound();

        if (dto.Name is not null)
        {
            var name = dto.Name.Trim();
            if (name.Length == 0) return this.ProblemWithCode(ApiErrors.ValidationError, "Name must not be empty.");
            if (name != tag.Name && await db.VocabTags.AnyAsync(t => t.Name == name, ct))
                return this.ProblemWithCode(ApiErrors.DuplicateTagName, "A tag with this name already exists.");
            tag.Name = name;
        }
        if (dto.Color is not null) tag.Color = dto.Color.Trim() is { Length: > 0 } c ? c : null;

        await db.SaveChangesAsync(ct);
        var count = await db.VocabTagLinks.CountAsync(l => l.VocabTagId == id, ct);
        return new VocabTagResponse(tag.Id, tag.Name, tag.Color, count, tag.CreatedAt);
    }

    /// <summary>Deletes a tag (automatically removes all vocabulary links).</summary>
    [HttpDelete("tags/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var tag = await db.VocabTags.FindAsync([id], ct);
        if (tag is null) return NotFound();
        db.VocabTags.Remove(tag);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Links a vocabulary entry with one or more tags (create-if-missing; already linked ones are skipped). Returns the current tags of the vocabulary entry.</summary>
    [HttpPost("{vocabularyId:int}/tags")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<VocabTagResponse>>> AttachTags(int vocabularyId, TagVocabDto dto, CancellationToken ct = default)
    {
        var vocab = await db.Vocabulary.Include(v => v.TagLinks).ThenInclude(l => l.VocabTag)
            .FirstOrDefaultAsync(v => v.Id == vocabularyId, ct);
        if (vocab is null) return NotFound();

        var names = (dto.Tags ?? []).Select(n => n.Trim()).Where(n => n.Length > 0).Distinct(StringComparer.Ordinal).ToList();
        if (names.Count == 0) return this.ProblemWithCode(ApiErrors.ValidationError, "At least one tag is required.");

        var existing = await db.VocabTags.Where(t => names.Contains(t.Name)).ToListAsync(ct);
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
        await db.SaveChangesAsync(ct);

        return vocab.TagLinks.Select(l => l.VocabTag!)
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .Select(t => new VocabTagResponse(t.Id, t.Name, t.Color, 0, t.CreatedAt)).ToList();
    }

    /// <summary>Removes the link of a vocabulary entry with a tag (the tag itself remains).</summary>
    [HttpDelete("{vocabularyId:int}/tags/{tagId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DetachTag(int vocabularyId, int tagId, CancellationToken ct = default)
    {
        var link = await db.VocabTagLinks.FirstOrDefaultAsync(l => l.VocabularyId == vocabularyId && l.VocabTagId == tagId, ct);
        if (link is null) return NotFound();
        db.VocabTagLinks.Remove(link);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
