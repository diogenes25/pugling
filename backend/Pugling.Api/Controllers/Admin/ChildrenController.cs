using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Admin;

/// <summary>Verwaltung der Kinder eines Vaters, inklusive Punktestand.</summary>
[ApiController]
[Route("api/fathers/{fatherId:int}/children")]
[Tags("Admin – Children")]
[Produces("application/json")]
[Authorize(Roles = Roles.Vater)]
public class ChildrenController(PuglingDbContext db) : ControllerBase, IActionFilter
{
    /// <summary>Ein Vater darf nur seinen eigenen Teilbaum bedienen (Route-fatherId == Token-fid).</summary>
    [NonAction]
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ActionArguments.TryGetValue("fatherId", out var v) && v is int fid && User.FatherId() != fid)
            context.Result = Forbid();
    }
    [NonAction]
    public void OnActionExecuted(ActionExecutedContext context) { }

    public record ChildResponse(int Id, int FatherId, string Name, int? BirthYear, DateTime CreatedAt,
        int PointsBalance);

    Task<bool> FatherExists(int fatherId) => db.Fathers.AnyAsync(f => f.Id == fatherId);

    Task<ChildResponse?> ProjectOne(int fatherId, int childId) =>
        db.Children
            .Where(c => c.Id == childId && c.FatherId == fatherId)
            .Select(c => new ChildResponse(c.Id, c.FatherId, c.Name, c.BirthYear, c.CreatedAt,
                c.PointsEntries.Sum(p => (int?)p.Amount) ?? 0))
            .FirstOrDefaultAsync();

    /// <summary>Liste der Kinder eines Vaters.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ChildResponse>>> List(int fatherId)
    {
        if (!await FatherExists(fatherId)) return NotFound();
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
    public async Task<ActionResult<ChildResponse>> Get(int fatherId, int childId)
    {
        var child = await ProjectOne(fatherId, childId);
        return child is null ? NotFound() : child;
    }

    public record CreateChildDto(string Name, int? BirthYear, string? Pin);

    /// <summary>Erstellt ein Kind unter einem Vater.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChildResponse>> Create(int fatherId, CreateChildDto dto)
    {
        if (!await FatherExists(fatherId)) return NotFound();
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name ist erforderlich.");

        var child = new Child { FatherId = fatherId, Name = dto.Name.Trim(), BirthYear = dto.BirthYear, Pin = dto.Pin ?? "" };
        db.Children.Add(child);
        await db.SaveChangesAsync();

        var response = new ChildResponse(child.Id, fatherId, child.Name, child.BirthYear, child.CreatedAt, 0);
        return CreatedAtAction(nameof(Get), new { fatherId, childId = child.Id }, response);
    }

    public record UpdateChildDto(string? Name, int? BirthYear, string? Pin);

    /// <summary>Ändert ein Kind (partiell).</summary>
    [HttpPatch("{childId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChildResponse>> Update(int fatherId, int childId, UpdateChildDto dto)
    {
        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == childId && c.FatherId == fatherId);
        if (child is null) return NotFound();

        if (dto.Name is not null) child.Name = dto.Name.Trim();
        if (dto.BirthYear.HasValue) child.BirthYear = dto.BirthYear;
        if (dto.Pin is not null) child.Pin = dto.Pin;
        await db.SaveChangesAsync();

        return (await ProjectOne(fatherId, childId))!;
    }

    /// <summary>Löscht ein Kind samt aller Fächer, Kapitel, Lektionen und Punkte-Buchungen.</summary>
    [HttpDelete("{childId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int fatherId, int childId)
    {
        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == childId && c.FatherId == fatherId);
        if (child is null) return NotFound();
        db.Children.Remove(child);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ---- Punkte des Kindes ----

    public record PointsEntryResponse(int Id, int ChildId, int Amount, string Reason, DateTime CreatedAt);
    public record ChildPointsResponse(int ChildId, int Balance, IEnumerable<PointsEntryResponse> Entries);

    /// <summary>Punktestand des Kindes mit den letzten Buchungen.</summary>
    [HttpGet("{childId:int}/points")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChildPointsResponse>> GetPoints(int fatherId, int childId)
    {
        if (!await db.Children.AnyAsync(c => c.Id == childId && c.FatherId == fatherId)) return NotFound();

        var entries = await db.ChildPoints
            .Where(p => p.ChildId == childId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(100)
            .Select(p => new PointsEntryResponse(p.Id, p.ChildId, p.Amount, p.Reason, p.CreatedAt))
            .ToListAsync();

        return new ChildPointsResponse(childId, entries.Sum(e => e.Amount), entries);
    }

    /// <summary>Manuelle Punkte-Buchung durch den Vater (Amount positiv = gutschreiben, negativ = abziehen).</summary>
    public record PointsEntryDto(int Amount, string Reason);

    [HttpPost("{childId:int}/points")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PointsEntryResponse>> AddPoints(int fatherId, int childId, PointsEntryDto dto)
    {
        if (!await db.Children.AnyAsync(c => c.Id == childId && c.FatherId == fatherId)) return NotFound();

        var entry = new ChildPointsEntry { ChildId = childId, Amount = dto.Amount, Reason = dto.Reason ?? "" };
        db.ChildPoints.Add(entry);
        await db.SaveChangesAsync();

        var response = new PointsEntryResponse(entry.Id, childId, entry.Amount, entry.Reason, entry.CreatedAt);
        return CreatedAtAction(nameof(GetPoints), new { fatherId, childId }, response);
    }
}
