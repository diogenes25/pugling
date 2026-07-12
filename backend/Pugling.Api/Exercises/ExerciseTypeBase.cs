using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Exercises;

/// <summary>
/// Bequeme Basis für Übungstypen: liefert für jede Facette einen sinnvollen Default (kein Check, immer getippt,
/// keine Auswahl/Facetten/Stufen, keine Capabilities, keine Store-Auflösung), sodass eine konkrete Typklasse nur
/// überschreibt, was sie wirklich braucht – analog zu den <c>virtual</c>-Hooks im <see cref="ExerciseControllerBase{TConfig}"/>.
/// <see cref="Key"/>, <see cref="Manifest"/> und <see cref="ItemsOf"/> sind bewusst abstrakt (jeder Typ hat sie).
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
    public virtual (int? LetterBoxLength, string? AudioUrl) StageFacets(ContentItem item, int stage) => (null, null);

    /// <inheritdoc/>
    public virtual IReadOnlyList<StageOption> StageOptions => [];

    /// <inheritdoc/>
    public virtual bool SupportsItemProgress => false;

    /// <inheritdoc/>
    public virtual bool SupportsLearnGoals => false;

    /// <inheritdoc/>
    public virtual bool SupportsObjectives => false;

    /// <inheritdoc/>
    public virtual StoreResolution StoreResolution => StoreResolution.None;

    /// <summary>Deserialisiert die typisierte Config (nie null; fällt auf Default zurück).</summary>
    protected static TConfig Deserialize<TConfig>(string configJson) where TConfig : new() =>
        (string.IsNullOrWhiteSpace(configJson) ? default : JsonSerializer.Deserialize<TConfig>(configJson, JsonOptions)) ?? new();

    /// <summary>Erwartete Lösung plus optionale Alternativen als roher Vergleichsvorrat (Normalisierung macht später der Grader).</summary>
    protected static IReadOnlyList<string> Accepted(string answer, IEnumerable<string>? alternatives = null) =>
        alternatives is null ? [answer] : [answer, .. alternatives];
}
