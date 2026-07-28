namespace Pugling.Contracts.Supervisor;

// Vertrag des Lehrplans: der Plan ist ein reiner Container, alles Lern-Spezifische (Ziel, Punkte,
// Stufe, Leitner) trägt die einzelne Position.

/// <summary>Ein Lehrplan-Container eines Kindes.</summary>
public record PlanResponse(int Id, int ChildId, string Title, int? SubjectId,
    DateOnly StartDate, DateOnly EndDate, bool Active, int PositionCount, string? Description)
{
    /// <summary>
    /// Server-autoritative Affordance: Ob dies der eine, aktuell spielbare Plan des Kindes ist
    /// (aktiv <b>und</b> heute in Laufzeit). Für den Sohn ist stets nur dieser sichtbar; dem Vater
    /// zeigt es unter mehreren Plänen den, den der Sohn gerade spielen kann – ohne die Regel im Client nachzubilden.
    /// </summary>
    public bool IsPlayable { get; init; }
}

/// <summary>Eingabe zum Anlegen eines leeren Lehrplan-Containers.</summary>
public record CreatePlanDto(int ChildId, string Title, int? SubjectId, DateOnly? StartDate, int DurationDays,
    string? Description = null);

/// <summary>Partielle Änderung des Containers. <see cref="ChildId"/> weist den Plan einem anderen eigenen Kind zu.</summary>
public record UpdatePlanDto(string? Title, int? SubjectId, DateOnly? StartDate, DateOnly? EndDate, bool? Active,
    string? Description = null, int? ChildId = null);

/// <summary>Eine Position im Lehrplan: die referenzierte Übung samt eigenem Ziel, Punkten, Stufe und Leitner.</summary>
/// <param name="Id">Id der Position.</param>
/// <param name="StudyPlanId">Lehrplan, zu dem die Position gehört.</param>
/// <param name="ExerciseId">
/// Die referenzierte Katalog-Übung. <b>Unveränderlich</b>: der Leitner-Fortschritt ist über Item-Indizes an
/// sie verankert; ein Austausch würde ihn auf fremde Inhalte umbiegen.
/// </param>
/// <param name="ExerciseTitle">Titel der Übung – mitgeliefert, damit eine Liste ohne zweiten Abruf lesbar ist.</param>
/// <param name="ExerciseType">Typ-Schlüssel der Übung (Wert aus dem Typ-Manifest).</param>
/// <param name="Order">Reihenfolge innerhalb des Plans.</param>
/// <param name="Stage">Abfrageform des Abschlusstests; <c>null</c> = Standard der Übung bzw. des Verfahrens.</param>
/// <param name="ItemCount">Wie viele Inhalte je Durchlauf; <c>null</c> = alle.</param>
/// <param name="Scope">Welcher Teil der Inhalte gespielt wird (siehe <see cref="ItemScope"/>).</param>
/// <param name="Cadence">
/// Ziel-Rhythmus (siehe <see cref="GoalCadence"/>). <c>None</c> = freies Üben, das nicht zur Pflicht zählt –
/// und damit auch keinen Malus auslösen kann.
/// </param>
/// <param name="OrderStrategy">
/// Ausspiel-Reihenfolge (siehe <see cref="PracticeOrder"/>); sie wird bei Sitzungs-/Testbeginn eingefroren.
/// </param>
/// <param name="GoalThreshold">
/// Bestehensgrenze des Abschlusstests in <b>Prozent</b> richtiger Antworten; <c>null</c> = 80 %. Die
/// Einheit gilt für <i>alle</i> prüfbaren Übungstypen – auch für Katalog-Checks, denn das Ziel einer
/// Position wird immer an einem bestandenen Positions-Test gemessen. Bei reinen Inhaltsübungen ungenutzt.
/// </param>
/// <param name="RequireTypedTest">Nur getippte, objektiv prüfbare Stufen im Abschlusstest – keine Selbsteinschätzung.</param>
/// <param name="UseLeitner">Leitner-Kasten mit Wiedervorlage statt eines einfachen Durchlaufs.</param>
/// <param name="MaxBox">Höchste Leitner-Box; ein Inhalt darin gilt als beherrscht (Standard 5).</param>
/// <param name="BoxIntervalDays">Wiedervorlage-Abstand in Tagen je Box; <c>null</c> = Verfahrens-Standard.</param>
/// <param name="StageSchedule">Fahrplan, welche Abfrageform in welcher Box gilt; <c>null</c> = Verfahrens-Standard.</param>
/// <param name="PointsGoalMet">Punkte, wenn das Ziel der Periode erfüllt ist.</param>
/// <param name="PenaltyCoins">
/// Münz-<b>Malus</b> bei gerissener Pflicht (der „Stick"); <c>0</c> = reine Belohnung. Wird nur für
/// abgeschlossene Perioden gebucht und darf den Saldo ins Minus ziehen.
/// </param>
/// <param name="NewContentPoints">Punkte für einen erstmals eingeführten Inhalt.</param>
/// <param name="ComboThreshold">Ab wie vielen richtigen Antworten in Folge der Combo-Bonus greift.</param>
/// <param name="ComboBonusPoints">Punkte je erreichter Combo.</param>
/// <param name="SpeedThresholdSeconds">Bis zu wie vielen Sekunden eine Antwort als „schnell" gilt; <c>0</c> = aus.</param>
/// <param name="SpeedBonusPoints">Punkte je schneller Antwort.</param>
public record PositionResponse(int Id, int StudyPlanId, int ExerciseId, string ExerciseTitle,
    string ExerciseType, int Order, int? Stage, int? ItemCount, ItemScope Scope, GoalCadence Cadence,
    PracticeOrder OrderStrategy, int? GoalThreshold, bool RequireTypedTest, bool UseLeitner, int MaxBox,
    List<int>? BoxIntervalDays, List<StageStep>? StageSchedule, int PointsGoalMet, int PenaltyCoins,
    int NewContentPoints, int ComboThreshold, int ComboBonusPoints, int SpeedThresholdSeconds, int SpeedBonusPoints);

