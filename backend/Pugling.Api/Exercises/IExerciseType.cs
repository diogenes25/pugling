using Pugling.Api.Models;

namespace Pugling.Api.Exercises;

/// <summary>
/// How the content of an exercise type is resolved into individual <see cref="ContentItem"/>s.
/// Pure config projection (<see cref="None"/>) or – for the vocabulary-backed types – additionally
/// from the database (store). The contract deliberately carries only this flag; the DB logic itself stays
/// in <see cref="ExerciseContentResolver"/> (no DbContext in the plugin contract).
/// </summary>
public enum StoreResolution
{
    /// <summary>Content comes exclusively from the <c>ConfigJson</c> (stateless).</summary>
    None = 0,
    /// <summary>Vocabulary items live as their own table (<see cref="ExerciseItem"/>) and reference the store.</summary>
    ItemTable = 1,
    /// <summary>Gaps reference store vocabulary by key (<see cref="Gap.VocabKey"/>).</summary>
    VocabRefs = 2,
}

// StageOption lives in the contract project (Pugling.Contracts).

/// <summary>
/// An exercise type as a self-describing unit ("one type = one class"). Replaces the former closed
/// <c>ExerciseType</c> enum along with the scattered <c>switch</c>/<c>== Vocabulary</c> spots: all type-specific
/// rules (content projection, answer checking, play/preview facets, capabilities) live here. Resolution goes
/// through the <see cref="ExerciseTypeRegistry"/> via a stable <see cref="Key"/> (= wire/DB value). Implementations
/// are stateless (singleton) and inherit sensible defaults from <see cref="ExerciseTypeBase"/>.
/// </summary>
public interface IExerciseType
{
    /// <summary>Stable key of the type (e.g. <c>"Vocabulary"</c>) – also the value of <see cref="Exercise.Type"/> and in the manifest.</summary>
    string Key { get; }

    /// <summary>Self-description for routing/checking/rendering (label, renderer, check mode, capabilities …).</summary>
    ExerciseTypeManifest Manifest { get; }

    /// <summary>Projects the exercise's content procedure-neutrally from its <c>ConfigJson</c>. Types without item-by-item comparison return an empty list.</summary>
    IReadOnlyList<ContentItem> ItemsOf(string configJson);

    /// <summary>
    /// Grades the child's answers at the catalog endpoint (only for check mode <c>CatalogCheck</c>/<c>CatalogGenerateCheck</c>).
    /// <paramref name="seed"/> is only relevant for seed-bound types (arithmetic drill). Default: <c>null</c> (no direct check).
    /// </summary>
    CheckResult? Check(string configJson, IReadOnlyList<GivenAnswer> answers, int? seed);

    /// <summary>
    /// Is the exercise a <b>set</b> rather than a sequence – does any answer not yet named count, no matter
    /// which card asked for it? Takes <paramref name="configJson"/> because it is a property of the concrete
    /// exercise, not of the type: a list grades as a set exactly while <c>Ordered</c> is off. Default
    /// <c>false</c> – every other type addresses its atoms individually.
    /// </summary>
    bool GradesAsSet(string configJson);

    /// <summary>
    /// How many content atoms the exercise holds, for the catalog row (B-424). <paramref name="itemTableRows"/> is the
    /// number of <see cref="ExerciseItem"/> rows - only the vocabulary type keeps its atoms there. <c>null</c> for a type
    /// without countable atoms (no <see cref="ExerciseTypeManifest.ItemNoun"/>). A config that cannot be projected may
    /// throw: the catalog search catches it per row and logs it, so a broken row costs only its own count (B-448). A type
    /// whose atoms live outside the config (like the vocabulary item rows) overrides this.
    /// </summary>
    int? ItemCount(string configJson, int itemTableRows);

    /// <summary>Procedure default stage, when neither the study plan nor a position/exercise override specifies a stage.</summary>
    int DefaultStage { get; }

