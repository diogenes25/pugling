namespace Pugling.Api.Models;

/// <summary>
/// A child's feedback on a card or on a whole exercise (B-628, decisions of B-623). One row per child, card and reason
/// while it is open: a repeated report raises <see cref="Count"/> and appends its text instead of adding a row
/// (decision 9). A text-only report has no <see cref="Reason"/>.
/// <para>
/// Has no effect on learning state, points, duty, penalty or the session cursor (decision 6) - a report must never be a
/// way to get rid of an answer or a card.
/// </para>
/// </summary>
public class ExerciseFeedback
{
    public int Id { get; set; }

    /// <summary>The reporting child; the feedback disappears with it.</summary>
    public int ChildId { get; set; }
    public Child? Child { get; set; }

    /// <summary>
    /// The position played when reporting. Required on the way in, but it may fade (SetNull): the observation about the
    /// card stays when the father removes the position.
    /// </summary>
    public int? PlanPositionId { get; set; }
    public PlanPosition? PlanPosition { get; set; }

    /// <summary>The exercise reported on; the feedback disappears with it.</summary>
    public int ExerciseId { get; set; }
    public Exercise? Exercise { get; set; }

    /// <summary>The card within the exercise, or <c>null</c> for the whole exercise.</summary>
    public int? ItemIndex { get; set; }

    /// <summary>
    /// The stable vocabulary item of the card, if the exercise has one. SetNull: a corrected card (a PUT replacing items)
    /// must not take the report with it (B-628, risk 1).
    /// </summary>
    public int? ExerciseItemId { get; set; }
    public ExerciseItem? ExerciseItem { get; set; }

    /// <summary>Snapshot of the card's store word, so "wrong translation: voyage" survives the correction.</summary>
    public int? VocabularyId { get; set; }

    /// <summary>
    /// The child's test answer the report is about (B-767, decision 1): its most recent stored answer to the card in a
    /// completed test of its own, frozen when the report is sent - a later answer must not change what the supervisor
    /// judges. <c>null</c> without such an answer (a report from practice only, the whole exercise, reports from before
    /// B-767); such a report cannot be judged justified. SetNull: the observation outlives its attempt.
    /// </summary>
    public int? TestItemResultId { get; set; }
    public TestItemResult? TestItemResult { get; set; }

    /// <summary>The reported reason, or <c>null</c> for a text-only report.</summary>
    public FeedbackReason? Reason { get; set; }

    /// <summary>How often the child reported this reason on this card while it was open.</summary>
    public int Count { get; set; } = 1;

    /// <summary>The child's free texts, joined by line breaks; <c>null</c> without one.</summary>
    public string? Text { get; set; }

    public FeedbackStatus Status { get; set; } = FeedbackStatus.Open;

    public DateTime CreatedAt { get; set; }
    public DateTime LastReportedAt { get; set; }

    /// <summary>When a supervisor marked it done.</summary>
    public DateTime? DoneAt { get; set; }

    /// <summary>
    /// The supervisor's judgement when marking it done (B-767, decision 2); <c>null</c> while open and for feedback marked
    /// done without one. A justified report is final: its correction is booked (B-768) and is never taken back.
    /// </summary>
    public FeedbackOutcome? Outcome { get; set; }

    /// <summary>Who marked it done; may fade.</summary>
    public int? DoneByAdultId { get; set; }
    public Adult? DoneByAdult { get; set; }

    /// <summary>When the child acknowledged the done notice; <c>null</c> = not yet seen (decision 8).</summary>
    public DateTime? SeenByChildAt { get; set; }
}
