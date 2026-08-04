namespace Pugling.Contracts.Supervisor;

// Contract of the goal tier above the learning state: **objectives** (the OKR core) - a dated bracket over
// several milestones (KeyResults), evaluated live from the aggregated learning state on every request; no penalty.
// The second tier (`LearnGoal`) is gone: structurally it was the same as a single KeyResult, only without
// the bracket and without the reward log (see docs/lernziele-objectives-plan.md).

/// <summary>Evaluated key result of an objective.</summary>
public record KeyResultResponse(int Id, int ObjectiveId, int SubjectId, int? SeriesUnitId, int? ExerciseId,
    string Scope, KeyResultMetric Metric, int TargetValue, int CurrentValue, int ProgressPercent,
    string Status, string? Title);

/// <summary>Evaluated objective incl. key results and roll-up (status <c>open</c>/<c>achieved</c>/<c>overdue</c>).</summary>
public record ObjectiveResponse(int Id, int ChildId, string Title, string? Motivation, ObjectiveKind Kind,
    DateOnly? Start, DateOnly? DueDate, bool Active, int RewardOnComplete, int RewardPerKeyResult,
    int AchievedCount, int TotalCount, int ProgressPercent, string Status, bool Rewarded,
    IReadOnlyList<KeyResultResponse> KeyResults, DateTime CreatedAt);

/// <summary>Creation of a key result (scope + metric + target value + optional title).</summary>
public record CreateKeyResultRequest(int SubjectId, int? SeriesUnitId, int? ExerciseId,
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
