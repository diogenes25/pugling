using Microsoft.Extensions.Options;

namespace Pugling.Api.Tests;

/// <summary>
/// Host-free boundary probes for <see cref="ScoringService"/> (B-27): each of its five numeric edges gets
/// a case sitting exactly on it, not just comfortably inside or outside it. Two of the five are already
/// pinned by <see cref="SpeedBonusTests"/>/<see cref="ComboTests"/>, but only at the cost of a full HTTP
/// host - here the same edges are pinned for free, alongside three that were never pinned at all (the
/// upper speed threshold, the time-slot boundary, and the repetition-points floor).
/// </summary>
public class ScoringServiceBoundaryTests
{
    // Deliberately all bonus features off by default (0) - each test turns on only the one it probes.
    private static readonly ScoringService.ScoreConfig Cfg =
        new("Test", NewContentPoints: 10, ComboThreshold: 0, ComboBonusPoints: 0,
            SpeedThresholdSeconds: 0, SpeedBonusPoints: 0, TimeSlots: null);

    private static ScoringService PlainService() =>
        new(Options.Create(new ScoringOptions { TimeSlotsEnabled = false }));

    [Theory]
    [InlineData(1.0, true)]    // exactly MinSpeedSeconds - counts as fast
    [InlineData(0.999, false)] // just under - the anti-farming floor rejects it
    public void SchnelleAntwort_UntereSchwelle_GreiftGenauAbEinerSekunde(double elapsedSeconds, bool expectBonus)
    {
        var cfg = Cfg with { SpeedThresholdSeconds = 10, SpeedBonusPoints = 5 };
        var score = PlainService().ScoreReview(cfg, reviewCount: 1, box: 3, postBox: 3,
            wasCorrect: true, combo: 0, DateTime.UtcNow, elapsedSeconds);

        Assert.Equal(expectBonus ? 5 : 0, score.SpeedBonus);
    }

    [Theory]
    [InlineData(10.0, true)]  // exactly the threshold - still counts as fast
    [InlineData(10.1, false)] // just over - too slow
    public void SchnelleAntwort_ObereSchwelle_GreiftGenauBisZurSchwelle(double elapsedSeconds, bool expectBonus)
    {
        var cfg = Cfg with { SpeedThresholdSeconds = 10, SpeedBonusPoints = 5 };
        var score = PlainService().ScoreReview(cfg, reviewCount: 1, box: 3, postBox: 3,
            wasCorrect: true, combo: 0, DateTime.UtcNow, elapsedSeconds);

        Assert.Equal(expectBonus ? 5 : 0, score.SpeedBonus);
    }

    [Theory]
    [InlineData(3, true)]  // exactly reaches the threshold - the bonus fires
    [InlineData(2, false)] // one short - no bonus yet
    public void ComboBonus_GreiftGenauAbDerSchwelle(int combo, bool expectBonus)
    {
        var cfg = Cfg with { ComboThreshold = 3, ComboBonusPoints = 5 };
        var score = PlainService().ScoreReview(cfg, reviewCount: 1, box: 3, postBox: 3,
            wasCorrect: true, combo, DateTime.UtcNow, elapsedSeconds: null);

        Assert.Equal(expectBonus ? 5 : 0, score.ComboBonus);
    }

    [Theory]
    [InlineData(13, 0, true)]  // exactly Start - inside, the interval is half-open
    [InlineData(15, 0, false)] // exactly End - already outside
    public void Zeitfenster_HalboffenesIntervall_GreiftAmStart_NichtAmEnde(int hour, int minute, bool expectDoubled)
    {
        var scoring = new ScoringService(Options.Create(new ScoringOptions { TimeSlotsEnabled = true }));
        var cfg = Cfg with
        {
            TimeSlots = [new ScoringTimeSlot { Name = "Fenster", Start = new TimeOnly(13, 0), End = new TimeOnly(15, 0), Multiplier = 2.0 }],
        };
        var at = DateOnly.FromDateTime(DateTime.UtcNow).ToDateTime(new TimeOnly(hour, minute));
        var score = scoring.ScoreReview(cfg, reviewCount: 0, box: 1, postBox: 2,
            wasCorrect: true, combo: 0, at, elapsedSeconds: null);

        Assert.Equal(expectDoubled ? 20 : 10, score.BasePoints);
    }

    [Theory]
    [InlineData(5, 3)] // still above the floor: 8 - 5 = 3
    [InlineData(6, 2)] // floor reached: 8 - 6 = 2
    [InlineData(7, 2)] // the clamp holds: 8 - 7 = 1, but Math.Max(2, …) keeps it at 2
    public void Wiederholungspunkte_BodenBeiZweiPunkten_GreiftAbBox6(int box, int expectedBasePoints)
    {
        var score = PlainService().ScoreReview(Cfg, reviewCount: 1, box, postBox: box + 1,
            wasCorrect: true, combo: 0, DateTime.UtcNow, elapsedSeconds: null);

        Assert.Equal(expectedBasePoints, score.BasePoints);
    }
}
