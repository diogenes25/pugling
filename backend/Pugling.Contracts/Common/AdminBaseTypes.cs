namespace Pugling.Contracts;

/// <summary>Verwandtschaftsrolle eines Supervisors zum Studenten (rein deskriptiv).</summary>
public enum SupervisorRelation
{
    Father = 0,
    Mother = 1,
    Grandma = 2,
    Grandpa = 3,
    Guardian = 4,
    Other = 5,
}

/// <summary>Geschlecht des Kindes (rein deskriptiv). Teil des übungsunabhängigen Profils; ein späterer
/// Lehrplan-Generator nutzt es allenfalls für die sprachliche Ansprache, nie für die Filterung des Stoffs.</summary>
public enum Gender
{
    None = 0,
    Male = 1,
    Female = 2,
    Diverse = 3,
}

/// <summary>
/// Kategorie einer Punkte-Buchung – macht Boni auswertbar/deckelbar (z. B. "wie viele Punkte
/// kamen aus Combo vs. Uhrzeit?"). <see cref="Base"/> ist der Standard für Altbuchungen.
/// </summary>
public enum PointKind
{
    /// <summary>Basispunkte einer richtigen Wiederholung (inkl. Zeitfenster-Faktor).</summary>
    Base = 0,
    /// <summary>Manuelle Vater-Buchung (Gutschrift/Einlösung).</summary>
    Manual = 1,
    /// <summary>Tagesziel Übungszeit erreicht.</summary>
    Minutes = 2,
    /// <summary>Abschlusstest bestanden.</summary>
    Test = 3,
    /// <summary>Tag vollständig (Zeit + Test).</summary>
    DayComplete = 4,
    /// <summary>Combo-Bonus (Treffer in Folge).</summary>
    Combo = 5,
    /// <summary>Bonus für schnelle Antwort.</summary>
    Speed = 6,
    /// <summary>Bonus für durchgehende Lernzeit.</summary>
    Duration = 7,
    /// <summary>Belohnung für eine erfüllte Mission (Tages-/Wochen-/Zusatzziel).</summary>
    Mission = 8,
    /// <summary>Belohnung für eine erreichte Auszeichnung.</summary>
    Achievement = 9,
    /// <summary>Einlösung von Münzen für einen Skin (negative Buchung).</summary>
    SkinPurchase = 10,
    /// <summary>Einlösung von Münzen für eine reale Prämie (z. B. Fernseh-/Spielzeit; negative Buchung).</summary>
    Reward = 11,
    /// <summary>Ziel einer Lehrplan-Position erreicht (Tages-/Wochenziel der Übung).</summary>
    Goal = 12,
    /// <summary>Einlösung von Münzen für einen Familien-Shop-Artikel (negative Buchung).</summary>
    ShopCoins = 13,
    /// <summary>Einlösung von Gems für einen Familien-Shop-Artikel (negative Buchung).</summary>
    ShopGems = 14,
    /// <summary>Manuelle Vater-Buchung in Gems (Gem-Zwilling zu <see cref="Manual"/>; Geschenk/Korrektur).</summary>
    ManualGems = 15,
    /// <summary>Malus, weil ein Pflichtziel einer Lehrplan-Position in der Periode gerissen wurde (negative Buchung).</summary>
    GoalPenalty = 16,
    /// <summary>Belohnung für ein erreichtes verbindliches Lernziel/Objective bzw. eine seiner Etappen (Münzen).</summary>
    ObjectiveCoins = 17,
    /// <summary>Belohnung für ein erreichtes Dehnungs-Objective bzw. eine seiner Etappen (Gems).</summary>
    ObjectiveGems = 18,
}
