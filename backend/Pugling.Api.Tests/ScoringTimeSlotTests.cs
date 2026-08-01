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
    private static readonly ScoringService.ScoreConfig Cfg = new("Test", NewContentPoints: 10, 0, 0, 0, 0);

    private static ScoringService With(params ScoringTimeSlot[] slots) =>
        new(Options.Create(new ScoringOptions { TimeSlotsEnabled = true, TimeSlots = [.. slots] }));

    private static ScoringTimeSlot Slot(string name, TimeOnly start, TimeOnly end, double multiplier) =>
        new() { Name = name, Start = start, End = end, Multiplier = multiplier };

    private static int BasePointsAt(ScoringService scoring, TimeOnly at) =>
        scoring.ScoreReview(Cfg, reviewCount: 0, box: 1, postBox: 2,
            wasCorrect: true, combo: 0,
            DateOnly.FromDateTime(DateTime.UtcNow).ToDateTime(at), elapsedSeconds: null).BasePoints;

    [Fact]
    public void Basispunkte_ImDoppelfenster_WerdenVerdoppelt_SonstUnveraendert()
    {
        var scoring = With(Slot("Nacht", new(2, 0), new(4, 0), 2.0));

        Assert.Equal(20, BasePointsAt(scoring, new(3, 0)));   // 10 × 2,0 – im Fenster
        Assert.Equal(10, BasePointsAt(scoring, new(22, 30))); // 10 × 1,0 – außerhalb aller Fenster
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
}
