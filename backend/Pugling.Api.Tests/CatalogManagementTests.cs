using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>Management of catalog exercises: detail GET with config, usage reverse lookup, delete protection.</summary>
public class CatalogManagementTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    [Fact]
    public async Task Detail_LiefertTypConfigUndMetadaten()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);

        var detail = await (await father.GetAsync($"/api/v1/creator/exercises/{exerciseId}"))
            .Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("Vocabulary", detail.GetProperty("type").GetString());
        // The vocabulary lives one level deeper as items of its own; the config only carries settings.
        Assert.Equal("front-to-back", detail.GetProperty("config").GetProperty("direction").GetString());
        Assert.False(string.IsNullOrEmpty(detail.GetProperty("subjectName").GetString()));
    }

    [Fact]
    public async Task Kapitel_MitVorhandenemNamen_Liefert409()
    {
        var father = await TestApi.FatherAsync(_factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            "/api/v1/creator/subjects", new { name = $"Dublette-Fach {Guid.NewGuid():N}" }));

        var first = await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "Unit 1", orderIndex = 1 });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        // Two "Unit 1" in the same subject are a duplicate. Without the pre-check in the controller the unique
        // index would come through as an unhandled 500 - the test pins both down: status AND code.
        var second = await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "Unit 1", orderIndex = 2 });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var problem = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("duplicate_chapter_name", problem.GetProperty("code").GetString());

        // B-97: the SAME conflict via PATCH. Without a pre-check the rename runs straight into the unique
        // index, and the caller gets a 500 with a half-written state instead of the code that already exists.
        var thirdId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "Unit 2", orderIndex = 3 }));
        var renamed = await father.PatchAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{thirdId}", new { name = "Unit 1" });
        Assert.Equal(HttpStatusCode.Conflict, renamed.StatusCode);
        Assert.Equal("duplicate_chapter_name",
            (await renamed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        // The rejected PATCH must leave the chapter untouched - a 409 that already renamed would be worse
        // than the 500 it replaces.
        var unchanged = await (await father.GetAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{thirdId}")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Unit 2", unchanged.GetProperty("name").GetString());

        // Renaming to its OWN name stays legal: the row must not collide with itself.
        var selfRename = await father.PatchAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{thirdId}", new { name = "Unit 2", orderIndex = 4 });
        Assert.Equal(HttpStatusCode.OK, selfRename.StatusCode);

        // A whitespace name is rejected like an empty one - what Create forbids, PATCH must not allow. Without
        // this the name would be written as "", and the SECOND empty name would hit the unique index as a 500:
        // the duplicate check alone does not close the path it was added to.
        var blank = await father.PatchAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{thirdId}", new { name = "   " });
        Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);
        Assert.Equal("validation_error",
            (await blank.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        var stillNamed = await (await father.GetAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{thirdId}")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Unit 2", stillNamed.GetProperty("name").GetString());

        // The same name under a DIFFERENT subject stays allowed - unique is (subject, name), not the name.
        var otherSubject = await TestApi.IdAsync(await father.PostAsJsonAsync(
            "/api/v1/creator/subjects", new { name = $"Dublette-Fach-2 {Guid.NewGuid():N}" }));
        var elsewhere = await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{otherSubject}/chapters", new { name = "Unit 1", orderIndex = 1 });
        Assert.Equal(HttpStatusCode.Created, elsewhere.StatusCode);
    }

    [Fact]
    public async Task Usage_ListetLehrplanMitKind()
    {
        var father = await TestApi.FatherAsync(_factory);
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
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);

        var usage = await (await father.GetAsync($"/api/v1/creator/exercises/{exerciseId}/usage"))
            .Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(0, usage.GetProperty("plans").GetArrayLength());
        Assert.Equal(0, usage.GetProperty("classTests").GetArrayLength());
    }

    [Fact]
    public async Task Delete_ReferenzierteUebung_Liefert409()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)Pugling.Api.Models.TestStage.FreeText);

        var detail = await (await father.GetAsync($"/api/v1/creator/exercises/{exerciseId}"))
            .Content.ReadFromJsonAsync<JsonElement>();
        var (subjectId, chapterId) = (detail.GetProperty("subjectId").GetInt32(), detail.GetProperty("chapterId").GetInt32());

        var res = await father.DeleteAsync($"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}");
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    /// <summary>
    /// Chapter and subject cascade onto their exercises – but the FK <c>PlanPosition→Exercise</c> is
    /// Restrict. Without its own check, the deletion would crash as an FK violation in a bare 500; here
    /// the same clear <c>exercise_in_use</c> conflict must come up as with directly deleting the exercise.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Delete_KapitelOderFach_MitVerwendeterUebung_Liefert409(bool wholeSubject)
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)Pugling.Api.Models.TestStage.FreeText);

        var detail = await (await father.GetAsync($"/api/v1/creator/exercises/{exerciseId}"))
            .Content.ReadFromJsonAsync<JsonElement>();
        var (subjectId, chapterId) = (detail.GetProperty("subjectId").GetInt32(), detail.GetProperty("chapterId").GetInt32());

        var res = await father.DeleteAsync(wholeSubject
            ? $"/api/v1/creator/subjects/{subjectId}"
            : $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}");

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("exercise_in_use", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Delete_KapitelUndFach_OhneVerwendung_Loescht()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var detail = await (await father.GetAsync($"/api/v1/creator/exercises/{exerciseId}"))
            .Content.ReadFromJsonAsync<JsonElement>();
        var (subjectId, chapterId) = (detail.GetProperty("subjectId").GetInt32(), detail.GetProperty("chapterId").GetInt32());

        // The protection only applies to *used* exercises - the cascade onto unused ones stays allowed.
        Assert.Equal(HttpStatusCode.NoContent,
            (await father.DeleteAsync($"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await father.GetAsync($"/api/v1/creator/exercises/{exerciseId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await father.DeleteAsync($"/api/v1/creator/subjects/{subjectId}")).StatusCode);
    }

    [Fact]
    public async Task Delete_UnbenutzteUebung_Loescht()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var detail = await (await father.GetAsync($"/api/v1/creator/exercises/{exerciseId}"))
            .Content.ReadFromJsonAsync<JsonElement>();
        var (subjectId, chapterId) = (detail.GetProperty("subjectId").GetInt32(), detail.GetProperty("chapterId").GetInt32());

        var res = await father.DeleteAsync($"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}");
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

        var after = await father.GetAsync($"/api/v1/creator/exercises/{exerciseId}");
        Assert.Equal(HttpStatusCode.NotFound, after.StatusCode);
    }
}
