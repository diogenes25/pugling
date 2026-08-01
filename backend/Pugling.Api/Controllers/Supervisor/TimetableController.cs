using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>Timetable of a child (subject × weekday) – maintained by the father, controls review vs. new material.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/children/{childId:int}/timetable")]
[Tags("Supervisor – Timetable")]
[Produces("application/json")]
[Authorize(Roles = Roles.Supervisor)]
// Ownership through the shared filter instead of inline per action (CLAUDE.md: "do not repeat it inline").
// Side effect and intent: on someone else's child the filter answers **404** instead of 403 - like every
// other child-scoped controller, so other people's child ids cannot be enumerated through status codes.
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class TimetableController(PuglingDbContext db) : ControllerBase
{
    static EntryResponse Map(TimetableEntry t) =>
        new(t.Id, t.ChildId, t.SubjectId, t.Subject!.Name, t.DayOfWeek, t.TimeOfDay);

    /// <summary>Timetable of the child.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<EntryResponse>>> List(int childId, CancellationToken ct = default)
    {
        var entries = await db.TimetableEntries.AsNoTracking().Include(t => t.Subject)
            .Where(t => t.ChildId == childId)
            .OrderBy(t => t.DayOfWeek).ThenBy(t => t.Subject!.Name)
            .ToListAsync(ct);
        return entries.Select(Map).ToList();
    }

    /// <summary>Registers a subject on a weekday.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EntryResponse>> Create(int childId, CreateEntryDto dto, CancellationToken ct = default)
    {
        if (!await db.Subjects.AnyAsync(s => s.Id == dto.SubjectId, ct)) return this.ProblemWithCode(ApiErrors.InvalidReference, "Subject not found.");
        if (await db.TimetableEntries.AnyAsync(t => t.ChildId == childId && t.SubjectId == dto.SubjectId && t.DayOfWeek == dto.DayOfWeek, ct))
            return this.ProblemWithCode(ApiErrors.TimetableSlotTaken, "This subject is already scheduled on this weekday.");

        var entry = new TimetableEntry { ChildId = childId, SubjectId = dto.SubjectId, DayOfWeek = dto.DayOfWeek, TimeOfDay = dto.TimeOfDay };
        db.TimetableEntries.Add(entry);
        await db.SaveChangesAsync(ct);
        await db.Entry(entry).Reference(t => t.Subject).LoadAsync(ct);
        return CreatedAtAction(nameof(List), new { childId }, Map(entry));
    }

    /// <summary>Removes a timetable entry.</summary>
    [HttpDelete("{entryId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int childId, int entryId, CancellationToken ct = default)
    {
        var entry = await db.TimetableEntries.FirstOrDefaultAsync(t => t.Id == entryId && t.ChildId == childId, ct);
        if (entry is null) return NotFound();
        db.TimetableEntries.Remove(entry);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
