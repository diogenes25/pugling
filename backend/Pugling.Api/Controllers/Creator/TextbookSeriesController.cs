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
    /// (fail-closed, same rule as there).
    /// </summary>
    private static IQueryable<TextbookSeriesResponse> Project(IQueryable<TextbookSeries> q, int? fid) =>
        q.Select(s => new TextbookSeriesResponse(s.Id, s.Name, s.Slug, s.PublisherId, s.Publisher!.Name,
            s.SubjectName, s.SubjectId, s.SchoolTypes, s.SourceLanguage, s.TargetLanguage, s.Notes, s.OwnerAdultId,
            fid != null && s.OwnerAdultId == fid, s.Units.Count,
            s.Units.Min(u => (int?)u.Grade), s.Units.Max(u => (int?)u.Grade), s.CreatedAt));

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
            query = query.Where(s => s.Name.Contains(search) || s.Slug.Contains(search)
                                     || (s.Publisher != null && s.Publisher.Name.Contains(search)));
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
    /// Creates a series. The slug is derived from the name; if it is already taken, the existing
    /// series comes back (idempotent, same pattern as <c>interest-tags</c>) – an agent may safely repeat the same
    /// catalog build instead of creating duplicates.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TextbookSeriesResponse>> Create(CreateTextbookSeriesDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return this.ProblemWithCode(ApiErrors.ValidationError, "Name is required.");

        var slug = InterestSlug.From(dto.Name);
        if (slug.Length == 0) return this.ProblemWithCode(ApiErrors.ValidationError, "Name must contain at least one letter or digit.");
        if (dto.SubjectId is int sid && !await db.Subjects.AnyAsync(s => s.Id == sid, ct))
            return this.ProblemWithCode(ApiErrors.InvalidReference, "SubjectId does not reference an existing subject.");
        if (dto.PublisherId is int pid && !await db.Publishers.AnyAsync(p => p.Id == pid, ct))
            return this.ProblemWithCode(ApiErrors.InvalidReference, "PublisherId does not reference an existing publisher.");

        var fid = User.CreatorId();
        var existing = await Project(db.TextbookSeries.AsNoTracking().Where(s => s.Slug == slug), fid).FirstOrDefaultAsync(ct);
        if (existing is not null) return Ok(existing);

        var series = new TextbookSeries
        {
            Name = dto.Name.Trim(),
            Slug = slug,
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

    /// <summary>Changes a series (partial, owner only). The slug remains immutable.</summary>
    [HttpPatch("{seriesId:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TextbookSeriesResponse>> Update(int seriesId, UpdateTextbookSeriesDto dto, CancellationToken ct = default)
    {
        var series = await db.TextbookSeries.FirstOrDefaultAsync(s => s.Id == seriesId, ct);
        if (series is null) return NotFound();
        var fid = User.CreatorId();
        if (!ClaimsPrincipalExtensions.IsOwnedBy(series.OwnerAdultId, fid))
            return this.ProblemWithCode(ApiErrors.NotOwner, "Only the owner may change this textbook series.");
        if (dto.SubjectId is int sid && !await db.Subjects.AnyAsync(s => s.Id == sid, ct))
            return this.ProblemWithCode(ApiErrors.InvalidReference, "SubjectId does not reference an existing subject.");
        if (dto.PublisherId is int pid && !await db.Publishers.AnyAsync(p => p.Id == pid, ct))
            return this.ProblemWithCode(ApiErrors.InvalidReference, "PublisherId does not reference an existing publisher.");

        if (dto.Name is not null)
        {
            var name = dto.Name.Trim();
            if (name.Length == 0) return this.ProblemWithCode(ApiErrors.ValidationError, "Name must not be empty.");
            series.Name = name;
        }
        if (dto.PublisherId.HasValue) series.PublisherId = dto.PublisherId;
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
