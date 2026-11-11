namespace Pugling.Agent.Creator.Briefing;

/// <summary>A word pair of the unit, as the vocabulary store holds it.</summary>
/// <param name="Word">The word in the language being learned.</param>
/// <param name="Translation">Its translation - the one the book gives, not one a model guessed.</param>
/// <param name="VocabularyId">The store entry, so that a new exercise links the pair instead of copying it.</param>
/// <param name="Hint">The hint the unit's exercise gives (irregular forms, "no pl") - the fallback when the model writes none.</param>
public sealed record VocabPair(string Word, string Translation, int VocabularyId, string? Hint = null);

/// <summary>
/// A vocabulary exercise that is part of the unit's word list: created by a person or a loader script, not by the
/// agent, and adding at least one word the lists before it do not hold.
/// </summary>
/// <param name="ExerciseId">The exercise a study plan assigns as it is.</param>
/// <param name="Title">Its title, for the report.</param>
/// <param name="ItemCount">How many entries a position on it plays - the base of the daily dose.</param>
public sealed record UnitWordList(int ExerciseId, string Title, int ItemCount);

/// <summary>
/// The unit's word list, read from the vocabulary exercises already in it, and the slice a single exercise uses.
/// <para>
/// This is the part of the material a program can decide better than a model: in the first study-plan run the model
/// invented its own translations and lost two of four exercises to pairs like "test = Test", while the unit held 124
/// pairs taken from the book. Which ten words an exercise practices is selection, not creativity.
/// </para>
/// </summary>
public static class UnitVocabulary
{
    /// <summary>
    /// The pairs for one exercise: first the child's weakly mastered words that belong to the unit, then the unit's
    /// pairs from <paramref name="part"/> × <paramref name="count"/> on, wrapping around at the end of the list.
    /// </summary>
    public static IReadOnlyList<VocabPair> Slice(IReadOnlyList<VocabPair> pool, IReadOnlyCollection<string> weakWords,
        int count, int part)
    {
        if (pool.Count == 0 || count <= 0) return [];

        var weak = pool.Where(p => weakWords.Contains(p.Word, StringComparer.OrdinalIgnoreCase)).ToList();
        var start = (int)((long)Math.Max(part, 0) * count % pool.Count);
        var rotated = pool.Skip(start).Concat(pool.Take(start));

        return [.. weak.Concat(rotated)
            .DistinctBy(p => VocabKey.Part(p.Word), StringComparer.OrdinalIgnoreCase)
            .Take(Math.Min(count, pool.Count))];
    }
}
