using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Pugling.Api.Tests;

/// <summary>
/// Phase 3: a student has multiple supervisors. They earn ONE shared wallet and buy from the
/// family shops of both supervisors; but only the respective issuing supervisor may cancel/redeem
/// (issuer-bound snapshot on the <c>ShopPurchase</c>).
/// </summary>
public class MultiSupervisorTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly WebApplicationFactory<Program> _factory = factory;

    private static async Task BuyAsync(HttpClient child, int listingId)
    {
        var res = await child.PostAsJsonAsync($"/api/v1/student/me/shop/listings/{listingId}/purchase", new { });
        res.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ZweiSupervisor_GemeinsamesWallet_AberEinloesungAusstellergebunden()
    {
        // Supervisor A = the seeded father (id 1), student = the seeded son (id 1, 50 coins starting balance).
        var supA = await TestApi.FatherAsync(_factory);

        // Register supervisor B (anonymously) and log in.
        var reg = await _factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = "Mama", email = (string?)null, pin = "2222" });
        var supBId = await TestApi.IdAsync(reg);
        var supB = await TestApi.FatherAsync(_factory, supBId, "2222");

        // A makes B a co-supervisor of student 1.
        (await supA.PostAsJsonAsync("/api/v1/supervisor/children/1/supervisors",
            new { supervisorId = supBId, relation = "Mother" })).EnsureSuccessStatusCode();

        // Both create one shop listing each for the same student (the article number is unique per adult).
        var listingA = await TestApi.CreateShopListingAsync(supA, "TEST-1", coinPrice: 10, unitsPerPurchase: 1, stock: 5, articleTitle: "Papas Artikel");
        var listingB = await TestApi.CreateShopListingAsync(supB, "TEST-1", coinPrice: 10, unitsPerPurchase: 1, stock: 5, articleTitle: "Mamas Artikel");

        // The student buys from BOTH shops - one shared wallet (50 → 30).
        var child = await TestApi.ChildAsync(_factory);
        await BuyAsync(child, listingA);
        await BuyAsync(child, listingB);
        var wallet = await (await child.GetAsync("/api/v1/student/me/points")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(30, wallet.GetProperty("coins").GetInt32());

        // Every supervisor sees ONLY their own purchase.
        var purchasesA = await (await supA.GetAsync("/api/v1/supervisor/children/1/shop/purchases"))
            .Content.ReadFromJsonAsync<JsonElement>();
        var purchasesB = await (await supB.GetAsync("/api/v1/supervisor/children/1/shop/purchases"))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Single(purchasesA.EnumerateArray());
        Assert.Single(purchasesB.EnumerateArray());
        var purchaseAId = purchasesA.EnumerateArray().First().GetProperty("id").GetInt32();
        var purchaseBId = purchasesB.EnumerateArray().First().GetProperty("id").GetInt32();
        Assert.NotEqual(purchaseAId, purchaseBId);

        // A must NOT cancel B's purchase (issued by someone else → 404), their own one yes.
        Assert.Equal(HttpStatusCode.NotFound,
            (await supA.PostAsJsonAsync($"/api/v1/supervisor/children/1/shop/purchases/{purchaseBId}/cancel", new { })).StatusCode);
        (await supA.PostAsJsonAsync($"/api/v1/supervisor/children/1/shop/purchases/{purchaseAId}/cancel", new { }))
            .EnsureSuccessStatusCode();
        // B cancels their own.
        (await supB.PostAsJsonAsync($"/api/v1/supervisor/children/1/shop/purchases/{purchaseBId}/cancel", new { }))
            .EnsureSuccessStatusCode();
    }

    // ─────────────────────────────────── Reading and removing supervision (a C3 coverage gap)

    [Fact]
    public async Task Betreuer_Liste_Und_Entfernen_Der_Letzte_Bleibt()
    {
        var supA = await TestApi.FatherAsync(_factory);
        var childId = await TestApi.IdAsync(await supA.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = "Betreutes Kind", pin = "6201" }));
        var url = $"/api/v1/supervisor/children/{childId}/supervisors";

        // With only one supervisor, that one **cannot** be removed - a student without any supervisor would be
        // exactly the orphan nobody can see or manage any more.
        var letzter = await supA.DeleteAsync($"{url}/1");
        Assert.Equal(HttpStatusCode.BadRequest, letzter.StatusCode);
        Assert.Equal("validation_error",
            (await letzter.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        var reg = await _factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = "Oma", pin = "6202" });
        var omaId = await TestApi.IdAsync(reg);
        (await supA.PostAsJsonAsync(url, new { supervisorId = omaId, relation = "Grandma" })).EnsureSuccessStatusCode();

        var liste = await (await supA.GetAsync(url)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, liste.GetArrayLength());
        Assert.Contains(omaId, liste.EnumerateArray().Select(l => l.GetProperty("supervisorId").GetInt32()));

        // Now there is a second one - the grandmother may go.
        Assert.Equal(HttpStatusCode.NoContent, (await supA.DeleteAsync($"{url}/{omaId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await supA.DeleteAsync($"{url}/{omaId}")).StatusCode);
        Assert.Equal(1, (await (await supA.GetAsync(url)).Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength());
    }

    // ─────────────────────────────────── B-98: the idempotent repeat answers 200 with the STORED link

    [Fact]
    public async Task ErneutesHinzufuegen_Meldet200_MitDerGespeichertenBeziehungNichtDerNeuen()
    {
        var supA = await TestApi.FatherAsync(_factory);
        var childId = await TestApi.IdAsync(await supA.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = "B-98-Kind", pin = "6301" }));
        var url = $"/api/v1/supervisor/children/{childId}/supervisors";

        var reg = await _factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = "Onkel", pin = "6302" });
        var onkelId = await TestApi.IdAsync(reg);

        var first = await supA.PostAsJsonAsync(url, new { supervisorId = onkelId, relation = "Guardian" });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        // A second POST with a DIFFERENT relation must not overwrite it and must not claim a second insert:
        // 200, and the ORIGINALLY stored relation, not the caller's new one.
        var second = await supA.PostAsJsonAsync(url, new { supervisorId = onkelId, relation = "Other" });
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var body = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Guardian", body.GetProperty("relation").GetString());

        var firstBody = await first.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(firstBody.GetProperty("createdAt").GetDateTime(), body.GetProperty("createdAt").GetDateTime());
    }
}
