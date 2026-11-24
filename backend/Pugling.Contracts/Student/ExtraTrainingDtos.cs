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
    /// <summary>Turn the card over and judge yourself - a claim, so it pays only a token coin per "known" (B-657).</summary>
    SelfAssess = 1,
    /// <summary>Pick the meaning from several options - checked, pays a little.</summary>
    Choice = 2,
    /// <summary>Produce the answer from memory - checked, pays full.</summary>
    Typed = 3,
}

/// <summary>
/// The bonus training of a child for today. <paramref name="Unlocked"/> is false until today's daily goals of all active
/// plans are met - every goal when no plan has a daily one (B-535/B-611; not <c>DutyDone</c>, which also counts weekly
/// goals). <paramref name="LockReason"/>: <c>no_plan</c> or <c>duty_open</c>, with the goals still open in <paramref
/// name="Outstanding"/> and their kind in <paramref name="RewardCadence"/> (<c>Daily</c>, <c>Weekly</c>, or <c>None</c>
/// without any goal - B-612). <paramref name="CoinsToday"/> counts bonus runs and mixed review rounds together, which share
/// <paramref name="DailyCoinCap"/> (B-765); the cap is <c>null</c> when there is none.
/// </summary>
public record ExtraTrainingOverview(bool Unlocked, string? LockReason, IReadOnlyList<string> Outstanding,
    int CoinsToday, int? DailyCoinCap, IReadOnlyList<ExtraExercise> Exercises, GoalCadence RewardCadence);

/// <summary>
/// One exercise of the bonus pool. Every exercise may be played again (B-538). <paramref name="Repeatable"/> marks the
/// ones that climb a form per completed run (<paramref name="CompletedRuns"/>, vocabulary) and so may also be played at
/// a full cap, for no coins; any other plays its checked stage each time and waits at a full cap, because its run would
/// show the solutions for nothing. <paramref name="NextRunPercent"/> is
/// what the next run is worth today in percent of its hits - 100, 50, 25, then 0, halved by every run of this exercise
/// completed earlier today; <paramref name="Done"/> means that worth is 0 until tomorrow. <paramref name="NextForm"/> is
/// the form the next run asks in, <paramref name="CoinsPerHit"/> what one correct card of it pays at 100 % - the highest
/// rate of its cards. <paramref name="MixedRates"/> (B-608) is true when its cards pay different rates (a "both" list asks
/// forward cards as a choice, a vocabulary choice card without a distractor is typed), so the client announces "up to"
/// that rate - on any form - instead of promising it on every card.
/// <paramref name="OpenRunId"/> names a started run that still waits for its answers - it is resumed, never replaced.
/// </summary>
public record ExtraExercise(int ExerciseId, string Title, string Type, string UnitLabel, int ItemCount,
    bool Repeatable, int CompletedRuns, bool Done, ExtraForm NextForm, int CoinsPerHit, int? OpenRunId,
    int NextRunPercent, bool MixedRates);

/// <summary>
/// A started bonus run with its cards. The cards follow the stage rules of the practice cards: the meaning
/// (<see cref="ExtraCard.Reveal"/>) is shown only on the self-assessed form. <paramref name="CoinsPerHit"/> is the full
/// rate of the run's form; what a hit on a single card pays is <see cref="ExtraCard.CoinsPerHit"/> (B-608).
/// <paramref name="RunPercent"/> is the share of those rates this run is worth as of its start (B-538). The submit judges
/// by the runs completed on the day it is handed in, so a run handed in after midnight can be worth more, never less.
/// <paramref name="CoinsLeftToday"/> is what today's bonus cap still pays as of the last start or resume (B-862): 0 when
/// today's cap (the child's own or the server default) is 0 or used up, <c>null</c> without any cap. The submit pays at
/// most what is left when it is handed in.
/// </summary>
public record ExtraRunResponse(int RunId, int ExerciseId, string Title, string Type, ExtraForm Form, int Stage,
    bool Typed, int CoinsPerHit, IReadOnlyList<ExtraCard> Cards, int RunPercent, int? CoinsLeftToday = null);

