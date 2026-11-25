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

/// <summary>One answer of a mixed review round, by card: typed or chosen (<paramref name="GivenAnswer"/>) or self-assessed (<paramref name="WasKnown"/>).</summary>
public record MixedReviewAnswer(int CardId, string? GivenAnswer, bool? WasKnown);

/// <summary>Body of the mixed review submit: all answers at once. A card without an answer counts as wrong.</summary>
public record SubmitMixedReviewDto(List<MixedReviewAnswer> Answers);

/// <summary>
/// The graded mixed review round (B-765). <paramref name="Total"/> and <paramref name="Correct"/> count the graded cards;
/// a skipped card (see <see cref="MixedReviewOutcome.Skipped"/>) counts in neither. <paramref name="CoinsAwarded"/> is
/// what was booked: checked hits only, each at the rate of the form it was asked in, and only where the card is due again
/// in the future and that the child did not meet earlier the same day - introduced, reviewed or asked in a test, each of
/// which may have shown its answer;
/// <paramref name="CoinsEarned"/> is that sum before the cap, and <paramref name="CapReached"/> says the shared bonus cap
/// cut it. <paramref name="CoinsToday"/> is the day's bonus coins after this round, over the bonus
/// training and the mixed review together, against <paramref name="DailyCoinCap"/> (<c>null</c> = no cap).
/// </summary>
public record MixedReviewResult(int RunId, int Total, int Correct, int CoinsAwarded, int CoinsEarned, bool CapReached,
    int CoinsToday, int? DailyCoinCap, IReadOnlyList<MixedReviewOutcome> Items);

/// <summary>
/// One card of a graded round, revealed after the round (<paramref name="Expected"/> is the solution in hindsight).
/// <paramref name="Box"/> and <paramref name="DueOn"/> are the card's place in its own position's Leitner box afterwards.
/// <paramref name="Skipped"/> marks a card that was not graded and moved nothing: no longer due (practised in its position
/// meanwhile), its position now carrying an open duty, no longer using Leitner or playing another exercise, or its
/// exercise changed since the start. <paramref name="AccentSlip"/> (B-624): wrong only by its diacritics on a position with
/// accent leniency - <paramref name="WasCorrect"/> stays false and nothing is paid, but the box held, as in the position's
/// own practice.
/// </summary>
public record MixedReviewOutcome(int CardId, int PositionId, string ExerciseTitle, string? Prompt, string Expected,
    string? GivenAnswer, bool WasCorrect, bool Skipped, int CoinsAwarded, int? Box, DateOnly? DueOn, int? GapIndex,
    bool AccentSlip = false);
