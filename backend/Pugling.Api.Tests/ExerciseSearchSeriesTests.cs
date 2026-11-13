using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// B-513: the catalog search filters by a whole textbook series. The exercise list offered "Reihe" as a filter, but
/// without a unit it sent nothing for it - the father saw the whole subject under a series he had chosen.
/// </summary>
public class ExerciseSearchSeriesTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static async Task<int> VocabAsync(HttpClient father, int seriesId, int unitId, string title) =>
        await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/vocabulary", new
            {
                title,
                orderIndex = 1,
                rewardPoints = 10,
                config = new
                {
                    direction = "front-to-back",
                    sourceLang = "en",
                    targetLang = "de",
                    items = new[] { new { front = $"word {title}", back = "Wort" } },
                },
            }));

    // Red if the parameter is ignored (the other series' exercise is listed) or filters by a single unit (the second unit
    // of the chosen series is missing).
    [Fact]
    public async Task Reihe_Filtert_Alle_Ihre_Units_Und_Nur_Sie()
    {
        var father = await TestApi.AdultAsync(factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Reihenfilter") }));
        var (seriesA, unitA1) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        var unitA2 = await TestApi.IdAsync(await father.PostAsJsonAsync($"/api/v1/creator/textbook-series/{seriesA}/units",
            new { label = "Unit 2", orderIndex = 2 }));
        var (seriesB, unitB1) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        var a1 = await VocabAsync(father, seriesA, unitA1, "A1");
        var a2 = await VocabAsync(father, seriesA, unitA2, "A2");
        var b1 = await VocabAsync(father, seriesB, unitB1, "B1");

        var rows = await father.GetFromJsonAsync<JsonElement>(
            $"/api/v1/creator/exercises?subjectId={subjectId}&seriesId={seriesA}");
        var ids = rows.EnumerateArray().Select(r => r.GetProperty("id").GetInt32()).Order().ToList();

        Assert.Equal(new[] { a1, a2 }.Order(), ids);
        Assert.DoesNotContain(b1, ids);
    }
}
