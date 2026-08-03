namespace Pugling.Contracts.Supervisor;

// Contract of the position-bound learning report. It lives on the supervisor tier because it carries the
// solution of every content item as a named field (`ItemReport.Answer`) - readable by the child, that report
// would be a cheat sheet for cards it has never even seen. The tier is the wall here, not the field: the
// report *is* the supervisor's evaluation, so it is gated as a whole instead of blanking a field per role
// (which would make the assurance unreadable in the contract).

/// <summary>Report row for a single content item, including its solution.</summary>
public record ItemReport(int ItemIndex, string Prompt, string Answer, bool Introduced,
    int Box, int MasteryPercent, int ReviewCount, DateOnly? DueOn, DateTime? LastReviewedAt,
    int TestsSeen, int TestsCorrect);

/// <summary>Report of a position including headline metrics (introduced/mastered).</summary>
public record Report(int PositionId, int ExerciseId, string ExerciseTitle, string ExerciseType,
    int MaxBox, int TotalItems, int IntroducedItems, int MasteredItems, IReadOnlyList<ItemReport> Items);
