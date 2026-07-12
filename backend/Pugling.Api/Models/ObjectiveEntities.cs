namespace Pugling.Api.Models;

/// <summary>
/// Art eines <see cref="Objective"/> – bestimmt Ton und Währung der Belohnung. <see cref="Committed"/> ist ein
/// verbindliches Ziel (Belohnung in 🪙 Münzen = reale Privilegien); <see cref="Stretch"/> ist ein
/// „Dehnungsziel" (Belohnung in 💎 Gems = kosmetisch). Bewusst gibt es <b>keinen Malus</b> auf Objectives –
/// der „Stick" wohnt allein am Pflichtziel der <see cref="PlanPosition"/> (siehe <c>PenaltyCoins</c>).
/// </summary>
public enum ObjectiveKind
{
    /// <summary>Verbindliches Ziel; Belohnung in Münzen (Currency.Coins).</summary>
    Committed = 0,
    /// <summary>Dehnungsziel; Belohnung in Gems (Currency.Gems).</summary>
    Stretch = 1,
}

/// <summary>
/// Kennzahl, an der ein <see cref="KeyResult"/> gemessen wird. Bewusst nur <b>tricksichere</b> Größen:
/// die outcome-/Leitner-basierten (<see cref="AvgMastery"/>/<see cref="MasteredPercent"/>/<see cref="MaxWeakItems"/>,
/// aus <c>ChildLearnProgressService.MasteryRollup</c>) und die vom Vater getippte Klassenarbeits-Note
/// (<see cref="ClassTestGrade"/>). Reine Aktivitäts-Zähler (Minuten/Wiederholungen) fehlen absichtlich –
/// sie belohnen Wiederholen statt Können und wären farmbar. Die abdeckungsbasierte „Coverage" (Wert 1 beim
/// verwandten <c>LearnGoalMetric</c>) fehlt hier bewusst: sie steigt schon durchs bloße Sehen von Vokabeln.
/// </summary>
public enum KeyResultMetric
{
    /// <summary>Ø-Beherrschung in Prozent über die eingeführten Items (Ziel: ≥ Zielwert).</summary>
    AvgMastery = 0,
    /// <summary>Anteil beherrschter Items in Prozent (Box ≥ MaxBox / vorhandene Items) (Ziel: ≥ Zielwert).</summary>
    MasteredPercent = 2,
    /// <summary>Höchstzahl schwacher Items (Beherrschung &lt; 50 %) – „nicht mehr als N" (Ziel: ≤ Zielwert).</summary>
    MaxWeakItems = 3,
    /// <summary>Beste Klassenarbeits-Note im Fach als Note×10 (z. B. 20 = „mindestens 2,0"; Ziel: ≤ Zielwert).</summary>
    ClassTestGrade = 4,
}

/// <summary>
/// Ein vom Vater gesetztes <b>großes Ziel</b> für ein Kind (der OKR-Kern, kindgerecht): eine terminierte,
/// motivierende Klammer über mehreren messbaren <see cref="KeyResult"/>s (den „Etappen"). Genau wie ein
/// <see cref="StudyPlan"/> ein Container über <see cref="PlanPosition"/>s ist, ist ein Objective ein Container
/// über KeyResults. Der Fortschritt wird <b>live</b> aus dem aggregierten Lernstand berechnet (kein
/// materialisierter Zustand); belohnt wird idempotent per Lazy Settlement (siehe <c>ObjectiveRewardService</c>):
/// je erreichter Etappe ein Häppchen (<see cref="RewardPerKeyResult"/>) und beim Voll-Abschluss der große Batzen
/// (<see cref="RewardOnComplete"/>). Kein Grading, kein Malus.
/// </summary>
public class Objective
{
    public int Id { get; set; }

    public int ChildId { get; set; }
    public Child? Child { get; set; }

    /// <summary>Konkreter, kindgerechter Titel (z. B. „Englisch Unit 3 sicher können").</summary>
    public string Title { get; set; } = "";
    /// <summary>Das „Warum" in einem Satz – wird dem Sohn zur Motivation gezeigt.</summary>
    public string? Motivation { get; set; }
    /// <summary>Verbindlich (Münzen) oder Dehnungsziel (Gems).</summary>
    public ObjectiveKind Kind { get; set; }

