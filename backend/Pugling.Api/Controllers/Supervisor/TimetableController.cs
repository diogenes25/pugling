using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>Stundenplan eines Kindes (Fach × Wochentag) – vom Vater gepflegt, steuert Wiederholung vs. neuer Stoff.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/children/{childId:int}/timetable")]
[Tags("Study – Timetable")]
[Produces("application/json")]
[Authorize(Roles = Roles.Vater)]
public class TimetableController(PuglingDbContext db, AuthAccess access) : ControllerBase
{
    public record EntryResponse(int Id, int ChildId, int SubjectId, string SubjectName, DayOfWeek DayOfWeek, string? TimeOfDay);

    static EntryResponse Map(TimetableEntry t) =>
        new(t.Id, t.ChildId, t.SubjectId, t.Subject!.Name, t.DayOfWeek, t.TimeOfDay);

    /// <summary>Stundenplan des Kindes.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<EntryResponse>>> List(int childId)
    {
        if (!await access.FatherOwnsChildAsync(User, childId)) return Forbid();
        var entries = await db.Timetable.Include(t => t.Subject)
            .Where(t => t.ChildId == childId)
            .OrderBy(t => t.DayOfWeek).ThenBy(t => t.Subject!.Name)
            .ToListAsync();
        return entries.Select(Map).ToList();
    }

    public record CreateEntryDto(int SubjectId, DayOfWeek DayOfWeek, string? TimeOfDay);

    /// <summary>Trägt ein Fach an einem Wochentag ein.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EntryResponse>> Create(int childId, CreateEntryDto dto)
    {
        if (!await access.FatherOwnsChildAsync(User, childId)) return Forbid();
        if (!await db.Subjects.AnyAsync(s => s.Id == dto.SubjectId)) return this.ProblemWithCode(ApiErrors.InvalidReference, "Subject not found.");
        if (await db.Timetable.AnyAsync(t => t.ChildId == childId && t.SubjectId == dto.SubjectId && t.DayOfWeek == dto.DayOfWeek))
            return this.ProblemWithCode(ApiErrors.TimetableSlotTaken, "This subject is already scheduled on this weekday.");

        var entry = new TimetableEntry { ChildId = childId, SubjectId = dto.SubjectId, DayOfWeek = dto.DayOfWeek, TimeOfDay = dto.TimeOfDay };
        db.Timetable.Add(entry);
        await db.SaveChangesAsync();
        await db.Entry(entry).Reference(t => t.Subject).LoadAsync();
        return CreatedAtAction(nameof(List), new { childId }, Map(entry));
    }

    /// <summary>Entfernt einen Stundenplan-Eintrag.</summary>
    [HttpDelete("{entryId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int childId, int entryId)
    {
        if (!await access.FatherOwnsChildAsync(User, childId)) return Forbid();
        var entry = await db.Timetable.FirstOrDefaultAsync(t => t.Id == entryId && t.ChildId == childId);
        if (entry is null) return NotFound();
        db.Timetable.Remove(entry);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
