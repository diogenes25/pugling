namespace Pugling.Contracts;

/// <summary>
/// Kennzahl, an der ein <c>LearnGoal</c> gemessen wird – jede bildet direkt ein Feld des
/// aggregierten Lernstands (siehe <c>MasteryRollup</c>) ab.
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
