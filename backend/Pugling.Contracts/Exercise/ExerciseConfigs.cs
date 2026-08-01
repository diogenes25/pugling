using System.Text.Json.Serialization;

namespace Pugling.Contracts;

// Config schemas per exercise type.
// Stored as JSON in Exercise.ConfigJson, but transferred typed in the API
// as part of ExercisePayload<TConfig> / ExerciseResponse<TConfig>.
// Every type = its own path + its own Swagger schema.
//
// They live in Pugling.Contracts so that the server (Pugling.Api) and the client projects
// (creator/supervisor) share the same config types over the wire.

/// <summary>Question with optional answer choices (empty = free text).</summary>
public record Question(string Prompt, List<string>? Choices, string Answer);

/// <summary>
/// Vocabulary exercise. References entries of the vocabulary store via <see cref="Refs"/> (by <see cref="VocabRef.VocabularyId"/>),
/// so the same vocabulary item can be linked across multiple exercises and maintained centrally. Inline <see cref="Items"/>
/// without their own <see cref="VocabItem.VocabularyId"/> are automatically created in the store and linked on save –
/// this guarantees every used vocabulary item lives in the store. <see cref="SourceLang"/>/<see cref="TargetLang"/> are
/// needed for this (the store key is formed from language + word + translation).
/// </summary>
public class VocabularyConfig
{
    /// <summary>Query direction: front-to-back | back-to-front | both.</summary>
    public string Direction { get; set; } = "front-to-back";
    /// <summary>Language code of the source language (e.g. "en"); needed to create inline <see cref="Items"/> in the store.</summary>
    public string SourceLang { get; set; } = "";
    /// <summary>Language code of the target language (e.g. "de"); needed to create inline <see cref="Items"/> in the store.</summary>
    public string TargetLang { get; set; } = "";
    /// <summary>References to vocabulary store entries (by ID; the response adds the link).</summary>
    public List<VocabRef>? Refs { get; set; }
    /// <summary>Inline vocabulary items; without <see cref="VocabItem.VocabularyId"/> they are automatically created in the store on save.</summary>
    public List<VocabItem> Items { get; set; } = new();
}

/// <summary>
/// Reference to a vocabulary store entry. The <paramref name="VocabularyId"/> is persisted (and – as a reading aid –
/// optionally the <paramref name="Key"/>). <paramref name="Self"/> is a purely derived HATEOAS link
/// (<c>/api/v1/creator/vocabulary/{id}</c>) that is only populated in responses and never stored.
/// </summary>
[JsonConverter(typeof(VocabRefJsonConverter))]
public record VocabRef(int VocabularyId, string? Key = null, string? Self = null);

/// <summary>
/// Inline vocabulary item – the same input shape as the item endpoint (<c>VocabItemInput</c>): word via
/// <paramref name="VocabularyId"/> (existing store entry; <paramref name="Front"/>/<paramref name="Back"/>
/// then come from the store) <b>or</b> inline via <paramref name="Front"/>/<paramref name="Back"/> (created/found
/// in the store on save). Both are therefore optional; an item needs either the
/// <paramref name="VocabularyId"/> or Front <i>and</i> Back. <paramref name="Self"/> is the derived, read-only
/// populated HATEOAS link.
/// </summary>
public record VocabItem(string? Front = null, string? Back = null, string? Hint = null,
    int? VocabularyId = null, [property: JsonPropertyName("_self")] string? Self = null);

/// <summary>Reading comprehension: text + comprehension questions.</summary>
public class ReadingConfig
{
    /// <summary>The text to be read.</summary>
    public string Text { get; set; } = "";
    /// <summary>Questions about the text – they are the graded content atoms of the exercise.</summary>
    public List<Question> Questions { get; set; } = new();
}

/// <summary>Cloze: text with placeholders {{1}}, {{2}} … + solutions.</summary>
public class ClozeConfig
{
    /// <summary>The text with placeholders <c>{{1}}</c>, <c>{{2}}</c> … at the gap positions.</summary>
    public string Text { get; set; } = "";
    /// <summary>The gaps; their <c>Index</c> refers to the placeholder of the same name in the text.</summary>
    public List<Gap> Gaps { get; set; } = new();
    /// <summary>Optional word pool to choose from.</summary>
    public List<string>? WordBank { get; set; }
}
/// <summary>
/// A gap. If <paramref name="VocabKey"/> is set, the solution comes from the vocabulary store
/// (the entry's word), centrally maintainable; the inline <paramref name="Answer"/> remains a fallback for
/// gaps without a store reference. This allows building a cloze from the maintained vocabulary.
/// </summary>
public record Gap(int Index, string Answer, List<string>? Alternatives = null, string? VocabKey = null);

