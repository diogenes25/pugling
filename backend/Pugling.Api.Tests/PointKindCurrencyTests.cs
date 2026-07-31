namespace Pugling.Api.Tests;

/// <summary>
/// Secures the currency mapping: every <see cref="PointKind"/> must be assigned to exactly one
/// <see cref="Currency"/> (no silent loss from the balance when a new kind is added), and the coin/gem
/// amounts must not overlap.
/// </summary>
public class PointKindCurrencyTests
{
    [Fact]
    public void JederPointKind_IstEinerWaehrungZugeordnet()
    {
        foreach (var kind in Enum.GetValues<PointKind>())
        {
            var currency = PointKindCurrency.Of(kind); // wirft, falls nicht gemappt
            Assert.True(currency is Currency.Coins or Currency.Gems);
        }
    }

    [Fact]
    public void CoinKinds_UndGemKinds_SindDisjunktUndVollstaendig()
    {
        var all = Enum.GetValues<PointKind>().ToHashSet();
        Assert.Empty(PointKindCurrency.CoinKinds.Intersect(PointKindCurrency.GemKinds));
        Assert.Equal(all, PointKindCurrency.CoinKinds.Concat(PointKindCurrency.GemKinds).ToHashSet());
    }

    [Theory]
    [InlineData(PointKind.Base, Currency.Coins)]
    [InlineData(PointKind.Goal, Currency.Coins)]
    [InlineData(PointKind.Manual, Currency.Coins)]
    [InlineData(PointKind.ShopCoins, Currency.Coins)]
    [InlineData(PointKind.GoalPenalty, Currency.Coins)]
    [InlineData(PointKind.Combo, Currency.Gems)]
    [InlineData(PointKind.Speed, Currency.Gems)]
    [InlineData(PointKind.Mission, Currency.Gems)]
    [InlineData(PointKind.Achievement, Currency.Gems)]
    [InlineData(PointKind.SkinPurchase, Currency.Gems)]
    [InlineData(PointKind.ShopGems, Currency.Gems)]
    [InlineData(PointKind.ManualGems, Currency.Gems)]
    [InlineData(PointKind.ObjectiveCoins, Currency.Coins)]
    [InlineData(PointKind.ObjectiveGems, Currency.Gems)]
    public void Zuordnung_EntsprichtDerFachlichenTrennung(PointKind kind, Currency expected) =>
        Assert.Equal(expected, PointKindCurrency.Of(kind));
}
