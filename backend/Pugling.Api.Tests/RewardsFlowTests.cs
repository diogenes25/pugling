using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Deckt den Angebots-Kauf-/Erfüllungs-Ablauf ab: Vater legt ein Angebot an, der Sohn kauft es direkt
/// (Münzen werden sofort abgebucht), der Vater erfüllt oder storniert (Rückerstattung). Nutzt frische
/// Kinder für deterministische Salden trotz geteilter Test-DB.
/// </summary>
public class RewardsFlowTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private async Task<(int childId, HttpClient child)> FreshChildAsync(HttpClient father, string pin)
    {
        var childId = await TestApi.IdAsync(
            await father.PostAsJsonAsync("/api/v1/children", new { name = "Angebots-Kind", pin }));
        return (childId, await TestApi.ChildAsync(factory, childId, pin));
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage res)
    {
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<int> CreateOfferAsync(HttpClient father, int childId, object body) =>
        (await JsonAsync(await father.PostAsJsonAsync($"/api/v1/children/{childId}/rewards", body)))
            .GetProperty("id").GetInt32();

    [Fact]
    public async Task Kauf_BuchtSofortAb_Erfuellung_SchliesstAb()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, child) = await FreshChildAsync(father, "9001");
        var offerId = await CreateOfferAsync(father, childId,
            new { title = "30 Min Fernsehen", cost = 200, period = "Weekly", quantity = 3 });

        // Guthaben schaffen (250 Münzen)
        (await father.PostAsJsonAsync($"/api/v1/children/{childId}/points",
            new { amount = 250, reason = "Test-Guthaben" })).EnsureSuccessStatusCode();

        // Sohn kauft -> Münzen sofort weg, Status Purchased
        var view = await JsonAsync(await child.PostAsJsonAsync($"/api/v1/me/rewards/{offerId}/purchase", new { }));
        Assert.Equal(50, view.GetProperty("coins").GetInt32()); // 250 - 200
        var purchase = view.GetProperty("redemptions").EnumerateArray().First();
        Assert.Equal("Purchased", purchase.GetProperty("status").GetString());
        var redemptionId = purchase.GetProperty("id").GetInt32();

        // Abbuchung ist als negative Reward-Buchung im Wallet nachvollziehbar
        var wallet = await JsonAsync(await child.GetAsync("/api/v1/me/points"));
        Assert.Equal(50, wallet.GetProperty("coins").GetInt32());
        var spend = wallet.GetProperty("entries").EnumerateArray()
            .First(e => e.GetProperty("kind").GetString() == "Reward");
        Assert.Equal(-200, spend.GetProperty("amount").GetInt32());

        // Vater erfüllt -> Status Fulfilled, kein weiterer Münz-Effekt
        var fulfilled = await JsonAsync(await father.PostAsJsonAsync(
            $"/api/v1/children/{childId}/rewards/redemptions/{redemptionId}/fulfill", new { }));
        Assert.Equal("Fulfilled", fulfilled.GetProperty("status").GetString());
        Assert.False(fulfilled.GetProperty("fulfilledAt").ValueKind == JsonValueKind.Null);

        // Erneutes Erfüllen -> 409 (nicht mehr offen)
        Assert.Equal(HttpStatusCode.Conflict,
            (await father.PostAsJsonAsync($"/api/v1/children/{childId}/rewards/redemptions/{redemptionId}/fulfill", new { })).StatusCode);
    }

    [Fact]
    public async Task Redemption_Affordances_NurBeiOffenemKauf()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, child) = await FreshChildAsync(father, "9007");
        (await father.PostAsJsonAsync($"/api/v1/children/{childId}/points", new { amount = 300, reason = "x" })).EnsureSuccessStatusCode();
        var offerId = await CreateOfferAsync(father, childId, new { title = "Nachtisch", cost = 100 });

        var view = await JsonAsync(await child.PostAsJsonAsync($"/api/v1/me/rewards/{offerId}/purchase", new { }));
        var redemptionId = view.GetProperty("redemptions").EnumerateArray().First().GetProperty("id").GetInt32();

        // Offener Kauf: erfüll- UND stornierbar (server-autoritative Affordance).
        var open = await ReadRedemptionAsync(father, childId, redemptionId);
        JsonAssert.True(open, "canFulfill");
        JsonAssert.True(open, "canCancel");

        // Nach Erfüllung: beide Aktionen entfallen.
        (await father.PostAsJsonAsync($"/api/v1/children/{childId}/rewards/redemptions/{redemptionId}/fulfill", new { }))
            .EnsureSuccessStatusCode();
        var done = await ReadRedemptionAsync(father, childId, redemptionId);
        JsonAssert.False(done, "canFulfill");
        JsonAssert.False(done, "canCancel");
    }

    private async Task<JsonElement> ReadRedemptionAsync(HttpClient father, int childId, int redemptionId)
    {
        var list = await JsonAsync(await father.GetAsync($"/api/v1/children/{childId}/rewards/redemptions"));
        return list.EnumerateArray().First(r => r.GetProperty("id").GetInt32() == redemptionId);
    }

    [Fact]
    public async Task Kauf_OhneDeckung_400_KeineAbbuchung()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, child) = await FreshChildAsync(father, "9002");
        var offerId = await CreateOfferAsync(father, childId, new { title = "Kinoabend", cost = 1500 });

        var res = await child.PostAsJsonAsync($"/api/v1/me/rewards/{offerId}/purchase", new { });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        var wallet = await JsonAsync(await child.GetAsync("/api/v1/me/points"));
        Assert.Equal(0, wallet.GetProperty("coins").GetInt32());
    }

    [Fact]
    public async Task Storno_ErstattetMuenzenZurueck()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, child) = await FreshChildAsync(father, "9003");
        (await father.PostAsJsonAsync($"/api/v1/children/{childId}/points", new { amount = 500, reason = "x" })).EnsureSuccessStatusCode();
        var offerId = await CreateOfferAsync(father, childId, new { title = "1 Std Zocken", cost = 400 });

        var view = await JsonAsync(await child.PostAsJsonAsync($"/api/v1/me/rewards/{offerId}/purchase", new { }));
        Assert.Equal(100, view.GetProperty("coins").GetInt32()); // 500 - 400
        var redemptionId = view.GetProperty("redemptions").EnumerateArray().First().GetProperty("id").GetInt32();

        var cancelled = await JsonAsync(await father.PostAsJsonAsync(
            $"/api/v1/children/{childId}/rewards/redemptions/{redemptionId}/cancel", new { }));
        Assert.Equal("Cancelled", cancelled.GetProperty("status").GetString());

        // Rückerstattung: Guthaben wieder bei 500
        var wallet = await JsonAsync(await child.GetAsync("/api/v1/me/points"));
        Assert.Equal(500, wallet.GetProperty("coins").GetInt32());
    }

    [Fact]
    public async Task Storno_EinesBereitsErfuelltenKaufs_409()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, child) = await FreshChildAsync(father, "9008");
        (await father.PostAsJsonAsync($"/api/v1/children/{childId}/points", new { amount = 300, reason = "x" })).EnsureSuccessStatusCode();
        var offerId = await CreateOfferAsync(father, childId, new { title = "Eis", cost = 100 });

        var view = await JsonAsync(await child.PostAsJsonAsync($"/api/v1/me/rewards/{offerId}/purchase", new { }));
        var redemptionId = view.GetProperty("redemptions").EnumerateArray().First().GetProperty("id").GetInt32();
        (await father.PostAsJsonAsync($"/api/v1/children/{childId}/rewards/redemptions/{redemptionId}/fulfill", new { }))
            .EnsureSuccessStatusCode();

        // Ein bereits erfüllter Kauf ist nicht mehr offen → Storno scheitert mit 409 (keine Doppel-Rückerstattung).
        var res = await father.PostAsJsonAsync($"/api/v1/children/{childId}/rewards/redemptions/{redemptionId}/cancel", new { });
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Erfuellen_MitUnbekannterRedemptionId_404()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, _) = await FreshChildAsync(father, "9009");

        // Kein solcher Kauf existiert für dieses (eigene) Kind → NotFound-Zweig, klar von 409 „nicht offen" getrennt.
        var res = await father.PostAsJsonAsync($"/api/v1/children/{childId}/rewards/redemptions/999999/fulfill", new { });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Sohn_KannAngebotFuerFremdesKindNichtKaufen_404()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childAId, _) = await FreshChildAsync(father, "9004");
        var offerId = await CreateOfferAsync(father, childAId, new { title = "Eis", cost = 100 });
        var (_, childB) = await FreshChildAsync(father, "9005");

        var res = await childB.PostAsJsonAsync($"/api/v1/me/rewards/{offerId}/purchase", new { });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Vater_KannAngebotFuerFremdesKindNichtAnlegen_403()
    {
        var father = await TestApi.FatherAsync(factory);

        var res = await father.PostAsJsonAsync("/api/v1/children/999999/rewards", new { title = "X", cost = 100 });

        Assert.True(res.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Anlage_MitUngueltigerQuantity_400()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, _) = await FreshChildAsync(father, "9006");

        var res = await father.PostAsJsonAsync($"/api/v1/children/{childId}/rewards",
            new { title = "X", cost = 100, quantity = 0 });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // ─── PATCH-Validierung ────────────────────────────────────────────────────

    [Fact]
    public async Task Patch_TitelAktualisieren_Aendert()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, _) = await FreshChildAsync(father, "9010");
        var offerId = await CreateOfferAsync(father, childId, new { title = "Alter Titel", cost = 100 });

        var patched = await JsonAsync(await father.PatchAsJsonAsync(
            $"/api/v1/children/{childId}/rewards/{offerId}", new { title = "Neuer Titel" }));
        Assert.Equal("Neuer Titel", patched.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Patch_LeereTitel_400()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, _) = await FreshChildAsync(father, "9011");
        var offerId = await CreateOfferAsync(father, childId, new { title = "Titel", cost = 100 });

        var res = await father.PatchAsJsonAsync(
            $"/api/v1/children/{childId}/rewards/{offerId}", new { title = "   " });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Patch_CostZero_400()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, _) = await FreshChildAsync(father, "9012");
        var offerId = await CreateOfferAsync(father, childId, new { title = "X", cost = 100 });

        var res = await father.PatchAsJsonAsync(
            $"/api/v1/children/{childId}/rewards/{offerId}", new { cost = 0 });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Patch_QuantityUnterEins_400()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, _) = await FreshChildAsync(father, "9013");
        var offerId = await CreateOfferAsync(father, childId, new { title = "X", cost = 50 });

        var res = await father.PatchAsJsonAsync(
            $"/api/v1/children/{childId}/rewards/{offerId}", new { quantity = 0 });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Patch_Deaktivieren_VerstecktAngebotFuerSohn()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, child) = await FreshChildAsync(father, "9014");
        var offerId = await CreateOfferAsync(father, childId, new { title = "Aktiv", cost = 50 });

        // Erst sichtbar
        var viewBefore = await JsonAsync(await child.GetAsync("/api/v1/me/rewards"));
        Assert.Contains(viewBefore.GetProperty("available").EnumerateArray(),
            o => o.GetProperty("id").GetInt32() == offerId);

        // Deaktivieren
        (await father.PatchAsJsonAsync($"/api/v1/children/{childId}/rewards/{offerId}", new { active = false }))
            .EnsureSuccessStatusCode();

        // Nicht mehr sichtbar
        var viewAfter = await JsonAsync(await child.GetAsync("/api/v1/me/rewards"));
        Assert.DoesNotContain(viewAfter.GetProperty("available").EnumerateArray(),
            o => o.GetProperty("id").GetInt32() == offerId);
    }

    [Fact]
    public async Task Patch_UnbekannteId_404()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, _) = await FreshChildAsync(father, "9015");

        var res = await father.PatchAsJsonAsync(
            $"/api/v1/children/{childId}/rewards/999999", new { title = "X" });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
