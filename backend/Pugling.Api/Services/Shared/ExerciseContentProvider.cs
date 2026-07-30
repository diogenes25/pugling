using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// A single practicable/checkable element of an exercise, projected type-agnostically from the exercise config.
/// <paramref name="Index"/> is the stable position reference (→ <see cref="PositionItemProgress.ItemIndex"/>).
/// <paramref name="AcceptedAnswers"/> contains the expected solution plus permitted alternatives (raw, the
/// text comparison normalizes later via <see cref="AnswerGrader"/>). <paramref name="GapIndex"/> is only
/// set for cloze texts (the {{n}} number of the gap). <paramref name="ItemId"/> and
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

    // Prompt/Antwort tauschen; die Alternativen des Rückwärts-Falls entfallen (galten für die alte Antwort),
    // der Artikel-Hinweis ebenso (er gehörte zum nun abgefragten Wort). Die Aussprache-Audioquelle entfällt
    // ebenfalls: sie liest das Wort vor, das nach dem Tausch die Lösung ist – sonst würde die Hör-Stufe die
    // Antwort vorsprechen (Anti-Schummel). Rückwärts-Items werden in der Hör-Stufe damit textlich gezeigt.
    // Das Bild bleibt: es zeigt die *Bedeutung* und ist damit richtungsunabhängig; ob es überhaupt gezeigt
    // werden darf, entscheidet ohnehin die Stufe (StageFacets), nicht die Richtung.
    private static ContentItem Swap(ContentItem it) =>
        it with { Prompt = it.Answer, Answer = it.Prompt, AcceptedAnswers = [it.Prompt], Hint = null, AudioUrl = null };
}
