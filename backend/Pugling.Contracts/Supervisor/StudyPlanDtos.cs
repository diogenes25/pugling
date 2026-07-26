namespace Pugling.Contracts.Supervisor;

// Vertrag des Lehrplans: der Plan ist ein reiner Container, alles Lern-Spezifische (Ziel, Punkte,
// Stufe, Leitner) trägt die einzelne Position.

/// <summary>Ein Lehrplan-Container eines Kindes.</summary>
public record PlanResponse(int Id, int ChildId, string Title, int? SubjectId,
    DateOnly StartDate, DateOnly EndDate, bool Active, int PositionCount, string? Description)
{
    /// <summary>
    /// Server-autoritative Affordance: Ob dies der eine, aktuell spielbare Plan des Kindes ist
    /// (aktiv <b>und</b> heute in Laufzeit). Für den Sohn ist stets nur dieser sichtbar; dem Vater
    /// zeigt es unter mehreren Plänen den, den der Sohn gerade spielen kann – ohne die Regel im Client nachzubilden.
    /// </summary>
    public bool IsPlayable { get; init; }
}

/// <summary>Eingabe zum Anlegen eines leeren Lehrplan-Containers.</summary>
public record CreatePlanDto(int ChildId, string Title, int? SubjectId, DateOnly? StartDate, int DurationDays,
    string? Description = null);

/// <summary>Partielle Änderung des Containers. <see cref="ChildId"/> weist den Plan einem anderen eigenen Kind zu.</summary>
public record UpdatePlanDto(string? Title, int? SubjectId, DateOnly? StartDate, DateOnly? EndDate, bool? Active,
    string? Description = null, int? ChildId = null);

/// <summary>Eine Position im Lehrplan: die referenzierte Übung samt eigenem Ziel, Punkten, Stufe und Leitner.</summary>
public record PositionResponse(int Id, int StudyPlanId, int ExerciseId, string ExerciseTitle,
    string ExerciseType, int Order, int? Stage, int? ItemCount, ItemScope Scope, GoalCadence Cadence,
    PracticeOrder OrderStrategy, int? GoalThreshold, bool RequireTypedTest, bool UseLeitner, int MaxBox,
    List<int>? BoxIntervalDays, List<StageStep>? StageSchedule, int PointsGoalMet, int PenaltyCoins,
    int NewContentPoints, int ComboThreshold, int ComboBonusPoints, int SpeedThresholdSeconds, int SpeedBonusPoints);

/// <summary>
/// Anlegen einer Position. Leere Override-Felder erben den Vorschlag der Übung (Hybrid-Prinzip):
/// Stufe/Item-Anzahl bleiben dann <c>null</c> und werden erst beim Spielen aus den Übungs-Defaults
/// aufgelöst; die Punkte-/Bonus-Felder werden aus dem Bonus-Vorschlag der Übung vorbelegt.
/// </summary>
public record CreatePositionDto(int ExerciseId, int? Order, int? Stage, int? ItemCount, ItemScope? Scope,
    GoalCadence? Cadence, PracticeOrder? OrderStrategy, int? GoalThreshold, bool? RequireTypedTest,
    bool? UseLeitner, int? MaxBox, List<int>? BoxIntervalDays, List<StageStep>? StageSchedule,
    int? PointsGoalMet, int? PenaltyCoins, int? NewContentPoints, int? ComboThreshold, int? ComboBonusPoints,
    int? SpeedThresholdSeconds, int? SpeedBonusPoints);

/// <summary>Partielle Änderung der Overrides/Ziele/Punkte. Die referenzierte Übung ist unveränderlich (Fortschritts-Indizes).</summary>
public record UpdatePositionDto(int? Order, int? Stage, int? ItemCount, ItemScope? Scope,
    GoalCadence? Cadence, PracticeOrder? OrderStrategy, int? GoalThreshold, bool? RequireTypedTest,
    bool? UseLeitner, int? MaxBox, List<int>? BoxIntervalDays, List<StageStep>? StageSchedule,
    int? PointsGoalMet, int? PenaltyCoins, int? NewContentPoints, int? ComboThreshold, int? ComboBonusPoints,
    int? SpeedThresholdSeconds, int? SpeedBonusPoints);
