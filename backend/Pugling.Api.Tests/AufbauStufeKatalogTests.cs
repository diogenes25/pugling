using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// B-700: an exercise knows its build level and its target exercise. A prerequisite (level 1 or 2) points at a target
/// in the same unit; the path is one level deep; the PUT is a full representation; a target with prerequisites cannot
/// be deleted; the write right is checked on the prerequisite, not on the target.
/// </summary>
public class AufbauStufeKatalogTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static string Base(int seriesId, int unitId) =>
        $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/vocabulary";

    private static object Body(string title, int? buildLevel = null, int? buildTargetExerciseId = null) => new
    {
        title,
        orderIndex = 1,
        rewardPoints = 10,
        config = new { direction = "front-to-back", sourceLang = "en", targetLang = "de", items = new[] { new { front = "hello", back = "hallo" } } },
        buildLevel,
        buildTargetExerciseId,
    };

    private static async Task<(int seriesId, int unitId, int targetId)> UnitWithTargetAsync(HttpClient creator)
    {
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(creator, await TestApi.DefaultSubjectIdAsync(creator));
        var targetId = await TestApi.IdAsync(await creator.PostAsJsonAsync(Base(seriesId, unitId), Body("Ziel")));
        return (seriesId, unitId, targetId);
    }

    private static async Task AssertProblemAsync(HttpResponseMessage res, HttpStatusCode status, string code)
    {
        Assert.Equal(status, res.StatusCode);
        Assert.Equal(code, (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    /// <summary>AC 1: created with level and target, both come back on POST and GET. Red: a field missing in the contract.</summary>
    [Fact]
    public async Task Vorstufe_Laesst_Sich_Anlegen_Und_Zuruecklesen()
    {
        var creator = await TestApi.AdultAsync(factory);
        var (seriesId, unitId, targetId) = await UnitWithTargetAsync(creator);

        var res = await creator.PostAsJsonAsync(Base(seriesId, unitId), Body("Vorstufe", 1, targetId));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var id = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var read = await creator.GetFromJsonAsync<JsonElement>($"{Base(seriesId, unitId)}/{id}");
        Assert.Equal(1, read.GetProperty("buildLevel").GetInt32());
        Assert.Equal(targetId, read.GetProperty("buildTargetExerciseId").GetInt32());
        // The target itself carries no level: it is the top of the path by definition.
        var target = await creator.GetFromJsonAsync<JsonElement>($"{Base(seriesId, unitId)}/{targetId}");
        Assert.Equal(JsonValueKind.Null, target.GetProperty("buildLevel").ValueKind);
    }

    /// <summary>
    /// The catalog views carry the path too: the detail is what the editor reads back before its full-replacement PUT,
    /// the unit listing is where it finds targets. Red: either projection without the fields.
    /// </summary>
    [Fact]
    public async Task Katalog_Detail_Und_Liste_Tragen_Den_Weg()
    {
        var creator = await TestApi.AdultAsync(factory);
        var (seriesId, unitId, targetId) = await UnitWithTargetAsync(creator);
        var id = await TestApi.IdAsync(await creator.PostAsJsonAsync(Base(seriesId, unitId), Body("Vorstufe", 2, targetId)));

        var detail = await creator.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{id}");
        Assert.Equal(2, detail.GetProperty("buildLevel").GetInt32());
        Assert.Equal(targetId, detail.GetProperty("buildTargetExerciseId").GetInt32());

        var list = await creator.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises?seriesUnitId={unitId}&take=50");
        var row = list.EnumerateArray().Single(e => e.GetProperty("id").GetInt32() == id);
        Assert.Equal(2, row.GetProperty("buildLevel").GetInt32());
        Assert.Equal(targetId, row.GetProperty("buildTargetExerciseId").GetInt32());
    }

    /// <summary>AC 2, target in another unit. Red: the same-unit rule removed.</summary>
    [Fact]
    public async Task Ziel_In_Anderer_Unit_Wird_Abgelehnt()
    {
        var creator = await TestApi.AdultAsync(factory);
        var (_, _, targetId) = await UnitWithTargetAsync(creator);
        var (otherSeries, otherUnit) = await TestApi.CreateSeriesAndUnitAsync(creator, await TestApi.DefaultSubjectIdAsync(creator));

        await AssertProblemAsync(await creator.PostAsJsonAsync(Base(otherSeries, otherUnit), Body("Fremd", 1, targetId)),
            HttpStatusCode.BadRequest, "validation_error");
    }

    /// <summary>AC 2, target that is itself a prerequisite. Red: the one-level rule on the target removed.</summary>
    [Fact]
    public async Task Ziel_Das_Selbst_Vorstufe_Ist_Wird_Abgelehnt()
    {
        var creator = await TestApi.AdultAsync(factory);
        var (seriesId, unitId, targetId) = await UnitWithTargetAsync(creator);
        var prerequisite = await TestApi.IdAsync(await creator.PostAsJsonAsync(Base(seriesId, unitId), Body("Stufe 2", 2, targetId)));

        await AssertProblemAsync(await creator.PostAsJsonAsync(Base(seriesId, unitId), Body("Stufe 1", 1, prerequisite)),
            HttpStatusCode.BadRequest, "validation_error");
    }

    /// <summary>
    /// AC 2, an exercise that already has prerequisites cannot become one itself. Red: the rule on the replaced
    /// exercise removed - the path would then be two levels deep.
    /// </summary>
    [Fact]
    public async Task Ziel_Mit_Vorstufen_Wird_Nicht_Selbst_Vorstufe()
    {
        var creator = await TestApi.AdultAsync(factory);
        var (seriesId, unitId, targetId) = await UnitWithTargetAsync(creator);
        await TestApi.IdAsync(await creator.PostAsJsonAsync(Base(seriesId, unitId), Body("Stufe 1", 1, targetId)));
        var other = await TestApi.IdAsync(await creator.PostAsJsonAsync(Base(seriesId, unitId), Body("Anderes Ziel")));

        await AssertProblemAsync(await creator.PutAsJsonAsync($"{Base(seriesId, unitId)}/{targetId}", Body("Ziel", 2, other)),
            HttpStatusCode.BadRequest, "validation_error");
    }

    /// <summary>AC 2, level outside 1-2 (0 and 3). Red: the range check removed, or opened to the target level.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public async Task Stufe_Ausserhalb_Eins_Bis_Zwei_Wird_Abgelehnt(int level)
    {
        var creator = await TestApi.AdultAsync(factory);
        var (seriesId, unitId, targetId) = await UnitWithTargetAsync(creator);

        await AssertProblemAsync(await creator.PostAsJsonAsync(Base(seriesId, unitId), Body("Stufe", level, targetId)),
            HttpStatusCode.BadRequest, "validation_error");
    }

    /// <summary>AC 2, a reference to itself. Red: the self check removed (the target would pass all other rules).</summary>
    [Fact]
    public async Task Verweis_Auf_Sich_Selbst_Wird_Abgelehnt()
    {
        var creator = await TestApi.AdultAsync(factory);
        var (seriesId, unitId, targetId) = await UnitWithTargetAsync(creator);

        await AssertProblemAsync(await creator.PutAsJsonAsync($"{Base(seriesId, unitId)}/{targetId}", Body("Ziel", 1, targetId)),
            HttpStatusCode.BadRequest, "validation_error");
    }

    /// <summary>Only one of the two fields, and an unknown target. Red: either pair rule or the existence check removed.</summary>
    [Fact]
    public async Task Halbe_Angabe_Und_Unbekanntes_Ziel_Werden_Abgelehnt()
    {
        var creator = await TestApi.AdultAsync(factory);
        var (seriesId, unitId, targetId) = await UnitWithTargetAsync(creator);

        await AssertProblemAsync(await creator.PostAsJsonAsync(Base(seriesId, unitId), Body("Nur Stufe", 1)),
            HttpStatusCode.BadRequest, "validation_error");
        await AssertProblemAsync(await creator.PostAsJsonAsync(Base(seriesId, unitId), Body("Nur Ziel", null, targetId)),
            HttpStatusCode.BadRequest, "validation_error");
        await AssertProblemAsync(await creator.PostAsJsonAsync(Base(seriesId, unitId), Body("Unbekannt", 1, int.MaxValue)),
            HttpStatusCode.BadRequest, "invalid_reference");
    }

    /// <summary>AC 3: a PUT without the two fields takes the exercise off the path. Red: PATCH semantics on the fields.</summary>
    [Fact]
    public async Task Put_Ohne_Felder_Loescht_Die_Stufe()
    {
        var creator = await TestApi.AdultAsync(factory);
        var (seriesId, unitId, targetId) = await UnitWithTargetAsync(creator);
        var id = await TestApi.IdAsync(await creator.PostAsJsonAsync(Base(seriesId, unitId), Body("Vorstufe", 1, targetId)));

        var put = await creator.PutAsJsonAsync($"{Base(seriesId, unitId)}/{id}", Body("Vorstufe"));
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var read = await creator.GetFromJsonAsync<JsonElement>($"{Base(seriesId, unitId)}/{id}");
        Assert.Equal(JsonValueKind.Null, read.GetProperty("buildLevel").ValueKind);
        Assert.Equal(JsonValueKind.Null, read.GetProperty("buildTargetExerciseId").ValueKind);
    }

    /// <summary>
    /// AC 4: deleting a target with a prerequisite is refused (409) with a code of its own, <c>exercise_is_build_target</c>
    /// (review finding: <c>exercise_in_use</c> is translated with a pointer to the usage display, which never shows
    /// prerequisites). The detail names the prerequisite. The way out the message advises - take the prerequisite off the
    /// path in its editor (a PUT without the fields) - is taken here and lifts the lock: <c>204</c>. Red: Cascade/SetNull on
    /// the key (no 409), or the old code.
    /// </summary>
    [Fact]
    public async Task Ziel_Mit_Vorstufe_Ist_Nicht_Loeschbar()
    {
        var creator = await TestApi.AdultAsync(factory);
        var (seriesId, unitId, targetId) = await UnitWithTargetAsync(creator);
        var id = await TestApi.IdAsync(await creator.PostAsJsonAsync(Base(seriesId, unitId), Body("Vorstufe", 2, targetId)));

        var refused = await creator.DeleteAsync($"{Base(seriesId, unitId)}/{targetId}");
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        var problem = await refused.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("exercise_is_build_target", problem.GetProperty("code").GetString());
        Assert.Contains(id.ToString(), problem.GetProperty("detail").GetString());
        Assert.Equal(HttpStatusCode.OK, (await creator.GetAsync($"{Base(seriesId, unitId)}/{id}")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await creator.PutAsJsonAsync($"{Base(seriesId, unitId)}/{id}", Body("Vorstufe"))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await creator.DeleteAsync($"{Base(seriesId, unitId)}/{targetId}")).StatusCode);
    }

    /// <summary>
    /// The unit delete cascades to target and prerequisite in one statement and still passes. Red: Restrict on the key
    /// (SQLite checks it per row and the unit delete dies with a foreign key error).
    /// </summary>
    [Fact]
    public async Task Unit_Mit_Aufbau_Weg_Ist_Loeschbar()
    {
        var creator = await TestApi.AdultAsync(factory);
        var (seriesId, unitId, targetId) = await UnitWithTargetAsync(creator);
        await TestApi.IdAsync(await creator.PostAsJsonAsync(Base(seriesId, unitId), Body("Vorstufe", 1, targetId)));

        Assert.Equal(HttpStatusCode.NoContent,
            (await creator.DeleteAsync($"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}")).StatusCode);
    }

    /// <summary>
    /// AC 5: a creator without write right on the prerequisite gets 403 <c>not_author</c> (the write-right code; the story said not_owner, which is the owner-only code) even though the target is
    /// his own. Red: a right check on the target instead of the prerequisite. A stranger may still build his own
    /// prerequisite to a foreign target (decision 4).
    /// </summary>
    [Fact]
    public async Task Ohne_Schreibrecht_An_Der_Vorstufe_Gibt_Es_403()
    {
        var owner = await TestApi.AdultAsync(factory);
        var (seriesId, unitId, targetId) = await UnitWithTargetAsync(owner);
        var foreign = await TestApi.IdAsync(await owner.PostAsJsonAsync(Base(seriesId, unitId), Body("Fremde Übung")));

        var res = await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = TestApi.UniqueName("Fremder"), pin = "4711" });
        res.EnsureSuccessStatusCode();
        var strangerId = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var stranger = await TestApi.AdultAsync(factory, strangerId, "4711");
        var strangerTarget = await TestApi.IdAsync(await stranger.PostAsJsonAsync(Base(seriesId, unitId), Body("Eigenes Ziel")));

        // Statement of intent, not a B-700 guard: the 403 comes from the write check every PUT already had. It stays to
        // pin AC 5's wording - the right is checked on the prerequisite - next to the half below that can fail.
        await AssertProblemAsync(await stranger.PutAsJsonAsync($"{Base(seriesId, unitId)}/{foreign}", Body("Fremde Übung", 1, strangerTarget)),
            HttpStatusCode.Forbidden, "not_author");

        var own = await stranger.PostAsJsonAsync(Base(seriesId, unitId), Body("Eigene Vorstufe", 1, targetId));
        Assert.Equal(HttpStatusCode.Created, own.StatusCode);
    }
}
