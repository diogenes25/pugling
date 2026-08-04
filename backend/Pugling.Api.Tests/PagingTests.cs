using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Offset paging of the list endpoints: <c>skip</c>/<c>take</c> deliver deterministic, disjoint
/// pages, <c>take</c> is clamped and the total count sits in the <c>X-Total-Count</c> header.
/// </summary>
public class PagingTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    /// <summary>Creates <paramref name="count"/> arithmetic exercises in a fresh series unit; returns subject/series/unit.</summary>
    private static async Task<(int subjectId, int seriesId, int seriesUnitId)> SeedExercisesAsync(HttpClient father, int count)
    {
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = "Paging-Fach" }));
        var seriesId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = TestApi.UniqueName("Paging-Reihe"), subjectId }));
        var seriesUnitId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units", new { label = "Kapitel" }));
        for (var i = 0; i < count; i++)
            await father.PostAsJsonAsync($"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/arithmetic", new
            {
                title = $"Aufgabe {i}",
                orderIndex = i,
                rewardPoints = 5,
                config = new { problems = new[] { new { prompt = "1 + 1", answer = 2, tolerance = 0 } } },
            });
        return (subjectId, seriesId, seriesUnitId);
    }

    private static int TotalCount(HttpResponseMessage res) =>
        int.Parse(res.Headers.GetValues("X-Total-Count").Single());

    private static async Task<int[]> IdsAsync(HttpResponseMessage res)
    {
        var arr = await res.Content.ReadFromJsonAsync<JsonElement>();
        return [.. arr.EnumerateArray().Select(e => e.GetProperty("id").GetInt32())];
    }

    private static async Task<string[]> StringsAsync(HttpResponseMessage res, string prop)
    {
        var arr = await res.Content.ReadFromJsonAsync<JsonElement>();
        return [.. arr.EnumerateArray().Select(e => e.GetProperty(prop).GetString()!)];
    }

    [Fact]
    public async Task TypedList_LiefertSeitenMitGesamtzahlImHeader()
    {
        var father = await TestApi.FatherAsync(factory);
        var (_, seriesId, seriesUnitId) = await SeedExercisesAsync(father, 5);
        var basePath = $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/arithmetic";

        var page1 = await father.GetAsync($"{basePath}?skip=0&take=2");
        var page2 = await father.GetAsync($"{basePath}?skip=2&take=2");
        var page3 = await father.GetAsync($"{basePath}?skip=4&take=2");

        // The total is identical on every page and counts the full filtered set (not the page).
        Assert.Equal(5, TotalCount(page1));
        Assert.Equal(5, TotalCount(page2));
        Assert.Equal(5, TotalCount(page3));

        var ids1 = await IdsAsync(page1);
        var ids2 = await IdsAsync(page2);
        var ids3 = await IdsAsync(page3);
        Assert.Equal(2, ids1.Length);
        Assert.Equal(2, ids2.Length);
        Assert.Single(ids3); // the remainder of the 5 elements

        // The pages are disjoint and together cover all 5 exercises.
        int[] all = [.. ids1, .. ids2, .. ids3];
        Assert.Equal(5, all.Distinct().Count());
    }

    [Fact]
    public async Task CatalogSearch_IstPaginierbar()
    {
        var father = await TestApi.FatherAsync(factory);
        await SeedExercisesAsync(father, 4);

        // Robust against the exercises present in the development seed: count the full set once …
        var full = await father.GetAsync("/api/v1/creator/exercises?take=500");
        var total = (await IdsAsync(full)).Length;
        Assert.Equal(total, TotalCount(full));
        Assert.True(total >= 4, "Seed + 4 angelegte Übungen erwartet.");

        // … and check that take limits the page while the header carries the total.
        var res = await father.GetAsync("/api/v1/creator/exercises?take=3");
        Assert.Equal(total, TotalCount(res));
        Assert.Equal(3, (await IdsAsync(res)).Length);
    }

    [Fact]
    public async Task CatalogSearch_SortiertNachTitel_AufUndAbsteigend()
    {
        var father = await TestApi.FatherAsync(factory);
        // Filter on our own subject, so that the seeded exercises do not mix into the result.
        var (subjectId, _, _) = await SeedExercisesAsync(father, 4); // titles: "Aufgabe 0".."Aufgabe 3"
        var basePath = $"/api/v1/creator/exercises?subjectId={subjectId}";

        var asc = await StringsAsync(await father.GetAsync($"{basePath}&sort=title"), "title");
        var desc = await StringsAsync(await father.GetAsync($"{basePath}&sort=title&dir=desc"), "title");

        Assert.Equal(new[] { "Aufgabe 0", "Aufgabe 1", "Aufgabe 2", "Aufgabe 3" }, asc);
        Assert.Equal(new[] { "Aufgabe 3", "Aufgabe 2", "Aufgabe 1", "Aufgabe 0" }, desc);

        // The short form -title is equivalent to sort=title&dir=desc.
        var shorthand = await StringsAsync(await father.GetAsync($"{basePath}&sort=-title"), "title");
        Assert.Equal(desc, shorthand);
    }

    [Fact]
    public async Task VocabularyStore_SortiertNachWort()
    {
        var father = await TestApi.FatherAsync(factory);
        // A distinctive prefix + the search filter isolates the three test words from any seeded stock.
        await TestApi.CreateStoreVocabAsync(father, "zzzbanana", "Banane");
        await TestApi.CreateStoreVocabAsync(father, "zzzapple", "Apfel");
        await TestApi.CreateStoreVocabAsync(father, "zzzcherry", "Kirsche");

        var asc = await StringsAsync(await father.GetAsync("/api/v1/creator/vocabulary?search=zzz&sort=word"), "word");
        Assert.Equal(new[] { "zzzapple", "zzzbanana", "zzzcherry" }, asc);

        var desc = await StringsAsync(await father.GetAsync("/api/v1/creator/vocabulary?search=zzz&sort=word&dir=desc"), "word");
        Assert.Equal(new[] { "zzzcherry", "zzzbanana", "zzzapple" }, desc);
    }

    [Fact]
    public async Task Take0_LiefertNurDieGesamtzahl_OhneZeilen()
    {
        var father = await TestApi.FatherAsync(factory);
        var (_, seriesId, seriesUnitId) = await SeedExercisesAsync(father, 3);
        var basePath = $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/arithmetic";

        // take=0 = the pure figure: the total in the header, but no rows (it saves the projection).
        var res = await father.GetAsync($"{basePath}?take=0");
        Assert.Equal(3, TotalCount(res));
        Assert.Empty(await IdsAsync(res));
    }
}
