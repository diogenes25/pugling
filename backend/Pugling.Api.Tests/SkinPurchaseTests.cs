using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Deckt die server-autoritative Skin-Ökonomie ab (<c>api/v1/me/skins…</c>): Der Kauf bucht echte
/// <b>Gems</b> ab (nicht Münzen), Besitz/Auswahl liegen am Kind. Nutzt frisch angelegte Kinder, damit
/// die Salden trotz geteilter Test-DB deterministisch sind.
/// </summary>
public class SkinPurchaseTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    /// <summary>Legt (als Vater) ein frisches Kind an und liefert dessen Id + einen Sohn-Client dafür.</summary>
    private async Task<(int childId, HttpClient child)> FreshChildAsync(HttpClient father, string pin)
    {
        var childId = await TestApi.IdAsync(
            await father.PostAsJsonAsync("/api/v1/children", new { name = "Skin-Kind", pin }));
        var child = await TestApi.ChildAsync(factory, childId, pin);
        return (childId, child);
    }

    /// <summary>Schreibt dem Kind Gems gut (kein API-Weg dafür – Gems entstehen aus Boni; Achievement → Gems).</summary>
    private async Task GrantGemsAsync(int childId, int amount)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        db.ChildPoints.Add(new ChildPointsEntry
        {
            ChildId = childId,
            Amount = amount,
            Kind = PointKind.Achievement,
            Reason = "Test-Gems",
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task NeuesKind_StartetMitGratisStarter()
    {
        var father = await TestApi.FatherAsync(factory);
        var (_, child) = await FreshChildAsync(father, "7001");

        var state = await (await child.GetAsync("/api/v1/me/skins")).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("pug", state.GetProperty("selected").GetString());
        Assert.Contains("pug", state.GetProperty("owned").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal(0, state.GetProperty("gems").GetInt32());
    }

    [Fact]
    public async Task Kauf_OhneDeckung_400()
    {
        var father = await TestApi.FatherAsync(factory);
        var (_, child) = await FreshChildAsync(father, "7002");

        var res = await child.PostAsJsonAsync("/api/v1/me/skins/fox/purchase", new { });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Kauf_MitDeckung_BuchtGemsAb_UndRuestetAus()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, child) = await FreshChildAsync(father, "7003");

        // Gems schaffen (2500), damit der Ninja (2000) bezahlbar ist.
        await GrantGemsAsync(childId, 2500);

        var res = await child.PostAsJsonAsync("/api/v1/me/skins/ninja/purchase", new { });
        res.EnsureSuccessStatusCode();
        var state = await res.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("ninja", state.GetProperty("selected").GetString());
        Assert.Contains("ninja", state.GetProperty("owned").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal(500, state.GetProperty("gems").GetInt32()); // 2500 − 2000

        // Die Abbuchung ist als negative Buchung mit eigener Kategorie im Wallet nachvollziehbar.
        var wallet = await (await child.GetAsync("/api/v1/me/points")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(500, wallet.GetProperty("gems").GetInt32());
        var spend = wallet.GetProperty("entries").EnumerateArray()
            .First(e => e.GetProperty("kind").GetString() == "SkinPurchase");
        Assert.Equal(-2000, spend.GetProperty("amount").GetInt32());
    }

    [Fact]
    public async Task Muenzen_ZahlenKeineSkins()
    {
        var father = await TestApi.FatherAsync(factory);
        var (childId, child) = await FreshChildAsync(father, "7007");

        // Nur Münzen (Manual → Coins), keine Gems: der Skin-Kauf muss trotzdem an der Deckung scheitern.
        (await father.PostAsJsonAsync($"/api/v1/children/{childId}/points",
            new { amount = 5000, reason = "Nur Münzen" })).EnsureSuccessStatusCode();

        var res = await child.PostAsJsonAsync("/api/v1/me/skins/fox/purchase", new { });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Kauf_BereitsBesessen_409()
    {
        var father = await TestApi.FatherAsync(factory);
        var (_, child) = await FreshChildAsync(father, "7004");

        var res = await child.PostAsJsonAsync("/api/v1/me/skins/pug/purchase", new { });

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Kauf_UnbekannterSkin_404()
    {
        var father = await TestApi.FatherAsync(factory);
        var (_, child) = await FreshChildAsync(father, "7005");

        var res = await child.PostAsJsonAsync("/api/v1/me/skins/banane/purchase", new { });

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Ausruesten_NichtBesessen_400()
    {
        var father = await TestApi.FatherAsync(factory);
        var (_, child) = await FreshChildAsync(father, "7006");

        var res = await child.PostAsJsonAsync("/api/v1/me/skins/ninja/equip", new { });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Vater_HatKeinenZugriffAufSkins_403()
    {
        var father = await TestApi.FatherAsync(factory);

        var res = await father.GetAsync("/api/v1/me/skins");

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task ConcurrencyToken_LaesstZweitenParallelenWriteScheitern()
    {
        // Beweist die Absicherung hinter dem 409 bei parallelen Käufen: laden zwei Kontexte dasselbe
        // Kind und schreiben beide (Stamp bumpen), muss der zweite mit DbUpdateConcurrencyException
        // scheitern – so kann keine Zweitbuchung den Deckungs-Check umgehen und doppelt abbuchen.
        var father = await TestApi.FatherAsync(factory);
        var childId = await TestApi.IdAsync(
            await father.PostAsJsonAsync("/api/v1/children", new { name = "Token-Kind", pin = "7100" }));

        using var scopeA = factory.Services.CreateScope();
        using var scopeB = factory.Services.CreateScope();
        var dbA = scopeA.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var dbB = scopeB.ServiceProvider.GetRequiredService<PuglingDbContext>();

        var childA = await dbA.Children.FirstAsync(c => c.Id == childId);
        var childB = await dbB.Children.FirstAsync(c => c.Id == childId);

        childA.SelectedSkin = "fox";
        childA.ConcurrencyStamp = Guid.NewGuid();
        await dbA.SaveChangesAsync();

        childB.SelectedSkin = "dragon";
        childB.ConcurrencyStamp = Guid.NewGuid();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => dbB.SaveChangesAsync());
    }
}
