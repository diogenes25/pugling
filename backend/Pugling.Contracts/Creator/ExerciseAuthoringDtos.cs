using System.Text.Json.Serialization;

namespace Pugling.Contracts.Creator;

// Contract of the typed exercise CRUD (one controller per exercise type, one shared generic envelope),
// of the vocabulary items as their own tier, and of the Birkenbihl decoding.

/// <summary>
/// Exercise for creating/changing: shared fields + type-specific config + optional bonus suggestion.
/// The metadata (grade level, school type, source, kind) serves study-plan pre-filtering and is optional.
///
/// <paramref name="RewardPoints"/> is a <b>recommendation</b>, not a setting: a new study-plan position
/// inherits it as its target reward when the supervisor states nothing, and stays free to override. It is
/// read at that moment only - raising it later does not move plans already running. Unusable figures (0,
/// negative, implausibly large) fall back to the position's default of 20.
///
/// <paramref name="InterestTagIds"/> names the interests the exercise is <b>dressed</b> in (B-46) - ids of
/// the shared taxonomy that the child's interests and the images already use. It is a full representation
/// like every other field here: what the payload does not list is removed. An unknown id is an
/// <c>invalid_reference</c> (B-329), never a silent skip.
///
/// <paramref name="GrammarTopicIds"/> names the grammar topics the exercise practices (B-155), with the same
/// full-representation semantics. An unknown id is an <c>invalid_reference</c>; a topic of another subject than the
/// series' is a <c>validation_error</c>.
/// </summary>
public record ExercisePayload<TConfig>(string Title, int OrderIndex, int RewardPoints, TConfig Config,
    SuggestedBonus? SuggestedBonus = null,
    int? GradeMin = null, int? GradeMax = null, SchoolTypes SchoolTypes = SchoolTypes.None,
    string? Source = null, int? CategoryId = null, string? Description = null,
    bool DefaultUseLeitner = false, bool DefaultRequireTypedTest = false, int? DefaultStage = null,
    int? DefaultItemCount = null, bool ExecutePublic = true,
    IReadOnlyList<int>? InterestTagIds = null, IReadOnlyList<int>? GrammarTopicIds = null);

/// <summary>
/// Exercise in the response. <paramref name="IsOwn"/> = the requesting creator may <b>change</b> it (owner or
/// write grant); <paramref name="IsOwner"/> = may <b>manage</b> it (owner: delete, grant permissions,
/// toggle visibility); <paramref name="ExecutePublic"/> = assignable to everyone.
///
/// <paramref name="InterestTagIds"/> is the dressing (B-46), <b>ascending</b> and therefore comparable
/// between two exercises without sorting first - that comparison is the whole point of the field, and
/// B-18 will build the child-fit filter on it. <paramref name="GrammarTopicIds"/> (B-155) follows the same
/// ordering rule.
/// </summary>
public record ExerciseResponse<TConfig>(int Id, int SeriesUnitId, string Type, string Title,
    int OrderIndex, int RewardPoints, DateTime CreatedAt, TConfig Config, SuggestedBonus? SuggestedBonus,
    int? GradeMin, int? GradeMax, SchoolTypes SchoolTypes, string? Source, int? CategoryId, string? CategoryName,
    int? AuthorAdultId, bool IsOwn, bool IsOwner, bool ExecutePublic, int GrantCount, string? Description,
    bool DefaultUseLeitner, bool DefaultRequireTypedTest,
    int? DefaultStage, int? DefaultItemCount, IReadOnlyList<int> InterestTagIds, IReadOnlyList<int> GrammarTopicIds);

/// <summary>
/// Child's answers for a direct catalog check, position-based (index in the problem/pair list).
/// <paramref name="Seed"/> is only needed for seed-bound types (arithmetic drill) – the one received when generating.
/// </summary>
public record CheckDto(List<Shared.GivenAnswer> Answers, int? Seed = null);

/// <summary>Selecting vocabulary by tag instead of a manual reference list.</summary>
public record RefsFromTagsDto(List<string> Tags, bool MatchAll = false, bool BaseFormsOnly = false);

