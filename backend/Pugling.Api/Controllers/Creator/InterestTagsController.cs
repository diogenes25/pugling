using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// The shared interest/style taxonomy – <b>one</b> controlled vocabulary for two consumers:
/// images carry the tags as a property (<c>creator/media/{id}/tags</c>), children as a weighted
/// preference or aversion (<c>supervisor/children/{id}/interests</c>). This exact dual use makes
/// individualized image selection computable; two separate vocabularies could only guess.
/// Maintained by the creator, but – like the vocabulary store – child-neutral and global.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/interest-tags")]
[Tags("Creator – Interest Tags")]
[Produces("application/json")]
[Authorize(Roles = Roles.Creator)]
public class InterestTagsController(PuglingDbContext db) : ControllerBase
{
    /// <summary>Projects along with usage counts from both sides – they show whether a tag is "dead".</summary>
    private static IQueryable<InterestTagResponse> Project(IQueryable<InterestTag> q) =>
        q.Select(t => new InterestTagResponse(t.Id, t.Slug, t.Label, t.Facet, t.Synonyms, t.Color,
            t.MediaLinks.Count, t.ChildInterests.Count, t.CreatedAt));

    /// <summary>
    /// All tags (alphabetically by slug), optionally filtered. The total count (before paging) is
    /// in the <c>X-Total-Count</c> header.
    /// </summary>
    /// <param name="search">Substring in slug or label.</param>
    /// <param name="facet">Only tags of this facet (e.g. only styles).</param>
    /// <param name="unused">true = only tags with no usage at all (cleanup view).</param>
    /// <param name="skip">Number of entries to skip (paging).</param>
    /// <param name="take">Maximum number of hits (1..500).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    public async Task<IEnumerable<InterestTagResponse>> List(
        [FromQuery] string? search = null,
        [FromQuery] InterestFacet? facet = null,
        [FromQuery] bool? unused = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = PagingExtensions.DefaultTake,
        CancellationToken ct = default)
    {
        var query = db.InterestTags.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Slug.Contains(search) || t.Label.Contains(search));
        if (facet is not null)
            query = query.Where(t => t.Facet == facet);
        if (unused is true)
            query = query.Where(t => t.MediaLinks.Count == 0 && t.ChildInterests.Count == 0);

        return await Project(query.OrderBy(t => t.Slug).ThenBy(t => t.Id)).ToPagedListAsync(Response, skip, take, ct);
    }

    /// <summary>A tag by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InterestTagResponse>> Get(int id, CancellationToken ct = default)
    {
        var tag = await Project(db.InterestTags.AsNoTracking().Where(t => t.Id == id)).FirstOrDefaultAsync(ct);
        return tag is null ? NotFound() : tag;
    }

    /// <summary>
    /// Creates a tag. If the slug is missing, it is derived from the label. If the slug already
    /// exists, the existing entry comes back (idempotent) – so an agent can safely repeat the same
    /// catalog build.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InterestTagResponse>> Create(CreateInterestTagDto dto, CancellationToken ct = default)
    {
        var (slug, problem) = this.DeriveRequiredSlug(dto.Label, "Label", dto.Slug);
        if (problem is not null) return problem;

        var existing = await Project(db.InterestTags.AsNoTracking().Where(t => t.Slug == slug)).FirstOrDefaultAsync(ct);
        if (existing is not null) return Ok(existing);

        var tag = new InterestTag
        {
            Slug = slug!,
            Label = dto.Label.Trim(),
            Facet = dto.Facet,
            Synonyms = Clean(dto.Synonyms),
            Color = dto.Color?.Trim() is { Length: > 0 } c ? c : null,
        };
        db.InterestTags.Add(tag);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = tag.Id },
            new InterestTagResponse(tag.Id, tag.Slug, tag.Label, tag.Facet, tag.Synonyms, tag.Color, 0, 0, tag.CreatedAt));
    }

    /// <summary>
    /// Changes label, facet, synonyms, or color. The <c>Slug</c> is deliberately <b>immutable</b> –
    /// it is the stable reference that images and child profiles hang off. It still decides: a new label
    /// whose slug another tag already carries is rejected, the same rule <c>Create</c> enforces
    /// (B-124) - otherwise two tags share a label in every picker.
    /// </summary>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InterestTagResponse>> Update(int id, UpdateInterestTagDto dto, CancellationToken ct = default)
    {
        var tag = await db.InterestTags.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tag is null) return NotFound();

        if (dto.Label is not null)
        {
            var label = dto.Label.Trim();
            if (label.Length == 0) return this.ProblemWithCode(ApiErrors.ValidationError, "Label must not be empty.");

            var (slug, slugProblem) = this.DeriveRequiredSlug(label, "Label");
            if (slugProblem is not null) return slugProblem;
            // Excluded by id, not by slug - see PublishersController.Update. As strong as Create's rule and
            // no stronger: Create accepts an explicit slug, so a label may legitimately differ from it, and
            // two labels that derive to different slugs stay allowed here too.
            if (await db.InterestTags.AnyAsync(t => t.Id != id && t.Slug == slug, ct))
                return this.ProblemWithCode(ApiErrors.DuplicateInterestTag,
                    "Another interest tag already uses the slug this label derives to.");

            tag.Label = label;
        }
        if (dto.Facet.HasValue) tag.Facet = dto.Facet.Value;
        // Assign a new list (no in-place mutation - the JSON column pitfall).
        if (dto.Synonyms is not null) tag.Synonyms = Clean(dto.Synonyms);
        if (dto.Color is not null) tag.Color = dto.Color.Trim() is { Length: > 0 } c ? c : null;

        await db.SaveChangesAsync(ct);
        return await Project(db.InterestTags.AsNoTracking().Where(t => t.Id == id)).FirstAsync(ct);
    }

    /// <summary>
    /// Deletes a tag along with its links to images and children (cascade). Deliberately without a
    /// usage lock: a tag carries no content, its loss only costs selection quality.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var tag = await db.InterestTags.FindAsync([id], ct);
        if (tag is null) return NotFound();
        db.InterestTags.Remove(tag);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Trims, discards empty entries, and deduplicates – synonyms are a pure search aid.</summary>
    private static List<string> Clean(List<string>? values) =>
        [.. (values ?? []).Select(s => s.Trim()).Where(s => s.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase)];
}
