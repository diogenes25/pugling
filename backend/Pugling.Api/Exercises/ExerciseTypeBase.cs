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
    public virtual int DefaultStage => (int)TestStage.SelfAssess;

    /// <inheritdoc/>
    public virtual int PreviewStage => (int)TestStage.SelfAssess;

    /// <inheritdoc/>
    public virtual bool IsTypedStage(int stage) => true;

    /// <inheritdoc/>
    public virtual IReadOnlyList<string>? Choices(IReadOnlyList<ContentItem> items, ContentItem item, int stage) => null;

    /// <inheritdoc/>
    public virtual (int? LetterBoxLength, string? AudioUrl, string? ImageUrl) StageFacets(ContentItem item, int stage) => (null, null, null);

    /// <inheritdoc/>
    public virtual IReadOnlyList<StageOption> StageOptions => [];

    /// <inheritdoc/>
    public virtual bool SupportsItemProgress => false;

    /// <inheritdoc/>

    /// <inheritdoc/>
    public virtual bool SupportsObjectives => false;

    /// <inheritdoc/>
    public virtual StoreResolution StoreResolution => StoreResolution.None;

    /// <summary>Deserializes the typed config (never null; falls back to default).</summary>
    protected static TConfig Deserialize<TConfig>(string configJson) where TConfig : new() =>
        (string.IsNullOrWhiteSpace(configJson) ? default : JsonSerializer.Deserialize<TConfig>(configJson, JsonOptions)) ?? new();

    /// <summary>Expected solution plus optional alternatives as a raw comparison pool (normalization is done later by the grader).</summary>
    protected static IReadOnlyList<string> Accepted(string answer, IEnumerable<string>? alternatives = null) =>
        alternatives is null ? [answer] : [answer, .. alternatives];
}
