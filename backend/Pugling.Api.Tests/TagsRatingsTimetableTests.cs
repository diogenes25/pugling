using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>Happy-Paths für Tags, Inhalts-Bewertungen (Sohn) und Stundenplan.</summary>
public class TagsRatingsTimetableTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task Tag_Anlegen_UebungMarkieren_Auflisten()
    {
        var father = await TestApi.FatherAsync(factory);
        var (_, _, exerciseId) = await TestApi.CreateArithmeticExerciseAsync(father);

        var tagId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/tags",
            new { childId = 1, name = "Klassenarbeit", color = "#3b82f6" }));

        var tagEx = await father.PostAsJsonAsync($"/api/v1/creator/tags/{tagId}/exercises", new { exerciseIds = new[] { exerciseId } });
        Assert.Equal(HttpStatusCode.OK, tagEx.StatusCode);
        Assert.Equal(1, (await tagEx.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("exerciseCount").GetInt32());

        var list = await (await father.GetAsync("/api/v1/creator/tags?childId=1")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(list.GetArrayLength() >= 1);

        var forEx = await father.GetAsync($"/api/v1/creator/tags/for-exercise/{exerciseId}?childId=1");
        Assert.Equal(HttpStatusCode.OK, forEx.StatusCode);
        Assert.True((await forEx.Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Tag_VokabelMarkieren_ForVocabulary_Detach()
    {
        var father = await TestApi.FatherAsync(factory);
        var (vocabId, _) = await TestApi.CreateStoreVocabAsync(father, "house", "Haus");

        var tagId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/tags",
            new { childId = 1, name = "Vokabeltest", color = "#22c55e" }));

        // Markieren -> VocabularyCount steigt.
        var tagVoc = await father.PostAsJsonAsync($"/api/v1/creator/tags/{tagId}/vocabulary", new { vocabularyIds = new[] { vocabId } });
        Assert.Equal(HttpStatusCode.OK, tagVoc.StatusCode);
        Assert.Equal(1, (await tagVoc.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("vocabularyCount").GetInt32());

        // for-vocabulary liefert den Tag; tags/{id}/vocabulary die Vokabel.
        var forVoc = await father.GetAsync($"/api/v1/creator/tags/for-vocabulary/{vocabId}?childId=1");
        Assert.Equal(HttpStatusCode.OK, forVoc.StatusCode);
        Assert.True((await forVoc.Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength() >= 1);

        var vocs = await (await father.GetAsync($"/api/v1/creator/tags/{tagId}/vocabulary")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(vocs.EnumerateArray(), v => v.GetProperty("id").GetInt32() == vocabId);

        // Detach -> weg.
        Assert.Equal(HttpStatusCode.NoContent, (await father.DeleteAsync($"/api/v1/creator/tags/{tagId}/vocabulary/{vocabId}")).StatusCode);
        var after = await (await father.GetAsync($"/api/v1/creator/tags/for-vocabulary/{vocabId}?childId=1")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, after.GetArrayLength());
    }

    [Fact]
    public async Task Tag_VokabelMarkieren_FremderVater_Verboten()
    {
        var father = await TestApi.FatherAsync(factory);
        var (vocabId, _) = await TestApi.CreateStoreVocabAsync(father, "car", "Auto");
        var tagId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/tags",
            new { childId = 1, name = "Fremd", color = "#ef4444" }));

        // Zweiter Vater darf weder das fremde Kind abfragen (403) noch dessen Tag bespielen (404, kein Enumerieren).
        var res = await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/fathers", new { name = "Papa2", pin = "2222" });
        res.EnsureSuccessStatusCode();
        var id2 = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var father2 = await TestApi.FatherAsync(factory, id2, "2222");

        Assert.Equal(HttpStatusCode.Forbidden, (await father2.GetAsync($"/api/v1/creator/tags/for-vocabulary/{vocabId}?childId=1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await father2.PostAsJsonAsync($"/api/v1/creator/tags/{tagId}/vocabulary", new { vocabularyIds = new[] { vocabId } })).StatusCode);
    }

    [Fact]
    public async Task Timetable_EintragAnlegen_Auflisten()
    {
        var father = await TestApi.FatherAsync(factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = "Sport" }));

        var create = await father.PostAsJsonAsync("/api/v1/supervisor/children/1/timetable",
            new { subjectId, dayOfWeek = "Monday", timeOfDay = "Vormittag" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var list = await (await father.GetAsync("/api/v1/supervisor/children/1/timetable")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(list.GetArrayLength() >= 1);
    }
}
