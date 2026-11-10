using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-04: which contents the test checks on a position with a daily dose - the day's portion and its two fallbacks.
/// </summary>
public class TagesportionTests
{
    private static readonly DateOnly Today = new(2026, 11, 10);

    private static PositionItemProgress Known(int index, int box, DateOnly dueOn, int reviewedDaysAgo = 3) => new()
    {
        ItemIndex = index,
        Box = box,
        IntroducedAt = Today.AddDays(-5),
        DueOn = dueOn,
        LastReviewedAt = Today.AddDays(-reviewedDaysAgo).ToDateTime(new TimeOnly(10, 0)),
    };

    [Fact]
    public void NichtsEingefuehrt_PrueftDieErsteDosis()
    {
        Assert.Equal([0, 1, 2], PositionPlayService.DailyTestPortion(3, poolSize: 10, [], Today));
        // The pool bounds the dose.
        Assert.Equal([0, 1], PositionPlayService.DailyTestPortion(3, poolSize: 2, [], Today));
    }

    [Fact]
    public void OffenerRestDerDosis_KommtKaltDazu_InBuchreihenfolge()
    {
        // One of three introduced today, an older one due: the test adds the two contents the dose still has room for.
        var introducedToday = new PositionItemProgress { ItemIndex = 0, IntroducedAt = Today, DueOn = Today };
        var due = Known(2, box: 2, dueOn: Today);

        Assert.Equal([0, 2, 1, 3], PositionPlayService.DailyTestPortion(3, poolSize: 10, [introducedToday, due], Today));
    }

    [Fact]
    public void VolleDosisHeute_BringtNichtsKaltesDazu()
    {
        PositionItemProgress[] today = [
            new() { ItemIndex = 0, IntroducedAt = Today, DueOn = Today },
            new() { ItemIndex = 1, IntroducedAt = Today, DueOn = Today },
        ];
        Assert.Equal([0, 1], PositionPlayService.DailyTestPortion(2, poolSize: 10, today, Today));
    }

    [Fact]
    public void Tagesportion_IstHeuteEingefuehrt_HeuteWiederholt_OderFaellig()
    {
        var introducedToday = new PositionItemProgress { ItemIndex = 0, IntroducedAt = Today, DueOn = Today.AddDays(2) };
        var reviewedToday = Known(1, box: 3, dueOn: Today.AddDays(4), reviewedDaysAgo: 0);
        var dueToday = Known(2, box: 2, dueOn: Today);
        var overdue = Known(3, box: 2, dueOn: Today.AddDays(-1));
        // The weakest of all, so the "nothing due" fallback would pick it first - the portion must not.
        var notToday = Known(4, box: 1, dueOn: Today.AddDays(7));
        var outsidePool = Known(9, box: 1, dueOn: Today);

        var portion = PositionPlayService.DailyTestPortion(2, poolSize: 5,
            [introducedToday, reviewedToday, dueToday, overdue, notToday, outsidePool], Today);

        Assert.Equal([0, 1, 2, 3], portion.Order());
    }

    [Fact]
    public void NichtsFaellig_PrueftDieSchwaechsten_HoechstensZweimalDieDosis()
    {
        var later = Today.AddDays(5);
        PositionItemProgress[] all = [Known(0, 5, later), Known(1, 2, later), Known(2, 4, later), Known(3, 3, later), Known(4, 2, later)];

        // Lowest box first, the index breaking the tie; two times the dose of 1 is two contents.
        // The pool is through (five of five introduced), so no new content is left to add.
        Assert.Equal([1, 4], PositionPlayService.DailyTestPortion(1, poolSize: 5, all, Today));
    }
}
