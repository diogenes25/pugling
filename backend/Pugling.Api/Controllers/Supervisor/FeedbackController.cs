using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// The children's feedback on cards as the supervisor sees it (B-628, B-623 decision 7): every supervisor of a child sees
/// all of it - child, exercise, card with its solution, the child's answer and text - and marks it done for all of them.
/// A supervisor of other children sees nothing.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/feedback")]
[Tags("Supervisor – Feedback")]
[Produces("application/json")]
[Authorize(Roles = Roles.Supervisor)]
public class FeedbackController(PuglingDbContext db, ExerciseFeedbackService feedback, AuthAccess access) : ControllerBase
{
    /// <summary>
    /// The feedback of the caller's children, open first and newest report first; filtered by <c>childId</c> (one of the
    /// caller's children, else 404) and <c>status</c>; <c>take</c> bounds the list (no default - a family's list is short,
    /// and a silent cut would hide reports).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<FeedbackResponse>>> List([FromQuery] int? childId = null,
        [FromQuery] FeedbackStatus? status = null, [FromQuery] int? take = null, CancellationToken ct = default)
    {
        if (take is < 1) return this.ProblemWithCode(ApiErrors.ValidationError, "take must be at least 1.");
        var adultId = User.AdultId();
        var children = await db.SupervisorLinks.Where(l => l.SupervisorId == adultId)
            .Select(l => l.StudentId).ToListAsync(ct);
        if (childId is { } only)
        {
            if (!children.Contains(only)) return this.ProblemWithCode(ApiErrors.NotFound, "Child not found.");
            children = [only];
        }
        return Ok(await feedback.ListAsync(children, status, take, ct));
    }

    /// <summary>
    /// Marks a feedback done - for every supervisor of the child, and the child gets a notice once - or open again.
    /// </summary>
    [HttpPatch("{feedbackId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeedbackResponse>> Update(int feedbackId, UpdateFeedbackDto dto, CancellationToken ct)
    {
        if (!Enum.IsDefined(dto.Status)) return this.ProblemWithCode(ApiErrors.ValidationError, "Unknown status.");
        var entry = await db.ExerciseFeedbacks.FirstOrDefaultAsync(f => f.Id == feedbackId, ct);
        // Somebody else's child answers like an unknown id: the response must not tell which ids exist.
        if (entry is null || !await access.SupervisorOwnsChildAsync(User, entry.ChildId, ct))
            return this.ProblemWithCode(ApiErrors.NotFound, "Feedback not found.");

        await feedback.SetStatusAsync(entry, dto.Status, User.AdultId(), ct);
        return (await feedback.ListAsync([entry.ChildId], null, null, ct)).Single(f => f.Id == entry.Id);
    }
}
