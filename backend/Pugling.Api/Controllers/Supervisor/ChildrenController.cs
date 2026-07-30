using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// Verwaltung der Kinder des angemeldeten Vaters, inklusive Punktestand. Der Vater ergibt sich aus
/// dem JWT (<c>fid</c>); kindbezogene Endpunkte sichert der <see cref="ChildOwnershipFilter"/> ab.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/children")]
[Tags("Supervisor – Children")]
[Produces("application/json")]
[Authorize(Roles = Roles.Supervisor)]
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class ChildrenController(PuglingDbContext db, WalletService wallet, AccountService accounts) : ControllerBase
{
    Task<ChildResponse?> ProjectOne(int childId, CancellationToken ct) =>
        db.Children
            .Where(c => c.Id == childId)
            .Select(c => new ChildResponse(c.Id, c.Name, c.BirthYear, c.Grade, c.SchoolType,
                c.Gender, c.Interests, c.ProfileNotes, c.AllowedContentRating,
                c.CreatedAt,
                c.PointsEntries.Where(p => PointKindCurrency.CoinKinds.Contains(p.Kind)).Sum(p => (int?)p.Amount) ?? 0,
                c.PointsEntries.Where(p => PointKindCurrency.GemKinds.Contains(p.Kind)).Sum(p => (int?)p.Amount) ?? 0))
            .FirstOrDefaultAsync(ct);

    /// <summary>Liste der vom angemeldeten Supervisor betreuten Studenten.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ChildResponse>>> List(CancellationToken ct = default)
    {
        var fatherId = User.AdultId();
        return await db.Children
            .Where(c => c.SupervisorLinks.Any(l => l.SupervisorId == fatherId))
            .OrderBy(c => c.Name)
            .Select(c => new ChildResponse(c.Id, c.Name, c.BirthYear, c.Grade, c.SchoolType,
                c.Gender, c.Interests, c.ProfileNotes, c.AllowedContentRating,
                c.CreatedAt,
                c.PointsEntries.Where(p => PointKindCurrency.CoinKinds.Contains(p.Kind)).Sum(p => (int?)p.Amount) ?? 0,
                c.PointsEntries.Where(p => PointKindCurrency.GemKinds.Contains(p.Kind)).Sum(p => (int?)p.Amount) ?? 0))
            .ToListAsync(ct);
    }

    /// <summary>Ein einzelnes Kind.</summary>
    [HttpGet("{childId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChildResponse>> Get(int childId, CancellationToken ct = default)
    {
        var child = await ProjectOne(childId, ct);
        return child is null ? NotFound() : child;
    }

    /// <summary>Erstellt ein Kind unter dem angemeldeten Vater.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChildResponse>> Create(CreateChildDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return this.ProblemWithCode(ApiErrors.ValidationError, "Name is required.");

        var child = new Child
        {
            Name = dto.Name.Trim(),
            BirthYear = dto.BirthYear,
            Grade = dto.Grade,
            SchoolType = dto.SchoolType ?? SchoolTypes.None,
            Gender = dto.Gender ?? Gender.None,
            Interests = dto.Interests ?? [],
            ProfileNotes = dto.ProfileNotes,
            // Ohne Angabe die strengste Stufe – eine Bild-Freigabe muss der Supervisor bewusst setzen.
            AllowedContentRating = dto.AllowedContentRating ?? ContentRating.Everyone,
            Pin = string.IsNullOrEmpty(dto.Pin) ? "" : PinHasher.Hash(dto.Pin),
        };
        db.Children.Add(child);
        // Betreuung durch den anlegenden Supervisor herstellen (ein Student kann später weitere bekommen)
        // – im SELBEN Commit wie das Kind: bräche der Request zwischen zwei SaveChanges ab (Client weg,
        // Verbindung tot), bliebe ein Kind ohne Link zurück, und das ist für niemanden mehr erreichbar
        // (List filtert über die Links, jeder Einzelzugriff läuft über ChildOwnershipFilter → 404).
        // Die StudentId füllt EF aus der Navigation, darum kein zweiter Durchgang für die Id.
        child.SupervisorLinks.Add(new SupervisorLink { SupervisorId = User.AdultId()!.Value });
        await db.SaveChangesAsync(ct);
        // Login-Konto (Student) sofort anlegen, damit sich das neue Kind einloggen kann.
        await accounts.EnsureForChildAsync(child, ct);

        var response = new ChildResponse(child.Id, child.Name, child.BirthYear, child.Grade,
            child.SchoolType, child.Gender, child.Interests, child.ProfileNotes,
            child.AllowedContentRating, child.CreatedAt, 0, 0);
        return CreatedAtAction(nameof(Get), new { childId = child.Id }, response);
    }

    /// <summary>Ändert ein Kind (partiell).</summary>
    [HttpPatch("{childId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChildResponse>> Update(int childId, UpdateChildDto dto, CancellationToken ct = default)
    {
        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == childId, ct);
        if (child is null) return NotFound();

        if (dto.Name is not null) child.Name = dto.Name.Trim();
        // Wert zuerst, Clear-Schalter danach – „leeren" gewinnt, falls ein Client beides schickt.
        if (dto.BirthYear.HasValue) child.BirthYear = dto.BirthYear;
        if (dto.ClearBirthYear) child.BirthYear = null;
        if (dto.Grade.HasValue) child.Grade = dto.Grade;
        if (dto.ClearGrade) child.Grade = null;
        if (dto.SchoolType.HasValue) child.SchoolType = dto.SchoolType.Value;
        if (dto.Gender.HasValue) child.Gender = dto.Gender.Value;
        // Neue Liste zuweisen (kein In-Place-Mutieren – JSON-Spalten-Fallstrick).
        if (dto.Interests is not null) child.Interests = [.. dto.Interests];
        if (dto.ProfileNotes is not null) child.ProfileNotes = dto.ProfileNotes;
        if (dto.AllowedContentRating.HasValue) child.AllowedContentRating = dto.AllowedContentRating.Value;
        if (dto.Pin is not null) child.Pin = string.IsNullOrEmpty(dto.Pin) ? "" : PinHasher.Hash(dto.Pin);
        // Name und PIN-Hash aufs Login-Konto spiegeln – im SELBEN Commit. Vorher ging nur die PIN mit, und
        // ein umbenanntes Kind wurde nach dem nächsten Anmelden weiter mit dem alten Namen begrüßt (der
        // Anzeigename kommt vom Konto). Siehe AccountService.MirrorAsync.
        await accounts.MirrorAsync(child, ct);
        await db.SaveChangesAsync(ct);

        return (await ProjectOne(childId, ct))!;
    }

    /// <summary>Löscht ein Kind samt aller Fächer, Kapitel, Lektionen und Punkte-Buchungen.</summary>
    [HttpDelete("{childId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int childId, CancellationToken ct = default)
    {
        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == childId, ct);
        if (child is null) return NotFound();
        db.Children.Remove(child);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ---- Ko-Supervisoren (mehrere Betreuer je Student) ----

    /// <summary>Alle Supervisor dieses Studenten (der handelnde Supervisor muss selbst einer sein).</summary>
    [HttpGet("{childId:int}/supervisors")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<SupervisorLinkResponse>>> Supervisors(int childId, CancellationToken ct = default) =>
        await db.SupervisorLinks.AsNoTracking()
            .Where(l => l.StudentId == childId)
            .OrderBy(l => l.CreatedAt)
            .Select(l => new SupervisorLinkResponse(l.SupervisorId, l.Supervisor!.Name, l.Relation, l.CreatedAt))
            .ToListAsync(ct);

    /// <summary>
    /// Fügt dem Studenten einen weiteren Supervisor hinzu (z. B. Mutter/Oma). Der handelnde Supervisor
    /// muss den Studenten bereits betreuen (<see cref="ChildOwnershipFilter"/>); der neue Supervisor muss existieren.
    /// Idempotent: eine bestehende Betreuung wird nicht dupliziert.
    /// </summary>
    [HttpPost("{childId:int}/supervisors")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupervisorLinkResponse>> AddSupervisor(int childId, AddSupervisorDto dto, CancellationToken ct = default)
    {
        var supervisor = await db.Adults.FirstOrDefaultAsync(f => f.Id == dto.SupervisorId, ct);
        if (supervisor is null) return this.ProblemWithCode(ApiErrors.InvalidReference, "Supervisor not found.");

        if (!await db.SupervisorLinks.AnyAsync(l => l.StudentId == childId && l.SupervisorId == dto.SupervisorId, ct))
        {
            db.SupervisorLinks.Add(new SupervisorLink { StudentId = childId, SupervisorId = dto.SupervisorId, Relation = dto.Relation });
            await db.SaveChangesAsync(ct);
        }
        return CreatedAtAction(nameof(Supervisors), new { childId },
            new SupervisorLinkResponse(supervisor.Id, supervisor.Name, dto.Relation, DateTime.UtcNow));
    }

    /// <summary>Entfernt eine Betreuung. Der letzte Supervisor kann nicht entfernt werden (Student wäre verwaist).</summary>
    [HttpDelete("{childId:int}/supervisors/{supervisorId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveSupervisor(int childId, int supervisorId, CancellationToken ct = default)
    {
        var link = await db.SupervisorLinks.FirstOrDefaultAsync(l => l.StudentId == childId && l.SupervisorId == supervisorId, ct);
        if (link is null) return NotFound();
        if (await db.SupervisorLinks.CountAsync(l => l.StudentId == childId, ct) <= 1)
            return this.ProblemWithCode(ApiErrors.ValidationError, "Cannot remove the last supervisor of a student.");
        db.SupervisorLinks.Remove(link);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ---- Punkte des Kindes ----

    /// <summary>Kontostand des Kindes (Münzen + Gems) mit den letzten Buchungen (neueste zuerst).</summary>
    /// <param name="childId">Kind, dessen Kontostand gelesen wird.</param>
    /// <param name="skip">Anzahl zu überspringender Buchungen (Paging).</param>
    /// <param name="take">Maximale Buchungszahl (1..500). Gesamtzahl im Header <c>X-Total-Count</c>.</param>
    /// <param name="ct">Abbruch-Token.</param>
    [HttpGet("{childId:int}/points")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChildPointsResponse>> GetPoints(
        int childId, [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake,
        CancellationToken ct = default)
    {
        // Saldo je Währung über ALLE Buchungen (in der DB summiert) – die Liste ist seitenweise (Default 100).
        // Sonst wiche der angezeigte Kontostand von der Seite ab, sobald ein Kind mehr Buchungen hat als eine
        // Seite fasst (Basis/Combo/Speed + Missionen/Auszeichnungen erzeugen viele kleine Zeilen pro Sitzung).
        var (coins, gems) = await wallet.BalancesAsync(childId, ct);

        var entries = await db.ChildPoints
            .AsNoTracking()
            .Where(p => p.ChildId == childId)
            .OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
            .Select(p => new PointsEntryResponse(p.Id, p.ChildId, p.Amount, p.Kind, p.Reason, p.CreatedAt))
            .ToPagedListAsync(Response, skip, take, ct);

        return new ChildPointsResponse(childId, coins, gems, entries);
    }

    /// <summary>
    /// Bucht eine manuelle Punktegutschrift oder -belastung (Verschenken/Abziehen außerhalb von Shop und
    /// Ziel-Malus). Die Währung bestimmt das <see cref="PointKind"/>: Coins → <see cref="PointKind.Manual"/>,
    /// Gems → <see cref="PointKind.ManualGems"/>.
    /// </summary>
    [HttpPost("{childId:int}/points")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PointsEntryResponse>> AddPoints(int childId, PointsEntryDto dto, CancellationToken ct = default)
    {
        // Währung → Buchungs-Kind: Gems über den Manual-Zwilling, sonst die klassische Münz-Manualbuchung.
        var kind = dto.Currency == Currency.Gems ? PointKind.ManualGems : PointKind.Manual;
        var entry = new ChildPointsEntry { ChildId = childId, Kind = kind, Amount = dto.Amount, Reason = dto.Reason ?? "" };
        db.ChildPoints.Add(entry);
        await db.SaveChangesAsync(ct);

        var response = new PointsEntryResponse(entry.Id, childId, entry.Amount, entry.Kind, entry.Reason, entry.CreatedAt);
        return CreatedAtAction(nameof(GetPoints), new { childId }, response);
    }
}
