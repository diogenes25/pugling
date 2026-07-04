using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Admin;

/// <summary>
/// Verwaltung der Kinder des angemeldeten Vaters, inklusive Punktestand. Der Vater ergibt sich aus
/// dem JWT (<c>fid</c>); kindbezogene Endpunkte sichert der <see cref="ChildOwnershipFilter"/> ab.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.V1 + "/children")]
[Tags("Admin – Children")]
[Produces("application/json")]
[Authorize(Roles = Roles.Vater)]
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class ChildrenController(PuglingDbContext db) : ControllerBase
{
    public record ChildResponse(int Id, int FatherId, string Name, int? BirthYear, DateTime CreatedAt,
        int PointsBalance);

    Task<ChildResponse?> ProjectOne(int childId) =>
        db.Children
            .Where(c => c.Id == childId)
            .Select(c => new ChildResponse(c.Id, c.FatherId, c.Name, c.BirthYear, c.CreatedAt,
                c.PointsEntries.Sum(p => (int?)p.Amount) ?? 0))
            .FirstOrDefaultAsync();

    /// <summary>Liste der eigenen Kinder.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ChildResponse>>> List()
    {
        var fatherId = User.FatherId();
        return await db.Children
            .Where(c => c.FatherId == fatherId)
            .OrderBy(c => c.Name)
            .Select(c => new ChildResponse(c.Id, c.FatherId, c.Name, c.BirthYear, c.CreatedAt,
                c.PointsEntries.Sum(p => (int?)p.Amount) ?? 0))
            .ToListAsync();
    }

    /// <summary>Ein einzelnes Kind.</summary>
    [HttpGet("{childId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChildResponse>> Get(int childId)
    {
        var child = await ProjectOne(childId);
        return child is null ? NotFound() : child;
    }

    public record CreateChildDto(string Name, int? BirthYear, string? Pin);

    /// <summary>Erstellt ein Kind unter dem angemeldeten Vater.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChildResponse>> Create(CreateChildDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return Problem(statusCode: 400, detail: "Name ist erforderlich.");

        var child = new Child { FatherId = User.FatherId()!.Value, Name = dto.Name.Trim(), BirthYear = dto.BirthYear, Pin = dto.Pin ?? "" };
        db.Children.Add(child);
        await db.SaveChangesAsync();

        var response = new ChildResponse(child.Id, child.FatherId, child.Name, child.BirthYear, child.CreatedAt, 0);
        return CreatedAtAction(nameof(Get), new { childId = child.Id }, response);
    }

    public record UpdateChildDto(string? Name, int? BirthYear, string? Pin);

    /// <summary>Ändert ein Kind (partiell).</summary>
    [HttpPatch("{childId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChildResponse>> Update(int childId, UpdateChildDto dto)
    {
        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == childId);
        if (child is null) return NotFound();

        if (dto.Name is not null) child.Name = dto.Name.Trim();
        if (dto.BirthYear.HasValue) child.BirthYear = dto.BirthYear;
        if (dto.Pin is not null) child.Pin = dto.Pin;
        await db.SaveChangesAsync();

        return (await ProjectOne(childId))!;
    }

    /// <summary>Löscht ein Kind samt aller Fächer, Kapitel, Lektionen und Punkte-Buchungen.</summary>
    [HttpDelete("{childId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int childId)
    {
        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == childId);
        if (child is null) return NotFound();
        db.Children.Remove(child);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ---- Punkte des Kindes ----

    public record PointsEntryResponse(int Id, int ChildId, int Amount, PointKind Kind, string Reason, DateTime CreatedAt);
    public record ChildPointsResponse(int ChildId, int Balance, IEnumerable<PointsEntryResponse> Entries);

    /// <summary>Punktestand des Kindes mit den letzten Buchungen.</summary>
    [HttpGet("{childId:int}/points")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChildPointsResponse>> GetPoints(int childId)
    {
        // Saldo über ALLE Buchungen (in der DB summiert) – die Liste zeigt nur die letzten 100. Sonst
        // wiche der angezeigte Punktestand ab, sobald ein Kind mehr als 100 Buchungen hat (Basis/Combo/
        // Speed + Missionen/Auszeichnungen erzeugen viele kleine Zeilen pro Sitzung).
        var balance = await db.ChildPoints
            .AsNoTracking()
            .Where(p => p.ChildId == childId)
            .SumAsync(p => (int?)p.Amount) ?? 0;

        var entries = await db.ChildPoints
            .AsNoTracking()
            .Where(p => p.ChildId == childId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(100)
            .Select(p => new PointsEntryResponse(p.Id, p.ChildId, p.Amount, p.Kind, p.Reason, p.CreatedAt))
            .ToListAsync();

        return new ChildPointsResponse(childId, balance, entries);
    }

    /// <summary>Manuelle Punkte-Buchung durch den Vater (Amount positiv = gutschreiben, negativ = abziehen).</summary>
    public record PointsEntryDto(int Amount, string Reason);

    [HttpPost("{childId:int}/points")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PointsEntryResponse>> AddPoints(int childId, PointsEntryDto dto)
    {
        var entry = new ChildPointsEntry { ChildId = childId, Kind = PointKind.Manual, Amount = dto.Amount, Reason = dto.Reason ?? "" };
        db.ChildPoints.Add(entry);
        await db.SaveChangesAsync();

        var response = new PointsEntryResponse(entry.Id, childId, entry.Amount, entry.Kind, entry.Reason, entry.CreatedAt);
        return CreatedAtAction(nameof(GetPoints), new { childId }, response);
    }
}
