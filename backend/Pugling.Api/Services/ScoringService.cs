using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services;

/// <summary>
/// Die eine Stelle, die eine Leitner-Wiederholung in Punkte übersetzt: Basispunkte
/// (Box/Neuheit × Zeitfenster-Faktor) plus Ereignis-Boni (Combo; später Schnelle Antwort, Dauer).
/// Bündelt bewusst, was vorher über <c>PointsService</c> und den Controller verstreut war, damit
/// neue Bonusarten an genau einer Stelle andocken und jede Buchung ihre <see cref="PointKind"/> trägt.
/// Zustandslos bis auf den Zeitfenster-Lookup (DB).
/// </summary>
public class ScoringService(PuglingDbContext db)
{
    /// <summary>Ein einzelner Punkte-Beitrag einer Wiederholung – wird 1:1 zu einer <see cref="ChildPointsEntry"/>.</summary>
    public record Contribution(PointKind Kind, int Amount, string Reason);

    /// <summary>
    /// Gesamtergebnis einer bewerteten Wiederholung: die Buchungen (<paramref name="Contributions"/>),
    /// die erreichte Combo und – als bequemer Direktzugriff fürs Frontend – Basispunkte und Combo-Bonus.
    /// </summary>
    public record ReviewScore(IReadOnlyList<Contribution> Contributions, int Combo)
    {
        /// <summary>Basispunkte (ohne Boni) – für <c>ReviewOutcome.Awarded</c>.</summary>
        public int BasePoints => Contributions.Where(c => c.Kind == PointKind.Base).Sum(c => c.Amount);
        /// <summary>Combo-Bonus dieser Wiederholung – für <c>ReviewOutcome.ComboBonus</c>.</summary>
        public int ComboBonus => Contributions.Where(c => c.Kind == PointKind.Combo).Sum(c => c.Amount);
        /// <summary>Schnelle-Antwort-Bonus dieser Wiederholung.</summary>
        public int SpeedBonus => Contributions.Where(c => c.Kind == PointKind.Speed).Sum(c => c.Amount);
        /// <summary>Summe aller Beiträge (Basis + Boni).</summary>
        public int Total => Contributions.Sum(c => c.Amount);
    }

    /// <summary>Untergrenze für die Schnelle-Antwort-Messung: darunter zählt es als Doppel-Klick/Automatik, nicht als „schnell".</summary>
    private const double MinSpeedSeconds = 1.0;

    /// <summary>
    /// Verfahrensneutrale Punkte-Einstellung einer Wiederholung – stammt von der <see cref="PlanPosition"/>
    /// (pro Übung). <paramref name="Label"/> geht in den Buchungstext ein.
    /// </summary>
    public record ScoreConfig(string Label, int NewContentPoints, int ComboThreshold, int ComboBonusPoints,
        int SpeedThresholdSeconds, int SpeedBonusPoints);

    /// <summary>
    /// Bewertet eine Wiederholung und liefert alle fälligen Punkte-Buchungen. VOR dem Box-Aufstieg
    /// aufrufen (<paramref name="box"/>/<paramref name="reviewCount"/> im Zustand davor – neuer Inhalt
    /// zählt am meisten). Falsche Antwort → keine Punkte. <paramref name="postBox"/> ist die Box NACH
    /// dem Aufstieg, nur für den Buchungstext. <paramref name="elapsedSeconds"/> ist die serverseitig
    /// gemessene Zeit seit der letzten Antwort (null bei der ersten Karte einer Sitzung).
    /// </summary>
    public async Task<ReviewScore> ScoreReviewAsync(ScoreConfig cfg, int reviewCount, int box, int postBox,
        bool wasCorrect, int combo, DateTime nowLocal, double? elapsedSeconds = null)
    {
        var contributions = new List<Contribution>();
        if (!wasCorrect)
            return new ReviewScore(contributions, combo);

        var basePoints = await BasePointsAsync(cfg, reviewCount, box, nowLocal);
        if (basePoints > 0)
            contributions.Add(new Contribution(PointKind.Base, basePoints,
                $"[{cfg.Label}] Leitner-Wiederholung richtig → Box {postBox}"));

        var comboBonus = ComboBonus(cfg, combo);
        if (comboBonus > 0)
            contributions.Add(new Contribution(PointKind.Combo, comboBonus,
                $"[{cfg.Label}] Combo ×{combo} – Bonus!"));

        if (IsFastAnswer(cfg, elapsedSeconds))
            contributions.Add(new Contribution(PointKind.Speed, cfg.SpeedBonusPoints,
                $"[{cfg.Label}] Schnelle Antwort (≤ {cfg.SpeedThresholdSeconds}s) – Bonus!"));

        return new ReviewScore(contributions, combo);
    }

    /// <summary>
    /// Schnell genug für den Bonus? Nur wenn Feature an (Schwelle &amp; Bonus &gt; 0) und die gemessene
    /// Zeit im Fenster [<see cref="MinSpeedSeconds"/>, Schwelle] liegt – die Untergrenze verhindert
    /// Punkte-Farming durch Doppel-Submits.
    /// </summary>
    private static bool IsFastAnswer(ScoreConfig cfg, double? elapsedSeconds) =>
        cfg.SpeedThresholdSeconds > 0 && cfg.SpeedBonusPoints > 0
        && elapsedSeconds is { } s && s >= MinSpeedSeconds && s <= cfg.SpeedThresholdSeconds;

    /// <summary>
    /// Combo-Bonus laut Einstellung: alle <see cref="ScoreConfig.ComboThreshold"/> Treffer in Folge
    /// ein eskalierender Bonus (Basis × Meilenstein-Nummer). Schwelle oder Basis 0 → Feature aus.
    /// </summary>
    private static int ComboBonus(ScoreConfig cfg, int combo) =>
        cfg.ComboThreshold > 0 && cfg.ComboBonusPoints > 0 && combo > 0 && combo % cfg.ComboThreshold == 0
            ? cfg.ComboBonusPoints * (combo / cfg.ComboThreshold)
            : 0;

    /// <summary>
    /// Basispunkte einer richtigen Wiederholung, verfahrensneutral: erstmalige Wiederholung
    /// (<paramref name="reviewCount"/> 0) zählt am meisten, spätere je höher die <paramref name="box"/>
    /// weniger; gewichtet nach dem zur Uhrzeit aktiven Zeitfenster.
    /// </summary>
    private async Task<int> BasePointsAsync(ScoreConfig cfg, int reviewCount, int box, DateTime nowLocal)
    {
        int basePoints = reviewCount == 0
            ? cfg.NewContentPoints                // neuer Inhalt (konfigurierbar)
            : Math.Max(2, 8 - box);               // Wiederholung: je höher die Box, desto weniger

        var time = TimeOnly.FromDateTime(nowLocal);
        var slot = await db.TimeSlots
            .Where(s => s.StartTime <= time && time < s.EndTime)
            .FirstOrDefaultAsync();

        return (int)Math.Round(basePoints * (slot?.Multiplier ?? 1.0));
    }
}
