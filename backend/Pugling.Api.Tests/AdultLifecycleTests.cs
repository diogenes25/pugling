using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;

namespace Pugling.Api.Tests;

/// <summary>
/// Lifecycle of an adult: registering, rejecting a duplicate address, deleting oneself.
/// <para>
/// Added while closing the coverage gap (docs/codequalitaet-gates-plan.md, C3). <c>Delete</c> was
/// listed first there, because the <b>cascade</b> hangs off it: deleting the adult takes their children,
/// subjects and chapters with it. A delete path that no test exercises is the most expensive blind spot - it
/// only surfaces once the data is already gone.
/// </para>
/// </summary>
public class AdultLifecycleTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private async Task<(HttpClient Client, int Id)> RegistriereAsync(string pin, string? email = null)
    {
        var id = await TestApi.IdAsync(await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = "Papa", email, pin }));
        return (await TestApi.AdultAsync(factory, id, pin), id);
    }

    [Fact]
    public async Task Loeschen_Nimmt_Das_Allein_Betreute_Kind_Und_Das_Konto_Mit()
    {
        var (papa, adultId) = await RegistriereAsync("5101", "allein@example.test");
        var childId = await TestApi.IdAsync(await papa.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = "Kind zum Löschen", pin = "5102" }));

        Assert.Equal(HttpStatusCode.NoContent, (await papa.DeleteAsync($"/api/v1/supervisor/adults/{adultId}")).StatusCode);

        // Look straight into the database: after the delete there is nobody left to ask through the API - and
        // that is exactly the point. Before the fix the child remained an **orphan**: visible or deletable for
        // no adult, but its PIN login still valid.
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        Assert.False(await db.Adults.AnyAsync(a => a.Id == adultId));
        Assert.False(await db.Children.AnyAsync(c => c.Id == childId));
        Assert.False(await db.Accounts.AnyAsync(a => a.Email == "allein@example.test"));
    }

    [Fact]
    public async Task Loeschen_Laesst_Das_Ko_Betreute_Kind_Stehen()
    {
        // The opposite direction, and the reason the cascade does not simply take "all children": a child
        // supervised by father **and** mother must not disappear because one of them leaves.
        var (vater, vaterId) = await RegistriereAsync("5108");
        var (_, mutterId) = await RegistriereAsync("5109");
        var childId = await TestApi.IdAsync(await vater.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = "Gemeinsames Kind", pin = "5110" }));
        (await vater.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/supervisors",
            new { supervisorId = mutterId, relation = "Mother" })).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.NoContent, (await vater.DeleteAsync($"/api/v1/supervisor/adults/{vaterId}")).StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        Assert.False(await db.Adults.AnyAsync(a => a.Id == vaterId));
        Assert.True(await db.Children.AnyAsync(c => c.Id == childId));
        // And the mother is still a supervisor - she did not lose the child along with them.
        var mutter = await TestApi.AdultAsync(factory, mutterId, "5109");
        Assert.Equal(HttpStatusCode.OK, (await mutter.GetAsync($"/api/v1/supervisor/children/{childId}")).StatusCode);
    }

    /// <summary>
    /// The transitive path of the same defect - and the one that gave the rebuild its name: deleting a
    /// supervisor took their shop articles with it (cascade, intended), and those took the <b>paid inventory</b>
    /// of all supervised children with them through a second cascade. Two individually reasonable
    /// cascades combined to destroy value; the purchase receipts remained via <c>SetNull</c> and
    /// pointed at stock that no longer existed.
    /// <para>
    /// The remaining stock afterwards is visible to the <b>child</b>, not to the remaining supervisor: the
    /// economy is issuer-bound (<c>SupervisorId</c> snapshot), the mother's shop is not
    /// the father's. That is the existing rule, not a new gap - however, the item can no longer be
    /// redeemed (activation targets a live article), and the compensation runs through
    /// the existing pressure valve <c>POST children/{id}/points</c>.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Loeschen_Vernichtet_Kein_Bezahltes_Kind_Inventar()
    {
        var (vater, vaterId) = await RegistriereAsync("5113");
        var (_, mutterId) = await RegistriereAsync("5114");
        var childId = await TestApi.IdAsync(await vater.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = "Kind mit Guthaben", pin = "5115" }));
        (await vater.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/supervisors",
            new { supervisorId = mutterId, relation = "Mother" })).EnsureSuccessStatusCode();

        var articleId = await TestApi.IdAsync(await vater.PostAsJsonAsync("/api/v1/supervisor/shop/articles",
            new { articleNumber = "TV-513", title = "Fernsehen", unitType = "Minute", actionType = "TV" }));
        var listingId = await TestApi.IdAsync(await vater.PostAsJsonAsync(
            $"/api/v1/supervisor/shop/articles/{articleId}/listings",
            new { title = "60 Minuten", coinPrice = 50, unitsPerPurchase = 60, currentStock = 1, maxStock = 1 }));
        (await vater.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/points",
            new { amount = 50, reason = "Coins" })).EnsureSuccessStatusCode();
        var kind = await TestApi.ChildAsync(factory, childId, "5115");
        (await kind.PostAsJsonAsync($"/api/v1/student/me/shop/listings/{listingId}/purchase", new { }))
            .EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.NoContent, (await vater.DeleteAsync($"/api/v1/supervisor/adults/{vaterId}")).StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            Assert.False(await db.ShopArticles.AnyAsync(a => a.Id == articleId)); // the article is gone, as intended
            var inv = await db.ChildInventories.AsNoTracking().SingleAsync(i => i.ChildId == childId);
            Assert.Equal(60, inv.Quantity);
            Assert.Null(inv.ShopArticleId);
        }

        // The child still sees its stock, named from the snapshot.
        var meins = await kind.GetAsync("/api/v1/student/me/shop/inventory");
        meins.EnsureSuccessStatusCode();
        var posten = Assert.Single((await meins.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray());
        Assert.Equal(60, posten.GetProperty("quantity").GetInt32());
        Assert.Equal("Fernsehen", posten.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Nach_Dem_Loeschen_Ist_Die_Adresse_Wieder_Frei()
    {
        // A follow-up of the orphaned account: if the profile-less account remained, it would hold its (unique)
        // e-mail forever - the address would be unusable after a cancellation.
        var (papa, adultId) = await RegistriereAsync("5111", "wieder-frei@example.test");
        (await papa.DeleteAsync($"/api/v1/supervisor/adults/{adultId}")).EnsureSuccessStatusCode();

        var erneut = await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = "Papa erneut", email = "wieder-frei@example.test", pin = "5112" });
        Assert.Equal(HttpStatusCode.Created, erneut.StatusCode);
    }

    [Fact]
    public async Task Loeschen_Eines_Unbekannten_Erwachsenen_Ist_404()
    {
        // The error case of the delete route - and at the same time the counter-check to the ownership rule:
        // the own id may be deleted, an invented one does not exist. (A *foreign* one is covered by OwnershipTests.)
        var (papa, adultId) = await RegistriereAsync("5103");
        Assert.Equal(HttpStatusCode.NoContent, (await papa.DeleteAsync($"/api/v1/supervisor/adults/{adultId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await papa.DeleteAsync($"/api/v1/supervisor/adults/{adultId}")).StatusCode);
    }

    [Fact]
    public async Task Zweite_Registrierung_Mit_Gleicher_Adresse_Ist_409()
    {
        // `Account.Email` carries a filtered unique index. Without a pre-check the registration ran halfway:
        // the `Adult` was saved, the account failed at the index → 500, and what remained was an adult
        // **without a login**. Found while building the PATCH guard (C2).
        var email = "doppelt@example.test";
        await RegistriereAsync("5104", email);

        var zweite = await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = "Papa 2", email, pin = "5105" });

        Assert.Equal(HttpStatusCode.Conflict, zweite.StatusCode);
        Assert.Equal("duplicate_email", await CodeAsync(zweite));
    }

    [Fact]
    public async Task Umbenennen_Auf_Eine_Fremde_Adresse_Ist_409_Die_Eigene_Bleibt_Erlaubt()
    {
        var (_, ersterId) = await RegistriereAsync("5106", "erster@example.test");
        var (zweiter, zweiterId) = await RegistriereAsync("5107", "zweiter@example.test");
        Assert.NotEqual(ersterId, zweiterId);

        var kollision = await zweiter.PatchAsJsonAsync($"/api/v1/supervisor/adults/{zweiterId}",
            new { email = "erster@example.test" });
        Assert.Equal(HttpStatusCode.Conflict, kollision.StatusCode);
        Assert.Equal("duplicate_email", await CodeAsync(kollision));

        // Sending your own address again is not a collision - otherwise every form that sends all fields would
        // be blocked after the first save.
        (await zweiter.PatchAsJsonAsync($"/api/v1/supervisor/adults/{zweiterId}",
            new { name = "Papa neu", email = "zweiter@example.test" })).EnsureSuccessStatusCode();
    }

    /// <summary>
    /// The display name lives in <b>two</b> places: the <c>Adult</c> row carries the domain name (it appears
    /// as the author on exercises), the <c>Account</c> the login's one. <c>PATCH auth/me</c> pulled both
    /// along - <c>PATCH supervisor/adults/{id}</c> only the domain one, so the UI kept greeting the old name
    /// after the next login.
    /// </summary>
    [Fact]
    public async Task Umbenennen_Zieht_Den_Namen_Des_Kontos_Nach()
    {
        var (papa, adultId) = await RegistriereAsync("5120");
        var accountId = (await papa.GetFromJsonAsync<JsonElement>("/api/v1/auth/me"))
            .GetProperty("accountId").GetInt32();

        (await papa.PatchAsJsonAsync($"/api/v1/supervisor/adults/{adultId}", new { name = "Papa Umbenannt" }))
            .EnsureSuccessStatusCode();

        // The account-centric login reads the name from the account (and puts it into the token as
        // ClaimTypes.Name), not from the adult row - it is thus the visible side of the mirroring.
        var login = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login",
            new { accountId, pin = "5120" });
        login.EnsureSuccessStatusCode();
        Assert.Equal("Papa Umbenannt",
            (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("name").GetString());
    }

    [Fact]
    public async Task Adresswechsel_Gibt_Die_Alte_Adresse_Wieder_Frei()
    {
        var (papa, adultId) = await RegistriereAsync("5121", "vorher@example.test");
        (await papa.PatchAsJsonAsync($"/api/v1/supervisor/adults/{adultId}",
            new { email = "nachher@example.test" })).EnsureSuccessStatusCode();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            Assert.True(await db.Accounts.AnyAsync(a => a.Email == "nachher@example.test"));
            Assert.False(await db.Accounts.AnyAsync(a => a.Email == "vorher@example.test"));
        }

        // If the old address stayed on the account, an adult would hold an address they no longer carry - and
        // nobody could ever get it again.
        Assert.Equal(HttpStatusCode.Created, (await factory.CreateClient().PostAsJsonAsync(
            "/api/v1/supervisor/adults",
            new { name = "Nachmieter", email = "vorher@example.test", pin = "5122" })).StatusCode);
    }

    /// <summary>
    /// The sharp side of the same drift: the collision check runs against <c>Account.Email</c>, because the
    /// filtered unique index sits there - but since E5 <c>Adult.Email</c> carries one as well. If the account
    /// was not pulled along, an occupied address looked <b>free</b>: the check let it through, the index on the
    /// <c>Adult</c> struck, and the due 409 became a 500 with a half-saved state.
    /// </summary>
    [Fact]
    public async Task Adresswechsel_Macht_Die_Neue_Adresse_Fuer_Andere_Belegt()
    {
        var (erster, erstenId) = await RegistriereAsync("5123", "wandert@example.test");
        var (zweiter, zweiterId) = await RegistriereAsync("5124", "bleibt@example.test");
        (await erster.PatchAsJsonAsync($"/api/v1/supervisor/adults/{erstenId}",
            new { email = "ziel@example.test" })).EnsureSuccessStatusCode();

        var kollision = await zweiter.PatchAsJsonAsync($"/api/v1/supervisor/adults/{zweiterId}",
            new { email = "ziel@example.test" });
        Assert.Equal(HttpStatusCode.Conflict, kollision.StatusCode);
        Assert.Equal("duplicate_email", await CodeAsync(kollision));
    }

    private static async Task<string?> CodeAsync(HttpResponseMessage res) =>
        (await res.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("code").GetString();
}
