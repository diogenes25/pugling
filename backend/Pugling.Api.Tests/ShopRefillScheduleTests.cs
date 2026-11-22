namespace Pugling.Api.Tests;

/// <summary>
/// B-645: the refill calendar as pure functions, at its edges (week and month change in UTC, a stock never refilled, a
/// child linked after the refill instant). Each case names the change that turns it red.
/// </summary>
public class ShopRefillScheduleTests
{
    private static DateTime Utc(int y, int m, int d, int h = 0, int min = 0) => new(y, m, d, h, min, 0, DateTimeKind.Utc);

    // 2026-11-16 is a Monday.
    private const DayOfWeek Monday = DayOfWeek.Monday;

    /// <summary>
    /// AK 2, weekly: Thursday → the Wednesday after, nobody looked on Monday - one refill is still owed. Red under the old
    /// rule "due only if today is the weekday" (ShopService before B-645), which answers 0 here.
    /// </summary>
    [Fact]
    public void Woechentlich_Montag_Verpasst_Wird_Nachgeholt() =>
        Assert.Equal(1, ShopRefillSchedule.DueRefills(ShopRefillKind.Weekly, Monday, null, Utc(2026, 11, 12, 18), Utc(2026, 11, 18, 9), TimeZoneInfo.Utc));

    /// <summary>Two Mondays skipped count twice; red when the count collapses into "due yes/no".</summary>
    [Fact]
    public void Woechentlich_Zwei_Montage_Verpasst_Zaehlt_Zwei() =>
        Assert.Equal(2, ShopRefillSchedule.DueRefills(ShopRefillKind.Weekly, Monday, null, Utc(2026, 11, 12), Utc(2026, 11, 24), TimeZoneInfo.Utc));

    /// <summary>Refilled on Monday morning, read Monday evening: nothing due. Red when the boundary is "&gt;=" on the day.</summary>
    [Fact]
    public void Woechentlich_Am_Selben_Montag_Nicht_Nochmal() =>
        Assert.Equal(0, ShopRefillSchedule.DueRefills(ShopRefillKind.Weekly, Monday, null, Utc(2026, 11, 16, 7), Utc(2026, 11, 16, 20), TimeZoneInfo.Utc));

    /// <summary>AK 5: the 31st is not a refill day, the 1st is (both in UTC). Red when monthly counts days instead of months.</summary>
    [Fact]
    public void Monatlich_Nur_Am_Ersten()
    {
        Assert.Equal(0, ShopRefillSchedule.DueRefills(ShopRefillKind.Monthly, null, null, Utc(2026, 10, 15), Utc(2026, 10, 31, 23, 59), TimeZoneInfo.Utc));
        Assert.Equal(1, ShopRefillSchedule.DueRefills(ShopRefillKind.Monthly, null, null, Utc(2026, 10, 15), Utc(2026, 11, 1, 0, 1), TimeZoneInfo.Utc));
    }

    /// <summary>AK 3, two 1sts skipped (31 Oct → 2 Dec) count twice; across the year change too.</summary>
    [Fact]
    public void Monatlich_Zaehlt_Verpasste_Erste_Auch_Ueber_Den_Jahreswechsel()
    {
        Assert.Equal(2, ShopRefillSchedule.DueRefills(ShopRefillKind.Monthly, null, null, Utc(2026, 10, 31), Utc(2026, 12, 2), TimeZoneInfo.Utc));
        Assert.Equal(1, ShopRefillSchedule.DueRefills(ShopRefillKind.Monthly, null, null, Utc(2026, 12, 20), Utc(2027, 1, 1), TimeZoneInfo.Utc));
    }

    /// <summary>Twice a day: midnight and noon. Red when the noon instant is dropped.</summary>
    [Fact]
    public void Zweimal_Taeglich_Zaehlt_Mittag_Und_Mitternacht()
    {
        Assert.Equal(1, ShopRefillSchedule.DueRefills(ShopRefillKind.TwiceDaily, null, null, Utc(2026, 11, 18, 8), Utc(2026, 11, 18, 13), TimeZoneInfo.Utc));
        Assert.Equal(3, ShopRefillSchedule.DueRefills(ShopRefillKind.TwiceDaily, null, null, Utc(2026, 11, 18, 8), Utc(2026, 11, 19, 13), TimeZoneInfo.Utc));
    }

    /// <summary>A stock never refilled owes one refill; Once only from its instant and only once.</summary>
    [Fact]
    public void Nie_Aufgefuellt_Und_Einmalig()
    {
        Assert.Equal(1, ShopRefillSchedule.DueRefills(ShopRefillKind.Daily, null, null, null, Utc(2026, 11, 18), TimeZoneInfo.Utc));
        Assert.Equal(0, ShopRefillSchedule.DueRefills(ShopRefillKind.Once, null, Utc(2026, 11, 20), null, Utc(2026, 11, 19), TimeZoneInfo.Utc));
        Assert.Equal(1, ShopRefillSchedule.DueRefills(ShopRefillKind.Once, null, Utc(2026, 11, 20), null, Utc(2026, 11, 21), TimeZoneInfo.Utc));
        Assert.Equal(0, ShopRefillSchedule.DueRefills(ShopRefillKind.Once, null, Utc(2026, 11, 20), Utc(2026, 11, 21), Utc(2026, 11, 30), TimeZoneInfo.Utc));
        Assert.Equal(0, ShopRefillSchedule.DueRefills(ShopRefillKind.None, null, null, null, Utc(2026, 11, 30), TimeZoneInfo.Utc));
    }

