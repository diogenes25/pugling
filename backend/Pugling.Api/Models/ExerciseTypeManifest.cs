namespace Pugling.Api.Models;

// Selbstbeschreibung der Übungstypen: die EINE Wahrheit, aus der ein (gegen die API gebautes)
// Frontend Routing, Prüfmodus, Renderer und Fähigkeiten je Typ liest – statt sie fest zu verdrahten.
// Bündelt das heute über Katalog-Controller, positionsbezogene Play-Controller und Enums
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

    /// <summary>Server-autoritativer, mehrstufiger Abschlusstest über eine Study-Plan-Position: <c>study-plans/{planId}/positions/{positionId}/{PlayRoute}</c>.</summary>
    StudyPlanTest = 1,

    /// <summary>Zustandsloser Direkt-Check am Katalog-Endpunkt: <c>POST .../{AuthoringRoute}/{id}/check</c>.</summary>
    CatalogCheck = 2,

    /// <summary>Erst Aufgaben erzeugen (<c>POST .../{AuthoringRoute}/{id}/generate</c>), dann seed-gebunden prüfen (<c>.../check</c>).</summary>
    CatalogGenerateCheck = 3,
}

/// <summary>
/// Selbstbeschreibung eines Übungstyps: die Brücke zwischen Autoren-Katalog (<see cref="IExerciseType"/>),
/// typischer Lernfamilie (<see cref="LearningMethod"/>), Play-Route und Frontend-Renderer. Das Frontend
/// liest die Manifest-Liste einmal und verdrahtet Routing, Prüfung und Darstellung generisch; die
/// eigentliche Render-Komponente bleibt handgebaut pro <see cref="Renderer"/> (die Play-Sicht deckt
/// je Leitner-Stufe unterschiedlich viel auf – das lässt sich nicht generisch aus JSON erzeugen).
/// </summary>
/// <param name="Type">Übungstyp-Schlüssel (Autoren-Katalog, = <see cref="IExerciseType.Key"/> und Wert von <see cref="Exercise.Type"/>).</param>
/// <param name="Label">Deutscher Anzeigename.</param>
/// <param name="Renderer">Id der Frontend-Komponente; mehrere Typen dürfen sich einen Renderer teilen (z. B. Arithmetic + ArithmeticDrill → <c>arithmetic</c>).</param>
/// <param name="SchemaVersion">Version des Typ-Schemas. Bewusst NUR hier (nicht an den Entities) – Verzweigungspunkt für spätere inkompatible Änderungen.</param>
/// <param name="AuthoringRoute">Routen-Segment der Vater-CRUD unter <c>.../creator/subjects/{subjectId}/chapters/{chapterId}/{AuthoringRoute}</c>.</param>
/// <param name="CheckMode">Primäre Prüf-/Spieloberfläche.</param>
/// <param name="PlayRoute">Nur bei <see cref="ExerciseCheckMode.StudyPlanTest"/>: Segment unter <c>study-plans/{planId}/positions/{positionId}/{PlayRoute}</c>; sonst <c>null</c>.</param>
/// <param name="Method">Nur bei <see cref="ExerciseCheckMode.StudyPlanTest"/>: Lernfamilie für Renderer/Kompatibilität; sonst <c>null</c>.</param>
/// <param name="Capabilities">Typ-Fähigkeiten, auf die ein Renderer reagieren kann (z. B. <c>wordBank</c>, <c>audio</c>, <c>letterHints</c>).</param>
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
