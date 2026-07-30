namespace Pugling.Contracts;

/// <summary>Verwandtschaftsrolle eines Supervisors zum Studenten (rein deskriptiv).</summary>
public enum SupervisorRelation
{
    /// <summary>Vater. Hier heißt „Vater" wirklich Vater – die fachliche Zeile dahinter ist ein <c>Adult</c>.</summary>
    Father = 0,
    /// <summary>Mutter.</summary>
    Mother = 1,
    /// <summary>Großmutter.</summary>
    Grandma = 2,
    /// <summary>Großvater.</summary>
    Grandpa = 3,
    /// <summary>Sorgeberechtigte Person ohne Verwandtschaft (Vormund, Pflegeeltern).</summary>
    Guardian = 4,
    /// <summary>Sonstige betreuende Person – etwa eine Lehrkraft mit Betreuungsauftrag.</summary>
    Other = 5,
}

/// <summary>Geschlecht des Kindes (rein deskriptiv). Teil des übungsunabhängigen Profils; ein späterer
/// Lehrplan-Generator nutzt es allenfalls für die sprachliche Ansprache, nie für die Filterung des Stoffs.</summary>
public enum Gender
{
    /// <summary>Keine Angabe. Default – die Oberfläche spricht das Kind dann geschlechtsneutral an.</summary>
    None = 0,
    /// <summary>Männlich.</summary>
    Male = 1,
    /// <summary>Weiblich.</summary>
    Female = 2,
    /// <summary>Divers.</summary>
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
    /// <summary>Combo-Bonus (Treffer in Folge).</summary>
    Combo = 2,
    /// <summary>Bonus für schnelle Antwort.</summary>
    Speed = 3,
    /// <summary>Belohnung für eine erfüllte Mission (Tages-/Wochen-/Zusatzziel).</summary>
    Mission = 4,
    /// <summary>Belohnung für eine erreichte Auszeichnung.</summary>
    Achievement = 5,
    /// <summary>Einlösung von Münzen für einen Skin (negative Buchung).</summary>
    SkinPurchase = 6,
    /// <summary>Ziel einer Lehrplan-Position erreicht (Tages-/Wochenziel der Übung).</summary>
    Goal = 7,
    /// <summary>Einlösung von Münzen für einen Familien-Shop-Artikel (negative Buchung).</summary>
    ShopCoins = 8,
    /// <summary>Einlösung von Gems für einen Familien-Shop-Artikel (negative Buchung).</summary>
    ShopGems = 9,
    /// <summary>Manuelle Vater-Buchung in Gems (Gem-Zwilling zu <see cref="Manual"/>; Geschenk/Korrektur).</summary>
    ManualGems = 10,
    /// <summary>Malus, weil ein Pflichtziel einer Lehrplan-Position in der Periode gerissen wurde (negative Buchung).</summary>
    GoalPenalty = 11,
    /// <summary>Belohnung für ein erreichtes verbindliches Lernziel/Objective bzw. eine seiner Etappen (Münzen).</summary>
    ObjectiveCoins = 12,
    /// <summary>Belohnung für ein erreichtes Dehnungs-Objective bzw. eine seiner Etappen (Gems).</summary>
    ObjectiveGems = 13,
}
