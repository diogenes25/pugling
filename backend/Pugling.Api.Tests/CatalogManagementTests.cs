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
        // Die Vokabeln leben eine Ebene tiefer als eigene Items; die Config trägt nur noch Einstellungen.
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

        // Zwei „Unit 1" im selben Fach sind eine Dublette. Ohne die Vorprüfung im Controller schlüge der
        // Unique-Index als unbehandelter 500 durch – der Test hält beides fest: Status UND Code.
        var second = await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "Unit 1", orderIndex = 2 });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var problem = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("duplicate_chapter_name", problem.GetProperty("code").GetString());

        // Derselbe Name unter einem ANDEREN Fach bleibt erlaubt – eindeutig ist (Fach, Name), nicht der Name.
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

        // Der Schutz gilt nur für *verwendete* Übungen – die Kaskade auf unbenutzte bleibt erlaubt.
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
