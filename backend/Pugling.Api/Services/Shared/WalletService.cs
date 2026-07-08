using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// Zentrale Lesequelle für den Kontostand eines Kindes je Währung. Der Saldo ist keine Spalte, sondern
/// stets die Summe der Punkte-Buchungen mit den zur Währung gehörenden <see cref="Models.PointKind"/>s
/// (siehe <see cref="PointKindCurrency"/>). Bündelt die vorher an mehreren Stellen duplizierte
/// <c>Where(...).SumAsync(...)</c>-Query an einer Stelle.
/// </summary>
public class WalletService(PuglingDbContext db)
{
    /// <summary>Kontostand beider Währungen eines Kindes.</summary>
    public record Balances(int Coins, int Gems);

    /// <summary>Aktueller Münz-Kontostand (für reale Angebote).</summary>
    public Task<int> CoinsAsync(int childId, CancellationToken ct = default) =>
        SumAsync(childId, PointKindCurrency.CoinKinds, ct);

    /// <summary>Aktueller Gem-Kontostand (für Skins/Spielfeatures).</summary>
    public Task<int> GemsAsync(int childId, CancellationToken ct = default) =>
        SumAsync(childId, PointKindCurrency.GemKinds, ct);

    /// <summary>Beide Kontostände in einem Rundtrip.</summary>
    public async Task<Balances> BalancesAsync(int childId, CancellationToken ct = default)
    {
        var grouped = await db.ChildPoints
            .Where(p => p.ChildId == childId)
            .GroupBy(p => PointKindCurrency.CoinKinds.Contains(p.Kind))
            .Select(g => new { IsCoin = g.Key, Sum = g.Sum(p => p.Amount) })
            .ToListAsync(ct);

        var coins = grouped.Where(g => g.IsCoin).Sum(g => g.Sum);
        var gems = grouped.Where(g => !g.IsCoin).Sum(g => g.Sum);
        return new Balances(coins, gems);
    }

    private async Task<int> SumAsync(int childId, PointKind[] kinds, CancellationToken ct) =>
        await db.ChildPoints
            .Where(p => p.ChildId == childId && kinds.Contains(p.Kind))
            .SumAsync(p => (int?)p.Amount, ct) ?? 0;
}
