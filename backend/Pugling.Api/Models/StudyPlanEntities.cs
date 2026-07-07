namespace Pugling.Api.Models;

// Lehrplan-Modell: Ein Lehrplan ist ein reiner Container aus referenzierten Katalog-Übungen
// (siehe PlanPosition). Zeit-/Punkte-/Leitner-Steuerung, Stufen und Ziele hängen an der jeweiligen
// Position, nicht mehr am Plan. Verfahrens-spezifisch sind nur der Inhalt (Übungs-Config) und die
// Test-Mechanik/Stufen (siehe PositionPlayService / PositionTestsController).

/// <summary>
/// Lernverfahren – nur noch die Selbstbeschreibung im Übungstyp-Manifest (<see cref="ExerciseTypeManifest"/>)
/// braucht diese Zuordnung. Kein plan-weites Verfahren mehr.
/// </summary>
public enum LearningMethod { Vocabulary = 0, Cloze = 1, Matching = 2 }

/// <summary>Stufe des Zuordnungs-Verfahrens (steigende Schwierigkeit). Nutzt den Vokabel-Store.</summary>
public enum MatchStage
{
    /// <summary>Wort → Übersetzung, keine Ablenker.</summary>
    Direct = 1,
    /// <summary>Wort → Übersetzung, mit Zusatz-Ablenkern im Auswahl-Pool.</summary>
    Distractors = 2,
    /// <summary>Übersetzung → Wort, keine Ablenker.</summary>
    Reverse = 3,
    /// <summary>Übersetzung → Wort, mit Ablenkern.</summary>
    ReverseDistractors = 4,
}

/// <summary>Teststufe des Vokabel-Lernkartentests (steigende Schwierigkeit).</summary>
public enum TestStage
{
    /// <summary>Vokabel + Übersetzung werden angezeigt (Kennenlernen).</summary>
    ShowBoth = 1,
    /// <summary>Vokabel -> aufdecken -> Selbsteinschätzung "gewusst? Ja/Nein".</summary>
    SelfAssess = 2,
    /// <summary>Übersetzung tippen; Länge bekannt (Buchstabenfelder), Buchstaben-Tipps möglich.</summary>
    LetterBoxes = 3,
    /// <summary>Übersetzung frei eintippen.</summary>
    FreeText = 4,
    /// <summary>Vokabel wird vorgelesen -> Übersetzung frei eintippen.</summary>
    Audio = 5,
    /// <summary>Auswahl aus mehreren Möglichkeiten (eine richtig, Rest Ablenker aus der Übung).</summary>
    MultipleChoice = 6,
}

/// <summary>Ein Schritt im Stufen-Fahrplan: ab Tag <c>DayNumber</c> (1-basiert) gilt Stufe <c>Stage</c>.</summary>
public record StageStep(int DayNumber, int Stage);

/// <summary>
/// Vom Vater erstellter Lehrplan für ein Kind: ein <b>Container</b>, der Katalog-Übungen als
/// <see cref="PlanPosition"/>en bündelt. Titel, Kind und Laufzeit gehören hierher; alles Lern-Spezifische
/// (Ziel, Punkte, Stufe, Leitner) trägt die einzelne Position.
/// </summary>
public class StudyPlan
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    public string Title { get; set; } = "";
    /// <summary>Freie Beschreibung des Plans (optional): Ziel/Umfang, damit er später gut erkennbar bleibt.</summary>
    public string? Description { get; set; }
    /// <summary>Optionale Verknüpfung zum Katalog-Fach (nur zur Einordnung/Filterung).</summary>
    public int? SubjectId { get; set; }
    public Subject? Subject { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Die Positionen des Plans: referenzierte Katalog-Übungen mit eigenem Ziel/Punkten/Leitner.</summary>
    public List<PlanPosition> Positions { get; set; } = new();
}

/// <summary>
/// Ausspiel-Modus einer Übungssitzung. <see cref="Info"/> = freies Üben: Inhalte am Stück, das Frontend
/// führt die Iteration, es fließt <b>kein</b> Lernfeedback (keine Bewertung/Punkte/Leitner, zählt nicht aufs
/// Ziel). <see cref="Lern"/> = server-geführt: der Server hält Cursor + eingefrorene Reihenfolge und bewertet.
/// </summary>
public enum PlayMode { Info = 0, Lern = 1 }

