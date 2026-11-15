using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// Central read source for a child's account balance per currency. The balance is not a column but
/// always the sum of the point ledger entries with the <see cref="PointKind"/>s belonging to the
/// currency (see <see cref="PointKindCurrency"/>). Bundles the <c>Where(...).SumAsync(...)</c> query
/// that used to be duplicated across several places into one location.
/// </summary>
public class WalletService(PuglingDbContext db)
{
    /// <summary>Account balance of both currencies of a child.</summary>
    public record Balances(int Coins, int Gems);

    /// <summary>Current gem balance (for skins/game features).</summary>
    public Task<int> GemsAsync(int childId, CancellationToken ct = default) =>
        SumAsync(childId, PointKindCurrency.GemKinds, ct);

    /// <summary>Both balances in a single round trip.</summary>
    public async Task<Balances> BalancesAsync(int childId, CancellationToken ct = default)
    {
        var grouped = await db.ChildPointsEntries
            .Where(p => p.ChildId == childId)
            .GroupBy(p => PointKindCurrency.CoinKinds.Contains(p.Kind))
            .Select(g => new { IsCoin = g.Key, Sum = g.Sum(p => p.Amount) })
            .ToListAsync(ct);

        var coins = grouped.Where(g => g.IsCoin).Sum(g => g.Sum);
        var gems = grouped.Where(g => !g.IsCoin).Sum(g => g.Sum);
        return new Balances(coins, gems);
    }

    private async Task<int> SumAsync(int childId, PointKind[] kinds, CancellationToken ct) =>
        await db.ChildPointsEntries
            .Where(p => p.ChildId == childId && kinds.Contains(p.Kind))
            .SumAsync(p => (int?)p.Amount, ct) ?? 0;

    /// <summary>
    /// Coin kinds that count as "earned" in the week comparison (B-253): every coin kind except those that move coins
    /// without learning. The comparison only sums positive entries, but each excluded kind can also be booked
    /// positive: <see cref="PointKind.Manual"/> is a gift, <see cref="PointKind.ShopCoins"/> a refund of a cancelled
    /// purchase and <see cref="PointKind.GoalPenalty"/> a fine (a positive one would be a correction) - none of them
    /// says anything about this week's learning. Gems (combo, speed, missions) stay out: one number, one currency.
    /// </summary>
    private static readonly PointKind[] EarnedCoinKinds =
        [.. PointKindCurrency.CoinKinds.Except([PointKind.Manual, PointKind.ShopCoins, PointKind.GoalPenalty])];

    /// <summary>
    /// The child's earned coins this week against its own previous week (B-253, user decision G-6: no siblings, no
    /// ranks). The week runs Monday to Sunday on the UTC calendar, like weekly missions and weekly duties. The
    /// previous week is cut at the same moment (<paramref name="now"/> minus seven days), so a Monday morning is not
    /// measured against a full week; the whole previous week rides along beside it. Summed in the database.
    /// </summary>
    public async Task<WeekCoinsResponse> WeekCoinsAsync(int childId, DateTime now, CancellationToken ct)
    {
        var monday = GamificationService.WeekMonday(DateOnly.FromDateTime(now));
        var weekStart = monday.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var lastWeekStart = weekStart.AddDays(-7);
        var lastWeekNow = now.AddDays(-7);
        var earned = db.ChildPointsEntries.AsNoTracking()
            .Where(p => p.ChildId == childId && p.Amount > 0 && EarnedCoinKinds.Contains(p.Kind));

        // Upper bound `now` on both "so far" sums: an entry stamped after the server's clock is not this week's yet.
        var thisWeek = await earned.Where(p => p.CreatedAt >= weekStart && p.CreatedAt <= now)
            .SumAsync(p => (int?)p.Amount, ct) ?? 0;
        var lastWeekSoFar = await earned.Where(p => p.CreatedAt >= lastWeekStart && p.CreatedAt <= lastWeekNow)
            .SumAsync(p => (int?)p.Amount, ct) ?? 0;
        var lastWeekTotal = await earned.Where(p => p.CreatedAt >= lastWeekStart && p.CreatedAt < weekStart)
            .SumAsync(p => (int?)p.Amount, ct) ?? 0;
        return new WeekCoinsResponse(monday, thisWeek, lastWeekSoFar, lastWeekTotal);
    }
}
