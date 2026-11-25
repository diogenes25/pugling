using Pugling.Contracts.Shared;

namespace Pugling.Contracts.Supervisor;

// Contract of the study plan: the plan is a pure container, everything learning-specific (goal, points,
// stage, Leitner) is carried by the individual position.

/// <summary>A study plan container of a child.</summary>
/// <param name="Id">Id of the plan.</param>
/// <param name="ChildId">The child the plan belongs to.</param>
/// <param name="Title">Name of the plan.</param>
/// <param name="SubjectId">
/// Subject the plan covers - the group of the activation rule: a child has at most one active plan per subject.
/// <c>null</c> only for a legacy plan or one whose subject was deleted; the next edit must give it one.
/// </param>
/// <param name="StartDate">First day of the run.</param>
/// <param name="EndDate">Last day of the run.</param>
/// <param name="Active">Whether the plan is switched on.</param>
/// <param name="PositionCount">Number of positions in the plan.</param>
/// <param name="Description">Free note for the supervisor.</param>
/// <param name="SeriesUnitId">Optional unit of a textbook series the plan works through; <c>null</c> = none.</param>
/// <param name="SeriesUnitLabel">Label of that unit, so a list is readable without a second round trip.</param>
public record PlanResponse(int Id, int ChildId, string Title, int? SubjectId,
    DateOnly StartDate, DateOnly EndDate, bool Active, int PositionCount, string? Description,
    int? SeriesUnitId = null, string? SeriesUnitLabel = null)
{
    /// <summary>
    /// Server-authoritative affordance: whether this plan is currently playable (active <b>and</b> within its run
    /// today). A child has at most one active plan per subject, so several plans of different subjects can be
    /// playable at once; the student sees exactly the playable ones. The supervisor reads the rule from here instead
    /// of reimplementing it on the client.
    /// </summary>
    public bool IsPlayable { get; init; }
}

/// <summary>Input for creating an empty study plan container.</summary>
/// <param name="ChildId">The child the plan belongs to; must be one of the caller's own.</param>
/// <param name="Title">Name of the plan.</param>
/// <param name="SubjectId">
/// Subject the plan covers. <b>Mandatory</b> unless <paramref name="SeriesUnitId"/> is given, whose series then
/// supplies it; a subject that differs from the unit's is rejected (<c>validation_error</c>).
/// </param>
/// <param name="StartDate">First day of the run; <c>null</c> = today.</param>
/// <param name="DurationDays">Length of the run in days; <c>&lt;= 0</c> = 10.</param>
/// <param name="Description">Free note for the supervisor.</param>
/// <param name="Active">
/// Whether the plan starts out active. <c>true</c> (the default): the new plan becomes the child's playable plan of
/// its subject, and an active plan of the <b>same</b> subject is switched off - plans of other subjects stay on.
/// <c>false</c> is for a plan that must be <b>reviewed before it becomes an obligation</b> - a machine-generated one
/// above all: without it, an agent run would silently replace the plan the child is currently playing, and
/// unchecked content would start costing penalty coins the same night.
/// </param>
/// <param name="SeriesUnitId">Optional unit of a textbook series the plan works through.</param>
public record CreatePlanDto(int ChildId, string Title, int? SubjectId, DateOnly? StartDate, int DurationDays,
    string? Description = null, bool Active = true, int? SeriesUnitId = null);

