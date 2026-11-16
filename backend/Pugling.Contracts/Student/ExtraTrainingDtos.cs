namespace Pugling.Contracts.Student;

// Contract of the bonus training: extra exercises on the child's current topic that pay coins beyond the day's duty.
// Server-authoritative like the practice loop - the server picks the form, grades the answers and books the coins.

/// <summary>
/// How a bonus run asks its cards. Ordered by difficulty: a vocabulary exercise climbs from <see cref="SelfAssess"/>
/// over <see cref="Choice"/> to <see cref="Typed"/> with every completed run; every other exercise is played once,
/// typed.
/// </summary>
public enum ExtraForm
{
    /// <summary>Turn the card over and judge yourself - a claim, so it pays nothing.</summary>
    SelfAssess = 1,
    /// <summary>Pick the meaning from several options - checked, pays a little.</summary>
    Choice = 2,
    /// <summary>Produce the answer from memory - checked, pays full.</summary>
    Typed = 3,
}

/// <summary>
/// The bonus training of a child for today. <paramref name="Unlocked"/> is false until today's daily goals are met -
/// all goals in a plan without daily ones (B-535; not <c>DutyDone</c>, which also counts weekly goals). <paramref
/// name="LockReason"/>: <c>no_plan</c> or <c>duty_open</c>, with the goals still open in <paramref name="Outstanding"/>. <paramref name="DailyCoinCap"/> is <c>null</c> when there is no cap.
/// </summary>
public record ExtraTrainingOverview(bool Unlocked, string? LockReason, IReadOnlyList<string> Outstanding,
    int CoinsToday, int? DailyCoinCap, IReadOnlyList<ExtraExercise> Exercises);

/// <summary>
/// One exercise of the bonus pool. Every exercise may be played again (B-538). <paramref name="Repeatable"/> marks the
/// ones that climb a form per completed run (<paramref name="CompletedRuns"/>, vocabulary) and so may also be played at
/// a full cap, for no coins; any other plays its checked stage each time and waits at a full cap, because its run would
/// show the solutions for nothing. <paramref name="NextRunPercent"/> is
/// what the next run is worth today in percent of its hits - 100, 50, 25, then 0, halved by every run of this exercise
/// completed earlier today; <paramref name="Done"/> means that worth is 0 until tomorrow. <paramref name="NextForm"/> is
/// the form the next run asks in, <paramref name="CoinsPerHit"/> what one correct card of it pays at 100 %.
/// <paramref name="OpenRunId"/> names a started run that still waits for its answers - it is resumed, never replaced.
/// </summary>
public record ExtraExercise(int ExerciseId, string Title, string Type, string UnitLabel, int ItemCount,
    bool Repeatable, int CompletedRuns, bool Done, ExtraForm NextForm, int CoinsPerHit, int? OpenRunId,
    int NextRunPercent);

/// <summary>
/// A started bonus run with its cards. The cards follow the stage rules of the practice cards: the meaning
/// (<see cref="ExtraCard.Reveal"/>) is shown only on the self-assessed form. <paramref name="CoinsPerHit"/> is the full
/// rate; <paramref name="RunPercent"/> is the share of it this run is worth as of its start (B-538). The submit judges
/// by the runs completed on the day it is handed in, so a run handed in after midnight can be worth more, never less.
/// </summary>
public record ExtraRunResponse(int RunId, int ExerciseId, string Title, string Type, ExtraForm Form, int Stage,
    bool Typed, int CoinsPerHit, IReadOnlyList<ExtraCard> Cards, int RunPercent);

/// <summary>One card of a bonus run - the same facets a practice card carries, without an image.</summary>
public record ExtraCard(int ItemIndex, string? Prompt, int? GapIndex, string? Hint, int? AnswerLength, string? Reveal,
    IReadOnlyList<string>? RevealAlternatives, IReadOnlyList<string>? Choices, string? AudioUrl, string? Passage,
    bool AnyOrder, IReadOnlyList<WordPair>? Decoding, string? AnswerPattern, IReadOnlyList<string>? LetterTiles,
    IReadOnlyList<string>? CueBox);

/// <summary>One answer of a bonus run: typed or chosen (<paramref name="GivenAnswer"/>) or self-assessed (<paramref name="WasKnown"/>).</summary>
public record ExtraAnswer(int ItemIndex, string? GivenAnswer, bool? WasKnown);

/// <summary>Body of the bonus-run submit: all answers at once. A card without an answer counts as wrong.</summary>
public record SubmitExtraRunDto(List<ExtraAnswer> Answers);

/// <summary>
/// The graded bonus run. <paramref name="CoinsAwarded"/> is what was booked; <paramref name="CapReached"/> says the
/// daily cap cut it short. <paramref name="CoinsToday"/> is the day's bonus total after this run.
/// The breakdown lets the child follow the sum: <paramref name="CoinsPerHit"/> is the full rate per right answer,
/// <paramref name="RunPercent"/> the share of it this run was worth at the submit (B-538), and
/// <paramref name="CoinsEarned"/> the rounded-down result before the daily cap.
/// </summary>
public record ExtraRunResult(int RunId, ExtraForm Form, int Total, int Correct, int CoinsAwarded, bool CapReached,
    int CoinsToday, int? DailyCoinCap, IReadOnlyList<ExtraItemOutcome> Items, int CoinsPerHit, int RunPercent,
    int CoinsEarned);

/// <summary>
/// One graded card, revealed after the run (<paramref name="Expected"/> is the solution shown in hindsight).
/// <paramref name="GapIndex"/> names the asked placeholder of a cloze <paramref name="Prompt"/>, as on the card.
/// </summary>
public record ExtraItemOutcome(int ItemIndex, string? Prompt, string Expected, string? GivenAnswer, bool WasCorrect,
    int? GapIndex);