/// <summary>
/// Anlegen einer Position. Leere Override-Felder erben den Vorschlag der Übung (Hybrid-Prinzip):
/// Stufe/Item-Anzahl bleiben dann <c>null</c> und werden erst beim Spielen aus den Übungs-Defaults
/// aufgelöst; die Punkte-/Bonus-Felder werden aus dem Bonus-Vorschlag der Übung vorbelegt.
/// </summary>
/// <param name="ExerciseId">Die zuzuweisende Katalog-Übung; muss ausführbar sein (Execute-Recht bzw. öffentlich).</param>
/// <param name="Order">Position im Plan; <c>null</c> = ans Ende.</param>
/// <param name="Stage">Abfrageform; <c>null</c> = erbt den Übungs-Standard (erst beim Spielen aufgelöst).</param>
/// <param name="ItemCount">Inhalte je Durchlauf; <c>null</c> = erbt den Übungs-Standard.</param>
/// <param name="Scope">Welcher Teil der Inhalte (siehe <see cref="ItemScope"/>); <c>null</c> = alle.</param>
/// <param name="Cadence">Ziel-Rhythmus (siehe <see cref="GoalCadence"/>); <c>null</c> = kein verpflichtendes Ziel.</param>
/// <param name="OrderStrategy">Ausspiel-Reihenfolge (siehe <see cref="PracticeOrder"/>); <c>null</c> = schwächste zuerst.</param>
/// <param name="GoalThreshold">
/// Bestehensgrenze in <b>Prozent</b> (siehe <see cref="PositionResponse"/>); <c>null</c> = 80 %.
/// </param>
/// <param name="RequireTypedTest">Wie bei <see cref="PositionResponse"/>; <c>null</c> = erbt die Vorgabe der Übung.</param>
/// <param name="UseLeitner">Wie bei <see cref="PositionResponse"/>; <c>null</c> = erbt die Vorgabe der Übung.</param>
/// <param name="MaxBox">Höchste Leitner-Box; <c>null</c> oder <c>&lt;= 0</c> = 5.</param>
/// <param name="BoxIntervalDays">Wiedervorlage-Abstände je Box; <c>null</c> = Verfahrens-Standard.</param>
/// <param name="StageSchedule">Stufen-Fahrplan je Box; <c>null</c> = Verfahrens-Standard.</param>
/// <param name="PointsGoalMet">Punkte bei erfülltem Ziel; <c>null</c> = 20.</param>
/// <param name="PenaltyCoins">Münz-Malus bei gerissener Pflicht; <c>null</c> = 0 (reine Belohnung, opt-in).</param>
/// <param name="NewContentPoints">Punkte für neuen Inhalt; <c>null</c> = Bonus-Vorschlag der Übung, sonst 10.</param>
/// <param name="ComboThreshold">Combo-Schwelle; <c>null</c> = Bonus-Vorschlag der Übung, sonst 5.</param>
/// <param name="ComboBonusPoints">Combo-Bonus; <c>null</c> = Bonus-Vorschlag der Übung, sonst 5.</param>
/// <param name="SpeedThresholdSeconds">Zeitgrenze für „schnell"; <c>null</c> = Bonus-Vorschlag der Übung, sonst 0 (aus).</param>
/// <param name="SpeedBonusPoints">Bonus je schneller Antwort; <c>null</c> = Bonus-Vorschlag der Übung, sonst 0.</param>
public record CreatePositionDto(int ExerciseId, int? Order, int? Stage, int? ItemCount, ItemScope? Scope,
    GoalCadence? Cadence, PracticeOrder? OrderStrategy, int? GoalThreshold, bool? RequireTypedTest,
    bool? UseLeitner, int? MaxBox, List<int>? BoxIntervalDays, List<StageStep>? StageSchedule,
    int? PointsGoalMet, int? PenaltyCoins, int? NewContentPoints, int? ComboThreshold, int? ComboBonusPoints,
    int? SpeedThresholdSeconds, int? SpeedBonusPoints);

