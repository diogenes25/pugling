namespace Pugling.Api.Services.Shared;

/// <summary>
/// The refill calendar of a shop listing as pure functions (B-645): how many refill instants passed between two moments,
/// when the next one comes, where the current period began, and what a child's stock starts at. Every path that refills
/// or creates a child's stock goes through here, so the list views, the purchase and the single view cannot disagree.
/// <para>
/// Refill instants are UTC like all day logic of the project: <c>Daily</c> at midnight, <c>TwiceDaily</c> at midnight
/// and noon, <c>Weekly</c> at midnight of the weekday, <c>Monthly</c> at midnight of the 1st. They are <b>counted</b>,
/// not tested for "today": a week nobody opened the shop is caught up on the next read instead of being lost.
/// </para>
/// </summary>
public static class ShopRefillSchedule
{
    /// <summary>Does the kind refill again and again on a calendar (not never, not once)?</summary>
    public static bool IsRecurring(ShopRefillKind kind) => kind is not (ShopRefillKind.None or ShopRefillKind.Once);

    /// <summary>
    /// Number of refill instants in <c>(lastUtc, nowUtc]</c>. A stock that was never refilled (<paramref name="lastUtc"/>
    /// <c>null</c>) owes exactly one refill - for <c>Once</c> only from <paramref name="onceAtUtc"/> on.
    /// </summary>
    public static int DueRefills(ShopRefillKind kind, DayOfWeek? weekday, DateTime? onceAtUtc, DateTime? lastUtc, DateTime nowUtc)
    {
        if (kind == ShopRefillKind.Once)
            return onceAtUtc is { } at && nowUtc >= at && lastUtc is null ? 1 : 0;
        if (kind == ShopRefillKind.None) return 0;
        if (lastUtc is not { } last) return 1;
        if (nowUtc <= last) return 0;

        return kind switch
        {
            ShopRefillKind.Daily => DayNumber(nowUtc) - DayNumber(last),
            ShopRefillKind.TwiceDaily => HalfDayNumber(nowUtc) - HalfDayNumber(last),
            ShopRefillKind.Weekly => weekday is { } w ? WeekdaysUpTo(nowUtc, w) - WeekdaysUpTo(last, w) : 0,
            ShopRefillKind.Monthly => MonthNumber(nowUtc) - MonthNumber(last),
            _ => 0,
        };
    }

    /// <summary>
    /// The next refill instant after <paramref name="nowUtc"/>; <c>null</c> when the listing never refills again
    /// (<c>None</c>, or <c>Once</c> after it fired).
    /// </summary>
    public static DateTime? NextRefillAtUtc(ShopRefillKind kind, DayOfWeek? weekday, DateTime? onceAtUtc, DateTime? lastUtc, DateTime nowUtc)
    {
        var today = Utc(nowUtc.Date);
        return kind switch
        {
            ShopRefillKind.Once => lastUtc is null ? onceAtUtc : null,
            ShopRefillKind.Daily => today.AddDays(1),
            ShopRefillKind.TwiceDaily => nowUtc.Hour < 12 ? today.AddHours(12) : today.AddDays(1),
            ShopRefillKind.Weekly when weekday is { } w => today.AddDays(DaysUntil(nowUtc.DayOfWeek, w) is var d && d == 0 ? 7 : d),
            ShopRefillKind.Monthly => Utc(new DateTime(nowUtc.Year, nowUtc.Month, 1)).AddMonths(1),
            _ => null,
        };
    }

    /// <summary>
    /// Start of the refill period <paramref name="nowUtc"/> lies in - the last refill instant at or before it. For
    /// <c>None</c> the listing's creation, for <c>Once</c> its instant. A child's purchases since then count against its
    /// start stock.
    /// </summary>
    public static DateTime PeriodStartUtc(ShopRefillKind kind, DayOfWeek? weekday, DateTime? onceAtUtc, DateTime createdAtUtc, DateTime nowUtc)
    {
        var today = Utc(nowUtc.Date);
        return kind switch
        {
            ShopRefillKind.Daily => today,
            ShopRefillKind.TwiceDaily => nowUtc.Hour < 12 ? today : today.AddHours(12),
            ShopRefillKind.Weekly when weekday is { } w => today.AddDays(-DaysUntil(w, nowUtc.DayOfWeek)),
            ShopRefillKind.Monthly => Utc(new DateTime(nowUtc.Year, nowUtc.Month, 1)),
            ShopRefillKind.Once when onceAtUtc is { } at => at,
            _ => createdAtUtc,
        };
    }

    /// <summary>
    /// The stock after <paramref name="dueRefills"/> refills of <paramref name="quantity"/> each, capped at
    /// <paramref name="maxStock"/>. A refill never lowers the stock: after the supervisor lowers the cap, a child above it
    /// keeps what it has until it buys below the new cap.
    /// </summary>
    public static int Refill(int currentStock, int maxStock, int quantity, int dueRefills)
    {
        if (dueRefills <= 0 || maxStock <= 0 || quantity <= 0) return currentStock;
        var raised = Math.Min(maxStock, currentStock + (long)dueRefills * quantity);
        return (int)Math.Max(currentStock, raised);
    }

    /// <summary>
    /// The start rule for a child that has no stock yet (B-645, decision 11): <c>MaxStock</c> minus the child's own,
    /// not cancelled purchases of the current period, never below 0, counted as refilled now. A <c>Once</c> listing before
    /// its instant starts empty and not yet refilled, so it is buyable for nobody until then.
    /// </summary>
    public static (int Stock, DateTime? LastRefilledAtUtc) StartStock(
        ShopRefillKind kind, DateTime? onceAtUtc, int maxStock, int ownPurchasesInPeriod, DateTime nowUtc)
    {
        if (kind == ShopRefillKind.Once && (onceAtUtc is not { } at || nowUtc < at)) return (0, null);
        var stock = Math.Max(0, maxStock - ownPurchasesInPeriod);
        return (stock, kind == ShopRefillKind.None ? null : nowUtc);
    }

    private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static int DayNumber(DateTime utc) => DateOnly.FromDateTime(utc).DayNumber;

    private static int HalfDayNumber(DateTime utc) => DayNumber(utc) * 2 + (utc.Hour >= 12 ? 1 : 0);

    private static int MonthNumber(DateTime utc) => utc.Year * 12 + utc.Month;

    // Count of days with weekday w in [day 0, utc.Date]. Day 0 (0001-01-01) is a Monday.
    private static int WeekdaysUpTo(DateTime utc, DayOfWeek w)
    {
        var offset = DaysUntil(DayOfWeek.Monday, w);
        return (DayNumber(utc) - offset + 7) / 7;
    }

    private static int DaysUntil(DayOfWeek from, DayOfWeek to) => ((int)to - (int)from + 7) % 7;
}
