namespace Pugling.Api.Models;

/// <summary>
/// Kennzahl, an der ein <see cref="LearnGoal"/> gemessen wird – jede bildet direkt ein Feld des
/// aggregierten Lernstands (siehe <c>ChildLearnProgressService.MasteryRollup</c>) ab.
/// </summary>
public enum LearnGoalMetric
{
    /// <summary>Ø-Beherrschung in Prozent über die eingeführten Items (Ziel: ≥ Zielwert).</summary>
    AvgMastery = 0,
    /// <summary>Abdeckung in Prozent: eingeführte / vorhandene Items (Ziel: ≥ Zielwert).</summary>
    Coverage = 1,
    /// <summary>Anteil beherrschter Items in Prozent: Box ≥ MaxBox / vorhandene Items (Ziel: ≥ Zielwert).</summary>
    MasteredPercent = 2,
    /// <summary>Höchstzahl schwacher Items (Beherrschung &lt; 50 %) – „nicht mehr als N" (Ziel: ≤ Zielwert).</summary>
    MaxWeakItems = 3,
}

/// <summary>
/// Ein vom Vater gesetztes <b>Ergebnis-/Beherrschungsziel</b> für ein Kind auf einem Katalog-Ausschnitt
/// (Fach, optional Kapitel, optional Übung). Anders als das plan-gebundene Pflicht-/Rhythmus-Ziel der
/// <see cref="PlanPosition"/> (Tag/Woche) und die aktivitätsbasierten <see cref="Mission"/>s misst ein
/// <c>LearnGoal</c> den <b>Lernstand</b> (Beherrschung/Abdeckung) und ist damit plan-übergreifend: es hängt
/// am Kind und am Katalog-Scope, nicht an einer Position, und überlebt das Abhängen einer Übung.
/// Der Zielstatus wird bei jeder Abfrage <b>live</b> aus dem aggregierten Lernstand berechnet
/// (kein materialisierter Zustand); belohnt wird in v1 nicht.
/// </summary>
public class LearnGoal
{
    public int Id { get; set; }

    public int ChildId { get; set; }
    public Child? Child { get; set; }

    // --- Katalog-Scope (Hierarchie: Exercise ⊂ Chapter ⊂ Subject) ---
    /// <summary>Fach des Ziels (Pflicht).</summary>
    public int SubjectId { get; set; }
    /// <summary>Optional: Kapitel; <c>null</c> = ganzes Fach.</summary>
    public int? ChapterId { get; set; }
    /// <summary>Optional: konkrete Vokabelübung; <c>null</c> = ganzes Kapitel/Fach. Setzt <see cref="ChapterId"/> voraus.</summary>
    public int? ExerciseId { get; set; }

    // --- Ziel ---
    /// <summary>Gemessene Kennzahl.</summary>
    public LearnGoalMetric Metric { get; set; }
    /// <summary>Zielwert (Prozent 0..100 bzw. Anzahl bei <see cref="LearnGoalMetric.MaxWeakItems"/>).</summary>
    public int TargetValue { get; set; }
    /// <summary>Optionaler Stichtag; danach gilt ein unerreichtes Ziel als „überfällig".</summary>
    public DateOnly? DueDate { get; set; }
    /// <summary>Optionaler frei wählbarer Titel (sonst aus Scope/Metric ableitbar).</summary>
    public string? Title { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
