namespace Pugling.Contracts.Supervisor;

// Contract of the day planner (B-734): the daily plan the planner composes for a child, read by the supervisor only.

/// <summary>
/// Whether the day planner runs for a child (B-734). <c>Off</c>: no daily plans are made. <c>Shadow</c>: a plan is made every
/// learning day and shown to the supervisor, but it has no effect - no duty, no penalty, nothing in the child's app.
/// <c>Enforced</c>: the plan's tasks are the day's duty (B-1036); not settable through the API before the cutoff-day reset (B-737).
/// </summary>
public enum PlannerMode
{
    /// <summary>The planner makes no plans for the child.</summary>
    Off,
    /// <summary>The planner makes daily plans the supervisor sees; they cost and pay nothing.</summary>
    Shadow,
    /// <summary>The day's tasks are the child's duty: streak, chest and goal points follow them, not the positions' cadence.</summary>
    Enforced,
}

/// <summary>
/// One daily plan of a child (B-734): composed once at the first access from 6 a.m. on its learning day and frozen since - a
/// later read returns the same tasks, however the child's progress changed.
/// </summary>
/// <param name="Day">The learning day the plan is for.</param>
/// <param name="CreatedAt">When the plan was composed (UTC).</param>
/// <param name="Mode">The planner mode the plan was composed in; a <c>Shadow</c> plan has no effect.</param>
/// <param name="NewWordsBudget">New words the planner allowed for the day: the child's tempo, held between the supervisor's
/// floor and ceiling.</param>
/// <param name="Warnings">What kept the plan from meeting a limit, e.g. fewer positions than the floor of tasks.</param>
/// <param name="Tasks">The day's tasks in the order the planner ranked them.</param>
public record DailyPlanResponse(DateOnly Day, DateTime CreatedAt, PlannerMode Mode, int NewWordsBudget,
    IReadOnlyList<string> Warnings, IReadOnlyList<DailyTaskResponse> Tasks);

/// <summary>One task of a daily plan: a position of an active plan with what the planner asks of it and why.</summary>
/// <param name="PlanPositionId">The position the task plays; <c>null</c> when the position was deleted since.</param>
/// <param name="ExerciseId">The exercise the position played when the plan was composed.</param>
/// <param name="ExerciseTitle">Its title at that time.</param>
/// <param name="SubjectId">The subject of the task (plan subject, else the exercise's series subject).</param>
/// <param name="SubjectName">The subject's name, empty without a subject.</param>
/// <param name="Order">Rank in the plan, from 1.</param>
/// <param name="DueReviews">Words due for review at the position; they do not count against the new-word ceiling.</param>
/// <param name="NewWords">New words the task introduces.</param>
/// <param name="Stage">The stage the planner suggests, one easier after a failure the day before; <c>null</c> = the
/// position's own stage.</param>
/// <param name="Reason">Why the task is in the plan, built from the inputs (due reviews, yesterday's errors, tempo).</param>
public record DailyTaskResponse(int? PlanPositionId, int ExerciseId, string ExerciseTitle, int? SubjectId, string SubjectName,
    int Order, int DueReviews, int NewWords, int? Stage, string Reason);
