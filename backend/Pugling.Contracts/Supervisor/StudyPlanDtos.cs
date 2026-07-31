namespace Pugling.Contracts.Supervisor;

// Vertrag des Lehrplans: der Plan ist ein reiner Container, alles Lern-Spezifische (Ziel, Punkte,
// Stufe, Leitner) trägt die einzelne Position.

/// <summary>A study plan container of a child.</summary>
public record PlanResponse(int Id, int ChildId, string Title, int? SubjectId,
    DateOnly StartDate, DateOnly EndDate, bool Active, int PositionCount, string? Description)
{
    /// <summary>
    /// Server-authoritative affordance: whether this is the one currently playable plan of the child
    /// (active <b>and</b> within its run today). For the student, only this one is ever visible; for the
    /// supervisor, among several plans it shows the one the student can currently play – without
    /// reimplementing the rule on the client.
    /// </summary>
    public bool IsPlayable { get; init; }
}

/// <summary>Input for creating an empty study plan container.</summary>
public record CreatePlanDto(int ChildId, string Title, int? SubjectId, DateOnly? StartDate, int DurationDays,
    string? Description = null);

/// <summary>Partial change to the container. <see cref="ChildId"/> reassigns the plan to another of the caller's own children.</summary>
public record UpdatePlanDto(string? Title, int? SubjectId, DateOnly? StartDate, DateOnly? EndDate, bool? Active,
    string? Description = null, int? ChildId = null);

/// <summary>A position in the study plan: the referenced exercise together with its own goal, points, stage, and Leitner settings.</summary>
/// <param name="Id">Id of the position.</param>
/// <param name="StudyPlanId">Study plan the position belongs to.</param>
/// <param name="ExerciseId">
/// The referenced catalog exercise. <b>Immutable</b>: Leitner progress is anchored to it via item
/// indices; swapping it would redirect that progress onto unrelated content.
/// </param>
/// <param name="ExerciseTitle">Title of the exercise – included so a list is readable without a second round trip.</param>
/// <param name="ExerciseType">Type key of the exercise (a value from the type manifest).</param>
/// <param name="Order">Order within the plan.</param>
/// <param name="Stage">Query form of the final test; <c>null</c> = default of the exercise or the method.</param>
/// <param name="ItemCount">How many items per run; <c>null</c> = all.</param>
/// <param name="Scope">Which part of the content is played (see <see cref="ItemScope"/>).</param>
/// <param name="Cadence">
/// Goal cadence (see <see cref="GoalCadence"/>). <c>None</c> = free practice that does not count toward
/// the mandatory goal – and therefore cannot trigger a penalty either.
/// </param>
/// <param name="OrderStrategy">
/// Play-out order (see <see cref="PracticeOrder"/>); it is frozen at the start of a session/test.
/// </param>
/// <param name="GoalThreshold">
/// Passing threshold of the final test in <b>percent</b> of correct answers; <c>null</c> = 80%. The
/// unit applies to <i>all</i> testable exercise types – including catalog checks, since a position's
/// goal is always measured by a passed position test. Unused for pure content exercises.
/// </param>
/// <param name="RequireTypedTest">Only typed, objectively checkable stages in the final test – no self-assessment.</param>
/// <param name="UseLeitner">Leitner box with review scheduling instead of a single simple pass.</param>
/// <param name="MaxBox">Highest Leitner box; an item there counts as mastered (default 5).</param>
/// <param name="BoxIntervalDays">Review interval in days per box; <c>null</c> = method default.</param>
/// <param name="StageSchedule">Schedule of which query form applies at which box; <c>null</c> = method default.</param>
/// <param name="PointsGoalMet">Points when the period's goal is met.</param>
/// <param name="PenaltyCoins">
/// Coin <b>penalty</b> for a missed mandatory goal (the "stick"); <c>0</c> = pure reward. Only booked
/// for completed periods, and may drive the balance negative.
/// </param>
/// <param name="NewContentPoints">Points for a piece of content introduced for the first time.</param>
/// <param name="ComboThreshold">How many correct answers in a row trigger the combo bonus.</param>
/// <param name="ComboBonusPoints">Points per combo reached.</param>
/// <param name="SpeedThresholdSeconds">Up to how many seconds an answer counts as "fast"; <c>0</c> = off.</param>
/// <param name="SpeedBonusPoints">Points per fast answer.</param>
public record PositionResponse(int Id, int StudyPlanId, int ExerciseId, string ExerciseTitle,
    string ExerciseType, int Order, int? Stage, int? ItemCount, ItemScope Scope, GoalCadence Cadence,
    PracticeOrder OrderStrategy, int? GoalThreshold, bool RequireTypedTest, bool UseLeitner, int MaxBox,
    List<int>? BoxIntervalDays, List<StageStep>? StageSchedule, int PointsGoalMet, int PenaltyCoins,
    int NewContentPoints, int ComboThreshold, int ComboBonusPoints, int SpeedThresholdSeconds, int SpeedBonusPoints);

