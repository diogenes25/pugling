namespace Pugling.Api.Services.Shared;

// Currency lives in the contract project (Pugling.Contracts).

/// <summary>
/// Maps each <see cref="PointKind"/> to exactly one <see cref="Currency"/>. Diligence for learning
/// (base, reached goals) as well as manual adult ledger entries and family-shop purchases run on coins;
/// all motivational bonuses (combo/speed/missions/awards) and skin purchases run on gems.
/// <para>
/// The currency is deliberately <b>derived from the kind</b> rather than stored as its own column:
/// that way the existing ledger stays unchanged (no migration/no backfill), and the balance per
/// currency is the sum of the ledger entries with the matching kind. The mapping is exhaustive – an
/// unmapped kind throws (see test), so that a new kind never silently drops out of the balance.
/// </para>
/// </summary>
public static class PointKindCurrency
{
    /// <summary>The currency a ledger entry of the given <paramref name="kind"/> counts toward.</summary>
    public static Currency Of(PointKind kind) => kind switch
    {
        PointKind.Base or PointKind.Goal or PointKind.Manual or PointKind.ShopCoins
            or PointKind.GoalPenalty or PointKind.ObjectiveCoins => Currency.Coins,
        PointKind.Combo or PointKind.Speed or PointKind.Mission
            or PointKind.Achievement or PointKind.SkinPurchase or PointKind.ShopGems
            or PointKind.ManualGems or PointKind.ObjectiveGems => Currency.Gems,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "PointKind ohne Währungs-Zuordnung"),
    };

    /// <summary>All ledger kinds that count toward the coin account (for balance queries).</summary>
    public static readonly PointKind[] CoinKinds =
        [.. Enum.GetValues<PointKind>().Where(k => Of(k) == Currency.Coins)];

    /// <summary>All ledger kinds that count toward the gem account (for balance queries).</summary>
    public static readonly PointKind[] GemKinds =
        [.. Enum.GetValues<PointKind>().Where(k => Of(k) == Currency.Gems)];
}
