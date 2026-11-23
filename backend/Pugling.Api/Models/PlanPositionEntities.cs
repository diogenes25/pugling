namespace Pugling.Api.Models;

// Study plan model: a plan is a method-MIXED composition of positions. Every position references a catalog
// exercise and carries its OWN goals (daily/weekly cadence) and points. The content lives solely in the
// exercise config; only the learning PROGRESS per content atom is materialized here (PositionItemProgress).
//
// The strangler is finished: the former plan-wide StudyPlanItem/Method model was removed completely - there
// is no legacy model left that anything here would run "additively" beside.

// GoalCadence/ItemScope/PracticeOrder live in the contract project (Pugling.Contracts).

/// <summary>
/// One position within a <see cref="StudyPlan"/>: it references a catalog <see cref="Exercise"/> and defines
/// HOW it is played within the plan (overrides), WHICH goal applies (cadence + threshold) and HOW points
/// flow. Empty override fields inherit the exercise's suggestion (hybrid principle).
/// </summary>
public class PlanPosition
{
    public int Id { get; set; }
    public int StudyPlanId { get; set; }
    public StudyPlan? StudyPlan { get; set; }

    /// <summary>Referenced catalog exercise – the content stays there (no copy in any store).</summary>
    public int ExerciseId { get; set; }
    public Exercise? Exercise { get; set; }

    /// <summary>Order within the plan (grouping by subject follows from the exercise).</summary>
    public int Order { get; set; }

    /// <summary>
    /// The build level the position plays (B-848): 1 or 2 plays the prerequisite of <see cref="Exercise"/> with that
    /// <see cref="Models.Exercise.BuildLevel"/>, <see cref="Models.Exercise.BuildLevelTarget"/> plays the target itself.
    /// <c>null</c> = no build path, the position plays its exercise as always. The one reader is
    /// <c>PositionPlayService.PlayedExercise</c>.
    /// </summary>
    public int? CurrentBuildLevel { get; set; }

    // --- Overrides (null = inherit the exercise's suggestion) ---
    /// <summary>Overridden test stage (interpreted per method); null = the exercise's default.</summary>
    public int? Stage { get; set; }
    /// <summary>How many of the exercise's contents are used; null = all of them.</summary>
    public int? ItemCount { get; set; }
    /// <summary>
    /// Daily dose (B-04): how many never-shown contents a learn round may bring per day; 0 = no cap, every unseen
    /// content is due at once. With a dose one position carries a pool for weeks - the round is today's new ones
    /// plus the known ones Leitner made due, and the test checks that portion instead of the whole pool.
    /// </summary>
    public int NewItemsPerDay { get; set; }
    /// <summary>Scope of the content selection (all/new/old).</summary>
    public ItemScope Scope { get; set; } = ItemScope.All;
    /// <summary>
    /// The order in which the server plays out the (due) contents (frozen when the session/test starts).
    /// Default <see cref="PracticeOrder.WeakestFirst"/> = the previous behavior.
    /// </summary>
    public PracticeOrder OrderStrategy { get; set; } = PracticeOrder.WeakestFirst;

    // --- Goal ---
    /// <summary>Goal cadence; <see cref="GoalCadence.None"/> = free practice without any obligation.</summary>
    public GoalCadence Cadence { get; set; } = GoalCadence.None;
    /// <summary>
    /// Pass threshold of the period in <b>percent</b>; <c>null</c> = 80 %. The sentence is always the same –
    /// "what percentage do you have to manage" – only the yardstick follows the exercise's
    /// <c>ExerciseCheckMode</c>:
    /// <list type="bullet">
    /// <item>checkable methods (test/catalog check): percentage of <b>correct answers</b> in the final test;</item>
    /// <item>pure content exercises (<c>ExerciseCheckMode.None</c>): percentage of the round that was
    /// <b>played</b> (cursor against the frozen order).</item>
    /// </list>
    /// <para>
    /// The unit is <b>type-agnostic</b> – for catalog-check methods too. That is not a simplification but
    /// follows from the fact that a <see cref="TestAttempt"/> is only ever created in the position test and
    /// that <c>PositionProgressService.IsGoalMetAsync</c> measures the goal of every checkable type against a
    /// passed attempt: there simply is no second path that could evaluate a different unit. An absolute number
    /// of hits would also be redundant here – how large the pool is, is already stated by
    /// <see cref="ItemCount"/>.
    /// </para>
    /// <para>
    /// For content exercises the value used to be unused: the goal already counted as done after one learning
    /// session with <i>any</i> activity – a heartbeat of 12 seconds was enough to fulfill the obligation and
    /// trigger the goal points. That is exactly why the value now carries a yardstick there as well.
    /// </para>
    /// </summary>
    public int? GoalThreshold { get; set; }
    /// <summary>
    /// Does a test only count as passed on a "graded" (typed/free-text) stage?
    /// Prevents mere clicking/selecting. Only relevant for test-capable methods.
    /// </summary>
    public bool RequireTypedTest { get; set; }

