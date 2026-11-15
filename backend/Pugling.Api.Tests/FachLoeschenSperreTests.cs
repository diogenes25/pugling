using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// B-144: deleting a subject used to cascade into rows that belong to a child - key results (with a
/// payout attached to a reached milestone) and timetable entries. The line runs along "can this row exist
/// without a subject?", not along a list of names and not along ownership: every optional
/// <c>SubjectId</c> keeps losing only its assignment - including the child-owned ones (textbook, study
/// plan, class test) - while the two mandatory ones block the delete with 409 <c>subject_in_use</c>.
/// </summary>
public class FachLoeschenSperreTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    /// <summary>
    /// Creates a subject through the real endpoint. Every fixture here goes through the API rather than
    /// the DbContext: the seed contains no objectives, key results or timetable entries at all, so a
    /// hand-inserted row would test the delete path instead of the product path.
    /// </summary>
    private static async Task<int> SubjectAsync(HttpClient creator) =>
        await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Fach") }));

    [Fact]
    public async Task Fach_MitMeilenstein_LaesstSichNichtLoeschen()
    {
        var adult = await TestApi.AdultAsync(factory);
        var subjectId = await SubjectAsync(adult);

        // The key result carries the subject; its scope is mandatory (KeyResult.SubjectId is not nullable).
        var objectiveId = await TestApi.IdAsync(await adult.PostAsJsonAsync(
            "/api/v1/supervisor/children/1/objectives",
            new
            {
                title = TestApi.UniqueName("Ziel"),
                motivation = (string?)null,
                kind = "Committed",
                start = (string?)null,
                dueDate = (string?)null,
                rewardOnComplete = 0,
                rewardPerKeyResult = 0,
                keyResults = new[]
                {
                    new
                    {
                        subjectId,
                        seriesUnitId = (int?)null,
                        exerciseId = (int?)null,
                        metric = "AvgMastery",
                        targetValue = 80,
                        title = "Achtzig Prozent im Schnitt",
                    },
                },
            }));
        Assert.True(objectiveId > 0);

        var res = await adult.DeleteAsync($"/api/v1/creator/subjects/{subjectId}");

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("subject_in_use", problem.GetProperty("code").GetString());
        // The detail names the kind of use, deliberately without a count: knowing there are three of them
        // does not make the subject deletable, and it would cost a second query.
        var detail = problem.GetProperty("detail").GetString();
        Assert.NotNull(detail);
        Assert.DoesNotContain(detail, char.IsDigit);
    }

    [Fact]
    public async Task Fach_MitStundenplanEintrag_LaesstSichNichtLoeschen()
    {
        var adult = await TestApi.AdultAsync(factory);
        var subjectId = await SubjectAsync(adult);

        var entry = await adult.PostAsJsonAsync("/api/v1/supervisor/children/1/timetable",
            new { subjectId, dayOfWeek = "Tuesday", timeOfDay = "08:00" });
        Assert.Equal(HttpStatusCode.Created, entry.StatusCode);

        var res = await adult.DeleteAsync($"/api/v1/creator/subjects/{subjectId}");

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("subject_in_use", problem.GetProperty("code").GetString());
    }

    /// <summary>
    /// B-404 AK 1 and 3: a series of the subject that hosts an exercise blocks the delete - otherwise the
    /// series would be left without a subject while carrying exercises, the state B-401 and B-106 T-01 refuse.
    /// Red probe: drop the exercise check in <c>SubjectsController.Delete</c> and the 409 becomes a 204; move
    /// it after <c>Remove</c>/<c>SaveChanges</c> and the category assertion falls.
    /// </summary>
    [Fact]
    public async Task Fach_MitReiheDieUebungenTraegt_LaesstSichNichtLoeschen()
    {
        var adult = await TestApi.AdultAsync(factory);
        var subjectId = await SubjectAsync(adult);
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(adult, subjectId);
        var categoryId = await TestApi.IdAsync(await adult.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/categories", new { name = "Grammatik" }));
        var exerciseId = await TestApi.IdAsync(await adult.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/arithmetic", new
            {
                title = "Kleines 1×1",
                orderIndex = 1,
                rewardPoints = 10,
                config = new { problems = new[] { new { prompt = "7 × 6", answer = 42, tolerance = 0 } } },
                categoryId,
            }));

        var res = await adult.DeleteAsync($"/api/v1/creator/subjects/{subjectId}");

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("subject_in_use", problem.GetProperty("code").GetString());
        // AK 3: the message names the kind of use and no way out - moving the series or removing its exercises can be
        // locked in turn (B-414 pattern, as B-401).
        var detail = problem.GetProperty("detail").GetString();
        Assert.Contains("series with exercises", detail);
        Assert.DoesNotContain("first", detail);

        // Subject, series and exercise (with its category) are untouched.
        Assert.Equal(HttpStatusCode.OK, (await adult.GetAsync($"/api/v1/creator/subjects/{subjectId}")).StatusCode);
        var series = await adult.GetFromJsonAsync<JsonElement>($"/api/v1/creator/textbook-series/{seriesId}");
        Assert.Equal(subjectId, series.GetProperty("subjectId").GetInt32());
        var exercise = await adult.GetFromJsonAsync<JsonElement>(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/arithmetic/{exerciseId}");
        Assert.Equal(categoryId, exercise.GetProperty("categoryId").GetInt32());
    }

    [Fact]
    public async Task Fach_MitNurEinerReihe_BleibtLoeschbar()
    {
        var adult = await TestApi.AdultAsync(factory);
        var subjectId = await SubjectAsync(adult);

        var seriesId = await TestApi.IdAsync(await adult.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = TestApi.UniqueName("Reihe"), subjectId }));

        // The counterpart to the two locks above: a catalog-internal reference must NOT block, otherwise
        // there would be no way left to get rid of a subject without rehanging every series first.
        var res = await adult.DeleteAsync($"/api/v1/creator/subjects/{subjectId}");
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

        // And it keeps losing only its assignment (SetNull), as before.
        var series = await adult.GetFromJsonAsync<JsonElement>($"/api/v1/creator/textbook-series/{seriesId}");
        Assert.False(series.GetProperty("subjectId").ValueKind is JsonValueKind.Number);
    }
}
