namespace Pugling.Api.Models;

// Motivations-Ebene über den Einzel-Boni: Missionen (zeitgebundene, wiederholbare Ziele) und
// Auszeichnungen (permanente Meilensteine). Beide messen dieselben Fortschritts-Metriken über die
// Aktivität eines Kindes (siehe Services.MetricsService) und schütten über ChildPointsEntry aus.

/// <summary>
/// Messbare Größe der Lern-Aktivität eines Kindes – gemeinsame Basis für Missionen und Auszeichnungen.
/// Alle Werte werden serverseitig aus den bestehenden Tabellen berechnet (kein Client-Vertrauen).
/// </summary>
public enum ProgressMetric
{
    /// <summary>Neu eingeführte Inhalte (StudyPlanItem.IntroducedAt).</summary>
    NewWords = 0,
    /// <summary>Richtige Leitner-Wiederholungen (ReviewEvent.WasCorrect).</summary>
    CorrectReviews = 1,
    /// <summary>Bestandene Abschlusstests (TestAttempt.Passed).</summary>
    TestsPassed = 2,
    /// <summary>Geübte Minuten (PracticeSession.ActiveSeconds).</summary>
    MinutesPracticed = 3,
    /// <summary>Vollständig geschaffte Tage (StudyDayReward Kind=DayCompleteBonus).</summary>
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

/// <summary>
/// Ein vom Vater definiertes Ziel für ein Kind (Tages-/Wochen-/Zusatzziel). Erfüllt das Kind im
/// jeweiligen Zeitraum die <see cref="Target"/>-Marke der <see cref="Metric"/>, gibt es einmalig
/// <see cref="RewardPoints"/>. Sinnvolle Vorlagen werden geseedet, sind aber frei editier-/löschbar.
/// </summary>
public class Mission
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    public string Title { get; set; } = "";
    public ProgressMetric Metric { get; set; }
    /// <summary>Zu erreichender Wert der Metrik im Zeitraum.</summary>
    public int Target { get; set; }
    public MissionPeriod Period { get; set; }
    /// <summary>Belohnung bei Erfüllung (einmal je Zeitraum).</summary>
    public int RewardPoints { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Protokolliert die einmalige Belohnung einer Mission je Zeitraum (idempotent, Anti-Doppelvergabe).</summary>
public class MissionAward
{
    public int Id { get; set; }
    public int MissionId { get; set; }
    public Mission? Mission { get; set; }
    /// <summary>Zeitraum-Schlüssel: "2026-07-04" (täglich), "2026-W27" (wöchentlich) oder "once".</summary>
    public string PeriodKey { get; set; } = "";
    public int Points { get; set; }
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Eine vom Vater definierte Auszeichnung (Badge) für ein Kind: ab <see cref="Threshold"/> der
/// <see cref="Metric"/> (lebenslang gezählt bzw. aktuelle Serie) einmalig verliehen, mit Emoji-Icon
/// und optionaler Punkte-Belohnung. Duolingo-artige Meilensteine, frei konfigurierbar.
/// </summary>
public class Achievement
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    public string Title { get; set; } = "";
    /// <summary>Emoji o. Ä. für die Badge-Darstellung (z. B. "🔥").</summary>
    public string? Icon { get; set; }
    public ProgressMetric Metric { get; set; }
    /// <summary>Schwelle, ab der die Auszeichnung erreicht ist.</summary>
    public int Threshold { get; set; }
    /// <summary>Optionale Punkte-Belohnung beim Erreichen (0 = nur Badge).</summary>
    public int RewardPoints { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Protokolliert, wann ein Kind eine Auszeichnung erreicht hat (genau einmal, idempotent).</summary>
public class AchievementAward
{
    public int Id { get; set; }
    public int AchievementId { get; set; }
    public Achievement? Achievement { get; set; }
    public int Points { get; set; }
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Wiederkehr eines Angebots – bestimmt, über welches Zeitfenster das Kontingent zählt und sich erneuert.</summary>
public enum OfferPeriod
{
    /// <summary>Einmalig; das Kontingent gilt über die gesamte Laufzeit und füllt sich nicht wieder auf.</summary>
    OneOff = 0,
    /// <summary>Pro Kalendertag (UTC); erneuert sich täglich.</summary>
    Daily = 1,
    /// <summary>Pro ISO-Woche (Mo–So); erneuert sich wöchentlich.</summary>
    Weekly = 2,
    /// <summary>Pro Kalendermonat; erneuert sich monatlich.</summary>
    Monthly = 3,
}

/// <summary>
/// Ein vom Vater definiertes, kaufbares <b>Angebot</b> (reale Belohnung, z. B. „1 h Spielzeit" oder
/// „Taschengeld") mit Münz-Preis. Anders als Missionen/Auszeichnungen wird hier <b>ausgegeben</b> statt
/// verdient: der Sohn kauft sofort (Münzen werden abgebucht), der Vater erfüllt später seinen Teil der
/// Abmachung (siehe <see cref="RewardRedemption"/>). <see cref="Quantity"/> Käufe sind je
/// <see cref="Period"/> möglich; das Kontingent füllt sich mit jeder neuen Periode wieder auf.
/// </summary>
public class Reward
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    public string Title { get; set; } = "";
    /// <summary>Kosten in Münzen, die bei jedem Kauf vom Kind-Konto abgebucht werden.</summary>
    public int Cost { get; set; }
    /// <summary>Wiederkehr des Angebots; steuert das Zeitfenster des Kontingents.</summary>
    public OfferPeriod Period { get; set; } = OfferPeriod.OneOff;
    /// <summary>Kontingent: wie oft das Angebot je <see cref="Period"/> gekauft werden kann (≥ 1).</summary>
    public int Quantity { get; set; } = 1;
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Stand eines gekauften Angebots im Sohn-Konto – der Kauf ist sofort verbindlich, die Erfüllung folgt.</summary>
public enum RewardRedemptionStatus
{
    /// <summary>Vom Sohn gekauft – Münzen wurden abgebucht, wartet auf die reale Erfüllung durch den Vater.</summary>
    Purchased = 0,
    /// <summary>Vom Vater erfüllt (reale Leistung erbracht) – abgeschlossen.</summary>
    Fulfilled = 1,
    /// <summary>Vom Vater storniert – die Münzen wurden zurückerstattet, der Kontingent-Slot ist wieder frei.</summary>
    Cancelled = 2,
}

/// <summary>
/// Ein vom Sohn gekauftes Angebot (<see cref="Reward"/>) im Konto. Titel/Kosten werden als Momentaufnahme
/// festgehalten, damit die Historie stabil bleibt, auch wenn das Angebot später geändert/gelöscht wird.
/// Die Münz-Abbuchung (negative <c>PointKind.Reward</c>-Buchung) erfolgt <b>sofort beim Kauf</b>; der Vater
/// erfüllt oder storniert (Rückerstattung) danach. Zeigt dem Sohn „gekauft am … – erfüllt am …".
/// </summary>
public class RewardRedemption
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    /// <summary>Referenz auf das Angebot; wird auf null gesetzt, falls das Angebot später gelöscht wird.</summary>
    public int? RewardId { get; set; }
    public Reward? Reward { get; set; }
    /// <summary>Titel des Angebots zum Kaufzeitpunkt (Momentaufnahme).</summary>
    public string Title { get; set; } = "";
    /// <summary>Kosten zum Kaufzeitpunkt (Momentaufnahme); maßgeblich für Abbuchung und Rückerstattung.</summary>
    public int Cost { get; set; }
    public RewardRedemptionStatus Status { get; set; } = RewardRedemptionStatus.Purchased;
    /// <summary>Kaufzeitpunkt; bestimmt zugleich, in welche Kontingent-Periode der Kauf fällt.</summary>
    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Zeitpunkt der Erfüllung bzw. Stornierung durch den Vater (null solange offen).</summary>
    public DateTime? FulfilledAt { get; set; }
}
