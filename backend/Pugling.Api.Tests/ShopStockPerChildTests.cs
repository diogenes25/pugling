using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;

namespace Pugling.Api.Tests;

/// <summary>
/// B-645 (stock per child, refill plan with catch-up, audience, euro) and B-665 (the supervisor redeems from the child's
/// inventory) against the running API. The class owns its factory, so it freezes the clock to the calendar days a case
/// needs; every case names the change that turns it red.
/// </summary>
public class ShopStockPerChildTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static DateTimeOffset At(int y, int m, int d, int h = 12) => new(y, m, d, h, 0, 0, TimeSpan.Zero);

    private static async Task<JsonElement> Json(HttpResponseMessage res)
    {
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<string> CodeAsync(HttpResponseMessage res) =>
        (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString()!;

    private async Task<(int Id, HttpClient Client)> KidAsync(HttpClient father, string pin)
    {
        var id = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Kind"), pin }));
        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{id}/points", new { amount = 5000, reason = "Test" }))
            .EnsureSuccessStatusCode();
        return (id, await TestApi.ChildAsync(factory, id, pin));
    }

    private static async Task<(int ArticleId, int ListingId)> ListingAsync(HttpClient father, object listing, string unitType = "Minute")
    {
        var number = TestApi.UniqueName("A");
        var articleId = (await Json(await father.PostAsJsonAsync("/api/v1/supervisor/shop/articles",
            new { articleNumber = number, title = number, unitType, actionType = "Sonstiges" }))).GetProperty("id").GetInt32();
        var res = await father.PostAsJsonAsync($"/api/v1/supervisor/shop/articles/{articleId}/listings", listing);
        return (articleId, (await Json(res)).GetProperty("id").GetInt32());
    }

    private static async Task<JsonElement?> OfferAsync(HttpClient child, int listingId)
    {
        var view = await Json(await child.GetAsync("/api/v1/student/me/shop"));
        foreach (var offer in view.GetProperty("available").EnumerateArray())
            if (offer.GetProperty("id").GetInt32() == listingId) return offer;
        return null;
    }

    private static async Task<int> StockAsync(HttpClient child, int listingId) =>
        (await OfferAsync(child, listingId))!.Value.GetProperty("currentStock").GetInt32();

    private static Task<HttpResponseMessage> BuyAsync(HttpClient child, int listingId) =>
        child.PostAsJsonAsync($"/api/v1/student/me/shop/listings/{listingId}/purchase", new { });

    private static Task<HttpResponseMessage> SetStockAsync(HttpClient father, int articleId, int listingId, int childId, int stock) =>
        father.PutAsJsonAsync($"/api/v1/supervisor/shop/articles/{articleId}/listings/{listingId}/stock/{childId}",
            new { currentStock = stock });

    // ─── B-645 ───────────────────────────────────────────────────────────────

    /// <summary>
    /// AK 1 + AK 9: a monthly listing with stock 1 is bought by each son once - Friedrich's purchase leaves Theodor's
    /// stock alone. The buyer then sees 0, not affordable, back on the 1st. Red when a purchase lowers a shared counter
    /// (the brother's purchase answers 409) or when <c>Affordable</c> reads anything but the child's own stock.
    /// </summary>
    [Fact]
    public async Task Monatsangebot_Jedes_Kind_Kauft_Aus_Eigenem_Bestand()
    {
        factory.Clock.FreezeAt(At(2026, 10, 31, 10));
        var father = await TestApi.AdultAsync(factory);
        var a = await KidAsync(father, "4101");
        var b = await KidAsync(father, "4102");
        var (_, listingId) = await ListingAsync(father, new
        {
            title = "Taschengeld",
            coinPrice = 10,
            gemPrice = 0,
            unitsPerPurchase = 5,
            maxStock = 1,
            refillKind = "Monthly",
        });

        Assert.Equal(HttpStatusCode.OK, (await BuyAsync(a.Client, listingId)).StatusCode);

        var own = (await OfferAsync(a.Client, listingId))!.Value;
        Assert.Equal(0, own.GetProperty("currentStock").GetInt32());
        Assert.False(own.GetProperty("affordable").GetBoolean());
        Assert.Equal(new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc), own.GetProperty("nextRefillAtUtc").GetDateTime().ToUniversalTime());

        var sibling = (await OfferAsync(b.Client, listingId))!.Value;
        Assert.Equal(1, sibling.GetProperty("currentStock").GetInt32());
        Assert.True(sibling.GetProperty("affordable").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await BuyAsync(b.Client, listingId)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await BuyAsync(a.Client, listingId)).StatusCode);
    }

    /// <summary>
    /// AK 2 + AK 5, monthly: empty on 20 Oct, still empty late on the 31st, refilled on 3 Nov although nobody opened the
    /// shop on the 1st. Red when monthly refills on another day, or when a missed 1st is lost.
    /// </summary>
    [Fact]
    public async Task Monatlich_Holt_Den_Ersten_Nach_Und_Nur_Am_Ersten()
    {
        factory.Clock.FreezeAt(At(2026, 10, 20));
        var father = await TestApi.AdultAsync(factory);
        var a = await KidAsync(father, "4111");
        var (_, listingId) = await ListingAsync(father, new { coinPrice = 10, unitsPerPurchase = 1, maxStock = 1, refillKind = "Monthly" });
        (await BuyAsync(a.Client, listingId)).EnsureSuccessStatusCode();

        factory.Clock.FreezeAt(At(2026, 10, 31, 23));
        Assert.Equal(0, await StockAsync(a.Client, listingId));

        factory.Clock.FreezeAt(At(2026, 11, 3, 9));
        Assert.Equal(1, await StockAsync(a.Client, listingId));
    }

    /// <summary>
    /// AK 2, weekly (the defect "Wöchentlich verfällt"): bought out on Thursday, nobody looks on Monday, Wednesday shows
    /// the refill. Red under the old rule "due only if today is the weekday".
    /// </summary>
    [Fact]
    public async Task Woechentlich_Holt_Den_Montag_Nach()
    {
        factory.Clock.FreezeAt(At(2026, 11, 12, 18));
        var father = await TestApi.AdultAsync(factory);
        var a = await KidAsync(father, "4121");
        var (_, listingId) = await ListingAsync(father, new
        {
            coinPrice = 10,
            unitsPerPurchase = 30,
            maxStock = 1,
            refillKind = "Weekly",
            refillDayOfWeek = "Monday",
        });
        (await BuyAsync(a.Client, listingId)).EnsureSuccessStatusCode();
        var empty = (await OfferAsync(a.Client, listingId))!.Value;
        Assert.Equal(new DateTime(2026, 11, 16, 0, 0, 0, DateTimeKind.Utc), empty.GetProperty("nextRefillAtUtc").GetDateTime().ToUniversalTime());

        factory.Clock.FreezeAt(At(2026, 11, 18, 9));
        Assert.Equal(1, await StockAsync(a.Client, listingId));
    }

    /// <summary>
    /// AK 3, saving up (+4 per month up to 8), each case with its start stock set by the supervisor: 2 → 6 (old rule: 8),
    /// 6 → 8 (cap), 0 with two missed 1sts → 8, with one → 4 (old rule: 8).
    /// </summary>
    [Fact]
    public async Task Ansparen_Bis_Zur_Obergrenze()
    {
        factory.Clock.FreezeAt(At(2026, 10, 31));
        var father = await TestApi.AdultAsync(factory);
        var a = await KidAsync(father, "4131");
        var (articleId, listingId) = await ListingAsync(father, new
        {
            coinPrice = 10,
            unitsPerPurchase = 5,
            maxStock = 8,
            refillQuantity = 4,
            refillKind = "Monthly",
        });

        (await SetStockAsync(father, articleId, listingId, a.Id, 2)).EnsureSuccessStatusCode();
        factory.Clock.FreezeAt(At(2026, 11, 1, 8));
        Assert.Equal(6, await StockAsync(a.Client, listingId));

        (await SetStockAsync(father, articleId, listingId, a.Id, 6)).EnsureSuccessStatusCode();
        factory.Clock.FreezeAt(At(2026, 12, 1, 8));
        Assert.Equal(8, await StockAsync(a.Client, listingId));

        (await SetStockAsync(father, articleId, listingId, a.Id, 0)).EnsureSuccessStatusCode();
        factory.Clock.FreezeAt(At(2027, 2, 2));
        Assert.Equal(8, await StockAsync(a.Client, listingId));

        (await SetStockAsync(father, articleId, listingId, a.Id, 0)).EnsureSuccessStatusCode();
        factory.Clock.FreezeAt(At(2027, 3, 2));
        Assert.Equal(4, await StockAsync(a.Client, listingId));
    }

    /// <summary>
    /// AK 10: a child linked after its sibling bought starts full (not with the sibling's remainder); a Once listing is
    /// empty for everyone before its instant and full after it. Red when a new child's stock is copied from another row,
    /// or when Once starts at MaxStock.
    /// </summary>
    [Fact]
    public async Task Startbestand_Neues_Kind_Und_Einmalig_Vor_Dem_Termin()
    {
        factory.Clock.FreezeAt(At(2026, 11, 19, 10));
        var father = await TestApi.AdultAsync(factory);
        var a = await KidAsync(father, "4141");
        var (_, daily) = await ListingAsync(father, new { coinPrice = 10, unitsPerPurchase = 10, maxStock = 2, refillKind = "Daily" });
        (await BuyAsync(a.Client, daily)).EnsureSuccessStatusCode();

        var b = await KidAsync(father, "4142");
        Assert.Equal(2, await StockAsync(b.Client, daily));
        Assert.Equal(1, await StockAsync(a.Client, daily));

        var (_, once) = await ListingAsync(father, new
        {
            coinPrice = 10,
            unitsPerPurchase = 1,
            maxStock = 2,
            refillKind = "Once",
            refillAtUtc = "2026-11-21T00:00:00Z",
        });
        Assert.Equal(0, await StockAsync(a.Client, once));
        Assert.Equal(HttpStatusCode.Conflict, (await BuyAsync(a.Client, once)).StatusCode);

        factory.Clock.FreezeAt(At(2026, 11, 22));
        Assert.Equal(2, await StockAsync(a.Client, once));
    }

    /// <summary>
    /// AK 10, the review's scenario: a one-off with a saving quantity (+4 of 8). One sibling's row exists before the
    /// instant (the father's list created it), the other child is linked after it - both end at 8. Red when a one-off
    /// refills by its quantity (the early row ends at 4).
    /// </summary>
    [Fact]
    public async Task Einmalig_Gibt_Geschwistern_Denselben_Bestand_Egal_Wann_Gelesen()
    {
        factory.Clock.FreezeAt(At(2026, 11, 19, 10));
        var father = await TestApi.AdultAsync(factory);
        var early = await KidAsync(father, "4143");
        var (articleId, once) = await ListingAsync(father, new
        {
            coinPrice = 10,
            unitsPerPurchase = 1,
            maxStock = 8,
            refillQuantity = 4,
            refillKind = "Once",
            refillAtUtc = "2026-11-21T00:00:00Z",
        });
        await Json(await father.GetAsync($"/api/v1/supervisor/shop/articles/{articleId}/listings"));
        Assert.Equal(0, await StockAsync(early.Client, once));

        factory.Clock.FreezeAt(At(2026, 11, 22));
        var late = await KidAsync(father, "4144");
        Assert.Equal(8, await StockAsync(early.Client, once));
        Assert.Equal(8, await StockAsync(late.Client, once));
    }

    /// <summary>
    /// AK 11 (decision 12): a listing for Friedrich only is in his shop, not in Theodor's, and Theodor's purchase is a 404;
    /// clearing the audience shows it to both; a foreign child id is refused. Red when the student list or the purchase
    /// skips the audience filter.
    /// </summary>
    [Fact]
    public async Task Zielgruppe_Trennt_Die_Angebote_Der_Geschwister()
    {
        factory.Clock.FreezeAt(At(2026, 11, 19));
        var father = await TestApi.AdultAsync(factory);
        var f = await KidAsync(father, "4151");
        var t = await KidAsync(father, "4152");
        var (articleId, listingId) = await ListingAsync(father, new
        {
            title = "Taschengeld Friedrich",
            coinPrice = 10,
            unitsPerPurchase = 5,
            maxStock = 4,
            refillKind = "Monthly",
            childIds = new[] { f.Id },
        });

        Assert.NotNull(await OfferAsync(f.Client, listingId));
        Assert.Null(await OfferAsync(t.Client, listingId));
        Assert.Equal(HttpStatusCode.NotFound, (await BuyAsync(t.Client, listingId)).StatusCode);

        var supervisorView = await Json(await father.GetAsync($"/api/v1/supervisor/shop/articles/{articleId}/listings/{listingId}"));
        Assert.Equal([f.Id], supervisorView.GetProperty("childIds").EnumerateArray().Select(e => e.GetInt32()));
        Assert.Equal([f.Id], supervisorView.GetProperty("stocks").EnumerateArray().Select(s => s.GetProperty("childId").GetInt32()));

        (await father.PatchAsJsonAsync($"/api/v1/supervisor/shop/articles/{articleId}/listings/{listingId}",
            new { childIds = Array.Empty<int>() })).EnsureSuccessStatusCode();
        Assert.NotNull(await OfferAsync(t.Client, listingId));

        var foreign = await father.PostAsJsonAsync($"/api/v1/supervisor/shop/articles/{articleId}/listings",
            new { coinPrice = 10, unitsPerPurchase = 1, maxStock = 1, childIds = new[] { 987654 } });
        Assert.Equal(HttpStatusCode.BadRequest, foreign.StatusCode);
        Assert.Equal("validation_error", await CodeAsync(foreign));
    }

    /// <summary>
    /// The supervisor's single view computes every child's stock without writing - no stock row exists after it; the list
    /// call persists them. Red when the GET saves (a write side effect).
    /// </summary>
    [Fact]
    public async Task Einzelansicht_Schreibt_Nicht_Die_Liste_Schon()
    {
        factory.Clock.FreezeAt(At(2026, 11, 19));
        var father = await TestApi.AdultAsync(factory);
        var a = await KidAsync(father, "4161");
        var (articleId, listingId) = await ListingAsync(father, new { coinPrice = 10, unitsPerPurchase = 1, maxStock = 3, refillKind = "Daily" });

        var single = await Json(await father.GetAsync($"/api/v1/supervisor/shop/articles/{articleId}/listings/{listingId}"));
        Assert.Equal(3, single.GetProperty("stocks").EnumerateArray().First(s => s.GetProperty("childId").GetInt32() == a.Id)
            .GetProperty("currentStock").GetInt32());
        Assert.Equal(0, await StockRowsAsync(listingId));

        await Json(await father.GetAsync($"/api/v1/supervisor/shop/articles/{articleId}/listings"));
        Assert.True(await StockRowsAsync(listingId) > 0);
    }

    private async Task<int> StockRowsAsync(int listingId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        return await db.ShopListingStocks.CountAsync(s => s.ShopListingId == listingId);
    }

    /// <summary>
    /// The manual stock endpoint: above MaxStock → 400, a child outside the audience → 400, an unknown child → 404. Red when
    /// any of the three checks is dropped.
    /// </summary>
    [Fact]
    public async Task Bestand_Setzen_Prueft_Grenze_Zielgruppe_Und_Kind()
    {
        factory.Clock.FreezeAt(At(2026, 11, 19));
        var father = await TestApi.AdultAsync(factory);
        var a = await KidAsync(father, "4171");
        var b = await KidAsync(father, "4172");
        var (articleId, listingId) = await ListingAsync(father, new
        {
            coinPrice = 10,
            unitsPerPurchase = 1,
            maxStock = 3,
            refillKind = "Daily",
            childIds = new[] { a.Id },
        });

        var above = await SetStockAsync(father, articleId, listingId, a.Id, 4);
        Assert.Equal(HttpStatusCode.BadRequest, above.StatusCode);
        Assert.Equal("validation_error", await CodeAsync(above));
        var outside = await SetStockAsync(father, articleId, listingId, b.Id, 1);
        Assert.Equal(HttpStatusCode.BadRequest, outside.StatusCode);
        Assert.Contains("not meant for this child", (await outside.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await SetStockAsync(father, articleId, listingId, 987654, 1)).StatusCode);

        var ok = await Json(await SetStockAsync(father, articleId, listingId, a.Id, 1));
        Assert.Equal(1, ok.GetProperty("stocks").EnumerateArray().Single().GetProperty("currentStock").GetInt32());
        Assert.Equal(1, await StockAsync(a.Client, listingId));
    }

    /// <summary>
    /// Validation of the refill plan (AK 6 server side, AK 7 "a PATCH of the price alone"): monthly needs no extra field,
    /// weekly needs its weekday, the quantity lies in 1..MaxStock, a refilling listing needs MaxStock ≥ 1. A PATCH of
    /// MaxStock alone keeps an expiring listing expiring and caps a saving one.
    /// </summary>
    [Fact]
    public async Task Auffuellplan_Wird_Geprueft_Und_Patch_Behaelt_Die_Art()
    {
        factory.Clock.FreezeAt(At(2026, 11, 19));
        var father = await TestApi.AdultAsync(factory);
        var (articleId, expiring) = await ListingAsync(father, new { coinPrice = 10, unitsPerPurchase = 1, maxStock = 3, refillKind = "Daily" });
        var url = $"/api/v1/supervisor/shop/articles/{articleId}/listings";

        Assert.Equal(HttpStatusCode.Created, (await father.PostAsJsonAsync(url,
            new { coinPrice = 10, unitsPerPurchase = 1, maxStock = 1, refillKind = "Monthly" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await father.PostAsJsonAsync(url,
            new { coinPrice = 10, unitsPerPurchase = 1, maxStock = 1, refillKind = "Weekly" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await father.PostAsJsonAsync(url,
            new { coinPrice = 10, unitsPerPurchase = 1, maxStock = 8, refillQuantity = 9, refillKind = "Monthly" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await father.PostAsJsonAsync(url,
            new { coinPrice = 10, unitsPerPurchase = 1, maxStock = 0, refillKind = "Daily" })).StatusCode);

        var patched = await Json(await father.PatchAsJsonAsync($"{url}/{expiring}", new { maxStock = 5 }));
        Assert.Equal(5, patched.GetProperty("refillQuantity").GetInt32());

        var saving = (await Json(await father.PostAsJsonAsync(url,
            new { coinPrice = 10, unitsPerPurchase = 1, maxStock = 8, refillQuantity = 4, refillKind = "Monthly" }))).GetProperty("id").GetInt32();
        Assert.Equal(4, (await Json(await father.PatchAsJsonAsync($"{url}/{saving}", new { coinPrice = 20 }))).GetProperty("refillQuantity").GetInt32());
        // A cap below a saving quantity is refused, not clamped: clamping would silently make the listing "expires".
        Assert.Equal(HttpStatusCode.BadRequest, (await father.PatchAsJsonAsync($"{url}/{saving}", new { maxStock = 3 })).StatusCode);
        Assert.Equal(4, (await Json(await father.PatchAsJsonAsync($"{url}/{saving}", new { maxStock = 12 }))).GetProperty("refillQuantity").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest, (await father.PostAsJsonAsync(url,
            new { coinPrice = 10, unitsPerPurchase = 1, maxStock = 1, refillQuantity = -5 })).StatusCode);
    }

    /// <summary>
    /// AK 8: a euro listing is bought into the inventory as "Euro", and the approved redemption shows as paid out in the
    /// supervisor's history. Red when the unit is lost on the way into the inventory.
    /// </summary>
    [Fact]
    public async Task Euro_Kauf_Und_Auszahlung()
    {
        factory.Clock.FreezeAt(At(2026, 11, 19));
        var father = await TestApi.AdultAsync(factory);
        var a = await KidAsync(father, "4181");
        var (articleId, listingId) = await ListingAsync(father,
            new { title = "Taschengeld 5 €", coinPrice = 150, unitsPerPurchase = 5, maxStock = 8, refillQuantity = 4, refillKind = "Monthly" },
            unitType: "Euro");

        var view = await Json(await BuyAsync(a.Client, listingId));
        var inv = view.GetProperty("inventory").EnumerateArray().First(i => i.GetProperty("shopArticleId").GetInt32() == articleId);
        Assert.Equal("Euro", inv.GetProperty("unitType").GetString());
        Assert.Equal(5, inv.GetProperty("quantity").GetInt32());

        var requestId = await TestApi.IdAsync(await a.Client.PostAsJsonAsync(
            $"/api/v1/student/me/shop/inventory/{articleId}/activate", new { quantity = 5 }));
        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{a.Id}/shop/activations/{requestId}/approve", new { }))
            .EnsureSuccessStatusCode();
        var history = await Json(await father.GetAsync($"/api/v1/supervisor/children/{a.Id}/shop/activations?status=Approved"));
        var paid = history.EnumerateArray().Single(r => r.GetProperty("id").GetInt32() == requestId);
        Assert.Equal("Euro", paid.GetProperty("unitType").GetString());
        Assert.Equal("Child", paid.GetProperty("requestedBy").GetString());
    }

    /// <summary>
    /// Risk 3: the son's list loses a race against a parallel refill+purchase (the "winner" stores 7) and must answer with
    /// the stored 7, not its own unsaved 4 + 4 = 8. Red when the student path returns the loser's copy after the conflict
    /// instead of re-reading.
    /// </summary>
    [Fact]
    public async Task Sohnliste_Nach_Verlorenem_Konflikt_Zeigt_Den_Gespeicherten_Bestand()
    {
        factory.Clock.FreezeAt(At(2026, 10, 31));
        var father = await TestApi.AdultAsync(factory);
        var a = await KidAsync(father, "4191");
        var (articleId, listingId) = await ListingAsync(father, new
        {
            coinPrice = 10,
            unitsPerPurchase = 1,
            maxStock = 8,
            refillQuantity = 4,
            refillKind = "Monthly",
        });
        (await SetStockAsync(father, articleId, listingId, a.Id, 4)).EnsureSuccessStatusCode();

        string connectionString;
        using (var scope = factory.Services.CreateScope())
            connectionString = scope.ServiceProvider.GetRequiredService<PuglingDbContext>().Database.GetConnectionString()!;

        var now = new DateTime(2026, 11, 1, 9, 0, 0, DateTimeKind.Utc);
        var winner = new WinnerInterceptor(connectionString, listingId, a.Id, now);
        var options = new DbContextOptionsBuilder<PuglingDbContext>().UseSqlite(connectionString).AddInterceptors(winner).Options;
        await using var db = new PuglingDbContext(options);
        var shop = new ShopService(db, new WalletService(db));

        var listings = await shop.ListingsForStudentAsync(a.Id, activeOnly: true, now, CancellationToken.None);

        Assert.True(winner.Fired);
        Assert.Equal(7, listings.Single(v => v.Listing.Id == listingId).CurrentStock);
    }

    // Plays the parallel winner: right before the loser saves, another connection refills and buys one (8 - 1 = 7) and
    // bumps the stamp, so the loser's save fails on the concurrency token.
    private sealed class WinnerInterceptor(string connectionString, int listingId, int childId, DateTime now) : SaveChangesInterceptor
    {
        public bool Fired { get; private set; }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (!Fired)
            {
                Fired = true;
                await using var connection = new SqliteConnection(connectionString);
                await connection.OpenAsync(cancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandText = """
                    UPDATE ShopListingStocks SET CurrentStock = 7, LastRefilledAtUtc = $now, ConcurrencyStamp = $stamp
                    WHERE ShopListingId = $listing AND ChildId = $child
                    """;
                command.Parameters.AddWithValue("$now", now.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("$stamp", Guid.NewGuid().ToString().ToUpperInvariant());
                command.Parameters.AddWithValue("$listing", listingId);
                command.Parameters.AddWithValue("$child", childId);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    // ─── B-665 ───────────────────────────────────────────────────────────────

    private async Task<(HttpClient Father, (int Id, HttpClient Client) Kid, int ArticleId)> KidWithThirtyMinutesAsync(string pin)
    {
        factory.Clock.FreezeAt(At(2026, 11, 19));
        var father = await TestApi.AdultAsync(factory);
        var kid = await KidAsync(father, pin);
        var (articleId, listingId) = await ListingAsync(father, new { coinPrice = 10, unitsPerPurchase = 30, maxStock = 3 });
        (await BuyAsync(kid.Client, listingId)).EnsureSuccessStatusCode();
        return (father, kid, articleId);
    }

    private static Task<HttpResponseMessage> RedeemAsync(HttpClient supervisor, int childId, int articleId, int quantity) =>
        supervisor.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/shop/inventory/{articleId}/redeem", new { quantity });

    private static async Task<int> InventoryAsync(HttpClient father, int childId, int articleId) =>
        (await Json(await father.GetAsync($"/api/v1/supervisor/children/{childId}/shop/inventory")))
            .EnumerateArray().Where(i => i.GetProperty("shopArticleId").GetInt32() == articleId)
            .Select(i => i.GetProperty("quantity").GetInt32()).SingleOrDefault();

    /// <summary>
    /// B-665 AK 1 + AK 6: the father redeems 20 of 30 minutes - approved at once, inventory 10, and the son's list marks
    /// it as the father's while his own request stays "Child". Red when the inventory is not lowered, the request is left
    /// pending, or the requester is not recorded.
    /// </summary>
    [Fact]
    public async Task Vater_Loest_Ein_Inventar_Sinkt_Kind_Sieht_Es()
    {
        var (father, kid, articleId) = await KidWithThirtyMinutesAsync("4201");

        var redeemed = await Json(await RedeemAsync(father, kid.Id, articleId, 20));
        Assert.Equal("Approved", redeemed.GetProperty("status").GetString());
        Assert.Equal("Supervisor", redeemed.GetProperty("requestedBy").GetString());
        Assert.False(redeemed.GetProperty("canApprove").GetBoolean());
        Assert.Equal(10, await InventoryAsync(father, kid.Id, articleId));

        (await kid.Client.PostAsJsonAsync($"/api/v1/student/me/shop/inventory/{articleId}/activate", new { quantity = 5 }))
            .EnsureSuccessStatusCode();
        var mine = await Json(await kid.Client.GetAsync("/api/v1/student/me/shop/activations"));
        var bySupervisor = mine.EnumerateArray().Single(r => r.GetProperty("requestedBy").GetString() == "Supervisor");
        Assert.Equal(20, bySupervisor.GetProperty("requestedQuantity").GetInt32());
        Assert.Equal("Approved", bySupervisor.GetProperty("status").GetString());
        var byChild = mine.EnumerateArray().Single(r => r.GetProperty("requestedBy").GetString() == "Child");
        Assert.Equal("Pending", byChild.GetProperty("status").GetString());
    }

    /// <summary>
    /// B-665 AK 2 + AK 5: more than the inventory → insufficient_inventory, quantity 0 → validation_error; neither creates
    /// a request or touches the inventory. Red when the coverage check or the quantity check is dropped.
    /// </summary>
    [Fact]
    public async Task Vater_Loest_Ein_Prueft_Deckung_Und_Menge()
    {
        var (father, kid, articleId) = await KidWithThirtyMinutesAsync("4211");

        var tooMuch = await RedeemAsync(father, kid.Id, articleId, 40);
        Assert.Equal(HttpStatusCode.BadRequest, tooMuch.StatusCode);
        Assert.Equal("insufficient_inventory", await CodeAsync(tooMuch));
        var zero = await RedeemAsync(father, kid.Id, articleId, 0);
        Assert.Equal(HttpStatusCode.BadRequest, zero.StatusCode);
        Assert.Equal("validation_error", await CodeAsync(zero));

        Assert.Equal(30, await InventoryAsync(father, kid.Id, articleId));
        Assert.Empty((await Json(await father.GetAsync($"/api/v1/supervisor/children/{kid.Id}/shop/activations"))).EnumerateArray());
    }

    /// <summary>
    /// B-665 AK 3 + AK 4: Grandma (a co-supervisor) cannot redeem the father's article (404), a stranger cannot reach the
    /// child (404), and the son's token is no supervisor (403). Red when the article check accepts any supervisor of the
    /// child instead of the caller only.
    /// </summary>
    [Fact]
    public async Task Vater_Loest_Ein_Nur_Eigene_Artikel_Eigener_Kinder()
    {
        var (father, kid, articleId) = await KidWithThirtyMinutesAsync("4221");
        var grandmaId = await TestApi.IdAsync(await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = TestApi.UniqueName("Oma"), pin = "4222" }));
        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{kid.Id}/supervisors",
            new { supervisorId = grandmaId, relation = "Grandma" })).EnsureSuccessStatusCode();
        var grandma = await TestApi.AdultAsync(factory, grandmaId, "4222");
        var strangerId = await TestApi.IdAsync(await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = TestApi.UniqueName("Fremd"), pin = "4223" }));
        var stranger = await TestApi.AdultAsync(factory, strangerId, "4223");

        Assert.Equal(HttpStatusCode.NotFound, (await RedeemAsync(grandma, kid.Id, articleId, 10)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await RedeemAsync(stranger, kid.Id, articleId, 10)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await RedeemAsync(kid.Client, kid.Id, articleId, 10)).StatusCode);
        Assert.Equal(30, await InventoryAsync(father, kid.Id, articleId));
    }

    // ─── B-771: switching the refill kind restarts the clock ────────────────

    private static Task<HttpResponseMessage> PatchListingAsync(HttpClient father, int articleId, int listingId, object patch) =>
        father.PatchAsJsonAsync($"/api/v1/supervisor/shop/articles/{articleId}/listings/{listingId}", patch);

    /// <summary>
    /// AK 1: "none" → "monthly" after the child bought everything. Before the fix the switch filled the stock at once
    /// (a stock row without a refill instant owed one refill), so the child bought its pocket money twice in November.
    /// Red when the switch refills now, or when the 1st no longer refills.
    /// </summary>
    [Fact]
    public async Task B771_Umstellen_Auf_Monatlich_Fuellt_Erst_Zum_Monatsersten()
    {
        factory.Clock.FreezeAt(At(2026, 11, 5));
        var father = await TestApi.AdultAsync(factory);
        var kid = await KidAsync(father, "7711");
        var (articleId, listingId) = await ListingAsync(father, new
        {
            title = "Taschengeld",
            coinPrice = 10,
            gemPrice = 0,
            unitsPerPurchase = 5,
            maxStock = 2,
            refillKind = "None",
        });
        Assert.Equal(HttpStatusCode.OK, (await BuyAsync(kid.Client, listingId)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await BuyAsync(kid.Client, listingId)).StatusCode);

        factory.Clock.FreezeAt(At(2026, 11, 19));
        (await PatchListingAsync(father, articleId, listingId, new { refillKind = "Monthly" })).EnsureSuccessStatusCode();
        Assert.Equal(0, await StockAsync(kid.Client, listingId));

        factory.Clock.FreezeAt(At(2026, 11, 30, 23));
        Assert.Equal(0, await StockAsync(kid.Client, listingId));
        factory.Clock.FreezeAt(At(2026, 12, 1, 1));
        Assert.Equal(2, await StockAsync(kid.Client, listingId));
    }

    /// <summary>
    /// AK 2: "none" → "daily": not today, but tomorrow. Red when the switch refills at once or the clock never starts.
    /// </summary>
    [Fact]
    public async Task B771_Umstellen_Auf_Taeglich_Fuellt_Am_Naechsten_Tag()
    {
        factory.Clock.FreezeAt(At(2026, 11, 10, 9));
        var father = await TestApi.AdultAsync(factory);
        var kid = await KidAsync(father, "7712");
        var (articleId, listingId) = await ListingAsync(father, new
        {
            title = "Spielzeit",
            coinPrice = 10,
            gemPrice = 0,
            unitsPerPurchase = 30,
            maxStock = 1,
            refillKind = "None",
        });
        Assert.Equal(HttpStatusCode.OK, (await BuyAsync(kid.Client, listingId)).StatusCode);

        factory.Clock.FreezeAt(At(2026, 11, 10, 15));
        (await PatchListingAsync(father, articleId, listingId, new { refillKind = "Daily" })).EnsureSuccessStatusCode();
        Assert.Equal(0, await StockAsync(kid.Client, listingId));
        factory.Clock.FreezeAt(At(2026, 11, 11, 8));
        Assert.Equal(1, await StockAsync(kid.Client, listingId));
    }

    /// <summary>
    /// AK 3: "daily" → "monthly" while yesterday's daily refill is still owed (nobody read the shop since). The owed refill
    /// is booked under the old rule before the new one starts. Red when the switch drops what the child already earned.
    /// </summary>
    [Fact]
    public async Task B771_Umstellen_Bucht_Die_Faellige_Auffuellung_Der_Alten_Regel()
    {
        factory.Clock.FreezeAt(At(2026, 11, 3, 9));
        var father = await TestApi.AdultAsync(factory);
        var kid = await KidAsync(father, "7713");
        var (articleId, listingId) = await ListingAsync(father, new
        {
            title = "Spielzeit",
            coinPrice = 10,
            gemPrice = 0,
            unitsPerPurchase = 30,
            maxStock = 1,
            refillKind = "Daily",
        });
        Assert.Equal(HttpStatusCode.OK, (await BuyAsync(kid.Client, listingId)).StatusCode);

        factory.Clock.FreezeAt(At(2026, 11, 4, 9));
        (await PatchListingAsync(father, articleId, listingId, new { refillKind = "Monthly" })).EnsureSuccessStatusCode();
        Assert.Equal(1, await StockAsync(kid.Client, listingId));
        Assert.Equal(HttpStatusCode.OK, (await BuyAsync(kid.Client, listingId)).StatusCode);
        factory.Clock.FreezeAt(At(2026, 11, 5, 9));
        Assert.Equal(0, await StockAsync(kid.Client, listingId));
    }

    /// <summary>
    /// AK 5 (review finding): a weekly listing moved from Monday to Friday on a Saturday does not refill at once - the Friday
    /// before the switch is not a refill of the new calendar. Red when only a kind change restarts the clock.
    /// </summary>
    [Fact]
    public async Task B771_Neuer_Wochentag_Fuellt_Erst_Am_Neuen_Tag()
    {
        factory.Clock.FreezeAt(At(2026, 11, 16, 8)); // Monday
        var father = await TestApi.AdultAsync(factory);
        var kid = await KidAsync(father, "7715");
        var (articleId, listingId) = await ListingAsync(father, new
        {
            title = "Kino",
            coinPrice = 10,
            gemPrice = 0,
            unitsPerPurchase = 1,
            maxStock = 1,
            refillKind = "Weekly",
            refillDayOfWeek = "Monday",
        });
        Assert.Equal(HttpStatusCode.OK, (await BuyAsync(kid.Client, listingId)).StatusCode);

        factory.Clock.FreezeAt(At(2026, 11, 21)); // Saturday
        (await PatchListingAsync(father, articleId, listingId, new { refillDayOfWeek = "Friday" })).EnsureSuccessStatusCode();
        Assert.Equal(0, await StockAsync(kid.Client, listingId));
        factory.Clock.FreezeAt(At(2026, 11, 27, 1)); // Friday
        Assert.Equal(1, await StockAsync(kid.Client, listingId));
    }

    /// <summary>
    /// AK 1 for a recurring old kind: weekly (last refill on Friday 30 Oct) → monthly on 4 Nov. The old rule owes nothing yet,
    /// the new one would count 1 Nov - a boundary before the switch. Red when the restart is limited to stocks without an
    /// instant (green before the fix as well: the defect needed a missing instant, this guards the chosen repair).
    /// </summary>
    [Fact]
    public async Task B771_Woechentlich_Auf_Monatlich_Zaehlt_Keine_Grenze_Vor_Der_Umstellung()
    {
        factory.Clock.FreezeAt(At(2026, 10, 30, 8)); // Friday
        var father = await TestApi.AdultAsync(factory);
        var kid = await KidAsync(father, "7716");
        var (articleId, listingId) = await ListingAsync(father, new
        {
            title = "Taschengeld",
            coinPrice = 10,
            gemPrice = 0,
            unitsPerPurchase = 5,
            maxStock = 1,
            refillKind = "Weekly",
            refillDayOfWeek = "Friday",
        });
        Assert.Equal(HttpStatusCode.OK, (await BuyAsync(kid.Client, listingId)).StatusCode);

        factory.Clock.FreezeAt(At(2026, 11, 4)); // Wednesday
        (await PatchListingAsync(father, articleId, listingId, new { refillKind = "Monthly" })).EnsureSuccessStatusCode();
        Assert.Equal(0, await StockAsync(kid.Client, listingId));
        factory.Clock.FreezeAt(At(2026, 12, 1, 1));
        Assert.Equal(1, await StockAsync(kid.Client, listingId));
    }

    /// <summary>
    /// AK 4: "none" → "once" with a future date still fires on that date - the restart is for recurring kinds only, "once"
    /// needs the empty refill instant. Red when the switch stamps every kind. (Green before the fix as well: it guards the
    /// fix, not the defect.)
    /// </summary>
    [Fact]
    public async Task B771_Umstellen_Auf_Einmalig_Loest_Zum_Termin_Aus()
    {
        factory.Clock.FreezeAt(At(2026, 11, 10));
        var father = await TestApi.AdultAsync(factory);
        var kid = await KidAsync(father, "7714");
        var (articleId, listingId) = await ListingAsync(father, new
        {
            title = "Kino",
            coinPrice = 10,
            gemPrice = 0,
            unitsPerPurchase = 1,
            maxStock = 1,
            refillKind = "None",
        });
        Assert.Equal(HttpStatusCode.OK, (await BuyAsync(kid.Client, listingId)).StatusCode);

        (await PatchListingAsync(father, articleId, listingId, new { refillKind = "Once", refillAtUtc = "2026-11-15T00:00:00Z" }))
            .EnsureSuccessStatusCode();
        Assert.Equal(0, await StockAsync(kid.Client, listingId));
        factory.Clock.FreezeAt(At(2026, 11, 15, 1));
        Assert.Equal(1, await StockAsync(kid.Client, listingId));
    }

    // ─── B-776: switching to "once" or a new date empties the refill instant ───

    // The instant as text: a "once" date read back from SQLite is serialized without an offset, so a local conversion
    // would shift it by the test machine's time zone.
    private static async Task<string?> FatherNextRefillAsync(HttpClient father, int articleId, int listingId, int childId)
    {
        var view = await Json(await father.GetAsync($"/api/v1/supervisor/shop/articles/{articleId}/listings/{listingId}"));
        var next = view.GetProperty("stocks").EnumerateArray().Single(s => s.GetProperty("childId").GetInt32() == childId)
            .GetProperty("nextRefillAtUtc");
        return next.ValueKind == JsonValueKind.Null ? null : next.GetString()![..19];
    }

    /// <summary>
    /// AK 1: "daily" → "once in five days" after a sell-out. The daily stock carries a refill instant, and "once" only fires
    /// on an empty one - before the fix the date never came and the father read "no further refill". Red when the switch
    /// leaves the instant in place.
    /// </summary>
    [Fact]
    public async Task B776_Taeglich_Auf_Einmalig_Fuellt_Zum_Termin()
    {
        factory.Clock.FreezeAt(At(2026, 11, 20, 10));
        var father = await TestApi.AdultAsync(factory);
        var kid = await KidAsync(father, "7761");
        var (articleId, listingId) = await ListingAsync(father, new
        {
            title = "Spielzeit",
            coinPrice = 10,
            gemPrice = 0,
            unitsPerPurchase = 1,
            maxStock = 2,
            refillKind = "Daily",
        });
        Assert.Equal(HttpStatusCode.OK, (await BuyAsync(kid.Client, listingId)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await BuyAsync(kid.Client, listingId)).StatusCode);

        (await PatchListingAsync(father, articleId, listingId, new { refillKind = "Once", refillAtUtc = "2026-11-25T00:00:00Z" }))
            .EnsureSuccessStatusCode();
        Assert.Equal(0, await StockAsync(kid.Client, listingId));
        Assert.Equal("2026-11-25T00:00:00", await FatherNextRefillAsync(father, articleId, listingId, kid.Id));

        factory.Clock.FreezeAt(At(2026, 11, 24, 23));
        Assert.Equal(0, await StockAsync(kid.Client, listingId));
        factory.Clock.FreezeAt(At(2026, 11, 25, 1));
        Assert.Equal(2, await StockAsync(kid.Client, listingId));
    }

    private async Task<(HttpClient Father, (int Id, HttpClient Client) Kid, int ArticleId, int ListingId)> FiredOnceSoldOutAsync(
        string pin)
    {
        factory.Clock.FreezeAt(At(2026, 11, 10));
        var father = await TestApi.AdultAsync(factory);
        var kid = await KidAsync(father, pin);
        var (articleId, listingId) = await ListingAsync(father, new
        {
            title = "Kino",
            coinPrice = 10,
            gemPrice = 0,
            unitsPerPurchase = 1,
            maxStock = 1,
            refillKind = "Once",
            refillAtUtc = "2026-11-12T00:00:00Z",
        });
        factory.Clock.FreezeAt(At(2026, 11, 12, 10));
        Assert.Equal(1, await StockAsync(kid.Client, listingId));
        Assert.Equal(HttpStatusCode.OK, (await BuyAsync(kid.Client, listingId)).StatusCode);
        factory.Clock.FreezeAt(At(2026, 11, 13));
        return (father, kid, articleId, listingId);
    }

    /// <summary>
    /// AK 2: a fired "once" gets a new date and fires again on it, not before. Red when only a change of the kind empties
    /// the instant.
    /// </summary>
    [Fact]
    public async Task B776_Neuer_Termin_Fuellt_Erneut_Zum_Neuen_Termin()
    {
        var (father, kid, articleId, listingId) = await FiredOnceSoldOutAsync("7762");

        (await PatchListingAsync(father, articleId, listingId, new { refillAtUtc = "2026-11-15T00:00:00Z" })).EnsureSuccessStatusCode();
        Assert.Equal(0, await StockAsync(kid.Client, listingId));
        factory.Clock.FreezeAt(At(2026, 11, 14, 23));
        Assert.Equal(0, await StockAsync(kid.Client, listingId));
        factory.Clock.FreezeAt(At(2026, 11, 15, 1));
        Assert.Equal(1, await StockAsync(kid.Client, listingId));
    }

    /// <summary>
    /// AK 3: a price change on a fired "once" that sends the unchanged date along does not refill. Red when every PATCH of a
    /// "once" listing empties the instant (each later price change would hand the stock out again).
    /// </summary>
    [Fact]
    public async Task B776_Gleicher_Termin_Fuellt_Nicht_Erneut()
    {
        var (father, kid, articleId, listingId) = await FiredOnceSoldOutAsync("7763");

        (await PatchListingAsync(father, articleId, listingId, new
        {
            coinPrice = 20,
            refillKind = "Once",
            refillAtUtc = "2026-11-12T00:00:00Z",
        })).EnsureSuccessStatusCode();
        Assert.Equal(0, await StockAsync(kid.Client, listingId));
        factory.Clock.FreezeAt(At(2026, 11, 20));
        Assert.Equal(0, await StockAsync(kid.Client, listingId));
    }

    /// <summary>
    /// B-782 AK 4: a "once" date with a time of day (set by an API caller), fired and sold out. The Vater-Web sends the date
    /// it loaded back unchanged with a price change - the server reads no new date and refills nothing. The same PATCH
    /// with the date cut to midnight, as the Vater-Web sent it before B-782, is a new date and refills (B-776): the case
    /// shows that the round trip, not the comparison, carries the fix. Red when <c>resetsOnceInstant</c> compares anything
    /// but the instant the client sent back.
    /// </summary>
    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    public async Task B782_Termin_Mit_Uhrzeit_Unveraendert_Zurueck_Fuellt_Nicht_Erneut(bool sendLoaded, int expectedStock)
    {
        factory.Clock.FreezeAt(At(2026, 11, 10));
        var father = await TestApi.AdultAsync(factory);
        var kid = await KidAsync(father, sendLoaded ? "7821" : "7822");
        var (articleId, listingId) = await ListingAsync(father, new
        {
            title = "Kino",
            coinPrice = 10,
            gemPrice = 0,
            unitsPerPurchase = 1,
            maxStock = 1,
            refillKind = "Once",
            refillAtUtc = "2026-11-12T10:00:00Z",
        });
        factory.Clock.FreezeAt(At(2026, 11, 12, 12));
        Assert.Equal(1, await StockAsync(kid.Client, listingId));
        Assert.Equal(HttpStatusCode.OK, (await BuyAsync(kid.Client, listingId)).StatusCode);
        factory.Clock.FreezeAt(At(2026, 11, 13));

        var loaded = (await Json(await father.GetAsync($"/api/v1/supervisor/shop/articles/{articleId}/listings/{listingId}")))
            .GetProperty("refillAtUtc").GetString()!;
        Assert.StartsWith("2026-11-12T10:00:00", loaded);
        (await PatchListingAsync(father, articleId, listingId, new
        {
            coinPrice = 20,
            refillKind = "Once",
            refillAtUtc = sendLoaded ? loaded : "2026-11-12T00:00:00Z",
        })).EnsureSuccessStatusCode();
        Assert.Equal(expectedStock, await StockAsync(kid.Client, listingId));
    }

    /// <summary>
    /// AK 4: "daily" → "once" while today's daily refill is still unread. The old rule books it before the instant is
    /// emptied, so the child keeps what it earned. Red when the switch to "once" skips booking the old rule.
    /// </summary>
    [Fact]
    public async Task B776_Umstellen_Auf_Einmalig_Bucht_Die_Alte_Regel()
    {
        factory.Clock.FreezeAt(At(2026, 11, 3, 9));
        var father = await TestApi.AdultAsync(factory);
        var kid = await KidAsync(father, "7764");
        var (articleId, listingId) = await ListingAsync(father, new
        {
            title = "Spielzeit",
            coinPrice = 10,
            gemPrice = 0,
            unitsPerPurchase = 30,
            maxStock = 1,
            refillKind = "Daily",
        });
        Assert.Equal(HttpStatusCode.OK, (await BuyAsync(kid.Client, listingId)).StatusCode);

        factory.Clock.FreezeAt(At(2026, 11, 4, 9));
        (await PatchListingAsync(father, articleId, listingId, new { refillKind = "Once", refillAtUtc = "2026-11-09T00:00:00Z" }))
            .EnsureSuccessStatusCode();
        Assert.Equal(1, await StockAsync(kid.Client, listingId));
        Assert.Equal(HttpStatusCode.OK, (await BuyAsync(kid.Client, listingId)).StatusCode);
        factory.Clock.FreezeAt(At(2026, 11, 8, 23));
        Assert.Equal(0, await StockAsync(kid.Client, listingId));
        factory.Clock.FreezeAt(At(2026, 11, 9, 1));
        Assert.Equal(1, await StockAsync(kid.Client, listingId));
    }

    /// <summary>
    /// Review finding (B-776): a listing that was once a one-off keeps its old date. Switching it back to "once" without a
    /// date is refused instead of firing on that stale date at once; with a date it works. Red when the switch reuses the
    /// stored date.
    /// </summary>
    [Fact]
    public async Task B776_Wechsel_Auf_Einmalig_Verlangt_Ein_Datum()
    {
        var (father, kid, articleId, listingId) = await FiredOnceSoldOutAsync("7765");
        (await PatchListingAsync(father, articleId, listingId, new { refillKind = "Daily" })).EnsureSuccessStatusCode();

        var stale = await PatchListingAsync(father, articleId, listingId, new { refillKind = "Once" });
        Assert.Equal(HttpStatusCode.BadRequest, stale.StatusCode);
        Assert.Equal("validation_error", await CodeAsync(stale));
        Assert.Equal(0, await StockAsync(kid.Client, listingId));

        (await PatchListingAsync(father, articleId, listingId, new { refillKind = "Once", refillAtUtc = "2026-11-16T00:00:00Z" }))
            .EnsureSuccessStatusCode();
        Assert.Equal(0, await StockAsync(kid.Client, listingId));
    }

    // ─── B-772: the answer of "set stock" shows the siblings as they are ───

    /// <summary>
    /// AK 1-3: two children sold out a daily listing yesterday, a third was linked today and has no stock row. Setting one
    /// child's stock answers with the sibling refilled (AK 1) and the newcomer at its start stock (AK 2), and writes neither
    /// of them (AK 3: the row count stays at two). Red for AK 1/2 when the answer is built from the loaded rows only, red for
    /// AK 3 when every child is materialized before the save.
    /// </summary>
    [Fact]
    public async Task B772_Bestand_Setzen_Zeigt_Geschwister_Aktuell_Und_Schreibt_Sie_Nicht()
    {
        factory.Clock.FreezeAt(At(2026, 11, 19, 10));
        var father = await TestApi.AdultAsync(factory);
        var a = await KidAsync(father, "7721");
        var b = await KidAsync(father, "7722");
        var (articleId, listingId) = await ListingAsync(father, new
        {
            coinPrice = 10,
            gemPrice = 0,
            unitsPerPurchase = 1,
            maxStock = 1,
            refillKind = "Daily",
        });
        Assert.Equal(HttpStatusCode.OK, (await BuyAsync(a.Client, listingId)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await BuyAsync(b.Client, listingId)).StatusCode);
        Assert.Equal(2, await StockRowsAsync(listingId));

        factory.Clock.FreezeAt(At(2026, 11, 20, 10));
        var c = await KidAsync(father, "7723");
        var answer = await Json(await SetStockAsync(father, articleId, listingId, a.Id, 0));
        int StockOf(int childId) => answer.GetProperty("stocks").EnumerateArray()
            .Single(s => s.GetProperty("childId").GetInt32() == childId).GetProperty("currentStock").GetInt32();

        Assert.Equal(0, StockOf(a.Id));
        Assert.Equal(1, StockOf(b.Id));
        Assert.Equal(1, StockOf(c.Id));
        Assert.Equal(2, await StockRowsAsync(listingId));
    }
}
