namespace Pugling.Contracts.Student;

// Contract of the mixed review (B-764, B-765): one short round of the cards due across all of the child's playable
// Leitner positions. Voluntary and behind the bonus training's lock; each card moves the box of its own position.

/// <summary>
/// The child's mixed review for today. <paramref name="Unlocked"/>, <paramref name="LockReason"/> and
/// <paramref name="Outstanding"/> are the bonus training's lock (<c>no_plan</c>, <c>duty_open</c>).
/// <paramref name="DueCards"/> counts the cards a round may draw now - due, already met, in a playable plan and in a
/// position without an open duty; a round asks at most <paramref name="RoundSize"/> of them. <paramref name="OpenRunId"/>
/// names today's started round, which a start resumes. <paramref name="CoinsToday"/> is the bonus coins of the day over the
/// bonus training and the mixed review together, against the shared <paramref name="DailyCoinCap"/> (<c>null</c> = no cap).
/// </summary>
public record MixedReviewOverview(bool Unlocked, string? LockReason, IReadOnlyList<string> Outstanding, int DueCards,
    int RoundSize, int? OpenRunId, int CoinsToday, int? DailyCoinCap);

/// <summary>
/// A started (or resumed) mixed review round with its cards, in the order they are asked. <paramref name="CoinsLeftToday"/>
/// is what the shared bonus cap still pays as of this start: 0 when it is used up, <c>null</c> without any cap.
/// </summary>
public record MixedReviewRunResponse(int RunId, IReadOnlyList<MixedReviewCardView> Cards, int? CoinsLeftToday);

/// <summary>
/// One card of a mixed review round, addressed by <paramref name="CardId"/> in the answer. It comes from position
/// <paramref name="PositionId"/> of plan <paramref name="PlanTitle"/> (subject <paramref name="SubjectName"/>) and is
/// asked at that position's stage of the day, <paramref name="Stage"/> - never an easier one. <paramref name="Card"/>
/// carries the facets of a bonus card; its <c>CoinsPerHit</c> is 0 where the stage is self-assessed, because only a
/// checked hit pays.
/// </summary>
public record MixedReviewCardView(int CardId, int PositionId, string PlanTitle, string? SubjectName, string ExerciseTitle,
    int Stage, bool Typed, ExtraCard Card);