/// <summary>
/// Partial change to the container (PATCH: <c>null</c> = unchanged). <see cref="ChildId"/> reassigns the plan to
/// another of the caller's own children. A plan without a subject must be given one with any change - except a
/// change that only switches it off (<c>active: false</c>), which never fails on a mandatory field.
/// </summary>
/// <param name="Title">New name.</param>
/// <param name="SubjectId">New subject; must match the unit's series if the plan has (or gets) a unit.</param>
/// <param name="StartDate">New first day of the run.</param>
/// <param name="EndDate">New last day of the run.</param>
/// <param name="Active">Switches the plan on or off; switching on turns off the active plan of the same subject.</param>
/// <param name="Description">New free note; empty removes it.</param>
/// <param name="ChildId">Moves the plan to another of the caller's own children.</param>
/// <param name="SeriesUnitId">New unit; its series fixes the plan's subject.</param>
/// <param name="ClearSeriesUnit">
/// Removes the plan's unit (the subject stays). Needed because <c>null</c> means "not specified". Wins over
/// <c>seriesUnitId</c> if both arrive together.
/// </param>
public record UpdatePlanDto(string? Title, int? SubjectId, DateOnly? StartDate, DateOnly? EndDate, bool? Active,
    string? Description = null, int? ChildId = null, int? SeriesUnitId = null, bool ClearSeriesUnit = false);

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
/// <param name="TimeSlots">
/// Time slots with their own points multiplier for this obligation; <c>null</c> = only the server's global
/// slots apply. They are considered <b>together with</b> the global ones, the narrowest window winning – a
/// position slot neither replaces the global ones nor multiplies with them.
/// </param>
/// <param name="NewItemsPerDay">
/// Daily dose: how many never-shown contents a learn round brings per day; <c>0</c> = no cap. With a dose the
/// final test checks the day's portion (introduced, reviewed or due today) instead of everything introduced.
/// </param>
/// <param name="DailyDoseInForce">
/// The dose the play actually applies: <paramref name="NewItemsPerDay"/>, but 0 while the exercise grades as a set (an
/// unordered list) - the stored dose then rests and comes back when the list is ordered again (B-478, B-480).
/// </param>
/// <param name="LearningCheck">
/// What meets this position's goal, computed by the server from the rules it grades by (B-550) - stage schedule,
/// position stage, exercise default stage and "typed tests only". <c>null</c> only for an exercise type the server
/// does not know.
/// </param>
/// <param name="ForgivingDemotion">
/// Forgiving demotion (B-219): a miss drops an item only one Leitner box, a second miss in a row sends it back to box 1.
/// <c>false</c> = every miss resets to box 1. Applies within this position only - the cross-plan learning state per
/// word always resets hard.
/// </param>
/// <param name="Remediation">
/// Remediation (B-635, "Nacharbeit"): every round plays the tasks not yet done, each starting as free text; the child may
/// make a card easier step by step, and a task is done only when typed freely right. Only for a type with an ease ladder.
/// </param>
/// <param name="CompletionBonus">
/// One-off coins when the last task of a position "daily, until done" (<see cref="GoalCadence.UntilDone"/>) is done
/// (B-636); <c>0</c> = none.
/// </param>
/// <param name="CompletedOn">
/// The day the last remediation task was done (B-636); <c>null</c> = still open. A position "daily, until done" carries
/// no duty from the day after.
/// </param>
/// <param name="CurrentBuildLevel">
/// The build level the position plays (B-848): 1 or 2 = the target's prerequisite on that level, 3 = the target itself;
/// <c>null</c> = no build path. <c>ExerciseId</c>/<c>ExerciseTitle</c> stay the target's.
/// </param>
/// <param name="Build">Level n of m and the exercise played now, for the supervisor's view; <c>null</c> = no build path.</param>
/// <param name="AccentLenient">
/// Accent leniency (B-624): in practice, an answer wrong only by its diacritics ("ecole" for "école") counts half - the
/// card keeps its box, earns no coins and does not break the combo. The final test and the cross-plan learning state
/// per word stay strict. On by default.
/// </param>
public record PositionResponse(int Id, int StudyPlanId, int ExerciseId, string ExerciseTitle,
    string ExerciseType, int Order, int? Stage, int? ItemCount, ItemScope Scope, GoalCadence Cadence,
    PracticeOrder OrderStrategy, int? GoalThreshold, bool RequireTypedTest, bool UseLeitner, int MaxBox,
    List<int>? BoxIntervalDays, List<StageStep>? StageSchedule, int PointsGoalMet, int PenaltyCoins,
    int NewContentPoints, int ComboThreshold, int ComboBonusPoints, int SpeedThresholdSeconds, int SpeedBonusPoints,
    List<ScoringTimeSlot>? TimeSlots, int NewItemsPerDay, int DailyDoseInForce, LearningCheck? LearningCheck = null,
    bool ForgivingDemotion = false, bool Remediation = false, int CompletionBonus = 0, DateOnly? CompletedOn = null,
    int? CurrentBuildLevel = null, BuildPath? Build = null, bool AccentLenient = true);

/// <summary>What carries a position's goal - the honest answer to "how strictly is this duty checked?" (B-550).</summary>
public enum LearningCheckKind
{
    /// <summary>The final test asks in a typed (objectively checked) form on every day.</summary>
    Typed,
    /// <summary>The final test asks in a form the child judges itself ("Gewusst!" counts) on every day.</summary>
    SelfAssessed,
    /// <summary>
    /// The stage schedule changes the form over the plan: self-assessed or display days first, typed from
    /// <see cref="LearningCheck.TypedFromDay"/> on (<c>null</c> if the schedule never ends on typed days).
    /// </summary>
    Ladder,
    /// <summary>A free display stage on every day: there is no question, playing the round through meets the goal (B-96).</summary>
    Playthrough,
    /// <summary>The exercise type has no final test at all; playing the round through meets the goal.</summary>
    NoTest,
    /// <summary>
    /// "Typed tests only" is set, but the test asks in a self-assessed form on at least one day: an attempt there is
    /// never graded, so the goal cannot be met on such a day.
    /// </summary>
    Unreachable,
}

