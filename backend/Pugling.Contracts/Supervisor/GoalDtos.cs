namespace Pugling.Contracts.Supervisor;

// Vertrag der Ziel-Ebene über dem Lernstand: **Objectives** (OKR-Kern) – eine terminierte Klammer über
// mehreren Etappen (KeyResults), bei jeder Abfrage live aus dem aggregierten Lernstand ausgewertet; kein Malus.
// Die zweite Ebene (`LearnGoal`) ist entfallen: sie war strukturell dasselbe wie ein einzelnes KeyResult,
// nur ohne Klammer und ohne Belohnungslog (siehe docs/lernziele-objectives-plan.md).

/// <summary>Evaluated key result of an objective.</summary>
public record KeyResultResponse(int Id, int ObjectiveId, int SubjectId, int? ChapterId, int? ExerciseId,
    string Scope, string Metric, int TargetValue, int CurrentValue, int ProgressPercent, string Status, string? Title);

/// <summary>Evaluated objective incl. key results and roll-up (status <c>open</c>/<c>achieved</c>/<c>overdue</c>).</summary>
public record ObjectiveResponse(int Id, int ChildId, string Title, string? Motivation, string Kind,
    DateOnly? Start, DateOnly? DueDate, bool Active, int RewardOnComplete, int RewardPerKeyResult,
    int AchievedCount, int TotalCount, int ProgressPercent, string Status, bool Rewarded,
    IReadOnlyList<KeyResultResponse> KeyResults, DateTime CreatedAt);

/// <summary>Creation of a key result (scope + metric + target value + optional title).</summary>
public record CreateKeyResultRequest(int SubjectId, int? ChapterId, int? ExerciseId,
    KeyResultMetric Metric, int TargetValue, string? Title);

/// <summary>Creation of an objective; key results can be supplied inline.</summary>
public record CreateObjectiveRequest(string Title, string? Motivation, ObjectiveKind Kind,
    DateOnly? Start, DateOnly? DueDate, int RewardOnComplete, int RewardPerKeyResult,
    IReadOnlyList<CreateKeyResultRequest>? KeyResults);

/// <summary>Partial update of an objective: only the fields that are set change.</summary>
public record UpdateObjectiveRequest(string? Title, string? Motivation, ObjectiveKind? Kind,
    DateOnly? Start, DateOnly? DueDate, bool? Active, int? RewardOnComplete, int? RewardPerKeyResult);

/// <summary>Partial update of a key result: metric/target value/title (scope stays fixed).</summary>
public record UpdateKeyResultRequest(KeyResultMetric? Metric, int? TargetValue, string? Title);

/// <summary>Daily state of a child, aggregated across its active study plans.</summary>
public record ChildDay(int ChildId, string Name, int ActivePlans, int GoalsTotal, int GoalsMet,
    int PointsToday, bool DutyDone, bool Practiced);

/// <summary>The daily overview across all children of a supervisor.</summary>
public record Dashboard(DateOnly Date, IReadOnlyList<ChildDay> Children);
