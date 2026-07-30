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
        return (await TestApi.FatherAsync(factory, id, pin), id);
    }

    [Fact]
    public async Task Loeschen_Nimmt_Das_Allein_Betreute_Kind_Und_Das_Konto_Mit()
    {
        var (papa, adultId) = await RegistriereAsync("5101", "allein@example.test");
        var childId = await TestApi.IdAsync(await papa.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = "Kind zum Löschen", pin = "5102" }));

        Assert.Equal(HttpStatusCode.NoContent, (await papa.DeleteAsync($"/api/v1/supervisor/adults/{adultId}")).StatusCode);

        // Direkt in der Datenbank nachsehen: über die API ist nach dem Löschen niemand mehr da, der fragen
        // könnte – und genau das ist der Punkt. Vor dem Fix blieb das Kind als **Waise** liegen: für keinen
        // Erwachsenen sichtbar oder löschbar, sein PIN-Login aber weiter gültig.
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        Assert.False(await db.Adults.AnyAsync(a => a.Id == adultId));
        Assert.False(await db.Children.AnyAsync(c => c.Id == childId));
        Assert.False(await db.Accounts.AnyAsync(a => a.Email == "allein@example.test"));
    }

    [Fact]
    public async Task Loeschen_Laesst_Das_Ko_Betreute_Kind_Stehen()
    {
        // Die Gegenrichtung, und der Grund, warum die Kaskade nicht einfach „alle Kinder" nimmt: ein von
        // Vater **und** Mutter betreutes Kind darf nicht verschwinden, weil einer der beiden geht.
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
        // Und die Mutter ist weiterhin Betreuerin – sie hat das Kind nicht mit verloren.
        var mutter = await TestApi.FatherAsync(factory, mutterId, "5109");
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
            Assert.False(await db.ShopArticles.AnyAsync(a => a.Id == articleId)); // Artikel weg, wie gewollt
            var inv = await db.ChildInventories.AsNoTracking().SingleAsync(i => i.ChildId == childId);
            Assert.Equal(60, inv.Quantity);
            Assert.Null(inv.ShopArticleId);
        }

        // Das Kind sieht seinen Bestand weiter, benannt aus der Momentaufnahme.
        var meins = await kind.GetAsync("/api/v1/student/me/shop/inventory");
        meins.EnsureSuccessStatusCode();
        var posten = Assert.Single((await meins.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray());
        Assert.Equal(60, posten.GetProperty("quantity").GetInt32());
        Assert.Equal("Fernsehen", posten.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Nach_Dem_Loeschen_Ist_Die_Adresse_Wieder_Frei()
    {
        // Folgefehler der Konto-Waise: bliebe das profillose Konto liegen, hielte es seine (eindeutige)
        // E-Mail für immer besetzt – die Adresse wäre nach einer Kündigung unbrauchbar.
        var (papa, adultId) = await RegistriereAsync("5111", "wieder-frei@example.test");
        (await papa.DeleteAsync($"/api/v1/supervisor/adults/{adultId}")).EnsureSuccessStatusCode();

        var erneut = await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = "Papa erneut", email = "wieder-frei@example.test", pin = "5112" });
        Assert.Equal(HttpStatusCode.Created, erneut.StatusCode);
    }

    [Fact]
    public async Task Loeschen_Eines_Unbekannten_Erwachsenen_Ist_404()
    {
        // Der Fehlerfall der Löschroute – und zugleich die Gegenprobe zur Eigentumsregel: die eigene Id
        // darf gelöscht werden, eine erfundene existiert nicht. (Eine *fremde* deckt OwnershipTests ab.)
        var (papa, adultId) = await RegistriereAsync("5103");
        Assert.Equal(HttpStatusCode.NoContent, (await papa.DeleteAsync($"/api/v1/supervisor/adults/{adultId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await papa.DeleteAsync($"/api/v1/supervisor/adults/{adultId}")).StatusCode);
    }

    [Fact]
    public async Task Zweite_Registrierung_Mit_Gleicher_Adresse_Ist_409()
    {
        // `Account.Email` trägt einen gefilterten Unique-Index. Ohne Vorprüfung lief die Registrierung auf
        // halbem Weg auf: der `Adult` war gespeichert, das Konto scheiterte am Index → 500, und zurück
        // blieb ein Erwachsener **ohne Login**. Gefunden beim Bauen des PATCH-Wächters (C2).
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

        // Die eigene Adresse erneut zu schicken ist keine Kollision – sonst wäre jedes Formular, das alle
        // Felder mitsendet, nach dem ersten Speichern blockiert.
        (await zweiter.PatchAsJsonAsync($"/api/v1/supervisor/adults/{zweiterId}",
            new { name = "Papa neu", email = "zweiter@example.test" })).EnsureSuccessStatusCode();
    }

    private static async Task<string?> CodeAsync(HttpResponseMessage res) =>
        (await res.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("code").GetString();
}