/// <summary>A position's learning check (B-550).</summary>
/// <param name="Kind">What meets the goal.</param>
/// <param name="TypedFromDay">For <see cref="LearningCheckKind.Ladder"/>: plan day (1-based) from which every day is typed; otherwise <c>null</c>.</param>
public record LearningCheck(LearningCheckKind Kind, int? TypedFromDay = null);

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
/// <param name="PointsGoalMet">Points when the goal is met; <c>null</c> = the exercise's recommendation when plausible (in (0, 200]), otherwise 20.</param>
/// <param name="PenaltyCoins">Coin penalty for a missed mandatory goal; <c>null</c> = 0 (pure reward, opt-in).</param>
/// <param name="NewContentPoints">Points for new content; <c>null</c> = the exercise's bonus suggestion, otherwise 10.</param>
/// <param name="ComboThreshold">Combo threshold; <c>null</c> = the exercise's bonus suggestion, otherwise 5.</param>
/// <param name="ComboBonusPoints">Combo bonus; <c>null</c> = the exercise's bonus suggestion, otherwise 5.</param>
/// <param name="SpeedThresholdSeconds">Time limit for "fast"; <c>null</c> = the exercise's bonus suggestion, otherwise 0 (off).</param>
/// <param name="SpeedBonusPoints">Bonus per fast answer; <c>null</c> = the exercise's bonus suggestion, otherwise 0.</param>
/// <param name="TimeSlots">
/// Own points time slots of this obligation (see <see cref="PositionResponse"/>); <c>null</c> = none, only the
/// global slots apply. Each slot needs <c>start &lt; end</c> and a multiplier above 0.
/// </param>
/// <param name="NewItemsPerDay">Daily dose of new contents (see <see cref="PositionResponse"/>); <c>null</c> = 0 (no cap). 0 to 200.</param>
/// <param name="ForgivingDemotion">As in <see cref="PositionResponse"/>; <c>null</c> = <c>false</c> (hard reset). Not inherited from the exercise.</param>
/// <param name="Remediation">As in <see cref="PositionResponse"/>; <c>null</c> = <c>false</c>.</param>
/// <param name="AccentLenient">As in <see cref="PositionResponse"/>; <c>null</c> = <c>true</c> (lenient, B-624 decision 4).</param>
/// <param name="CompletionBonus">As in <see cref="PositionResponse"/>; <c>null</c> = 0. 0 to 1000.</param>
/// <param name="CurrentBuildLevel">
/// Start level on the exercise's build path (B-848, see <see cref="PositionResponse"/>); <c>null</c> = no build path.
/// The exercise must have a prerequisite on that level (or any prerequisite for 3), and the position takes no daily or
/// weekly duty while on a path.
/// </param>
public record CreatePositionDto(int ExerciseId, int? Order, int? Stage, int? ItemCount, ItemScope? Scope,
    GoalCadence? Cadence, PracticeOrder? OrderStrategy, int? GoalThreshold, bool? RequireTypedTest,
    bool? UseLeitner, int? MaxBox, List<int>? BoxIntervalDays, List<StageStep>? StageSchedule,
    int? PointsGoalMet, int? PenaltyCoins, int? NewContentPoints, int? ComboThreshold, int? ComboBonusPoints,
    int? SpeedThresholdSeconds, int? SpeedBonusPoints, List<ScoringTimeSlot>? TimeSlots = null,
    int? NewItemsPerDay = null, bool? ForgivingDemotion = null, bool? Remediation = null, int? CompletionBonus = null,
    int? CurrentBuildLevel = null, bool? AccentLenient = null);

/// <summary>
/// The weak words of the plan's child to review daily (B-584): ids of vocabulary store entries the child already has
/// learning state for, 1 to 25, one language pair. Duplicates count once. The server builds a vocabulary exercise from
/// exactly these store entries and adds it to the plan as a daily position.
/// </summary>
/// <param name="VocabularyIds">The store entries, in the order the review should list them.</param>
public record CreateReviewFromWordsDto(List<int> VocabularyIds);

