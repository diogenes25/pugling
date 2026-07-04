using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Deckt die zwei jüngsten Backend-Änderungen ab: den Sohn-Wallet-Endpunkt (<c>GET api/me/points</c>)
/// und den Tagestest-Fallback (Üben darf den Test nicht aussperren).
/// </summary>
public class MeAndTestFallbackTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task Sohn_LiestEigenenPunktestand()
    {
        var child = await TestApi.ChildAsync(factory);

        var res = await child.GetAsync("/api/me/points");
        res.EnsureSuccessStatusCode();
        var wallet = await res.Content.ReadFromJsonAsync<JsonElement>();

        // Der geseedete Sohn startet mit 50 Punkten Startguthaben (weitere Buchungen anderer Tests
        // dieser Klasse teilen sich die DB, daher >= 50 statt exakt).
        Assert.Equal(1, wallet.GetProperty("childId").GetInt32());
        Assert.True(wallet.GetProperty("balance").GetInt32() >= 50);
        Assert.Contains("Startguthaben", wallet.GetProperty("entries").EnumerateArray()
            .Select(e => e.GetProperty("reason").GetString()));
    }

    [Fact]
    public async Task Vater_HatKeinenZugriffAufSohnWallet_403()
    {
        var father = await TestApi.FatherAsync(factory);

        var res = await father.GetAsync("/api/me/points");

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Wallet_OhneToken_401()
    {
        var res = await factory.CreateClient().GetAsync("/api/me/points");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Tagestest_NachVollstaendigerUebung_NichtGesperrt()
    {
        var father = await TestApi.FatherAsync(factory);
        // Leitner-Plan: nach dem Üben rutscht die Fälligkeit in die Zukunft.
        var planId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/study-plans", new
        {
            childId = 1,
            title = "Leitner-Plan",
            method = "Vocabulary",
            durationDays = 5,
            contentKeys = new[] { "en_house_de_haus", "en_go_de_gehen" },
            useLeitner = true,
        }));

        var child = await TestApi.ChildAsync(factory);
        var plan = await (await child.GetAsync($"/api/study-plans/{planId}")).Content.ReadFromJsonAsync<JsonElement>();
        var contentIds = plan.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("contentId").GetInt32()).ToList();

        var sid = (await (await child.PostAsJsonAsync($"/api/study-plans/{planId}/practice-sessions", new { }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        foreach (var cid in contentIds)
            (await child.PostAsJsonAsync($"/api/study-plans/{planId}/practice-sessions/{sid}/review",
                new { contentId = cid, stage = 2, wasKnown = true })).EnsureSuccessStatusCode();

        // Alle Karten sind jetzt hochgestuft und nicht mehr fällig – der Tagestest muss trotzdem starten.
        var testRes = await child.PostAsJsonAsync($"/api/study-plans/{planId}/tests", new { });

        Assert.Equal(HttpStatusCode.Created, testRes.StatusCode);
        var attempt = await testRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(contentIds.Count, attempt.GetProperty("totalItems").GetInt32());
    }
}