    // --- Points (default from the exercise's bonus suggestion, overridable per position here) ---
    /// <summary>Points for reaching the position's goal within its period.</summary>
    public int PointsGoalMet { get; set; } = 20;
    /// <summary>
    /// Coin <b>penalty</b> that is deducted when the mandatory goal (<see cref="Cadence"/> daily/weekly) was
    /// <b>missed</b> in a closed period – the "stick" against not learning. 0 = no penalty (reward only).
    /// Only effective for <see cref="GoalCadence.Daily"/>/<see cref="GoalCadence.Weekly"/>.
    /// Debt is allowed: the coin balance may go negative because of it.
    /// </summary>
    public int PenaltyCoins { get; set; }
    /// <summary>Base points for a content repeated for the first time (new content) – "new material counts most".</summary>
    public int NewContentPoints { get; set; } = 10;
    /// <summary>Every N correct answers in a row yields a combo bonus. 0 = off.</summary>
    public int ComboThreshold { get; set; } = 5;
    /// <summary>Base bonus points per combo milestone; escalating (Nth milestone → base × N). 0 = off.</summary>
    public int ComboBonusPoints { get; set; } = 5;
    /// <summary>Maximum seconds for a "fast answer"; 0 = feature off.</summary>
    public int SpeedThresholdSeconds { get; set; }
    /// <summary>Bonus points for a fast answer. 0 = off.</summary>
    public int SpeedBonusPoints { get; set; }
    /// <summary>
    /// Time slots with their own points multiplier for <b>this</b> obligation ("homework counts double between
    /// 13:00 and 15:00"); <c>null</c> = only the global slots from the configuration apply.
    /// <para>
    /// The carrier is the position and not the child, and that is the statement: a window is an assertion about
    /// <i>this</i> task, not about the child around the clock - evening vocabulary practice stays untouched.
    /// </para>
    /// <para>
    /// The slots are considered <b>together with</b> the global ones and follow the existing ordering (the
    /// narrowest wins); they neither replace them nor multiply with them. <c>Scoring:TimeSlotsEnabled=false</c>
    /// switches these off as well.
    /// </para>
    /// <para>
    /// A list even though the form currently only offers one window: the storage stays list-capable, so a later
    /// extension to several windows costs UI only and <b>no</b> migration.
    /// </para>
    /// </summary>
    public List<ScoringTimeSlot>? TimeSlots { get; set; }

