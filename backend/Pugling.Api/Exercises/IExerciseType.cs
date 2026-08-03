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

    /// <summary>Procedure default stage, when neither the study plan nor a position/exercise override specifies a stage.</summary>
    int DefaultStage { get; }

    /// <summary>Representative stage for trying out in the supervisor test mode (usually the most meaningful typed stage).</summary>
    int PreviewStage { get; }

    /// <summary>Is the stage "typed"/objective (checkable server-side against the solution) instead of pure self-assessment?</summary>
    bool IsTypedStage(int stage);

    /// <summary>
    /// The options offered for the task, or <c>null</c> if the type/stage has none. Takes
    /// <paramref name="configJson"/> like <see cref="Check"/> does, because a pool need not be derivable from
    /// the atoms: vocabulary builds distractors out of the sibling items, the cloze reads the word bank the
    /// author curated.
    /// </summary>
    IReadOnlyList<string>? Choices(string configJson, IReadOnlyList<ContentItem> items, ContentItem item, int stage);

    /// <summary>
    /// Type-specific card facets per stage: letter-box length, audio source, and/or image
    /// (otherwise <c>null</c>). Here – and only here – the type also decides whether an image would
    /// give away the solution: it is <b>stricter</b> for images than for audio, because a motif shows
    /// the meaning in both query directions, while the pronunciation only reads out one word.
    /// </summary>
    (int? LetterBoxLength, string? AudioUrl, string? ImageUrl) StageFacets(ContentItem item, int stage);

    /// <summary>The query forms switchable in test mode (empty if the type only has one form).</summary>
    IReadOnlyList<StageOption> StageOptions { get; }

    /// <summary>Does the type carry cross-plan item learning progress (today only vocabulary)?</summary>
    bool SupportsItemProgress { get; }

    /// <summary>May objectives/key results be set for this type?</summary>
    bool SupportsObjectives { get; }

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
