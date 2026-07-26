using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// Die Units einer Lehrwerk-Reihe. Band und Unit liegen in <b>einer</b> Ebene (<c>grade</c> = Band):
/// „Access 8, Unit 3" ist eine Zeile. Der fachliche Wert steckt in <c>topics</c>/<c>grammar</c>/
/// <c>vocabularyNotes</c> – das ist der Stoff, den ein KI-Creator kennen muss, statt ihn zu erfinden.
/// Lesen darf jeder Creator, schreiben nur der Owner der Reihe.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/textbook-series/{seriesId:int}/units")]
[Tags("Creator – Textbook Series")]
[Produces("application/json")]
[Authorize(Roles = Roles.Creator)]
public class SeriesUnitsController(PuglingDbContext db) : ControllerBase
{
    static SeriesUnitResponse Map(SeriesUnit u) =>
        new(u.Id, u.SeriesId, u.Grade, u.OrderIndex, u.Label, u.Topics, u.Grammar, u.VocabularyNotes, u.CreatedAt);

    /// <summary>Alle Units der Reihe, nach Band und Reihenfolge.</summary>
    /// <param name="seriesId">Die Reihe.</param>
    /// <param name="grade">Nur Units dieses Bandes.</param>
    /// <param name="ct">Abbruch-Token.</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SeriesUnitResponse>>> List(int seriesId,
        [FromQuery] int? grade, CancellationToken ct)
    {
        if (!await db.TextbookSeries.AnyAsync(s => s.Id == seriesId, ct)) return NotFound();

        var query = db.SeriesUnits.AsNoTracking().Where(u => u.SeriesId == seriesId);
        if (grade is int g) query = query.Where(u => u.Grade == g);

        return await query.OrderBy(u => u.Grade).ThenBy(u => u.OrderIndex).ThenBy(u => u.Id)
            .Select(u => Map(u)).ToListAsync(ct);
    }

    /// <summary>Eine Unit der Reihe.</summary>
    [HttpGet("{unitId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SeriesUnitResponse>> Get(int seriesId, int unitId, CancellationToken ct)
    {
        var unit = await db.SeriesUnits.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == unitId && u.SeriesId == seriesId, ct);
        return unit is null ? NotFound() : Map(unit);
    }

    /// <summary>Hängt eine Unit an die Reihe (nur Owner). Ohne <c>orderIndex</c> landet sie hinten im Band.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SeriesUnitResponse>> Create(int seriesId, CreateSeriesUnitDto dto, CancellationToken ct)
    {
        var series = await db.TextbookSeries.FirstOrDefaultAsync(s => s.Id == seriesId, ct);
        if (series is null) return NotFound();
        if (!ClaimsPrincipalExtensions.IsOwnedBy(series.OwnerFatherId, User.CreatorId()))
            return this.ProblemWithCode(ApiErrors.NotOwner, "Only the owner of the series may add units.");
        if (string.IsNullOrWhiteSpace(dto.Label)) return this.ProblemWithCode(ApiErrors.ValidationError, "Label is required.");

        var unit = new SeriesUnit
        {
            SeriesId = seriesId,
            Grade = dto.Grade,
            OrderIndex = dto.OrderIndex ?? await NextOrderIndexAsync(seriesId, dto.Grade, ct),
            Label = dto.Label.Trim(),
            Topics = Trimmed(dto.Topics),
            Grammar = Trimmed(dto.Grammar),
            VocabularyNotes = Trimmed(dto.VocabularyNotes),
        };
        db.SeriesUnits.Add(unit);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { seriesId, unitId = unit.Id }, Map(unit));
    }

    /// <summary>Ändert eine Unit (partiell, nur Owner der Reihe).</summary>
    [HttpPatch("{unitId:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SeriesUnitResponse>> Update(int seriesId, int unitId,
        UpdateSeriesUnitDto dto, CancellationToken ct)
    {
        var unit = await db.SeriesUnits.Include(u => u.Series)
            .FirstOrDefaultAsync(u => u.Id == unitId && u.SeriesId == seriesId, ct);
        if (unit is null) return NotFound();
        if (!ClaimsPrincipalExtensions.IsOwnedBy(unit.Series?.OwnerFatherId, User.CreatorId()))
            return this.ProblemWithCode(ApiErrors.NotOwner, "Only the owner of the series may change its units.");

        if (dto.Label is not null)
        {
            var label = dto.Label.Trim();
            if (label.Length == 0) return this.ProblemWithCode(ApiErrors.ValidationError, "Label must not be empty.");
            unit.Label = label;
        }
        if (dto.Grade.HasValue) unit.Grade = dto.Grade;
        if (dto.OrderIndex.HasValue) unit.OrderIndex = dto.OrderIndex.Value;
        if (dto.Topics is not null) unit.Topics = Trimmed(dto.Topics);
        if (dto.Grammar is not null) unit.Grammar = Trimmed(dto.Grammar);
        if (dto.VocabularyNotes is not null) unit.VocabularyNotes = Trimmed(dto.VocabularyNotes);

        await db.SaveChangesAsync(ct);
        return Map(unit);
    }

    /// <summary>
    /// Löscht eine Unit (nur Owner der Reihe). Kind-Lehrbücher, die auf sie zeigen, verlieren nur die
    /// Zuordnung (SetNull) – der Lernstand des Kindes hängt nicht an der Unit.
    /// </summary>
    [HttpDelete("{unitId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int seriesId, int unitId, CancellationToken ct)
    {
        var unit = await db.SeriesUnits.Include(u => u.Series)
            .FirstOrDefaultAsync(u => u.Id == unitId && u.SeriesId == seriesId, ct);
        if (unit is null) return NotFound();
        if (!ClaimsPrincipalExtensions.IsOwnedBy(unit.Series?.OwnerFatherId, User.CreatorId()))
            return this.ProblemWithCode(ApiErrors.NotOwner, "Only the owner of the series may delete its units.");

        db.SeriesUnits.Remove(unit);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Nächste freie Position im Band – so muss der Aufrufer die Reihenfolge nicht kennen.</summary>
    private async Task<int> NextOrderIndexAsync(int seriesId, int? grade, CancellationToken ct)
    {
        var highest = await db.SeriesUnits
            .Where(u => u.SeriesId == seriesId && u.Grade == grade)
            .Select(u => (int?)u.OrderIndex)
            .MaxAsync(ct);
        return (highest ?? 0) + 1;
    }

    private static string? Trimmed(string? value) => value?.Trim() is { Length: > 0 } v ? v : null;
}
