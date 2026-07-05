namespace Pugling.Api.Models;

// Gemeinsamer Lern-Katalog (learn):
//   Subject -> Chapter -> Exercise (typisiert)
// Der Katalog wird EINMAL gepflegt (nicht pro Kind) und später Kindern zugeordnet.

/// <summary>Schulfach im Lehrplan-Katalog (z. B. Englisch, Mathe).</summary>
public class Subject
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Chapter> Chapters { get; set; } = new();

    /// <summary>Fachabhängige Übungs-Arten (z. B. Grammatik/Vokabeln bei Englisch).</summary>
    public List<ExerciseCategory> Categories { get; set; } = new();
}

/// <summary>
/// Schularten, für die eine Übung geeignet ist. <c>[Flags]</c>-Enum, damit eine Übung
/// mehreren Schularten zugeordnet werden kann (z. B. Realschule | Gymnasium).
/// <see cref="None"/> bedeutet „für alle Schularten" (kein Filter-Ausschluss).
/// </summary>
[Flags]
public enum SchoolTypes
{
    None = 0,
    Grundschule = 1,
    Hauptschule = 2,
    Realschule = 4,
    Gymnasium = 8,
    Gesamtschule = 16,
    Berufsschule = 32,
}

/// <summary>
/// Fachabhängige „Art" einer Übung (z. B. Grammatik/Vokabeln bei Sprachen,
/// Grundrechenarten/Algebra bei Mathe). Kindneutrales, kontrolliertes Vokabular je Fach –
/// dient der Vorfilterung von Übungen bei der Lehrplan-Erstellung.
/// </summary>
public class ExerciseCategory
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public Subject? Subject { get; set; }
    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Kapitel innerhalb eines Fachs.</summary>
public class Chapter
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public Subject? Subject { get; set; }
    public string Name { get; set; } = "";
    public int OrderIndex { get; set; }

    public List<Exercise> Exercises { get; set; } = new();
}

/// <summary>Art der Übung. Bestimmt, wie <see cref="Exercise.ConfigJson"/> interpretiert wird.</summary>
public enum ExerciseType
{
    Vocabulary = 0,
    Reading = 1,
    Cloze = 2,
    Essay = 3,
    Listening = 4,
    Grammar = 5,
    Matching = 6,
    Translation = 7,
    Arithmetic = 8,
    ArithmeticDrill = 9,
    List = 10,
    Birkenbihl = 11,
}

/// <summary>
/// Vom Übungsersteller vorgeschlagenes Bonus-System (global an der Übung). Dient nur als Vorlage:
/// beim Erzeugen eines Lehrplans aus der Übung werden diese Werte EINMAL in dessen Bonus-Felder
/// kopiert. Spätere Änderungen an der Übung wirken damit NICHT rückwirkend auf bestehende Kind-Pläne –
/// das laufende Bonus-System bleibt kind-individuell und pro Lehrplan anpassbar (Motivations-Steuerung
/// je Kind/Übung). Felder spiegeln die Bonus-Knöpfe des <see cref="StudyPlan"/>.
/// </summary>
public record SuggestedBonus(
    int ComboThreshold,
    int ComboBonusPoints,
    int SpeedThresholdSeconds,
    int SpeedBonusPoints,
    int NewContentPoints);

/// <summary>
/// Eine Übung in einem Kapitel. Die gemeinsamen Felder sind typisiert;
/// der typ-spezifische Teil steckt als JSON in <see cref="ConfigJson"/>
/// und wird im API pro Typ als eigenes Schema ein-/ausgegeben.
/// </summary>
public class Exercise
{
    public int Id { get; set; }
    public int ChapterId { get; set; }
    public Chapter? Chapter { get; set; }
    public ExerciseType Type { get; set; }
    public string Title { get; set; } = "";
    public int OrderIndex { get; set; }
    /// <summary>Punkte, die das Kind für das Absolvieren erhält.</summary>
    public int RewardPoints { get; set; }
    /// <summary>Typ-spezifische Konfiguration als JSON (siehe die *Config-Klassen).</summary>
    public string ConfigJson { get; set; } = "{}";
    /// <summary>Optionaler Bonus-Vorschlag des Erstellers (Vorlage, wird beim Plan-Erzeugen kopiert).</summary>
    public SuggestedBonus? SuggestedBonus { get; set; }

    // Vorschlags-Defaults für eine Lehrplan-Position (Hybrid-Prinzip: die Position erbt sie,
    // solange sie nicht selbst übersteuert – siehe PlanPosition.Stage/ItemCount).
    /// <summary>Empfohlene Teststufe (verfahrensabhängig interpretiert); null = Verfahrens-Standard.</summary>
    public int? DefaultStage { get; set; }
    /// <summary>Empfohlene Anzahl genutzter Inhalte je Position; null = alle.</summary>
    public int? DefaultItemCount { get; set; }

    // Strukturierte Metadaten zur Vorfilterung bei der Lehrplan-Erstellung.
    // Fach = Subject (über Chapter), Thema = Chapter – hier nur das Ergänzende.

    /// <summary>Unterste geeignete Klassenstufe (inklusive); null = keine Untergrenze.</summary>
    public int? GradeMin { get; set; }
    /// <summary>Oberste geeignete Klassenstufe (inklusive); null = keine Obergrenze.</summary>
    public int? GradeMax { get; set; }
    /// <summary>Geeignete Schularten; <see cref="SchoolTypes.None"/> = für alle.</summary>
    public SchoolTypes SchoolTypes { get; set; } = SchoolTypes.None;
    /// <summary>Quelle der Übung (z. B. Schulbuch „Green Line 3, Unit 4"); optional.</summary>
    public string? Source { get; set; }
    /// <summary>Fachabhängige Art (FK auf <see cref="ExerciseCategory"/>); optional.</summary>
    public int? CategoryId { get; set; }
    public ExerciseCategory? Category { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