/// <summary>
/// Partielle Änderung der Overrides/Ziele/Punkte. Die referenzierte Übung ist unveränderlich
/// (Fortschritts-Indizes).
/// <para>
/// <b>PATCH-Semantik:</b> <c>null</c> heißt bei <i>jedem</i> Feld „nicht angegeben" – der bisherige Wert
/// bleibt. Es heißt <b>nicht</b> „auf Standard zurücksetzen"; dafür bräuchte es einen eigenen
/// <c>Clear</c>-Schalter, den dieses DTO bewusst nicht hat.
/// </para>
/// </summary>
/// <param name="Order">Neue Position im Plan.</param>
/// <param name="Stage">Abfrageform des Abschlusstests.</param>
/// <param name="ItemCount">Inhalte je Durchlauf.</param>
/// <param name="Scope">Welcher Teil der Inhalte (siehe <see cref="ItemScope"/>).</param>
/// <param name="Cadence">Ziel-Rhythmus (siehe <see cref="GoalCadence"/>).</param>
/// <param name="OrderStrategy">Ausspiel-Reihenfolge (siehe <see cref="PracticeOrder"/>).</param>
/// <param name="GoalThreshold">
/// Bestehensgrenze in <b>Prozent</b> (siehe <see cref="PositionResponse"/>); <c>null</c> = nicht angegeben.
/// </param>
/// <param name="RequireTypedTest">Nur getippte Stufen im Abschlusstest.</param>
/// <param name="UseLeitner">Leitner-Kasten statt einfachem Durchlauf.</param>
/// <param name="MaxBox">Höchste Leitner-Box.</param>
/// <param name="BoxIntervalDays">Wiedervorlage-Abstände je Box.</param>
/// <param name="StageSchedule">Stufen-Fahrplan je Box.</param>
/// <param name="PointsGoalMet">Punkte bei erfülltem Ziel.</param>
/// <param name="PenaltyCoins">Münz-Malus bei gerissener Pflicht.</param>
/// <param name="NewContentPoints">Punkte für neuen Inhalt.</param>
/// <param name="ComboThreshold">Combo-Schwelle.</param>
/// <param name="ComboBonusPoints">Combo-Bonus.</param>
/// <param name="SpeedThresholdSeconds">Zeitgrenze für „schnell"; <c>0</c> = aus.</param>
/// <param name="SpeedBonusPoints">Bonus je schneller Antwort.</param>
public record UpdatePositionDto(int? Order, int? Stage, int? ItemCount, ItemScope? Scope,
    GoalCadence? Cadence, PracticeOrder? OrderStrategy, int? GoalThreshold, bool? RequireTypedTest,
    bool? UseLeitner, int? MaxBox, List<int>? BoxIntervalDays, List<StageStep>? StageSchedule,
    int? PointsGoalMet, int? PenaltyCoins, int? NewContentPoints, int? ComboThreshold, int? ComboBonusPoints,
    int? SpeedThresholdSeconds, int? SpeedBonusPoints);
