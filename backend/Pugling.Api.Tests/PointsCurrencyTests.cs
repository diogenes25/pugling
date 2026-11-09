using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// B-425: a ledger row says which currency it moved. The currency is a function of the kind (PointKindCurrency); the
/// contract has to carry it, or the history shows "+300" in a coin column for a gem gift. Both views - the supervisor's
/// account page and the child's own - read the same rule from the server, never a frontend copy of it.
/// </summary>
public class PointsCurrencyTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static async Task<(string coinReason, string gemReason)> BookBothAsync(HttpClient father)
    {
        var coinReason = TestApi.UniqueName("Münzen");
        var gemReason = TestApi.UniqueName("Gems");
        (await father.PostAsJsonAsync("/api/v1/supervisor/children/1/points", new { amount = 7, reason = coinReason, currency = "Coins" })).EnsureSuccessStatusCode();
        (await father.PostAsJsonAsync("/api/v1/supervisor/children/1/points", new { amount = 9, reason = gemReason, currency = "Gems" })).EnsureSuccessStatusCode();
        return (coinReason, gemReason);
    }

    private static string CurrencyOf(JsonElement entries, string reason) =>
        entries.EnumerateArray().Single(e => e.GetProperty("reason").GetString() == reason).GetProperty("currency").GetString()!;

    // AK 1, supervisor view. Red if the field is missing or always "Coins" (a gem gift would read as coins again).
    [Fact]
    public async Task Vaterliste_NenntDieWaehrungJeBuchung()
    {
        var father = await TestApi.AdultAsync(factory);
        var (coinReason, gemReason) = await BookBothAsync(father);

        var list = await father.GetFromJsonAsync<JsonElement>("/api/v1/supervisor/children/1/points");
        var entries = list.GetProperty("entries");

        Assert.Equal("Coins", CurrencyOf(entries, coinReason));
        Assert.Equal("Gems", CurrencyOf(entries, gemReason));
    }

    // AK 1, the created row: the POST answer is the same contract and must not guess either.
    [Fact]
    public async Task Buchen_AntwortetMitDerWaehrung()
    {
        var father = await TestApi.AdultAsync(factory);
        var gems = await father.PostAsJsonAsync("/api/v1/supervisor/children/1/points", new { amount = 3, reason = TestApi.UniqueName("Gem"), currency = "Gems" });
        var coins = await father.PostAsJsonAsync("/api/v1/supervisor/children/1/points", new { amount = 3, reason = TestApi.UniqueName("Coin"), currency = "Coins" });

        // Both directions (review finding): a constant answer - "Gems" just as much as "Coins" - turns one of the two red.
        Assert.Equal("Gems", (await gems.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("currency").GetString());
        Assert.Equal("Coins", (await coins.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("currency").GetString());
    }

    // AK 1, the child's view ("Mein Konto"): list and single entry. Red if the student DTO stays without currency.
    [Fact]
    public async Task Kindansicht_NenntDieWaehrungInListeUndEinzelbuchung()
    {
        var father = await TestApi.AdultAsync(factory);
        var (coinReason, gemReason) = await BookBothAsync(father);
        var child = await TestApi.ChildAsync(factory);

        var entries = await child.GetFromJsonAsync<JsonElement>("/api/v1/student/me/points/entries");
        Assert.Equal("Coins", CurrencyOf(entries, coinReason));
        Assert.Equal("Gems", CurrencyOf(entries, gemReason));

        int IdOf(string reason) => entries.EnumerateArray().Single(e => e.GetProperty("reason").GetString() == reason).GetProperty("id").GetInt32();
        var gemSingle = await child.GetFromJsonAsync<JsonElement>($"/api/v1/student/me/points/entries/{IdOf(gemReason)}");
        var coinSingle = await child.GetFromJsonAsync<JsonElement>($"/api/v1/student/me/points/entries/{IdOf(coinReason)}");
        Assert.Equal("Gems", gemSingle.GetProperty("currency").GetString());
        Assert.Equal("Coins", coinSingle.GetProperty("currency").GetString());
    }
}
