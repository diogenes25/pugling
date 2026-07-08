using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Pugling.Api.Tests;

/// <summary>
/// Phase 3: ein Student hat mehrere Supervisor. Er verdient EIN gemeinsames Wallet und kauft aus den
/// Shops/Angeboten beider Supervisor; einlösen darf aber nur der jeweils ausstellende Supervisor.
/// </summary>
public class MultiSupervisorTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly WebApplicationFactory<Program> _factory = factory;

    private static async Task<int> CreateRewardAsync(HttpClient sup, int childId, string title, int cost)
    {
        var res = await sup.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/rewards",
            new { title, cost, period = "OneOff", quantity = 5 });
        return await TestApi.IdAsync(res);
    }

    private static async Task BuyAsync(HttpClient child, int rewardId)
    {
        var res = await child.PostAsJsonAsync($"/api/v1/student/me/rewards/available/{rewardId}/purchase", new { });
        res.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ZweiSupervisor_GemeinsamesWallet_AbersEinloesungAusstellergebunden()
    {
        // Supervisor A = geseedeter Papa (id 1), Student = geseedeter Sohn (id 1, 50 Münzen Startguthaben).
        var supA = await TestApi.FatherAsync(_factory);

        // Supervisor B neu registrieren (anonym) und einloggen.
        var reg = await _factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/fathers",
            new { name = "Mama", email = (string?)null, pin = "2222" });
        var supBId = await TestApi.IdAsync(reg);
        var supB = await TestApi.FatherAsync(_factory, supBId, "2222");

        // A macht B zum Ko-Supervisor des Studenten 1.
        (await supA.PostAsJsonAsync("/api/v1/supervisor/children/1/supervisors",
            new { supervisorId = supBId, relation = "Mother" })).EnsureSuccessStatusCode();

        // Beide legen je ein Angebot für denselben Studenten an.
        var offerA = await CreateRewardAsync(supA, 1, "Papas Angebot", 10);
        var offerB = await CreateRewardAsync(supB, 1, "Mamas Angebot", 10);

        // Der Student kauft aus BEIDEN Shops – ein gemeinsames Wallet (50 → 30).
        var child = await TestApi.ChildAsync(_factory);
        await BuyAsync(child, offerA);
        await BuyAsync(child, offerB);
        var wallet = await (await child.GetAsync("/api/v1/student/me/points")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(30, wallet.GetProperty("coins").GetInt32());

        // Jeder Supervisor sieht NUR seinen eigenen Kauf.
        var redemptionsA = await (await supA.GetAsync("/api/v1/supervisor/children/1/rewards/redemptions"))
            .Content.ReadFromJsonAsync<JsonElement>();
        var redemptionsB = await (await supB.GetAsync("/api/v1/supervisor/children/1/rewards/redemptions"))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Single(redemptionsA.EnumerateArray());
        Assert.Single(redemptionsB.EnumerateArray());
        var redemptionAId = redemptionsA.EnumerateArray().First().GetProperty("id").GetInt32();
        var redemptionBId = redemptionsB.EnumerateArray().First().GetProperty("id").GetInt32();
        Assert.NotEqual(redemptionAId, redemptionBId);

        // A darf B's Kauf NICHT einlösen (fremd ausgestellt → 404), seinen eigenen schon.
        Assert.Equal(HttpStatusCode.NotFound,
            (await supA.PostAsJsonAsync($"/api/v1/supervisor/children/1/rewards/redemptions/{redemptionBId}/fulfill", new { })).StatusCode);
        (await supA.PostAsJsonAsync($"/api/v1/supervisor/children/1/rewards/redemptions/{redemptionAId}/fulfill", new { }))
            .EnsureSuccessStatusCode();
        // B löst seinen eigenen ein.
        (await supB.PostAsJsonAsync($"/api/v1/supervisor/children/1/rewards/redemptions/{redemptionBId}/fulfill", new { }))
            .EnsureSuccessStatusCode();
    }
}
