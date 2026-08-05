using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// Management of the logged-in father's children, including point balance. The father is derived from
/// the JWT (<c>fid</c>); child-related endpoints are secured by the <see cref="ChildOwnershipFilter"/>.
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

    /// <summary>List of students supervised by the logged-in supervisor.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ChildResponse>>> List(CancellationToken ct = default)
    {
        var supervisorId = User.AdultId();
        return await db.Children
            .Where(c => c.SupervisorLinks.Any(l => l.SupervisorId == supervisorId))
            .OrderBy(c => c.Name)
            .Select(c => new ChildResponse(c.Id, c.Name, c.BirthYear, c.Grade, c.SchoolType,
                c.Gender, c.Interests, c.ProfileNotes, c.AllowedContentRating,
                c.CreatedAt,
                c.PointsEntries.Where(p => PointKindCurrency.CoinKinds.Contains(p.Kind)).Sum(p => (int?)p.Amount) ?? 0,
                c.PointsEntries.Where(p => PointKindCurrency.GemKinds.Contains(p.Kind)).Sum(p => (int?)p.Amount) ?? 0))
            .ToListAsync(ct);
    }

    /// <summary>A single child.</summary>
    [HttpGet("{childId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChildResponse>> Get(int childId, CancellationToken ct = default)
    {
        var child = await ProjectOne(childId, ct);
        return child is null ? NotFound() : child;
    }

    /// <summary>Creates a child under the logged-in father.</summary>
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
            // With nothing given, the strictest level - releasing images must be a deliberate supervisor choice.
            AllowedContentRating = dto.AllowedContentRating ?? ContentRating.Everyone,
            Pin = string.IsNullOrEmpty(dto.Pin) ? "" : PinHasher.Hash(dto.Pin),
        };
        db.Children.Add(child);
        // Establish supervision by the creating supervisor (a student can get more later) - in the SAME commit
        // as the child: if the request broke between two SaveChanges (client gone, connection dead), a child
        // without a link would remain, and that is reachable for nobody (List filters through the links, every
        // single access runs through ChildOwnershipFilter → 404). EF fills the StudentId from the navigation,
        // hence no second pass for the id.
        child.SupervisorLinks.Add(new SupervisorLink { SupervisorId = User.AdultId()!.Value });
        await db.SaveChangesAsync(ct);
        // Create the login account (student) right away so the new child can log in.
        await accounts.EnsureForChildAsync(child, ct);

        var response = new ChildResponse(child.Id, child.Name, child.BirthYear, child.Grade,
            child.SchoolType, child.Gender, child.Interests, child.ProfileNotes,
            child.AllowedContentRating, child.CreatedAt, 0, 0);
        return CreatedAtAction(nameof(Get), new { childId = child.Id }, response);
    }

    /// <summary>Changes a child (partial).</summary>
    [HttpPatch("{childId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChildResponse>> Update(int childId, UpdateChildDto dto, CancellationToken ct = default)
    {
        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == childId, ct);
        if (child is null) return NotFound();

        if (dto.Name is not null) child.Name = dto.Name.Trim();
        // Value first, clear switch second - "clear" wins if a client sends both.
        if (dto.BirthYear.HasValue) child.BirthYear = dto.BirthYear;
        if (dto.ClearBirthYear) child.BirthYear = null;
        if (dto.Grade.HasValue) child.Grade = dto.Grade;
        if (dto.ClearGrade) child.Grade = null;
        if (dto.SchoolType.HasValue) child.SchoolType = dto.SchoolType.Value;
        if (dto.Gender.HasValue) child.Gender = dto.Gender.Value;
        // Assign a new list (no in-place mutation - the JSON column pitfall).
        if (dto.Interests is not null) child.Interests = [.. dto.Interests];
        if (dto.ProfileNotes is not null) child.ProfileNotes = dto.ProfileNotes;
        if (dto.AllowedContentRating.HasValue) child.AllowedContentRating = dto.AllowedContentRating.Value;
        if (dto.Pin is not null) child.Pin = string.IsNullOrEmpty(dto.Pin) ? "" : PinHasher.Hash(dto.Pin);
        // Mirror name and PIN hash onto the login account - in the SAME commit. Only the PIN used to travel
        // along, and a renamed child was still greeted with the old name after the next login (the display
        // name comes from the account). See AccountService.MirrorAsync.
        await accounts.MirrorAsync(child, ct);
        await db.SaveChangesAsync(ct);

        return (await ProjectOne(childId, ct))!;
    }

    /// <summary>Deletes a child together with all subjects, chapters, lessons and point ledger entries.</summary>
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

    // ---- Co-supervisors (several supervisors per student) ----

    /// <summary>All supervisors of this student (the acting supervisor must be one themself).</summary>
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
    /// Adds another supervisor to the student (e.g. mother/grandmother). The acting supervisor
    /// must already supervise the student (<see cref="ChildOwnershipFilter"/>); the new supervisor must exist.
    /// Idempotent: an existing supervision link is not duplicated - then answers <c>200</c> with the stored
    /// link (its own relation, not the caller's), <c>201</c> only on a real insert.
    /// </summary>
    [HttpPost("{childId:int}/supervisors")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupervisorLinkResponse>> AddSupervisor(int childId, AddSupervisorDto dto, CancellationToken ct = default)
    {
        var supervisor = await db.Adults.FirstOrDefaultAsync(f => f.Id == dto.SupervisorId, ct);
        if (supervisor is null) return this.ProblemWithCode(ApiErrors.InvalidReference, "Supervisor not found.");

        var existing = await db.SupervisorLinks.AsNoTracking()
            .FirstOrDefaultAsync(l => l.StudentId == childId && l.SupervisorId == dto.SupervisorId, ct);
        if (existing is not null)
            return Ok(new SupervisorLinkResponse(supervisor.Id, supervisor.Name, existing.Relation, existing.CreatedAt));

        var link = new SupervisorLink { StudentId = childId, SupervisorId = dto.SupervisorId, Relation = dto.Relation };
        db.SupervisorLinks.Add(link);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Supervisors), new { childId },
            new SupervisorLinkResponse(supervisor.Id, supervisor.Name, link.Relation, link.CreatedAt));
    }

    /// <summary>Removes a supervision link. The last supervisor cannot be removed (the student would be orphaned).</summary>
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

    // ---- The child's points ----

    /// <summary>Wallet balance of the child (coins + gems) with the latest ledger entries (newest first).</summary>
    /// <param name="childId">Child whose wallet balance is being read.</param>
    /// <param name="skip">Number of ledger entries to skip (paging).</param>
    /// <param name="take">Maximum number of ledger entries (1..500). Total count in the <c>X-Total-Count</c> header.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("{childId:int}/points")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChildPointsResponse>> GetPoints(
        int childId, [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake,
        CancellationToken ct = default)
    {
        // Balance per currency over ALL entries (summed in the DB) - the list itself is paged (default 100).
        // Otherwise the displayed balance would differ from the page as soon as a child has more entries than
        // one page holds (base/combo/speed + missions/awards create many small rows per session).
        var (coins, gems) = await wallet.BalancesAsync(childId, ct);

        var entries = await db.ChildPointsEntries
            .AsNoTracking()
            .Where(p => p.ChildId == childId)
            .OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
            .Select(p => new PointsEntryResponse(p.Id, p.ChildId, p.Amount, p.Kind, p.Reason, p.CreatedAt))
            .ToPagedListAsync(Response, skip, take, ct);

        return new ChildPointsResponse(childId, coins, gems, entries);
    }

    /// <summary>
    /// Books a manual point credit or debit (gifting/deducting outside of the shop and
    /// goal penalty). The currency determines the <see cref="PointKind"/>: coins → <see cref="PointKind.Manual"/>,
    /// gems → <see cref="PointKind.ManualGems"/>.
    /// </summary>
    [HttpPost("{childId:int}/points")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PointsEntryResponse>> AddPoints(int childId, PointsEntryDto dto, CancellationToken ct = default)
    {
        // Currency → point kind: gems through the manual twin, otherwise the classic manual coin entry.
        var kind = dto.Currency == Currency.Gems ? PointKind.ManualGems : PointKind.Manual;
        var entry = new ChildPointsEntry { ChildId = childId, Kind = kind, Amount = dto.Amount, Reason = dto.Reason ?? "" };
        db.ChildPointsEntries.Add(entry);
        await db.SaveChangesAsync(ct);

        var response = new PointsEntryResponse(entry.Id, childId, entry.Amount, entry.Kind, entry.Reason, entry.CreatedAt);
        return CreatedAtAction(nameof(GetPoints), new { childId }, response);
    }
}
