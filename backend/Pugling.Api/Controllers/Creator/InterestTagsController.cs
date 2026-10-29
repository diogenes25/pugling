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
    /// <summary>
    /// Projects along with the usage counts from all <b>three</b> sides – they show whether a tag is
    /// "dead". Since B-46 the exercises are the third side, and forgetting them here would have made a
    /// tag that dresses exercises look unused.
    /// </summary>
    private static IQueryable<InterestTagResponse> Project(IQueryable<InterestTag> q) =>
        q.Select(t => new InterestTagResponse(t.Id, t.Slug, t.Label, t.Facet, t.Synonyms, t.Color,
            t.MediaLinks.Count, t.ChildInterests.Count, t.CreatedAt, t.ExerciseLinks.Count));

    /// <summary>
    /// All tags (alphabetically by slug), optionally filtered. The total count (before paging) is
    /// in the <c>X-Total-Count</c> header.
    /// </summary>
    /// <param name="search">Substring in slug or label.</param>
    /// <param name="facet">Only tags of this facet (e.g. only styles).</param>
    /// <param name="unused">
    /// true = only tags with no usage at all (cleanup view) – no image, no child, and since B-46 no
    /// exercise either.
    /// </param>
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
        {
            // LIKE, not Contains: EF maps Contains to SQLite's byte-exact instr() - see SearchPattern (B-135).
            // The slug is lowercase by derivation, so folding case can only ever add hits here, never remove any.
            var pattern = SearchPattern.Contains(search);
            query = query.Where(t => EF.Functions.Like(t.Slug, pattern, SearchPattern.Escape)
                                     || EF.Functions.Like(t.Label, pattern, SearchPattern.Escape));
        }
        if (facet is not null)
            query = query.Where(t => t.Facet == facet);
        if (unused is true)
            query = query.Where(t => t.MediaLinks.Count == 0 && t.ChildInterests.Count == 0
                                     && t.ExerciseLinks.Count == 0);

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
    /// catalog build. A label that another tag already carries (exact comparison) is rejected with 409.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InterestTagResponse>> Create(CreateInterestTagDto dto, CancellationToken ct = default)
    {
        var (slug, problem) = this.DeriveRequiredSlug(dto.Label, "Label", dto.Slug);
        if (problem is not null) return problem;

        var existing = await Project(db.InterestTags.AsNoTracking().Where(t => t.Slug == slug)).FirstOrDefaultAsync(ct);
        if (existing is not null) return Ok(existing);

        // The slug cannot keep labels apart on its own: it may be set explicitly and freezes on rename (B-141).
        var label = dto.Label.Trim();
        if (await LabelTakenAsync(label, exceptId: null, ct))
            return this.ProblemWithCode(ApiErrors.DuplicateInterestTag, "Another interest tag already carries this label.");

        var tag = new InterestTag
        {
            Slug = slug!,
            Label = label,
            Facet = dto.Facet,
            Synonyms = Clean(dto.Synonyms),
            Color = dto.Color?.Trim() is { Length: > 0 } c ? c : null,
        };
        db.InterestTags.Add(tag);
        await db.SaveChangesAsync(ct);

        // All three usage counts are zero by construction: the tag exists as of this line and nothing can
        // point at it yet. Spelled out rather than projected, because a round trip would only re-read what
        // we just wrote.
        return CreatedAtAction(nameof(Get), new { id = tag.Id },
            new InterestTagResponse(tag.Id, tag.Slug, tag.Label, tag.Facet, tag.Synonyms, tag.Color, 0, 0,
                tag.CreatedAt, 0));
    }

    /// <summary>
    /// Changes label, facet, synonyms, or color. The <c>Slug</c> is deliberately <b>immutable</b> –
    /// it is the stable reference that images and child profiles hang off. A new label is rejected when another
    /// tag already carries it (exact comparison, B-141) or the slug it derives to (B-124) - otherwise two tags
    /// share a label in every picker.
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
            // Excluded by id, not by slug - see PublishersController.Update.
            if (await db.InterestTags.AnyAsync(t => t.Id != id && t.Slug == slug, ct))
                return this.ProblemWithCode(ApiErrors.DuplicateInterestTag,
                    "Another interest tag already uses the slug this label derives to.");
            if (await LabelTakenAsync(label, exceptId: id, ct))
                return this.ProblemWithCode(ApiErrors.DuplicateInterestTag, "Another interest tag already carries this label.");

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
    /// Deletes a tag along with its links to images and children (cascade). Links to <b>exercises</b> block
    /// the delete instead (409) – the one asymmetry that matters here (B-206).
    ///
    /// The line runs along <b>what the loss costs</b>. An image link or a child's preference can be set
    /// again from what is still there; a tag that dresses an exercise carries <b>authored information</b>
    /// that nothing else records, so a cascade would erase it silently and unrecoverably. Until B-46 only
    /// the first kind existed, which is why the old reasoning ("its loss only costs selection quality")
    /// was right at the time and is wrong now.
    ///
    /// The lock has a way out, and it needs one: the exercise editor shows the dressing and lets it be
    /// removed. Without that door this would be a dead end instead of a guard rail.
    ///
    /// <b>Two limits, so this does not read as more than it is.</b> It guards the tag, not the dressing:
    /// the exercise <c>PUT</c> is a replacement, so a caller that omits <c>InterestTagIds</c> still clears
    /// the dressing without a word (deliberate since B-46 - whether an omitted field should mean "keep" is
    /// a contract question of its own). And it is an application-level check, not a constraint: the foreign
    /// key stays <c>Cascade</c>, so a request that dresses an exercise between this check and the commit
    /// slips through.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var tag = await db.InterestTags.FindAsync([id], ct);
        if (tag is null) return NotFound();

        // Only the exercises block. Images and children stay outside the lock because their loss is
        // recoverable, not because of the cleanup view: `?unused=true` already requires all three counts to
        // be zero, so a broader lock could never fire there. What a broader lock would do is make the tag
        // catalog effectively append-only - nearly every tag carries an image or a child.
        var dressed = await db.ExerciseInterestLinks.CountAsync(l => l.InterestTagId == id, ct);
        if (dressed > 0)
            // "Remove it from them first" only where the caller may: the tag catalog is global, exercises
            // are grant-protected. Another creator's exercise can therefore block this delete for good, and
            // a message that hides that would promise a door this caller cannot open (B-150/B-160).
            return this.ProblemWithCode(ApiErrors.InterestTagInUse,
                $"This interest tag dresses {dressed} exercise(s) – the dressing is authored information "
                + "and cannot be restored. Remove it from those exercises first; ones you may not edit "
                + "have to be cleared by their author.");

        db.InterestTags.Remove(tag);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>
    /// Whether another tag carries exactly this label. Ordinal on purpose (B-141): another spelling is a distinct
    /// label, and a case-insensitive rule would need a NOCASE column and thus a migration.
    /// </summary>
    private Task<bool> LabelTakenAsync(string label, int? exceptId, CancellationToken ct) =>
        db.InterestTags.AnyAsync(t => t.Id != exceptId && t.Label == label, ct);

    /// <summary>Trims, discards empty entries, and deduplicates – synonyms are a pure search aid.</summary>
    private static List<string> Clean(List<string>? values) =>
        [.. (values ?? []).Select(s => s.Trim()).Where(s => s.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase)];
}