/// <summary>Essay: writing prompt + constraints.</summary>
public class EssayConfig
{
    /// <summary>The writing prompt.</summary>
    public string Prompt { get; set; } = "";
    /// <summary>Minimum length in words (empty = no lower bound).</summary>
    public int? MinWords { get; set; }
    /// <summary>Maximum length in words (empty = no upper bound).</summary>
    public int? MaxWords { get; set; }
    /// <summary>Optional grading criteria.</summary>
    public List<RubricCriterion>? Rubric { get; set; }
}

/// <summary>A grading criterion of an essay.</summary>
/// <param name="Criterion">What is being assessed (e.g. "structure", "vocabulary").</param>
/// <param name="MaxScore">Maximum score this criterion can achieve.</param>
public record RubricCriterion(string Criterion, int MaxScore);

/// <summary>Listening comprehension: audio source + comprehension questions.</summary>
public class ListeningConfig
{
    /// <summary>URL / reference to the audio file.</summary>
    public string AudioUrl { get; set; } = "";
    /// <summary>Optional full transcript of the recording – for the creator only, never for the child (anti-cheat).</summary>
    public string? Transcript { get; set; }
    /// <summary>Questions about the recording – they are the graded content atoms of the exercise.</summary>
    public List<Question> Questions { get; set; } = new();
}

/// <summary>Grammar: transformation / rule-based tasks.</summary>
public class GrammarConfig
{
    /// <summary>Optional instruction covering all tasks (e.g. "Put the verb in the correct form").</summary>
    public string? Instruction { get; set; }
    /// <summary>The individual tasks – the graded content atoms of the exercise.</summary>
    public List<GrammarTask> Tasks { get; set; } = new();
}

/// <summary>A grammar task.</summary>
/// <param name="Prompt">The task statement, usually with a gap ("He ___ (to feed) the horse").</param>
/// <param name="Answer">The expected solution.</param>
/// <param name="RuleHint">Optional rule hint that may be shown after the answer.</param>
public record GrammarTask(string Prompt, string Answer, string? RuleHint = null);

/// <summary>Matching: pairs left ↔ right.</summary>
public class MatchingConfig
{
    /// <summary>Optional instruction covering all pairs.</summary>
    public string? Instruction { get; set; }
    /// <summary>The pairs to be matched – the graded content atoms of the exercise.</summary>
    public List<MatchPair> Pairs { get; set; } = new();
}

/// <summary>A pair to be matched.</summary>
/// <param name="Left">The given entry (left column).</param>
/// <param name="Right">The matching counterpart (right column).</param>
public record MatchPair(string Left, string Right);

/// <summary>Translation: sentences with expected translation.</summary>
public class TranslationConfig
{
    /// <summary>Source language as a language code (e.g. "en").</summary>
    public string SourceLang { get; set; } = "";
    /// <summary>Target language as a language code (e.g. "de").</summary>
    public string TargetLang { get; set; } = "";
    /// <summary>The sentence pairs – the graded content atoms of the exercise.</summary>
    public List<TranslationItem> Items { get; set; } = new();
}
/// <summary>
/// A translation pair. <paramref name="VocabularyId"/> refers to the associated store entry
/// (automatically created on save); <paramref name="Self"/> is the derived, read-only populated link.
/// </summary>
public record TranslationItem(string Source, string Target, List<string>? Alternatives = null,
    int? VocabularyId = null, [property: JsonPropertyName("_self")] string? Self = null);

/// <summary>Fixed arithmetic problems: manually maintained list of expression and expected solution.</summary>
public class ArithmeticConfig
{
    /// <summary>The fixed problems – the graded content atoms of the exercise.</summary>
    public List<ArithmeticProblem> Problems { get; set; } = new();
}
/// <summary>
/// A fixed arithmetic problem. <see cref="Tolerance"/> allows rounding leeway
/// (0 = exact solution expected), e.g. for divisions that don't come out evenly.
/// </summary>
public record ArithmeticProblem(string Prompt, decimal Answer, decimal Tolerance = 0m);

/// <summary>Operation type of a generated problem.</summary>
public enum ArithmeticOperation
{
    /// <summary>Addition.</summary>
    Addition,
    /// <summary>Subtraction – whether a negative result is allowed is controlled by <see cref="ArithmeticDrillConfig.AllowNegativeResults"/>.</summary>
    Subtraction,
    /// <summary>Multiplication.</summary>
    Multiplication,
    /// <summary>Division – whether it must divide evenly is controlled by <see cref="ArithmeticDrillConfig.DivisionMustBeWhole"/>.</summary>
    Division,
}

