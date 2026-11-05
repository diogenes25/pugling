using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// B-291: grammar topics and categories are bound to a subject, and the subject boundary is checked when a carrier is
/// WRITTEN. A series that changed or cleared its subject left every old assignment standing, and the next PUT of such an
/// exercise was rejected for its own ids. The subject change is now refused with 409 <c>series_subject_in_use</c> while a
/// carrier of the series holds a topic or category of the old subject; the answer names how many exercises and units.
/// </summary>
public class ReiheFachwechselTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private sealed record Reihe(HttpClient Creator, int OldSubject, int NewSubject, int SeriesId, int UnitId)
    {
        public string Url => $"/api/v1/creator/textbook-series/{SeriesId}";
    }

    private async Task<Reihe> ReiheAsync()
    {
        var creator = await TestApi.AdultAsync(factory);
        var oldSubject = await SubjectAsync(creator, "Alt-Fach");
        var newSubject = await SubjectAsync(creator, "Neu-Fach");
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(creator, oldSubject);
        return new Reihe(creator, oldSubject, newSubject, seriesId, unitId);
    }

    private static async Task<int> SubjectAsync(HttpClient creator, string name) =>
        await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/subjects", new { name = TestApi.UniqueName(name) }));

    private static async Task<int> TopicAsync(HttpClient creator, int subjectId) =>
        await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/grammar-topics",
            new { label = TestApi.UniqueName("Thema"), subjectId }));

    private static async Task<int> ExerciseAsync(Reihe r, int[]? topicIds = null, int? categoryId = null) =>
        await TestApi.IdAsync(await r.Creator.PostAsJsonAsync($"{r.Url}/units/{r.UnitId}/vocabulary", new
        {
            title = TestApi.UniqueName("Fachwechsel-Übung"),
            orderIndex = 1,
            rewardPoints = 10,
            categoryId,
            config = new { direction = "front-to-back", sourceLang = "en", targetLang = "de", items = new[] { new { front = "hello", back = "hallo" } } },
            grammarTopicIds = topicIds ?? [],
        }));

    private static async Task<int?> SubjectOfAsync(Reihe r)
    {
        var series = await r.Creator.GetFromJsonAsync<JsonElement>(r.Url);
        return series.GetProperty("subjectId").ValueKind == JsonValueKind.Null ? null : series.GetProperty("subjectId").GetInt32();
    }

    private static async Task AssertBlockedAsync(Reihe r, HttpResponseMessage res, string counts)
    {
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("series_subject_in_use", problem.GetProperty("code").GetString());
        Assert.Contains(counts, problem.GetProperty("detail").GetString());
        // Refused means nothing written: the series keeps its old subject.
        Assert.Equal(r.OldSubject, await SubjectOfAsync(r));
    }

    /// <summary>Red if the exercise-topic carrier is not counted.</summary>
    [Fact]
    public async Task Thema_An_Einer_Uebung_Sperrt_Den_Fachwechsel()
    {
        var r = await ReiheAsync();
        await ExerciseAsync(r, [await TopicAsync(r.Creator, r.OldSubject)]);

        await AssertBlockedAsync(r, await r.Creator.PatchAsJsonAsync(r.Url, new { subjectId = r.NewSubject }), "1 exercise(s) and 0 unit(s)");
    }

    /// <summary>Red if the unit-topic carrier is not counted.</summary>
    [Fact]
    public async Task Thema_An_Einer_Unit_Sperrt_Den_Fachwechsel()
    {
        var r = await ReiheAsync();
        var topic = await TopicAsync(r.Creator, r.OldSubject);
        (await r.Creator.PatchAsJsonAsync($"{r.Url}/units/{r.UnitId}", new { grammarTopicIds = new[] { topic } })).EnsureSuccessStatusCode();

        await AssertBlockedAsync(r, await r.Creator.PatchAsJsonAsync(r.Url, new { subjectId = r.NewSubject }), "0 exercise(s) and 1 unit(s)");
    }

    /// <summary>Red if a category of the old subject is not counted - the same drift, older than the topics.</summary>
    [Fact]
    public async Task Kategorie_An_Einer_Uebung_Sperrt_Den_Fachwechsel()
    {
        var r = await ReiheAsync();
        var category = await TestApi.IdAsync(await r.Creator.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{r.OldSubject}/categories", new { name = TestApi.UniqueName("Art") }));
        await ExerciseAsync(r, categoryId: category);

        await AssertBlockedAsync(r, await r.Creator.PatchAsJsonAsync(r.Url, new { subjectId = r.NewSubject }), "1 exercise(s) and 0 unit(s)");
    }

    /// <summary>Clearing the subject is a change too. Red if only a new id is checked.</summary>
    [Fact]
    public async Task Fach_Leeren_Wird_Ebenso_Gesperrt()
    {
        var r = await ReiheAsync();
        await ExerciseAsync(r, [await TopicAsync(r.Creator, r.OldSubject)]);

        await AssertBlockedAsync(r, await r.Creator.PatchAsJsonAsync(r.Url, new { clearSubject = true }), "1 exercise(s) and 0 unit(s)");
    }

    /// <summary>
    /// B-401: a series without a subject may not host an exercise (B-106 T-01), so clearing the subject of a series that has
    /// exercises is refused even without topics or categories. Changing to ANOTHER subject stays allowed (counter-check in
    /// <see cref="Ohne_Zuordnungen_Oder_Ohne_Wechsel_Geht_Es_Durch"/>). Red if clearing is checked only for assignments.
    /// </summary>
    [Fact]
    public async Task Fach_Leeren_Mit_Uebungen_Wird_Gesperrt()
    {
        var r = await ReiheAsync();
        await ExerciseAsync(r);

        var res = await r.Creator.PatchAsJsonAsync(r.Url, new { clearSubject = true });
        // The clearing variant names its own reason - red if the two message branches are swapped.
        Assert.Contains("depend on its subject", await res.Content.ReadAsStringAsync());
        await AssertBlockedAsync(r, res, "1 exercise(s)");
    }

    /// <summary>
    /// Only the series' OWN carriers count: two series on the same subject, only A carries a topic - B may change, A is
    /// refused with exactly its own count. Red if either count drops the series filter.
    /// </summary>
    [Fact]
    public async Task Nur_Die_Eigenen_Traeger_Der_Reihe_Zaehlen()
    {
        var a = await ReiheAsync();
        var topic = await TopicAsync(a.Creator, a.OldSubject);
        await ExerciseAsync(a, [topic]);
        (await a.Creator.PatchAsJsonAsync($"{a.Url}/units/{a.UnitId}", new { grammarTopicIds = new[] { topic } })).EnsureSuccessStatusCode();
        var (seriesB, unitB) = await TestApi.CreateSeriesAndUnitAsync(a.Creator, a.OldSubject);
        var b = a with { SeriesId = seriesB, UnitId = unitB };

        Assert.Equal(HttpStatusCode.OK, (await b.Creator.PatchAsJsonAsync(b.Url, new { subjectId = b.NewSubject })).StatusCode);
        await AssertBlockedAsync(a, await a.Creator.PatchAsJsonAsync(a.Url, new { subjectId = a.NewSubject }), "1 exercise(s) and 1 unit(s)");
    }

    /// <summary>
    /// Counter-checks (green before and after): without old assignments the change goes through, and re-sending the same
    /// subject is no change at all. Red if the guard blocks every change or compares against the payload alone.
    /// </summary>
    [Fact]
    public async Task Ohne_Zuordnungen_Oder_Ohne_Wechsel_Geht_Es_Durch()
    {
        var r = await ReiheAsync();
        await ExerciseAsync(r, [await TopicAsync(r.Creator, r.OldSubject)]);
        Assert.Equal(HttpStatusCode.OK, (await r.Creator.PatchAsJsonAsync(r.Url, new { subjectId = r.OldSubject })).StatusCode);

        var frei = await ReiheAsync();
        await ExerciseAsync(frei);
        Assert.Equal(HttpStatusCode.OK, (await frei.Creator.PatchAsJsonAsync(frei.Url, new { subjectId = frei.NewSubject })).StatusCode);
        Assert.Equal(frei.NewSubject, await SubjectOfAsync(frei));
    }
}
