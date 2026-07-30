namespace Pugling.Api.Models;

// Lehrplan-Modell: Ein Lehrplan ist ein reiner Container aus referenzierten Katalog-Übungen
// (siehe PlanPosition). Zeit-/Punkte-/Leitner-Steuerung, Stufen und Ziele hängen an der jeweiligen
// Position, nicht mehr am Plan. Verfahrens-spezifisch sind nur der Inhalt (Übungs-Config) und die
// Test-Mechanik/Stufen (siehe PositionPlayService / PositionTestsController).

// LearningMethod lebt im Vertrags-Projekt (Pugling.Contracts).

/// <summary>
/// Stufe des Zuordnungs-Verfahrens (steigende Schwierigkeit). Nutzt den Vokabel-Store.
/// <para>
/// <b>Achtung, halb umgesetzt:</b> <c>MatchingExerciseType</c> überschreibt weder <c>StageOptions</c> noch
/// <c>IsTypedStage</c> noch <c>Choices</c> – es gibt also keinen Code, der auf diesen Enum verzweigt.
/// <see cref="PlanPosition.Stage"/> wird für Zuordnungs-Positionen gespeichert und beim Ausspielen
/// ignoriert. Die beiden Rückwärts-Stufen (<c>Reverse</c>, <c>ReverseDistractors</c>) sind entfallen, weil
/// sie nirgends vorkamen; die verbleibenden zwei bleiben, weil <c>Direct</c> als <c>DefaultStage</c> und
/// <c>Distractors</c> im Seed gesetzt werden. Den Enum wirklich wirksam zu machen ist ein
/// Verhaltensumbau, kein Struktur-Schritt.
/// </para>
/// </summary>
public enum MatchStage
{
    /// <summary>Wort → Übersetzung, keine Ablenker.</summary>
    Direct = 1,
    /// <summary>Wort → Übersetzung, mit Zusatz-Ablenkern im Auswahl-Pool.</summary>
    Distractors = 2,
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

// StageStep lebt im Vertrags-Projekt (Pugling.Contracts).

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

// PlayMode lebt im Vertrags-Projekt (Pugling.Contracts).

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

/// <summary>
/// Einzelne Wiederholung innerhalb einer Übungssitzung (verfahrensneutral). Bewusst schmal: gelesen
/// werden nur <see cref="WasCorrect"/> und <see cref="At"/> – daraus entstehen die Combo-Serie und die
/// Antwortzeit (siehe <c>PositionPracticeController.Review</c>) sowie die Metrik <c>CorrectReviews</c>.
/// <para>
/// Was das Atom war, steht <b>nicht</b> hier: dafür gibt es <see cref="ItemReviewEvent"/> mit der stabilen
/// <c>ItemId</c>. Die früheren Felder <c>ContentId</c> (eine FK-lose Kopie von
/// <see cref="PlanPosition.ExerciseId"/>), <c>ItemIndex</c> und <c>StageValue</c> wurden geschrieben und
/// von niemandem gelesen – eine zweite, index-adressierte Wahrheit ohne Konsumenten.
/// </para>
/// </summary>
public class ReviewEvent
{
    public int Id { get; set; }
    public int PracticeSessionId { get; set; }
    public PracticeSession? PracticeSession { get; set; }
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
    /// <summary>Index des Inhaltsatoms in der Übung der Position.</summary>
    public int? ItemIndex { get; set; }
    public int StageValue { get; set; }
    public string? GivenAnswer { get; set; }
    public bool WasCorrect { get; set; }
    /// <summary>
    /// Genutzte Buchstaben-Tipps. <b>Wird von keinem Pfad gesetzt</b> und ist daher immer 0 – die Spalte
    /// bleibt nur, weil sie über <c>ItemResultDto</c> im Vertrag steht; sie zu entfernen wäre ein
    /// Vertragsbruch und gehört damit nicht in einen reinen Struktur-Umbau. Entweder befüllen (die Tipps
    /// existieren in der Ausspielung) oder mit dem DTO gemeinsam streichen.
    /// </summary>
    public int HintsUsed { get; set; }
}
