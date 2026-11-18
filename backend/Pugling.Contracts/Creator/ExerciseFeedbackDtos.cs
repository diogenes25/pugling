namespace Pugling.Contracts.Creator;

// Contract of the creator's view of feedback on an exercise (B-628, B-623 decision 7): counted reasons only - no child,
// no free text, because a teacher account must not read what other people's children wrote.

/// <summary>How often a reason was reported.</summary>
/// <param name="Reason">The reason.</param>
/// <param name="Reach">Its fixed reach.</param>
/// <param name="Count">Number of reports (all children, open and done).</param>
public record FeedbackReasonCount(FeedbackReason Reason, FeedbackReach Reach, int Count);

/// <summary>The counted reasons of one card (or of the whole exercise).</summary>
/// <param name="ItemIndex">The card, or <c>null</c> for feedback on the whole exercise.</param>
/// <param name="Reasons">Counted reasons, most reported first.</param>
public record FeedbackCardCount(int? ItemIndex, IReadOnlyList<FeedbackReasonCount> Reasons);

/// <summary>The feedback on an exercise, counted per reason - in total and per card.</summary>
/// <param name="ExerciseId">The exercise.</param>
/// <param name="Total">Number of reports with a reason.</param>
/// <param name="ByReason">Counted reasons over the whole exercise, most reported first.</param>
/// <param name="ByCard">Counted reasons per card, in card order (whole-exercise feedback first).</param>
public record ExerciseFeedbackSummary(int ExerciseId, int Total, IReadOnlyList<FeedbackReasonCount> ByReason,
    IReadOnlyList<FeedbackCardCount> ByCard);
