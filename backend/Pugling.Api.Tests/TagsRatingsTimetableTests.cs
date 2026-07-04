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

        var tagId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/tags",
            new { childId = 1, name = "Klassenarbeit", color = "#3b82f6" }));

        var tagEx = await father.PostAsJsonAsync($"/api/tags/{tagId}/exercises", new { exerciseIds = new[] { exerciseId } });
        Assert.Equal(HttpStatusCode.OK, tagEx.StatusCode);
        Assert.Equal(1, (await tagEx.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("exerciseCount").GetInt32());

        var list = await (await father.GetAsync("/api/tags?childId=1")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(list.GetArrayLength() >= 1);

        var forEx = await father.GetAsync($"/api/tags/for-exercise/{exerciseId}?childId=1");
        Assert.Equal(HttpStatusCode.OK, forEx.StatusCode);
        Assert.True((await forEx.Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Sohn_BewertetInhaltDesPlans()
    {
        var father = await TestApi.FatherAsync(factory);
        var planId = await TestApi.CreateVocabPlanAsync(father);
        var plan = await (await father.GetAsync($"/api/study-plans/{planId}")).Content.ReadFromJsonAsync<JsonElement>();
        var contentId = plan.GetProperty("items")[0].GetProperty("contentId").GetInt32();

        var child = await TestApi.ChildAsync(factory);
        var rate = await child.PostAsJsonAsync($"/api/study-plans/{planId}/ratings",
            new { contentId, feedback = "Gut", comment = "passt zum Thema" });
        Assert.Equal(HttpStatusCode.Created, rate.StatusCode);

        var list = await (await child.GetAsync($"/api/study-plans/{planId}/ratings")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(list.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Timetable_EintragAnlegen_Auflisten()
    {
        var father = await TestApi.FatherAsync(factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/learn/subjects", new { name = "Sport" }));

        var create = await father.PostAsJsonAsync("/api/children/1/timetable",
            new { subjectId, dayOfWeek = "Monday", timeOfDay = "Vormittag" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var list = await (await father.GetAsync("/api/children/1/timetable")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(list.GetArrayLength() >= 1);
    }
}
