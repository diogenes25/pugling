namespace Pugling.Api.Models;

/// <summary>
/// A single, stably identifiable item of a vocabulary exercise: a positioned reference to an entry of the
/// vocabulary store (<see cref="Vocabulary"/>). It supersedes the former index-based addressing of the
/// inline <c>VocabularyConfig.Items</c>/<c>Refs</c> – every item now carries its own <see cref="Id"/>
/// (the "ItemId"), so reordering/deleting can no longer tip the learning progress onto a different atom,
/// and the progress per child becomes stable per item (and – through <see cref="VocabularyId"/> – per word).
/// <para>
/// Front/back are deliberately <b>not</b> duplicated: they are properties of the referenced vocabulary entry
/// (centrally maintainable, live) and are only read from the store when the content is resolved.
/// <see cref="Hint"/> is an optional exercise-local hint that overrides the derived store hint (e.g. the article).
/// </para>
/// </summary>
public class ExerciseItem
{
    public int Id { get; set; }

    /// <summary>Exercise the item belongs to (vocabulary exercises only); disappears with it (cascade).</summary>
    public int ExerciseId { get; set; }
    public Exercise? Exercise { get; set; }

    /// <summary>
    /// Order within the exercise. Corresponds to the study plan engine's previous <c>ItemIndex</c>
    /// (0-based, gapless, unique per exercise) – that keeps existing Leitner/test progress valid.
    /// </summary>
    public int OrderIndex { get; set; }

    /// <summary>Referenced vocabulary store entry (word/translation/audio come from there).</summary>
    public int VocabularyId { get; set; }
    public Vocabulary? Vocabulary { get; set; }

    /// <summary>Optional exercise-local hint; <c>null</c> = derived store hint (e.g. the article).</summary>
    public string? Hint { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
