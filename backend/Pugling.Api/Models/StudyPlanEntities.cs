namespace Pugling.Api.Models;

// Study plan model: a plan is a pure container of referenced catalog exercises (see PlanPosition). Time,
// points, Leitner control, stages and goals hang on the individual position, no longer on the plan.
// Method-specific are only the content (exercise config) and the test mechanics/stages (see
// PositionPlayService / PositionTestsController).

// LearningMethod lives in the contract project (Pugling.Contracts).

/// <summary>
/// Stage of the matching method (increasing difficulty). It uses the vocabulary store.
/// <para>
/// <b>Careful, only half implemented:</b> <c>MatchingExerciseType</c> overrides neither <c>StageOptions</c>
/// nor <c>IsTypedStage</c> nor <c>Choices</c> – so there is no code branching on this enum.
/// <see cref="PlanPosition.Stage"/> is stored for matching positions and ignored during delivery.
/// The two reverse stages (<c>Reverse</c>, <c>ReverseDistractors</c>) are gone because they appeared nowhere;
/// the remaining two stay because <c>Direct</c> is used as <c>DefaultStage</c> and <c>Distractors</c> is set in
/// the seed. Actually making the enum effective is a behavioral rebuild, not a structural step.
/// </para>
/// </summary>
public enum MatchStage
{
    /// <summary>Word → translation, no distractors.</summary>
    Direct = 1,
    /// <summary>Word → translation, with additional distractors in the choice pool.</summary>
    Distractors = 2,
}

/// <summary>Test stage of the vocabulary flashcard test (increasing difficulty).</summary>
public enum TestStage
{
    /// <summary>Word and translation are both shown (getting acquainted).</summary>
    ShowBoth = 1,
    /// <summary>Word -> reveal -> self-assessment "did you know it? yes/no".</summary>
    SelfAssess = 2,
    /// <summary>Type the translation; the length is known (letter boxes), letter hints are possible.</summary>
    LetterBoxes = 3,
    /// <summary>Type the translation freely.</summary>
    FreeText = 4,
    /// <summary>The word is read out loud -> type the translation freely.</summary>
    Audio = 5,
    /// <summary>Choice from several options (one correct, the rest distractors from the exercise).</summary>
    MultipleChoice = 6,
}

// StageStep lives in the contract project (Pugling.Contracts).

/// <summary>
/// Study plan created by the supervisor for a child: a <b>container</b> that bundles catalog exercises as
/// <see cref="PlanPosition"/>s. Title, child and runtime belong here; everything learning-specific
/// (goal, points, stage, Leitner) is carried by the individual position.
/// </summary>
public class StudyPlan
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    public string Title { get; set; } = "";
    /// <summary>Free description of the plan (optional): goal/scope, so it stays recognizable later.</summary>
    public string? Description { get; set; }
    /// <summary>Optional link to the catalog subject (for classification/filtering only).</summary>
    public int? SubjectId { get; set; }
    public Subject? Subject { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>The plan's positions: referenced catalog exercises with their own goal/points/Leitner.</summary>
    public List<PlanPosition> Positions { get; set; } = new();
}

// PlayMode lives in the contract project (Pugling.Contracts).

