using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;
using Pugling.Api.Services;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// Verwaltung der Kinder des angemeldeten Vaters, inklusive Punktestand. Der Vater ergibt sich aus
/// dem JWT (<c>fid</c>); kindbezogene Endpunkte sichert der <see cref="ChildOwnershipFilter"/> ab.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/children")]
[Tags("Admin – Children")]
[Produces("application/json")]
[Authorize(Roles = Roles.Vater)]
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class ChildrenController(PuglingDbContext db, WalletService wallet, AccountService accounts) : ControllerBase
{
    public record ChildResponse(int Id, int FatherId, string Name, int? BirthYear, int? Grade,
        SchoolTypes SchoolType, DateTime CreatedAt, int Coins, int Gems);

    Task<ChildResponse?> ProjectOne(int childId) =>
        db.Children
            .Where(c => c.Id == childId)
            .Select(c => new ChildResponse(c.Id, c.FatherId, c.Name, c.BirthYear, c.Grade, c.SchoolType,
                c.CreatedAt,
                c.PointsEntries.Where(p => PointKindCurrency.CoinKinds.Contains(p.Kind)).Sum(p => (int?)p.Amount) ?? 0,
                c.PointsEntries.Where(p => PointKindCurrency.GemKinds.Contains(p.Kind)).Sum(p => (int?)p.Amount) ?? 0))
            .FirstOrDefaultAsync();

    /// <summary>Liste der eigenen Kinder.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ChildResponse>>> List()
    {
        var fatherId = User.FatherId();
        return await db.Children
            .Where(c => c.FatherId == fatherId)
            .OrderBy(c => c.Name)
            .Select(c => new ChildResponse(c.Id, c.FatherId, c.Name, c.BirthYear, c.Grade, c.SchoolType,
                c.CreatedAt,
                c.PointsEntries.Where(p => PointKindCurrency.CoinKinds.Contains(p.Kind)).Sum(p => (int?)p.Amount) ?? 0,
                c.PointsEntries.Where(p => PointKindCurrency.GemKinds.Contains(p.Kind)).Sum(p => (int?)p.Amount) ?? 0))
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

    public record CreateChildDto(string Name, int? BirthYear, int? Grade, SchoolTypes? SchoolType, string? Pin);

    /// <summary>Erstellt ein Kind unter dem angemeldeten Vater.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChildResponse>> Create(CreateChildDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return this.ProblemWithCode(ApiErrors.ValidationError, "Name is required.");

        var child = new Child
        {
            FatherId = User.FatherId()!.Value,
            Name = dto.Name.Trim(),
            BirthYear = dto.BirthYear,
            Grade = dto.Grade,
            SchoolType = dto.SchoolType ?? SchoolTypes.None,
            Pin = string.IsNullOrEmpty(dto.Pin) ? "" : PinHasher.Hash(dto.Pin),
        };
        db.Children.Add(child);
        await db.SaveChangesAsync();
        // Login-Konto (Student) sofort anlegen, damit sich das neue Kind einloggen kann.
        await accounts.EnsureForChildAsync(child);

        var response = new ChildResponse(child.Id, child.FatherId, child.Name, child.BirthYear, child.Grade,
            child.SchoolType, child.CreatedAt, 0, 0);
        return CreatedAtAction(nameof(Get), new { childId = child.Id }, response);
    }

    public record UpdateChildDto(string? Name, int? BirthYear, int? Grade, SchoolTypes? SchoolType, string? Pin);

    /// <summary>Ändert ein Kind (partiell).</summary>
    [HttpPatch("{childId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChildResponse>> Update(int childId, UpdateChildDto dto)
    {
        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == childId);
        if (child is null) return NotFound();

        if (dto.Name is not null) child.Name = dto.Name.Trim();
        if (dto.BirthYear.HasValue) child.BirthYear = dto.BirthYear;
        if (dto.Grade.HasValue) child.Grade = dto.Grade;
        if (dto.SchoolType.HasValue) child.SchoolType = dto.SchoolType.Value;
        if (dto.Pin is not null)
        {
            child.Pin = string.IsNullOrEmpty(dto.Pin) ? "" : PinHasher.Hash(dto.Pin);
            // PIN-Hash auf das Login-Konto spiegeln (konto-zentrischer Login /auth/login bleibt synchron).
            (await accounts.EnsureForChildAsync(child)).PinHash = child.Pin;
        }
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
    public record ChildPointsResponse(int ChildId, int Coins, int Gems, IEnumerable<PointsEntryResponse> Entries);

    /// <summary>Kontostand des Kindes (Münzen + Gems) mit den letzten Buchungen (neueste zuerst).</summary>
    /// <param name="childId">Kind, dessen Kontostand gelesen wird.</param>
    /// <param name="skip">Anzahl zu überspringender Buchungen (Paging).</param>
    /// <param name="take">Maximale Buchungszahl (1..500). Gesamtzahl im Header <c>X-Total-Count</c>.</param>
    [HttpGet("{childId:int}/points")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChildPointsResponse>> GetPoints(
        int childId, [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake)
    {
        // Saldo je Währung über ALLE Buchungen (in der DB summiert) – die Liste ist seitenweise (Default 100).
        // Sonst wiche der angezeigte Kontostand von der Seite ab, sobald ein Kind mehr Buchungen hat als eine
        // Seite fasst (Basis/Combo/Speed + Missionen/Auszeichnungen erzeugen viele kleine Zeilen pro Sitzung).
        var (coins, gems) = await wallet.BalancesAsync(childId);

        var entries = await db.ChildPoints
            .AsNoTracking()
            .Where(p => p.ChildId == childId)
            .OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
            .Select(p => new PointsEntryResponse(p.Id, p.ChildId, p.Amount, p.Kind, p.Reason, p.CreatedAt))
            .ToPagedListAsync(Response, skip, take);

        return new ChildPointsResponse(childId, coins, gems, entries);
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
