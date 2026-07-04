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

/// <summary>
/// Eine vom Vater definierte, einlösbare Prämie (reale Belohnung, z. B. „30 Min Fernsehen") mit
/// Münz-Preis. Anders als Missionen/Auszeichnungen wird hier <b>ausgegeben</b> statt verdient:
/// der Sohn fragt eine Einlösung an, der Vater genehmigt sie (siehe <see cref="RewardRedemption"/>).
/// </summary>
public class Reward
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    public string Title { get; set; } = "";
    /// <summary>Kosten in Münzen, die bei Genehmigung vom Kind-Konto abgebucht werden.</summary>
    public int Cost { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Status einer Einlöse-Anfrage: der Vater entscheidet, erst dann fließen Münzen.</summary>
public enum RewardRedemptionStatus
{
    /// <summary>Vom Sohn angefragt, wartet auf Vater-Entscheidung (noch keine Abbuchung).</summary>
    Requested = 0,
    /// <summary>Vom Vater genehmigt – Münzen wurden abgebucht.</summary>
    Approved = 1,
    /// <summary>Vom Vater abgelehnt – keine Abbuchung.</summary>
    Rejected = 2,
}

/// <summary>
/// Einlöse-Anfrage des Sohns für eine <see cref="Reward"/>. Titel/Kosten werden als Momentaufnahme
/// festgehalten, damit die Historie stabil bleibt, auch wenn die Prämie später geändert/gelöscht wird.
/// Die Münz-Abbuchung (negative <see cref="Child.PointsEntries"/>-Buchung, <c>PointKind.Reward</c>)
/// erfolgt erst bei Genehmigung durch den Vater.
/// </summary>
public class RewardRedemption
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    /// <summary>Referenz auf die Prämie; wird auf null gesetzt, falls die Prämie später gelöscht wird.</summary>
    public int? RewardId { get; set; }
    public Reward? Reward { get; set; }
    /// <summary>Titel der Prämie zum Anfragezeitpunkt (Momentaufnahme).</summary>
    public string Title { get; set; } = "";
    /// <summary>Kosten zum Anfragezeitpunkt (Momentaufnahme); maßgeblich für die Abbuchung.</summary>
    public int Cost { get; set; }
    public RewardRedemptionStatus Status { get; set; } = RewardRedemptionStatus.Requested;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAt { get; set; }
}
