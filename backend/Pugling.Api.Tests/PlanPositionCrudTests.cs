using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Position CRUD (stage 5): the father assembles a study plan from global catalog exercises –
/// creating, retrieving, playing, deleting a position (with history protection).
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
        var father = await TestApi.AdultAsync(_factory);
        var (_, k1) = await TestApi.CreateStoreVocabAsync(father, "spring", "Frühling");
        var (_, k2) = await TestApi.CreateStoreVocabAsync(father, "autumn", "Herbst");
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, k1, k2);
        var planId = await EmptyPlanAsync(father);

        // The supervisor creates the position on the global exercise (Leitner, typed stage).
        var posId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId, useLeitner = true, stage = (int)TestStage.FreeText, cadence = "Daily" }));

        var list = await father.GetFromJsonAsync<List<JsonElement>>($"/api/v1/supervisor/study-plans/{planId}/positions");
        Assert.Single(list!);
        Assert.Equal(exerciseId, list![0].GetProperty("exerciseId").GetInt32());

        // The child plays the position → the content comes from the referenced exercise/store.
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
        var father = await TestApi.AdultAsync(_factory);
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, "winter", "Winter");
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, key);
        var planId = await EmptyPlanAsync(father);

        // Unplayed → deletable.
        var posId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions", new { exerciseId, useLeitner = true, stage = (int)TestStage.FreeText }));
        var del = await father.DeleteAsync($"/api/v1/supervisor/study-plans/{planId}/positions/{posId}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        // A new position, this time played → protected (409, no loss of the learning history).
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
        var father = await TestApi.AdultAsync(_factory);
        var planId = await EmptyPlanAsync(father);
        var res = await father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions", new { exerciseId = 999999 });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Position_UebernimmtExerciseDefaults_UndOrderStrategyIstApiSichtbar()
    {
        var father = await TestApi.AdultAsync(_factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = "Defaults-Position" }));
        var seriesId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = TestApi.UniqueName("Defaults-Reihe"), subjectId }));
        var seriesUnitId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units", new { label = "Unit" }));
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary", new
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
        var father = await TestApi.AdultAsync(_factory);
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, "summer", "Sommer");
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, key);
        var planId = await EmptyPlanAsync(father);
        var posId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions", new { exerciseId, useLeitner = true, stage = (int)TestStage.FreeText }));

        // Play the position → a session/progress exists (blocks the position DELETE, but not the plan DELETE).
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{posId}/practice-sessions";
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { }));
        await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/review", new { itemIndex = 0, givenAnswer = "Sommer" });

        // The whole plan can be deleted (cascading positions/progress/sessions).
        var del = await father.DeleteAsync($"/api/v1/supervisor/study-plans/{planId}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        // Afterwards the plan (and thus the position list) is gone → 404 through the ownership filter.
        var after = await father.GetAsync($"/api/v1/supervisor/study-plans/{planId}/positions");
        Assert.Equal(HttpStatusCode.NotFound, after.StatusCode);

        // The referenced catalog exercise is preserved.
        Assert.Equal(HttpStatusCode.OK, (await father.GetAsync($"/api/v1/creator/exercises/{exerciseId}")).StatusCode);
    }

    /*
     * `goalThreshold` ist eine Prozent-Bestehensgrenze. Wer sie mit einer Trefferzahl verwechselt, soll ein
     * 400 sehen statt einer Pflicht, die lautlos jeden Versuch durchwinkt – 0 und 120 sind beides keine
     * Prozentangaben, und „Standard" sagt man mit dem Auslassen des Feldes.
     */
    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(-5)]
    public async Task Position_SchwelleAusserhalbProzent_WirdAbgewiesen(int goalThreshold)
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var planId = await EmptyPlanAsync(father);
        var url = $"/api/v1/supervisor/study-plans/{planId}/positions";

        var create = await father.PostAsJsonAsync(url, new { exerciseId, cadence = "Daily", goalThreshold });
        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
        Assert.Equal("validation_error",
            (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        // Not afterwards either: otherwise PATCH would let in exactly what POST rejects.
        var posId = await TestApi.IdAsync(await father.PostAsJsonAsync(url, new { exerciseId, cadence = "Daily" }));
        var patch = await father.PatchAsJsonAsync($"{url}/{posId}", new { goalThreshold });
        Assert.Equal(HttpStatusCode.BadRequest, patch.StatusCode);

        // The valid bound goes through and comes back.
        var ok = await father.PatchAsJsonAsync($"{url}/{posId}", new { goalThreshold = 90 });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(90, (await ok.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("goalThreshold").GetInt32());
    }

    /// <summary>Creates an empty Birkenbihl exercise (a type with no typed stage at all) and returns its id.</summary>
    private static async Task<int> CreateBirkenbihlExerciseAsync(HttpClient father)
    {
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Birkenbihl-Position-Test") }));
        var seriesId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = TestApi.UniqueName("Birkenbihl-Reihe"), subjectId }));
        var unitId = await TestApi.IdAsync(await father.PostAsJsonAsync($"/api/v1/creator/textbook-series/{seriesId}/units",
            new { label = "Lektion 1", orderIndex = 1 }));
        return await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/birkenbihl", new
            {
                title = "Birkenbihl",
                orderIndex = 1,
                rewardPoints = 10,
                config = new { learningLang = "en", nativeLang = "de" },
            }));
    }

    /*
     * RequireTypedTest gates PositionPracticeController's `scored` on `typed || !RequireTypedTest`. Birkenbihl's
     * IsTypedStage is constant false (it learns by reading, never by typing) - so a position with
     * requireTypedTest: true on this type would never score, silently, and the father would only notice after
     * weeks of an unmet goal (B-93).
     */
    [Fact]
    public async Task RequireTypedTest_AufEinemTypOhneGetippteStufe_WirdAbgewiesen()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await CreateBirkenbihlExerciseAsync(father);
        var planId = await EmptyPlanAsync(father);
        var url = $"/api/v1/supervisor/study-plans/{planId}/positions";

        var create = await father.PostAsJsonAsync(url, new { exerciseId, requireTypedTest = true });
        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
        Assert.Equal("validation_error",
            (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        // Not afterwards either: otherwise PATCH would let in exactly what POST rejects.
        var posId = await TestApi.IdAsync(await father.PostAsJsonAsync(url, new { exerciseId }));
        var patch = await father.PatchAsJsonAsync($"{url}/{posId}", new { requireTypedTest = true });
        Assert.Equal(HttpStatusCode.BadRequest, patch.StatusCode);

        // A type that DOES have a typed stage (vocabulary) is unaffected - the same setting still works there.
        var vocabId = await TestApi.CreateVocabExerciseAsync(father);
        var vocabPos = await father.PostAsJsonAsync(url, new { exerciseId = vocabId, requireTypedTest = true });
        Assert.Equal(HttpStatusCode.Created, vocabPos.StatusCode);
    }

    [Fact]
    public async Task Einzelne_Position_Wird_Gelesen_Eine_Fremde_Nicht()
    {
        // The single view of the position (a C3 coverage gap): only the list was covered so far.
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var planId = await EmptyPlanAsync(father);
        var url = $"/api/v1/supervisor/study-plans/{planId}/positions";
        var posId = await TestApi.IdAsync(await father.PostAsJsonAsync(url, new { exerciseId, stage = 2 }));

        var position = await (await father.GetAsync($"{url}/{posId}")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(posId, position.GetProperty("id").GetInt32());
        Assert.Equal(exerciseId, position.GetProperty("exerciseId").GetInt32());
        Assert.Equal(2, position.GetProperty("stage").GetInt32());

        // A position belonging to a *different* plan is not findable under this plan - otherwise the position
        // of someone else's plan could be read through a plan of your own.
        var andererPlan = await EmptyPlanAsync(father);
        Assert.Equal(HttpStatusCode.NotFound,
            (await father.GetAsync($"/api/v1/supervisor/study-plans/{andererPlan}/positions/{posId}")).StatusCode);
    }
}
