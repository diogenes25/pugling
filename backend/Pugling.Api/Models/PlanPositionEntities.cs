namespace Pugling.Api.Models;

// Neues Lehrplan-Modell (Strangler): Ein Lehrplan wird zur verfahrens-GEMISCHTEN Zusammenstellung
// von Positionen. Jede Position verweist auf eine Katalog-Übung (Exercise) und trägt ihre EIGENEN
// Ziele (Rhythmus Tag/Woche) und Punkte. Der Inhalt lebt allein in der Übungs-Config; hier wird nur
// der Lern-FORTSCHRITT pro Inhalts-Atom materialisiert (PositionItemProgress).
//
// Läuft in Etappe 1 ADDITIV neben dem alten StudyPlanItem/Method-Modell. Der Übungs-/Test-/Ziel-Motor
// wird erst in späteren Etappen umgeschlüsselt; danach entfällt das Alt-Modell.

/// <summary>Ziel-Rhythmus einer Lehrplan-Position: in welchem Takt sie erfüllt werden muss.</summary>
public enum GoalCadence
{
    /// <summary>Kein verpflichtendes Ziel – freies Üben, zählt nicht zum Tages-/Wochenziel.</summary>
    None = 0,
    /// <summary>Muss an jedem Übungstag erfüllt werden (Tagesziel).</summary>
    Daily = 1,
    /// <summary>Muss einmal pro Woche erfüllt werden (Wochenziel).</summary>
    Weekly = 2,
}

/// <summary>Auswahl-Umfang der Inhalte einer Position aus dem Übungs-Pool.</summary>
public enum ItemScope
{
    /// <summary>Alle Inhalte der Übung.</summary>
    All = 0,
    /// <summary>Nur noch nicht eingeführte (neue) Inhalte.</summary>
    New = 1,
    /// <summary>Nur bereits eingeführte (alte) Inhalte – Wiederholung.</summary>
    Old = 2,
}

/// <summary>
/// Eine Position in einem <see cref="StudyPlan"/>: verweist auf eine Katalog-<see cref="Exercise"/>
/// und legt fest, WIE sie im Plan gespielt wird (Overrides), WELCHES Ziel gilt (Rhythmus + Schwelle)
/// und WIE Punkte fließen. Leere Override-Felder erben den Vorschlag der Übung (Hybrid-Prinzip).
/// </summary>
public class PlanPosition
{
    public int Id { get; set; }
    public int StudyPlanId { get; set; }
    public StudyPlan? StudyPlan { get; set; }

    /// <summary>Referenzierte Katalog-Übung – der Inhalt bleibt dort (keine Kopie in Stores).</summary>
    public int ExerciseId { get; set; }
    public Exercise? Exercise { get; set; }

    /// <summary>Reihenfolge innerhalb des Plans (Gruppierung nach Fach ergibt sich aus der Übung).</summary>
    public int Order { get; set; }

    // --- Overrides (null = Vorschlag der Übung erben) ---
    /// <summary>Übersteuerte Teststufe (verfahrensabhängig interpretiert); null = Übungs-Default.</summary>
    public int? Stage { get; set; }
    /// <summary>Wie viele Inhalte der Übung genutzt werden; null = alle.</summary>
    public int? ItemCount { get; set; }
    /// <summary>Umfang der Inhaltsauswahl (alle/neu/alt).</summary>
    public ItemScope Scope { get; set; } = ItemScope.All;

    // --- Ziel ---
    /// <summary>Ziel-Rhythmus; <see cref="GoalCadence.None"/> = freies Üben ohne Pflicht.</summary>
    public GoalCadence Cadence { get; set; } = GoalCadence.None;
    /// <summary>
    /// Typ-abhängige Ziel-Schwelle: bei Katalog-Check die Anzahl korrekt zu lösender Aufgaben,
    /// bei einem Leitner-Test die Prozent-Bestehensgrenze; bei reinen Inhaltsübungen ungenutzt.
    /// Null = Standard des jeweiligen Verfahrens.
    /// </summary>
    public int? GoalThreshold { get; set; }
    /// <summary>
    /// Zählt ein Test nur auf einer „gewerteten" (getippten/Freitext-)Stufe als bestanden?
    /// Verhindert bloßes Klicken/Auswählen. Nur für test-fähige Verfahren relevant.
    /// </summary>
    public bool RequireTypedTest { get; set; }

