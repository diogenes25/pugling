using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Exercises;

/// <summary>
/// Convenient base for exercise types: provides a sensible default for every facet (no check, always typed,
/// no choices/facets/stages, no capabilities, no store resolution), so a concrete type class only
/// overrides what it actually needs – analogous to the <c>virtual</c> hooks in <see cref="ExerciseControllerBase{TConfig}"/>.
/// <see cref="Key"/>, <see cref="Manifest"/>, and <see cref="ItemsOf"/> are deliberately abstract (every type has them).
/// </summary>
public abstract class ExerciseTypeBase : IExerciseType
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc/>
    public abstract string Key { get; }

    /// <inheritdoc/>
    public abstract ExerciseTypeManifest Manifest { get; }

    /// <inheritdoc/>
    public abstract IReadOnlyList<ContentItem> ItemsOf(string configJson);

    /// <inheritdoc/>
    public virtual CheckResult? Check(string configJson, IReadOnlyList<GivenAnswer> answers, int? seed) => null;

    /// <inheritdoc/>
    public virtual bool GradesAsSet(string configJson) => false;

    /// <inheritdoc/>
    /// <remarks>Throws on a config it cannot read; the catalog search catches and logs it per row (B-448).</remarks>
    public virtual int? ItemCount(string configJson, int itemTableRows) =>
        Manifest.ItemNoun is null ? null : ItemsOf(configJson).Count;

    /// <inheritdoc/>
    public virtual int DefaultStage => (int)TestStage.SelfAssess;

    /// <inheritdoc/>
    public virtual int PreviewStage => (int)TestStage.SelfAssess;

    /// <inheritdoc/>
    public virtual bool IsTypedStage(int stage) => true;

    /// <inheritdoc/>
    public virtual bool IsDisplayOnlyStage(int stage) => false;

    /// <inheritdoc/>
    public virtual bool IsLearnCardStage(int stage) => false;

    /// <inheritdoc/>
    public virtual IReadOnlyList<int> EaseLadder => [];

    /// <inheritdoc/>
    public virtual bool SupportsRemediation => false;

    /// <inheritdoc/>
    public virtual int? IntroductionStage =>
        StageOptions.Where(o => IsDisplayOnlyStage(o.Value)).Select(o => (int?)o.Value).FirstOrDefault();

    /// <inheritdoc/>
    public virtual int CardStage(int targetStage, int box) => targetStage;

    /// <inheritdoc/>
    public virtual int CardCeiling(ContentItem item, int stage) => stage;

    /// <inheritdoc/>
    public virtual ContentItem CardForStage(ContentItem item, int stage) => item;

    /// <inheritdoc/>
    public virtual IReadOnlyList<string>? Choices(string configJson, IReadOnlyList<ContentItem> items, ContentItem item, int stage) => null;
    /// <inheritdoc/>
    public virtual bool ChoicesShareOnePool(int stage) => false;
    /// <inheritdoc/>
    public virtual int? ChoiceFallbackStage(int stage) => null;
    /// <inheritdoc/>
    public virtual IReadOnlyList<string>? CueBox(string configJson, IReadOnlyList<ContentItem> items, int stage) => null;

    /// <inheritdoc/>
    public virtual (int? LetterBoxLength, string? AudioUrl, string? ImageUrl, string? AnswerPattern) StageFacets(ContentItem item, int stage) => (null, null, null, null);

    /// <inheritdoc/>
    public virtual string? LetterTileSource(ContentItem item, int stage) => null;

    /// <inheritdoc/>
    public virtual bool AudioReplacesPrompt(int stage) => false;

    /// <inheritdoc/>
    public virtual IReadOnlyList<StageOption> StageOptions => [];

    /// <inheritdoc/>
    public virtual ContentItem? StaticView(string configJson) => null;

    /// <inheritdoc/>
    public virtual bool SupportsItemProgress => false;

    /// <inheritdoc/>

    /// <inheritdoc/>
    public virtual bool SupportsObjectives => false;

    /// <inheritdoc/>
    public virtual bool SupportsRequireTypedTest => true;

    /// <inheritdoc/>
    public virtual StoreResolution StoreResolution => StoreResolution.None;

    /// <summary>Deserializes the typed config (never null; falls back to default).</summary>
    protected static TConfig Deserialize<TConfig>(string configJson) where TConfig : new() =>
        (string.IsNullOrWhiteSpace(configJson) ? default : JsonSerializer.Deserialize<TConfig>(configJson, JsonOptions)) ?? new();

    /// <summary>Expected solution plus optional alternatives as a raw comparison pool (normalization is done later by the grader).</summary>
    protected static IReadOnlyList<string> Accepted(string answer, IEnumerable<string>? alternatives = null) =>
        alternatives is null ? [answer] : [answer, .. alternatives];
}
