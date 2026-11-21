namespace Pugling.Api.Models;

// Study plan model: a plan is a pure container of referenced catalog exercises (see PlanPosition). Time,
// points, Leitner control, stages and goals hang on the individual position, no longer on the plan.
// Method-specific are only the content (exercise config) and the test mechanics/stages (see
// PositionPlayService / PositionTestsController).

// LearningMethod lives in the contract project (Pugling.Contracts).

/// <summary>
/// Stage of the matching method (increasing difficulty). It uses the vocabulary store.
/// <para>
/// The enum is effective as of B-73: <c>MatchingExerciseType.Choices</c> branches on it and offers a pool of
/// counterparts on <see cref="Distractors"/>, nothing on <see cref="Direct"/>. <c>IsTypedStage</c> stays
/// unoverridden on purpose – matching is checked against the pair on either stage, so the base class already
/// answers <c>true</c> for both.
/// The two reverse stages (<c>Reverse</c>, <c>ReverseDistractors</c>) are gone because they appeared nowhere.
/// </para>
/// <para>
/// <b>Still missing a way in:</b> <c>StageOptions</c> remains empty, so no stage picker offers
/// <see cref="Distractors"/> by name. Four sources feed the value nonetheless, in the order
/// <c>PositionPlayService.StageForDay</c> applies them: <see cref="PlanPosition.StageSchedule"/> →
/// <see cref="PlanPosition.Stage"/> → <c>Exercise.DefaultStage</c> → the type default. The first three are
/// settable per request and none of them is validated against <c>StageOptions</c> (B-79), so a plain "2"
/// meant for a vocabulary position turns a matching position into multiple choice.
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
    /// <summary>Dictation (B-597): the learnt word is read out loud -> type that word.</summary>
    Audio = 5,
    /// <summary>Choice from several options (one correct, the rest distractors from the exercise).</summary>
    MultipleChoice = 6,
    /// <summary>
    /// Put the scrambled letters of the translation in order (B-05). Words of 2-12 letters each, scrambled word by word
    /// (B-634); any other answer is played as <see cref="LetterBoxes"/>, card by card.
    /// </summary>
    LetterScramble = 7,
    /// <summary>
    /// Learn card (B-598): the first encounter of a word in four steps - German shown, uncover the learnt word and say
    /// whether it was known, copy it, then write it from memory. Never scores and moves no box; the test never plays it.
    /// </summary>
    LearnCard = 8,
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
    /// <summary>
    /// The subject the plan covers: the group of the activation rule "one active plan per subject and child" (B-539).
    /// Mandatory in the contract, nullable in the column: a deleted subject (SetNull) or a legacy plan whose positions
    /// give no majority subject leaves a plan without one until the supervisor edits it.
    /// </summary>
    public int? SubjectId { get; set; }
    public Subject? Subject { get; set; }
    /// <summary>Optional unit of a textbook series the plan works through; its series fixes the plan's subject.</summary>
    public int? SeriesUnitId { get; set; }
    public SeriesUnit? SeriesUnit { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool Active { get; set; } = true;
    /// <summary>
    /// The UTC day the plan was last switched on (B-613): <see cref="StartDate"/> on creation, today on a switch from
    /// inactive to active; a moved start of an active plan clamps it between <c>min(old start, today)</c> and
    /// <c>max(new start, today)</c> (B-672). Days before it count like days of an inactive plan - only in the child's favour - for the
    /// streak and the penalty, because the child could not play the plan then. Only the last switch is known; the
    /// blur always goes in the child's favour. <c>default</c> (never set) means "active since the start".
    /// </summary>
    public DateOnly ActivatedAt { get; set; }
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
    /// <summary>
    /// The stage the child made a card easier to (B-635), by its place in <see cref="Order"/> - a place, not an item, so
    /// a card that comes back later in the round starts over. Only the server writes it (the "easier" call, one rung at a
    /// time); grading reads it, so a client cannot claim the free-text stage for an eased card. <c>null</c> = none eased.
    /// Reassign, do not mutate in place.
    /// </summary>
    public Dictionary<int, int>? EasedStages { get; set; }

    /// <summary>
    /// When the card after the latest answer became visible (B-530), stamped by the server's clock on the client's
    /// "shown" call. The fast-answer bonus measures from here instead of from the previous answer, so reading the
    /// feedback sheet does not count as answer time. One stamp per card: it is only set while it is not later than the
    /// latest answer. <c>null</c> (old client, lost call) keeps the old measurement from the previous answer.
    /// </summary>
    public DateTime? CardShownAt { get; set; }

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
