using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Positions-CRUD (Etappe 5): der Vater stellt einen Lehrplan aus globalen Katalog-Übungen zusammen –
/// Position anlegen, abrufen, spielen, löschen (mit Verlaufs-Schutz).
/// </summary>
public class PlanPositionCrudTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    private static async Task<int> EmptyPlanAsync(HttpClient father, int childId = 1)
    {
        var res = await father.PostAsJsonAsync("/api/v1/study-plans",
            new { childId, title = "Positions-Plan", durationDays = 10 });
        return await TestApi.IdAsync(res);
    }

    [Fact]
    public async Task Position_Anlegen_Abrufen_Spielen()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (_, k1) = await TestApi.CreateStoreVocabAsync(father, "spring", "Frühling");
        var (_, k2) = await TestApi.CreateStoreVocabAsync(father, "autumn", "Herbst");
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, k1, k2);
        var planId = await EmptyPlanAsync(father);

        // Vater legt die Position auf die globale Übung an (Leitner, getippte Stufe).
        var posId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/study-plans/{planId}/positions",
            new { exerciseId, useLeitner = true, stage = (int)TestStage.FreeText, cadence = "Daily" }));

        var list = await father.GetFromJsonAsync<List<JsonElement>>($"/api/v1/study-plans/{planId}/positions");
        Assert.Single(list!);
        Assert.Equal(exerciseId, list![0].GetProperty("exerciseId").GetInt32());

        // Der Sohn spielt die Position → Inhalt kommt aus der referenzierten Übung/Store.
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/study-plans/{planId}/positions/{posId}/practice-sessions";
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { }));
        var outcome = await (await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/review",
            new { itemIndex = 0, givenAnswer = "Frühling" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(outcome.GetProperty("wasCorrect").GetBoolean());
    }

    [Fact]
    public async Task Position_Loeschen_OhneVerlauf204_MitVerlauf409()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, "winter", "Winter");
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, key);
        var planId = await EmptyPlanAsync(father);

        // Ungespielt → löschbar.
        var posId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/study-plans/{planId}/positions", new { exerciseId, useLeitner = true, stage = (int)TestStage.FreeText }));
        var del = await father.DeleteAsync($"/api/v1/study-plans/{planId}/positions/{posId}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        // Neue Position, diesmal gespielt → geschützt (409, kein Verlust der Lernhistorie).
        var posId2 = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/study-plans/{planId}/positions", new { exerciseId, useLeitner = true, stage = (int)TestStage.FreeText }));
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/study-plans/{planId}/positions/{posId2}/practice-sessions";
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { }));
        await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/review", new { itemIndex = 0, givenAnswer = "Winter" });

        var del2 = await father.DeleteAsync($"/api/v1/study-plans/{planId}/positions/{posId2}");
        Assert.Equal(HttpStatusCode.Conflict, del2.StatusCode);
    }

    [Fact]
    public async Task Position_UnbekannteUebung_Liefert400()
    {
        var father = await TestApi.FatherAsync(_factory);
        var planId = await EmptyPlanAsync(father);
        var res = await father.PostAsJsonAsync($"/api/v1/study-plans/{planId}/positions", new { exerciseId = 999999 });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
}
