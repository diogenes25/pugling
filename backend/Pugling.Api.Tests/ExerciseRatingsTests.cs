using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;

namespace Pugling.Api.Tests;

/// <summary>
/// Integration tests for rating shared exercises (B-12): rate/overwrite/delete, the owner exclusion,
/// the aggregate on search and detail, and <c>sort=rating</c>.
/// </summary>
public class ExerciseRatingsTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private async Task<(HttpClient client, int id)> NewCreatorAsync(string name)
    {
        var res = await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = TestApi.UniqueName(name), pin = "3333" });
        res.EnsureSuccessStatusCode();
        var id = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        return (await TestApi.AdultAsync(factory, id, "3333"), id);
    }

    /// <summary>A fresh subject/series/unit of the given author, so that a search filtered on it sees only this test's rows.</summary>
    private static async Task<(int seriesId, int seriesUnitId)> NewUnitAsync(HttpClient author)
    {
        var subjectId = await TestApi.IdAsync(await author.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Rating-Fach") }));
        return await TestApi.CreateSeriesAndUnitAsync(author, subjectId);
    }

    private static async Task<int> NewExerciseAsync(HttpClient author, int seriesId, int seriesUnitId, string title) =>
        await TestApi.IdAsync(await author.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/arithmetic", new
            {
                title,
                orderIndex = 1,
                rewardPoints = 10,
                config = new { problems = new[] { new { prompt = "2 + 2", answer = 4, tolerance = 0 } } },
            }));

    private static Task<HttpResponseMessage> RateAsync(HttpClient c, int exerciseId, int stars, string? comment = null) =>
        c.PutAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/rating", new { stars, comment });

    private static async Task<string?> CodeAsync(HttpResponseMessage res) =>
        (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();

    private static async Task<JsonElement[]> SearchUnitAsync(HttpClient c, int seriesUnitId, string? sort = null)
    {
        var url = $"/api/v1/creator/exercises?seriesUnitId={seriesUnitId}" + (sort is null ? "" : $"&sort={sort}");
        return [.. (await c.GetFromJsonAsync<JsonElement>(url)).EnumerateArray()];
    }

    [Fact]
    public async Task FremderCreator_Bewertet_UndJederCreatorSiehtDieEinzelbewertung()
    {
        var (owner, _) = await NewCreatorAsync("Autor");
        var (rater, raterId) = await NewCreatorAsync("Bewerter");
        var (reader, _) = await NewCreatorAsync("Leser");
        var (seriesId, unitId) = await NewUnitAsync(owner);
        var exerciseId = await NewExerciseAsync(owner, seriesId, unitId, "Plus");

        var res = await RateAsync(rater, exerciseId, 4, "  Klar aufgebaut  ");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var own = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(4, own.GetProperty("stars").GetInt32());
        Assert.Equal("Klar aufgebaut", own.GetProperty("comment").GetString());
        Assert.True(own.GetProperty("isMine").GetBoolean());

        // The widget finds "my rating" through IsMine in the LIST, not in the PUT answer - so check it there.
        var mine = Assert.Single((await rater.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}/ratings"))
            .EnumerateArray().ToList());
        Assert.True(mine.GetProperty("isMine").GetBoolean());

        // AK 4: visible to every creator, the owner included - not only to the rater.
        foreach (var viewer in new[] { owner, reader })
        {
            var list = (await viewer.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}/ratings"))
                .EnumerateArray().ToList();
            var row = Assert.Single(list);
            Assert.Equal(raterId, row.GetProperty("creatorId").GetInt32());
            Assert.StartsWith("Bewerter", row.GetProperty("creatorName").GetString());
            Assert.Equal(4, row.GetProperty("stars").GetInt32());
            Assert.Equal("Klar aufgebaut", row.GetProperty("comment").GetString());
            Assert.False(row.GetProperty("isMine").GetBoolean());
        }
    }

    [Fact]
    public async Task Owner_UndMitOwner_Erhalten403_WriteGrantee_DarfBewerten()
    {
        var (owner, _) = await NewCreatorAsync("Autor");
        var (coOwner, coOwnerId) = await NewCreatorAsync("MitAutor");
        var (writer, writerId) = await NewCreatorAsync("Schreiber");
        var (seriesId, unitId) = await NewUnitAsync(owner);
        var exerciseId = await NewExerciseAsync(owner, seriesId, unitId, "Plus");
        // Both grants must really exist, or "the writer may rate" would hold for a creator without any grant.
        (await owner.PostAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/grants", new { creatorId = coOwnerId, permission = "Owner" }))
            .EnsureSuccessStatusCode();
        (await owner.PostAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/grants", new { creatorId = writerId, permission = "Write" }))
            .EnsureSuccessStatusCode();

        foreach (var own in new[] { owner, coOwner })
        {
            var denied = await RateAsync(own, exerciseId, 5);
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            Assert.Equal("cannot_rate_own_exercise", await CodeAsync(denied));
        }

        // Writing is not administering (decision 2 excludes owners only): a co-author with a write grant rates.
        Assert.Equal(HttpStatusCode.OK, (await RateAsync(writer, exerciseId, 3)).StatusCode);
        var detail = await owner.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}");
        Assert.Equal(1, detail.GetProperty("ratingCount").GetInt32());
    }

    [Fact]
    public async Task Aggregat_Bei_Null_Einer_Und_Mehreren_Bewertungen_In_Suche_Und_Detail()
    {
        var (owner, _) = await NewCreatorAsync("Autor");
        var (r1, _) = await NewCreatorAsync("Bewerter1");
        var (r2, _) = await NewCreatorAsync("Bewerter2");
        var (seriesId, unitId) = await NewUnitAsync(owner);
        var exerciseId = await NewExerciseAsync(owner, seriesId, unitId, "Plus");

        async Task AssertAggregateAsync(double? average, int count)
        {
            var hit = Assert.Single(await SearchUnitAsync(owner, unitId));
            var detail = await owner.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}");
            foreach (var view in new[] { hit, detail })
            {
                var avg = view.GetProperty("averageRating");
                if (average is null) Assert.Equal(JsonValueKind.Null, avg.ValueKind);
                else Assert.Equal(average.Value, avg.GetDouble(), 3);
                Assert.Equal(count, view.GetProperty("ratingCount").GetInt32());
            }
        }

        await AssertAggregateAsync(null, 0);
        Assert.Equal(HttpStatusCode.OK, (await RateAsync(r1, exerciseId, 4)).StatusCode);
        await AssertAggregateAsync(4, 1);
        // 4 and 1: the mean (2.5) is neither of the two values, nor their sum, nor an integer division.
        Assert.Equal(HttpStatusCode.OK, (await RateAsync(r2, exerciseId, 1)).StatusCode);
        await AssertAggregateAsync(2.5, 2);
    }

    [Fact]
    public async Task ErneutesPut_Ueberschreibt_Delete_Entfernt_Nur_Die_Eigene()
    {
        var (owner, _) = await NewCreatorAsync("Autor");
        var (rater, raterId) = await NewCreatorAsync("Bewerter");
        var (other, _) = await NewCreatorAsync("Anderer");
        var (seriesId, unitId) = await NewUnitAsync(owner);
        var exerciseId = await NewExerciseAsync(owner, seriesId, unitId, "Plus");

        var first = await (await RateAsync(rater, exerciseId, 2, "geht so")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.OK, (await RateAsync(other, exerciseId, 5)).StatusCode);
        var second = await (await RateAsync(rater, exerciseId, 4)).Content.ReadFromJsonAsync<JsonElement>();

        // AK 5: never a second row per (exercise, creator); the comment left out is gone, not kept.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            Assert.Equal(1, db.ExerciseRatings.Count(r => r.ExerciseId == exerciseId && r.CreatorId == raterId));
        }
        Assert.Equal(first.GetProperty("createdAt").GetDateTime(), second.GetProperty("createdAt").GetDateTime());
        // B-269: overwriting moves UpdatedAt on, or "newest change first" in the list would sort by the first rating.
        Assert.True(second.GetProperty("updatedAt").GetDateTime() > first.GetProperty("updatedAt").GetDateTime());
        Assert.Equal(JsonValueKind.Null, second.GetProperty("comment").ValueKind);
        var detail = await owner.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}");
        Assert.Equal(4.5, detail.GetProperty("averageRating").GetDouble(), 3);
        Assert.Equal(2, detail.GetProperty("ratingCount").GetInt32());

        Assert.Equal(HttpStatusCode.NoContent, (await rater.DeleteAsync($"/api/v1/creator/exercises/{exerciseId}/rating")).StatusCode);
        var after = (await owner.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}/ratings"))
            .EnumerateArray().ToList();
        // Only the deleting creator's own rating went; the other creator's 5 stays.
        Assert.Equal(5, Assert.Single(after).GetProperty("stars").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await rater.DeleteAsync($"/api/v1/creator/exercises/{exerciseId}/rating")).StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task Sterne_Ausserhalb_1_Bis_5_Sind_400(int stars)
    {
        var (owner, _) = await NewCreatorAsync("Autor");
        var (rater, _) = await NewCreatorAsync("Bewerter");
        var (seriesId, unitId) = await NewUnitAsync(owner);
        var exerciseId = await NewExerciseAsync(owner, seriesId, unitId, "Plus");

        var res = await RateAsync(rater, exerciseId, stars);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("validation_error", await CodeAsync(res));
        Assert.Equal(0, (await owner.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}"))
            .GetProperty("ratingCount").GetInt32());
    }

    [Fact]
    public async Task Unbekannte_Uebung_Ist_404()
    {
        var (rater, _) = await NewCreatorAsync("Bewerter");
        Assert.Equal(HttpStatusCode.NotFound, (await RateAsync(rater, 987654, 3)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await rater.GetAsync("/api/v1/creator/exercises/987654/ratings")).StatusCode);
    }

    [Fact]
    public async Task SortRating_Nach_Durchschnitt_Unbewertete_In_Beiden_Richtungen_Am_Ende()
    {
        var (owner, _) = await NewCreatorAsync("Autor");
        var (r1, _) = await NewCreatorAsync("Bewerter1");
        var (r2, _) = await NewCreatorAsync("Bewerter2");
        var (seriesId, unitId) = await NewUnitAsync(owner);
        // Created in id order A < B < C < D. The default order (all orderIndex 1) is therefore A, B, C, D -
        // different from both expected rating orders, so a sort key that is silently ignored fails.
        var a = await NewExerciseAsync(owner, seriesId, unitId, "A unbewertet");
        var b = await NewExerciseAsync(owner, seriesId, unitId, "B");
        var c = await NewExerciseAsync(owner, seriesId, unitId, "C");
        var d = await NewExerciseAsync(owner, seriesId, unitId, "D unbewertet");
        // B: 1 and 5 → average 3, but maximum 5, count 2 and sum 6. C: 4 → average 4, maximum 4, count 1, sum 4.
        // Among max, count, sum and average, only the average puts B before C. Id order also puts B before C, so
        // "rated first, then Id" passes the ASCENDING assertion; it fails descending, and ascending in the E2E spec.
        // Checked: the discrimination above only holds if all three ratings really landed.
        Assert.Equal(HttpStatusCode.OK, (await RateAsync(r1, b, 1)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await RateAsync(r2, b, 5)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await RateAsync(r1, c, 4)).StatusCode);

        int[] Ids(JsonElement[] rows) => [.. rows.Select(r => r.GetProperty("id").GetInt32())];
        Assert.Equal([b, c, a, d], Ids(await SearchUnitAsync(owner, unitId, "rating")));
        // Descending is how a supervisor looks for the best exercises: the unrated stay at the end instead of
        // mirroring to the front (decided 2026-10-26). [a, d, c, b] would be the mirror.
        Assert.Equal([c, b, a, d], Ids(await SearchUnitAsync(owner, unitId, "-rating")));
    }

    [Fact]
    public async Task Liste_Nennt_Die_Juengste_Aenderung_Zuerst()
    {
        var (owner, _) = await NewCreatorAsync("Autor");
        var (r1, r1Id) = await NewCreatorAsync("Bewerter1");
        var (r2, r2Id) = await NewCreatorAsync("Bewerter2");
        var (r3, r3Id) = await NewCreatorAsync("Bewerter3");
        var (seriesId, unitId) = await NewUnitAsync(owner);
        var exerciseId = await NewExerciseAsync(owner, seriesId, unitId, "Plus");
        // Rated in the order r1, r2, r3, then r1 changes. Newest change first is [r1, r3, r2]; newest creation
        // first would be [r3, r2, r1], row id order [r1, r2, r3] - three different answers, so a list that sorts by
        // anything but UpdatedAt fails (B-269). r1 changes only the COMMENT and keeps all stars equal: a list sorted
        // by stars has nothing to go on, and an UpdatedAt that only moves with the stars stays behind.
        foreach (var rater in new[] { r1, r2, r3 })
            Assert.Equal(HttpStatusCode.OK, (await RateAsync(rater, exerciseId, 3)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await RateAsync(r1, exerciseId, 3, "nachgeschärft")).StatusCode);

        var list = (await owner.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}/ratings"))
            .EnumerateArray().Select(r => r.GetProperty("creatorId").GetInt32()).ToArray();
        Assert.Equal([r1Id, r3Id, r2Id], list);
    }
}
