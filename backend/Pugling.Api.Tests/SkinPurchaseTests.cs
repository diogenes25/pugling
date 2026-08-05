using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Covers the server-authoritative skin economy (<c>api/v1/student/me/skins…</c>): the purchase deducts real
/// <b>gems</b> (not coins), ownership/selection live on the child. Uses freshly created children so that
/// the balances are deterministic despite the shared test database.
/// </summary>
public class SkinPurchaseTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    /// <summary>Creates a fresh child (as the supervisor) and returns its id + a student client for it.</summary>
    private async Task<(int childId, HttpClient child)> FreshChildAsync(HttpClient father, string pin)
    {
        var childId = await TestApi.IdAsync(
            await father.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Skin-Kind", pin }));
        var child = await TestApi.ChildAsync(factory, childId, pin);
        return (childId, child);
    }

    /// <summary>Credits the child with gems (no API path for this - gems arise from bonuses; Achievement → Gems).</summary>
    private async Task GrantGemsAsync(int childId, int amount)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        db.ChildPointsEntries.Add(new ChildPointsEntry
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
        var father = await TestApi.AdultAsync(factory);
        var (_, child) = await FreshChildAsync(father, "7001");

        var state = await (await child.GetAsync("/api/v1/student/me/skins")).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("pug", state.GetProperty("selected").GetString());
        Assert.Contains("pug", state.GetProperty("owned").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal(0, state.GetProperty("gems").GetInt32());
    }

    [Fact]
    public async Task Kauf_OhneDeckung_400()
    {
        var father = await TestApi.AdultAsync(factory);
        var (_, child) = await FreshChildAsync(father, "7002");

        var res = await child.PostAsJsonAsync("/api/v1/student/me/skins/fox/purchase", new { });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Kauf_MitDeckung_BuchtGemsAb_UndRuestetAus()
    {
        var father = await TestApi.AdultAsync(factory);
        var (childId, child) = await FreshChildAsync(father, "7003");

        // Provide gems (2500) so that the ninja (2000) is affordable.
        await GrantGemsAsync(childId, 2500);

        var res = await child.PostAsJsonAsync("/api/v1/student/me/skins/ninja/purchase", new { });
        res.EnsureSuccessStatusCode();
        var state = await res.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("ninja", state.GetProperty("selected").GetString());
        Assert.Contains("ninja", state.GetProperty("owned").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal(500, state.GetProperty("gems").GetInt32()); // 2500 − 2000

        // The debit is traceable in the wallet as a negative entry with its own category.
        var wallet = await (await child.GetAsync("/api/v1/student/me/points")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(500, wallet.GetProperty("gems").GetInt32());
        var entries = await (await child.GetAsync("/api/v1/student/me/points/entries")).Content.ReadFromJsonAsync<JsonElement>();
        var spend = entries.EnumerateArray()
            .First(e => e.GetProperty("kind").GetString() == "SkinPurchase");
        Assert.Equal(-2000, spend.GetProperty("amount").GetInt32());
    }

    [Fact]
    public async Task Muenzen_ZahlenKeineSkins()
    {
        var father = await TestApi.AdultAsync(factory);
        var (childId, child) = await FreshChildAsync(father, "7007");

        // Coins only (Manual → coins), no gems: the skin purchase must still fail on the funds check.
        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/points",
            new { amount = 5000, reason = "Nur Münzen" })).EnsureSuccessStatusCode();

        var res = await child.PostAsJsonAsync("/api/v1/student/me/skins/fox/purchase", new { });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Kauf_BereitsBesessen_409()
    {
        var father = await TestApi.AdultAsync(factory);
        var (_, child) = await FreshChildAsync(father, "7004");

        var res = await child.PostAsJsonAsync("/api/v1/student/me/skins/pug/purchase", new { });

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Kauf_UnbekannterSkin_404()
    {
        var father = await TestApi.AdultAsync(factory);
        var (_, child) = await FreshChildAsync(father, "7005");

        var res = await child.PostAsJsonAsync("/api/v1/student/me/skins/banane/purchase", new { });

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Ausruesten_NichtBesessen_400()
    {
        var father = await TestApi.AdultAsync(factory);
        var (_, child) = await FreshChildAsync(father, "7006");

        var res = await child.PostAsJsonAsync("/api/v1/student/me/skins/ninja/equip", new { });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Vater_HatKeinenZugriffAufSkins_403()
    {
        var father = await TestApi.AdultAsync(factory);

        var res = await father.GetAsync("/api/v1/student/me/skins");

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task ConcurrencyToken_LaesstZweitenParallelenWriteScheitern()
    {
        // Proves the safeguard behind the 409 on parallel purchases: if two contexts load the same child and
        // both write (bumping the stamp), the second has to fail with a DbUpdateConcurrencyException - that way
        // no second entry can bypass the funds check and debit twice.
        var father = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(
            await father.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Token-Kind", pin = "7100" }));

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