    /// <summary>Optionaler Start; Klassenarbeits-Noten zählen nur ab diesem Tag (null = ohne Untergrenze).</summary>
    public DateOnly? Start { get; set; }
    /// <summary>Optionaler Stichtag; danach gilt ein unerreichtes Ziel als „überfällig".</summary>
    public DateOnly? DueDate { get; set; }
    /// <summary>Ob das Ziel aktiv verfolgt (und belohnt) wird. Inaktive Ziele werden nicht mehr abgerechnet.</summary>
    public bool Active { get; set; } = true;

    /// <summary>Belohnung beim Erreichen ALLER Key Results (Münzen bzw. Gems je <see cref="Kind"/>). 0 = keine.</summary>
    public int RewardOnComplete { get; set; }
    /// <summary>Belohnung je einzeln erreichter Etappe (kurzer Feedback-Loop). 0 = keine.</summary>
    public int RewardPerKeyResult { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<KeyResult> KeyResults { get; set; } = [];
}

/// <summary>
/// Eine messbare <b>Etappe</b> eines <see cref="Objective"/> auf einem Katalog-Scope (Fach, optional Kapitel/Übung).
/// Die Beherrschungs-Metriken werden – wie beim <see cref="LearnGoal"/> – live über den <c>ScopeEvaluator</c>
/// des Lernstands ausgewertet; <see cref="KeyResultMetric.ClassTestGrade"/> liest die vom Vater nachgetragene
/// <see cref="Klassenarbeit.Grade"/> des Fachs (Scope dann nur Fach).
/// </summary>
public class KeyResult
{
    public int Id { get; set; }

    public int ObjectiveId { get; set; }
    public Objective? Objective { get; set; }

    // --- Katalog-Scope (Hierarchie: Exercise ⊂ Chapter ⊂ Subject) ---
    /// <summary>Fach der Etappe (Pflicht).</summary>
    public int SubjectId { get; set; }
    /// <summary>Optional: Kapitel; <c>null</c> = ganzes Fach. Nur bei Beherrschungs-Metriken zulässig.</summary>
    public int? ChapterId { get; set; }
    /// <summary>Optional: konkrete Vokabelübung; setzt <see cref="ChapterId"/> voraus. Nur bei Beherrschungs-Metriken.</summary>
    public int? ExerciseId { get; set; }

    // --- Ziel ---
    /// <summary>Gemessene Kennzahl.</summary>
    public KeyResultMetric Metric { get; set; }
    /// <summary>Zielwert: Prozent (0..100) bzw. Anzahl (MaxWeakItems) bzw. Note×10 (ClassTestGrade, 10..60).</summary>
    public int TargetValue { get; set; }
    /// <summary>Optionaler frei wählbarer Titel (sonst aus Scope/Metric ableitbar).</summary>
    public string? Title { get; set; }
}

/// <summary>
/// Protokolliert eine <b>einmalige</b> Belohnungs-Buchung eines <see cref="Objective"/> – das Objective-Gegenstück
/// zu <see cref="PositionGoalReward"/>. Ein Unique-Index auf <c>(ObjectiveId, PeriodKey)</c> garantiert, dass
/// jede Etappe (<see cref="PeriodKey"/> = <c>kr:{keyResultId}</c>) und der Voll-Abschluss (<see cref="PeriodKey"/> =
/// <c>done</c>) je Objective höchstens einmal ausgezahlt werden – auch wenn das Lazy Settlement mehrfach läuft.
/// Anders als bei den periodischen Positions-Zielen ist die Belohnung hier <b>einmalig</b> (kein Periodenschlüssel):
/// ein späterer Rückfall des Lernstands nimmt eine bereits verdiente Etappe nicht zurück (kein Malus auf Objectives).
/// </summary>
public class ObjectiveReward
{
    public int Id { get; set; }
    public int ObjectiveId { get; set; }
    public Objective? Objective { get; set; }
    /// <summary>Anlass der Buchung: <c>kr:{keyResultId}</c> je Etappe bzw. <c>done</c> für den Voll-Abschluss.</summary>
    public string PeriodKey { get; set; } = "";
    /// <summary>Gutgeschriebene Menge (positiver Betrag; Münzen bzw. Gems je <see cref="ObjectiveKind"/>).</summary>
    public int Points { get; set; }
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
}
