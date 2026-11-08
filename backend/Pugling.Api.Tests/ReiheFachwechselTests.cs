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

    private static object Payload(string title, int[] topicIds) => new
    {
        title,
        orderIndex = 1,
        rewardPoints = 10,
        config = new { direction = "front-to-back", sourceLang = "en", targetLang = "de", items = new[] { new { front = "hello", back = "hallo" } } },
        grammarTopicIds = topicIds,
    };

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
        // StartsWith, not Contains: the detail opens with the counts, and "1 exercise(s)" is also a suffix of "11 exercise(s)" (B-406).
        Assert.StartsWith(counts, problem.GetProperty("detail").GetString());
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

        var res = await r.Creator.PatchAsJsonAsync(r.Url, new { subjectId = r.NewSubject });
        // B-414: the message names the lock only - "remove them" offered a way out the edit dialog does not have for a category.
        Assert.DoesNotContain("remove them", await res.Content.ReadAsStringAsync());
        await AssertBlockedAsync(r, res, "1 exercise(s) and 0 unit(s)");
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
    /// exercises is refused even without topics or categories. Changing to ANOTHER subject stays allowed as long as no topics or categories of the old subject remain (counter-check in
    /// <see cref="Ohne_Zuordnungen_Oder_Ohne_Wechsel_Geht_Es_Durch"/>). Red if clearing is checked only for assignments.
    /// </summary>
    [Fact]
    public async Task Fach_Leeren_Mit_Uebungen_Wird_Gesperrt()
    {
        var r = await ReiheAsync();
        await ExerciseAsync(r);

        var res = await r.Creator.PatchAsJsonAsync(r.Url, new { clearSubject = true });
        // The clearing variant names its own reason - red if the two message branches are swapped.
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("depend on its subject", body);
        // B-414: the message names what locks and no way out beyond it. Deleting an exercise is its own lock
        // (exercise_in_use), switching subjects another one - red if the message offers either again.
        Assert.DoesNotContain("deleted", body);
        Assert.DoesNotContain("switching", body);
        Assert.DoesNotContain("Remove them", body);
        await AssertBlockedAsync(r, res, "1 exercise(s)");
    }

    /// <summary>
    /// B-406: the ways out the message names are walked once - blocked, way out, unblocked. Topics come off at the unit (PATCH)
    /// and at the exercise (PUT, as the edit dialog sends it); then the change goes through. Red if a way out does not lift the
    /// block, i.e. if the message promises something the server does not honour.
    /// </summary>
    [Fact]
    public async Task Ausweg_Themen_Herausnehmen_Entsperrt_Den_Wechsel()
    {
        var r = await ReiheAsync();
        var topic = await TopicAsync(r.Creator, r.OldSubject);
        var title = TestApi.UniqueName("Ausweg-Übung");
        var exerciseId = await TestApi.IdAsync(await r.Creator.PostAsJsonAsync($"{r.Url}/units/{r.UnitId}/vocabulary", Payload(title, [topic])));
        (await r.Creator.PatchAsJsonAsync($"{r.Url}/units/{r.UnitId}", new { grammarTopicIds = new[] { topic } })).EnsureSuccessStatusCode();
        await AssertBlockedAsync(r, await r.Creator.PatchAsJsonAsync(r.Url, new { subjectId = r.NewSubject }), "1 exercise(s) and 1 unit(s)");

        (await r.Creator.PatchAsJsonAsync($"{r.Url}/units/{r.UnitId}", new { grammarTopicIds = Array.Empty<int>() })).EnsureSuccessStatusCode();
        (await r.Creator.PutAsJsonAsync($"{r.Url}/units/{r.UnitId}/vocabulary/{exerciseId}", Payload(title, []))).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.OK, (await r.Creator.PatchAsJsonAsync(r.Url, new { subjectId = r.NewSubject })).StatusCode);
        Assert.Equal(r.NewSubject, await SubjectOfAsync(r));
    }

    /// <summary>
    /// B-406: "delete the exercise" is a way out only once the exercise is in no study plan - deleting a planned one is
    /// <c>exercise_in_use</c>. Walked: blocked clear, refused delete, position removed, delete, clear goes through. Red if the
    /// delete lock or the order changes. Since B-414 the message no longer names this path (it crosses a second lock); the walk
    /// keeps guarding the behaviour itself.
    /// </summary>
    [Fact]
    public async Task Ausweg_Loeschen_Geht_Bei_Einer_Verplanten_Uebung_Erst_Nach_Dem_Herausnehmen()
    {
        var r = await ReiheAsync();
        var exerciseId = await ExerciseAsync(r);
        var planId = await TestApi.CreateEmptyPlanAsync(r.Creator);
        var positionId = await TestApi.IdAsync(await r.Creator.PostAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions", new { exerciseId }));
        await AssertBlockedAsync(r, await r.Creator.PatchAsJsonAsync(r.Url, new { clearSubject = true }), "1 exercise(s)");

        var refused = await r.Creator.DeleteAsync($"{r.Url}/units/{r.UnitId}/vocabulary/{exerciseId}");
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Equal("exercise_in_use", (await refused.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await r.Creator.DeleteAsync($"/api/v1/supervisor/study-plans/{planId}/positions/{positionId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await r.Creator.DeleteAsync($"{r.Url}/units/{r.UnitId}/vocabulary/{exerciseId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await r.Creator.PatchAsJsonAsync(r.Url, new { clearSubject = true })).StatusCode);
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
