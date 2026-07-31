namespace Pugling.Contracts.Student;

// Vertrag der katalog-hierarchischen Lernstand-Sicht (Fach → Kapitel → Übung → Item), abgeleitet aus
// den Lehrplänen des Kindes. Ergänzt die flache Vokabel-Sicht in ProgressDtos.cs; das Item-DTO teilen
// sich beide Sichten bewusst (identische Form). Dazu die Sohn-Sicht auf Missionen/Auszeichnungen
// und der positionsgebundene Report.

/// <summary>Aggregated learning progress across a set of vocabulary items (same shape at every level).</summary>
public record MasteryRollup(
    int TotalItems, int IntroducedItems, int MasteredItems, int WeakItems,
    int AvgMasteryPercent, int SeenCount, int CorrectCount, int CorrectPercent, DateTime? LastActivityAt);

/// <summary>Progress of a subject. <paramref name="Active"/> = contains ≥1 exercise currently assigned (via an active plan).</summary>
public record SubjectProgressResponse(int SubjectId, string Name, int ChapterCount, int ExerciseCount, bool Active, MasteryRollup Progress);

/// <summary>Progress of a chapter. <paramref name="Active"/> = contains ≥1 exercise currently assigned.</summary>
public record ChapterProgressResponse(int ChapterId, string Name, int OrderIndex, int ExerciseCount, bool Active, MasteryRollup Progress);

/// <summary>Progress of a single vocabulary exercise. <paramref name="Active"/> = currently assigned via an active plan.</summary>
public record ExerciseProgressResponse(int ExerciseId, string Title, int OrderIndex, bool Active, MasteryRollup Progress);

/// <summary>Status of a mission from the child's perspective: target, current value, completed?</summary>
public record MissionStatus(int Id, string Title, ProgressMetric Metric, MissionPeriod Period,
    int Target, int Current, bool Completed, int RewardPoints);

/// <summary>Status of an award from the child's perspective: threshold, current value, earned?</summary>
public record AchievementStatus(int Id, string Title, string? Icon, ProgressMetric Metric,
    int Threshold, int Current, bool Earned, DateTime? EarnedAt, int RewardPoints);

/// <summary>Report row for a single content item.</summary>
public record ItemReport(int ItemIndex, string Prompt, string Answer, bool Introduced,
    int Box, int MasteryPercent, int ReviewCount, DateOnly? DueOn, DateTime? LastReviewedAt,
    int TestsSeen, int TestsCorrect);

/// <summary>Report of a position including headline metrics (introduced/mastered).</summary>
public record Report(int PositionId, int ExerciseId, string ExerciseTitle, string ExerciseType,
    int MaxBox, int TotalItems, int IntroducedItems, int MasteredItems, IReadOnlyList<ItemReport> Items);
