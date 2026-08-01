using Microsoft.Extensions.Options;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// The single place that translates a Leitner review into points: base points
/// (box/novelty × time-slot factor) plus event bonuses (combo; later fast answer, duration).
/// Deliberately bundles what used to be scattered across <c>PointsService</c> and the controller, so
/// new bonus kinds dock in exactly one place and every ledger entry carries its <see cref="PointKind"/>.
/// <b>Completely stateless</b> – a pure function. Until E12 a <c>PuglingDbContext</c> hung here, solely for
/// the time-slot lookup; ever since the slots became configuration (<see cref="ScoringOptions"/>), the point
/// calculation needs neither a database nor a cancellation token. That makes it testable without a host.
/// </summary>
public class ScoringService(IOptions<ScoringOptions> options)
{
    /// <summary>A single point contribution of a review – maps 1:1 to a <see cref="ChildPointsEntry"/>.</summary>
    public record Contribution(PointKind Kind, int Amount, string Reason);

    /// <summary>
    /// Overall result of a scored review: the ledger entries (<paramref name="Contributions"/>),
    /// the combo reached and – as a convenient direct access for the frontend – base points and combo bonus.
    /// </summary>
    public record ReviewScore(IReadOnlyList<Contribution> Contributions, int Combo)
    {
        /// <summary>Base points (without bonuses) – for <c>ReviewOutcome.Awarded</c>.</summary>
        public int BasePoints => Contributions.Where(c => c.Kind == PointKind.Base).Sum(c => c.Amount);
        /// <summary>Combo bonus of this review – for <c>ReviewOutcome.ComboBonus</c>.</summary>
        public int ComboBonus => Contributions.Where(c => c.Kind == PointKind.Combo).Sum(c => c.Amount);
        /// <summary>Fast-answer bonus of this review.</summary>
        public int SpeedBonus => Contributions.Where(c => c.Kind == PointKind.Speed).Sum(c => c.Amount);
        /// <summary>Sum of all contributions (base + bonuses).</summary>
        public int Total => Contributions.Sum(c => c.Amount);
    }

    /// <summary>Lower bound for the fast-answer measurement: below this it counts as a double click/automation, not as "fast".</summary>
    private const double MinSpeedSeconds = 1.0;

    /// <summary>
    /// Procedure-neutral point settings for a review – comes from the <see cref="PlanPosition"/>
    /// (per exercise). <paramref name="Label"/> goes into the ledger entry text.
    /// </summary>
    public record ScoreConfig(string Label, int NewContentPoints, int ComboThreshold, int ComboBonusPoints,
        int SpeedThresholdSeconds, int SpeedBonusPoints);

    /// <summary>
    /// Scores a review and returns all due point contributions. Call BEFORE the box promotion
    /// (<paramref name="box"/>/<paramref name="reviewCount"/> in the state before it – new content
    /// counts the most). Wrong answer → no points. <paramref name="postBox"/> is the box AFTER
    /// the promotion, only for the ledger entry text. <paramref name="elapsedSeconds"/> is the
    /// server-side measured time since the last answer (null for the first card of a session).
    /// </summary>
    public ReviewScore ScoreReview(ScoreConfig cfg, int reviewCount, int box, int postBox,
        bool wasCorrect, int combo, DateTime nowLocal, double? elapsedSeconds = null)
    {
        var contributions = new List<Contribution>();
        if (!wasCorrect)
            return new ReviewScore(contributions, combo);

        var basePoints = BasePoints(cfg, reviewCount, box, nowLocal);
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
    /// Fast enough for the bonus? Only if the feature is on (threshold &amp; bonus &gt; 0) and the measured
    /// time falls within the window [<see cref="MinSpeedSeconds"/>, threshold] – the lower bound prevents
    /// point farming through double submits.
    /// </summary>
    private static bool IsFastAnswer(ScoreConfig cfg, double? elapsedSeconds) =>
        cfg.SpeedThresholdSeconds > 0 && cfg.SpeedBonusPoints > 0
        && elapsedSeconds is { } s && s >= MinSpeedSeconds && s <= cfg.SpeedThresholdSeconds;

    /// <summary>
    /// Combo bonus per settings: every <see cref="ScoreConfig.ComboThreshold"/> hits in a row give
    /// an escalating bonus (base × milestone number). Threshold or base 0 → feature off.
    /// </summary>
    private static int ComboBonus(ScoreConfig cfg, int combo) =>
        cfg.ComboThreshold > 0 && cfg.ComboBonusPoints > 0 && combo > 0 && combo % cfg.ComboThreshold == 0
            ? cfg.ComboBonusPoints * (combo / cfg.ComboThreshold)
            : 0;

    /// <summary>
    /// Base points of a correct review, procedure-neutral: the first review
    /// (<paramref name="reviewCount"/> 0) counts the most, later ones less the higher the
    /// <paramref name="box"/>; weighted by the time slot active at that time of day.
    /// </summary>
    private int BasePoints(ScoreConfig cfg, int reviewCount, int box, DateTime nowLocal)
    {
        int basePoints = reviewCount == 0
            ? cfg.NewContentPoints                // new content (configurable)
            : Math.Max(2, 8 - box);               // repetition: the higher the box, the less

        return (int)Math.Round(basePoints * MultiplierAt(TimeOnly.FromDateTime(nowLocal)));
    }

    /// <summary>
    /// The multiplier for the time of day; 1.0 outside all slots or when the slots are switched off.
    /// <para>
    /// Deterministically ordered: overlapping slots are allowed (the configuration does not forbid them), and
    /// without a fixed ordering the order in the file would decide which multiplier applies - the same correct
    /// answer would then yield a different number of points. The slot starting latest (the narrowest) wins, on
    /// a tie the one ending earlier.
    /// </para>
    /// </summary>
    private double MultiplierAt(TimeOnly time)
    {
        var cfg = options.Value;
        if (!cfg.TimeSlotsEnabled) return 1.0;

        return cfg.TimeSlots
            .Where(s => s.Start <= time && time < s.End)
            .OrderByDescending(s => s.Start).ThenBy(s => s.End)
            .Select(s => s.Multiplier)
            .DefaultIfEmpty(1.0)
            .First();
    }
}
