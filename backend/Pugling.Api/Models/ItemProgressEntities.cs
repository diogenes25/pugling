namespace Pugling.Api.Models;

/// <summary>Where a recorded answer came from: free practice (Leitner), a final test or the tower game (B-851).</summary>
public enum ItemReviewSource
{
    Practice = 0,
    Test = 1,
    Game = 2,
}

/// <summary>
/// Why a typed answer was wrong (B-732) - fixed when the answer is given, so a later change to the solution or to the
/// rules does not rewrite the past. Only a wrong answer the child typed carries one: a right answer, a pick from options
/// and a self-assessment carry none. Determined by <see cref="Services.Shared.AnswerErrorClassifier"/>.
/// </summary>
public enum AnswerErrorKind
{
    /// <summary>Right but for its diacritics ("ecole" for "école").</summary>
    Accent = 1,
    /// <summary>The right word misspelled, by the distance rule of <see cref="Services.Shared.AnswerGrader.IsMisspelled"/>.</summary>
    Typo = 2,
    /// <summary>The learnt word of another store entry with the same meaning - a word the child knows, not the one asked.</summary>
    Synonym = 3,
    /// <summary>Not the word: another word, a neighbouring word of the exercise, or nothing at all.</summary>
    WrongWord = 4,
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
    /// <summary>
    /// The wrong answer was right but for its diacritics and counted half in practice (B-624). The word's learning
    /// state still records a miss; the flag lets the supervisor see whether words or only accents are missing.
    /// </summary>
    public bool AccentSlip { get; set; }
    /// <summary>
    /// Why the typed answer was wrong, fixed at the answer (B-732); <c>null</c> for a right answer, a pick, a
    /// self-assessment, a test question never answered (cursor not reached, left out of a bulk submission) and every
    /// answer recorded before the column existed. A typed empty answer is a wrong word, not null.
    /// </summary>
    public AnswerErrorKind? ErrorKind { get; set; }
    /// <summary>
    /// The cross-plan box (<see cref="ItemProgress.Box"/>) before this answer moved it (B-732), so a day's evaluation can
    /// tell a word that was consolidated from one that fell back. <c>null</c> when the answer does not count for mastery.
    /// </summary>
    public int? BoxBefore { get; set; }

    /// <summary>
    /// The learning day the answer belongs to - the day of its round or test, not of <see cref="At"/> (B-1028). A supervisor's
    /// catch-up for a past day is written now but belongs to that day; the evaluation counts answers by this day.
    /// </summary>
    public DateOnly Day { get; set; }

    public DateTime At { get; set; } = DateTime.UtcNow;
}
