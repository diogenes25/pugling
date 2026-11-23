using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;

namespace Pugling.Api.Controllers.Student;

/// <summary>
/// The child reports a card or an exercise of a position (B-628): a few fixed reasons and an optional text. The report
/// changes nothing about the game - no box, points, duty, penalty or cursor (B-623, decision 6). Only the child
/// reports; a supervisor previewing the plan reads, he does not report on the child's behalf.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Student + "/study-plans/{planId:int}/positions/{positionId:int}/feedback")]
[Tags("Student – Feedback")]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(PlanOwnershipFilter))]
public class ExerciseFeedbackController(PuglingDbContext db, PositionPlayService play, ExerciseFeedbackService feedback,
    LearningDay learningDay)
    : ControllerBase
{
    /// <summary>
    /// Reports the card <c>itemIndex</c> (or, without it, the whole exercise) with one or more reasons and/or a text. A
    /// reason already reported on the same card and still open is merged into (count up, text appended). An open test attempt
    /// on the position does not block it: the test screen offers no report button, and a report changes nothing of the game
    /// (B-685).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeedbackReceipt>> Create(int planId, int positionId, CreateFeedbackDto dto,
        CancellationToken ct)
    {
        if (!User.IsStudent() || User.StudentId() is not { } childId)
            return this.ProblemWithCode(ApiErrors.Forbidden, "Only the child reports feedback on its cards.");
        var pos = await db.PlanPositions.Include(p => p.Exercise).ThenInclude(e => e!.Prerequisites).Include(p => p.StudyPlan)
            .FirstOrDefaultAsync(p => p.Id == positionId && p.StudyPlanId == planId, ct);
        if (pos is null) return this.ProblemWithCode(ApiErrors.NotFound, "Position not found.");
        // The plan filter also lets a supervisor of the plan's child through; an account that is both supervisor and
        // student would otherwise report a supervised child's card under its own child id.
        if (pos.StudyPlan!.ChildId != childId)
            return this.ProblemWithCode(ApiErrors.Forbidden, "Only the child reports feedback on its cards.");

        var reasons = (dto.Reasons ?? []).Distinct().ToList();
        if (reasons.Any(r => !Enum.IsDefined(r)))
            return this.ProblemWithCode(ApiErrors.ValidationError, "Unknown feedback reason.");
        var text = string.IsNullOrWhiteSpace(dto.Text) ? null : dto.Text.Trim();
        if (reasons.Count == 0 && text is null)
            return this.ProblemWithCode(ApiErrors.ValidationError, "Give at least one reason or a text.");
        if (text is { Length: > ExerciseFeedbackService.MaxTextLength })
            return this.ProblemWithCode(ApiErrors.ValidationError,
                $"The text must not exceed {ExerciseFeedbackService.MaxTextLength} characters.");

        // The card as the child plays it today (B-849): a level raised today is played from tomorrow.
        var today = learningDay.Today;
        ContentItem? card = null;
        if (dto.ItemIndex is { } index)
        {
            card = (await play.ItemsOfAsync(pos, today, ct: ct)).ElementAtOrDefault(index);
            if (index < 0 || card is null)
                return this.ProblemWithCode(ApiErrors.ItemNotFound, "This card does not belong to the position's exercise.");
        }
        // No lock for an open test (B-685): the test screen offers no report button, and a report changes nothing of the
        // game - so the only case a lock would catch is practising after leaving a test with "later".
        var ids = await feedback.ReportAsync(childId, pos, today, card, reasons, text, ct);
        return StatusCode(StatusCodes.Status201Created, new FeedbackReceipt(ids));
    }
}

/// <summary>
/// The fixed feedback reasons and the child's notices about handled feedback (B-628, B-623 decisions 4 and 8).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Student)]
[Tags("Student – Feedback")]
[Produces("application/json")]
[Authorize]
public class FeedbackNoticesController(PuglingDbContext db, ExerciseFeedbackService feedback,
    TimeProvider time) : ControllerBase
{
    /// <summary>The fixed reasons in the order the child sees them, each with its reach.</summary>
    [HttpGet("feedback-reasons")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IEnumerable<FeedbackReasonInfo> Reasons() =>
        FeedbackReasons.All.Select(r => new FeedbackReasonInfo(r, FeedbackReasons.ReachOf(r)));

    /// <summary>The child's feedback that a supervisor marked done and the child has not acknowledged yet.</summary>
    [HttpGet("children/{childId:int}/feedback-notices")]
    [ServiceFilter(typeof(ChildOwnershipFilter))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IReadOnlyList<FeedbackNotice>> Notices(int childId, CancellationToken ct) =>
        await feedback.NoticesAsync(childId, ct);

    /// <summary>
    /// The child acknowledges a notice; it is shown once. Only the child itself - a supervisor reading the list must not
    /// swallow the notice before the child saw it.
    /// </summary>
    [HttpPost("children/{childId:int}/feedback-notices/{feedbackId:int}/seen")]
    [ServiceFilter(typeof(ChildOwnershipFilter))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Seen(int childId, int feedbackId, CancellationToken ct)
    {
        // The child itself, not a supervised one: the child filter lets a supervisor through, and an account may be both.
        if (!User.IsStudent() || User.StudentId() != childId)
            return this.ProblemWithCode(ApiErrors.Forbidden, "Only the child acknowledges its notices.");
        var entry = await db.ExerciseFeedbacks.FirstOrDefaultAsync(f => f.Id == feedbackId && f.ChildId == childId
            && f.Status == FeedbackStatus.Done, ct);
        if (entry is null) return this.ProblemWithCode(ApiErrors.NotFound, "Notice not found.");
        entry.SeenByChildAt ??= time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
