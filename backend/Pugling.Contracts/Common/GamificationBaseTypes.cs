namespace Pugling.Contracts;

/// <summary>
/// Messbare Größe der Lern-Aktivität eines Kindes – gemeinsame Basis für Missionen und Auszeichnungen.
/// Alle Werte werden serverseitig aus den bestehenden Tabellen berechnet (kein Client-Vertrauen).
/// </summary>
public enum ProgressMetric
{
    /// <summary>Neu eingeführte Inhalte (PositionItemProgress.IntroducedAt).</summary>
    NewWords = 0,
    /// <summary>Richtige Leitner-Wiederholungen (ReviewEvent.WasCorrect).</summary>
    CorrectReviews = 1,
    /// <summary>Bestandene Abschlusstests (TestAttempt.Passed).</summary>
    TestsPassed = 2,
    /// <summary>Geübte Minuten (PracticeSession.ActiveSeconds).</summary>
    MinutesPracticed = 3,
    /// <summary>Vollständig geschaffte Tage nach der Tagesregel des <c>PositionProgressService</c>.</summary>
    DaysComplete = 4,
    /// <summary>Aktuelle Serie aufeinanderfolgender vollständiger Tage (nur sinnvoll für Auszeichnungen).</summary>
    StreakDays = 5,
}

/// <summary>Zeitraum, über den eine Mission zählt und sich erneuert.</summary>
public enum MissionPeriod
{
    /// <summary>Pro Kalendertag (UTC); erneuert sich täglich.</summary>
    Daily = 0,
    /// <summary>Pro ISO-Woche (Mo–So); erneuert sich wöchentlich.</summary>
    Weekly = 1,
    /// <summary>Einmalig; erfüllt und dann dauerhaft erledigt.</summary>
    OneOff = 2,
}

/// <summary>Maßeinheit des Artikels – bestimmt, wie Mengen im Inventar und bei der Aktivierung dargestellt werden.</summary>
public enum UnitType
{
    /// <summary>Einheiten ohne spezifische Maßeinheit (Stückzahl).</summary>
    Stueck = 0,
    /// <summary>Zeiteinheit Minuten (z. B. „30 Minuten Fernsehen").</summary>
    Minute = 1,
    /// <summary>Zeiteinheit Stunden.</summary>
    Stunde = 2,
    /// <summary>Gewichtseinheit Gramm (z. B. Süßigkeiten).</summary>
    Gramm = 3,
    /// <summary>Allgemeine Mal-Angabe (z. B. „3 Mal Eisessen").</summary>
    Mal = 4,
}

/// <summary>Typ der Aktion, die der Artikel repräsentiert – kategorisiert den Artikel für den Vater.</summary>
public enum ActionType
{
    /// <summary>Sonstige / nicht kategorisiert.</summary>
    Sonstiges = 0,
    /// <summary>Fernsehen / Medienkonsum.</summary>
    TV = 1,
    /// <summary>Videospielen / Zocken.</summary>
    Zocken = 2,
    /// <summary>Süßigkeiten / Snacks.</summary>
    Suessigkeit = 3,
    /// <summary>Ausflug / Freizeitaktivität.</summary>
    Ausflug = 4,
}

/// <summary>Automatische Auffüll-Regel eines Shop-Angebots (<c>ShopListing</c>).</summary>
public enum ShopRefillKind
{
    /// <summary>Keine automatische Auffüllung; Bestand wird nur vom Vater geändert.</summary>
    None = 0,
    /// <summary>Einmalig zu einem festen Zeitpunkt auffüllen.</summary>
    Once = 1,
    /// <summary>Einmal täglich auffüllen.</summary>
    Daily = 2,
    /// <summary>Zweimal täglich auffüllen.</summary>
    TwiceDaily = 3,
    /// <summary>Einmal wöchentlich an einem festen Wochentag auffüllen.</summary>
    Weekly = 4,
}

/// <summary>Stand einer historischen Shop-Kaufbuchung.</summary>
public enum ShopPurchaseStatus
{
    /// <summary>Kauf aktiv – die erworbenen Einheiten liegen im aggregierten Inventar (<c>ChildInventory</c>) des Sohns.</summary>
    Owned = 0,
    /// <summary>Kauf vom Vater storniert; Währung erstattet, Inventar entsprechend reduziert.</summary>
    Cancelled = 1,
}

/// <summary>Status einer Aktivierungsanfrage des Sohns.</summary>
public enum ActivationRequestStatus
{
    /// <summary>Anfrage gestellt – wartet auf Vater-Entscheidung.</summary>
    Pending = 0,
    /// <summary>Vom Vater genehmigt – Einheiten aus dem Inventar entnommen.</summary>
    Approved = 1,
    /// <summary>Vom Vater abgelehnt – Einheiten verbleiben im Inventar.</summary>
    Rejected = 2,
}
