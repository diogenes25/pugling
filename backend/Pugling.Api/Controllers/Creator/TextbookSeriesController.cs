using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// Lehrwerk-Reihen („Access", „Green Line") als <b>geteilter</b> Katalog. Sie sind das Bindeglied zwischen
/// dem Kind (<c>supervisor/children/{id}/textbooks</c> verweist auf die Reihe) und dem Creator-Profil, das
/// auf dieses Werk optimiert ist – nur weil beide Seiten denselben Datensatz nennen, ist die Frage „wer
/// kennt das Material dieses Kindes?" maschinell beantwortbar statt ein Freitext-Vergleich.
/// Eigentum wie bei der Übung: <b>lesen darf jeder Creator</b>, ändern nur der Owner.
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
    /// Projektion samt Unit-Zahl. Das Eigentum steht hier <b>ausgeschrieben</b> statt als Aufruf von
    /// <see cref="ClaimsPrincipalExtensions.IsOwnedBy"/>: EF müsste den Methodenaufruf übersetzen und
    /// bräche zur Laufzeit. Fehlender <c>fid</c> ⇒ <c>false</c> (fail-closed, gleiche Regel wie dort).
    /// </summary>
    private static IQueryable<TextbookSeriesResponse> Project(IQueryable<TextbookSeries> q, int? fid) =>
        q.Select(s => new TextbookSeriesResponse(s.Id, s.Name, s.Slug, s.Publisher, s.SubjectName, s.SubjectId,
            s.SchoolTypes, s.SourceLanguage, s.TargetLanguage, s.Notes, s.OwnerAdultId,
            fid != null && s.OwnerAdultId == fid, s.Units.Count, s.CreatedAt));

    /// <summary>
    /// Alle Reihen (alphabetisch), optional gefiltert. Die Gesamtzahl vor dem Paging steht im Header
    /// <c>X-Total-Count</c>.
    /// </summary>
    /// <param name="search">Teilstring in Name, Slug oder Verlag.</param>
    /// <param name="subjectId">Nur Reihen zu diesem Katalog-Fach.</param>
    /// <param name="mineOnly">true = nur eigene Reihen.</param>
    /// <param name="skip">Anzahl zu überspringender Einträge (Paging).</param>
    /// <param name="take">Maximale Trefferzahl (1..500).</param>
    /// <param name="ct">Abbruch-Token.</param>
    [HttpGet]
    public async Task<IEnumerable<TextbookSeriesResponse>> List(
        [FromQuery] string? search = null,
        [FromQuery] int? subjectId = null,
        [FromQuery] bool? mineOnly = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = PagingExtensions.DefaultTake,
        CancellationToken ct = default)
    {
        var fid = User.CreatorId();
        var query = db.TextbookSeries.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s => s.Name.Contains(search) || s.Slug.Contains(search)
                                     || (s.Publisher != null && s.Publisher.Contains(search)));
        if (subjectId is int sid) query = query.Where(s => s.SubjectId == sid);
        if (mineOnly is true) query = query.Where(s => s.OwnerAdultId == fid);

        return await Project(query.OrderBy(s => s.Name).ThenBy(s => s.Id), fid)
            .ToPagedListAsync(Response, skip, take, ct);
    }

    /// <summary>Eine Reihe per Id.</summary>
    [HttpGet("{seriesId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TextbookSeriesResponse>> Get(int seriesId, CancellationToken ct = default)
    {
        var series = await Project(db.TextbookSeries.AsNoTracking().Where(s => s.Id == seriesId), User.CreatorId())
            .FirstOrDefaultAsync(ct);
        return series is null ? NotFound() : series;
    }

    /// <summary>
    /// Legt eine Reihe an. Der Slug entsteht aus dem Namen; ist er schon vergeben, kommt die bestehende
    /// Reihe zurück (idempotent, Muster <c>interest-tags</c>) – ein Agent darf denselben Katalog-Aufbau
    /// gefahrlos wiederholen, statt Dubletten zu erzeugen.
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

        var fid = User.CreatorId();
        var existing = await Project(db.TextbookSeries.AsNoTracking().Where(s => s.Slug == slug), fid).FirstOrDefaultAsync(ct);
        if (existing is not null) return Ok(existing);

        var series = new TextbookSeries
        {
            Name = dto.Name.Trim(),
            Slug = slug,
            Publisher = Trimmed(dto.Publisher),
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

    /// <summary>Ändert eine Reihe (partiell, nur Owner). Der Slug bleibt unveränderlich.</summary>
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

        if (dto.Name is not null)
        {
            var name = dto.Name.Trim();
            if (name.Length == 0) return this.ProblemWithCode(ApiErrors.ValidationError, "Name must not be empty.");
            series.Name = name;
        }
        if (dto.Publisher is not null) series.Publisher = Trimmed(dto.Publisher);
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
    /// Löscht eine Reihe samt Units (nur Owner). Bewusst <b>ohne</b> Verwendungs-Sperre: Kind-Lehrbücher
    /// und Profile verlieren nur die Zuordnung (SetNull) und bleiben mit ihrem Freitext arbeitsfähig.
    /// </summary>
    [HttpDelete("{seriesId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int seriesId, CancellationToken ct = default)
    {
        var series = await db.TextbookSeries.FirstOrDefaultAsync(s => s.Id == seriesId, ct);
        if (series is null) return NotFound();
        if (!ClaimsPrincipalExtensions.IsOwnedBy(series.OwnerAdultId, User.CreatorId()))
            return this.ProblemWithCode(ApiErrors.NotOwner, "Only the owner may delete this textbook series.");

        db.TextbookSeries.Remove(series);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static string? Trimmed(string? value) => value?.Trim() is { Length: > 0 } v ? v : null;
}
