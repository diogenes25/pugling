namespace Pugling.Contracts.Supervisor;

// Vertrag der beiden Ziel-Ebenen über dem Lernstand:
//   * Lernziele (LearnGoal) – einzelne Ergebnis-/Beherrschungsziele auf einem Katalog-Scope,
//   * Objectives (OKR-Kern) – terminierte Klammer über mehreren Etappen (KeyResults).
// Beide werden bei jeder Abfrage live aus dem aggregierten Lernstand ausgewertet; kein Malus.

/// <summary>Ausgewertetes Lernziel inkl. aktuellem Wert und Status (<c>open</c>/<c>achieved</c>/<c>overdue</c>).</summary>
public record LearnGoalResponse(int Id, int ChildId, int SubjectId, int? ChapterId, int? ExerciseId,
    string Scope, string Metric, int TargetValue, int CurrentValue, int ProgressPercent,
    DateOnly? DueDate, string Status, string? Title, DateTime CreatedAt);

/// <summary>Anlage-Request (Scope + Metrik + Zielwert + optionaler Stichtag/Titel).</summary>
public record CreateLearnGoalRequest(int SubjectId, int? ChapterId, int? ExerciseId,
    LearnGoalMetric Metric, int TargetValue, DateOnly? DueDate, string? Title);

/// <summary>Teil-Update: nur gesetzte Felder ändern sich (Scope bleibt fix – zum Umhängen neu anlegen).</summary>
public record UpdateLearnGoalRequest(LearnGoalMetric? Metric, int? TargetValue, DateOnly? DueDate, string? Title);

/// <summary>Ausgewertete Etappe eines Objectives.</summary>
public record KeyResultResponse(int Id, int ObjectiveId, int SubjectId, int? ChapterId, int? ExerciseId,
    string Scope, string Metric, int TargetValue, int CurrentValue, int ProgressPercent, string Status, string? Title);

/// <summary>Ausgewertetes Objective inkl. Etappen und Roll-up (Status <c>open</c>/<c>achieved</c>/<c>overdue</c>).</summary>
public record ObjectiveResponse(int Id, int ChildId, string Title, string? Motivation, string Kind,
    DateOnly? Start, DateOnly? DueDate, bool Active, int RewardOnComplete, int RewardPerKeyResult,
    int AchievedCount, int TotalCount, int ProgressPercent, string Status, bool Rewarded,
    IReadOnlyList<KeyResultResponse> KeyResults, DateTime CreatedAt);

/// <summary>Anlage einer Etappe (Scope + Metrik + Zielwert + optionaler Titel).</summary>
public record CreateKeyResultRequest(int SubjectId, int? ChapterId, int? ExerciseId,
    KeyResultMetric Metric, int TargetValue, string? Title);

/// <summary>Anlage eines Objectives; Etappen können inline mitgegeben werden.</summary>
public record CreateObjectiveRequest(string Title, string? Motivation, ObjectiveKind Kind,
    DateOnly? Start, DateOnly? DueDate, int RewardOnComplete, int RewardPerKeyResult,
    IReadOnlyList<CreateKeyResultRequest>? KeyResults);

/// <summary>Teil-Update eines Objectives: nur gesetzte Felder ändern sich.</summary>
public record UpdateObjectiveRequest(string? Title, string? Motivation, ObjectiveKind? Kind,
    DateOnly? Start, DateOnly? DueDate, bool? Active, int? RewardOnComplete, int? RewardPerKeyResult);

/// <summary>Teil-Update einer Etappe: Metrik/Zielwert/Titel (Scope bleibt fix).</summary>
public record UpdateKeyResultRequest(KeyResultMetric? Metric, int? TargetValue, string? Title);

/// <summary>Tagesstand eines Kindes, aggregiert über seine aktiven Lehrpläne.</summary>
public record ChildDay(int ChildId, string Name, int ActivePlans, int GoalsTotal, int GoalsMet,
    int PointsToday, bool DutyDone, bool Practiced);

/// <summary>Der Tagesüberblick über alle Kinder eines Vaters.</summary>
public record Dashboard(DateOnly Date, IReadOnlyList<ChildDay> Children);