/// <summary>
/// Creating a position. Empty override fields inherit the exercise's suggestion (hybrid principle):
/// stage/item count then stay <c>null</c> and are resolved from the exercise defaults only when played;
/// the points/bonus fields are prefilled from the exercise's bonus suggestion.
/// </summary>
/// <param name="ExerciseId">The catalog exercise to assign; must be executable (execute permission or public).</param>
/// <param name="Order">Position within the plan; <c>null</c> = append at the end.</param>
/// <param name="Stage">Query form; <c>null</c> = inherits the exercise default (resolved only when played).</param>
/// <param name="ItemCount">Items per run; <c>null</c> = inherits the exercise default.</param>
/// <param name="Scope">Which part of the content (see <see cref="ItemScope"/>); <c>null</c> = all.</param>
/// <param name="Cadence">Goal cadence (see <see cref="GoalCadence"/>); <c>null</c> = no mandatory goal.</param>
/// <param name="OrderStrategy">Play-out order (see <see cref="PracticeOrder"/>); <c>null</c> = weakest first.</param>
/// <param name="GoalThreshold">
/// Passing threshold in <b>percent</b> (see <see cref="PositionResponse"/>); <c>null</c> = 80%.
/// </param>
/// <param name="RequireTypedTest">As in <see cref="PositionResponse"/>; <c>null</c> = inherits the exercise default.</param>
/// <param name="UseLeitner">As in <see cref="PositionResponse"/>; <c>null</c> = inherits the exercise default.</param>
/// <param name="MaxBox">Highest Leitner box; <c>null</c> or <c>&lt;= 0</c> = 5.</param>
/// <param name="BoxIntervalDays">Review intervals per box; <c>null</c> = method default.</param>
/// <param name="StageSchedule">Stage schedule per box; <c>null</c> = method default.</param>
/// <param name="PointsGoalMet">Points when the goal is met; <c>null</c> = 20.</param>
/// <param name="PenaltyCoins">Coin penalty for a missed mandatory goal; <c>null</c> = 0 (pure reward, opt-in).</param>
/// <param name="NewContentPoints">Points for new content; <c>null</c> = the exercise's bonus suggestion, otherwise 10.</param>
/// <param name="ComboThreshold">Combo threshold; <c>null</c> = the exercise's bonus suggestion, otherwise 5.</param>
/// <param name="ComboBonusPoints">Combo bonus; <c>null</c> = the exercise's bonus suggestion, otherwise 5.</param>
/// <param name="SpeedThresholdSeconds">Time limit for "fast"; <c>null</c> = the exercise's bonus suggestion, otherwise 0 (off).</param>
/// <param name="SpeedBonusPoints">Bonus per fast answer; <c>null</c> = the exercise's bonus suggestion, otherwise 0.</param>
public record CreatePositionDto(int ExerciseId, int? Order, int? Stage, int? ItemCount, ItemScope? Scope,
    GoalCadence? Cadence, PracticeOrder? OrderStrategy, int? GoalThreshold, bool? RequireTypedTest,
    bool? UseLeitner, int? MaxBox, List<int>? BoxIntervalDays, List<StageStep>? StageSchedule,
    int? PointsGoalMet, int? PenaltyCoins, int? NewContentPoints, int? ComboThreshold, int? ComboBonusPoints,
    int? SpeedThresholdSeconds, int? SpeedBonusPoints);

/// <summary>
/// Partial change to the overrides/goals/points. The referenced exercise is immutable
/// (progress indices).
/// <para>
/// <b>PATCH semantics:</b> <c>null</c> means "not specified" for <i>every</i> field – the previous
/// value stays. It does <b>not</b> mean "reset to default"; that would need its own
/// <c>Clear</c> switch, which this DTO deliberately does not have.
/// </para>
/// </summary>
/// <param name="Order">New position within the plan.</param>
/// <param name="Stage">Query form of the final test.</param>
/// <param name="ItemCount">Items per run.</param>
/// <param name="Scope">Which part of the content (see <see cref="ItemScope"/>).</param>
/// <param name="Cadence">Goal cadence (see <see cref="GoalCadence"/>).</param>
/// <param name="OrderStrategy">Play-out order (see <see cref="PracticeOrder"/>).</param>
/// <param name="GoalThreshold">
/// Passing threshold in <b>percent</b> (see <see cref="PositionResponse"/>); <c>null</c> = not specified.
/// </param>
/// <param name="RequireTypedTest">Only typed stages in the final test.</param>
/// <param name="UseLeitner">Leitner box instead of a single simple pass.</param>
/// <param name="MaxBox">Highest Leitner box.</param>
/// <param name="BoxIntervalDays">Review intervals per box.</param>
/// <param name="StageSchedule">Stage schedule per box.</param>
/// <param name="PointsGoalMet">Points when the goal is met.</param>
/// <param name="PenaltyCoins">Coin penalty for a missed mandatory goal.</param>
/// <param name="NewContentPoints">Points for new content.</param>
/// <param name="ComboThreshold">Combo threshold.</param>
/// <param name="ComboBonusPoints">Combo bonus.</param>
/// <param name="SpeedThresholdSeconds">Time limit for "fast"; <c>0</c> = off.</param>
/// <param name="SpeedBonusPoints">Bonus per fast answer.</param>
public record UpdatePositionDto(int? Order, int? Stage, int? ItemCount, ItemScope? Scope,
    GoalCadence? Cadence, PracticeOrder? OrderStrategy, int? GoalThreshold, bool? RequireTypedTest,
    bool? UseLeitner, int? MaxBox, List<int>? BoxIntervalDays, List<StageStep>? StageSchedule,
    int? PointsGoalMet, int? PenaltyCoins, int? NewContentPoints, int? ComboThreshold, int? ComboBonusPoints,
    int? SpeedThresholdSeconds, int? SpeedBonusPoints);
