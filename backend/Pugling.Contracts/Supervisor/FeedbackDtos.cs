namespace Pugling.Contracts.Supervisor;

// Contract of the supervisor's view of the children's feedback (B-628, B-623 decision 7): the full picture - child,
// exercise, card, the child's answer and the free text - for every supervisor of the child.

/// <summary>A child's feedback as the supervisor sees it.</summary>
/// <param name="Id">The feedback entry.</param>
/// <param name="ChildId">The reporting child.</param>
/// <param name="ChildName">Its name.</param>
/// <param name="StudyPlanId">The plan of the position, or <c>null</c> if the position was deleted since.</param>
/// <param name="PlanPositionId">The position played, or <c>null</c> if it was deleted since.</param>
/// <param name="ExerciseId">The exercise.</param>
/// <param name="ExerciseTitle">Its title.</param>
/// <param name="ItemIndex">The card, or <c>null</c> for the whole exercise.</param>
/// <param name="Cue">The card's prompt as it is now, or <c>null</c> (whole exercise, or the card no longer exists).</param>
/// <param name="Solution">The card's expected answer as it is now, or <c>null</c>.</param>
/// <param name="ChildAnswer">The child's most recent stored answer to that card, or <c>null</c> when none is stored.</param>
/// <param name="Reason">The reason, or <c>null</c> for a text-only feedback.</param>
/// <param name="Reach">The reason's reach, or <c>null</c> without a reason.</param>
/// <param name="Count">How often the child reported this reason on this card while it was open.</param>
/// <param name="Text">The child's free text, several reports joined by line breaks.</param>
/// <param name="Status">Open or done.</param>
/// <param name="CreatedAt">First report.</param>
/// <param name="LastReportedAt">Latest report.</param>
/// <param name="DoneAt">When it was marked done, or <c>null</c>.</param>
public record FeedbackResponse(int Id, int ChildId, string ChildName, int? StudyPlanId, int? PlanPositionId,
    int ExerciseId, string ExerciseTitle, int? ItemIndex, string? Cue, string? Solution, string? ChildAnswer,
    FeedbackReason? Reason, FeedbackReach? Reach, int Count, string? Text, FeedbackStatus Status,
    DateTime CreatedAt, DateTime LastReportedAt, DateTime? DoneAt);

/// <summary>Changes the processing state of a feedback; done holds for all supervisors of the child.</summary>
/// <param name="Status">The new state.</param>
public record UpdateFeedbackDto(FeedbackStatus Status);
