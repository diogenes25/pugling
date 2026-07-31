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
        // Supervisor A = geseedeter Papa (id 1), Student = geseedeter Sohn (id 1, 50 Münzen Startguthaben).
        var supA = await TestApi.FatherAsync(_factory);

        // Supervisor B neu registrieren (anonym) und einloggen.
        var reg = await _factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = "Mama", email = (string?)null, pin = "2222" });
        var supBId = await TestApi.IdAsync(reg);
        var supB = await TestApi.FatherAsync(_factory, supBId, "2222");

        // A macht B zum Ko-Supervisor des Studenten 1.
        (await supA.PostAsJsonAsync("/api/v1/supervisor/children/1/supervisors",
            new { supervisorId = supBId, relation = "Mother" })).EnsureSuccessStatusCode();

        // Beide legen je ein Shop-Angebot für denselben Studenten an (Artikelnummer ist je Vater eindeutig).
        var listingA = await TestApi.CreateShopListingAsync(supA, "TEST-1", coinPrice: 10, unitsPerPurchase: 1, stock: 5, articleTitle: "Papas Artikel");
        var listingB = await TestApi.CreateShopListingAsync(supB, "TEST-1", coinPrice: 10, unitsPerPurchase: 1, stock: 5, articleTitle: "Mamas Artikel");

        // Der Student kauft aus BEIDEN Shops – ein gemeinsames Wallet (50 → 30).
        var child = await TestApi.ChildAsync(_factory);
        await BuyAsync(child, listingA);
        await BuyAsync(child, listingB);
        var wallet = await (await child.GetAsync("/api/v1/student/me/points")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(30, wallet.GetProperty("coins").GetInt32());

        // Jeder Supervisor sieht NUR seinen eigenen Kauf.
        var purchasesA = await (await supA.GetAsync("/api/v1/supervisor/children/1/shop/purchases"))
            .Content.ReadFromJsonAsync<JsonElement>();
        var purchasesB = await (await supB.GetAsync("/api/v1/supervisor/children/1/shop/purchases"))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Single(purchasesA.EnumerateArray());
        Assert.Single(purchasesB.EnumerateArray());
        var purchaseAId = purchasesA.EnumerateArray().First().GetProperty("id").GetInt32();
        var purchaseBId = purchasesB.EnumerateArray().First().GetProperty("id").GetInt32();
        Assert.NotEqual(purchaseAId, purchaseBId);

        // A darf B's Kauf NICHT stornieren (fremd ausgestellt → 404), seinen eigenen schon.
        Assert.Equal(HttpStatusCode.NotFound,
            (await supA.PostAsJsonAsync($"/api/v1/supervisor/children/1/shop/purchases/{purchaseBId}/cancel", new { })).StatusCode);
        (await supA.PostAsJsonAsync($"/api/v1/supervisor/children/1/shop/purchases/{purchaseAId}/cancel", new { }))
            .EnsureSuccessStatusCode();
        // B storniert seinen eigenen.
        (await supB.PostAsJsonAsync($"/api/v1/supervisor/children/1/shop/purchases/{purchaseBId}/cancel", new { }))
            .EnsureSuccessStatusCode();
    }

    // ─────────────────────────────────── Betreuung lesen und lösen (C3-Abdeckungslücke)

    [Fact]
    public async Task Betreuer_Liste_Und_Entfernen_Der_Letzte_Bleibt()
    {
        var supA = await TestApi.FatherAsync(_factory);
        var childId = await TestApi.IdAsync(await supA.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = "Betreutes Kind", pin = "6201" }));
        var url = $"/api/v1/supervisor/children/{childId}/supervisors";

        // Mit nur einem Betreuer lässt sich dieser **nicht** entfernen – ein Student ohne jeden Supervisor
        // wäre genau die Waise, die niemand mehr sehen oder verwalten kann.
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

        // Jetzt gibt es einen zweiten – die Oma darf gehen.
        Assert.Equal(HttpStatusCode.NoContent, (await supA.DeleteAsync($"{url}/{omaId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await supA.DeleteAsync($"{url}/{omaId}")).StatusCode);
        Assert.Equal(1, (await (await supA.GetAsync(url)).Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength());
    }
}