/// <summary>A single vocabulary pair of the exercise. Front/back come from the linked store entry.</summary>
/// <param name="Id">Stable item id (ItemId).</param>
/// <param name="OrderIndex">Sort key within the exercise.</param>
/// <param name="VocabularyId">Linked vocabulary store entry.</param>
/// <param name="Front">Word in the learning language (from the store).</param>
/// <param name="Back">Translation (from the store).</param>
/// <param name="TranslationAlternatives">Equally valid translations of the store entry (<c>null</c> = none);
/// read back so an editor shows what is already declared.</param>
/// <param name="Hint">Exercise-local hint; overrides the derived store hint.</param>
/// <param name="Self">HATEOAS link to the item itself.</param>
/// <param name="Vocabulary">HATEOAS link to the store entry.</param>
public record VocabItemResponse(int Id, int OrderIndex, int VocabularyId, string Front, string Back,
    IReadOnlyList<string>? TranslationAlternatives, string? Hint,
    [property: JsonPropertyName("_self")] string Self,
    [property: JsonPropertyName("vocabulary")] string Vocabulary);

/// <summary>
/// Creating/changing an item: either via <paramref name="VocabularyId"/> (existing store vocabulary) or inline
/// via <paramref name="Front"/>/<paramref name="Back"/> (created/found in the store). <paramref name="Hint"/>
/// empty = delete, set = overwrite; on PATCH every omitted field remains unchanged.
/// </summary>
/// <param name="VocabularyId">Existing store entry the item points to.</param>
/// <param name="Front">Word in the learning language, for creating/finding the store entry inline.</param>
/// <param name="Back">Translation, for creating/finding the store entry inline.</param>
/// <param name="Hint">Exercise-local hint; empty deletes it.</param>
/// <param name="OrderIndex">Sort key within the exercise.</param>
/// <param name="TranslationAlternatives">Equally valid translations, written onto the item's store entry –
/// also when that entry already existed, so they are never dropped silently. <c>null</c> (or omitted) leaves
/// the entry's alternatives unchanged; an empty list <c>[]</c> deletes them. The store keeps one list per
/// word, so every exercise using the same entry sees the change.</param>
public record VocabItemInput(int? VocabularyId = null, string? Front = null, string? Back = null,
    string? Hint = null, int? OrderIndex = null, List<string>? TranslationAlternatives = null);

/// <summary>An interchangeable vocabulary candidate for a word (several for homonyms).</summary>
/// <param name="VocabularyId">Vocabulary id.</param>
/// <param name="Word">Word in the learning language.</param>
/// <param name="Translation">Native-language gloss of this meaning.</param>
/// <param name="PartOfSpeech">Part of speech (helps distinguish identical spellings).</param>
/// <param name="Self">Link to the vocabulary card (<c>_self</c>).</param>
public record VocabCandidate(int VocabularyId, string Word, string Translation, string PartOfSpeech,
    [property: JsonPropertyName("_self")] string Self);

/// <summary>
/// A decoded word of the output: <paramref name="LearningWord"/> in the learning language → literal gloss
/// <paramref name="Gloss"/>. <paramref name="Gloss"/>/<paramref name="VocabularyId"/>/<paramref name="Self"/>
/// are <c>null</c> if the word is not (yet) in the vocabulary store. <paramref name="Candidates"/> is only
/// populated for ambiguous words (several matching cards – the supervisor can pick the right one via the word endpoint).
/// </summary>
public record DecodedWord(int WordId, string LearningWord, string? Gloss, int? VocabularyId,
    [property: JsonPropertyName("_self")] string? Self, IReadOnlyList<VocabCandidate>? Candidates);

/// <summary>A decoded sentence: original + natural translation + the word-for-word tuples.</summary>
public record DecodedSentence(int SentenceId, string LearningSentence, string NaturalTranslation, IReadOnlyList<DecodedWord> Result);

/// <summary>
/// Input for adding a sentence: the sentence in the learning language + its natural, correct translation. The translation is
/// optional; missing or <c>null</c> is stored as an empty string.
/// </summary>
public record BirkenbihlSentenceInput(string LearningSentence, string? NaturalTranslation);

/// <summary>
/// Correction of a single word. <paramref name="VocabularyId"/> set → the gloss follows this card
/// (correct meaning for homonyms). Only <paramref name="Gloss"/> set → free gloss without a card. Both
/// empty → remove gloss (word stays undecoded).
/// </summary>
public record WordOverride(int? VocabularyId, string? Gloss);

/// <summary>
/// Input for the stateless preview: languages + the sentence to decode along with its translation. The translation is
/// optional and comes back empty when missing; the languages are required, because the store is matched per language pair.
/// </summary>
public record DecodePreviewInput(string LearningLang, string NativeLang, string LearningSentence, string? NaturalTranslation);

/// <summary>A freshly generated problem set for a drill exercise.</summary>
public record GeneratedDrill(int ExerciseId, string Title, int Seed, IReadOnlyList<Shared.GeneratedProblem> Problems);
