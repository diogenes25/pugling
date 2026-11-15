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
/// <paramref name="Decoding"/> is the word-for-word decoding of the atom (Birkenbihl): the method itself, not a
/// solution, and therefore material like <paramref name="Passage"/>. <c>null</c> for every other type.
/// <paramref name="ArticleOptionalIn"/> (B-421) is the language of the expected answer when a leading article of it may
/// be left out (<see cref="AnswerGrader"/>); <c>null</c> means exact - every type but vocabulary, and a vocabulary
/// exercise that trains the article.
/// <paramref name="MeaningsSeparable"/> (B-462) marks an answer that is a word-list entry: each single meaning of it
/// counts (<see cref="AnswerVariants"/>). Only vocabulary sets it - in a sentence a comma is part of the answer.
/// <paramref name="Reversed"/> marks a vocabulary card asked against the exercise's canonical direction (translation →
/// word): its answer is in the source language. Distractors are only drawn from cards answered in the same language.
/// <paramref name="Explanation"/> (B-529) says why an answer is right - handed out only with the review, never on the
/// card. Unlike <paramref name="Hint"/>, which is help before the answer ("starts with b").
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
    string? ImageAlt = null,
    IReadOnlyList<WordPair>? Decoding = null,
    string? ArticleOptionalIn = null,
    bool MeaningsSeparable = false,
    bool Reversed = false,
    string? Explanation = null)
{
    /// <summary>
    /// Everything a typed answer is compared against: the accepted answers, plus their single meanings where the
    /// atom is a word-list entry. The one list for grading and for keeping a right answer out of the distractors.
    /// </summary>
    public IEnumerable<string> GradedAnswers() =>
        MeaningsSeparable ? AcceptedAnswers.SelectMany(AnswerVariants.Of).Distinct() : AcceptedAnswers;
}

/// <summary>
/// Thin facade over the <see cref="ExerciseTypeRegistry"/>: projects the contents of a catalog <see cref="Exercise"/>
/// type-agnostically into <see cref="ContentItem"/>s by delegating to the matching <see cref="IExerciseType"/>
/// (store-free projection; the DB-backed resolution is done by the <see cref="ExerciseContentResolver"/>). Replaces the
/// former type <c>switch</c>; the query direction (<see cref="WithDirection"/>) remains a shared helper.
/// </summary>
public class ExerciseContentProvider(ExerciseTypeRegistry registry)
{
    /// <summary>The contents of an exercise as a type-agnostic item list.</summary>
    public IReadOnlyList<ContentItem> ItemsOf(Exercise exercise) => ItemsOf(exercise.Type, exercise.ConfigJson);

    /// <summary>Same as <see cref="ItemsOf(Exercise)"/>, but directly from type key + raw JSON (unknown type → empty).</summary>
    public IReadOnlyList<ContentItem> ItemsOf(string typeKey, string configJson) =>
        registry.ByKey(typeKey)?.ItemsOf(configJson) ?? [];

    /// <summary>
    /// Applies the vocabulary query direction to items built canonically (word → translation, index = list position):
    /// <c>front-to-back</c> keeps them, <c>back-to-front</c> swaps each, and <c>both</c> asks every word twice - card
    /// 2i in the canonical direction, card 2i+1 swapped (B-472). The two cards of a word lie side by side so that a new
    /// word is met both ways on the same day. The resulting index is what Leitner and test progress are stored under.
    /// </summary>
    public static IReadOnlyList<ContentItem> WithDirection(IReadOnlyList<ContentItem> canonical, string? direction) =>
        direction switch
        {
            Both => [.. canonical.SelectMany(it => new[] { it with { Index = 2 * it.Index }, Swap(it) with { Index = 2 * it.Index + 1 } })],
            BackToFront => [.. canonical.Select(Swap)],
            _ => canonical,
        };

    /// <summary>
    /// How many cards a vocabulary exercise with <paramref name="words"/> entries plays in <paramref name="direction"/>.
    /// The catalog states this number, and a daily dose counts these cards.
    /// </summary>
    public static int CardCount(int words, string? direction) => direction == Both ? 2 * words : words;

    private const string Both = "both";
    private const string BackToFront = "back-to-front";

    // Swap prompt/answer; the alternatives of the reverse case fall away (they applied to the old answer), as
    // does the article hint (it belonged to the word now being asked). The pronunciation audio falls away too:
    // it reads out the word that is the solution after the swap - the listening stage would otherwise speak the
    // answer out loud (anti-cheat). Reverse items are therefore shown as text in the listening stage.
    // The image stays: it shows the *meaning* and is thus direction-independent; whether it may be shown at
    // all is decided by the stage (StageFacets) anyway, not by the direction.
    private static ContentItem Swap(ContentItem it) =>
        it with { Prompt = it.Answer, Answer = it.Prompt, AcceptedAnswers = [it.Prompt], Hint = null, AudioUrl = null, Reversed = true };
}