    /// <summary>Representative stage for trying out in the supervisor test mode (usually the most meaningful typed stage).</summary>
    int PreviewStage { get; }

    /// <summary>Is the stage "typed"/objective (checkable server-side against the solution) instead of pure self-assessment?</summary>
    bool IsTypedStage(int stage);

    /// <summary>
    /// Is the stage a free display stage – both sides shown at once, no self-assessment, no grading, no
    /// Leitner box movement? Neither typed nor self-assessed; a third state next to <see cref="IsTypedStage"/>
    /// (default <c>false</c> – most stages are one or the other).
    /// </summary>
    bool IsDisplayOnlyStage(int stage);

    /// <summary>
    /// The stage one introduced card is asked at, given its Leitner <paramref name="box"/> and the
    /// <paramref name="targetStage"/> the position sets for the day (B-476). The target is a ceiling, not the entry
    /// point: a type with a ladder of easier forms (recognise, then choose, then produce) lets a card earn its way up
    /// box by box, and never above the target. Default: the target itself - a type without a ladder plays as before.
    /// </summary>
    int CardStage(int targetStage, int box);

    /// <summary>
    /// The options offered for the task, or <c>null</c> if the type/stage has none. Takes
    /// <paramref name="configJson"/> like <see cref="Check"/> does, because a pool need not be derivable from
    /// the atoms: vocabulary builds distractors out of the sibling items, the cloze reads the word bank the
    /// author curated.
    /// </summary>
    IReadOnlyList<string>? Choices(string configJson, IReadOnlyList<ContentItem> items, ContentItem item, int stage);

    /// <summary>
    /// Whether <see cref="Choices"/> is one pool shared by every card of the exercise (the cloze word bank) rather than
    /// the options of one question. A shared pool keeps the order the type gives it - reshuffled per card it would be
    /// unreadable; every other list is shuffled per card and session, so the child learns the value, not the position.
    /// </summary>
    bool ChoicesShareOnePool { get; }

    /// <summary>
    /// The cue box of the stage (B-524): the meanings of all atoms plus curated distractors as one unordered list,
    /// or <c>null</c> where the stage has none. Non-null also means the asked atom shows no hint of its own - the box
    /// replaces it, and a hint next to it would name the meaning the box is meant to make the child find.
    /// </summary>
    IReadOnlyList<string>? CueBox(string configJson, IReadOnlyList<ContentItem> items, int stage);

    /// <summary>
    /// Type-specific card facets per stage: letter-box length, audio source, and/or image
    /// (otherwise <c>null</c>). Here – and only here – the type also decides whether an image would
    /// give away the solution: it is <b>stricter</b> for images than for audio, because a motif shows
    /// the meaning in both query directions, while the pronunciation only reads out one word.
    /// <para>
    /// <c>AnswerPattern</c> is the letter-box mask (B-66): an underscore per letter/digit to type, every
    /// other character (space, punctuation, hyphen) kept literally – it is already fixed by the solution and
    /// typing it teaches nothing. <c>null</c> outside the letter-box stage, same as <c>LetterBoxLength</c>.
    /// </para>
    /// </summary>
    (int? LetterBoxLength, string? AudioUrl, string? ImageUrl, string? AnswerPattern) StageFacets(ContentItem item, int stage);

    /// <summary>
    /// The word whose letters this stage hands out as scrambled tiles (B-05), or <c>null</c>. Only the source:
    /// the shuffle needs the session's seed, which the type does not know (see
    /// <c>PositionPlayService.CardFacets</c>).
    /// </summary>
    string? LetterTileSource(ContentItem item, int stage);

    /// <summary>
    /// Does the recording <b>replace</b> the prompt at this stage, instead of accompanying it? Only the type
    /// can answer that, and it cannot be derived from the card: at the vocabulary listening stage showing the
    /// word alongside would turn "listen, then type" into a reading task, while a listening comprehension
    /// needs recording <b>and</b> question or the child has nothing to answer. Withholding it is an anti-cheat
    /// decision and therefore belongs to the server, not to a branch in the renderer.
    /// </summary>
    bool AudioReplacesPrompt(int stage);

