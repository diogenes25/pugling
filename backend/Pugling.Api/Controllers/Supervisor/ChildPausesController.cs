using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// A child's pauses (B-731): days from a date to a date, also entered after the fact for today, on which no duty is owed -
/// no penalty, and the streak neither counts nor breaks. Maintained by the supervisor only; the child cannot pause itself.
/// A penalty booked before the pause was entered stays (decision: the supervisor gives it back by hand).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/children/{childId:int}/pauses")]
[Tags("Supervisor – Pauses")]
[Produces("application/json")]
[Authorize(Roles = Roles.Supervisor)]
// Someone else's child answers 404 through the shared filter, like every child-scoped controller.
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class ChildPausesController(PuglingDbContext db, TimeProvider time) : ControllerBase
{
    private const int MaxNoteLength = 200;

    private static ChildPauseResponse Map(ChildPause p) => new(p.Id, p.From, p.Until, p.Note, p.CreatedAt);

    /// <summary>The child's pauses, latest first.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ChildPauseResponse>>> List(int childId, CancellationToken ct = default)
    {
        var pauses = await db.ChildPauses.AsNoTracking().Where(p => p.ChildId == childId)
            .OrderByDescending(p => p.From).ThenByDescending(p => p.Id)
            .ToListAsync(ct);
        return pauses.Select(Map).ToList();
    }

    /// <summary>Enters a pause; both days count as paused. Overlapping pauses are allowed - a day paused twice is paused.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChildPauseResponse>> Create(int childId, CreateChildPauseDto dto, CancellationToken ct = default)
    {
        if (dto.From > dto.Until)
            return this.ProblemWithCode(ApiErrors.ValidationError, "A pause cannot end before it starts: 'until' is before 'from'.");
        var note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim();
        if (note?.Length > MaxNoteLength)
            return this.ProblemWithCode(ApiErrors.ValidationError, $"The note may have at most {MaxNoteLength} characters.");

        var pause = new ChildPause
        {
            ChildId = childId,
            From = dto.From,
            Until = dto.Until,
            Note = note,
            CreatedAt = time.GetUtcNow().UtcDateTime,
        };
        db.ChildPauses.Add(pause);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { childId }, Map(pause));
    }

    /// <summary>Removes a pause; its days owe their duty again from the next settlement on.</summary>
    [HttpDelete("{pauseId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int childId, int pauseId, CancellationToken ct = default)
    {
        var pause = await db.ChildPauses.FirstOrDefaultAsync(p => p.Id == pauseId && p.ChildId == childId, ct);
        if (pause is null) return NotFound();
        db.ChildPauses.Remove(pause);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
