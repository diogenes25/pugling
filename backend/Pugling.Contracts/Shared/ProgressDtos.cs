namespace Pugling.Contracts.Shared;

// Ebenen-übergreifender Vertrag des Lehrplan-Fortschritts: der Sohn liest daraus seine Tagesmission,
// der Vater denselben Rollup als Verlauf/Auswertung.

/// <summary>Status einer einzelnen Position für einen Tag – genug, damit der Sohn-Client die richtige Aktion rendert.</summary>
public record PositionStatus(
    int PositionId, int ExerciseId, string ExerciseTitle, string ExerciseType, string Renderer,
    int Order, GoalCadence Cadence, ExerciseCheckMode CheckMode, bool UseLeitner, bool Testable,
    bool GoalMet, int DueCount, int PoolSize, int PointsGoalMet);

/// <summary>Tages-Rollup eines Lehrplans über seine Positionen.</summary>
public record DayOverview(
    DateOnly Day, bool DutyDone, int GoalsTotal, int GoalsMet, int PointsAwarded,
    IReadOnlyList<string> Outstanding, IReadOnlyList<PositionStatus> Positions);

/// <summary>Ein Tag im Verlauf (für die Vater-Auswertung).</summary>
public record ProgressDay(DateOnly Day, bool DutyDone, int GoalsTotal, int GoalsMet, int PointsAwarded);

/// <summary>Aggregierter Verlauf: laufzeitweite Kennzahlen + gefilterte/sortierte Tagesliste.</summary>
public record ProgressView(int DaysComplete, int TotalDays, int TotalPoints, int CurrentStreak,
    IReadOnlyList<ProgressDay> Days);