    /// <summary>
    /// AK 3 / decision 4: +4 per refill up to 8. 2 → 6 (the old rule "raise to MaxStock" says 8), 6 → 8 (capped; red
    /// without the cap), 0 with two refills → 8, with one → 4. A refill never lowers a manual top-up above the cap.
    /// </summary>
    [Fact]
    public void Ansparen_Bis_Zur_Obergrenze()
    {
        Assert.Equal(6, ShopRefillSchedule.Refill(2, 8, 4, 1));
        Assert.Equal(8, ShopRefillSchedule.Refill(6, 8, 4, 1));
        Assert.Equal(8, ShopRefillSchedule.Refill(0, 8, 4, 2));
        Assert.Equal(4, ShopRefillSchedule.Refill(0, 8, 4, 1));
        Assert.Equal(10, ShopRefillSchedule.Refill(10, 8, 4, 1));
        Assert.Equal(3, ShopRefillSchedule.Refill(3, 8, 4, 0));
    }

    /// <summary>The next instant the son sees ("wieder da am …"); null when nothing refills any more.</summary>
    [Fact]
    public void Naechster_Termin()
    {
        Assert.Equal(Utc(2026, 12, 1), ShopRefillSchedule.NextRefillAtUtc(ShopRefillKind.Monthly, null, null, Utc(2026, 11, 1), Utc(2026, 11, 19, 10), TimeZoneInfo.Utc));
        Assert.Equal(Utc(2026, 11, 23), ShopRefillSchedule.NextRefillAtUtc(ShopRefillKind.Weekly, Monday, null, Utc(2026, 11, 16), Utc(2026, 11, 16, 10), TimeZoneInfo.Utc));
        Assert.Equal(Utc(2026, 11, 23), ShopRefillSchedule.NextRefillAtUtc(ShopRefillKind.Weekly, Monday, null, Utc(2026, 11, 16), Utc(2026, 11, 19, 10), TimeZoneInfo.Utc));
        Assert.Equal(Utc(2026, 11, 19, 12), ShopRefillSchedule.NextRefillAtUtc(ShopRefillKind.TwiceDaily, null, null, null, Utc(2026, 11, 19, 10), TimeZoneInfo.Utc));
        Assert.Equal(Utc(2026, 11, 20), ShopRefillSchedule.NextRefillAtUtc(ShopRefillKind.Daily, null, null, null, Utc(2026, 11, 19, 10), TimeZoneInfo.Utc));
        Assert.Null(ShopRefillSchedule.NextRefillAtUtc(ShopRefillKind.None, null, null, null, Utc(2026, 11, 19), TimeZoneInfo.Utc));
        Assert.Null(ShopRefillSchedule.NextRefillAtUtc(ShopRefillKind.Once, null, Utc(2026, 11, 1), Utc(2026, 11, 1), Utc(2026, 11, 19), TimeZoneInfo.Utc));
    }

    /// <summary>
    /// Decision 11: own purchases of the current period count against the start; a Once listing before its instant
    /// starts empty and not refilled. The period of a weekly listing begins on its last weekday.
    /// </summary>
    [Fact]
    public void Startbestand_Und_Periodenbeginn()
    {
        Assert.Equal((1, (DateTime?)Utc(2026, 11, 19)), ShopRefillSchedule.StartStock(ShopRefillKind.Daily, null, 3, 2, Utc(2026, 11, 19)));
        Assert.Equal((0, (DateTime?)Utc(2026, 11, 19)), ShopRefillSchedule.StartStock(ShopRefillKind.Daily, null, 3, 5, Utc(2026, 11, 19)));
        Assert.Equal((0, (DateTime?)null), ShopRefillSchedule.StartStock(ShopRefillKind.Once, Utc(2026, 11, 21), 2, 0, Utc(2026, 11, 19)));
        Assert.Equal((4, (DateTime?)null), ShopRefillSchedule.StartStock(ShopRefillKind.None, null, 4, 0, Utc(2026, 11, 19)));
        Assert.Equal(Utc(2026, 11, 16), ShopRefillSchedule.PeriodStartUtc(ShopRefillKind.Weekly, Monday, null, Utc(2026, 1, 1), Utc(2026, 11, 19, 10), TimeZoneInfo.Utc));
        Assert.Equal(Utc(2026, 11, 1), ShopRefillSchedule.PeriodStartUtc(ShopRefillKind.Monthly, null, null, Utc(2026, 1, 1), Utc(2026, 11, 19, 10), TimeZoneInfo.Utc));
    }
}
