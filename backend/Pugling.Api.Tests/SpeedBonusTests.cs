using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Prüft den Schnelle-Antwort-Bonus: serverseitig aus der Zeit seit der letzten Antwort gemessen,
/// per Plan-Einstellung (<c>SpeedThresholdSeconds</c>/<c>SpeedBonusPoints</c>) konfigurierbar. Die
/// erste Karte einer Sitzung hat keinen Vorgänger und darum keinen Bonus.
/// </summary>
public class SpeedBonusTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    // Combo bewusst aus (Schwelle 0), damit nur der Speed-Bonus wirkt.
    private static async Task<int> SpeedPlanAsync(HttpClient father, int thresholdSeconds, int bonus) =>
        await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/study-plans", new
        {
            childId = 1,
            title = "Speed-Plan",
            method = "Vocabulary",
            durationDays = 5,
            contentKeys = new[] { "en_house_de_haus", "en_go_de_gehen" },
            useLeitner = true,
            comboThreshold = 0,
            speedThresholdSeconds = thresholdSeconds,
            speedBonusPoints = bonus,
        }));

    private static async Task<(int session, List<int> ids)> StartAsync(HttpClient child, int planId)
    {
        var plan = await (await child.GetAsync($"/api/v1/study-plans/{planId}")).Content.ReadFromJsonAsync<JsonElement>();
        var ids = plan.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("contentId").GetInt32()).ToList();
        var sid = (await (await child.PostAsJsonAsync($"/api/v1/study-plans/{planId}/practice-sessions", new { }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        return (sid, ids);
    }

    // Stufe 2 = SelfAssess; ohne RequireTypedTest zählt das WasKnown-Flag voll (wie in den Combo-Tests).
    private static async Task<JsonElement> ReviewAsync(HttpClient child, int planId, int sid, int contentId) =>
        await (await child.PostAsJsonAsync($"/api/v1/study-plans/{planId}/practice-sessions/{sid}/review",
            new { contentId, stage = 2, wasKnown = true })).Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    public async Task SchnelleAntwort_ImZeitfenster_BringtBonus_ErsteKarteNicht()
    {
        var father = await TestApi.FatherAsync(factory);
        var planId = await SpeedPlanAsync(father, thresholdSeconds: 60, bonus: 4);
        var child = await TestApi.ChildAsync(factory);
        var (sid, ids) = await StartAsync(child, planId);

        // Erste Karte: kein Vorgänger → keine Messung → kein Speed-Bonus.
        var first = await ReviewAsync(child, planId, sid, ids[0]);
        Assert.Equal(0, first.GetProperty("speedBonus").GetInt32());

        // Über der Anti-Cheat-Untergrenze (1s), aber weit unter der Schwelle (60s) → Bonus.
        await Task.Delay(1200);
        var second = await ReviewAsync(child, planId, sid, ids[1]);
        Assert.Equal(4, second.GetProperty("speedBonus").GetInt32());
    }

    [Fact]
    public async Task SpeedBonus_AbgeschaltetBeiSchwelleNull()
    {
        var father = await TestApi.FatherAsync(factory);
        var planId = await SpeedPlanAsync(father, thresholdSeconds: 0, bonus: 4);
        var child = await TestApi.ChildAsync(factory);
        var (sid, ids) = await StartAsync(child, planId);

        await ReviewAsync(child, planId, sid, ids[0]);
        await Task.Delay(1200);
        var second = await ReviewAsync(child, planId, sid, ids[1]);
        Assert.Equal(0, second.GetProperty("speedBonus").GetInt32()); // Feature aus
    }
}
