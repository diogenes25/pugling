using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>Management of catalog exercises: detail GET with config, usage reverse lookup, delete protection.</summary>
public class CatalogManagementTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    /// <summary>
    /// Creates (as the creator) subject + textbook series (with the subject attached) + unit + a vocabulary
    /// exercise, so the test keeps the series/unit ids at hand instead of reading them back from the exercise
    /// detail - the detail no longer carries a series id (only <c>seriesUnitId</c>/<c>subjectId</c>).
    /// </summary>
    private async Task<(int seriesId, int seriesUnitId, int exerciseId)> CreateVocabExerciseAsync()
    {
        var father = await TestApi.AdultAsync(_factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            "/api/v1/creator/subjects", new { name = TestApi.UniqueName("Kat-Mgmt-Fach") }));
        var seriesId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            "/api/v1/creator/textbook-series", new { name = TestApi.UniqueName("Kat-Mgmt-Reihe"), subjectId }));
        var seriesUnitId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units", new { label = "Unit 1", orderIndex = 1 }));
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary", new
            {
                title = "Begrüßungen",
                orderIndex = 1,
                rewardPoints = 10,
                config = new
                {
                    direction = "front-to-back",
                    sourceLang = "en",
                    targetLang = "de",
                    items = new[] { new { front = "hello", back = "hallo" }, new { front = "goodbye", back = "tschüss" } },
                },
            }));
        return (seriesId, seriesUnitId, exerciseId);
    }

    [Fact]
    public async Task Detail_LiefertTypConfigUndMetadaten()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);

        var detail = await (await father.GetAsync($"/api/v1/creator/exercises/{exerciseId}"))
            .Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("Vocabulary", detail.GetProperty("type").GetString());
        // The vocabulary lives one level deeper as items of its own; the config only carries settings.
        Assert.Equal("front-to-back", detail.GetProperty("config").GetProperty("direction").GetString());
        Assert.False(string.IsNullOrEmpty(detail.GetProperty("subjectName").GetString()));
    }

    // Chapter uniqueness removed with B-106; SeriesUnit has no equivalent constraint.

    [Fact]
    public async Task Usage_ListetLehrplanMitKind()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, _) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)Pugling.Api.Models.TestStage.FreeText);

        var usage = await (await father.GetAsync($"/api/v1/creator/exercises/{exerciseId}/usage"))
            .Content.ReadFromJsonAsync<JsonElement>();

        var plans = usage.GetProperty("plans");
        Assert.Equal(1, plans.GetArrayLength());
        Assert.Equal(planId, plans[0].GetProperty("planId").GetInt32());
        Assert.Equal(1, plans[0].GetProperty("childId").GetInt32());
    }

    [Fact]
    public async Task Usage_OhneVerwendung_IstLeer()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);

        var usage = await (await father.GetAsync($"/api/v1/creator/exercises/{exerciseId}/usage"))
            .Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(0, usage.GetProperty("plans").GetArrayLength());
        Assert.Equal(0, usage.GetProperty("classTests").GetArrayLength());
    }

    [Fact]
    public async Task Delete_ReferenzierteUebung_Liefert409()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (seriesId, seriesUnitId, exerciseId) = await CreateVocabExerciseAsync();
        TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)Pugling.Api.Models.TestStage.FreeText);

        var res = await father.DeleteAsync($"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary/{exerciseId}");
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    /// <summary>
    /// Series unit and series cascade onto their exercises – but the FK <c>PlanPosition→Exercise</c> is
    /// Restrict. Without its own check, the deletion would crash as an FK violation in a bare 500. Since B-413 the
    /// conflict carries its own code: its translation must not point at "usage" of an exercise, which never shows a
    /// milestone on the unit. Red if the unit or series delete answers <c>exercise_in_use</c> again, or names a way out
    /// ("remove it there first") that runs into a second lock.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Delete_UnitOderReihe_MitVerwendeterUebung_Liefert409(bool wholeSeries)
    {
        var father = await TestApi.AdultAsync(_factory);
        var (seriesId, seriesUnitId, exerciseId) = await CreateVocabExerciseAsync();
        TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)Pugling.Api.Models.TestStage.FreeText);

        var res = await father.DeleteAsync(wholeSeries
            ? $"/api/v1/creator/textbook-series/{seriesId}"
            : $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}");

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("unit_content_in_use", problem.GetProperty("code").GetString());
        Assert.DoesNotContain("there first", problem.GetProperty("detail").GetString());
    }

    /// <summary>
    /// B-413, case 3: a milestone on the whole unit blocks deleting the unit although no exercise of it is used anywhere -
    /// exactly the lock "usage" of an exercise can never show. Red if the unit delete loses the milestone check or answers
    /// with the exercise code. B-436: the series delete carries the same promise for milestones on any of its units; red
    /// there if the series path drops its unit scope (the delete would then end at the restrict FK as a bare 500).
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Delete_UnitOderReihe_MitZielEtappeAufDerUnit_Liefert409(bool wholeSeries)
    {
        var father = await TestApi.AdultAsync(_factory);
        var (seriesId, seriesUnitId, exerciseId) = await CreateVocabExerciseAsync();
        await AddUnitMilestoneAsync(father, seriesUnitId, exerciseId);

        var res = await father.DeleteAsync(wholeSeries
            ? $"/api/v1/creator/textbook-series/{seriesId}"
            : $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}");

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("unit_content_in_use", (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    /// <summary>
    /// B-436, review finding: the milestone lock is scoped to the unit (or the series' units) being deleted. A milestone on
    /// a neighbouring unit of the same series, or on a unit of another series, must not block. Red if the unit path widens
    /// to the whole series or the series path drops its series filter - the creator would get a 409 for content nobody
    /// uses. The class database seeds no milestones, so without this case such a widening only shows by test order.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Delete_UnitOderReihe_ZielEtappeAufEinerAnderenUnit_SperrtNicht(bool wholeSeries)
    {
        var father = await TestApi.AdultAsync(_factory);
        var (seriesId, seriesUnitId, exerciseId) = await CreateVocabExerciseAsync();
        await AddUnitMilestoneAsync(father, seriesUnitId, exerciseId);

        string target;
        if (wholeSeries)
        {
            var (otherSeriesId, _, _) = await CreateVocabExerciseAsync();
            target = $"/api/v1/creator/textbook-series/{otherSeriesId}";
        }
        else
        {
            var neighbourUnitId = await TestApi.IdAsync(await father.PostAsJsonAsync(
                $"/api/v1/creator/textbook-series/{seriesId}/units", new { label = "Unit 2", orderIndex = 2 }));
            target = $"/api/v1/creator/textbook-series/{seriesId}/units/{neighbourUnitId}";
        }

        Assert.Equal(HttpStatusCode.NoContent, (await father.DeleteAsync(target)).StatusCode);
    }

    private static async Task AddUnitMilestoneAsync(HttpClient father, int seriesUnitId, int exerciseId)
    {
        var subjectId = (await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}"))
            .GetProperty("subjectId").GetInt32();
        (await father.PostAsJsonAsync("/api/v1/supervisor/children/1/objectives", new
        {
            title = TestApi.UniqueName("Unitziel"),
            motivation = (string?)null,
            kind = "Committed",
            start = (string?)null,
            dueDate = (string?)null,
            rewardOnComplete = 0,
            rewardPerKeyResult = 0,
            keyResults = new[]
            {
                new { subjectId, seriesUnitId = (int?)seriesUnitId, exerciseId = (int?)null, metric = "AvgMastery", targetValue = 80, title = (string?)null },
            },
        })).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Delete_UnitUndReihe_OhneVerwendung_Loescht()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (seriesId, seriesUnitId, exerciseId) = await CreateVocabExerciseAsync();

        // The protection only applies to *used* exercises - the cascade onto unused ones stays allowed.
        Assert.Equal(HttpStatusCode.NoContent,
            (await father.DeleteAsync($"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await father.GetAsync($"/api/v1/creator/exercises/{exerciseId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await father.DeleteAsync($"/api/v1/creator/textbook-series/{seriesId}")).StatusCode);
    }

    [Fact]
    public async Task Delete_UnbenutzteUebung_Loescht()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (seriesId, seriesUnitId, exerciseId) = await CreateVocabExerciseAsync();

        var res = await father.DeleteAsync($"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary/{exerciseId}");
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

        var after = await father.GetAsync($"/api/v1/creator/exercises/{exerciseId}");
        Assert.Equal(HttpStatusCode.NotFound, after.StatusCode);
    }

    /// <summary>
    /// Since B-106 a subject no longer cascades onto exercises (those hang off a textbook series unit) -
    /// deleting it only clears the FK on series/exercise categories pointing at it, never a 409.
    /// </summary>
    [Fact]
    public async Task Delete_Subject_Loescht()
    {
        var father = await TestApi.AdultAsync(_factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            "/api/v1/creator/subjects", new { name = TestApi.UniqueName("Kat-Mgmt-Loesch-Fach") }));

        var res = await father.DeleteAsync($"/api/v1/creator/subjects/{subjectId}");
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

        var after = await father.GetAsync($"/api/v1/creator/subjects/{subjectId}");
        Assert.Equal(HttpStatusCode.NotFound, after.StatusCode);
    }
}
