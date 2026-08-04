namespace Pugling.Contracts.Student;

// Contract of the catalog-hierarchical progress view (subject → series unit → exercise → item), derived from
// the child's study plans. It complements the flat vocabulary view in ProgressDtos.cs; both views
// deliberately share the item DTO (identical shape). Plus the child's view on missions/awards.
//
// The position-bound report used to live here too and moved to Pugling.Contracts.Supervisor: it names the
// solution of every item, so it is not child-readable and the tier folder has to say so.

/// <summary>Aggregated learning progress across a set of vocabulary items (same shape at every level).</summary>
public record MasteryRollup(
    int TotalItems, int IntroducedItems, int MasteredItems, int WeakItems,
    int AvgMasteryPercent, int SeenCount, int CorrectCount, int CorrectPercent, DateTime? LastActivityAt);

/// <summary>Progress of a subject. <paramref name="Active"/> = contains ≥1 exercise currently assigned (via an active plan).</summary>
public record SubjectProgressResponse(int SubjectId, string Name, int SeriesUnitCount, int ExerciseCount, bool Active, MasteryRollup Progress);

/// <summary>Progress of a series unit. <paramref name="Active"/> = contains ≥1 exercise currently assigned.</summary>
public record SeriesUnitProgressResponse(int SeriesUnitId, string Name, int OrderIndex, int ExerciseCount, bool Active, MasteryRollup Progress);

/// <summary>Progress of a single vocabulary exercise. <paramref name="Active"/> = currently assigned via an active plan.</summary>
public record ExerciseProgressResponse(int ExerciseId, string Title, int OrderIndex, bool Active, MasteryRollup Progress);

/// <summary>Status of a mission from the child's perspective: target, current value, completed?</summary>
public record MissionStatus(int Id, string Title, ProgressMetric Metric, MissionPeriod Period,
    int Target, int Current, bool Completed, int RewardPoints);

/// <summary>Status of an award from the child's perspective: threshold, current value, earned?</summary>
public record AchievementStatus(int Id, string Title, string? Icon, ProgressMetric Metric,
    int Threshold, int Current, bool Earned, DateTime? EarnedAt, int RewardPoints);
