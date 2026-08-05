using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;

namespace Pugling.Api.Tests;

/// <summary>
/// Core scenario of the shared exercise library: An <b>English teacher</b> (father account) creates
/// exercises at the level of 9th-grade Gymnasium. <b>Another father</b> finds them via the global
/// catalog, adopts them into their own study plan, and sets up an individual reward – but must not
/// modify/delete them.
/// </summary>
public class SharedLibraryScenarioTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private async Task<(int id, HttpClient client)> RegisterAndLoginAsync(string name, string pin)
    {
        var reg = await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults", new { name, pin });
        reg.EnsureSuccessStatusCode();
        var id = (await reg.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        return (id, await TestApi.AdultAsync(factory, id, pin));
    }

    /// <summary>Creates, as a teacher, subject → series/unit → a 9th-grade Gymnasium vocabulary exercise; returns the ids.</summary>
    private static async Task<(int subjectId, int seriesId, int seriesUnitId, int exerciseId)> CreateGrade9GymExerciseAsync(HttpClient teacher)
    {
        var subjectId = await TestApi.IdAsync(await teacher.PostAsJsonAsync("/api/v1/creator/subjects", new { name = "Englisch (geteilt)" }));
        // A series' slug creation is idempotent (reused on a matching name) - a per-call unique name keeps
        // this fixture from silently reattaching to a different test's series when the DB is shared.
        var seriesId = await TestApi.IdAsync(await teacher.PostAsJsonAsync("/api/v1/creator/textbook-series", new
        {
            name = $"Green Line 5 (geteilt {Guid.NewGuid():N})",
            publisher = (string?)null,
            subjectName = (string?)null,
            subjectId,
            schoolTypes = (object?)null,
            sourceLanguage = (string?)null,
            targetLanguage = (string?)null,
            notes = (string?)null,
        }));
        var seriesUnitId = await TestApi.IdAsync(await teacher.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units", new
            {
                label = "Unit 5 – Global challenges",
                grade = (int?)null,
                orderIndex = 5,
                topics = (string?)null,
                grammar = (string?)null,
                vocabularyNotes = (string?)null,
            }));
        var exerciseId = await TestApi.IdAsync(await teacher.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary", new
            {
                title = "Vocabulary: The environment",
                orderIndex = 1,
                rewardPoints = 15,
                gradeMin = 9,
                gradeMax = 10,
                schoolTypes = "Gymnasium",
                source = "Green Line 5",
                config = new
                {
                    direction = "front-to-back",
                    sourceLang = "en",
                    targetLang = "de",
                    items = new[]
                    {
                        new { front = "sustainability", back = "Nachhaltigkeit" },
                        new { front = "pollution", back = "Umweltverschmutzung" },
                    },
                },
            }));
        return (subjectId, seriesId, seriesUnitId, exerciseId);
    }

    [Fact]
    public async Task Lehrer_ErstelltUebung_TraegtAutorschaftUndIstEditierbar()
    {
        var (teacherId, teacher) = await RegisterAndLoginAsync("Herr Schmidt", "7777");
        var (subjectId, _, _, exerciseId) = await CreateGrade9GymExerciseAsync(teacher);

        // The teacher's catalog search: grade 9 + Gymnasium + subject → their exercise with attribution + IsOwn.
        var hits = await (await teacher.GetAsync(
                $"/api/v1/creator/exercises?subjectId={subjectId}&grade=9&schoolType=Gymnasium"))
            .Content.ReadFromJsonAsync<JsonElement>();

        var mine = hits.EnumerateArray().Single(e => e.GetProperty("id").GetInt32() == exerciseId);
        Assert.Equal(teacherId, mine.GetProperty("authorAdultId").GetInt32());
        Assert.Equal("Herr Schmidt", mine.GetProperty("authorName").GetString());
        JsonAssert.True(mine, "isOwn");
    }

    [Fact]
    public async Task AndererVater_FindetUndUebernimmtUebung_KannSieAberNichtAendern()
    {
        // 1) The teacher creates the exercise.
        var (teacherId, teacher) = await RegisterAndLoginAsync("Frau Meier", "7777");
        var (subjectId, seriesId, seriesUnitId, exerciseId) = await CreateGrade9GymExerciseAsync(teacher);

        // 2) Another adult registers and creates a child.
        var (_, other) = await RegisterAndLoginAsync("Papa Müller", "8888");
        var childId = await TestApi.IdAsync(await other.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = "Tom", grade = 9, schoolType = "Gymnasium" }));

        // 3) The other adult finds the teacher's exercise in the global catalog - marked as someone else's.
        var hits = await (await other.GetAsync(
                $"/api/v1/creator/exercises?subjectId={subjectId}&grade=9&schoolType=Gymnasium"))
            .Content.ReadFromJsonAsync<JsonElement>();
        var found = hits.EnumerateArray().Single(e => e.GetProperty("id").GetInt32() == exerciseId);
        Assert.Equal("Frau Meier", found.GetProperty("authorName").GetString());
        JsonAssert.False(found, "isOwn");

        // 4) They must NOT change or delete it (protecting the other author's work).
        var putBody = new
        {
            title = "Gehackt",
            orderIndex = 1,
            rewardPoints = 999,
            config = new { direction = "front-to-back", items = new[] { new { front = "x", back = "y" } } },
        };
        var put = await other.PutAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary/{exerciseId}", putBody);
        Assert.Equal(HttpStatusCode.Forbidden, put.StatusCode);

        var del = await other.DeleteAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary/{exerciseId}");
        Assert.Equal(HttpStatusCode.Forbidden, del.StatusCode);

        // 5) But they may take it into a study plan of their OWN (the catalog is globally usable).
        var planId = await TestApi.IdAsync(await other.PostAsJsonAsync("/api/v1/supervisor/study-plans",
            new { childId, title = "Toms Englisch-Plan", durationDays = 14 }));
        var posRes = await other.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId, useLeitner = true });
        Assert.Equal(HttpStatusCode.Created, posRes.StatusCode);
        var pos = await posRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(exerciseId, pos.GetProperty("exerciseId").GetInt32());

        // 6) And they set up a listing for their child in their family shop.
        var listingId = await TestApi.CreateShopListingAsync(other, "GAME-1", coinPrice: 300, unitsPerPurchase: 60,
            stock: 2, articleTitle: "Zockzeit", listingTitle: "1 Stunde Zocken", unitType: "Minute", actionType: "Zocken");
        Assert.True(listingId > 0);

        // 7) The teacher themselves may still change their exercise.
        var teacherPut = await teacher.PutAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary/{exerciseId}",
            new
            {
                title = "Vocabulary: The environment (überarbeitet)",
                orderIndex = 1,
                rewardPoints = 18,
                gradeMin = 9,
                gradeMax = 10,
                schoolTypes = "Gymnasium",
                config = new { direction = "front-to-back", sourceLang = "en", targetLang = "de", items = new[] { new { front = "waste", back = "Abfall" } } },
            });
        Assert.Equal(HttpStatusCode.OK, teacherPut.StatusCode);
        _ = teacherId;
    }

    [Fact]
    public async Task GeseedeteSystemUebung_IstFuerNiemandenEditierbar()
    {
        // The seeded catalog exercises (English "Begrüßungen") have no author → not editable.
        var (_, father) = await RegisterAndLoginAsync("Irgendwer", "8888");
        var hits = await (await father.GetAsync("/api/v1/creator/exercises?search=Begrüßungen"))
            .Content.ReadFromJsonAsync<JsonElement>();
        var seeded = hits.EnumerateArray().First();
        Assert.True(seeded.GetProperty("authorAdultId").ValueKind == JsonValueKind.Null);

        var seriesUnitId = seeded.GetProperty("seriesUnitId").GetInt32();
        var id = seeded.GetProperty("id").GetInt32();

        // The catalog search only carries the unit, not its series - resolved via the DB the same way the
        // seed itself knows it (there is no lookup route for a unit's series without already knowing it).
        using var scope = factory.Services.CreateScope();
        var seriesId = await scope.ServiceProvider.GetRequiredService<PuglingDbContext>()
            .SeriesUnits.Where(u => u.Id == seriesUnitId).Select(u => u.SeriesId).FirstAsync();

        var del = await father.DeleteAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary/{id}");
        Assert.Equal(HttpStatusCode.Forbidden, del.StatusCode);
    }
}
