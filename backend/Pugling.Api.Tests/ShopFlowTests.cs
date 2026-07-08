using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;
using Pugling.Api.Services;

namespace Pugling.Api.Tests;

/// <summary>
/// Sichert den Familien-Shop ab: Vater erstellt Artikel + Angebote, Sohn kauft, aggregiertes Inventar
/// entsteht, Sohn stellt Aktivierungsanfrage, Vater genehmigt/lehnt ab.
/// </summary>
public class ShopFlowTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private async Task<(int childId, HttpClient child)> FreshChildAsync(HttpClient father, string pin)
    {
        var childId = await TestApi.IdAsync(
            await father.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Shop-Kind", pin }));
        return (childId, await TestApi.ChildAsync(factory, childId, pin));
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage res)
    {
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    /// <summary>Erstellt einen Artikel und gibt seine Id zurück.</summary>
    private static async Task<int> CreateArticleAsync(HttpClient father, object body) =>
        (await JsonAsync(await father.PostAsJsonAsync("/api/v1/supervisor/shop/articles", body)))
            .GetProperty("id").GetInt32();

    /// <summary>Erstellt ein Angebot zu einem Artikel und gibt seine Id zurück.</summary>
    private static async Task<int> CreateListingAsync(HttpClient father, int articleId, object body) =>
        (await JsonAsync(await father.PostAsJsonAsync($"/api/v1/supervisor/shop/articles/{articleId}/listings", body)))
            .GetProperty("id").GetInt32();

    private async Task AddGemsAsync(int childId, int amount)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        db.ChildPoints.Add(new ChildPointsEntry
        {
            ChildId = childId,
            Amount = amount,
            Kind = PointKind.Mission,
            Reason = "Test-Gems",
        });
        await db.SaveChangesAsync();
    }

    // ─── Kaufen ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Kauf_MitCoinsUndGems_BuchtAbUndSenktBestand_ErhoehtInventar()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, child) = await FreshChildAsync(father, "9301");
        var articleId = await CreateArticleAsync(father, new
        {
            articleNumber = "TV-901",
            title = "Fernsehen",
            unitType = "Minute",
            actionType = "TV",
        });
        var listingId = await CreateListingAsync(father, articleId, new
        {
            title = "30 Minuten Fernsehen",
            coinPrice = 120,
            gemPrice = 30,
            unitsPerPurchase = 30,
            currentStock = 2,
            maxStock = 2,
        });

        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/points", new { amount = 200, reason = "Coins" }))
            .EnsureSuccessStatusCode();
        await AddGemsAsync(childId, 50);

        var view = await JsonAsync(await child.PostAsJsonAsync($"/api/v1/student/me/shop/listings/{listingId}/purchase", new { }));

        Assert.Equal(80, view.GetProperty("coins").GetInt32());
        Assert.Equal(20, view.GetProperty("gems").GetInt32());

        // Bestand gesunken
        var listing = view.GetProperty("available").EnumerateArray()
            .First(a => a.GetProperty("id").GetInt32() == listingId);
        Assert.Equal(1, listing.GetProperty("currentStock").GetInt32());

        // Aggregiertes Inventar erhöht
        var inv = view.GetProperty("inventory").EnumerateArray()
            .First(i => i.GetProperty("shopArticleId").GetInt32() == articleId);
        Assert.Equal(30, inv.GetProperty("quantity").GetInt32());

        // Wallet-Buchungen
        var wallet = await JsonAsync(await child.GetAsync("/api/v1/student/me/points"));
        Assert.Equal(80, wallet.GetProperty("coins").GetInt32());
        var entries = await JsonAsync(await child.GetAsync("/api/v1/student/me/points/entries"));
        Assert.Contains(entries.EnumerateArray(), e =>
            e.GetProperty("kind").GetString() == "ShopCoins" && e.GetProperty("amount").GetInt32() == -120);
        Assert.Contains(entries.EnumerateArray(), e =>
            e.GetProperty("kind").GetString() == "ShopGems" && e.GetProperty("amount").GetInt32() == -30);
    }

    /// <summary>
    /// Ein gekaufter Artikel erscheint im dedizierten Sohn-Bestand (<c>GET me/shop/inventory</c>) –
    /// dem Gegenstück zum Aktivierungs-POST; die Gesamtzahl steht im Header <c>X-Total-Count</c>.
    /// </summary>
    [Fact]
    public async Task GekaufterArtikel_ErscheintImEigenenBestand()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, child) = await FreshChildAsync(father, "9302");
        var articleId = await CreateArticleAsync(father, new
        {
            articleNumber = "INV-001",
            title = "Naschpaket",
            unitType = "Gramm",
            actionType = "Suessigkeit",
        });
        var listingId = await CreateListingAsync(father, articleId, new
        {
            coinPrice = 100,
            gemPrice = 0,
            unitsPerPurchase = 50,
            currentStock = 2,
            maxStock = 2,
        });

        // Vor dem Kauf: leerer Bestand.
        var empty = await child.GetAsync("/api/v1/student/me/shop/inventory");
        empty.EnsureSuccessStatusCode();
        Assert.Empty((await empty.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray());

        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/points", new { amount = 200, reason = "Coins" }))
            .EnsureSuccessStatusCode();
        await child.PostAsJsonAsync($"/api/v1/student/me/shop/listings/{listingId}/purchase", new { });

        var res = await child.GetAsync("/api/v1/student/me/shop/inventory");
        res.EnsureSuccessStatusCode();
        Assert.Equal("1", res.Headers.GetValues("X-Total-Count").Single());

        var item = (await res.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray()
            .Single(i => i.GetProperty("shopArticleId").GetInt32() == articleId);
        Assert.Equal("INV-001", item.GetProperty("articleNumber").GetString());
        Assert.Equal(50, item.GetProperty("quantity").GetInt32());
    }

    [Fact]
    public async Task ZweiKaeufe_GleicherArtikel_AddierenImInventar()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, child) = await FreshChildAsync(father, "9310");
        var articleId = await CreateArticleAsync(father, new
        {
            articleNumber = "ZK-001",
            title = "TV-Zeit",
            unitType = "Minute",
            actionType = "TV",
        });
        var listing30 = await CreateListingAsync(father, articleId, new
        {
            coinPrice = 100,
            gemPrice = 0,
            unitsPerPurchase = 30,
            currentStock = 3,
            maxStock = 3,
        });
        var listing60 = await CreateListingAsync(father, articleId, new
        {
            coinPrice = 180,
            gemPrice = 0,
            unitsPerPurchase = 60,
            currentStock = 3,
            maxStock = 3,
        });

        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/points", new { amount = 500, reason = "Coins" }))
            .EnsureSuccessStatusCode();

        await child.PostAsJsonAsync($"/api/v1/student/me/shop/listings/{listing30}/purchase", new { });
        await child.PostAsJsonAsync($"/api/v1/student/me/shop/listings/{listing60}/purchase", new { });

        var view = await JsonAsync(await child.GetAsync("/api/v1/student/me/shop"));
        var inv = view.GetProperty("inventory").EnumerateArray()
            .First(i => i.GetProperty("shopArticleId").GetInt32() == articleId);
        Assert.Equal(90, inv.GetProperty("quantity").GetInt32()); // 30 + 60
    }

    [Fact]
    public async Task Kauf_OhneBestand_409_KeineAbbuchung()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, child) = await FreshChildAsync(father, "9304");
        var articleId = await CreateArticleAsync(father, new
        {
            articleNumber = "LEER-001",
            title = "Ausverkauft",
            unitType = "Stueck",
            actionType = "Sonstiges",
        });
        var listingId = await CreateListingAsync(father, articleId, new
        {
            coinPrice = 100,
            gemPrice = 0,
            unitsPerPurchase = 1,
            currentStock = 0,
            maxStock = 1,
        });
        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/points", new { amount = 300, reason = "Coins" }))
            .EnsureSuccessStatusCode();

        var res = await child.PostAsJsonAsync($"/api/v1/student/me/shop/listings/{listingId}/purchase", new { });

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        var wallet = await JsonAsync(await child.GetAsync("/api/v1/student/me/points"));
        Assert.Equal(300, wallet.GetProperty("coins").GetInt32());
    }

    [Fact]
    public async Task Sohn_SiehtKeineAngeboteAusFremderFamilie()
    {
        var fatherA = await TestApi.FatherAsync(factory);
        var (_, child) = await FreshChildAsync(fatherA, "9305");

        // Fremder Artikel/Angebot anlegen
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var fremdVater = new Father { Name = "Fremd-Vater", Pin = "unused" };
        var fremdArticle = new ShopArticle { Father = fremdVater, ArticleNumber = "F-001", Title = "Fremd" };
        var fremdListing = new ShopListing
        {
            ShopArticle = fremdArticle,
            CoinPrice = 1,
            UnitsPerPurchase = 1,
            CurrentStock = 1,
            MaxStock = 1,
        };
        db.ShopListings.Add(fremdListing);
        await db.SaveChangesAsync();
        var fremdId = fremdListing.Id;

        var view = await JsonAsync(await child.GetAsync("/api/v1/student/me/shop"));
        Assert.DoesNotContain(view.GetProperty("available").EnumerateArray(),
            a => a.GetProperty("id").GetInt32() == fremdId);
        Assert.Equal(HttpStatusCode.NotFound,
            (await child.PostAsJsonAsync($"/api/v1/student/me/shop/listings/{fremdId}/purchase", new { })).StatusCode);
    }

    // ─── Stornierung ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Storno_ErstattetCoinsUndGems_ReduzuiertInventar()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, child) = await FreshChildAsync(father, "9303");
        var articleId = await CreateArticleAsync(father, new
        {
            articleNumber = "STO-001",
            title = "Sticker",
            unitType = "Stueck",
            actionType = "Sonstiges",
        });
        var listingId = await CreateListingAsync(father, articleId, new
        {
            coinPrice = 100,
            gemPrice = 25,
            unitsPerPurchase = 5,
            currentStock = 1,
            maxStock = 1,
        });
        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/points", new { amount = 300, reason = "Coins" }))
            .EnsureSuccessStatusCode();
        await AddGemsAsync(childId, 40);

        var view = await JsonAsync(await child.PostAsJsonAsync($"/api/v1/student/me/shop/listings/{listingId}/purchase", new { }));
        var purchaseId = view.GetProperty("purchases").EnumerateArray().First().GetProperty("id").GetInt32();
        Assert.Equal(200, view.GetProperty("coins").GetInt32());
        Assert.Equal(15, view.GetProperty("gems").GetInt32());

        var cancelled = await JsonAsync(await father.PostAsJsonAsync(
            $"/api/v1/supervisor/children/{childId}/shop/purchases/{purchaseId}/cancel", new { }));
        Assert.Equal("Cancelled", cancelled.GetProperty("status").GetString());

        var wallet = await JsonAsync(await child.GetAsync("/api/v1/student/me/points"));
        Assert.Equal(300, wallet.GetProperty("coins").GetInt32());
        Assert.Equal(40, wallet.GetProperty("gems").GetInt32());

        // Inventar muss wieder 0 sein
        var shopView = await JsonAsync(await child.GetAsync("/api/v1/student/me/shop"));
        var invItems = shopView.GetProperty("inventory").EnumerateArray().ToList();
        Assert.DoesNotContain(invItems, i => i.GetProperty("shopArticleId").GetInt32() == articleId && i.GetProperty("quantity").GetInt32() > 0);
    }

    [Fact]
    public async Task DoppelStorno_ErstattetNurEinmal()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, child) = await FreshChildAsync(father, "9306");
        var articleId = await CreateArticleAsync(father, new
        {
            articleNumber = "DS-001",
            title = "Doppel-Storno",
            unitType = "Stueck",
            actionType = "Sonstiges",
        });
        var listingId = await CreateListingAsync(father, articleId, new
        {
            coinPrice = 100,
            gemPrice = 0,
            unitsPerPurchase = 1,
            currentStock = 1,
            maxStock = 1,
        });
        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/points", new { amount = 300, reason = "Coins" }))
            .EnsureSuccessStatusCode();
        var view = await JsonAsync(await child.PostAsJsonAsync($"/api/v1/student/me/shop/listings/{listingId}/purchase", new { }));
        var purchaseId = view.GetProperty("purchases").EnumerateArray().First().GetProperty("id").GetInt32();

        using var scope1 = factory.Services.CreateScope();
        using var scope2 = factory.Services.CreateScope();
        var svc1 = scope1.ServiceProvider.GetRequiredService<ShopService>();
        var svc2 = scope2.ServiceProvider.GetRequiredService<ShopService>();
        var now = new DateTime(2026, 7, 6, 12, 0, 0, DateTimeKind.Utc);

        var first = await svc1.CancelPurchaseAsync(1, childId, purchaseId, now);
        var second = await svc2.CancelPurchaseAsync(1, childId, purchaseId, now);

        // Genau einer der beiden darf erfolgreich sein
        Assert.Contains(ShopService.ShopError.None, new[] { first.Error, second.Error });
        Assert.NotEqual(ShopService.ShopError.None, first.Error == ShopService.ShopError.None ? second.Error : first.Error);
        var wallet = await JsonAsync(await child.GetAsync("/api/v1/student/me/points"));
        Assert.Equal(300, wallet.GetProperty("coins").GetInt32());
    }

    // ─── Aktivierungsflow ────────────────────────────────────────────────────

    [Fact]
    public async Task Aktivierung_SohnStellt_VaterGenehmigt_InventarSinkt()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, child) = await FreshChildAsync(father, "9307");
        var articleId = await CreateArticleAsync(father, new
        {
            articleNumber = "AKT-001",
            title = "Fernsehen",
            unitType = "Minute",
            actionType = "TV",
        });
        var listingId = await CreateListingAsync(father, articleId, new
        {
            coinPrice = 100,
            gemPrice = 0,
            unitsPerPurchase = 50,
            currentStock = 3,
            maxStock = 3,
        });
        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/points", new { amount = 200, reason = "Coins" }))
            .EnsureSuccessStatusCode();

        // Kauf → 50 Min im Inventar
        await child.PostAsJsonAsync($"/api/v1/student/me/shop/listings/{listingId}/purchase", new { });

        // Anfrage: nur 10 Min
        var req = await JsonAsync(await child.PostAsJsonAsync(
            $"/api/v1/student/me/shop/inventory/{articleId}/activate", new { quantity = 10 }));
        var requestId = req.GetProperty("id").GetInt32();
        Assert.Equal("Pending", req.GetProperty("status").GetString());
        Assert.Equal(10, req.GetProperty("requestedQuantity").GetInt32());

        // Vater genehmigt
        var approved = await JsonAsync(await father.PostAsJsonAsync(
            $"/api/v1/supervisor/children/{childId}/shop/activations/{requestId}/approve", new { }));
        Assert.Equal("Approved", approved.GetProperty("status").GetString());
        Assert.False(approved.GetProperty("closedAt").ValueKind == JsonValueKind.Null);

        // Inventar: 50 - 10 = 40
        var shopView = await JsonAsync(await child.GetAsync("/api/v1/student/me/shop"));
        var inv = shopView.GetProperty("inventory").EnumerateArray()
            .First(i => i.GetProperty("shopArticleId").GetInt32() == articleId);
        Assert.Equal(40, inv.GetProperty("quantity").GetInt32());
    }

    [Fact]
    public async Task Aktivierung_VaterLehntAb_InventarBleibt()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, child) = await FreshChildAsync(father, "9308");
        var articleId = await CreateArticleAsync(father, new
        {
            articleNumber = "REJE-001",
            title = "Spielzeit",
            unitType = "Minute",
            actionType = "Zocken",
        });
        var listingId = await CreateListingAsync(father, articleId, new
        {
            coinPrice = 100,
            gemPrice = 0,
            unitsPerPurchase = 30,
            currentStock = 1,
            maxStock = 1,
        });
        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/points", new { amount = 200, reason = "Coins" }))
            .EnsureSuccessStatusCode();
        await child.PostAsJsonAsync($"/api/v1/student/me/shop/listings/{listingId}/purchase", new { });

        var req = await JsonAsync(await child.PostAsJsonAsync(
            $"/api/v1/student/me/shop/inventory/{articleId}/activate", new { quantity = 20 }));
        var requestId = req.GetProperty("id").GetInt32();

        var rejected = await JsonAsync(await father.PostAsJsonAsync(
            $"/api/v1/supervisor/children/{childId}/shop/activations/{requestId}/reject", new { }));
        Assert.Equal("Rejected", rejected.GetProperty("status").GetString());

        // Inventar noch 30 (unveränert)
        var shopView = await JsonAsync(await child.GetAsync("/api/v1/student/me/shop"));
        var inv = shopView.GetProperty("inventory").EnumerateArray()
            .First(i => i.GetProperty("shopArticleId").GetInt32() == articleId);
        Assert.Equal(30, inv.GetProperty("quantity").GetInt32());
    }

    [Fact]
    public async Task Aktivierung_MehrAlsInventar_400()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, child) = await FreshChildAsync(father, "9309");
        var articleId = await CreateArticleAsync(father, new
        {
            articleNumber = "OVER-001",
            title = "Eis",
            unitType = "Gramm",
            actionType = "Suessigkeit",
        });
        var listingId = await CreateListingAsync(father, articleId, new
        {
            coinPrice = 50,
            gemPrice = 0,
            unitsPerPurchase = 10,
            currentStock = 1,
            maxStock = 1,
        });
        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/points", new { amount = 100, reason = "Coins" }))
            .EnsureSuccessStatusCode();
        await child.PostAsJsonAsync($"/api/v1/student/me/shop/listings/{listingId}/purchase", new { });

        // 10 im Inventar, 20 beantragen → 400
        var res = await child.PostAsJsonAsync(
            $"/api/v1/student/me/shop/inventory/{articleId}/activate", new { quantity = 20 });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Aktivierung_DoppelGenehmigung_409()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, child) = await FreshChildAsync(father, "9311");
        var articleId = await CreateArticleAsync(father, new
        {
            articleNumber = "DBLAP-001",
            title = "Doppel",
            unitType = "Stueck",
            actionType = "Sonstiges",
        });
        var listingId = await CreateListingAsync(father, articleId, new
        {
            coinPrice = 50,
            gemPrice = 0,
            unitsPerPurchase = 5,
            currentStock = 2,
            maxStock = 2,
        });
        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/points", new { amount = 200, reason = "Coins" }))
            .EnsureSuccessStatusCode();
        await child.PostAsJsonAsync($"/api/v1/student/me/shop/listings/{listingId}/purchase", new { });

        var req = await JsonAsync(await child.PostAsJsonAsync(
            $"/api/v1/student/me/shop/inventory/{articleId}/activate", new { quantity = 3 }));
        var requestId = req.GetProperty("id").GetInt32();

        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/shop/activations/{requestId}/approve", new { }))
            .EnsureSuccessStatusCode();

        // Erneute Genehmigung → 409
        Assert.Equal(HttpStatusCode.Conflict,
            (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/shop/activations/{requestId}/approve", new { })).StatusCode);
    }

    // ─── Affordances ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Affordances_NurBeiFaelligemStatus()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, child) = await FreshChildAsync(father, "9312");
        var articleId = await CreateArticleAsync(father, new
        {
            articleNumber = "AFF-001",
            title = "Test",
            unitType = "Stueck",
            actionType = "Sonstiges",
        });
        var listingId = await CreateListingAsync(father, articleId, new
        {
            coinPrice = 50,
            gemPrice = 0,
            unitsPerPurchase = 5,
            currentStock = 2,
            maxStock = 2,
        });
        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/points", new { amount = 200, reason = "Coins" }))
            .EnsureSuccessStatusCode();
        await child.PostAsJsonAsync($"/api/v1/student/me/shop/listings/{listingId}/purchase", new { });

        var req = await JsonAsync(await child.PostAsJsonAsync(
            $"/api/v1/student/me/shop/inventory/{articleId}/activate", new { quantity = 3 }));
        var requestId = req.GetProperty("id").GetInt32();

        // Offen: beide Affordances
        var queue = await JsonAsync(await father.GetAsync($"/api/v1/supervisor/children/{childId}/shop/activations"));
        var open = queue.EnumerateArray().First(r => r.GetProperty("id").GetInt32() == requestId);
        Assert.True(open.GetProperty("canApprove").GetBoolean());
        Assert.True(open.GetProperty("canReject").GetBoolean());

        // Nach Genehmigung: keine Affordances mehr
        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/shop/activations/{requestId}/approve", new { }))
            .EnsureSuccessStatusCode();
        var done = await JsonAsync(await father.GetAsync($"/api/v1/supervisor/children/{childId}/shop/activations"));
        var closed = done.EnumerateArray().First(r => r.GetProperty("id").GetInt32() == requestId);
        Assert.False(closed.GetProperty("canApprove").GetBoolean());
        Assert.False(closed.GetProperty("canReject").GetBoolean());
    }

    // ─── Refill ──────────────────────────────────────────────────────────────

    [Fact]
    public void DailyRefill_MitNullLastRefilledAt_IstFaellig()
    {
        var listing = new ShopListing
        {
            CurrentStock = 0,
            MaxStock = 3,
            RefillKind = ShopRefillKind.Daily,
        };

        var changed = ShopService.ApplyDueRefill(listing, new DateTime(2026, 7, 6, 10, 0, 0, DateTimeKind.Utc));

        Assert.True(changed);
        Assert.Equal(3, listing.CurrentStock);
        Assert.NotNull(listing.LastRefilledAtUtc);
    }

    // ─── Artikel-CRUD ────────────────────────────────────────────────────────

    [Fact]
    public async Task ArtikelAnlegen_UndLesen_RoundTrip()
    {
        var father = await TestApi.FatherAsync(factory);
        var articleId = await CreateArticleAsync(father, new
        {
            articleNumber = "CRUD-001",
            title = "Schlafstunde",
            unitType = "Minute",
            actionType = "Sonstiges",
        });

        var list = await JsonAsync(await father.GetAsync("/api/v1/supervisor/shop/articles"));
        var art = list.EnumerateArray().First(a => a.GetProperty("id").GetInt32() == articleId);
        Assert.Equal("CRUD-001", art.GetProperty("articleNumber").GetString());
        Assert.Equal("Schlafstunde", art.GetProperty("title").GetString());
    }

    [Fact]
    public async Task ArtikelAnlegen_DoppelteNummer_409()
    {
        var father = await TestApi.FatherAsync(factory);
        await CreateArticleAsync(father, new
        {
            articleNumber = "DUP-NUM",
            title = "Erster",
            unitType = "Minute",
            actionType = "TV",
        });

        var res = await father.PostAsJsonAsync("/api/v1/supervisor/shop/articles", new
        {
            articleNumber = "DUP-NUM",
            title = "Zweiter",
            unitType = "Minute",
            actionType = "TV",
        });
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("duplicate_key", body!.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ArtikelPatch_TitelUndNummer_Aendert()
    {
        var father = await TestApi.FatherAsync(factory);
        var articleId = await CreateArticleAsync(father, new
        {
            articleNumber = "OLD-001",
            title = "Alter Titel",
            unitType = "Stueck",
            actionType = "Sonstiges",
        });

        var patched = await JsonAsync(await father.PatchAsJsonAsync(
            $"/api/v1/supervisor/shop/articles/{articleId}", new { title = "Neuer Titel", articleNumber = "NEW-001" }));
        Assert.Equal("NEW-001", patched.GetProperty("articleNumber").GetString());
        Assert.Equal("Neuer Titel", patched.GetProperty("title").GetString());
    }

    [Fact]
    public async Task ArtikelPatch_LeererTitel_400()
    {
        var father = await TestApi.FatherAsync(factory);
        var articleId = await CreateArticleAsync(father, new
        {
            articleNumber = "ETITLE-001",
            title = "Ursprung",
            unitType = "Stueck",
            actionType = "Sonstiges",
        });

        var res = await father.PatchAsJsonAsync($"/api/v1/supervisor/shop/articles/{articleId}", new { title = "  " });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task ArtikelLoeschen_EntferntEintrag()
    {
        var father = await TestApi.FatherAsync(factory);
        var articleId = await CreateArticleAsync(father, new
        {
            articleNumber = "DEL-001",
            title = "Zu löschen",
            unitType = "Stueck",
            actionType = "Sonstiges",
        });

        (await father.DeleteAsync($"/api/v1/supervisor/shop/articles/{articleId}")).EnsureSuccessStatusCode();

        var list = await JsonAsync(await father.GetAsync("/api/v1/supervisor/shop/articles"));
        Assert.DoesNotContain(list.EnumerateArray(), a => a.GetProperty("id").GetInt32() == articleId);
    }

    // ─── Listing-CRUD ────────────────────────────────────────────────────────

    [Fact]
    public async Task ListingAnlegen_UndLesen_RoundTrip()
    {
        var father = await TestApi.FatherAsync(factory);
        var articleId = await CreateArticleAsync(father, new
        {
            articleNumber = "LST-001",
            title = "Listing-Test",
            unitType = "Minute",
            actionType = "TV",
        });
        var listingId = await CreateListingAsync(father, articleId, new
        {
            title = "30 Min",
            coinPrice = 150,
            gemPrice = 0,
            unitsPerPurchase = 30,
            currentStock = 5,
            maxStock = 5,
        });

        var list = await JsonAsync(await father.GetAsync($"/api/v1/supervisor/shop/articles/{articleId}/listings"));
        var l = list.EnumerateArray().First(x => x.GetProperty("id").GetInt32() == listingId);
        Assert.Equal(150, l.GetProperty("coinPrice").GetInt32());
        Assert.Equal(30, l.GetProperty("unitsPerPurchase").GetInt32());
    }

    [Fact]
    public async Task ListingAnlegen_BeidePreisNull_400()
    {
        var father = await TestApi.FatherAsync(factory);
        var articleId = await CreateArticleAsync(father, new
        {
            articleNumber = "ZEROPRICE",
            title = "Null-Preis",
            unitType = "Stueck",
            actionType = "Sonstiges",
        });

        var res = await father.PostAsJsonAsync($"/api/v1/supervisor/shop/articles/{articleId}/listings",
            new { coinPrice = 0, gemPrice = 0, unitsPerPurchase = 1, currentStock = 1, maxStock = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task ListingPatch_Deaktivieren()
    {
        var father = await TestApi.FatherAsync(factory);
        var articleId = await CreateArticleAsync(father, new
        {
            articleNumber = "DEACT-001",
            title = "Deaktivierbar",
            unitType = "Stueck",
            actionType = "Sonstiges",
        });
        var listingId = await CreateListingAsync(father, articleId, new
        {
            coinPrice = 50,
            gemPrice = 0,
            unitsPerPurchase = 1,
            currentStock = 3,
            maxStock = 3,
        });

        var patched = await JsonAsync(await father.PatchAsJsonAsync(
            $"/api/v1/supervisor/shop/articles/{articleId}/listings/{listingId}", new { active = false }));
        Assert.False(patched.GetProperty("active").GetBoolean());
    }

    [Fact]
    public async Task ListingLoeschen_EntferntEintrag()
    {
        var father = await TestApi.FatherAsync(factory);
        var articleId = await CreateArticleAsync(father, new
        {
            articleNumber = "DELLIST-001",
            title = "Listing löschen",
            unitType = "Stueck",
            actionType = "Sonstiges",
        });
        var listingId = await CreateListingAsync(father, articleId, new
        {
            coinPrice = 50,
            gemPrice = 0,
            unitsPerPurchase = 1,
            currentStock = 1,
            maxStock = 1,
        });

        (await father.DeleteAsync($"/api/v1/supervisor/shop/articles/{articleId}/listings/{listingId}")).EnsureSuccessStatusCode();

        var list = await JsonAsync(await father.GetAsync($"/api/v1/supervisor/shop/articles/{articleId}/listings"));
        Assert.DoesNotContain(list.EnumerateArray(), l => l.GetProperty("id").GetInt32() == listingId);
    }

    // ─── Kauf-Fehler ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Kauf_OhneCoins_400_KeineAbbuchung()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, child) = await FreshChildAsync(father, "9350");
        // Kein Guthaben

        var articleId = await CreateArticleAsync(father, new
        {
            articleNumber = "NOCOIN-001",
            title = "Kein Geld",
            unitType = "Stueck",
            actionType = "Sonstiges",
        });
        var listingId = await CreateListingAsync(father, articleId, new
        {
            coinPrice = 500,
            gemPrice = 0,
            unitsPerPurchase = 1,
            currentStock = 5,
            maxStock = 5,
        });

        var res = await child.PostAsJsonAsync($"/api/v1/student/me/shop/listings/{listingId}/purchase", new { });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("insufficient_coins", body!.GetProperty("code").GetString());

        var wallet = await JsonAsync(await child.GetAsync("/api/v1/student/me/points"));
        Assert.Equal(0, wallet.GetProperty("coins").GetInt32());
    }

    [Fact]
    public async Task Kauf_OhneGems_400()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, child) = await FreshChildAsync(father, "9351");
        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/points",
            new { amount = 500, reason = "Coins" })).EnsureSuccessStatusCode();
        // Keine Gems

        var articleId = await CreateArticleAsync(father, new
        {
            articleNumber = "NOGEM-001",
            title = "Kein Gem",
            unitType = "Stueck",
            actionType = "Sonstiges",
        });
        var listingId = await CreateListingAsync(father, articleId, new
        {
            coinPrice = 100,
            gemPrice = 50,
            unitsPerPurchase = 1,
            currentStock = 5,
            maxStock = 5,
        });

        var res = await child.PostAsJsonAsync($"/api/v1/student/me/shop/listings/{listingId}/purchase", new { });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("insufficient_gems", body!.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Kauf_InaktivesAngebot_400()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, child) = await FreshChildAsync(father, "9352");
        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/points",
            new { amount = 500, reason = "Coins" })).EnsureSuccessStatusCode();

        var articleId = await CreateArticleAsync(father, new
        {
            articleNumber = "INACT-001",
            title = "Inaktiv",
            unitType = "Stueck",
            actionType = "Sonstiges",
        });
        var listingId = await CreateListingAsync(father, articleId, new
        {
            coinPrice = 100,
            gemPrice = 0,
            unitsPerPurchase = 1,
            currentStock = 5,
            maxStock = 5,
        });
        await father.PatchAsJsonAsync(
            $"/api/v1/supervisor/shop/articles/{articleId}/listings/{listingId}", new { active = false });

        var res = await child.PostAsJsonAsync($"/api/v1/student/me/shop/listings/{listingId}/purchase", new { });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("shop_listing_inactive", body!.GetProperty("code").GetString());
    }

    // ─── Kauf-Affordances ────────────────────────────────────────────────────

    [Fact]
    public async Task KaufAffordances_NurBeiFaelligemStatus()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, child) = await FreshChildAsync(father, "9353");
        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/points",
            new { amount = 300, reason = "Coins" })).EnsureSuccessStatusCode();

        var articleId = await CreateArticleAsync(father, new
        {
            articleNumber = "AFFORD-001",
            title = "Affordance-Test",
            unitType = "Stueck",
            actionType = "Sonstiges",
        });
        var listingId = await CreateListingAsync(father, articleId, new
        {
            coinPrice = 100,
            gemPrice = 0,
            unitsPerPurchase = 1,
            currentStock = 3,
            maxStock = 3,
        });

        var view = await JsonAsync(await child.PostAsJsonAsync(
            $"/api/v1/student/me/shop/listings/{listingId}/purchase", new { }));
        var purchaseId = view.GetProperty("purchases").EnumerateArray().First().GetProperty("id").GetInt32();

        // Kauf offen: canCancel = true
        var purchases = await JsonAsync(await father.GetAsync($"/api/v1/supervisor/children/{childId}/shop/purchases"));
        var open = purchases.EnumerateArray().First(p => p.GetProperty("id").GetInt32() == purchaseId);
        Assert.True(open.GetProperty("canCancel").GetBoolean());

        // Nach Storno: canCancel = false
        (await father.PostAsJsonAsync(
            $"/api/v1/supervisor/children/{childId}/shop/purchases/{purchaseId}/cancel", new { })).EnsureSuccessStatusCode();
        var after = await JsonAsync(await father.GetAsync($"/api/v1/supervisor/children/{childId}/shop/purchases"));
        var closed = after.EnumerateArray().First(p => p.GetProperty("id").GetInt32() == purchaseId);
        Assert.False(closed.GetProperty("canCancel").GetBoolean());
    }

    // ─── Paging ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Paging_KaufHistorie_LiefertXTotalCount()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, child) = await FreshChildAsync(father, "9354");
        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/points",
            new { amount = 2000, reason = "Coins" })).EnsureSuccessStatusCode();

        var articleId = await CreateArticleAsync(father, new
        {
            articleNumber = "PAGE-001",
            title = "Paging-Test",
            unitType = "Stueck",
            actionType = "Sonstiges",
        });
        var listingId = await CreateListingAsync(father, articleId, new
        {
            coinPrice = 50,
            gemPrice = 0,
            unitsPerPurchase = 1,
            currentStock = 10,
            maxStock = 10,
        });

        for (var i = 0; i < 5; i++)
            await child.PostAsJsonAsync($"/api/v1/student/me/shop/listings/{listingId}/purchase", new { });

        var res = await father.GetAsync($"/api/v1/supervisor/children/{childId}/shop/purchases?take=2");
        Assert.Equal(2, (await JsonAsync(res)).EnumerateArray().Count());
        Assert.Equal("5", res.Headers.GetValues("X-Total-Count").First());
    }

    [Fact]
    public async Task Paging_Aktivierungen_LiefertXTotalCount()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, child) = await FreshChildAsync(father, "9355");
        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/points",
            new { amount = 500, reason = "Coins" })).EnsureSuccessStatusCode();

        var articleId = await CreateArticleAsync(father, new
        {
            articleNumber = "APAGE-001",
            title = "Aktivierungs-Paging",
            unitType = "Stueck",
            actionType = "Sonstiges",
        });
        var listingId = await CreateListingAsync(father, articleId, new
        {
            coinPrice = 50,
            gemPrice = 0,
            unitsPerPurchase = 10,
            currentStock = 2,
            maxStock = 2,
        });
        await child.PostAsJsonAsync($"/api/v1/student/me/shop/listings/{listingId}/purchase", new { });

        // 3 Anfragen je 1 Einheit (Inventar hat 10)
        for (var i = 0; i < 3; i++)
            await child.PostAsJsonAsync($"/api/v1/student/me/shop/inventory/{articleId}/activate", new { quantity = 1 });

        var res = await father.GetAsync($"/api/v1/supervisor/children/{childId}/shop/activations?take=2");
        Assert.Equal(2, (await JsonAsync(res)).EnumerateArray().Count());
        Assert.Equal("3", res.Headers.GetValues("X-Total-Count").First());
    }

    // ─── Wallet-/Inventar-Integrität ───────────────────────────────────────────

    [Fact]
    public async Task Kauf_BumptChildConcurrencyStamp_SchuetztWalletVorParallelemDoppelkauf()
    {
        // Wie Angebote/Skins muss ein Shop-Kauf das Concurrency-Token des Kindes erhöhen: nur so scheitert
        // ein parallel gestarteter zweiter Wallet-Write (vgl. SkinPurchaseTests.ConcurrencyToken) und der
        // Deckungs-Check kann nicht doppelt umgangen werden. Der Listing-Stamp allein schützt nur denselben
        // Bestand – über verschiedene Listings/Angebote hinweg bliebe der Saldo sonst ungeschützt.
        var father = await TestApi.FatherAsync(factory);
        var (childId, child) = await FreshChildAsync(father, "9360");
        var articleId = await CreateArticleAsync(father, new
        {
            articleNumber = "STAMP-001",
            title = "Stamp",
            unitType = "Stueck",
            actionType = "Sonstiges",
        });
        var listingId = await CreateListingAsync(father, articleId, new
        {
            coinPrice = 50,
            gemPrice = 0,
            unitsPerPurchase = 1,
            currentStock = 3,
            maxStock = 3,
        });
        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/points",
            new { amount = 200, reason = "Coins" })).EnsureSuccessStatusCode();

        Guid StampOf()
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            return db.Children.First(c => c.Id == childId).ConcurrencyStamp;
        }

        var before = StampOf();
        (await child.PostAsJsonAsync($"/api/v1/student/me/shop/listings/{listingId}/purchase", new { }))
            .EnsureSuccessStatusCode();

        Assert.NotEqual(before, StampOf());
    }

    [Fact]
    public async Task Aktivierung_ZweiOffeneAnfragen_UebersteigenInventar_ZweiteGenehmigungScheitert()
    {
        // Der Anfrage-Check ist nicht-transaktional: zwei offene Anfragen können in Summe das Inventar
        // übersteigen. Verbindlich ist erst die Deckungsprüfung bei der Genehmigung – sie darf das Inventar
        // nicht stillschweigend auf 0 klemmen, sondern muss die zweite Genehmigung ablehnen (kein Freibetrag).
        var father = await TestApi.FatherAsync(factory);
        var (childId, child) = await FreshChildAsync(father, "9361");
        var articleId = await CreateArticleAsync(father, new
        {
            articleNumber = "OVERAP-001",
            title = "Fernsehen",
            unitType = "Minute",
            actionType = "TV",
        });
        var listingId = await CreateListingAsync(father, articleId, new
        {
            coinPrice = 100,
            gemPrice = 0,
            unitsPerPurchase = 30,
            currentStock = 1,
            maxStock = 1,
        });
        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/points",
            new { amount = 200, reason = "Coins" })).EnsureSuccessStatusCode();
        await child.PostAsJsonAsync($"/api/v1/student/me/shop/listings/{listingId}/purchase", new { }); // Inventar = 30

        var req1 = (await JsonAsync(await child.PostAsJsonAsync(
            $"/api/v1/student/me/shop/inventory/{articleId}/activate", new { quantity = 30 }))).GetProperty("id").GetInt32();
        var req2 = (await JsonAsync(await child.PostAsJsonAsync(
            $"/api/v1/student/me/shop/inventory/{articleId}/activate", new { quantity = 10 }))).GetProperty("id").GetInt32();

        // Erste Genehmigung zieht das gesamte Inventar ab (30 → 0).
        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/shop/activations/{req1}/approve", new { }))
            .EnsureSuccessStatusCode();

        // Zweite Genehmigung findet kein Inventar mehr → 400 insufficient_inventory, Anfrage bleibt offen.
        var overRes = await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/shop/activations/{req2}/approve", new { });
        Assert.Equal(HttpStatusCode.BadRequest, overRes.StatusCode);
        Assert.Equal("insufficient_inventory",
            (await overRes.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        var queue = await JsonAsync(await father.GetAsync($"/api/v1/supervisor/children/{childId}/shop/activations"));
        var stillOpen = queue.EnumerateArray().First(r => r.GetProperty("id").GetInt32() == req2);
        Assert.Equal("Pending", stillOpen.GetProperty("status").GetString());

        // Die weiterhin offene Anfrage lässt sich ablehnen.
        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/shop/activations/{req2}/reject", new { }))
            .EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Aktivierung_QuantityNull_400_ValidationError()
    {
        var father = await TestApi.FatherAsync(factory);
        var (_, child) = await FreshChildAsync(father, "9362");
        var articleId = await CreateArticleAsync(father, new
        {
            articleNumber = "QZERO-001",
            title = "Null-Menge",
            unitType = "Stueck",
            actionType = "Sonstiges",
        });

        var res = await child.PostAsJsonAsync(
            $"/api/v1/student/me/shop/inventory/{articleId}/activate", new { quantity = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("validation_error",
            (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Artikel_UndAngebot_LassenSichEinzelnLesen()
    {
        // Symmetrie zu PATCH/DELETE: die Einzel-Ressource ist auch per GET abrufbar (Read-after-write).
        var father = await TestApi.FatherAsync(factory);
        var articleId = await CreateArticleAsync(father, new
        {
            articleNumber = "GET-001",
            title = "Einzelabruf",
            unitType = "Minute",
            actionType = "TV",
        });
        var listingId = await CreateListingAsync(father, articleId, new
        {
            title = "10 Minuten",
            coinPrice = 40,
            gemPrice = 0,
            unitsPerPurchase = 10,
            currentStock = 3,
            maxStock = 3,
        });

        var article = await JsonAsync(await father.GetAsync($"/api/v1/supervisor/shop/articles/{articleId}"));
        Assert.Equal(articleId, article.GetProperty("id").GetInt32());
        Assert.Equal("GET-001", article.GetProperty("articleNumber").GetString());

        var listing = await JsonAsync(await father.GetAsync($"/api/v1/supervisor/shop/articles/{articleId}/listings/{listingId}"));
        Assert.Equal(listingId, listing.GetProperty("id").GetInt32());
        Assert.Equal(articleId, listing.GetProperty("shopArticleId").GetInt32());

        // Unbekannte IDs → 404 (kein 200 mit leerem Body).
        Assert.Equal(HttpStatusCode.NotFound,
            (await father.GetAsync($"/api/v1/supervisor/shop/articles/{articleId}/listings/999999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await father.GetAsync("/api/v1/supervisor/shop/articles/999999")).StatusCode);
    }

    [Fact]
    public async Task Artikel_EinzelabrufFremderVater_404()
    {
        // Ownership: ein anderer Vater darf den Artikel nicht per Einzel-GET sehen.
        var owner = await TestApi.FatherAsync(factory);
        var articleId = await CreateArticleAsync(owner, new
        {
            articleNumber = "OWN-001",
            title = "Fremd",
            unitType = "Stueck",
            actionType = "Sonstiges",
        });
        var anon = factory.CreateClient();
        var strangerId = await TestApi.IdAsync(
            await anon.PostAsJsonAsync("/api/v1/supervisor/fathers", new { name = "Fremder Papa", pin = "2222" }));
        var stranger = await TestApi.FatherAsync(factory, strangerId, "2222");

        Assert.Equal(HttpStatusCode.NotFound,
            (await stranger.GetAsync($"/api/v1/supervisor/shop/articles/{articleId}")).StatusCode);
    }

    [Fact]
    public async Task CreateArticle_MitUngueltigemEnum_NenntFeldUndErlaubteWerte()
    {
        // Ein ungültiger Enum-Wert soll nicht nur „value is not of the expected type" liefern, sondern
        // das fehlerhafte Feld benennen UND die zulässigen Werte auflisten (ohne den DTO-Typ zu leaken).
        var father = await TestApi.FatherAsync(factory);
        var res = await father.PostAsJsonAsync("/api/v1/supervisor/shop/articles", new
        {
            articleNumber = "TV-900",
            title = "Fernsehzeit",
            description = "Bildschirmzeit",
            unitType = "WRONG",
            actionType = "TV",
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        var raw = await res.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Pugling.Api", raw);          // kein Typnamen-Leak
        Assert.DoesNotContain("could not be converted", raw); // keine rohe System.Text.Json-Meldung

        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("validation_error", body.GetProperty("code").GetString());
        var message = body.GetProperty("errors").GetProperty("unitType")[0].GetString();
        Assert.Contains("allowed values", message!, StringComparison.OrdinalIgnoreCase);
        foreach (var name in Enum.GetNames<UnitType>())
            Assert.Contains(name, message);
    }
}
