using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// A single practicable/checkable element of an exercise, projected type-agnostically from the exercise config.
/// <paramref name="Index"/> is the stable position reference (→ <see cref="PositionItemProgress.ItemIndex"/>).
/// <paramref name="AcceptedAnswers"/> contains the expected solution plus permitted alternatives (raw, the
/// text comparison normalizes later via <see cref="AnswerGrader"/>). <paramref name="GapIndex"/> is only
/// set for cloze texts (the {{n}} number of the gap). <paramref name="Passage"/> carries the content the
/// question is <i>about</i> and that belongs to the whole exercise, not to this atom – the reading text, the
/// instruction covering all grammar tasks. It repeats on every atom on purpose: the card is the unit the
/// server hands out, and a "once per run" delivery would have to be carried by three playback paths plus
/// the offline batch. <paramref name="ItemId"/> and
/// <paramref name="VocabularyId"/> carry (only for vocabulary exercises) the stable item resp. store identity –
/// the basis for the learning progress logged per child/item; <c>null</c> for all other types.
/// <paramref name="ImageUrl"/>/<paramref name="ImageAlt"/> are the image selected for <b>this child</b>
/// (see <see cref="MediaSelector"/>) – only populated when the resolver was called with a child;
/// child-neutral paths (preview, evaluation) leave them empty.
/// </summary>
public record ContentItem(
    int Index,
    string Prompt,
    string Answer,
    IReadOnlyList<string> AcceptedAnswers,
    string? Hint = null,
    int? GapIndex = null,
    string? AudioUrl = null,
    string? Passage = null,
    int? ItemId = null,
    int? VocabularyId = null,
    string? ImageUrl = null,
    string? ImageAlt = null);

/// <summary>
/// Thin facade over the <see cref="ExerciseTypeRegistry"/>: projects the contents of a catalog <see cref="Exercise"/>
/// type-agnostically into <see cref="ContentItem"/>s by delegating to the matching <see cref="IExerciseType"/>
/// (store-free projection; the DB-backed resolution is done by the <see cref="ExerciseContentResolver"/>). Replaces the
/// former type <c>switch</c>; the direction flip (<see cref="WithDirection"/>) remains a shared helper.
/// </summary>
public class ExerciseContentProvider(ExerciseTypeRegistry registry)
{
    /// <summary>The contents of an exercise as a type-agnostic item list.</summary>
    public IReadOnlyList<ContentItem> ItemsOf(Exercise exercise) => ItemsOf(exercise.Type, exercise.ConfigJson);

    /// <summary>Same as <see cref="ItemsOf(Exercise)"/>, but directly from type key + raw JSON (unknown type → empty).</summary>
    public IReadOnlyList<ContentItem> ItemsOf(string typeKey, string configJson) =>
        registry.ByKey(typeKey)?.ItemsOf(configJson) ?? [];

    /// <summary>
    /// Applies the vocabulary query direction to an item built canonically (word → translation):
    /// <c>back-to-front</c> swaps prompt/answer, <c>both</c> swaps deterministically for an odd
    /// index (stable per item, without randomness). The index stays the same – the Leitner/test progress does not flip.
    /// </summary>
    public static ContentItem WithDirection(ContentItem item, string? direction) => direction switch
    {
        "back-to-front" => Swap(item),
        "both" => item.Index % 2 == 0 ? item : Swap(item),
        _ => item,
    };

    // Swap prompt/answer; the alternatives of the reverse case fall away (they applied to the old answer), as
    // does the article hint (it belonged to the word now being asked). The pronunciation audio falls away too:
    // it reads out the word that is the solution after the swap - the listening stage would otherwise speak the
    // answer out loud (anti-cheat). Reverse items are therefore shown as text in the listening stage.
    // The image stays: it shows the *meaning* and is thus direction-independent; whether it may be shown at
    // all is decided by the stage (StageFacets) anyway, not by the direction.
    private static ContentItem Swap(ContentItem it) =>
        it with { Prompt = it.Answer, Answer = it.Prompt, AcceptedAnswers = [it.Prompt], Hint = null, AudioUrl = null };
}
