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
[Tags("Supervisor – Timetable")]
[Produces("application/json")]
[Authorize(Roles = Roles.Supervisor)]
// Eigentum über den geteilten Filter statt inline je Action (CLAUDE.md: „nicht inline wiederholen").
// Nebeneffekt und Absicht: der Filter antwortet auf ein fremdes Kind mit **404** statt 403 – so wie jeder
// andere kindbezogene Controller, damit sich fremde Kind-Ids nicht durch die Statuscodes enumerieren lassen.
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class TimetableController(PuglingDbContext db) : ControllerBase
{
    static EntryResponse Map(TimetableEntry t) =>
        new(t.Id, t.ChildId, t.SubjectId, t.Subject!.Name, t.DayOfWeek, t.TimeOfDay);

    /// <summary>Stundenplan des Kindes.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<EntryResponse>>> List(int childId)
    {
        var entries = await db.Timetable.AsNoTracking().Include(t => t.Subject)
            .Where(t => t.ChildId == childId)
            .OrderBy(t => t.DayOfWeek).ThenBy(t => t.Subject!.Name)
            .ToListAsync();
        return entries.Select(Map).ToList();
    }

    /// <summary>Trägt ein Fach an einem Wochentag ein.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EntryResponse>> Create(int childId, CreateEntryDto dto)
    {
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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int childId, int entryId)
    {
        var entry = await db.Timetable.FirstOrDefaultAsync(t => t.Id == entryId && t.ChildId == childId);
        if (entry is null) return NotFound();
        db.Timetable.Remove(entry);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