/// <summary>
/// Rules for randomly generated arithmetic problems. Only the rules are stored;
/// the concrete problems are generated by the server per request (see ArithmeticDrillController.Generate).
/// </summary>
public class ArithmeticDrillConfig
{
    /// <summary>Allowed operation types; one is chosen at random per problem.</summary>
    public List<ArithmeticOperation> Operations { get; set; } = new() { ArithmeticOperation.Addition };
    /// <summary>Smallest operand (inclusive).</summary>
    public int MinOperand { get; set; } = 1;
    /// <summary>Largest operand (inclusive).</summary>
    public int MaxOperand { get; set; } = 10;
    /// <summary>Number of problems generated per run.</summary>
    public int ProblemCount { get; set; } = 10;
    /// <summary>Whether subtractions may yield a negative result. Default: no.</summary>
    public bool AllowNegativeResults { get; set; }
    /// <summary>Whether divisions must come out without a remainder (integer result).</summary>
    public bool DivisionMustBeWhole { get; set; } = true;
    /// <summary>Optional fixed seed for reproducible runs (empty = true randomness).</summary>
    public int? Seed { get; set; }
}

/// <summary>
/// A list to be memorized (e.g. "the 16 German states"). By default, grading only checks
/// whether the entries were named – order only counts if <see cref="Ordered"/> is set.
/// </summary>
public class ListConfig
{
    /// <summary>Optional instruction/question (e.g. "Name all 16 German states").</summary>
    public string? Instruction { get; set; }
    /// <summary>Whether order counts for grading.</summary>
    public bool Ordered { get; set; }
    /// <summary>The entries to be named – the graded content atoms of the exercise.</summary>
    public List<ListEntry> Items { get; set; } = new();
}
/// <summary>A list entry; <paramref name="Alternatives"/> allows acceptable synonyms/spellings.</summary>
public record ListEntry(string Value, List<string>? Alternatives = null);

/// <summary>
/// Birkenbihl method: a text in the language being learned is decoded word for word into the
/// native language, independent of grammar. Each sentence additionally has a
/// natural, grammatically correct translation so the meaning is clear. Learning happens
/// through reading/listening to the decoding – the method deliberately forgoes active testing
/// (which is why this type has no <c>/check</c>; points are awarded for working through it).
/// </summary>
public class BirkenbihlConfig
{
    /// <summary>Language code of the language being learned – the sentences are in it (e.g. "en"). Must match the vocabulary store.</summary>
    public string LearningLang { get; set; } = "";
    /// <summary>Language code of the native language (glosses + translation, e.g. "de"). Must match the vocabulary store.</summary>
    public string NativeLang { get; set; } = "";
    /// <summary>Next <see cref="BirkenbihlSentence.SentenceId"/> to be assigned (monotonic, no recycling of deleted IDs).</summary>
    public int NextSentenceId { get; set; }
    /// <summary>
    /// Next <see cref="WordPair.WordId"/> to be assigned. Deliberately unique <b>exercise-wide</b> (not per sentence),
    /// so the swap endpoint <c>.../words/{wordId}</c> uniquely addresses a word without a sentence segment.
    /// </summary>
    public int NextWordId { get; set; }
    /// <summary>The sentences of the text in reading order.</summary>
    public List<BirkenbihlSentence> Sentences { get; set; } = new();
}

/// <summary>
/// A sentence of the Birkenbihl exercise: the original sentence in the language being learned (<paramref name="LearningSentence"/>),
/// its position-accurate word-for-word decoding (<paramref name="Decoding"/>), and a natural,
/// grammatically correct translation (<paramref name="NaturalTranslation"/>).
/// </summary>
public record BirkenbihlSentence(int SentenceId, string LearningSentence, string NaturalTranslation, List<WordPair> Decoding);

/// <summary>
/// A word tuple of the decoding: <paramref name="LearningWord"/> of the language being learned → literal native-language
/// gloss <paramref name="Gloss"/>. <paramref name="Gloss"/>/<paramref name="VocabularyId"/> are <c>null</c> if
/// the word is not (yet) in the vocabulary store and no manual gloss was set. <paramref name="WordId"/>
/// is unique exercise-wide (see <see cref="BirkenbihlConfig.NextWordId"/>).
/// </summary>
public record WordPair(int WordId, string LearningWord, string? Gloss, int? VocabularyId,
    [property: JsonPropertyName("_self")] string? Self = null);
