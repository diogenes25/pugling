using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// Textbook series ("Access", "Green Line") as a <b>shared</b> catalog. They are the link between
/// the child (<c>supervisor/children/{id}/textbooks</c> refers to the series) and the creator profile that
/// is optimized for this work – only because both sides name the same record is the question "who
/// knows this child's material?" machine-answerable instead of a free-text comparison.
/// Ownership as with the exercise: <b>any creator may read</b>, only the owner may change.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/textbook-series")]
[Tags("Creator – Textbook Series")]
[Produces("application/json")]
[Authorize(Roles = Roles.Creator)]
public class TextbookSeriesController(PuglingDbContext db) : ControllerBase
{
    /// <summary>
    /// Projection along with unit count and the grade range actually present. Ownership is spelled out
    /// <b>inline</b> here instead of a call to <see cref="ClaimsPrincipalExtensions.IsOwnedBy"/>: EF would
    /// need to translate the method call and would break at runtime. Missing <c>fid</c> ⇒ <c>false</c>
    /// (fail-closed, same rule as there). Count/Min/Max come from <b>one</b> correlated subquery
    /// (<c>GroupBy(_ =&gt; 1)</c> collapses the per-series units into a single row) instead of three.
    /// </summary>
    private static IQueryable<TextbookSeriesResponse> Project(IQueryable<TextbookSeries> q, int? fid) =>
        q.Select(s => new
        {
            s,
            stat = s.Units.GroupBy(_ => 1)
                .Select(g => new { Count = g.Count(), Min = g.Min(u => (int?)u.Grade), Max = g.Max(u => (int?)u.Grade) })
                .FirstOrDefault(),
        })
        .Select(x => new TextbookSeriesResponse(x.s.Id, x.s.Name, x.s.Slug, x.s.PublisherId, x.s.Publisher!.Name,
            x.s.SubjectName, x.s.SubjectId, x.s.SchoolTypes, x.s.SourceLanguage, x.s.TargetLanguage, x.s.Notes,
            x.s.OwnerAdultId, fid != null && x.s.OwnerAdultId == fid,
            x.stat != null ? x.stat.Count : 0, x.stat != null ? x.stat.Min : null, x.stat != null ? x.stat.Max : null,
            x.s.CreatedAt));

