using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// B-428: a unit tells the creator how many exercises it already carries, so gaps in the catalog show without three filters.
/// The count covers every exercise of the unit (the unit is shared catalog) - withdrawn ones included, assignability is another
/// question (B-140).
/// </summary>
public class SeriesUnitExerciseCountTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static async Task<int> ArithmeticAsync(HttpClient creator, int seriesId, int unitId, int orderIndex) =>
        await TestApi.IdAsync(await creator.PostAsJsonAsync($"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/arithmetic", new
        {
            title = TestApi.UniqueName("Rechnen"),
            orderIndex,
            rewardPoints = 10,
            config = new { problems = new[] { new { prompt = "2 + 2", answer = 4, tolerance = 0 } } },
        }));

    // AK 1 and AK 3. Red if the count is missing, constant, counts the whole series, or drops a withdrawn exercise.
    [Fact]
    public async Task Units_NennenIhreUebungszahl()
    {
        var creator = await TestApi.AdultAsync(factory);
        var subjectId = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/subjects", new { name = TestApi.UniqueName("Zählfach") }));
        var (seriesId, fullUnitId) = await TestApi.CreateSeriesAndUnitAsync(creator, subjectId, "Unit 1");
        var emptyUnitId = await TestApi.IdAsync(await creator.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units", new { label = "Unit 2", orderIndex = 2 }));
        await ArithmeticAsync(creator, seriesId, fullUnitId, 1);
        await ArithmeticAsync(creator, seriesId, fullUnitId, 2);
        var withdrawn = await ArithmeticAsync(creator, seriesId, fullUnitId, 3);
        (await creator.PatchAsJsonAsync($"/api/v1/creator/exercises/{withdrawn}/sharing", new { executePublic = false })).EnsureSuccessStatusCode();

        var units = await creator.GetFromJsonAsync<JsonElement>($"/api/v1/creator/textbook-series/{seriesId}/units");
        int CountOf(int id) => units.EnumerateArray().Single(u => u.GetProperty("id").GetInt32() == id).GetProperty("exerciseCount").GetInt32();
        Assert.Equal(3, CountOf(fullUnitId));
        Assert.Equal(0, CountOf(emptyUnitId));

        var single = await creator.GetFromJsonAsync<JsonElement>($"/api/v1/creator/textbook-series/{seriesId}/units/{fullUnitId}");
        Assert.Equal(3, single.GetProperty("exerciseCount").GetInt32());

        // Decision 3 (review finding): another creator reads the same number - the count has no author or grant filter.
        var register = await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults", new { name = TestApi.UniqueName("Fremd"), pin = "4321" });
        var otherId = (await register.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var other = await TestApi.AdultAsync(factory, otherId, "4321");
        var seenByOther = await other.GetFromJsonAsync<JsonElement>($"/api/v1/creator/textbook-series/{seriesId}/units/{fullUnitId}");
        Assert.Equal(3, seenByOther.GetProperty("exerciseCount").GetInt32());

        // The PATCH answer is the same contract: renaming a unit must not report it as empty.
        var patched = await (await creator.PatchAsJsonAsync($"/api/v1/creator/textbook-series/{seriesId}/units/{fullUnitId}",
            new { label = "Unit 1 – neu" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, patched.GetProperty("exerciseCount").GetInt32());
    }
}
