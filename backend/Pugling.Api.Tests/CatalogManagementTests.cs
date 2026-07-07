using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>Verwaltung von Katalog-Übungen: Detail-GET mit Config, Usage-Rückwärts-Lookup, Lösch-Schutz.</summary>
public class CatalogManagementTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    [Fact]
    public async Task Detail_LiefertTypConfigUndMetadaten()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);

        var detail = await (await father.GetAsync($"/api/v1/learn/exercises/{exerciseId}"))
            .Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("Vocabulary", detail.GetProperty("type").GetString());
        // Die Vokabeln leben eine Ebene tiefer als eigene Items; die Config trägt nur noch Einstellungen.
        Assert.Equal("front-to-back", detail.GetProperty("config").GetProperty("direction").GetString());
        Assert.False(string.IsNullOrEmpty(detail.GetProperty("subjectName").GetString()));
    }

    [Fact]
    public async Task Usage_ListetLehrplanMitKind()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, _) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)Pugling.Api.Models.TestStage.FreeText);

        var usage = await (await father.GetAsync($"/api/v1/learn/exercises/{exerciseId}/usage"))
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

        var usage = await (await father.GetAsync($"/api/v1/learn/exercises/{exerciseId}/usage"))
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

        var detail = await (await father.GetAsync($"/api/v1/learn/exercises/{exerciseId}"))
            .Content.ReadFromJsonAsync<JsonElement>();
        var (subjectId, chapterId) = (detail.GetProperty("subjectId").GetInt32(), detail.GetProperty("chapterId").GetInt32());

        var res = await father.DeleteAsync($"/api/v1/learn/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}");
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Delete_UnbenutzteUebung_Loescht()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var detail = await (await father.GetAsync($"/api/v1/learn/exercises/{exerciseId}"))
            .Content.ReadFromJsonAsync<JsonElement>();
        var (subjectId, chapterId) = (detail.GetProperty("subjectId").GetInt32(), detail.GetProperty("chapterId").GetInt32());

        var res = await father.DeleteAsync($"/api/v1/learn/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}");
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

        var after = await father.GetAsync($"/api/v1/learn/exercises/{exerciseId}");
        Assert.Equal(HttpStatusCode.NotFound, after.StatusCode);
    }
}
