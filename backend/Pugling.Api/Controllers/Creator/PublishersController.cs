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
/// <para>
/// Deleting is the one exception, and it is not an owner check on the publisher but on what hangs off it
/// (B-127): an ownerless row must not reach into owned ones. See <see cref="Delete"/>.
/// </para>
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/publishers")]
[Tags("Creator – Publishers")]
[Produces("application/json")]
[Authorize(Roles = Roles.Creator)]
public class PublishersController(PuglingDbContext db) : ControllerBase
{
    /// <summary>
    /// Projection including the two counts. The foreign one carries the delete rule into the response, so
    /// a caller can see the lock instead of running into it (see <see cref="Delete"/>); ownership is
    /// spelled out inline rather than via <see cref="ClaimsPrincipalExtensions.IsOwnedBy"/>, which EF
    /// cannot translate - same fail-closed reading, and the same "ownerless counts as foreign".
    /// </summary>
    private IQueryable<PublisherResponse> Project(IQueryable<Publisher> q, int? fid) =>
        q.Select(p => new PublisherResponse(p.Id, p.Name, p.Slug,
            db.TextbookSeries.Count(s => s.PublisherId == p.Id),
            db.TextbookSeries.Count(s => s.PublisherId == p.Id && (s.OwnerAdultId == null || s.OwnerAdultId != fid)),
            p.CreatedAt));

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

        return await Project(query.OrderBy(p => p.Slug).ThenBy(p => p.Id), User.CreatorId())
            .ToPagedListAsync(Response, skip, take, ct);
    }

    /// <summary>A publisher by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublisherResponse>> Get(int id, CancellationToken ct = default)
    {
        var publisher = await Project(db.Publishers.AsNoTracking().Where(p => p.Id == id), User.CreatorId())
            .FirstOrDefaultAsync(ct);
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
        var existing = await Project(db.Publishers.AsNoTracking().Where(p => p.Slug == slug), User.CreatorId())
            .FirstOrDefaultAsync(ct);
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

        // A fresh publisher has no series at all yet, so both counts are zero by construction.
        return CreatedAtAction(nameof(Get), new { id = publisher.Id },
            new PublisherResponse(publisher.Id, publisher.Name, publisher.Slug, 0, 0, publisher.CreatedAt));
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
        // Re-read through the shared projection rather than counting by hand: the response carries two
        // counts now, and a second hand-written copy of the ownership predicate would drift from it.
        return await Project(db.Publishers.AsNoTracking().Where(p => p.Id == id), User.CreatorId()).FirstAsync(ct);
    }

    /// <summary>
    /// Deletes a publisher, as long as only the caller's own series point at it. Those lose the assignment
    /// (SetNull) - a publisher carries no content, and for its own author that loss only costs a
    /// filter/display value.
    /// <para>
    /// That sentence used to be the whole justification, and it was only true for the deleter (B-127): the
    /// same call clears the assignment on every OTHER account's series too, with no way back. So a series
    /// of a foreign account turns this into 409 <c>publisher_in_use</c>. The lock is narrower than a
    /// confirmation prompt and hits better: it allows exactly the case this page exists for (cleaning up a
    /// typo one has made oneself) and blocks exactly the harmful one.
    /// </para>
    /// <para>
    /// A series without an owner counts as foreign, not as free. <c>OwnerAdultId</c> is nullable and means
    /// "seeded, owned by nobody"; reading that fail-closed is the same rule <c>IsOwnedBy</c> follows, and
    /// it is what protects the shared catalog.
    /// </para>
    /// <para>
    /// The null branch is spelled out rather than left to <c>!=</c>, for one reason that survives
    /// measurement: it keeps blocking when the <c>fid</c> claim is missing. EF Core compensates C# null
    /// semantics by default, so for a present claim the short form produces the same SQL - the two differ
    /// only for a null <c>fid</c>, where the short form would turn the lock off entirely. That claim
    /// cannot go missing behind <c>Roles.Creator</c> today; the point is that it stays harmless if it ever
    /// does. (Note for the next reader: "SQL drops NULL rows" is true of hand-written SQL, not of this.)
    /// </para>
    /// <para>
    /// The <c>Admin</c> role overrides the lock - and that valve is the honest limit of this design, not a
    /// way out for the caller: <see cref="Adult.IsAdmin"/> is a break-glass flag set in the database, no
    /// endpoint and no DTO writes it. So two creators who each hang a series on the same publisher really
    /// are both locked out until an operator steps in. The seeded catalog is the everyday case of that:
    /// "Klett" carries the ownerless "Green Line 1", which makes it undeletable by design - removing it
    /// would strip the publisher from a row the whole catalog shares.
    /// </para>
    /// <para>
    /// The message therefore has to name the ownerless case too. Naming only "another account" sends the
    /// caller looking for a foreign series that does not exist, on the one database where the lock is
    /// certain to fire. <c>PublisherResponse.ForeignSeriesCount</c> lets a UI show the lock beforehand.
    /// </para>
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var publisher = await db.Publishers.FindAsync([id], ct);
        if (publisher is null) return NotFound();

        // Check and delete belong in one transaction. Without it they are two round-trips on two pooled
        // connections, and a series created in between would be unassigned by the FK's SetNull - silently,
        // because there is no `Restrict` backstop here the way the subject delete has one (SetNull is what
        // lets the caller's OWN series survive). The read holds SQLite's SHARED lock until commit, so no
        // other connection can commit that insert while the check is being acted on.
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var fid = User.CreatorId();
        if (!User.IsAdmin()
            && await db.TextbookSeries.AnyAsync(
                s => s.PublisherId == id && (s.OwnerAdultId == null || s.OwnerAdultId != fid), ct))
            return this.ProblemWithCode(ApiErrors.PublisherInUse,
                "A series of another account, or an ownerless series of the shared catalog, points at this publisher.");

        db.Publishers.Remove(publisher);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return NoContent();
    }
}
