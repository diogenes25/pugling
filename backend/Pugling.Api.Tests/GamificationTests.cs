using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Prüft Missionen (zeitgebundene Ziele) und Auszeichnungen (Badges): serverseitige Auswertung der
/// Metriken, idempotente Belohnung (je Zeitraum/Auszeichnung genau einmal) und die Sohn-Sicht
/// unter <c>api/me/missions</c> bzw. <c>api/me/achievements</c>.
/// </summary>
public class GamificationTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static async Task<int> LeitnerPlanAsync(HttpClient father) =>
        await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/study-plans", new
        {
            childId = 1,
            title = "Gami-Plan",
            method = "Vocabulary",
            durationDays = 5,
            contentKeys = new[] { "en_house_de_haus", "en_go_de_gehen" },
            useLeitner = true,
            comboThreshold = 0, // Combo aus, damit nur Missions-/Achievement-Punkte auftauchen
        }));

    private static async Task<(int session, List<int> ids)> StartAsync(HttpClient child, int planId)
    {
        var plan = await (await child.GetAsync($"/api/v1/study-plans/{planId}")).Content.ReadFromJsonAsync<JsonElement>();
        var ids = plan.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("contentId").GetInt32()).ToList();
        var sid = (await (await child.PostAsJsonAsync($"/api/v1/study-plans/{planId}/practice-sessions", new { }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        return (sid, ids);
    }

    private static Task ReviewAsync(HttpClient child, int planId, int sid, int contentId) =>
        child.PostAsJsonAsync($"/api/v1/study-plans/{planId}/practice-sessions/{sid}/review",
            new { contentId, stage = 2, wasKnown = true });

    private static async Task<int> CountPointReasonAsync(HttpClient child, string reason)
    {
        var wallet = await (await child.GetAsync("/api/v1/me/points")).Content.ReadFromJsonAsync<JsonElement>();
        return wallet.GetProperty("entries").EnumerateArray()
            .Count(e => e.GetProperty("reason").GetString() == reason);
    }

    [Fact]
    public async Task Mission_BeiZielerreichung_EinmaligBelohnt_UndAlsErfuelltSichtbar()
    {
        var father = await TestApi.FatherAsync(factory);
        var planId = await LeitnerPlanAsync(father);
        // Eigene Tages-Mission mit niedrigem Ziel; eindeutiger Titel isoliert den Test.
        var missionTitle = "TEST Tagesziel 2 Treffer";
        await father.PostAsJsonAsync("/api/v1/children/1/missions", new
        {
            title = missionTitle,
            metric = "CorrectReviews",
            target = 2,
            period = "Daily",
            rewardPoints = 25,
        });

        var child = await TestApi.ChildAsync(factory);
        var (sid, ids) = await StartAsync(child, planId);

        await ReviewAsync(child, planId, sid, ids[0]);
        await ReviewAsync(child, planId, sid, ids[1]); // Ziel (2) erreicht → Auswertung nach dem Review

        // Sichtbar als erfüllt in der Sohn-Sicht.
        var missions = await (await child.GetAsync("/api/v1/me/missions")).Content.ReadFromJsonAsync<JsonElement>();
        var mine = missions.EnumerateArray().First(m => m.GetProperty("title").GetString() == missionTitle);
        Assert.True(mine.GetProperty("completed").GetBoolean());

        // Genau einmal belohnt – auch nach weiteren Treffern (Idempotenz je Tag).
        await ReviewAsync(child, planId, sid, ids[0]);
        Assert.Equal(1, await CountPointReasonAsync(child, $"Mission erfüllt: {missionTitle}"));
    }

    [Fact]
    public async Task Auszeichnung_BeiSchwelle_EinmaligVerliehen_UndAlsErreichtSichtbar()
    {
        var father = await TestApi.FatherAsync(factory);
        var planId = await LeitnerPlanAsync(father);
        var title = "TEST Badge 1 Treffer";
        await father.PostAsJsonAsync("/api/v1/children/1/achievements", new
        {
            title,
            icon = "⭐",
            metric = "CorrectReviews",
            threshold = 1,
            rewardPoints = 33,
        });

        var child = await TestApi.ChildAsync(factory);
        var (sid, ids) = await StartAsync(child, planId);
        await ReviewAsync(child, planId, sid, ids[0]); // Schwelle (1) erreicht

        var achievements = await (await child.GetAsync("/api/v1/me/achievements")).Content.ReadFromJsonAsync<JsonElement>();
        var mine = achievements.EnumerateArray().First(a => a.GetProperty("title").GetString() == title);
        Assert.True(mine.GetProperty("earned").GetBoolean());
        Assert.Equal("⭐", mine.GetProperty("icon").GetString());

        // Genau einmal verliehen, auch nach weiteren Treffern.
        await ReviewAsync(child, planId, sid, ids[1]);
        Assert.Equal(1, await CountPointReasonAsync(child, $"Auszeichnung erreicht: {title}"));
    }

    [Fact]
    public async Task Missionen_NurEigene_FremdesKindBekommt404()
    {
        var father = await TestApi.FatherAsync(factory);
        // Kind 999 gehört dem Vater nicht → der ChildOwnershipFilter liefert 404 (kein Enumerieren).
        var res = await father.GetAsync("/api/v1/children/999/missions");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, res.StatusCode);
    }
}