    // --- Leitner review (only for drillable methods such as vocabulary/cloze/matching) ---
    /// <summary>Enables the Leitner box scheduling of this position.</summary>
    public bool UseLeitner { get; set; }
    /// <summary>
    /// Forgiving demotion (B-219): a single miss drops the item only one box, a second miss in a row sends it back to
    /// box 1. Off by default - the hard reset is the father's baseline, he opts in per obligation. Position-only: the
    /// cross-plan learning state per word stays hard so it remains an honest measurement.
    /// </summary>
    public bool ForgivingDemotion { get; set; }
    /// <summary>
    /// Remediation (B-635, "Nacharbeit"): the round plays every task not yet done, in order, each starting as free text;
    /// the child may make the current card easier step by step, and a task is done only when typed freely right. Rounds
    /// repeat until every task is done. Only for a type with an ease ladder (<see cref="IExerciseType.EaseLadder"/>).
    /// </summary>
    public bool Remediation { get; set; }
    /// <summary>
    /// One-off coins for the last remediation task done (B-636), booked once via <see cref="PositionCompletionBonus"/> and
    /// only on <see cref="GoalCadence.UntilDone"/>. 0 = none.
    /// </summary>
    public int CompletionBonus { get; set; }
    /// <summary>
    /// The day the last remediation task was done (B-636). On <see cref="GoalCadence.UntilDone"/> the duty ends after this
    /// day: no penalty and no place in the day's mission from the next day on, also for days settled only later.
    /// </summary>
    public DateOnly? CompletedOn { get; set; }
    /// <summary>
    /// The UTC day the position became a duty (B-613): its creation day, or the day a free position was switched to a
    /// cadence. A duty counts for streak, missions, timeline and penalty only from then on - a free day before cannot
    /// have been missed. Set only on the switch from <see cref="GoalCadence.None"/>; a mere save keeps it.
    /// <c>default</c> (never set) falls back to <see cref="CreatedAt"/>.
    /// </summary>
    public DateOnly DutySince { get; set; }
    /// <summary>
    /// The last day (inclusive) of a duty that was switched to <see cref="GoalCadence.None"/> (B-659), together with
    /// <see cref="CadenceUntil"/>. The child's met days stay met when the father frees a position later: the history reads
    /// the old cadence up to this day. <c>null</c> = never freed, or a duty again since (the switch back clears it).
    /// Only one earlier duty phase is kept; a second switch to free overwrites it.
    /// </summary>
    public DateOnly? DutyUntil { get; set; }
    /// <summary>The cadence that held up to <see cref="DutyUntil"/> (B-659); <c>null</c> together with it.</summary>
    public GoalCadence? CadenceUntil { get; set; }
    /// <summary>Highest box (default 5).</summary>
    public int MaxBox { get; set; } = 5;
    /// <summary>Interval in days per box (index = box; index 0 unused). Null = the default <c>[0,1,2,4,7,14]</c>.</summary>
    public List<int>? BoxIntervalDays { get; set; }
    /// <summary>Optional stage schedule (day → stage); raises the difficulty over the plan's runtime.</summary>
    public List<StageStep>? StageSchedule { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Materialized Leitner/introduction progress per content atom of this position.</summary>
    public List<PositionItemProgress> ItemProgress { get; set; } = new();
}

/// <summary>
/// Records the <b>one-off</b> points credit for a reached position goal per period – the position counterpart
/// to the idempotent daily reward. It prevents the goal points
/// (<see cref="PlanPosition.PointsGoalMet"/>) from flowing twice when the same position is
/// completed/requested several times within the same period.
/// <para>
/// The period is <b>(<see cref="Cadence"/>, <see cref="PeriodStart"/>)</b>, and the cadence explicitly belongs
/// to it: it is a <b>snapshot</b> of the position at the time of the entry. Without it a switch from daily to
/// weekly would reinterpret periods booked earlier – the reward for a Monday as a daily goal would silently
/// reject the week starting on that Monday as "already paid".
/// </para>
/// <para>
/// <see cref="PeriodStart"/> is <b>not</b> the same as <see cref="Day"/>: for a weekly goal reached on a
/// Wednesday, the Monday sits in one field and the Wednesday in the other. Both are needed – the day for the
/// daily/streak metrics, the period for idempotency.
/// </para>
/// </summary>
public class PositionGoalReward
{
    public int Id { get; set; }
    public int PlanPositionId { get; set; }
    public PlanPosition? PlanPosition { get; set; }
    /// <summary>Cadence of the position at the time of the entry (snapshot – see the class documentation).</summary>
    public GoalCadence Cadence { get; set; }
    /// <summary>First day of the rewarded period: the day itself for a daily goal, the Monday for a weekly goal.</summary>
    public DateOnly PeriodStart { get; set; }
    /// <summary>Calendar day the goal was reached on (the basis of the daily/streak metrics).</summary>
    public DateOnly Day { get; set; }
    public int Points { get; set; }
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// The one-off completion bonus of a remediation position "daily, until done" (B-636). A position finishes only once, so
/// the unique index on <see cref="PlanPositionId"/> is the guarantee against a second booking - the same role
/// <see cref="PositionGoalReward"/> plays per period.
/// </summary>
public class PositionCompletionBonus
{
    public int Id { get; set; }
    public int PlanPositionId { get; set; }
    public PlanPosition? PlanPosition { get; set; }
    /// <summary>The day the last task was done.</summary>
    public DateOnly Day { get; set; }
    /// <summary>Coins booked (the position's <see cref="PlanPosition.CompletionBonus"/> at that moment).</summary>
    public int Points { get; set; }
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Records the <b>one-off</b> coin penalty for a <b>missed</b> mandatory position goal per period – the
/// negative counterpart to <see cref="PositionGoalReward"/>. A unique index on
/// <c>(PlanPositionId, Cadence, PeriodStart)</c> guarantees that the penalty
/// (<see cref="PlanPosition.PenaltyCoins"/>) is deducted at most once per period – even if the lazy settlement
/// runs over the same closed period several times. The period is built exactly as for the reward, including
/// the snapshot of the cadence (rationale there).
/// </summary>
public class PositionGoalPenalty
{
    public int Id { get; set; }
    public int PlanPositionId { get; set; }
    public PlanPosition? PlanPosition { get; set; }
    /// <summary>Cadence of the position at the time of the entry (snapshot, see <see cref="PositionGoalReward"/>).</summary>
    public GoalCadence Cadence { get; set; }
    /// <summary>First day of the missed period: the day itself for a daily goal, the Monday for a weekly goal.</summary>
    public DateOnly PeriodStart { get; set; }
    /// <summary>Last day of the missed period (the day itself, or the week's Sunday) – for reporting.</summary>
    public DateOnly Day { get; set; }
    /// <summary>Coins deducted (a positive value; the ledger entry is negative).</summary>
    public int Points { get; set; }
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Learning progress of a single content atom (e.g. one vocabulary pair) within a
/// <see cref="PlanPosition"/>. Created lazily on the first introduction – the content itself stays in the
/// exercise config, here only the Leitner box/introduction state per child is kept (one plan = one child).
/// </summary>
public class PositionItemProgress
{
    public int Id { get; set; }
    public int PlanPositionId { get; set; }
    public PlanPosition? PlanPosition { get; set; }