    /// <summary>The query forms switchable in test mode (empty if the type only has one form).</summary>
    IReadOnlyList<StageOption> StageOptions { get; }

    /// <summary>
    /// A read-only view of an exercise that has no gradable atoms by its nature (an essay is a writing task,
    /// not question/answer pairs), so the test mode can still show what the child will get. Only consulted when
    /// <see cref="ItemsOf"/> is empty; <c>null</c> (default) keeps "nothing to show" for every other type, so an
    /// unfilled exercise of an atom-based type is never dressed up as a view.
    /// </summary>
    ContentItem? StaticView(string configJson);

    /// <summary>Does the type carry cross-plan item learning progress (today only vocabulary)?</summary>
    bool SupportsItemProgress { get; }

    /// <summary>May objectives/key results be set for this type?</summary>
    bool SupportsObjectives { get; }

    /// <summary>
    /// Does the type have ANY typed stage at all (<see cref="IsTypedStage"/> can be <c>true</c> for at least
    /// one stage)? Default <c>true</c> - most types do. A type whose <see cref="IsTypedStage"/> is constant
    /// <c>false</c> (Birkenbihl: it learns by reading, never by typing) overrides this to <c>false</c>, so a
    /// position's <see cref="PlanPosition.RequireTypedTest"/> can be rejected at write time (B-93) instead of
    /// silently never applying (a graded attempt that can never happen).
    /// </summary>
    bool SupportsRequireTypedTest { get; }

    /// <summary>How the content is resolved (purely from config or additionally from the store).</summary>
    StoreResolution StoreResolution { get; }
}

/// <summary>
/// An exercise type whose concrete tasks are generated server-side per request (arithmetic drill). The set is
/// generated reproducibly from a fixed <c>Seed</c>, so the later check (<see cref="IExerciseType.Check"/>)
/// can grade exactly the same set.
/// </summary>
public interface IGeneratingExerciseType : IExerciseType
{
    /// <summary>Generates a problem set according to the stored rules and returns the seed used (for later checking).</summary>
    (int Seed, IReadOnlyList<GeneratedProblem> Problems) Generate(string configJson, int? seed);
}

/// <summary>
/// Stable keys of the built-in exercise types – for the few places that mean a concrete built-in
/// (seed, store linking, backfill), instead of a generic capability flag. No more magic strings.
/// </summary>
public static class ExerciseTypeKeys
{
    /// <summary>Key of vocabulary training.</summary>
    public const string Vocabulary = "Vocabulary";
    /// <summary>Key of reading comprehension.</summary>
    public const string Reading = "Reading";
    /// <summary>Key of the cloze exercise.</summary>
    public const string Cloze = "Cloze";
    /// <summary>Key of the essay.</summary>
    public const string Essay = "Essay";
    /// <summary>Key of listening comprehension.</summary>
    public const string Listening = "Listening";
    /// <summary>Key of the grammar exercise.</summary>
    public const string Grammar = "Grammar";
    /// <summary>Key of the matching exercise.</summary>
    public const string Matching = "Matching";
    /// <summary>Key of the translation exercise.</summary>
    public const string Translation = "Translation";
    /// <summary>Key of the fixed arithmetic problems.</summary>
    public const string Arithmetic = "Arithmetic";
    /// <summary>Key of the arithmetic drill (generated seed-based).</summary>
    public const string ArithmeticDrill = "ArithmeticDrill";
    /// <summary>Key of the list exercise (memorization).</summary>
    public const string List = "List";
    /// <summary>Key of the Birkenbihl word-for-word decoding.</summary>
    public const string Birkenbihl = "Birkenbihl";
}