/// <summary>Practice session of a study plan position: records real practice time and what was practiced.</summary>
public class PracticeSession
{
    public int Id { get; set; }
    public int StudyPlanId { get; set; }
    public StudyPlan? StudyPlan { get; set; }
    /// <summary>Position (exercise) the session belongs to.</summary>
    public int? PlanPositionId { get; set; }
    public PlanPosition? PlanPosition { get; set; }
    public DateOnly Day { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    /// <summary>Seconds actively practiced (only time with interaction).</summary>
    public int ActiveSeconds { get; set; }

    /// <summary>Playback mode (Info = free, Lern = server-driven with a cursor).</summary>
    public PlayMode Mode { get; set; } = PlayMode.Lern;
    /// <summary>
    /// Play-out order (item indexes) frozen at the start according to <see cref="PlanPosition.OrderStrategy"/>.
    /// It stays stable over the run so that the order does not shift because of box changes.
    /// </summary>
    public List<int> Order { get; set; } = new();
    /// <summary>Current position within <see cref="Order"/> (the server-driven cursor in learn mode).</summary>
    public int Cursor { get; set; }

    public List<ReviewEvent> Reviews { get; set; } = new();
}

/// <summary>
/// A single review within a practice session (type-agnostic). Deliberately narrow: only
/// <see cref="WasCorrect"/> and <see cref="At"/> are read – from them come the combo streak and the answer
/// time (see <c>PositionPracticeController.Review</c>) as well as the metric <c>CorrectReviews</c>.
/// <para>
/// What the atom was is <b>not</b> recorded here: <see cref="ItemReviewEvent"/> with its stable <c>ItemId</c>
/// exists for that. The former fields <c>ContentId</c> (an FK-less copy of
/// <see cref="PlanPosition.ExerciseId"/>), <c>ItemIndex</c> and <c>StageValue</c> were written and read by
/// nobody – a second, index-addressed truth without consumers.
/// </para>
/// </summary>
public class ReviewEvent
{
    public int Id { get; set; }
    public int PracticeSessionId { get; set; }
    public PracticeSession? PracticeSession { get; set; }
    public bool WasCorrect { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
}

/// <summary>One final-test attempt of a position on a given day (type-agnostic).</summary>
public class TestAttempt
{
    public int Id { get; set; }
    public int StudyPlanId { get; set; }
    public StudyPlan? StudyPlan { get; set; }
    /// <summary>Position (exercise) the test belongs to.</summary>
    public int? PlanPositionId { get; set; }
    public PlanPosition? PlanPosition { get; set; }
    public DateOnly Day { get; set; }
    /// <summary>Stage (TestStage or ClozeStage, depending on the method).</summary>
    public int StageValue { get; set; }
    /// <summary>Does this attempt count as "graded" (typed/free text)? Set by the controller.</summary>
    public bool Graded { get; set; }
    /// <summary>
    /// Was this attempt started by a supervisor (preview/catch-up) instead of the child? The child's rules do
    /// not apply to such an attempt – it picks its stage freely – so it must stay out of the child's world:
    /// it is neither resumed by the child (who would then be examined at a foreign stage) nor counted against
    /// the child's daily attempt cap (two father previews would otherwise burn the child's whole day).
    /// </summary>
    public bool BySupervisor { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int TotalItems { get; set; }
    public int CorrectItems { get; set; }
    public int ScorePercent { get; set; }
    public bool Passed { get; set; }

    /// <summary>
    /// Examination order (item indexes) frozen at the start according to <see cref="PlanPosition.OrderStrategy"/>.
    /// The class-test mode is strictly server-driven: one question after another, no going back.
    /// </summary>
    public List<int> Order { get; set; } = new();
    /// <summary>Current position within <see cref="Order"/> (the server-driven cursor of the examination).</summary>
    public int Cursor { get; set; }

    public List<TestItemResult> Results { get; set; } = new();
}

/// <summary>One result line of a test attempt – usually one content atom of the exercise.</summary>
public class TestItemResult
{
    public int Id { get; set; }
    public int TestAttemptId { get; set; }
    public TestAttempt? TestAttempt { get; set; }
    /// <summary>
    /// Index of the content atom within the position's exercise – or <c>null</c> for a <b>wrong mention</b>:
    /// in a set-graded exercise (an unordered list) an answer that matches no open entry belongs to no atom.
    /// <para>
    /// That state carries meaning, so it must never be coalesced away: a <c>?? 0</c> would turn such a line
    /// into an answer to the first entry and let it score against it. Every reader filters on
    /// <c>ItemIndex is not null</c> instead.
    /// </para>
    /// </summary>
    public int? ItemIndex { get; set; }
    public int StageValue { get; set; }
    public string? GivenAnswer { get; set; }
    public bool WasCorrect { get; set; }
    /// <summary>
    /// Letter hints used. <b>Set by no path</b> and therefore always 0 – the column only remains because it is
    /// part of the contract through <c>ItemResultDto</c>; removing it would break the contract and therefore
    /// does not belong in a purely structural rebuild. Either fill it (the hints do exist during delivery) or
    /// drop it together with the DTO.
    /// </summary>
    public int HintsUsed { get; set; }
}
