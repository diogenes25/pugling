namespace Pugling.Api.Models;

// ObjectiveKind/KeyResultMetric leben im Vertrags-Projekt (Pugling.Contracts).

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
/// zu <see cref="PositionGoalReward"/>. Zwei <b>gefilterte</b> Unique-Indizes garantieren, dass jede Etappe und
/// der Voll-Abschluss je Objective höchstens einmal ausgezahlt werden – auch wenn das Lazy Settlement mehrfach läuft.
/// Anders als bei den periodischen Positions-Zielen ist die Belohnung hier <b>einmalig</b> (keine Periode):
/// ein späterer Rückfall des Lernstands nimmt eine bereits verdiente Etappe nicht zurück (kein Malus auf Objectives).
/// <para>
/// Der Anlass steckt in <see cref="PaidKeyResultId"/>: gesetzt = diese Etappe, <c>null</c> = der Voll-Abschluss.
/// Vorher stand dort ein Text (<c>kr:42</c> bzw. <c>done</c>) in einer Spalte namens <c>PeriodKey</c> – ein
/// Name, der keine Periode benannte, und ein Format, das jeder Leser parsen musste.
/// </para>
/// </summary>
public class ObjectiveReward
{
    public int Id { get; set; }
    public int ObjectiveId { get; set; }
    public Objective? Objective { get; set; }
    /// <summary>
    /// Die bezahlte Etappe; <c>null</c> steht für den Voll-Abschluss des Objectives.
    /// <para>
    /// Bewusst <b>kein</b> Fremdschlüssel auf <see cref="KeyResult"/>, aus drei Gründen, die alle in
    /// dieselbe Richtung zeigen: <c>SetNull</c> würde eine Etappen-Buchung beim Löschen der Etappe lautlos in
    /// die <i>Abschluss</i>-Buchung verwandeln (ein Diskriminator darf nicht durch ein Löschen kippen);
    /// <c>Cascade</c> ergäbe einen zweiten Kaskadenpfad vom Objective her (Objective → KeyResult → Reward
    /// neben Objective → Reward), also genau den SQLite-Diamanten, den dieses Modell sonst vermeidet; und die
    /// Buchung soll die Etappe ohnehin <b>überleben</b> – bezahlt ist bezahlt. Damit ist die Spalte eine
    /// Audit-Momentaufnahme wie <c>ShopPurchase.SupervisorId</c>.
    /// </para>
    /// </summary>
    public int? PaidKeyResultId { get; set; }
    /// <summary>Gutgeschriebene Menge (positiver Betrag; Münzen bzw. Gems je <see cref="ObjectiveKind"/>).</summary>
    public int Points { get; set; }
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
}
