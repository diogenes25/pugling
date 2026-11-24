using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// A child's homework as its supervisor sets and judges it (B-760): set with a due day, accept a hand-in (the child's coins,
/// once) or give it back with a note. A homework without a hand-in by its due day costs the child's penalty at its next
/// login - the supervisor does nothing for that.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/children/{childId:int}/homework")]
[Tags("Supervisor – Homework")]
[Produces("application/json")]
[Authorize(Roles = Roles.Supervisor)]
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class HomeworkController(PuglingDbContext db, HomeworkService homework) : ControllerBase
{
    /// <summary>
    /// The child's homework with its hand-ins, latest due day first. <c>take</c> bounds the list (no default - the list grows
    /// by a school day at a time, and a silent cut would hide an open homework).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<HomeworkResponse>>> List(int childId, [FromQuery] int? take = null,
        CancellationToken ct = default)
    {
        if (take is < 1) return this.ProblemWithCode(ApiErrors.ValidationError, "take must be at least 1.");
        return Ok(await homework.ListAsync(childId, take, ct));
    }

    /// <summary>Sets a homework. A due day already over is allowed for the record; such a homework costs no penalty.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HomeworkResponse>> Create(int childId, CreateHomeworkDto dto, CancellationToken ct)
    {
        var text = dto.Text?.Trim();
        if (string.IsNullOrEmpty(text)) return this.ProblemWithCode(ApiErrors.ValidationError, "Text is required.");
        // A missing dueOn binds to 0001-01-01: the homework would be "set after its due day" and silently never cost anything.
        if (dto.DueOn == default) return this.ProblemWithCode(ApiErrors.ValidationError, "dueOn is required.");
        if (text.Length > HomeworkService.MaxTextLength)
            return this.ProblemWithCode(ApiErrors.ValidationError, $"The text must not exceed {HomeworkService.MaxTextLength} characters.");
        if (dto.SubjectId is { } subjectId && !await db.Subjects.AnyAsync(s => s.Id == subjectId, ct))
            return this.ProblemWithCode(ApiErrors.InvalidReference, "Subject not found.");

        var created = await homework.CreateAsync(childId, dto.SubjectId, text, dto.DueOn, ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    /// <summary>Accepts the hand-in: the child's homework coins are booked once. Needs a hand-in; final.</summary>
    [HttpPost("{homeworkId:int}/accept")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HomeworkResponse>> Accept(int childId, int homeworkId, CancellationToken ct) =>
        Result(await homework.AcceptAsync(childId, homeworkId, ct));

    /// <summary>Gives the hand-in back for rework with a note; no coins, and the due day stays.</summary>
    [HttpPost("{homeworkId:int}/rework")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HomeworkResponse>> Rework(int childId, int homeworkId, ReworkHomeworkDto dto, CancellationToken ct)
    {
        var note = dto.Note?.Trim();
        if (string.IsNullOrEmpty(note)) return this.ProblemWithCode(ApiErrors.ValidationError, "Say what is missing: the note is required.");
        if (note.Length > HomeworkService.MaxNoteLength)
            return this.ProblemWithCode(ApiErrors.ValidationError, $"The note must not exceed {HomeworkService.MaxNoteLength} characters.");
        return Result(await homework.ReworkAsync(childId, homeworkId, note, ct));
    }

    /// <summary>Removes a homework; coins or a penalty already booked stay in the ledger.</summary>
    [HttpDelete("{homeworkId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int childId, int homeworkId, CancellationToken ct) =>
        await homework.DeleteAsync(childId, homeworkId, ct)
            ? NoContent()
            : this.ProblemWithCode(ApiErrors.NotFound, "Homework not found.");

    private ActionResult<HomeworkResponse> Result(HomeworkService.Outcome outcome) =>
        outcome.Error is { } error ? this.ProblemWithCode(error, outcome.Detail) : outcome.Value!;
}
