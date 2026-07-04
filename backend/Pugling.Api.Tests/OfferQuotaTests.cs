using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;
using Pugling.Api.Services;

namespace Pugling.Api.Tests;

/// <summary>
/// Sichert das Kontingent von Angeboten ab: pro Periode sind genau <c>Quantity</c> Käufe möglich, das
/// Kontingent füllt sich in der nächsten Periode wieder auf, und ein Storno gibt seinen Slot frei. Die
/// zeitabhängigen Fälle laufen direkt gegen den <see cref="OfferService"/> (kontrollierbares „jetzt");
/// die Erschöpfung innerhalb einer Periode zusätzlich Ende-zu-Ende über die HTTP-API.
/// </summary>
public class OfferQuotaTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    // Ein Montag (Wochenbeginn) für deterministische Wochen-Fenster.
    private static readonly DateTime Monday = new(2026, 7, 6, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Kontingent_ProPeriode_ErschoepftUeberHttp()
    {
        var father = await TestApi.FatherAsync(factory);
        var childId = await TestApi.IdAsync(
            await father.PostAsJsonAsync("/api/v1/children", new { name = "Kontingent-Kind", pin = "9200" }));
        var child = await TestApi.ChildAsync(factory, childId, "9200");

        (await father.PostAsJsonAsync($"/api/v1/children/{childId}/points", new { amount = 1000, reason = "x" })).EnsureSuccessStatusCode();
        var offerId = (await (await father.PostAsJsonAsync($"/api/v1/children/{childId}/rewards",
            new { title = "Snack", cost = 100, period = "Weekly", quantity = 2 })).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetInt32();

        (await child.PostAsJsonAsync($"/api/v1/me/rewards/{offerId}/purchase", new { })).EnsureSuccessStatusCode();
        (await child.PostAsJsonAsync($"/api/v1/me/rewards/{offerId}/purchase", new { })).EnsureSuccessStatusCode();

        // Dritter Kauf in derselben Woche -> Kontingent erschöpft (409), obwohl noch Münzen da sind.
        var third = await child.PostAsJsonAsync($"/api/v1/me/rewards/{offerId}/purchase", new { });
        Assert.Equal(HttpStatusCode.Conflict, third.StatusCode);
    }

    [Fact]
    public async Task Kontingent_FuelltSichInNaechsterPeriodeWiederAuf()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var offers = scope.ServiceProvider.GetRequiredService<OfferService>();

        var (childId, offer) = await SetupAsync(db, coins: 1000, quantity: 1, OfferPeriod.Weekly);

        Assert.Equal(OfferService.OfferError.None, (await offers.PurchaseAsync(childId, offer, Monday)).Error);
        // Zweiter Kauf in derselben Woche -> erschöpft.
        Assert.Equal(OfferService.OfferError.QuotaExceeded, (await offers.PurchaseAsync(childId, offer, Monday)).Error);
        // Nächste Woche -> wieder frei.
        Assert.Equal(OfferService.OfferError.None, (await offers.PurchaseAsync(childId, offer, Monday.AddDays(7))).Error);
    }

    [Fact]
    public async Task Storno_GibtKontingentSlotWiederFrei()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var offers = scope.ServiceProvider.GetRequiredService<OfferService>();

        var (childId, offer) = await SetupAsync(db, coins: 1000, quantity: 1, OfferPeriod.Weekly);

        var first = await offers.PurchaseAsync(childId, offer, Monday);
        Assert.Equal(OfferService.OfferError.None, first.Error);
        Assert.Equal(OfferService.OfferError.QuotaExceeded, (await offers.PurchaseAsync(childId, offer, Monday)).Error);

        // Storno gibt den Slot frei -> erneuter Kauf in derselben Woche klappt wieder.
        Assert.Equal(OfferService.OfferError.None, (await offers.CancelAsync(childId, first.Redemption!.Id, Monday)).Error);
        Assert.Equal(OfferService.OfferError.None, (await offers.PurchaseAsync(childId, offer, Monday)).Error);
    }

    [Fact]
    public async Task Kauf_OhneDeckung_MeldetInsufficientCoins()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var offers = scope.ServiceProvider.GetRequiredService<OfferService>();

        var (childId, offer) = await SetupAsync(db, coins: 50, quantity: 5, OfferPeriod.Weekly, cost: 100);

        Assert.Equal(OfferService.OfferError.InsufficientCoins, (await offers.PurchaseAsync(childId, offer, Monday)).Error);
    }

    /// <summary>Legt direkt in der DB ein Kind mit Münz-Guthaben und ein Angebot an; liefert deren Ids.</summary>
    private static async Task<(int childId, int offerId)> SetupAsync(
        PuglingDbContext db, int coins, int quantity, OfferPeriod period, int cost = 100)
    {
        var father = new Father { Name = "Q-Vater", Pin = "0000" };
        var child = new Child { Father = father, Name = "Q-Kind", Pin = "0000" };
        // Münzen über eine Coins-Buchung (Manual → Coins).
        child.PointsEntries.Add(new ChildPointsEntry { Amount = coins, Kind = PointKind.Manual, Reason = "Test" });
        db.Fathers.Add(father);
        db.Children.Add(child);
        var offer = new Reward { Child = child, Title = "Q-Angebot", Cost = cost, Period = period, Quantity = quantity };
        db.Rewards.Add(offer);
        await db.SaveChangesAsync();
        return (child.Id, offer.Id);
    }
}