/// <summary>
/// One card of a bonus run - the same facets a practice card carries, without an image.
///
/// <c>CoinsPerHit</c> (B-608) is what a right answer on this card pays at 100 %, by the form the card is asked in - the
/// same rate the submit books, so the announcement never promises more than the card pays.
///
/// <c>DictationSpokenAnswer</c>/<c>DictationLang</c> (B-597) are set only on the dictation stage of a vocabulary card
/// without a recording: the learnt word the device reads aloud, and the language to read it in. <b>This is the answer,
/// handed out before the answer</b> - a deliberate exception to the anti-cheat rule (B-597 decision 9), because without
/// it no word of the stock could be dictated. The client never shows it as text; without a voice for the language it
/// plays the card as free text from the German <c>Prompt</c>.
///
/// <c>KeyboardLang</c> (B-661) is the language the learner types in on this exercise: the learnt language of its
/// textbook series. The client offers the special characters of that language as an on-screen bar, so a phone keyboard
/// without "î" never blocks a word. It names a character set, not an answer; <c>null</c> = no bar.
/// </summary>
public record ExtraCard(int ItemIndex, string? Prompt, int? GapIndex, string? Hint, int? AnswerLength, string? Reveal,
    IReadOnlyList<string>? RevealAlternatives, IReadOnlyList<string>? Choices, string? AudioUrl, string? Passage,
    bool AnyOrder, IReadOnlyList<WordPair>? Decoding, string? AnswerPattern, IReadOnlyList<string>? LetterTiles,
    IReadOnlyList<string>? CueBox, int CoinsPerHit, string? DictationSpokenAnswer = null, string? DictationLang = null,
    string? KeyboardLang = null);

/// <summary>One answer of a bonus run: typed or chosen (<paramref name="GivenAnswer"/>) or self-assessed (<paramref name="WasKnown"/>).</summary>
public record ExtraAnswer(int ItemIndex, string? GivenAnswer, bool? WasKnown);

/// <summary>Body of the bonus-run submit: all answers at once. A card without an answer counts as wrong.</summary>
public record SubmitExtraRunDto(List<ExtraAnswer> Answers);

/// <summary>
/// The graded bonus run. <paramref name="CoinsAwarded"/> is what was booked; <paramref name="CapReached"/> says the
/// daily cap cut it short. <paramref name="CoinsToday"/> is the day's bonus total after this run - bonus runs and mixed
/// review rounds together, which share the cap (B-765).
/// The breakdown lets the child follow the sum: <paramref name="CoinsPerHit"/> is the full rate per right answer,
/// <paramref name="RunPercent"/> the share of it this run was worth at the submit (B-538), and
/// <paramref name="CoinsEarned"/> the rounded-down result before the daily cap. <paramref name="CoinsForHits"/> is the
/// hits' worth at the full rate: each hit pays by the form its card was asked in (B-597, B-599), so in a run that mixes
/// picks and written answers it is not <c>Correct × CoinsPerHit</c>.
/// </summary>
public record ExtraRunResult(int RunId, ExtraForm Form, int Total, int Correct, int CoinsAwarded, bool CapReached,
    int CoinsToday, int? DailyCoinCap, IReadOnlyList<ExtraItemOutcome> Items, int CoinsPerHit, int RunPercent,
    int CoinsEarned, int CoinsForHits = 0);

/// <summary>
/// One graded card, revealed after the run (<paramref name="Expected"/> is the solution shown in hindsight).
/// <paramref name="GapIndex"/> names the asked placeholder of a cloze <paramref name="Prompt"/>, as on the card.
/// </summary>
public record ExtraItemOutcome(int ItemIndex, string? Prompt, string Expected, string? GivenAnswer, bool WasCorrect,
    int? GapIndex);
