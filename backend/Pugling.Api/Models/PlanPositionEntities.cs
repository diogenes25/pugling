namespace Pugling.Api.Models;

// Lehrplan-Modell: Ein Lehrplan ist eine verfahrens-GEMISCHTE Zusammenstellung von Positionen. Jede
// Position verweist auf eine Katalog-Übung (Exercise) und trägt ihre EIGENEN Ziele (Rhythmus Tag/Woche)
// und Punkte. Der Inhalt lebt allein in der Übungs-Config; hier wird nur der Lern-FORTSCHRITT pro
// Inhalts-Atom materialisiert (PositionItemProgress).
//
// Der Strangler ist abgeschlossen: das frühere plan-weite StudyPlanItem/Method-Modell wurde mit der
// Migration `PlanContainerCleanup` (2026-07-05) vollständig entfernt – es gibt kein Alt-Modell mehr,
// neben dem hier noch etwas „additiv" laufen würde.

// GoalCadence/ItemScope/PracticeOrder leben im Vertrags-Projekt (Pugling.Contracts).

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
    /// <summary>
    /// Reihenfolge, in der der Server die (fälligen) Inhalte ausspielt (beim Sitzungs-/Testbeginn eingefroren).
    /// Standard <see cref="PracticeOrder.WeakestFirst"/> = bisheriges Verhalten.
    /// </summary>
    public PracticeOrder OrderStrategy { get; set; } = PracticeOrder.WeakestFirst;

    // --- Ziel ---
    /// <summary>Ziel-Rhythmus; <see cref="GoalCadence.None"/> = freies Üben ohne Pflicht.</summary>
    public GoalCadence Cadence { get; set; } = GoalCadence.None;
    /// <summary>
    /// Bestehensgrenze des Abschlusstests in <b>Prozent</b> richtiger Antworten; <c>null</c> = 80 %.
    /// <para>
    /// Die Einheit ist <b>typ-unabhängig</b> – auch bei Katalog-Check-Verfahren. Das ist keine
    /// Vereinfachung, sondern folgt daraus, dass ein <see cref="TestAttempt"/> ausschließlich im
    /// Positions-Test entsteht und <c>PositionProgressService.IsGoalMetAsync</c> das Ziel jedes
    /// prüfbaren Typs an einem bestandenen Versuch misst: es gibt gar keinen zweiten Pfad, der eine
    /// andere Einheit auswerten könnte. Eine absolute Trefferzahl wäre hier auch überflüssig – wie
    /// groß der Pool ist, sagt bereits <see cref="ItemCount"/>.
    /// </para>
    /// <para>
    /// Bei reinen Inhaltsübungen (<c>ExerciseCheckMode.None</c>) bleibt der Wert ungenutzt: dort gilt
    /// das Ziel schon mit einer Lern-Sitzung als erledigt.
    /// </para>
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
    /// <summary>
    /// Münz-<b>Malus</b>, der abgezogen wird, wenn das Pflichtziel (<see cref="Cadence"/> Tag/Woche) in
    /// einer abgeschlossenen Periode <b>gerissen</b> wurde – der „Stick" gegen Nicht-Lernen. 0 = kein Malus
    /// (reine Belohnung). Nur bei <see cref="GoalCadence.Daily"/>/<see cref="GoalCadence.Weekly"/> wirksam.
    /// Schulden sind erlaubt: der Münz-Saldo darf dadurch negativ werden.
    /// </summary>
    public int PenaltyCoins { get; set; }
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
/// Protokolliert die <b>einmalige</b> Punkte-Gutschrift für ein erreichtes Positions-Ziel je Periode –
/// das Positions-Gegenstück zur idempotenten Tages-Belohnung. Verhindert, dass die Ziel-Punkte
/// (<see cref="PlanPosition.PointsGoalMet"/>) doppelt fließen, wenn dieselbe Position in derselben
/// Periode mehrfach abgeschlossen/aufgerufen wird.
/// <para>
/// Die Periode ist <b>(<see cref="Cadence"/>, <see cref="PeriodStart"/>)</b>, und die Taktung gehört
/// ausdrücklich dazu: sie ist eine <b>Momentaufnahme</b> der Position zum Zeitpunkt der Buchung. Ohne sie
/// deutete ein Wechsel Tag→Woche rückwirkend gebuchte Perioden um – die Belohnung für Montag als Tagesziel
/// würde die Woche, die an diesem Montag beginnt, stillschweigend als „schon bezahlt" abweisen.
/// </para>
/// <para>
/// <see cref="PeriodStart"/> ist <b>nicht</b> dasselbe wie <see cref="Day"/>: bei einem Wochenziel, das am
/// Mittwoch erreicht wird, steht der Montag im einen und der Mittwoch im anderen Feld. Beide werden
/// gebraucht – der Tag für die Tages-/Serien-Metriken, die Periode für die Idempotenz.
/// </para>
/// </summary>
public class PositionGoalReward
{
    public int Id { get; set; }
    public int PlanPositionId { get; set; }
    public PlanPosition? PlanPosition { get; set; }
    /// <summary>Taktung der Position zum Zeitpunkt der Buchung (Momentaufnahme – siehe Klassen-Doku).</summary>
    public GoalCadence Cadence { get; set; }
    /// <summary>Erster Tag der belohnten Periode: der Tag selbst beim Tagesziel, der Montag beim Wochenziel.</summary>
    public DateOnly PeriodStart { get; set; }
    /// <summary>Kalendertag, an dem das Ziel erreicht wurde (Grundlage der Tages-/Serien-Metriken).</summary>
    public DateOnly Day { get; set; }
    public int Points { get; set; }
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Protokolliert den <b>einmaligen</b> Münz-Malus für ein <b>gerissenes</b> Positions-Pflichtziel je Periode –
/// das negative Gegenstück zu <see cref="PositionGoalReward"/>. Ein Unique-Index auf
/// <c>(PlanPositionId, Cadence, PeriodStart)</c> garantiert, dass der Malus
/// (<see cref="PlanPosition.PenaltyCoins"/>) je Periode höchstens einmal abgezogen wird – auch wenn das Lazy
/// Settlement mehrfach über dieselbe abgeschlossene Periode läuft. Die Periode ist identisch aufgebaut wie
/// beim Reward, samt der Momentaufnahme der Taktung (Begründung dort).
/// </summary>
public class PositionGoalPenalty
{
    public int Id { get; set; }
    public int PlanPositionId { get; set; }
    public PlanPosition? PlanPosition { get; set; }
    /// <summary>Taktung der Position zum Zeitpunkt der Buchung (Momentaufnahme, siehe <see cref="PositionGoalReward"/>).</summary>
    public GoalCadence Cadence { get; set; }
    /// <summary>Erster Tag der gerissenen Periode: der Tag selbst beim Tagesziel, der Montag beim Wochenziel.</summary>
    public DateOnly PeriodStart { get; set; }
    /// <summary>Letzter Tag der gerissenen Periode (Tag selbst bzw. Wochen-Sonntag) – für die Auswertung.</summary>
    public DateOnly Day { get; set; }
    /// <summary>Abgezogene Münzen (positiver Betrag; die Ledger-Buchung ist negativ).</summary>
    public int Points { get; set; }
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Lern-Fortschritt eines einzelnen Inhaltsatoms (z. B. einer Vokabel) innerhalb einer
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
