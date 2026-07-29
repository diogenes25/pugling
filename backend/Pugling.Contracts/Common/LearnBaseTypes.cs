namespace Pugling.Contracts;

/// <summary>
/// Schularten, für die eine Übung geeignet ist. <c>[Flags]</c>-Enum, damit eine Übung
/// mehreren Schularten zugeordnet werden kann (z. B. Realschule | Gymnasium).
/// <see cref="None"/> bedeutet „für alle Schularten" (kein Filter-Ausschluss).
/// </summary>
[Flags]
public enum SchoolTypes
{
    /// <summary>Keine Einschränkung – die Übung passt zu jeder Schulart.</summary>
    None = 0,
    /// <summary>Grundschule.</summary>
    Grundschule = 1,
    /// <summary>Hauptschule.</summary>
    Hauptschule = 2,
    /// <summary>Realschule.</summary>
    Realschule = 4,
    /// <summary>Gymnasium.</summary>
    Gymnasium = 8,
    /// <summary>Gesamtschule.</summary>
    Gesamtschule = 16,
    /// <summary>Berufsschule.</summary>
    Berufsschule = 32,
}

/// <summary>
/// Vom Übungsersteller vorgeschlagenes Bonus-System (global an der Übung). Dient nur als Vorlage:
/// beim Erzeugen eines Lehrplans aus der Übung werden diese Werte EINMAL in dessen Bonus-Felder
/// kopiert. Spätere Änderungen an der Übung wirken damit NICHT rückwirkend auf bestehende Kind-Pläne –
/// das laufende Bonus-System bleibt kind-individuell und pro Lehrplan anpassbar (Motivations-Steuerung
/// je Kind/Übung). Felder spiegeln die Bonus-Knöpfe des <c>StudyPlan</c>.
/// </summary>
public record SuggestedBonus(
    int ComboThreshold,
    int ComboBonusPoints,
    int SpeedThresholdSeconds,
    int SpeedBonusPoints,
    int NewContentPoints);

/// <summary>
/// RWX-Recht, das ein Owner einem einzelnen Creator an einer Übung erteilt. Hierarchie
/// <see cref="Owner"/> ⊃ <see cref="Write"/> ⊃ <see cref="Execute"/>: Owner darf zusätzlich löschen,
/// <c>Exercise.ExecutePublic</c> umschalten und selbst Rechte vergeben/entziehen. Read ist bewusst
/// nicht Teil des Modells – der Katalog bleibt für alle lesbar (geteilte Bibliothek).
/// </summary>
public enum GrantPermission
{
    /// <summary>Voller Zugriff: ändern, löschen, Freigabe umschalten, Rechte vergeben und entziehen.</summary>
    Owner,
    /// <summary>Darf die Übung inhaltlich ändern, aber nicht löschen und keine Rechte vergeben.</summary>
    Write,
    /// <summary>Darf die Übung einem betreuten Kind zuweisen und ausspielen, aber nicht ändern.</summary>
    Execute,
}
