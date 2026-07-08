using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>Happy-Path des Lern-Katalogs: Fach → Kapitel → Übung (CRUD + Auswertung).</summary>
public class CatalogExerciseTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task Subject_Chapter_Exercise_Anlegen_Lesen_Auswerten()
    {
        var father = await TestApi.FatherAsync(factory);
        var (subjectId, chapterId, exerciseId) = await TestApi.CreateArithmeticExerciseAsync(father);
        var basePath = $"/api/v1/learn/subjects/{subjectId}/chapters/{chapterId}/arithmetic";

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
    public async Task ExerciseDefaults_WerdenGespeichertUndZurueckgegeben()
    {
        var father = await TestApi.FatherAsync(factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/learn/subjects", new { name = "Default-Fach" }));
        var chapterId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/learn/subjects/{subjectId}/chapters", new { name = "Kapitel", orderIndex = 1 }));
        var basePath = $"/api/v1/learn/subjects/{subjectId}/chapters/{chapterId}/vocabulary";

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

    // Hinweis: Der Bonus-Vorschlag der Übung wird jetzt beim Anlegen einer Lehrplan-POSITION übernommen
    // (siehe PlanPositionsController); der frühere „to-study-plan"-Kopierpfad entfiel mit dem Legacy-Modell.

    [Fact]
    public async Task Sohn_DarfKeineUebungAnlegen_403()
    {
        var father = await TestApi.FatherAsync(factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/learn/subjects", new { name = "Fach" }));
        var chapterId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/learn/subjects/{subjectId}/chapters", new { name = "Kapitel", orderIndex = 1 }));
        var child = await TestApi.ChildAsync(factory);

        var res = await child.PostAsJsonAsync($"/api/v1/learn/subjects/{subjectId}/chapters/{chapterId}/arithmetic",
            new { title = "X", orderIndex = 1, rewardPoints = 5, config = new { problems = Array.Empty<object>() } });

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }
}
