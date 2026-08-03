using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>Happy path of the learning catalog: subject → chapter → exercise (CRUD + scoring).</summary>
public class CatalogExerciseTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task Subject_Chapter_Exercise_Anlegen_Lesen_Auswerten()
    {
        var father = await TestApi.FatherAsync(factory);
        var (subjectId, chapterId, exerciseId) = await TestApi.CreateArithmeticExerciseAsync(father);
        var basePath = $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/arithmetic";

        var list = await (await father.GetAsync(basePath)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(list.GetArrayLength() >= 1);

        var get = await father.GetAsync($"{basePath}/{exerciseId}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal("Arithmetic", (await get.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("type").GetString());

        // Grading: the correct solution (7 × 6 = 42) → 100 %.
        var check = await father.PostAsJsonAsync($"{basePath}/{exerciseId}/check",
            new { answers = new[] { new { index = 0, value = "42" } } });
        Assert.Equal(HttpStatusCode.OK, check.StatusCode);
        Assert.Equal(100, (await check.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("scorePercent").GetInt32());
    }

    [Fact]
    public async Task ListCheck_Ungeordnet_ZaehltNennungenUnabhaengigVonReihenfolge()
    {
        var father = await TestApi.FatherAsync(factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = "Erdkunde" }));
        var chapterId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "Bundesländer", orderIndex = 1 }));
        var basePath = $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/list";

        var id = await TestApi.IdAsync(await father.PostAsJsonAsync(basePath, new
        {
            title = "Nenne 3 Bundesländer",
            orderIndex = 1,
            rewardPoints = 10,
            config = new
            {
                instruction = "Nenne drei Bundesländer.",
                ordered = false,
                items = new object[]
                {
                    new { value = "Bayern" },
                    new { value = "Hessen", alternatives = new[] { "Hesse" } },
                    new { value = "Berlin" },
                },
            },
        }));

        // Unordered: the order does not matter, an alternative counts → 100 %. The index in GivenAnswer is irrelevant here.
        var check = await father.PostAsJsonAsync($"{basePath}/{id}/check", new
        {
            answers = new[]
            {
                new { index = 0, value = "Berlin" },
                new { index = 1, value = "Hesse" },
                new { index = 2, value = "Bayern" },
            },
        });
        Assert.Equal(HttpStatusCode.OK, check.StatusCode);
        Assert.Equal(100, (await check.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("scorePercent").GetInt32());
    }

    [Fact]
    public async Task ListCheck_Geordnet_WertetPositionsgenauUeberIndexAus()
    {
        var father = await TestApi.FatherAsync(factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = "Reihenfolge" }));
        var chapterId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "Podest", orderIndex = 1 }));
        var basePath = $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/list";

        var id = await TestApi.IdAsync(await father.PostAsJsonAsync(basePath, new
        {
            title = "Treppchen",
            orderIndex = 1,
            rewardPoints = 10,
            config = new
            {
                // Mandatory since B-77/E7: the instruction is the only text a list card can show.
                instruction = "Nenne das Treppchen von oben nach unten.",
                ordered = true,
                items = new object[] { new { value = "Gold" }, new { value = "Silber" }, new { value = "Bronze" } },
            },
        }));

        // Ordered: the grading takes the value per index; here positions 0 and 2 sit, position 1 is wrong → 2/3.
        var check = await father.PostAsJsonAsync($"{basePath}/{id}/check", new
        {
            answers = new[]
            {
                new { index = 0, value = "Gold" },
                new { index = 1, value = "Bronze" },
                new { index = 2, value = "Bronze" },
            },
        });
        Assert.Equal(HttpStatusCode.OK, check.StatusCode);
        var body = await check.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, body.GetProperty("correct").GetInt32());
        Assert.Equal(3, body.GetProperty("total").GetInt32());
    }

    /// <summary>
    /// B-77/E7: a list without an instruction is rejected – on creating <b>and</b> on updating. Its entries are
    /// the solutions, so the instruction is the only question the card can carry; without it the child is asked
    /// to name something without being told what.
    /// </summary>
    [Fact]
    public async Task Liste_OhneAnweisung_WirdAbgewiesen_BeimAnlegenUndBeimAendern()
    {
        var father = await TestApi.FatherAsync(factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Liste-Pflicht") }));
        var chapterId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "Kapitel", orderIndex = 1 }));
        var basePath = $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/list";
        object Payload(string? instruction) => new
        {
            title = "Ohne Anweisung",
            orderIndex = 1,
            rewardPoints = 10,
            config = new { instruction, ordered = false, items = new object[] { new { value = "Bayern" } } },
        };

        var created = await father.PostAsJsonAsync(basePath, Payload("   "));
        Assert.Equal(HttpStatusCode.BadRequest, created.StatusCode);
        Assert.Equal("validation_error",
            (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        // The same guard on the update path - otherwise the instruction could be emptied after the fact.
        var id = await TestApi.IdAsync(await father.PostAsJsonAsync(basePath, Payload("Nenne ein Bundesland.")));
        var updated = await father.PutAsJsonAsync($"{basePath}/{id}", Payload(null));
        Assert.Equal(HttpStatusCode.BadRequest, updated.StatusCode);
    }

    [Fact]
    public async Task ExerciseDefaults_WerdenGespeichertUndZurueckgegeben()
    {
        var father = await TestApi.FatherAsync(factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = "Default-Fach" }));
        var chapterId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "Kapitel", orderIndex = 1 }));
        var basePath = $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary";

        var created = await (await father.PostAsJsonAsync(basePath, new
        {
            title = "Defaults sichtbar",
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
                    new { front = "one", back = "eins" },
                    new { front = "two", back = "zwei" },
                    new { front = "three", back = "drei" },
                },
            },
        })).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal((int)TestStage.FreeText, created.GetProperty("defaultStage").GetInt32());
        Assert.Equal(2, created.GetProperty("defaultItemCount").GetInt32());
        JsonAssert.True(created, "defaultUseLeitner");
        JsonAssert.True(created, "defaultRequireTypedTest");

        var loaded = await father.GetFromJsonAsync<JsonElement>($"{basePath}/{created.GetProperty("id").GetInt32()}");
        Assert.Equal((int)TestStage.FreeText, loaded.GetProperty("defaultStage").GetInt32());
        Assert.Equal(2, loaded.GetProperty("defaultItemCount").GetInt32());
    }

    // Note: the exercise's bonus suggestion is now taken over when a study plan POSITION is created (see
    // PlanPositionsController); the former "to-study-plan" copy path went away with the legacy model.

    [Fact]
    public async Task Sohn_DarfKeineUebungAnlegen_403()
    {
        var father = await TestApi.FatherAsync(factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = "Fach" }));
        var chapterId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "Kapitel", orderIndex = 1 }));
        var child = await TestApi.ChildAsync(factory);

        var res = await child.PostAsJsonAsync($"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/arithmetic",
            new { title = "X", orderIndex = 1, rewardPoints = 5, config = new { problems = Array.Empty<object>() } });

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }
}
