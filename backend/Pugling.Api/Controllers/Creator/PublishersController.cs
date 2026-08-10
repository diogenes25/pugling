using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// The shared publisher vocabulary ("Cornelsen", "Klett") a <see cref="TextbookSeries"/> may point at.
/// Global and child-neutral like the vocabulary store: naming a publisher is not authorship, so - unlike
/// the series itself - there is no owner and no write restriction.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/publishers")]
[Tags("Creator – Publishers")]
[Produces("application/json")]
[Authorize(Roles = Roles.Creator)]
public class PublishersController(PuglingDbContext db) : ControllerBase
{
    private IQueryable<PublisherResponse> Project(IQueryable<Publisher> q) =>
        q.Select(p => new PublisherResponse(p.Id, p.Name, p.Slug,
            db.TextbookSeries.Count(s => s.PublisherId == p.Id), p.CreatedAt));

    /// <summary>
    /// All publishers (alphabetically by slug), optionally filtered. The total count before paging is in
    /// the header <c>X-Total-Count</c>.
    /// </summary>
    /// <param name="search">Substring in slug or name.</param>
    /// <param name="skip">Number of entries to skip (paging).</param>
    /// <param name="take">Maximum number of hits (1..500).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    public async Task<IEnumerable<PublisherResponse>> List(
        [FromQuery] string? search = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = PagingExtensions.DefaultTake,
        CancellationToken ct = default)
    {
        var query = db.Publishers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            // LIKE instead of Contains: `instr()` is byte-exact and ignores the column collation (B-128).
            var pattern = SearchPattern.Contains(search);
            query = query.Where(p => EF.Functions.Like(p.Slug, pattern, SearchPattern.Escape)
                                     || EF.Functions.Like(p.Name, pattern, SearchPattern.Escape));
        }

        return await Project(query.OrderBy(p => p.Slug).ThenBy(p => p.Id)).ToPagedListAsync(Response, skip, take, ct);
    }

    /// <summary>A publisher by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublisherResponse>> Get(int id, CancellationToken ct = default)
    {
        var publisher = await Project(db.Publishers.AsNoTracking().Where(p => p.Id == id)).FirstOrDefaultAsync(ct);
        return publisher is null ? NotFound() : publisher;
    }

    /// <summary>
    /// Creates a publisher. If the slug is already taken <b>by a publisher of the same display name</b>,
    /// that publisher comes back (idempotent) - so an agent can safely repeat the same catalog build
    /// instead of creating duplicates. A taken slug whose publisher meanwhile carries a <em>different</em>
    /// name is a conflict (409), not a hit: the slug is immutable and stops matching the name after a
    /// rename (B-136, the rule <c>textbook-series</c> already follows since B-133).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PublisherResponse>> Create(CreatePublisherDto dto, CancellationToken ct = default)
    {
        var (slug, problem) = this.DeriveRequiredSlug(dto.Name, "Name");
        if (problem is not null) return problem;

        var name = dto.Name.Trim();

        // The slug hit is what makes this endpoint idempotent - but only while name and slug still agree.
        // The slug freezes on rename, so a publisher named "Cornelsen" can still carry the slug "klett":
        // posting "Klett" would then hit it and hand back a publisher of a different name, and a catalog
        // agent would hang its series off the wrong one without ever seeing an error.
        // Known and accepted asymmetry (same as TextbookSeriesController): this comparison folds full
        // Unicode, the one below folds in SQLite (`NOCASE`, ASCII only). NOCASE-equal always implies
        // OrdinalIgnoreCase-equal, so this branch can never hand out a row of a different name - the
        // residue runs the other way: once a rename has decoupled name and slug, a non-ASCII case pair
        // ("ökotest" next to "Ökotest") passes both checks and creates a second row. Closing that would
        // need an ICU collation, the same limit Services/Shared/SearchPattern.cs already documents.
        var existing = await Project(db.Publishers.AsNoTracking().Where(p => p.Slug == slug)).FirstOrDefaultAsync(ct);
        if (existing is not null)
            return string.Equals(existing.Name, name, StringComparison.OrdinalIgnoreCase)
                ? Ok(existing)
                : this.ProblemWithCode(ApiErrors.DuplicatePublisher,
                    "Another publisher already uses the slug this name derives to.");

        // And the mirror image: a free slug does not mean a free display name, for the same reason. The
        // comparison rides on the NOCASE collation on Publisher.Name (B-128) - which had no equality
        // comparison to act on until this line existed.
        if (await db.Publishers.AnyAsync(p => p.Name == name, ct))
            return this.ProblemWithCode(ApiErrors.DuplicatePublisher,
                "Another publisher already uses this display name.");

        var publisher = new Publisher { Name = name, Slug = slug! };
        db.Publishers.Add(publisher);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = publisher.Id },
            new PublisherResponse(publisher.Id, publisher.Name, publisher.Slug, 0, publisher.CreatedAt));
    }

    /// <summary>
    /// Changes the display name. The slug stays fixed - agents reference publishers by it, so letting it
    /// travel along would break stable references. It is still the yardstick: a new name whose slug is
    /// already taken by another publisher is rejected, the same rule <c>Create</c> enforces (B-124).
    /// </summary>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PublisherResponse>> Update(int id, UpdatePublisherDto dto, CancellationToken ct = default)
    {
        var publisher = await db.Publishers.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (publisher is null) return NotFound();

        if (dto.Name is not null)
        {
            var name = dto.Name.Trim();
            if (name.Length == 0) return this.ProblemWithCode(ApiErrors.ValidationError, "Name must not be empty.");

            var (slug, problem) = this.DeriveRequiredSlug(name, "Name");
            if (problem is not null) return problem;
            // Excluded by id, not by slug: the row would otherwise always collide with itself and no
            // rename could ever go through (same trap as B-97's PATCH guard).
            if (await db.Publishers.AnyAsync(p => p.Id != id && p.Slug == slug, ct))
                return this.ProblemWithCode(ApiErrors.DuplicatePublisher,
                    "Another publisher already uses the slug this name derives to.");

            // The display name needs its own check, and only its absence was the defect (B-136): once a
            // rename has decoupled name and slug elsewhere, "slug is free" and "name is free" stop being
            // the same question - the target name can sit on a row whose slug derives from its old name.
            if (await db.Publishers.AnyAsync(p => p.Id != id && p.Name == name, ct))
                return this.ProblemWithCode(ApiErrors.DuplicatePublisher,
                    "Another publisher already uses this display name.");

            publisher.Name = name;
        }

        await db.SaveChangesAsync(ct);
        var seriesCount = await db.TextbookSeries.CountAsync(s => s.PublisherId == id, ct);
        return new PublisherResponse(publisher.Id, publisher.Name, publisher.Slug, seriesCount, publisher.CreatedAt);
    }

    /// <summary>
    /// Deletes a publisher. Series pointing at it only lose the assignment (SetNull) - deliberately without
    /// a usage lock: a publisher carries no content, its loss only costs a filter/display value.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var publisher = await db.Publishers.FindAsync([id], ct);
        if (publisher is null) return NotFound();
        db.Publishers.Remove(publisher);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
