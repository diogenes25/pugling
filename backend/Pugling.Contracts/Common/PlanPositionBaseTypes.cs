namespace Pugling.Contracts;

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
/// Reihenfolge-Strategie, in der der Server die (fälligen) Inhalte einer Position ausspielt. Die Reihenfolge
/// wird bei Sitzungs-/Testbeginn <b>einmal</b> materialisiert (eingefroren), damit sie sich nicht mitten im
/// Lauf verschiebt, wenn sich Boxen durch Antworten ändern.
/// </summary>
public enum PracticeOrder
{
    /// <summary>Schwächste zuerst: nach Leitner-Box aufsteigend, dann Index (Standard, bisheriges Verhalten).</summary>
    WeakestFirst = 0,
    /// <summary>Streng seriell nach Item-Index.</summary>
    Serial = 1,
    /// <summary>Zufällige Reihenfolge (einmalig beim Einfrieren gemischt).</summary>
    Random = 2,
    /// <summary>Gewichtete Ziehung: zuletzt eingeführte (bzw. noch nie eingeführte) Inhalte stark bevorzugt.</summary>
    NewestWeighted = 3,
}
