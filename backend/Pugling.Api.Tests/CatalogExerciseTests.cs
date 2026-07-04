using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>Happy-Path des Lern-Katalogs: Fach → Kapitel → Übung (CRUD + Auswertung).</summary>
public class CatalogExerciseTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task Subject_Chapter_Exercise_Anlegen_Lesen_Auswerten()
    {
        var father = await TestApi.FatherAsync(factory);
        var (subjectId, chapterId, exerciseId) = await TestApi.CreateArithmeticExerciseAsync(father);
        var basePath = $"/api/learn/subjects/{subjectId}/chapters/{chapterId}/arithmetic";

        var list = await (await father.GetAsync(basePath)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(list.GetArrayLength() >= 1);

        var get = await father.GetAsync($"{basePath}/{exerciseId}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal("Arithmetic", (await get.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("type").GetString());

        // Auswerten: richtige Lösung (7 × 6 = 42) → 100 %.
        var check = await father.PostAsJsonAsync($"{basePath}/{exerciseId}/check",
            new { answers = new[] { new { index = 0, value = "42" } } });
        Assert.Equal(HttpStatusCode.OK, check.StatusCode);
        Assert.Equal(100, (await check.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("scorePercent").GetInt32());
    }

    [Fact]
    public async Task BonusVorschlag_WirdBeimPlanErzeugen_Kopiert_UndWirktNichtRueckwirkend()
    {
        var father = await TestApi.FatherAsync(factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/learn/subjects", new { name = "Erd" }));
        var chapterId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/learn/subjects/{subjectId}/chapters", new { name = "DE", orderIndex = 1 }));
        var matchBase = $"/api/learn/subjects/{subjectId}/chapters/{chapterId}/matching";

        // Übung mit Bonus-Vorschlag (großzügig, um Motivation zu erhöhen).
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(matchBase, new
        {
            title = "Länder",
            orderIndex = 1,
            rewardPoints = 20,
            config = new { instruction = "Ordne zu.", pairs = new[] { new { left = "Bayern", right = "München" }, new { left = "Hessen", right = "Wiesbaden" } } },
            suggestedBonus = new { comboThreshold = 3, comboBonusPoints = 9, speedThresholdSeconds = 8, speedBonusPoints = 4, newContentPoints = 15 },
        }));

        // Plan aus der Übung → Bonus-Felder aus dem Vorschlag kopiert.
        var planId = (await (await father.PostAsJsonAsync($"{matchBase}/{exerciseId}/to-study-plan",
            new { childId = 1, durationDays = 7 })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("planId").GetInt32();

        var plan = await (await father.GetAsync($"/api/study-plans/{planId}")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, plan.GetProperty("comboThreshold").GetInt32());
        Assert.Equal(9, plan.GetProperty("comboBonusPoints").GetInt32());
        Assert.Equal(8, plan.GetProperty("speedThresholdSeconds").GetInt32());
        Assert.Equal(15, plan.GetProperty("newContentPoints").GetInt32());

        // Übungs-Vorschlag nachträglich ändern (PUT) – der bestehende Kind-Plan bleibt unberührt.
        await father.PutAsJsonAsync($"{matchBase}/{exerciseId}", new
        {
            title = "Länder",
            orderIndex = 1,
            rewardPoints = 20,
            config = new { instruction = "Ordne zu.", pairs = new[] { new { left = "Bayern", right = "München" } } },
            suggestedBonus = new { comboThreshold = 99, comboBonusPoints = 99, speedThresholdSeconds = 99, speedBonusPoints = 99, newContentPoints = 99 },
        });

        var planAfter = await (await father.GetAsync($"/api/study-plans/{planId}")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, planAfter.GetProperty("comboThreshold").GetInt32()); // unverändert
    }

    [Fact]
    public async Task Sohn_DarfKeineUebungAnlegen_403()
    {
        var father = await TestApi.FatherAsync(factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/learn/subjects", new { name = "Fach" }));
        var chapterId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/learn/subjects/{subjectId}/chapters", new { name = "Kapitel", orderIndex = 1 }));
        var child = await TestApi.ChildAsync(factory);

        var res = await child.PostAsJsonAsync($"/api/learn/subjects/{subjectId}/chapters/{chapterId}/arithmetic",
            new { title = "X", orderIndex = 1, rewardPoints = 5, config = new { problems = Array.Empty<object>() } });

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }
}
