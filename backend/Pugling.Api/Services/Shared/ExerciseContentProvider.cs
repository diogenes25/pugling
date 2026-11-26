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
/// <paramref name="SpeechLang"/> (B-252) is the language the prompt may be read aloud in by the device's speech output:
/// set only for a vocabulary card asked in its reading direction whose word has no recording - the prompt is then the
/// foreign word itself, never the solution. A reversed card loses it (its prompt is the German side).
/// <paramref name="Written"/> (B-597) is this card as asked on a writing stage: German shown, the learnt language expected.
/// Set only on a vocabulary card that is asked the other way round elsewhere; <c>null</c> means the card is written as it
/// is (it already expects the learnt language, or it is a recognition card that never reaches a writing stage).
/// <paramref name="RecognitionOnly"/> (B-597) marks the forward card of a "both" word: its sibling card is the one that is
/// written, so this one climbs no higher than the choice.
/// <paramref name="DictationAudioUrl"/>/<paramref name="DictationLang"/> (B-597) are the recording resp. the speech language
/// of the learnt word on a card whose answer is that word - the source of the dictation. Handed out only on the dictation
/// stage, where hearing the answer is the task.
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
    string? Explanation = null,
    string? SpeechLang = null,
    ContentItem? Written = null,
    bool RecognitionOnly = false,
    string? DictationAudioUrl = null,
    string? DictationLang = null,
    NumericTarget? Numeric = null,
    NumericSetTarget? NumericSet = null)
{
    /// <summary>
    /// Is this atom graded as a number or a set of numbers (B-639, B-710)? Such an answer is never a spelling or an
    /// accent slip - a wrong number is another number.
    /// </summary>
    public bool IsNumber => Numeric is not null || NumericSet is not null;

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
            // B-597: the forward card of a word asked both ways only recognises; the reversed one is the word to write.
            Both => [.. canonical.SelectMany(it => new[] { it with { Index = 2 * it.Index, RecognitionOnly = true }, Swap(it) with { Index = 2 * it.Index + 1 } })],
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
    // does the article hint (it belonged to the word now being asked). The pronunciation audio leaves the card's own
    // audio: it reads out the word that is the solution after the swap, so outside the dictation it would speak the
    // answer out loud (anti-cheat).
    // The image stays: it shows the *meaning* and is thus direction-independent; whether it may be shown at
    // all is decided by the stage (StageFacets) anyway, not by the direction.
    // B-597: the recording and the speech language move to the dictation pair instead - there hearing the answer is the
    // task, and only the dictation stage hands them out.
    internal static ContentItem Swap(ContentItem it) =>
        it with
        {
            Prompt = it.Answer,
            Answer = it.Prompt,
            AcceptedAnswers = [it.Prompt],
            Hint = null,
            AudioUrl = null,
            Reversed = true,
            // B-252: the reversed prompt is the translation; reading the foreign word aloud here would speak the solution.
            SpeechLang = null,
            DictationAudioUrl = it.AudioUrl,
            DictationLang = it.SpeechLang,
            Written = null,
            RecognitionOnly = false,
            // The reversed card asks the prompt; a number target kept here would grade it against the old number.
            Numeric = null,
            NumericSet = null,
        };
}
