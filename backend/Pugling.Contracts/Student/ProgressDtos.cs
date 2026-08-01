using Pugling.Contracts.Shared;

namespace Pugling.Contracts.Student;

// Contract of the child's views on its own progress: daily mission and runtime history of a study plan,
// plus the cross-plan vocabulary progress (per item, per word, with answer history).

/// <summary>Daily mission: the plan overview plus today's rollup over its positions.</summary>
public record OverviewResponse(int PlanId, string Title, DateOnly StartDate, DateOnly EndDate, bool Active,
    int CurrentStreak, DayOverview Today);

/// <summary>Day-by-day progress over the entire run (completed days, goals reached, points).</summary>
public record ProgressResponse(int PlanId, DateOnly StartDate, DateOnly EndDate, int DaysComplete,
    int TotalDays, int TotalPoints, int CurrentStreak, IReadOnlyList<ProgressDay> Days);

/// <summary>Learning progress of a child on an item (front/back from the store, canonically word → translation).</summary>
public record ItemProgressResponse(int ItemId, int ExerciseId, int VocabularyId, string Front, string Back,
    int Box, int MaxBox, int MasteryPercent, int SeenCount, int CorrectCount,
    DateOnly? IntroducedAt, DateTime? LastAnswerAt, bool? LastCorrect,
    [property: System.Text.Json.Serialization.JsonPropertyName("vocabulary")] string Vocabulary);

/// <summary>Aggregated word mastery status across all exercises that use this store word.</summary>
public record WordMasteryResponse(int VocabularyId, string Word, string Translation, int ItemCount,
    int AvgMasteryPercent, int MinBox, int SeenCount, int CorrectCount, int CorrectPercent,
    [property: System.Text.Json.Serialization.JsonPropertyName("vocabulary")] string Vocabulary);

/// <summary>A logged answer event from the item history.</summary>
public record HistoryResponse(DateTime At, string Source, int StageValue, string? GivenAnswer,
    bool WasCorrect, int? PlanPositionId);