    /// <summary>
    /// The exercise this progress was made on (B-848): the position's exercise, or the prerequisite it played on a build
    /// path. One position keeps a separate state per exercise, so a word learnt on the easier level is not counted as
    /// known on the target. Restrict: a played prerequisite is not deleted with the child's progress on it.
    /// </summary>
    public int ExerciseId { get; set; }
    public Exercise? Exercise { get; set; }

    /// <summary>Index of the content in the item list of the referenced exercise.</summary>
    public int ItemIndex { get; set; }

    /// <summary>Current Leitner box (1 = new/hard … MaxBox = safe).</summary>
    public int Box { get; set; } = 1;
    /// <summary>Day the content is next due. Null = due immediately (never reviewed yet).</summary>
    public DateOnly? DueOn { get; set; }
    /// <summary>How often this content has been reviewed through Leitner so far.</summary>
    public int ReviewCount { get; set; }
    /// <summary>Instant of the last Leitner review.</summary>
    public DateTime? LastReviewedAt { get; set; }
    /// <summary>When the content was first introduced as "new". Null = not introduced yet.</summary>
    public DateOnly? IntroducedAt { get; set; }
    /// <summary>
    /// B-643: the day the word was first written from memory, right and without help, on its learn card ("eingeprägt").
    /// Until then the word comes back as a learn card, every day and round after round. Only the card selection and the
    /// daily dose read it; due dates, new-content points and metrics stay on <see cref="IntroducedAt"/>. A content whose
    /// introduction is no learn card never needs it (see <c>PositionPlayService.NeedsImprint</c>).
    /// </summary>
    public DateOnly? ImprintedAt { get; set; }
    /// <summary>
    /// When the content paid its one-time new-content points (B-535). Null = not paid yet: "new" lasts until the first
    /// graded HIT, not until the first contact - free rungs and a wrong first try must not use the bonus up.
    /// </summary>
    public DateTime? NewBonusPaidAt { get; set; }
    /// <summary>
    /// Whether the last graded review of this content was a miss (B-219). Kept on every review, also with forgiving
    /// demotion off: switching it on mid-plan must see the real history, or a word missed yesterday would be forgiven twice.
    /// </summary>
    public bool LastMissed { get; set; }
    /// <summary>
    /// Remediation (B-635): the day the task was first typed freely right, without being made easier. <c>null</c> = still
    /// open - the next round brings it again. Never set by an answer on an easier stage, whatever the client sends.
    /// </summary>
    public DateOnly? RemediationDoneAt { get; set; }
    /// <summary>
    /// Remediation (B-635, read by the father's view B-637): how often the task was solved right, per stage it was played
    /// at (stage value → count). Kept across days; <c>null</c> = never solved. Reassign, do not mutate in place.
    /// </summary>
    public Dictionary<int, int>? SolvedByStage { get; set; }
}
