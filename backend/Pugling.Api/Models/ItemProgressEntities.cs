namespace Pugling.Api.Models;

/// <summary>Where a recorded answer came from: free practice (Leitner) or a final test.</summary>
public enum ItemReviewSource
{
    Practice = 0,
    Test = 1,
}

/// <summary>
/// A child's cross-plan learning state for a single exercise item (vocabulary pair). Unlike
/// <see cref="PositionItemProgress"/> (Leitner review scheduling per plan position), this state hangs on the
/// stable <see cref="ExerciseItem.Id"/> (the "ItemId") and carries the <see cref="VocabularyId"/>
/// denormalized alongside – so progress can be evaluated both per exercise item and per word (across
/// exercises). Exactly one row per (child, item); rolled forward from the recorded answers (see
/// <see cref="ItemReviewEvent"/>).
/// </summary>
public class ItemProgress
{
    public int Id { get; set; }

    public int ChildId { get; set; }
    public Child? Child { get; set; }

    /// <summary>The exercise item; disappears with it (cascade).</summary>
    public int ItemId { get; set; }
    public ExerciseItem? Item { get; set; }

    /// <summary>Denormalized: the item's exercise (filter "progress within this exercise").</summary>
    public int ExerciseId { get; set; }
    /// <summary>Denormalized: the referenced store vocabulary entry (rollup "how well does this word sit across all exercises?").</summary>
    public int VocabularyId { get; set; }

    /// <summary>Current Leitner box (1 = new/hard … <see cref="MaxBox"/> = safe).</summary>
    public int Box { get; set; } = 1;
    /// <summary>Highest box of this aggregated state (fixed, independent of any plan).</summary>
    public const int MaxBox = 5;
    /// <summary>Shared evaluation threshold: below this mastery (percent) an item/word counts as "weak".</summary>
    public const int WeakBelowPercent = 50;
    /// <summary>Mastery in percent, derived from <see cref="Box"/> (for simple evaluation/sorting).</summary>
    public int MasteryPercent { get; set; }

    /// <summary>How often the item has been answered so far (practice + test).</summary>
    public int SeenCount { get; set; }
    /// <summary>Of those, answered correctly.</summary>
    public int CorrectCount { get; set; }

    /// <summary>Day of the first answer (initial introduction).</summary>
    public DateOnly? IntroducedAt { get; set; }
    /// <summary>Instant of the last answer.</summary>
    public DateTime? LastAnswerAt { get; set; }
    /// <summary>Whether the last answer was correct.</summary>
    public bool? LastCorrect { get; set; }
}

/// <summary>
/// A single recorded answer of a child to an item – the history behind <see cref="ItemProgress"/>.
/// Carries <see cref="ExerciseId"/>/<see cref="VocabularyId"/> denormalized so that the word history is
/// preserved even when the item is deleted later (<see cref="ItemId"/> is then set to <c>null</c>).
/// </summary>
public class ItemReviewEvent
{
    public int Id { get; set; }

    public int ChildId { get; set; }
    public Child? Child { get; set; }

    /// <summary>The exercise item; <c>null</c> if it was deleted after the answer (the history stays for the word rollup).</summary>
    public int? ItemId { get; set; }
    public ExerciseItem? Item { get; set; }

    /// <summary>Denormalized: the item's exercise.</summary>
    public int ExerciseId { get; set; }
    /// <summary>Denormalized: the referenced store vocabulary entry (word rollup, survives item deletion).</summary>
    public int VocabularyId { get; set; }
    /// <summary>Optional context: the study plan position the item was practiced/tested through.</summary>
    public int? PlanPositionId { get; set; }

    /// <summary>Origin of the answer (practice or test).</summary>
    public ItemReviewSource Source { get; set; }
    /// <summary>The server-enforced stage the answer was given on.</summary>
    public int StageValue { get; set; }
    /// <summary>The answer given (on typed stages); <c>null</c> for pure self-assessment.</summary>
    public string? GivenAnswer { get; set; }
    /// <summary>Whether the answer was correct.</summary>
    public bool WasCorrect { get; set; }

    public DateTime At { get; set; } = DateTime.UtcNow;
}