/// <summary>
/// Partial change to the overrides/goals/points. The referenced exercise is immutable
/// (progress indices).
///
/// <b>PATCH semantics:</b> a <c>null</c> means "not specified" for <i>every</i> field – the previous
/// value stays. It does <b>not</b> mean "reset to default"; that needs an explicit <c>Clear</c> switch, and
/// four fields have one: <see cref="ClearTimeSlots"/>, <see cref="ClearStage"/>, <see cref="ClearItemCount"/> and
/// <see cref="ClearGoalThreshold"/>. The two list fields <c>BoxIntervalDays</c> and <c>StageSchedule</c> take an
/// <b>empty</b> list as "back to the method default" instead (like <c>TimeSlots</c>). The other fields cannot be
/// emptied: they are mandatory or carry "off" as a value of their own (0).
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
/// <param name="BoxIntervalDays">Review intervals per box; an <b>empty</b> list puts it back to the method default.</param>
/// <param name="StageSchedule">Stage schedule per box; an <b>empty</b> list puts it back to the method default.</param>
/// <param name="PointsGoalMet">Points when the goal is met.</param>
/// <param name="PenaltyCoins">Coin penalty for a missed mandatory goal.</param>
/// <param name="NewContentPoints">Points for new content.</param>
/// <param name="ComboThreshold">Combo threshold.</param>
/// <param name="ComboBonusPoints">Combo bonus.</param>
/// <param name="SpeedThresholdSeconds">Time limit for "fast"; <c>0</c> = off.</param>
/// <param name="SpeedBonusPoints">Bonus per fast answer.</param>
/// <param name="TimeSlots">
/// Own points time slots of this obligation (see <see cref="PositionResponse"/>). Sending a list
/// <b>replaces</b> the stored one, an <b>empty</b> list removes it (like <c>clearTimeSlots</c>);
/// <c>null</c> leaves it untouched.
/// </param>
/// <param name="ClearTimeSlots">
/// Removes the position's own time slots, so only the global ones apply again. Needed because <c>null</c>
/// means "not specified": without the switch a form with an emptied window would report "saved" while the old
/// window kept doubling the points. Wins over <c>timeSlots</c> if both arrive together.
/// </param>
/// <param name="NewItemsPerDay">Daily dose of new contents (see <see cref="PositionResponse"/>); <c>0</c> removes the cap. 0 to 200.</param>
/// <param name="ClearStage">
/// Puts the stage back to the exercise's default (B-16). Needed because <c>null</c> means "not specified": without the
/// switch a form set back to "default" would report "saved" while the old stage stayed. Wins over <c>stage</c> if both
/// arrive together.
/// </param>
/// <param name="ClearItemCount">
/// Puts the item count back to "all items". Needed because <c>null</c> means "not specified": without the switch an
/// emptied field would report "saved" while the old count stayed. Wins over <c>itemCount</c> if both arrive together.
/// </param>
/// <param name="ClearGoalThreshold">
/// Puts the passing threshold back to the default (80 %). Wins over <c>goalThreshold</c> if both arrive together; a
/// value sent alongside is still range-checked.
/// </param>
/// <param name="ForgivingDemotion">Forgiving demotion (see <see cref="PositionResponse"/>); <c>false</c> switches it off.</param>
/// <param name="Remediation">Remediation (see <see cref="PositionResponse"/>); <c>false</c> switches it off, the done tasks stay.</param>
/// <param name="AccentLenient">Accent leniency (see <see cref="PositionResponse"/>); <c>false</c> grades accents strictly.</param>
/// <param name="CompletionBonus">Completion bonus (see <see cref="PositionResponse"/>); <c>0</c> = none. 0 to 1000.</param>
/// <param name="CurrentBuildLevel">
/// Moves the position to this level of its build path (B-848, rules as in <see cref="CreatePositionDto"/>). There is no
/// way back to "no build path" in this story; level 3 plays the target.
/// </param>
public record UpdatePositionDto(int? Order, int? Stage, int? ItemCount, ItemScope? Scope,
    GoalCadence? Cadence, PracticeOrder? OrderStrategy, int? GoalThreshold, bool? RequireTypedTest,
    bool? UseLeitner, int? MaxBox, List<int>? BoxIntervalDays, List<StageStep>? StageSchedule,
    int? PointsGoalMet, int? PenaltyCoins, int? NewContentPoints, int? ComboThreshold, int? ComboBonusPoints,
    int? SpeedThresholdSeconds, int? SpeedBonusPoints, List<ScoringTimeSlot>? TimeSlots = null,
    bool ClearTimeSlots = false, int? NewItemsPerDay = null, bool ClearStage = false,
    bool ClearItemCount = false, bool ClearGoalThreshold = false, bool? ForgivingDemotion = null, bool? Remediation = null,
    int? CompletionBonus = null, int? CurrentBuildLevel = null, bool? AccentLenient = null);
