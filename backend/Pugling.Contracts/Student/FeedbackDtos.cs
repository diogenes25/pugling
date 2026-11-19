namespace Pugling.Contracts.Student;

// Contract of the child's feedback on a card (B-628). The student side carries ids, reasons and the card's cue only -
// never the card's solution (the solution-field rule, ConventionGuardTests).

/// <summary>
/// A child's feedback on a card or on the whole exercise of a position. At least one reason or a text is required.
/// </summary>
/// <param name="Reasons">One or more fixed reasons (multiple choice); may be empty when a text is given.</param>
/// <param name="Text">Optional free text (at most 500 characters).</param>
/// <param name="ItemIndex">The card within the position's exercise; <c>null</c> = the exercise as a whole.</param>
public record CreateFeedbackDto(List<FeedbackReason>? Reasons, string? Text, int? ItemIndex);

/// <summary>
/// Receipt of a feedback: one entry per reason (a repeated open reason on the same card is merged into its entry).
/// </summary>
/// <param name="Ids">The feedback entries written or merged into, one per reason (one for a text-only feedback).</param>
public record FeedbackReceipt(IReadOnlyList<int> Ids);

/// <summary>A reason with its fixed reach, for the child's dialog and for the creating agent.</summary>
/// <param name="Reason">The reason.</param>
/// <param name="Reach">Whom it concerns when new content is created.</param>
public record FeedbackReasonInfo(FeedbackReason Reason, FeedbackReach Reach);

/// <summary>
/// A feedback of the child that a supervisor has marked done and the child has not acknowledged yet (B-623, decision 8).
/// </summary>
/// <param name="Id">The feedback entry.</param>
/// <param name="ExerciseId">The exercise it was about.</param>
/// <param name="ExerciseTitle">Title of that exercise.</param>
/// <param name="ItemIndex">The card, or <c>null</c> for the whole exercise.</param>
/// <param name="Cue">What the card showed the child (its prompt), or <c>null</c> for the whole exercise.</param>
/// <param name="Reason">The reported reason, or <c>null</c> for a text-only feedback.</param>
/// <param name="DoneAt">When a supervisor marked it done.</param>
public record FeedbackNotice(int Id, int ExerciseId, string ExerciseTitle, int? ItemIndex, string? Cue,
    FeedbackReason? Reason, DateTime DoneAt);
