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
        var res = await father.PostAsJsonAsync("/api/v1/supervisor/study-plans",
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
            $"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId, useLeitner = true, stage = (int)TestStage.FreeText, cadence = "Daily" }));

        var list = await father.GetFromJsonAsync<List<JsonElement>>($"/api/v1/supervisor/study-plans/{planId}/positions");
        Assert.Single(list!);
        Assert.Equal(exerciseId, list![0].GetProperty("exerciseId").GetInt32());

        // Der Sohn spielt die Position → Inhalt kommt aus der referenzierten Übung/Store.
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{posId}/practice-sessions";
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { }));
        var outcome = await (await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/review",
            new { itemIndex = 0, givenAnswer = "Frühling" })).Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.True(outcome, "wasCorrect");
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
            $"/api/v1/supervisor/study-plans/{planId}/positions", new { exerciseId, useLeitner = true, stage = (int)TestStage.FreeText }));
        var del = await father.DeleteAsync($"/api/v1/supervisor/study-plans/{planId}/positions/{posId}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        // Neue Position, diesmal gespielt → geschützt (409, kein Verlust der Lernhistorie).
        var posId2 = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions", new { exerciseId, useLeitner = true, stage = (int)TestStage.FreeText }));
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{posId2}/practice-sessions";
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { }));
        await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/review", new { itemIndex = 0, givenAnswer = "Winter" });

        var del2 = await father.DeleteAsync($"/api/v1/supervisor/study-plans/{planId}/positions/{posId2}");
        Assert.Equal(HttpStatusCode.Conflict, del2.StatusCode);
    }

    [Fact]
    public async Task Position_UnbekannteUebung_Liefert400()
    {
        var father = await TestApi.FatherAsync(_factory);
        var planId = await EmptyPlanAsync(father);
        var res = await father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions", new { exerciseId = 999999 });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Position_UebernimmtExerciseDefaults_UndOrderStrategyIstApiSichtbar()
    {
        var father = await TestApi.FatherAsync(_factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = "Defaults-Position" }));
        var chapterId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "Unit", orderIndex = 1 }));
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary", new
            {
                title = "Nur zwei Karten",
                orderIndex = 1,
                rewardPoints = 10,
                defaultStage = (int)TestStage.FreeText,
                defaultItemCount = 2,
                defaultUseLeitner = true,
                defaultRequireTypedTest = true,
                config = new
                {
                    direction = "front-to-back",
                    sourceLang = "en",
                    targetLang = "de",
                    items = new[]
                    {
                        new { front = "a", back = "1" },
                        new { front = "b", back = "2" },
                        new { front = "c", back = "3" },
                    },
                },
            }));
        var planId = await EmptyPlanAsync(father);

        var created = await (await father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions", new
        {
            exerciseId,
            orderStrategy = "Serial",
        })).Content.ReadFromJsonAsync<JsonElement>();
        var positionId = created.GetProperty("id").GetInt32();

        Assert.Equal(JsonValueKind.Null, created.GetProperty("stage").ValueKind);
        Assert.Equal(JsonValueKind.Null, created.GetProperty("itemCount").ValueKind);
        Assert.Equal("Serial", created.GetProperty("orderStrategy").GetString());
        JsonAssert.True(created, "useLeitner");
        JsonAssert.True(created, "requireTypedTest");

        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = TestApi.PracticeBase(planId, positionId);
        var session = await (await child.PostAsJsonAsync(baseUrl, new { mode = "Info" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, session.GetProperty("total").GetInt32());

        var cards = await child.GetFromJsonAsync<JsonElement>($"{baseUrl}/{session.GetProperty("id").GetInt32()}/cards");
        Assert.Equal(new[] { 0, 1 }, cards.EnumerateArray().Select(card => card.GetProperty("itemIndex").GetInt32()).ToArray());

        var patched = await (await father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions/{positionId}", new
        {
            orderStrategy = "Random",
        })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Random", patched.GetProperty("orderStrategy").GetString());
    }

    [Fact]
    public async Task Plan_Loeschen_EntferntPlanMitGespielterPosition()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, "summer", "Sommer");
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, key);
        var planId = await EmptyPlanAsync(father);
        var posId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions", new { exerciseId, useLeitner = true, stage = (int)TestStage.FreeText }));

        // Position bespielen → Session/Progress vorhanden (blockiert Positions-DELETE, aber nicht Plan-DELETE).
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{posId}/practice-sessions";
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { }));
        await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/review", new { itemIndex = 0, givenAnswer = "Sommer" });

        // Der ganze Plan lässt sich löschen (kaskadiert Positionen/Fortschritt/Sitzungen).
        var del = await father.DeleteAsync($"/api/v1/supervisor/study-plans/{planId}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        // Danach ist der Plan (und damit die Positionsliste) weg → 404 über den Ownership-Filter.
        var after = await father.GetAsync($"/api/v1/supervisor/study-plans/{planId}/positions");
        Assert.Equal(HttpStatusCode.NotFound, after.StatusCode);

        // Die referenzierte Katalog-Übung bleibt erhalten.
        Assert.Equal(HttpStatusCode.OK, (await father.GetAsync($"/api/v1/creator/exercises/{exerciseId}")).StatusCode);
    }
}
