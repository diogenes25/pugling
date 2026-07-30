namespace Pugling.Contracts.Shared;

// Ebenen-übergreifender Vertrag des Lehrplan-Fortschritts: der Sohn liest daraus seine Tagesmission,
// der Vater denselben Rollup als Verlauf/Auswertung.

/// <summary>Status of a single position for one day – enough for the student client to render the right action.</summary>
public record PositionStatus(
    int PositionId, int ExerciseId, string ExerciseTitle, string ExerciseType, string Renderer,
    int Order, GoalCadence Cadence, ExerciseCheckMode CheckMode, bool UseLeitner, bool Testable,
    bool GoalMet, int DueCount, int PoolSize, int PointsGoalMet);

/// <summary>Daily rollup of a study plan across its positions.</summary>
public record DayOverview(
    DateOnly Day, bool DutyDone, int GoalsTotal, int GoalsMet, int PointsAwarded,
    IReadOnlyList<string> Outstanding, IReadOnlyList<PositionStatus> Positions);

/// <summary>One day in the history (for the supervisor's evaluation).</summary>
public record ProgressDay(DateOnly Day, bool DutyDone, int GoalsTotal, int GoalsMet, int PointsAwarded);

/// <summary>Aggregated history: run-wide metrics plus a filtered/sorted list of days.</summary>
public record ProgressView(int DaysComplete, int TotalDays, int TotalPoints, int CurrentStreak,
    IReadOnlyList<ProgressDay> Days);
