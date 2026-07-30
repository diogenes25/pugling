namespace Pugling.Contracts;

/// <summary>
/// Art eines <c>Objective</c> – bestimmt Ton und Währung der Belohnung. <see cref="Committed"/> ist ein
/// verbindliches Ziel (Belohnung in 🪙 Münzen = reale Privilegien); <see cref="Stretch"/> ist ein
/// „Dehnungsziel" (Belohnung in 💎 Gems = kosmetisch). Bewusst gibt es <b>keinen Malus</b> auf Objectives –
/// der „Stick" wohnt allein am Pflichtziel der <c>PlanPosition</c> (siehe <c>PenaltyCoins</c>).
/// </summary>
public enum ObjectiveKind
{
    /// <summary>Verbindliches Ziel; Belohnung in Münzen (Currency.Coins).</summary>
    Committed = 0,
    /// <summary>Dehnungsziel; Belohnung in Gems (Currency.Gems).</summary>
    Stretch = 1,
}

/// <summary>
/// Kennzahl, an der ein <c>KeyResult</c> gemessen wird. Bewusst nur <b>tricksichere</b> Größen:
/// die outcome-/Leitner-basierten (<see cref="AvgMastery"/>/<see cref="MasteredPercent"/>/<see cref="MaxWeakItems"/>,
/// aus <c>MasteryRollup</c>) und die vom Vater getippte Klassenarbeits-Note
/// (<see cref="ClassTestGrade"/>). Reine Aktivitäts-Zähler (Minuten/Wiederholungen) fehlen absichtlich –
/// sie belohnen Wiederholen statt Können und wären farmbar. Die abdeckungsbasierte „Coverage" (Wert 1 beim
/// früheren <c>LearnGoalMetric</c>) fehlt hier bewusst: sie steigt schon durchs bloße Sehen von Vokabeln – sie
/// war der einzige Unterschied der entfallenen Lernziel-Ebene, und zwar ein farmbarer.
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
