using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Offset-Paging der Listen-Endpunkte: <c>skip</c>/<c>take</c> liefern deterministische, disjunkte
/// Seiten, <c>take</c> wird geklemmt und die Gesamtzahl steht im Header <c>X-Total-Count</c>.
/// </summary>
public class PagingTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    /// <summary>Legt <paramref name="count"/> Rechen-Übungen in einem frischen Kapitel an; liefert Fach/Kapitel.</summary>
    private static async Task<(int subjectId, int chapterId)> SeedExercisesAsync(HttpClient father, int count)
    {
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = "Paging-Fach" }));
        var chapterId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "Kapitel", orderIndex = 1 }));
        for (var i = 0; i < count; i++)
            await father.PostAsJsonAsync($"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/arithmetic", new
            {
                title = $"Aufgabe {i}",
                orderIndex = i,
                rewardPoints = 5,
                config = new { problems = new[] { new { prompt = "1 + 1", answer = 2, tolerance = 0 } } },
            });
        return (subjectId, chapterId);
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
        var (subjectId, chapterId) = await SeedExercisesAsync(father, 5);
        var basePath = $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/arithmetic";

        var page1 = await father.GetAsync($"{basePath}?skip=0&take=2");
        var page2 = await father.GetAsync($"{basePath}?skip=2&take=2");
        var page3 = await father.GetAsync($"{basePath}?skip=4&take=2");

        // Gesamtzahl ist auf jeder Seite identisch und zählt die volle gefilterte Menge (nicht die Seite).
        Assert.Equal(5, TotalCount(page1));
        Assert.Equal(5, TotalCount(page2));
        Assert.Equal(5, TotalCount(page3));

        var ids1 = await IdsAsync(page1);
        var ids2 = await IdsAsync(page2);
        var ids3 = await IdsAsync(page3);
        Assert.Equal(2, ids1.Length);
        Assert.Equal(2, ids2.Length);
        Assert.Single(ids3); // Rest der 5 Elemente

        // Seiten sind disjunkt und decken zusammen alle 5 Übungen ab.
        int[] all = [.. ids1, .. ids2, .. ids3];
        Assert.Equal(5, all.Distinct().Count());
    }

    [Fact]
    public async Task CatalogSearch_IstPaginierbar()
    {
        var father = await TestApi.FatherAsync(factory);
        await SeedExercisesAsync(father, 4);

        // Robust gegen die im Development-Seed vorhandenen Übungen: die volle Menge einmal zählen …
        var full = await father.GetAsync("/api/v1/creator/exercises?take=500");
        var total = (await IdsAsync(full)).Length;
        Assert.Equal(total, TotalCount(full));
        Assert.True(total >= 4, "Seed + 4 angelegte Übungen erwartet.");

        // … und prüfen, dass take die Seite begrenzt, der Header aber die Gesamtzahl trägt.
        var res = await father.GetAsync("/api/v1/creator/exercises?take=3");
        Assert.Equal(total, TotalCount(res));
        Assert.Equal(3, (await IdsAsync(res)).Length);
    }

    [Fact]
    public async Task CatalogSearch_SortiertNachTitel_AufUndAbsteigend()
    {
        var father = await TestApi.FatherAsync(factory);
        // Auf das eigene Fach filtern, damit die Seed-Übungen das Ergebnis nicht mischen.
        var (subjectId, _) = await SeedExercisesAsync(father, 4); // Titel: "Aufgabe 0".."Aufgabe 3"
        var basePath = $"/api/v1/creator/exercises?subjectId={subjectId}";

        var asc = await StringsAsync(await father.GetAsync($"{basePath}&sort=title"), "title");
        var desc = await StringsAsync(await father.GetAsync($"{basePath}&sort=title&dir=desc"), "title");

        Assert.Equal(new[] { "Aufgabe 0", "Aufgabe 1", "Aufgabe 2", "Aufgabe 3" }, asc);
        Assert.Equal(new[] { "Aufgabe 3", "Aufgabe 2", "Aufgabe 1", "Aufgabe 0" }, desc);

        // Kurzform -title ist äquivalent zu sort=title&dir=desc.
        var shorthand = await StringsAsync(await father.GetAsync($"{basePath}&sort=-title"), "title");
        Assert.Equal(desc, shorthand);
    }

    [Fact]
    public async Task VocabularyStore_SortiertNachWort()
    {
        var father = await TestApi.FatherAsync(factory);
        // Distinktiver Präfix + search-Filter isoliert die drei Testvokabeln von etwaigem Seed-Bestand.
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
        var (subjectId, chapterId) = await SeedExercisesAsync(father, 3);
        var basePath = $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/arithmetic";

        // take=0 = reine Kennzahl: Gesamtzahl im Header, aber keine Zeilen (spart die Projektion).
        var res = await father.GetAsync($"{basePath}?take=0");
        Assert.Equal(3, TotalCount(res));
        Assert.Empty(await IdsAsync(res));
    }
}
