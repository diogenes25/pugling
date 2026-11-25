using Pugling.Contracts.Shared;

namespace Pugling.Contracts.Supervisor;

// Contract of the position-bound learning report. It lives on the supervisor tier because it carries the
// solution of every content item as a named field (`ItemReport.Answer`) - readable by the child, that report
// would be a cheat sheet for cards it has never even seen. The tier is the wall here, not the field: the
// report *is* the supervisor's evaluation, so it is gated as a whole instead of blanking a field per role
// (which would make the assurance unreadable in the contract).

/// <summary>
/// Report row for a single content item, including its solution. <c>Prompt</c>/<c>Answer</c> are the card as the exercise's
/// direction asks it. B-597: <c>TurnedWhenWritten</c> marks a card that is asked the other way round wherever the child
/// writes (German shown, the learnt word expected); <c>RecognitionOnly</c> marks the forward card of a word asked both
/// ways, which is never written - its sibling is. B-624: <c>AccentSlips</c> counts the practice answers of this position that
/// were right but for their diacritics and counted half - beside the box it tells whether the word or only its accents
/// are missing. Vocabulary items only (they carry the stable item identity the history is kept by).
/// </summary>
public record ItemReport(int ItemIndex, string Prompt, string Answer, bool Introduced,
    int Box, int MasteryPercent, int ReviewCount, DateOnly? DueOn, DateTime? LastReviewedAt,
    int TestsSeen, int TestsCorrect, bool TurnedWhenWritten = false, bool RecognitionOnly = false,
    RemediationTaskReport? Remediation = null, int AccentSlips = 0);

/// <summary>
/// One task of a remediation position as the father sees it (B-637): helping costs the child no coins, so this is where
/// the help shows. Set only on a remediation position.
/// </summary>
/// <param name="Cue">The German cue the child translates; <c>null</c> where the gap carries none.</param>
/// <param name="DoneAt">The day the task was first typed freely right; <c>null</c> = still open.</param>
/// <param name="SolvedByStage">How often it was solved right per stage, in the order of the ease ladder (free text first).</param>
public record RemediationTaskReport(string? Cue, DateOnly? DoneAt, IReadOnlyList<StageSolveCount> SolvedByStage);

/// <summary>Solutions of a remediation task on one stage (B-637); the stage label comes from the type manifest.</summary>
public record StageSolveCount(int Stage, int Count);

/// <summary>
/// Report of a position including headline metrics (introduced/mastered). <c>Remediation</c>/<c>CompletedOn</c> (B-637,
/// B-636): whether the position runs as remediation, and the day its last task was done. On a build path (B-848) the
/// title is the target's, <c>Build</c> names the level and the exercise played now, and the items are those of the
/// played exercise with the progress made on it.
/// </summary>
public record Report(int PositionId, int ExerciseId, string ExerciseTitle, string ExerciseType,
    int MaxBox, int TotalItems, int IntroducedItems, int MasteredItems, IReadOnlyList<ItemReport> Items,
    bool Remediation = false, DateOnly? CompletedOn = null, BuildPath? Build = null);
