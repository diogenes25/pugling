namespace Pugling.Api.Models;

/// <summary>
/// The day planner's plan for one child and learning day (B-734): composed once at the first access from 6 a.m. and frozen
/// since, so the plan the supervisor read in the morning is the plan the day is judged by. One per child and day - the
/// unique index is what keeps the timer and a first request racing each other from making two.
/// </summary>
public class DailyPlan
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    /// <summary>The learning day (German time, <c>LearningDay</c>).</summary>
    public DateOnly Day { get; set; }
    /// <summary>
    /// The planner mode at composing time, a snapshot: switching the child's planner later does not turn a shadow plan into
    /// one that was in force.
    /// </summary>
    public PlannerMode Mode { get; set; }
    /// <summary>New words allowed for the day: the child's tempo held between the supervisor's floor and ceiling.</summary>
    public int NewWordsBudget { get; set; }
    /// <summary>What kept the plan from meeting a limit (fewer positions than the floor, too few new words left).</summary>
    public List<string> Warnings { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public List<DailyTask> Tasks { get; set; } = [];
}

/// <summary>
/// One task of a <see cref="DailyPlan"/>: a position of an active plan with what the planner asks of it and why. The
/// exercise is a snapshot of what the position played when the plan was composed, so a later build-level change does not
/// rewrite the morning's plan.
/// </summary>
public class DailyTask
{
    public int Id { get; set; }
    public int DailyPlanId { get; set; }
    public DailyPlan? DailyPlan { get; set; }
    /// <summary>The position the task plays; <c>null</c> once the position is deleted - the plan stays as it was read.</summary>
    public int? PlanPositionId { get; set; }
    public PlanPosition? PlanPosition { get; set; }
    /// <summary>The exercise the position played at composing time (snapshot, no foreign key).</summary>
    public int ExerciseId { get; set; }
    /// <summary>Its title at composing time.</summary>
    public string ExerciseTitle { get; set; } = "";
    public int? SubjectId { get; set; }
    public Subject? Subject { get; set; }
    /// <summary>Rank in the plan, from 1.</summary>
    public int Order { get; set; }
    /// <summary>Words due for review at the position; they do not count against the new-word ceiling.</summary>
    public int DueReviews { get; set; }
    /// <summary>New words the task introduces.</summary>
    public int NewWords { get; set; }
    /// <summary>The suggested stage, one easier after a failure the day before; <c>null</c> = the position's own stage.</summary>
    public int? Stage { get; set; }
    /// <summary>Why the task is in the plan, built from the inputs.</summary>
    public string Reason { get; set; } = "";
    /// <summary>
    /// When the task's goal was first met on its day (B-1037), stamped by the path that books the goal; <c>null</c> = not met
    /// (yet). A goal once met stays met for the settlement, like the reward lock of a position.
    /// </summary>
    public DateTime? DoneAt { get; set; }
}

/// <summary>
/// The penalty booked for an enforced task left open at the end of its day (B-1037). At most one per task - the unique index is
/// what keeps the lazy settlement at login and the timer, racing each other, from charging a task twice.
/// </summary>
public class DailyTaskPenalty
{
    public int Id { get; set; }
    public int DailyTaskId { get; set; }
    public DailyTask? DailyTask { get; set; }
    /// <summary>Coins deducted (a positive value; the ledger entry is negative): the child's amount at settlement.</summary>
    public int Points { get; set; }
    public DateTime AppliedAt { get; set; }
    /// <summary>
    /// When a justified feedback made the task's day met and took the penalty back (B-768 for tasks, B-1037). The row stays, or
    /// the settlement would book the penalty again.
    /// </summary>
    public DateTime? RefundedAt { get; set; }
    /// <summary>The feedback whose confirmation took the penalty back; may fade.</summary>
    public int? RefundedByFeedbackId { get; set; }
    public ExerciseFeedback? RefundedByFeedback { get; set; }
}
