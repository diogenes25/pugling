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
    /// <summary>Übungstyp-Schlüssel (z. B. <c>"Vocabulary"</c>) – aufgelöst über die <see cref="ExerciseTypeRegistry"/>; bestimmt, wie <see cref="ConfigJson"/> interpretiert wird.</summary>
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    /// <summary>
    /// Freier Beschreibungstext (optional). Hilft, die Übung beim Zusammenstellen eines Lehrplans
    /// zu erkennen (was übt sie, für wen, worauf achten) und fließt in die Katalog-Freitextsuche ein.
    /// </summary>
    public string? Description { get; set; }
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
    /// <summary>Standard für den Leitner-Kasten (Vorschlag der Übung; Position kann übersteuern).</summary>
    public bool DefaultUseLeitner { get; set; }
    /// <summary>Standard „nur getippte/gewertete Tests zählen" (Vorschlag der Übung; Position kann übersteuern).</summary>
    public bool DefaultRequireTypedTest { get; set; }

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

    /// <summary>
    /// Autor der Übung (der Vater, der sie angelegt hat). Der Katalog ist bewusst <b>global</b>:
    /// jeder Vater darf jede Übung <i>finden und verwenden</i>, aber nur der Autor darf sie
    /// <i>ändern oder löschen</i> – so bleibt die von einem Lehrer erstellte Übung geschützt,
    /// während andere Väter sie in ihre Lehrpläne übernehmen. <c>null</c> = geseedete System-Übung
    /// (gehört niemandem, entsprechend nicht editierbar). Bleibt beim Löschen des Autors erhalten
    /// (FK → <c>SetNull</c>), damit fremde Lehrpläne, die sie referenzieren, nicht brechen.
    /// </summary>
    public int? AuthorFatherId { get; set; }
    public Father? Author { get; set; }

    /// <summary>
    /// Ob die Übung <b>für alle</b> Creator ausführbar (in Lehrpläne/Klassenarbeiten aufnehmbar) ist.
    /// <c>true</c> (Default) = bisheriges Verhalten (jeder darf zuweisen). Setzt ein Owner sie auf <c>false</c>,
    /// dürfen nur Owner und Creator mit einem Execute-/Write-<see cref="ExerciseGrant"/> die Übung zuweisen.
    /// Wirkt nur auf <i>neue</i> Zuweisungen – bereits laufende Pläne bleiben unberührt.
    /// </summary>
    public bool ExecutePublic { get; set; } = true;

    /// <summary>Vergebene RWX-Rechte an einzelne Creator (Owner/Write/Execute) – siehe <see cref="ExerciseGrant"/>.</summary>
    public List<ExerciseGrant> Grants { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Ein an einen einzelnen Creator vergebenes Recht auf eine Übung. Ersetzt das frühere 1-Autor-Modell:
/// Der ursprüngliche <see cref="Exercise.AuthorFatherId"/> wird per Migration zum ersten <see cref="GrantPermission.Owner"/>;
/// weitere Owner/Write/Execute-Rechte kommen über diese Tabelle hinzu (Co-Authoring, kontrollierte Weitergabe).
/// Muster analog <see cref="SupervisorLink"/> (Surrogat-PK + eindeutiger Composite-Index, beide FKs Cascade).
/// </summary>
public class ExerciseGrant
{
    public int Id { get; set; }
    public int ExerciseId { get; set; }
    public Exercise? Exercise { get; set; }
    /// <summary>Begünstigter Creator (= <see cref="Father.Id"/>).</summary>
    public int CreatorId { get; set; }
    public Father? Creator { get; set; }
    public GrantPermission Permission { get; set; }
    /// <summary>Audit: welcher Vater das Recht vergeben hat (null bei Migrations-Seed).</summary>
    public int? GrantedByFatherId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