    /// <summary>
    /// All series (alphabetically), optionally filtered. The total count before paging is in the header
    /// <c>X-Total-Count</c>.
    /// </summary>
    /// <param name="search">Substring in name, slug, or publisher.</param>
    /// <param name="subjectId">Only series for this catalog subject.</param>
    /// <param name="publisherId">Only series of this publisher.</param>
    /// <param name="schoolTypes">Only series meant for (any of) these school types.</param>
    /// <param name="grade">Only series with at least one unit of this volume.</param>
    /// <param name="mineOnly">true = only own series.</param>
    /// <param name="skip">Number of entries to skip (paging).</param>
    /// <param name="take">Maximum number of hits (1..500).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    public async Task<IEnumerable<TextbookSeriesResponse>> List(
        [FromQuery] string? search = null,
        [FromQuery] int? subjectId = null,
        [FromQuery] int? publisherId = null,
        [FromQuery] SchoolTypes? schoolTypes = null,
        [FromQuery] int? grade = null,
        [FromQuery] bool? mineOnly = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = PagingExtensions.DefaultTake,
        CancellationToken ct = default)
    {
        var fid = User.CreatorId();
        var query = db.TextbookSeries.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            // LIKE instead of Contains: `instr()` is byte-exact and ignores the column collation (B-128).
            // The publisher name has no slug to fall back on, so it was the worst hit of the three.
            var pattern = SearchPattern.Contains(search);
            query = query.Where(s => EF.Functions.Like(s.Name, pattern, SearchPattern.Escape)
                                     || EF.Functions.Like(s.Slug, pattern, SearchPattern.Escape)
                                     || (s.Publisher != null
                                         && EF.Functions.Like(s.Publisher.Name, pattern, SearchPattern.Escape)));
        }
        if (subjectId is int sid) query = query.Where(s => s.SubjectId == sid);
        if (publisherId is int pid) query = query.Where(s => s.PublisherId == pid);
        // None (0) matches everything - the same "no restriction" reading the flags carry elsewhere.
        if (schoolTypes is SchoolTypes st and not SchoolTypes.None)
            query = query.Where(s => s.SchoolTypes == SchoolTypes.None || (s.SchoolTypes & st) != 0);
        if (grade is int g) query = query.Where(s => s.Units.Any(u => u.Grade == g));
        if (mineOnly is true) query = query.Where(s => s.OwnerAdultId == fid);

        return await Project(query.OrderBy(s => s.Name).ThenBy(s => s.Id), fid)
            .ToPagedListAsync(Response, skip, take, ct);
    }

    /// <summary>A series by id.</summary>
    [HttpGet("{seriesId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TextbookSeriesResponse>> Get(int seriesId, CancellationToken ct = default)
    {
        var series = await Project(db.TextbookSeries.AsNoTracking().Where(s => s.Id == seriesId), User.CreatorId())
            .FirstOrDefaultAsync(ct);
        return series is null ? NotFound() : series;
    }

    /// <summary>
    /// Checks that <paramref name="subjectId"/> and <paramref name="publisherId"/>, if given, reference
    /// existing rows – shared between <see cref="Create"/> and <see cref="Update"/> so the two round trips
    /// stay in one place instead of being duplicated per action.
    /// </summary>
    private async Task<ObjectResult?> ValidateReferencesAsync(int? subjectId, int? publisherId, CancellationToken ct)
    {
        if (subjectId is int sid && !await db.Subjects.AnyAsync(s => s.Id == sid, ct))
            return this.ProblemWithCode(ApiErrors.InvalidReference, "SubjectId does not reference an existing subject.");
        if (publisherId is int pid && !await db.Publishers.AnyAsync(p => p.Id == pid, ct))
            return this.ProblemWithCode(ApiErrors.InvalidReference, "PublisherId does not reference an existing publisher.");
        return null;
    }

    /// <summary>
    /// Creates a series. The slug is derived from the name; if it is already taken <b>by a series of the
    /// same display name</b>, that series comes back (idempotent, same pattern as <c>interest-tags</c>) –
    /// an agent may safely repeat the same catalog build instead of creating duplicates. A taken slug
    /// whose series meanwhile carries a <em>different</em> name is a conflict (409), not a hit: the slug
    /// is immutable and stops matching the name after a rename.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TextbookSeriesResponse>> Create(CreateTextbookSeriesDto dto, CancellationToken ct = default)
    {
        var (slug, slugProblem) = this.DeriveRequiredSlug(dto.Name, "Name");
        if (slugProblem is not null) return slugProblem;
        if (await ValidateReferencesAsync(dto.SubjectId, dto.PublisherId, ct) is { } refProblem) return refProblem;

        var fid = User.CreatorId();
        var name = dto.Name.Trim();

        // The slug hit is what makes this endpoint idempotent - but only while name and slug still agree.
        // The slug freezes on rename, so a series named "Green Line" can still carry the slug "access":
        // posting "Access" would then hit it and hand back a series of a different name, and a catalog
        // agent would hang its units off the wrong one without ever seeing an error (B-133). Only the same
        // display name may be answered with the existing row.
        // Known and accepted asymmetry: this comparison folds full Unicode, the two below fold in SQLite
        // (`NOCASE`, ASCII only). NOCASE-equal always implies OrdinalIgnoreCase-equal, so this branch can
        // never hand out a row of a different name - the residue runs the other way: after a rename has
        // decoupled name and slug, a non-ASCII case pair ("ökotest" next to "Ökotest") passes both checks
        // and creates a second row. Closing that would need an ICU collation, the same limit
        // Services/Shared/SearchPattern.cs already documents for the search.
        var existing = await Project(db.TextbookSeries.AsNoTracking().Where(s => s.Slug == slug), fid).FirstOrDefaultAsync(ct);
        if (existing is not null)
            return string.Equals(existing.Name, name, StringComparison.OrdinalIgnoreCase)
                ? Ok(existing)
                : this.ProblemWithCode(ApiErrors.DuplicateTextbookSeries,
                    "Another textbook series already uses the slug this name derives to.");

        // And the mirror image: a free slug does not mean a free display name, for the same reason.
        // Case-insensitive through the NOCASE collation on TextbookSeries.Name.
        if (await db.TextbookSeries.AnyAsync(s => s.Name == name, ct))
            return this.ProblemWithCode(ApiErrors.DuplicateTextbookSeries,
                "Another textbook series already uses this display name.");

        var series = new TextbookSeries
        {
            Name = name,
            Slug = slug!,
            PublisherId = dto.PublisherId,
            SubjectName = Trimmed(dto.SubjectName),
            SubjectId = dto.SubjectId,
            SchoolTypes = dto.SchoolTypes ?? SchoolTypes.None,
            SourceLanguage = Trimmed(dto.SourceLanguage),
            TargetLanguage = Trimmed(dto.TargetLanguage),
            Notes = Trimmed(dto.Notes),
            OwnerAdultId = fid,
        };
        db.TextbookSeries.Add(series);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { seriesId = series.Id },
            await Project(db.TextbookSeries.AsNoTracking().Where(s => s.Id == series.Id), fid).FirstAsync(ct));
    }

    /// <summary>
    /// Changes a series (partial, owner only). The slug remains immutable, but still decides: a new name
    /// whose slug another series already carries is rejected, the same rule <c>Create</c> enforces
    /// (B-124) - otherwise two series share a display name in every picker.
    /// </summary>
    [HttpPatch("{seriesId:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TextbookSeriesResponse>> Update(int seriesId, UpdateTextbookSeriesDto dto, CancellationToken ct = default)
    {
        var series = await db.TextbookSeries.FirstOrDefaultAsync(s => s.Id == seriesId, ct);
        if (series is null) return NotFound();
        var fid = User.CreatorId();
        if (!ClaimsPrincipalExtensions.IsOwnedBy(series.OwnerAdultId, fid))
            return this.ProblemWithCode(ApiErrors.NotOwner, "Only the owner may change this textbook series.");
        if (await ValidateReferencesAsync(dto.SubjectId, dto.PublisherId, ct) is { } refProblem) return refProblem;

        if (dto.Name is not null)
        {
            var name = dto.Name.Trim();
            if (name.Length == 0) return this.ProblemWithCode(ApiErrors.ValidationError, "Name must not be empty.");

            var (slug, slugNameProblem) = this.DeriveRequiredSlug(name, "Name");
            if (slugNameProblem is not null) return slugNameProblem;
            // Excluded by id, not by slug - see PublishersController.Update. The lookup is global like
            // Create's: series slugs are unique across creators, not per owner.
            if (await db.TextbookSeries.AnyAsync(s => s.Id != seriesId && s.Slug == slug, ct))
                return this.ProblemWithCode(ApiErrors.DuplicateTextbookSeries,
                    "Another textbook series already uses the slug this name derives to.");
            // The slug check alone is not enough once any series has been renamed: from then on name and
            // slug diverge, and only this comparison still sees the display name the guard is about
            // (B-133). Case-insensitive through the NOCASE collation on the column.
            if (await db.TextbookSeries.AnyAsync(s => s.Id != seriesId && s.Name == name, ct))
                return this.ProblemWithCode(ApiErrors.DuplicateTextbookSeries,
                    "Another textbook series already uses this display name.");

            series.Name = name;
        }
        if (dto.PublisherId.HasValue) series.PublisherId = dto.PublisherId;
        if (dto.ClearPublisherId) series.PublisherId = null;
        if (dto.SubjectName is not null) series.SubjectName = Trimmed(dto.SubjectName);
        if (dto.SubjectId.HasValue) series.SubjectId = dto.SubjectId;
        if (dto.SchoolTypes.HasValue) series.SchoolTypes = dto.SchoolTypes.Value;
        if (dto.SourceLanguage is not null) series.SourceLanguage = Trimmed(dto.SourceLanguage);
        if (dto.TargetLanguage is not null) series.TargetLanguage = Trimmed(dto.TargetLanguage);
        if (dto.Notes is not null) series.Notes = Trimmed(dto.Notes);

        await db.SaveChangesAsync(ct);
        return await Project(db.TextbookSeries.AsNoTracking().Where(s => s.Id == seriesId), fid).FirstAsync(ct);
    }

    /// <summary>
    /// Deletes a series along with its units and their exercises (owner only). Not possible while an
    /// exercise in it is used in a study plan, a class test or an objective milestone (B-106: exercises
    /// now cascade from series → unit). Child textbooks and profiles pointing at the series itself only
    /// lose the assignment (SetNull) and remain usable with their free text.
    /// </summary>
    [HttpDelete("{seriesId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int seriesId, CancellationToken ct = default)
    {
        var series = await db.TextbookSeries.FirstOrDefaultAsync(s => s.Id == seriesId, ct);
        if (series is null) return NotFound();
        if (!ClaimsPrincipalExtensions.IsOwnedBy(series.OwnerAdultId, User.CreatorId()))
            return this.ProblemWithCode(ApiErrors.NotOwner, "Only the owner may delete this textbook series.");
        if (await ExerciseUsageQueries.AnyBlockingAsync(db,
                db.Exercises.Where(x => x.SeriesUnit!.SeriesId == seriesId),
                db.SeriesUnits.Where(u => u.SeriesId == seriesId), ct))
            return this.ProblemWithCode(ApiErrors.ExerciseInUse,
                "Content in this series is still used in a study plan, a class test or an objective "
                + "milestone; remove it there first.");

        db.TextbookSeries.Remove(series);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static string? Trimmed(string? value) => value?.Trim() is { Length: > 0 } v ? v : null;
}
