namespace Pugling.Contracts.Supervisor;

// Vertrag der Ziel-Ebene über dem Lernstand: **Objectives** (OKR-Kern) – eine terminierte Klammer über
// mehreren Etappen (KeyResults), bei jeder Abfrage live aus dem aggregierten Lernstand ausgewertet; kein Malus.
// Die zweite Ebene (`LearnGoal`) ist entfallen: sie war strukturell dasselbe wie ein einzelnes KeyResult,
// nur ohne Klammer und ohne Belohnungslog (siehe docs/lernziele-objectives-plan.md).

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
