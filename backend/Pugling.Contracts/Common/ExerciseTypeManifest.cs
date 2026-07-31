namespace Pugling.Contracts;

/// <summary>A query form that can be switched in test mode (stage value + display label).</summary>
public record StageOption(int Value, string Label);

/// <summary>
/// How an exercise of this type is primarily checked/played. Describes the API surface that
/// actually exists – not a wish list. When a new check is added, the type moves to the matching
/// mode (and the <see cref="ExerciseTypeManifest.SchemaVersion"/> in the manifest is incremented).
/// </summary>
public enum ExerciseCheckMode
{
    /// <summary>No automatic check – a pure content/reading exercise (e.g. Birkenbihl) or one that cannot (yet) be machine-graded (e.g. essay).</summary>
    None = 0,

    /// <summary>Server-authoritative, multi-stage final test on a study plan position: <c>study-plans/{planId}/positions/{positionId}/{PlayRoute}</c>.</summary>
    StudyPlanTest = 1,

    /// <summary>Stateless direct check at the catalog endpoint: <c>POST .../{AuthoringRoute}/{id}/check</c>.</summary>
    CatalogCheck = 2,

    /// <summary>First generate tasks (<c>POST .../{AuthoringRoute}/{id}/generate</c>), then check against the bound seed (<c>.../check</c>).</summary>
    CatalogGenerateCheck = 3,
}

/// <summary>
/// Self-description of an exercise type: the bridge between the authoring catalog (<c>IExerciseType</c>),
/// typical learning family (<see cref="LearningMethod"/>), play route, and frontend renderer. The frontend
/// reads the manifest list once and wires up routing, checking, and display generically; the
/// actual render component remains hand-built per <see cref="Renderer"/> (the play view reveals
/// a different amount depending on the Leitner box – that cannot be generated generically from JSON).
/// </summary>
/// <param name="Type">Exercise type key (authoring catalog, = <c>IExerciseType.Key</c> and the value of <c>Exercise.Type</c>).</param>
/// <param name="Label">German display name.</param>
/// <param name="Renderer">Id of the frontend component; multiple types may share one renderer (e.g. Arithmetic + ArithmeticDrill → <c>arithmetic</c>).</param>
/// <param name="SchemaVersion">Version of the type schema. Deliberately ONLY here (not on the entities) – branch point for later incompatible changes.</param>
/// <param name="AuthoringRoute">Route segment of the creator's CRUD under <c>.../creator/subjects/{subjectId}/chapters/{chapterId}/{AuthoringRoute}</c>.</param>
/// <param name="CheckMode">Primary check/play surface.</param>
/// <param name="PlayRoute">Only for <see cref="ExerciseCheckMode.StudyPlanTest"/>: segment under <c>study-plans/{planId}/positions/{positionId}/{PlayRoute}</c>; otherwise <c>null</c>.</param>
/// <param name="Method">Only for <see cref="ExerciseCheckMode.StudyPlanTest"/>: learning family for renderer/compatibility; otherwise <c>null</c>.</param>
/// <param name="Capabilities">Type capabilities that a renderer can react to (e.g. <c>wordBank</c>, <c>audio</c>, <c>letterHints</c>).</param>
public record ExerciseTypeManifest(
    string Type,
    string Label,
    string Renderer,
    int SchemaVersion,
    string AuthoringRoute,
    ExerciseCheckMode CheckMode,
    string? PlayRoute,
    LearningMethod? Method,
    IReadOnlyList<string> Capabilities);
