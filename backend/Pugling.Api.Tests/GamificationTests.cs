using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Prüft Missionen (zeitgebundene Ziele) und Auszeichnungen (Badges): serverseitige Auswertung der
/// Metriken aus dem Positions-Motor, idempotente Belohnung (je Zeitraum/Auszeichnung genau einmal)
/// und die Sohn-Sicht unter <c>api/me/missions</c> bzw. <c>api/me/achievements</c>.
/// </summary>
public class GamificationTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    // Combo aus (Schwelle 0), damit nur Missions-/Achievement-Punkte auftauchen.
    private async Task<(int planId, int positionId, int sessionId)> SetupAsync()
    {
        var father = await TestApi.FatherAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.SelfAssess, comboThreshold: 0);
        var child = await TestApi.ChildAsync(factory);
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);
        return (planId, positionId, sessionId);
    }

    private static Task ReviewAsync(HttpClient child, int planId, int positionId, int sid, int itemIndex) =>
        TestApi.PositionReviewAsync(child, planId, positionId, sid, itemIndex, wasKnown: true);

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
        var missionTitle = "TEST Tagesziel 2 Treffer";
        await father.PostAsJsonAsync("/api/v1/children/1/missions", new
        {
            title = missionTitle,
            metric = "CorrectReviews",
            target = 2,
            period = "Daily",
            rewardPoints = 25,
        });

        var (planId, positionId, sid) = await SetupAsync();
        var child = await TestApi.ChildAsync(factory);

        await ReviewAsync(child, planId, positionId, sid, 0);
        await ReviewAsync(child, planId, positionId, sid, 1); // Ziel (2) erreicht → Auswertung nach dem Review

        var missions = await (await child.GetAsync("/api/v1/me/missions")).Content.ReadFromJsonAsync<JsonElement>();
        var mine = missions.EnumerateArray().First(m => m.GetProperty("title").GetString() == missionTitle);
        Assert.True(mine.GetProperty("completed").GetBoolean());

        // Genau einmal belohnt – auch nach weiteren Treffern (Idempotenz je Tag).
        await ReviewAsync(child, planId, positionId, sid, 0);
        Assert.Equal(1, await CountPointReasonAsync(child, $"Mission erfüllt: {missionTitle}"));
    }

    [Fact]
    public async Task Auszeichnung_BeiSchwelle_EinmaligVerliehen_UndAlsErreichtSichtbar()
    {
        var father = await TestApi.FatherAsync(factory);
        var title = "TEST Badge 1 Treffer";
        await father.PostAsJsonAsync("/api/v1/children/1/achievements", new
        {
            title,
            icon = "⭐",
            metric = "CorrectReviews",
            threshold = 1,
            rewardPoints = 33,
        });

        var (planId, positionId, sid) = await SetupAsync();
        var child = await TestApi.ChildAsync(factory);
        await ReviewAsync(child, planId, positionId, sid, 0); // Schwelle (1) erreicht

        var achievements = await (await child.GetAsync("/api/v1/me/achievements")).Content.ReadFromJsonAsync<JsonElement>();
        var mine = achievements.EnumerateArray().First(a => a.GetProperty("title").GetString() == title);
        Assert.True(mine.GetProperty("earned").GetBoolean());
        Assert.Equal("⭐", mine.GetProperty("icon").GetString());

        // Genau einmal verliehen, auch nach weiteren Treffern.
        await ReviewAsync(child, planId, positionId, sid, 1);
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
