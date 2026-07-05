namespace Pugling.Api.Models;

// Selbstbeschreibung der Übungstypen: die EINE Wahrheit, aus der ein (gegen die API gebautes)
// Frontend Routing, Prüfmodus, Renderer und Fähigkeiten je Typ liest – statt sie fest zu verdrahten.
// Bündelt das heute über Katalog-Controller, Play-Controller (study-plans/…/*-tests) und Enums
// (ExerciseType ↔ LearningMethod) verstreute Wissen an einer Stelle.

/// <summary>
/// Wie eine Übung dieses Typs primär geprüft/gespielt wird. Beschreibt die tatsächlich vorhandene
/// API-Oberfläche – kein Wunschbild. Kommt eine Prüfung neu hinzu, wandert der Typ auf den passenden
/// Modus (und die <see cref="ExerciseTypeManifest.SchemaVersion"/> im Manifest wird erhöht).
/// </summary>
public enum ExerciseCheckMode
{
    /// <summary>Keine automatische Prüfung – reine Inhalts-/Leseübung (z. B. Birkenbihl) oder (noch) nicht maschinell bewertbar (z. B. Aufsatz).</summary>
    None = 0,

    /// <summary>Server-autoritativer, mehrstufiger Abschlusstest über einen Study-Plan (Leitner): <c>study-plans/{planId}/{PlayRoute}</c>.</summary>
    StudyPlanTest = 1,

    /// <summary>Zustandsloser Direkt-Check am Katalog-Endpunkt: <c>POST .../{AuthoringRoute}/{id}/check</c>.</summary>
    CatalogCheck = 2,

    /// <summary>Erst Aufgaben erzeugen (<c>POST .../{AuthoringRoute}/{id}/generate</c>), dann seed-gebunden prüfen (<c>.../check</c>).</summary>
    CatalogGenerateCheck = 3,
}

/// <summary>
/// Selbstbeschreibung eines Übungstyps: die Brücke zwischen Autoren-Katalog (<see cref="ExerciseType"/>),
/// Lehrplan-Verfahren (<see cref="LearningMethod"/>), Play-Route und Frontend-Renderer. Das Frontend
/// liest die Manifest-Liste einmal und verdrahtet Routing, Prüfung und Darstellung generisch; die
/// eigentliche Render-Komponente bleibt handgebaut pro <see cref="Renderer"/> (die Play-Sicht deckt
/// je Leitner-Stufe unterschiedlich viel auf – das lässt sich nicht generisch aus JSON erzeugen).
/// </summary>
/// <param name="Type">Übungstyp (Autoren-Katalog).</param>
/// <param name="Label">Deutscher Anzeigename.</param>
/// <param name="Renderer">Id der Frontend-Komponente; mehrere Typen dürfen sich einen Renderer teilen (z. B. Arithmetic + ArithmeticDrill → <c>arithmetic</c>).</param>
/// <param name="SchemaVersion">Version des Typ-Schemas. Bewusst NUR hier (nicht an den Entities) – Verzweigungspunkt für spätere inkompatible Änderungen.</param>
/// <param name="AuthoringRoute">Routen-Segment der Vater-CRUD unter <c>.../learn/subjects/{subjectId}/chapters/{chapterId}/{AuthoringRoute}</c>.</param>
/// <param name="CheckMode">Primäre Prüf-/Spieloberfläche.</param>
/// <param name="PlayRoute">Nur bei <see cref="ExerciseCheckMode.StudyPlanTest"/>: Segment unter <c>study-plans/{planId}/{PlayRoute}</c>; sonst <c>null</c>.</param>
/// <param name="Method">Nur bei <see cref="ExerciseCheckMode.StudyPlanTest"/>: zugehöriges Lehrplan-Verfahren; sonst <c>null</c>.</param>
/// <param name="Capabilities">Typ-Fähigkeiten, auf die ein Renderer reagieren kann (z. B. <c>wordBank</c>, <c>audio</c>, <c>letterHints</c>).</param>
public record ExerciseTypeManifest(
    ExerciseType Type,
    string Label,
    string Renderer,
    int SchemaVersion,
    string AuthoringRoute,
    ExerciseCheckMode CheckMode,
    string? PlayRoute,
    LearningMethod? Method,
    IReadOnlyList<string> Capabilities);

/// <summary>
/// Registry aller Übungstyp-Manifeste – die eine Wahrheit für Routing/Prüfung/Darstellung.
/// Ein neuer Übungstyp wird HIER eingetragen; ein Konsistenztest erzwingt Vollständigkeit
/// (jeder <see cref="ExerciseType"/> → genau ein Eintrag) und die Invarianten je Prüfmodus.
/// </summary>
public static class ExerciseManifests
{
    // Bis zur ersten inkompatiblen Schema-Änderung tragen alle Typen Version 1.
    private const int V1 = 1;

    /// <summary>Manifest aller bekannten Übungstypen (Reihenfolge = <see cref="ExerciseType"/>).</summary>
    public static IReadOnlyList<ExerciseTypeManifest> All { get; } =
    [
        new(ExerciseType.Vocabulary, "Vokabeln", "flashcards", V1, "vocabulary",
            ExerciseCheckMode.StudyPlanTest, "tests", LearningMethod.Vocabulary,
            ["letterHints", "audio", "selfAssess"]),
        new(ExerciseType.Reading, "Leseverständnis", "reading", V1, "reading",
            ExerciseCheckMode.None, null, null, []),
        new(ExerciseType.Cloze, "Lückentext", "cloze", V1, "cloze",
            ExerciseCheckMode.StudyPlanTest, "cloze-tests", LearningMethod.Cloze,
            ["wordBank", "translation", "letterHints", "vocabStore"]),
        new(ExerciseType.Essay, "Aufsatz", "essay", V1, "essays",
            ExerciseCheckMode.None, null, null, ["rubric", "wordCount"]),
        new(ExerciseType.Listening, "Hörverständnis", "listening", V1, "listening",
            ExerciseCheckMode.None, null, null, ["audio", "transcript"]),
        new(ExerciseType.Grammar, "Grammatik", "prompts", V1, "grammar",
            ExerciseCheckMode.None, null, null, ["ruleHints"]),
        new(ExerciseType.Matching, "Zuordnung", "matching", V1, "matching",
            ExerciseCheckMode.StudyPlanTest, "matching-tests", LearningMethod.Matching,
            ["distractors", "reverse"]),
        new(ExerciseType.Translation, "Übersetzung", "prompts", V1, "translation",
            ExerciseCheckMode.None, null, null, ["alternatives"]),
        new(ExerciseType.Arithmetic, "Rechenaufgaben", "arithmetic", V1, "arithmetic",
            ExerciseCheckMode.CatalogCheck, null, null, ["tolerance"]),
        new(ExerciseType.ArithmeticDrill, "Rechen-Drill", "arithmetic", V1, "arithmetic-drill",
            ExerciseCheckMode.CatalogGenerateCheck, null, null, ["generated", "seed"]),
        new(ExerciseType.List, "Liste", "list", V1, "list",
            ExerciseCheckMode.CatalogCheck, null, null, ["orderedOptional", "alternatives"]),
        new(ExerciseType.Birkenbihl, "Birkenbihl", "birkenbihl", V1, "birkenbihl",
            ExerciseCheckMode.None, null, null, ["wordByWord"]),
    ];

    /// <summary>Manifest zu einem Typ, oder <c>null</c>, wenn keins hinterlegt ist.</summary>
    public static ExerciseTypeManifest? ByType(ExerciseType type) =>
        All.FirstOrDefault(m => m.Type == type);
}