/// <summary>Übungssitzung einer Lehrplan-Position: erfasst echte Übungszeit und was geübt wurde.</summary>
public class PracticeSession
{
    public int Id { get; set; }
    public int StudyPlanId { get; set; }
    public StudyPlan? StudyPlan { get; set; }
    /// <summary>Position (Übung), zu der die Sitzung gehört.</summary>
    public int? PlanPositionId { get; set; }
    public PlanPosition? PlanPosition { get; set; }
    public DateOnly Day { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    /// <summary>Aktiv geübte Sekunden (nur Zeit mit Interaktion).</summary>
    public int ActiveSeconds { get; set; }

    /// <summary>Ausspiel-Modus (Info = frei, Lern = server-geführt mit Cursor).</summary>
    public PlayMode Mode { get; set; } = PlayMode.Lern;
    /// <summary>
    /// Beim Start eingefrorene Ausspiel-Reihenfolge (Item-Indizes) gemäß <see cref="PlanPosition.OrderStrategy"/>.
    /// Bleibt über den Lauf stabil, damit sich die Reihenfolge nicht durch Box-Änderungen verschiebt.
    /// </summary>
    public List<int> Order { get; set; } = new();
    /// <summary>Aktuelle Position in <see cref="Order"/> (server-geführter Cursor im Lern-Modus).</summary>
    public int Cursor { get; set; }

    public List<ReviewEvent> Reviews { get; set; } = new();
}

/// <summary>Einzelne Wiederholung innerhalb einer Übungssitzung (verfahrensneutral).</summary>
public class ReviewEvent
{
    public int Id { get; set; }
    public int PracticeSessionId { get; set; }
    public PracticeSession? PracticeSession { get; set; }
    /// <summary>Übungs-Id der Position (der Inhalt lebt in der Übungs-Config).</summary>
    public int ContentId { get; set; }
    /// <summary>Index des Inhaltsatoms in der Übung der Position.</summary>
    public int? ItemIndex { get; set; }
    public int StageValue { get; set; }
    public bool WasCorrect { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
}

/// <summary>Ein Abschlusstest-Versuch einer Position an einem Tag (verfahrensneutral).</summary>
public class TestAttempt
{
    public int Id { get; set; }
    public int StudyPlanId { get; set; }
    public StudyPlan? StudyPlan { get; set; }
    /// <summary>Position (Übung), zu der der Test gehört.</summary>
    public int? PlanPositionId { get; set; }
    public PlanPosition? PlanPosition { get; set; }
    public DateOnly Day { get; set; }
    /// <summary>Stufe (je nach Verfahren TestStage bzw. ClozeStage).</summary>
    public int StageValue { get; set; }
    /// <summary>Gilt dieser Versuch als "gewertet" (getippt/Freitext)? Setzt der Controller.</summary>
    public bool Graded { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int TotalItems { get; set; }
    public int CorrectItems { get; set; }
    public int ScorePercent { get; set; }
    public bool Passed { get; set; }

    /// <summary>
    /// Beim Start eingefrorene Prüfungsreihenfolge (Item-Indizes) gemäß <see cref="PlanPosition.OrderStrategy"/>.
    /// Der Klausur-Modus ist strikt server-getrieben: eine Frage nach der anderen, kein Zurück.
    /// </summary>
    public List<int> Order { get; set; } = new();
    /// <summary>Aktuelle Position in <see cref="Order"/> (server-geführter Cursor der Prüfung).</summary>
    public int Cursor { get; set; }

    public List<TestItemResult> Results { get; set; } = new();
}

/// <summary>Ergebnis einer einzelnen Test-Position (ein Inhalts-Atom der Übung).</summary>
public class TestItemResult
{
    public int Id { get; set; }
    public int TestAttemptId { get; set; }
    public TestAttempt? TestAttempt { get; set; }
    /// <summary>Übungs-Id der Position (der Inhalt lebt in der Übungs-Config).</summary>
    public int ContentId { get; set; }
    /// <summary>Index des Inhaltsatoms in der Übung der Position.</summary>
    public int? ItemIndex { get; set; }
    /// <summary>Bei Lückentext: Index der Lücke; sonst null.</summary>
    public int? GapIndex { get; set; }
    public int StageValue { get; set; }
    public string? GivenAnswer { get; set; }
    public bool WasCorrect { get; set; }
    public int HintsUsed { get; set; }
}
