using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;

namespace Pugling.Api.Tests;

/// <summary>
/// B-593: the child pins one listing of its family shop as a savings goal and reads how far away it is - in coins from
/// the real balance and as a forecast in days at its pace of the seven full days before today. The goal only reads:
/// it books nothing and leaves the child's concurrency stamp alone. Every case works on its own fresh child so the
/// balance and the first ledger entry are the case's alone.
/// </summary>
public class SparzielTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private const string Goal = "/api/v1/student/me/savings-goal";

    // Wednesday; the calendar week started on Monday 2025-03-10, the rolling span is 2025-03-05 .. 2025-03-11.
    private static readonly DateTime Now = new(2025, 3, 12, 12, 0, 0, DateTimeKind.Utc);

    private static DateTime Day(int month, int day) => new(2025, month, day, 10, 0, 0, DateTimeKind.Utc);

    private static async Task<JsonElement> Json(HttpResponseMessage res)
    {
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<(int Id, HttpClient Client)> KidAsync(HttpClient father, string pin)
    {
        var id = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Sparkind"), pin }));
        return (id, await TestApi.ChildAsync(factory, id, pin));
    }

    private static async Task<(int ArticleId, int ListingId)> ListingAsync(HttpClient father, object listing)
    {
        var number = TestApi.UniqueName("Z");
        var articleId = (await Json(await father.PostAsJsonAsync("/api/v1/supervisor/shop/articles",
            new { articleNumber = number, title = "Artikel " + number, unitType = "Stueck", actionType = "Sonstiges" })))
            .GetProperty("id").GetInt32();
        var res = await father.PostAsJsonAsync($"/api/v1/supervisor/shop/articles/{articleId}/listings", listing);
        return (articleId, (await Json(res)).GetProperty("id").GetInt32());
    }

    private static Task<HttpResponseMessage> PinAsync(HttpClient child, int listingId) =>
        child.PutAsJsonAsync(Goal, new { listingId });

    private (int? ListingId, string? Title, Guid Stamp, int Entries, int Sum) Row(int childId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var c = db.Children.AsNoTracking().Single(x => x.Id == childId);
        var entries = db.ChildPointsEntries.AsNoTracking().Where(p => p.ChildId == childId).ToList();
        return (c.SavingsGoalListingId, c.SavingsGoalTitle, c.ConcurrencyStamp, entries.Count, entries.Sum(p => p.Amount));
    }

    private static int? IntOrNull(JsonElement e, string name) =>
        e.GetProperty(name).ValueKind == JsonValueKind.Null ? null : e.GetProperty(name).GetInt32();

    /// <summary>
    /// AK 1: a listing of the own shop is pinned (200), a listing meant only for a sibling and an inactive listing are
    /// unknown (404). Red when the pin checks only that the listing exists instead of going through the child's own
    /// shop view (<c>ListingsForStudentAsync</c>, active only).
    /// </summary>
    [Fact]
    public async Task Nur_Ein_Angebot_Aus_Dem_Eigenen_Shop_Wird_Ziel()
    {
        factory.Clock.FreezeAt(Now);
        var father = await TestApi.AdultAsync(factory);
        var kid = await KidAsync(father, "5931");
        var sibling = await KidAsync(father, "5932");
        var (_, own) = await ListingAsync(father, new { title = "Kino", coinPrice = 100, unitsPerPurchase = 1, maxStock = 3 });
        var (_, siblings) = await ListingAsync(father, new
        {
            coinPrice = 50,
            unitsPerPurchase = 1,
            maxStock = 3,
            childIds = new[] { sibling.Id },
        });
        var (inactiveArticle, inactive) = await ListingAsync(father, new { coinPrice = 50, unitsPerPurchase = 1, maxStock = 3 });
        (await father.PatchAsJsonAsync($"/api/v1/supervisor/shop/articles/{inactiveArticle}/listings/{inactive}",
            new { active = false })).EnsureSuccessStatusCode();

        var pinned = await Json(await PinAsync(kid.Client, own));
        Assert.Equal(own, pinned.GetProperty("listingId").GetInt32());
        Assert.Equal("Kino", pinned.GetProperty("title").GetString());
        Assert.True(pinned.GetProperty("available").GetBoolean());

        Assert.Equal(HttpStatusCode.NotFound, (await PinAsync(kid.Client, siblings)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PinAsync(kid.Client, inactive)).StatusCode);
        Assert.Equal(own, Row(kid.Id).ListingId);
    }

    /// <summary>
    /// AK 2: the missing coins count from the real balance - 30 coins against a price of 100 is 70 missing, a debt of
    /// 20 makes it 120. Red when the balance is floored at 0 (the debt would vanish: 100) or the debt is left out.
    /// </summary>
    [Fact]
    public async Task Fehlbetrag_Zaehlt_Ab_Dem_Echten_Stand_Auch_Bei_Schulden()
    {
        factory.Clock.FreezeAt(Now);
        var father = await TestApi.AdultAsync(factory);
        var kid = await KidAsync(father, "5933");
        var (_, listing) = await ListingAsync(father, new { coinPrice = 100, unitsPerPurchase = 1, maxStock = 3 });
        TestApi.Book(factory, kid.Id, 30, PointKind.Base, Day(3, 1));
        await PinAsync(kid.Client, listing);

        Assert.Equal(70, (await Json(await kid.Client.GetAsync(Goal))).GetProperty("missingCoins").GetInt32());

        TestApi.Book(factory, kid.Id, -50, PointKind.GoalPenalty, Day(3, 2));
        var inDebt = await Json(await kid.Client.GetAsync(Goal));
        Assert.Equal(-20, inDebt.GetProperty("coins").GetInt32());
        Assert.Equal(120, inDebt.GetProperty("missingCoins").GetInt32());
    }

    /// <summary>
    /// AK 3 and AK 4, the computed case. Earned in the rolling span: 35 on Thursday 03-06 (the previous calendar week).
    /// Outside it: 7 on 03-04 (the day before the span) and 14 today. Not earned: a gift of 20 and a positive fine
    /// correction of 30. Balance 96, price 166, 70 missing, pace 35/7 = 5 a day: 14 days. Red when the span is the calendar week (no number), when today
    /// counts (10), when the day before the span counts (12), or when the gift or the correction counts (9 or 8).
    /// </summary>
    [Fact]
    public async Task Tage_Beim_Tempo_Der_Sieben_Vollen_Tage_Vor_Heute()
    {
        factory.Clock.FreezeAt(Now);
        var father = await TestApi.AdultAsync(factory);
        var kid = await KidAsync(father, "5934");
        var (_, listing) = await ListingAsync(father, new { coinPrice = 166, unitsPerPurchase = 1, maxStock = 3 });
        TestApi.Book(factory, kid.Id, 7, PointKind.Base, Day(3, 4));
        TestApi.Book(factory, kid.Id, 35, PointKind.Base, Day(3, 6));
        TestApi.Book(factory, kid.Id, 20, PointKind.Manual, Day(3, 7));
        TestApi.Book(factory, kid.Id, 30, PointKind.GoalPenalty, Day(3, 8));
        TestApi.Book(factory, kid.Id, -10, PointKind.GoalPenalty, Day(3, 9));
        TestApi.Book(factory, kid.Id, 14, PointKind.Base, Now.AddHours(-1));
        await PinAsync(kid.Client, listing);

        var goal = await Json(await kid.Client.GetAsync(Goal));

        Assert.Equal(96, goal.GetProperty("coins").GetInt32());
        Assert.Equal(70, goal.GetProperty("missingCoins").GetInt32());
        Assert.Equal(14, goal.GetProperty("daysAtPace").GetInt32());
    }

    /// <summary>
    /// AK 3, the null cases and the boundary of the history. A first entry on 03-06 is younger than the seven full days
    /// (null); one on 03-05, the first day of the span, is old enough (35 earned there, 70 missing: 14 days). Nothing
    /// earned in the span - only a gift - is null without a division by zero. A pure gem price has missing gems but no
    /// days. Red when a short history yields a number, when the boundary is off by one day, or when the gem price is
    /// converted.
    /// </summary>
    [Fact]
    public async Task Keine_Tage_Bei_Kurzem_Verlauf_Ohne_Verdienst_Und_Bei_Gem_Preis()
    {
        factory.Clock.FreezeAt(Now);
        var father = await TestApi.AdultAsync(factory);
        var (_, coins) = await ListingAsync(father, new { coinPrice = 105, unitsPerPurchase = 1, maxStock = 3 });
        var (_, gems) = await ListingAsync(father, new { coinPrice = 0, gemPrice = 5, unitsPerPurchase = 1, maxStock = 3 });

        var shortHistory = await KidAsync(father, "5935");
        TestApi.Book(factory, shortHistory.Id, 35, PointKind.Base, Day(3, 6));
        await PinAsync(shortHistory.Client, coins);
        var shortGoal = await Json(await shortHistory.Client.GetAsync(Goal));
        Assert.Equal(70, shortGoal.GetProperty("missingCoins").GetInt32());
        Assert.Null(IntOrNull(shortGoal, "daysAtPace"));

        var exactlySeven = await KidAsync(father, "5936");
        TestApi.Book(factory, exactlySeven.Id, 35, PointKind.Base, Day(3, 5));
        await PinAsync(exactlySeven.Client, coins);
        Assert.Equal(14, IntOrNull(await Json(await exactlySeven.Client.GetAsync(Goal)), "daysAtPace"));

        var giftOnly = await KidAsync(father, "5937");
        TestApi.Book(factory, giftOnly.Id, 35, PointKind.Manual, Day(3, 1));
        await PinAsync(giftOnly.Client, coins);
        var giftGoal = await Json(await giftOnly.Client.GetAsync(Goal));
        Assert.Equal(70, giftGoal.GetProperty("missingCoins").GetInt32());
        Assert.Null(IntOrNull(giftGoal, "daysAtPace"));

        TestApi.Book(factory, exactlySeven.Id, 2, PointKind.Combo, Day(3, 6));
        await PinAsync(exactlySeven.Client, gems);
        var gemGoal = await Json(await exactlySeven.Client.GetAsync(Goal));
        Assert.Equal(3, gemGoal.GetProperty("missingGems").GetInt32());
        Assert.Equal(0, gemGoal.GetProperty("missingCoins").GetInt32());
        Assert.Null(IntOrNull(gemGoal, "daysAtPace"));
    }

    /// <summary>
    /// AK 5 and AK 6. A price raised by the supervisor shows in the next read. A deactivated listing keeps the goal with
    /// <c>available: false</c>; a deleted listing - and a deleted article, which takes its listings along - leaves the
    /// title snapshot with no id and <c>available: false</c>, not a silent 204. Red when the price is copied onto the
    /// child, or when the delete clears the goal without a title left.
    /// </summary>
    [Fact]
    public async Task Preis_Schlaegt_Durch_Und_Ein_Verschwundenes_Angebot_Bleibt_Sichtbar()
    {
        factory.Clock.FreezeAt(Now);
        var father = await TestApi.AdultAsync(factory);
        var kid = await KidAsync(father, "5938");
        var (articleId, listing) = await ListingAsync(father, new { title = "Kino", coinPrice = 100, unitsPerPurchase = 1, maxStock = 3 });
        await PinAsync(kid.Client, listing);

        (await father.PatchAsJsonAsync($"/api/v1/supervisor/shop/articles/{articleId}/listings/{listing}",
            new { coinPrice = 140 })).EnsureSuccessStatusCode();
        Assert.Equal(140, (await Json(await kid.Client.GetAsync(Goal))).GetProperty("missingCoins").GetInt32());

        (await father.PatchAsJsonAsync($"/api/v1/supervisor/shop/articles/{articleId}/listings/{listing}",
            new { active = false })).EnsureSuccessStatusCode();
        var inactive = await Json(await kid.Client.GetAsync(Goal));
        Assert.False(inactive.GetProperty("available").GetBoolean());
        Assert.Equal(listing, inactive.GetProperty("listingId").GetInt32());

        (await father.DeleteAsync($"/api/v1/supervisor/shop/articles/{articleId}/listings/{listing}")).EnsureSuccessStatusCode();
        var deleted = await Json(await kid.Client.GetAsync(Goal));
        Assert.Equal(JsonValueKind.Null, deleted.GetProperty("listingId").ValueKind);
        Assert.Equal("Kino", deleted.GetProperty("title").GetString());
        Assert.False(deleted.GetProperty("available").GetBoolean());

        var (otherArticle, other) = await ListingAsync(father, new { title = "Eis", coinPrice = 30, unitsPerPurchase = 1, maxStock = 3 });
        await PinAsync(kid.Client, other);
        (await father.DeleteAsync($"/api/v1/supervisor/shop/articles/{otherArticle}")).EnsureSuccessStatusCode();
        var articleGone = await Json(await kid.Client.GetAsync(Goal));
        Assert.Equal(JsonValueKind.Null, articleGone.GetProperty("listingId").ValueKind);
        Assert.Equal("Eis", articleGone.GetProperty("title").GetString());
        Assert.False(articleGone.GetProperty("available").GetBoolean());
    }

    /// <summary>
    /// AK 7 and AK 8: a second pin replaces the first, removing clears pointer and title (204 afterwards). Pin, replace
    /// and remove leave balance, ledger and the concurrency stamp unchanged. Red when two goals can stand, the title
    /// stays behind, or any of the three writes the stamp or books a ledger entry.
    /// </summary>
    [Fact]
    public async Task Ersetzen_Und_Entfernen_Buchen_Nichts()
    {
        factory.Clock.FreezeAt(Now);
        var father = await TestApi.AdultAsync(factory);
        var kid = await KidAsync(father, "5939");
        TestApi.Book(factory, kid.Id, 40, PointKind.Base, Day(3, 1));
        var (_, first) = await ListingAsync(father, new { title = "Erstes", coinPrice = 100, unitsPerPurchase = 1, maxStock = 3 });
        var (_, second) = await ListingAsync(father, new { title = "Zweites", coinPrice = 60, unitsPerPurchase = 1, maxStock = 3 });
        var before = Row(kid.Id);

        await PinAsync(kid.Client, first);
        var replaced = await Json(await PinAsync(kid.Client, second));
        Assert.Equal(second, replaced.GetProperty("listingId").GetInt32());
        var pinned = Row(kid.Id);
        Assert.Equal((second, "Zweites"), (pinned.ListingId, pinned.Title));

        Assert.Equal(HttpStatusCode.NoContent, (await kid.Client.DeleteAsync(Goal)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await kid.Client.GetAsync(Goal)).StatusCode);
        var after = Row(kid.Id);
        Assert.Null(after.ListingId);
        Assert.Null(after.Title);
        Assert.Equal((before.Stamp, before.Entries, before.Sum), (after.Stamp, after.Entries, after.Sum));
        Assert.Equal((before.Stamp, before.Entries, before.Sum), (pinned.Stamp, pinned.Entries, pinned.Sum));
    }

    /// <summary>
    /// AK 9: the supervisor reads the child's goal under his own route (204 without one), cannot pin in the child's
    /// place (403 on the student route), and a supervisor who does not look after the child gets 404. Neither the pin nor
    /// the reads write the child's stock rows. Red when the student route lets a supervisor through, the supervisor
    /// route skips the ownership filter, or the goal reads the shop with <c>persist: true</c>.
    /// </summary>
    [Fact]
    public async Task Betreuer_Liest_Das_Ziel_Setzt_Es_Aber_Nicht()
    {
        factory.Clock.FreezeAt(Now);
        var father = await TestApi.AdultAsync(factory);
        var kid = await KidAsync(father, "5940");
        var (_, listing) = await ListingAsync(father, new { title = "Kino", coinPrice = 100, unitsPerPurchase = 1, maxStock = 3 });
        var url = $"/api/v1/supervisor/children/{kid.Id}/savings-goal";
        Assert.Equal(HttpStatusCode.NoContent, (await father.GetAsync(url)).StatusCode);

        await PinAsync(kid.Client, listing);
        var read = await Json(await father.GetAsync(url));
        Assert.Equal(listing, read.GetProperty("listingId").GetInt32());
        Assert.Equal(100, read.GetProperty("missingCoins").GetInt32());

        // Review finding: reading the goal must not write the child's stock rows - a GET has no write side effect.
        using (var scope = factory.Services.CreateScope())
            Assert.False(scope.ServiceProvider.GetRequiredService<PuglingDbContext>().ShopListingStocks
                .Any(s => s.ChildId == kid.Id));

        Assert.Equal(HttpStatusCode.Forbidden, (await PinAsync(father, listing)).StatusCode);
        var regId = await TestApi.IdAsync(await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = TestApi.UniqueName("Fremd"), email = (string?)null, pin = "5941" }));
        var stranger = await TestApi.AdultAsync(factory, regId, "5941");
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync(url)).StatusCode);
    }
}
