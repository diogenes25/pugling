using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// Procedure-neutral stage/answer mechanics shared by the plan-position learning engine and the answer
/// comparison: which stage is "typed" (objectively checkable) and how an answer is normalized for
/// comparison. Deliberately stateless statics – the single source of truth for these small rules.
/// </summary>
public static class StageMechanics
{
    /// <summary>
    /// Vocabulary stages that are objectively checkable (server-side against the solution) – as
    /// opposed to pure self-assessment. Multiple choice counts as one of these: the selection is
    /// checked against the correct option.
    /// </summary>
    public static bool IsTyped(TestStage stage) =>
        stage is TestStage.LetterBoxes or TestStage.FreeText or TestStage.Audio or TestStage.MultipleChoice;

    /// <summary>
    /// Objectively checkable cloze stages – every one of them, including the word bank. Picking from a pool
    /// is an answer like any other (same reasoning as multiple choice above): the server compares it against
    /// the gap's solution. Counting the word-bank stage as self-assessment would hand the child the solution
    /// as <c>Reveal</c> and turn the pool into decoration next to it.
    /// </summary>
    public static bool IsTyped(ClozeStage stage) =>
        stage is ClozeStage.TranslationWordBank or ClozeStage.TranslationFreeText or ClozeStage.FreeText;

    /// <summary>Normalizes an answer for comparison (trim, lowercase, collapse repeated spaces).</summary>
    public static string Normalize(string? s) =>
        string.Join(' ', (s ?? "").Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    /// <summary>
    /// Multiple-choice pool built from the exercise itself: the atom's own solution plus up to
    /// <paramref name="maxDistractors"/> wrong options taken from the other atoms, rotated deterministically
    /// by index so the solution is not always in front. No randomness on purpose – the same card must offer
    /// the same pool when it is fetched twice.
    /// <para>
    /// Deduplication runs over the whole <see cref="ContentItem.AcceptedAnswers"/> set on both sides, not
    /// just over the primary answer: an answer declared equally valid must never show up as the <i>wrong</i>
    /// option of the same question. That holds even when only one of the two rows declares the equivalence –
    /// so a candidate is dropped as soon as <b>any</b> of its accepted answers has been seen.
    /// </para>
    /// <para>
    /// Shared by the vocabulary multiple-choice stage and the matching distractor stage: both ask "this
    /// solution among the neighbours' solutions", and the pool must not drift apart between them.
    /// </para>
    /// <para>
    /// <b>No pool without a distractor.</b> If no candidate survives – a single-atom exercise, or every
    /// neighbour holding the same answer – the result would be one option, and that option <i>is</i> the
    /// solution: the child taps the only button and scores. Returning <c>null</c> instead falls back to free
    /// text, which is the harder form and the honest one.
    /// </para>
    /// </summary>
    public static IReadOnlyList<string>? DistractorPool(IReadOnlyList<ContentItem> items, ContentItem item,
        int maxDistractors)
    {
        if (string.IsNullOrWhiteSpace(item.Answer)) return null;

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var accepted in item.AcceptedAnswers) seen.Add(Normalize(accepted));
        seen.Add(Normalize(item.Answer));

        var distractors = new List<string>();
        foreach (var other in items)
        {
            if (other.Index == item.Index || string.IsNullOrWhiteSpace(other.Answer)) continue;
            var candidate = other.AcceptedAnswers.Append(other.Answer).Select(Normalize).ToList();
            if (candidate.Any(seen.Contains)) continue;
            foreach (var normalized in candidate) seen.Add(normalized);
            distractors.Add(other.Answer);
            if (distractors.Count >= maxDistractors) break;
        }

        if (distractors.Count == 0) return null;

        var choices = new List<string>(distractors.Count + 1) { item.Answer };
        choices.AddRange(distractors);
        var shift = item.Index % choices.Count;
        return [.. choices.Skip(shift), .. choices.Take(shift)];
    }
}
