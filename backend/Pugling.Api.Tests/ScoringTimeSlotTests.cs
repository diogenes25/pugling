using Microsoft.Extensions.Options;

namespace Pugling.Api.Tests;

/// <summary>
/// Checks the time-slot multiplier of the base points (<see cref="ScoringService"/>): a correct
/// review is weighted with the factor of the slot active at that time of day; outside all slots,
/// factor 1.0 applies.
/// <para>
/// <b>Without a host and without a database.</b> Until E12 the slots were a table, so this test needed the
/// web factory, a scope, a SQLite file and a <c>SaveChanges</c> – for one multiplication. Ever since the
/// slots are configuration, the service is a pure function: the slots sit in the test itself, visible next
/// to the expectation. That was exactly the reason for dissolving the table.
/// </para>
/// </summary>
public class ScoringTimeSlotTests
{
    // New content (reviewCount 0) yields exactly NewContentPoints as the base - convenient for checking the math.
    // The trailing null says "this position has no windows of its own" - the parameter carries no default on
    // purpose, so nobody can drop the position's windows by omission.
    private static readonly ScoringService.ScoreConfig Cfg = new("Test", NewContentPoints: 10, 0, 0, 0, 0, null);

    /// <summary>The same config, but with the position's own slots (<c>PlanPosition.TimeSlots</c>).</summary>
    private static ScoringService.ScoreConfig CfgWith(params ScoringTimeSlot[] slots) =>
        Cfg with { TimeSlots = slots };

    private static ScoringService With(params ScoringTimeSlot[] slots) =>
        new(Options.Create(new ScoringOptions { TimeSlotsEnabled = true, TimeSlots = [.. slots] }));

    private static ScoringTimeSlot Slot(string name, TimeOnly start, TimeOnly end, double multiplier) =>
        new() { Name = name, Start = start, End = end, Multiplier = multiplier };

    private static int BasePointsAt(ScoringService scoring, TimeOnly at) => BasePointsAt(scoring, Cfg, at);

    private static int BasePointsAt(ScoringService scoring, ScoringService.ScoreConfig cfg, TimeOnly at) =>
        scoring.ScoreReview(cfg, reviewCount: 0, box: 1, postBox: 2,
            wasCorrect: true, combo: 0,
            DateOnly.FromDateTime(DateTime.UtcNow).ToDateTime(at), elapsedSeconds: null).BasePoints;

    [Fact]
    public void Basispunkte_ImDoppelfenster_WerdenVerdoppelt_SonstUnveraendert()
    {
        var scoring = With(Slot("Nacht", new(2, 0), new(4, 0), 2.0));

        Assert.Equal(20, BasePointsAt(scoring, new(3, 0)));   // 10 × 2.0 - inside the window
        Assert.Equal(10, BasePointsAt(scoring, new(22, 30))); // 10 × 1.0 - outside every window
    }

    /// <summary>
    /// Overlapping slots are allowed (the configuration does not forbid them) – but the selection must still
    /// be <b>deterministic</b>: the narrowest slot, i.e. the one starting latest, wins. Without this ordering
    /// the order inside the file decided, and the same correct answer sometimes yielded 30, sometimes 50.
    /// </summary>
    [Fact]
    public void Bei_ueberlappenden_Fenstern_gewinnt_deterministisch_das_engste()
    {
        // Deliberately the WIDE window first: without the ordering it would come first and the test would see 30 instead of 50.
        var scoring = With(
            Slot("Weit", new(4, 0), new(6, 0), 3.0),
            Slot("Eng", new(4, 30), new(5, 0), 5.0));

        Assert.Equal(50, BasePointsAt(scoring, new(4, 45))); // in both → the narrower one applies
        Assert.Equal(30, BasePointsAt(scoring, new(4, 15))); // in the wide one only
    }

    /// <summary>
    /// The kill switch (<c>Scoring:TimeSlotsEnabled=false</c>) is not a test trick but a contract: the whole
    /// suite runs with it, because the score would otherwise hang on the time of the run and the checked-in
    /// documentation would get diff noise. So it belongs under test as well.
    /// </summary>
    [Fact]
    public void Abgeschaltete_Fenster_Bedeuten_Faktor_Eins()
    {
        var scoring = new ScoringService(Options.Create(new ScoringOptions
        {
            TimeSlotsEnabled = false,
            TimeSlots = [Slot("Nacht", new(2, 0), new(4, 0), 2.0)],
        }));

        Assert.Equal(10, BasePointsAt(scoring, new(3, 0))); // in the middle of the 2× window - and still 10
    }

    /// <summary>
    /// The obligation's own window ("homework counts double between 13:00 and 15:00") next to the global ones:
    /// both are considered, the narrowest wins. The position window must <b>not</b> quietly take the evening
    /// malus with it - that is the whole point of one shared list instead of "position replaces global".
    /// </summary>
    [Fact]
    public void Positions_Fenster_Gilt_Neben_Den_Globalen()
    {
        var scoring = With(Slot("Abend", new(20, 0), new(23, 0), 0.8));
        var cfg = CfgWith(Slot("Hausaufgaben", new(13, 0), new(15, 0), 2.0));

        Assert.Equal(20, BasePointsAt(scoring, cfg, new(14, 0)));  // 10 × 2,0 – only the position's window
        Assert.Equal(8, BasePointsAt(scoring, cfg, new(21, 0)));   // 10 × 0,8 – the global one still applies
        Assert.Equal(10, BasePointsAt(scoring, cfg, new(17, 0)));  // outside both
    }

    /// <summary>
    /// Overlap between a position window and a global one is decided by the <b>existing</b> ordering (the
    /// narrowest, i.e. the latest starting, wins) - the carrier of a window grants it no precedence. Without
    /// this, "position beats global" would be a second rule for the same question.
    /// </summary>
    [Fact]
    public void Bei_Ueberlappung_Entscheidet_Die_Breite_Nicht_Der_Traeger()
    {
        // The global window is the narrower one - so it wins although the other one belongs to the position.
        var scoring = With(Slot("Kurz", new(13, 30), new(14, 0), 3.0));
        var cfg = CfgWith(Slot("Hausaufgaben", new(13, 0), new(15, 0), 2.0));

        Assert.Equal(30, BasePointsAt(scoring, cfg, new(13, 45))); // in both → the narrower global one
        Assert.Equal(20, BasePointsAt(scoring, cfg, new(14, 30))); // only in the position's window
    }

    /// <summary>
    /// The kill switch also switches off the <b>position's</b> windows. Not an oversight but forced by the
    /// facts: it returns early, before the lists are merged - otherwise the documentation checked in by
    /// <c>DocsCaptureTests</c> would hang on the time of the run again.
    /// </summary>
    [Fact]
    public void Abgeschaltete_Fenster_Bedeuten_Faktor_Eins_Auch_Fuer_Die_Position()
    {
        var scoring = new ScoringService(Options.Create(new ScoringOptions { TimeSlotsEnabled = false }));
        var cfg = CfgWith(Slot("Hausaufgaben", new(13, 0), new(15, 0), 2.0));

        Assert.Equal(10, BasePointsAt(scoring, cfg, new(14, 0)));
    }
}
