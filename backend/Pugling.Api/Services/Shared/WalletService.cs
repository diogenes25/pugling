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
}
