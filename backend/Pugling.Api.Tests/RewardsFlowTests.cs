using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Deckt den Prämien-/Einlöse-Ablauf ab: Vater legt eine reale Prämie an, der Sohn fragt sie an
/// (ohne Abbuchung), der Vater genehmigt (bucht ab) oder lehnt ab. Nutzt frische Kinder für
/// deterministische Salden trotz geteilter Test-DB.
/// </summary>
public class RewardsFlowTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private async Task<(int childId, HttpClient child)> FreshChildAsync(HttpClient father, string pin)
    {
        var childId = await TestApi.IdAsync(
            await father.PostAsJsonAsync("/api/v1/children", new { name = "Prämien-Kind", pin }));
        return (childId, await TestApi.ChildAsync(factory, childId, pin));
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage res)
    {
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Anfrage_BuchtNichtsAb_ErstGenehmigung_BuchtAb()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, child) = await FreshChildAsync(father, "9001");
        var rewardId = (await JsonAsync(await father.PostAsJsonAsync(
            $"/api/v1/children/{childId}/rewards", new { title = "30 Min Fernsehen", cost = 200 })))
            .GetProperty("id").GetInt32();

        // Guthaben schaffen (250)
        (await father.PostAsJsonAsync($"/api/v1/children/{childId}/points",
            new { amount = 250, reason = "Test-Guthaben" })).EnsureSuccessStatusCode();

        // Sohn fragt an -> noch keine Abbuchung
        var view = await JsonAsync(await child.PostAsJsonAsync($"/api/v1/me/rewards/{rewardId}/redeem", new { }));
        Assert.Equal(250, view.GetProperty("balance").GetInt32());
        var redemptionId = view.GetProperty("redemptions").EnumerateArray().First().GetProperty("id").GetInt32();
        Assert.Equal("Requested", view.GetProperty("redemptions").EnumerateArray().First().GetProperty("status").GetString());

        // Zweite offene Anfrage derselben Prämie -> 409
        Assert.Equal(HttpStatusCode.Conflict,
            (await child.PostAsJsonAsync($"/api/v1/me/rewards/{rewardId}/redeem", new { })).StatusCode);

        // Vater genehmigt -> jetzt abgebucht
        var approved = await JsonAsync(await father.PostAsJsonAsync(
            $"/api/v1/children/{childId}/rewards/redemptions/{redemptionId}/approve", new { }));
        Assert.Equal("Approved", approved.GetProperty("status").GetString());

        var wallet = await JsonAsync(await child.GetAsync("/api/v1/me/points"));
        Assert.Equal(50, wallet.GetProperty("balance").GetInt32()); // 250 - 200
        var spend = wallet.GetProperty("entries").EnumerateArray()
            .First(e => e.GetProperty("kind").GetString() == "Reward");
        Assert.Equal(-200, spend.GetProperty("amount").GetInt32());

        // Erneutes Genehmigen -> 409 (schon entschieden)
        Assert.Equal(HttpStatusCode.Conflict,
            (await father.PostAsJsonAsync($"/api/v1/children/{childId}/rewards/redemptions/{redemptionId}/approve", new { })).StatusCode);
    }

    [Fact]
    public async Task Genehmigung_OhneDeckung_400_KeineAbbuchung()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, child) = await FreshChildAsync(father, "9002");
        var rewardId = (await JsonAsync(await father.PostAsJsonAsync(
            $"/api/v1/children/{childId}/rewards", new { title = "Kinoabend", cost = 1500 })))
            .GetProperty("id").GetInt32();

        var view = await JsonAsync(await child.PostAsJsonAsync($"/api/v1/me/rewards/{rewardId}/redeem", new { }));
        var redemptionId = view.GetProperty("redemptions").EnumerateArray().First().GetProperty("id").GetInt32();

        var res = await father.PostAsJsonAsync($"/api/v1/children/{childId}/rewards/redemptions/{redemptionId}/approve", new { });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        var wallet = await JsonAsync(await child.GetAsync("/api/v1/me/points"));
        Assert.Equal(0, wallet.GetProperty("balance").GetInt32());
    }

    [Fact]
    public async Task Ablehnung_LaesstGuthabenUnberuehrt()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, child) = await FreshChildAsync(father, "9003");
        (await father.PostAsJsonAsync($"/api/v1/children/{childId}/points", new { amount = 500, reason = "x" })).EnsureSuccessStatusCode();
        var rewardId = (await JsonAsync(await father.PostAsJsonAsync(
            $"/api/v1/children/{childId}/rewards", new { title = "1 Std Zocken", cost = 400 })))
            .GetProperty("id").GetInt32();

        var view = await JsonAsync(await child.PostAsJsonAsync($"/api/v1/me/rewards/{rewardId}/redeem", new { }));
        var redemptionId = view.GetProperty("redemptions").EnumerateArray().First().GetProperty("id").GetInt32();

        var rejected = await JsonAsync(await father.PostAsJsonAsync(
            $"/api/v1/children/{childId}/rewards/redemptions/{redemptionId}/reject", new { }));
        Assert.Equal("Rejected", rejected.GetProperty("status").GetString());

        var wallet = await JsonAsync(await child.GetAsync("/api/v1/me/points"));
        Assert.Equal(500, wallet.GetProperty("balance").GetInt32());
    }

    [Fact]
    public async Task Sohn_KannPraemieFuerFremdesKindNichtAnfragen_404()
    {
        var father = await TestApi.FatherAsync(factory);
        // Prämie gehört Kind A; Kind B versucht sie einzulösen.
        var (childAId, _) = await FreshChildAsync(father, "9004");
        var rewardId = (await JsonAsync(await father.PostAsJsonAsync(
            $"/api/v1/children/{childAId}/rewards", new { title = "Eis", cost = 100 })))
            .GetProperty("id").GetInt32();
        var (_, childB) = await FreshChildAsync(father, "9005");

        var res = await childB.PostAsJsonAsync($"/api/v1/me/rewards/{rewardId}/redeem", new { });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Vater_KannPraemieFuerFremdesKindNichtAnlegen_403()
    {
        var father = await TestApi.FatherAsync(factory);

        var res = await father.PostAsJsonAsync("/api/v1/children/999999/rewards", new { title = "X", cost = 100 });

        Assert.True(res.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound);
    }
}
