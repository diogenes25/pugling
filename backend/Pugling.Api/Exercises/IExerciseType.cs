using Pugling.Api.Models;

namespace Pugling.Api.Exercises;

/// <summary>
/// Wie die Inhalte eines Übungstyps zu einzelnen <see cref="ContentItem"/>s aufgelöst werden.
/// Reine Konfig-Projektion (<see cref="None"/>) oder – bei den Vokabel-gestützten Typen – zusätzlich
/// aus der Datenbank (Store). Der Contract trägt bewusst nur dieses Flag; die DB-Logik selbst bleibt
/// im <see cref="ExerciseContentResolver"/> (kein DbContext im Plugin-Contract).
/// </summary>
public enum StoreResolution
{
    /// <summary>Inhalte kommen ausschließlich aus der <c>ConfigJson</c> (zustandslos).</summary>
    None = 0,
    /// <summary>Vokabel-Items liegen als eigene Tabelle (<see cref="ExerciseItem"/>) und verweisen auf den Store.</summary>
    ItemTable = 1,
    /// <summary>Lücken referenzieren Store-Vokabeln per Key (<see cref="Gap.VocabKey"/>).</summary>
    VocabRefs = 2,
}

// StageOption lebt im Vertrags-Projekt (Pugling.Contracts).

/// <summary>
/// Ein Übungstyp als selbstbeschreibende Einheit („ein Typ = eine Klasse"). Ersetzt das frühere geschlossene
/// <c>ExerciseType</c>-Enum samt der verstreuten <c>switch</c>-/<c>== Vocabulary</c>-Stellen: alle typ-spezifischen
/// Regeln (Content-Projektion, Antwort-Prüfung, Play-/Preview-Facetten, Fähigkeiten) leben hier. Aufgelöst wird
/// über die <see cref="ExerciseTypeRegistry"/> per stabilem <see cref="Key"/> (= Wire-/DB-Wert). Implementierungen
/// sind zustandslos (Singleton) und erben sinnvolle Defaults aus <see cref="ExerciseTypeBase"/>.
/// </summary>
public interface IExerciseType
{
    /// <summary>Stabiler Schlüssel des Typs (z. B. <c>"Vocabulary"</c>) – zugleich Wert von <see cref="Exercise.Type"/> und im Manifest.</summary>
    string Key { get; }

    /// <summary>Selbstbeschreibung für Routing/Prüfung/Darstellung (Label, Renderer, CheckMode, Capabilities …).</summary>
    ExerciseTypeManifest Manifest { get; }

    /// <summary>Projiziert die Inhalte der Übung verfahrensneutral aus ihrer <c>ConfigJson</c>. Typen ohne Item-für-Item-Abgleich liefern eine leere Liste.</summary>
    IReadOnlyList<ContentItem> ItemsOf(string configJson);

    /// <summary>
    /// Bewertet die Antworten des Kindes am Katalog-Endpunkt (nur bei CheckMode <c>CatalogCheck</c>/<c>CatalogGenerateCheck</c>).
    /// <paramref name="seed"/> ist nur für seed-gebundene Typen (Rechen-Drill) relevant. Standard: <c>null</c> (kein Direkt-Check).
    /// </summary>
    CheckResult? Check(string configJson, IReadOnlyList<GivenAnswer> answers, int? seed);

    /// <summary>Verfahrens-Standardstufe, wenn weder Fahrplan noch Positions-/Übungs-Override eine Stufe vorgeben.</summary>
    int DefaultStage { get; }

    /// <summary>Repräsentative Stufe fürs Ausprobieren im Vater-Testmodus (i. d. R. die aussagekräftigste getippte Stufe).</summary>
    int PreviewStage { get; }

    /// <summary>Ist die Stufe „getippt"/objektiv (serverseitig gegen die Lösung prüfbar) statt reiner Selbsteinschätzung?</summary>
    bool IsTypedStage(int stage);

    /// <summary>Multiple-Choice-Auswahl für die Aufgabe (richtige Antwort + Ablenker) oder <c>null</c>, wenn der Typ/die Stufe keine Auswahl kennt.</summary>
    IReadOnlyList<string>? Choices(IReadOnlyList<ContentItem> items, ContentItem item, int stage);