    // --- Punkte (Default aus dem Bonus-Vorschlag der Übung, hier pro Position überschreibbar) ---
    /// <summary>Punkte für das Erreichen des Positionsziels in seiner Periode.</summary>
    public int PointsGoalMet { get; set; } = 20;
    /// <summary>Basispunkte für einen erstmals wiederholten (neuen) Inhalt – „neuer Stoff zählt am meisten".</summary>
    public int NewContentPoints { get; set; } = 10;
    /// <summary>Alle N richtigen Antworten in Folge gibt es einen Combo-Bonus. 0 = aus.</summary>
    public int ComboThreshold { get; set; } = 5;
    /// <summary>Basis-Bonuspunkte je Combo-Meilenstein; eskaliert (N-ter Meilenstein → Basis × N). 0 = aus.</summary>
    public int ComboBonusPoints { get; set; } = 5;
    /// <summary>Höchst-Sekunden für eine „schnelle Antwort"; 0 = Feature aus.</summary>
    public int SpeedThresholdSeconds { get; set; }
    /// <summary>Bonuspunkte für eine schnelle Antwort. 0 = aus.</summary>
    public int SpeedBonusPoints { get; set; }

    // --- Leitner-Wiederholung (nur für drill-fähige Verfahren wie Vokabeln/Cloze/Matching) ---
    /// <summary>Aktiviert die Karteikasten-Terminierung dieser Position.</summary>
    public bool UseLeitner { get; set; }
    /// <summary>Höchste Box (Standard 5).</summary>
    public int MaxBox { get; set; } = 5;
    /// <summary>Intervall in Tagen je Box (Index = Box; Index 0 ungenutzt). Null = Standard <c>[0,1,2,4,7,14]</c>.</summary>
    public List<int>? BoxIntervalDays { get; set; }
    /// <summary>Optionaler Stufen-Fahrplan (Tag → Stufe); steigert die Schwierigkeit über die Laufzeit.</summary>
    public List<StageStep>? StageSchedule { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Materialisierter Leitner-/Einführungs-Fortschritt je Inhalts-Atom dieser Position.</summary>
    public List<PositionItemProgress> ItemProgress { get; set; } = new();
}

/// <summary>
/// Lern-Fortschritt eines einzelnen Inhalts-Atoms (z. B. einer Vokabel) innerhalb einer
/// <see cref="PlanPosition"/>. Faul angelegt beim ersten Einführen – der Inhalt selbst bleibt in der
/// Übungs-Config, hier steht nur der Karteikasten-/Einführungs-Zustand pro Kind (ein Plan = ein Kind).
/// </summary>
public class PositionItemProgress
{
    public int Id { get; set; }
    public int PlanPositionId { get; set; }
    public PlanPosition? PlanPosition { get; set; }

    /// <summary>Index des Inhalts in der Item-Liste der referenzierten Übung.</summary>
    public int ItemIndex { get; set; }

    /// <summary>Aktuelle Leitner-Box (1 = neu/schwer … MaxBox = sicher).</summary>
    public int Box { get; set; } = 1;
    /// <summary>Tag, an dem der Inhalt das nächste Mal fällig ist. Null = sofort fällig (noch nie bewertet).</summary>
    public DateOnly? DueOn { get; set; }
    /// <summary>Wie oft dieser Inhalt schon per Leitner wiederholt wurde.</summary>
    public int ReviewCount { get; set; }
    /// <summary>Zeitpunkt der letzten Leitner-Wiederholung.</summary>
    public DateTime? LastReviewedAt { get; set; }
    /// <summary>Wann der Inhalt erstmals als „neu" eingeführt wurde. Null = noch nicht eingeführt.</summary>
    public DateOnly? IntroducedAt { get; set; }
}
