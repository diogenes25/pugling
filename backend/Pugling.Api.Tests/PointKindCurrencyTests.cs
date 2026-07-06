using Pugling.Api.Models;
using Pugling.Api.Services;

namespace Pugling.Api.Tests;

/// <summary>
/// Sichert die Währungs-Zuordnung ab: jeder <see cref="PointKind"/> muss genau einer <see cref="Currency"/>
/// zugeordnet sein (kein stiller Verlust aus dem Saldo, wenn ein neuer Kind hinzukommt), und die
/// Coin-/Gem-Mengen dürfen sich nicht überlappen.
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
    [InlineData(PointKind.Minutes, Currency.Coins)]
    [InlineData(PointKind.Test, Currency.Coins)]
    [InlineData(PointKind.DayComplete, Currency.Coins)]
    [InlineData(PointKind.Goal, Currency.Coins)]
    [InlineData(PointKind.Manual, Currency.Coins)]
    [InlineData(PointKind.Reward, Currency.Coins)]
    [InlineData(PointKind.ShopCoins, Currency.Coins)]
    [InlineData(PointKind.Combo, Currency.Gems)]
    [InlineData(PointKind.Speed, Currency.Gems)]
    [InlineData(PointKind.Duration, Currency.Gems)]
    [InlineData(PointKind.Mission, Currency.Gems)]
    [InlineData(PointKind.Achievement, Currency.Gems)]
    [InlineData(PointKind.SkinPurchase, Currency.Gems)]
    [InlineData(PointKind.ShopGems, Currency.Gems)]
    public void Zuordnung_EntsprichtDerFachlichenTrennung(PointKind kind, Currency expected) =>
        Assert.Equal(expected, PointKindCurrency.Of(kind));
}