    /// <summary>
    /// Typ-spezifische Karten-Facetten je Stufe: Buchstabenkästchen-Länge, Audioquelle und/oder Bild
    /// (sonst <c>null</c>). Hier – und nur hier – entscheidet der Typ auch, ob ein Bild die Lösung
    /// verriete: es ist beim Bild <b>schärfer</b> als beim Audio, weil ein Motiv die Bedeutung in beide
    /// Abfragerichtungen zeigt, während die Aussprache nur ein Wort vorliest.
    /// </summary>
    (int? LetterBoxLength, string? AudioUrl, string? ImageUrl) StageFacets(ContentItem item, int stage);

    /// <summary>Die im Testmodus umschaltbaren Abfrageformen (leer, wenn der Typ nur eine Form kennt).</summary>
    IReadOnlyList<StageOption> StageOptions { get; }

    /// <summary>Trägt der Typ plan-übergreifenden Item-Lernstand (heute nur Vokabeln)?</summary>
    bool SupportsItemProgress { get; }

    /// <summary>Dürfen für diesen Typ Lernziele (LearnGoals) gesetzt werden?</summary>
    bool SupportsLearnGoals { get; }

    /// <summary>Dürfen für diesen Typ Objectives/KeyResults gesetzt werden?</summary>
    bool SupportsObjectives { get; }

    /// <summary>Wie die Inhalte aufgelöst werden (rein aus Config oder zusätzlich aus dem Store).</summary>
    StoreResolution StoreResolution { get; }
}

/// <summary>
/// Ein Übungstyp, dessen konkrete Aufgaben pro Abruf serverseitig erzeugt werden (Rechen-Drill). Der Satz wird aus
/// einem festen <c>Seed</c> reproduzierbar erzeugt, damit die spätere Prüfung (<see cref="IExerciseType.Check"/>)
/// exakt denselben Satz bewerten kann.
/// </summary>
public interface IGeneratingExerciseType : IExerciseType
{
    /// <summary>Erzeugt einen Aufgabensatz nach den gespeicherten Regeln und gibt den verwendeten Seed zurück (fürs spätere Prüfen).</summary>
    (int Seed, IReadOnlyList<GeneratedProblem> Problems) Generate(string configJson, int? seed);
}

/// <summary>
/// Stabile Schlüssel der eingebauten Übungstypen – für die wenigen Stellen, die einen konkreten Built-in meinen
/// (Seed, Store-Verlinkung, Backfill), statt eines generischen Capability-Flags. Nie mehr Magic Strings.
/// </summary>
public static class ExerciseTypeKeys
{
    /// <summary>Schlüssel des Vokabeltrainings.</summary>
    public const string Vocabulary = "Vocabulary";
    /// <summary>Schlüssel des Leseverständnisses.</summary>
    public const string Reading = "Reading";
    /// <summary>Schlüssel des Lückentexts.</summary>
    public const string Cloze = "Cloze";
    /// <summary>Schlüssel des Aufsatzes.</summary>
    public const string Essay = "Essay";
    /// <summary>Schlüssel des Hörverständnisses.</summary>
    public const string Listening = "Listening";
    /// <summary>Schlüssel der Grammatikübung.</summary>
    public const string Grammar = "Grammar";
    /// <summary>Schlüssel der Zuordnungsübung.</summary>
    public const string Matching = "Matching";
    /// <summary>Schlüssel der Übersetzungsübung.</summary>
    public const string Translation = "Translation";
    /// <summary>Schlüssel der festen Rechenaufgaben.</summary>
    public const string Arithmetic = "Arithmetic";
    /// <summary>Schlüssel des Rechen-Drills (seed-basiert generiert).</summary>
    public const string ArithmeticDrill = "ArithmeticDrill";
    /// <summary>Schlüssel der Listenübung (Auswendiglernen).</summary>
    public const string List = "List";
    /// <summary>Schlüssel der Birkenbihl-Wort-für-Wort-Dekodierung.</summary>
    public const string Birkenbihl = "Birkenbihl";
}
