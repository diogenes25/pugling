using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Strukturierte Übungs-Metadaten (Klassenstufe, Schulart, Quelle, Art) und die
/// darauf aufbauende Vorfilterung über <c>GET api/learn/exercises</c>.
/// </summary>
public class ExerciseMetadataTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    // Legt Fach + Art an und liefert (subjectId, chapterId, categoryId).
    private static async Task<(int subjectId, int chapterId, int categoryId)> SetupAsync(HttpClient father, string subject)
    {
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = subject }));
        var chapterId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "Kapitel 1", orderIndex = 1 }));
        var categoryId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/categories", new { name = "Grammatik" }));
        return (subjectId, chapterId, categoryId);
    }

    private static object ArithmeticBody(string title, int categoryId, int gradeMin, int gradeMax, string schoolTypes) => new
    {
        title,
        orderIndex = 1,
        rewardPoints = 10,
        config = new { problems = new[] { new { prompt = "1 + 1", answer = 2, tolerance = 0 } } },
        gradeMin,
        gradeMax,
        schoolTypes,
        source = "Testbuch, Kapitel 1",
        categoryId,
    };

    [Fact]
    public async Task Uebung_TraegtMetadaten_UndFilterFindetSie()
    {
        var father = await TestApi.FatherAsync(factory);
        var (subjectId, chapterId, categoryId) = await SetupAsync(father, $"Meta-Fach-{Guid.NewGuid():N}");
        var basePath = $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/arithmetic";

        // Anlegen mit Metadaten: Klasse 5–7, Gymnasium.
        var created = await father.PostAsJsonAsync(basePath, ArithmeticBody("Grammatik-Drill", categoryId, 5, 7, "Gymnasium"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var body = await created.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(5, body.GetProperty("gradeMin").GetInt32());
        Assert.Equal("Grammatik", body.GetProperty("categoryName").GetString());
        Assert.Equal("Gymnasium", body.GetProperty("schoolTypes").GetString());

        // Vorfilterung: Fach + Klasse 6 + Gymnasium + Art → Treffer.
        var hit = await Search(father, subjectId, grade: 6, schoolType: "Gymnasium", categoryId: categoryId);
        Assert.Contains(hit.EnumerateArray(), e => e.GetProperty("title").GetString() == "Grammatik-Drill");
    }

    [Fact]
    public async Task Filter_SchliesstFalscheKlassenstufeUndSchulartAus()
    {
        var father = await TestApi.FatherAsync(factory);
        var (subjectId, chapterId, categoryId) = await SetupAsync(father, $"Meta-Fach-{Guid.NewGuid():N}");
        var basePath = $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/arithmetic";
        await father.PostAsJsonAsync(basePath, ArithmeticBody("Nur-Gym-5bis7", categoryId, 5, 7, "Gymnasium"));

        // Klasse 3 liegt unter GradeMin → kein Treffer.
        var tooYoung = await Search(father, subjectId, grade: 3);
        Assert.Empty(tooYoung.EnumerateArray());

        // Realschule ist nicht gesetzt → kein Treffer.
        var wrongSchool = await Search(father, subjectId, schoolType: "Realschule");
        Assert.Empty(wrongSchool.EnumerateArray());

        // Passende Klasse ohne weitere Filter → Treffer.
        var ok = await Search(father, subjectId, grade: 6);
        Assert.Single(ok.EnumerateArray());
    }

    [Fact]
    public async Task Uebung_MitFremderArt_Wird_Abgelehnt()
    {
        var father = await TestApi.FatherAsync(factory);
        var (subjectId, chapterId, _) = await SetupAsync(father, $"Meta-Fach-{Guid.NewGuid():N}");
        var (_, _, fremdeArtId) = await SetupAsync(father, $"Anderes-Fach-{Guid.NewGuid():N}");
        var basePath = $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/arithmetic";

        // Art gehört zu einem anderen Fach → BadRequest.
        var res = await father.PostAsJsonAsync(basePath, ArithmeticBody("Falsche Art", fremdeArtId, 5, 7, "Gymnasium"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    private static async Task<JsonElement> Search(HttpClient father, int subjectId,
        int? grade = null, string? schoolType = null, int? categoryId = null)
    {
        var query = $"?subjectId={subjectId}";
        if (grade is int g) query += $"&grade={g}";
        if (schoolType is not null) query += $"&schoolType={schoolType}";
        if (categoryId is int c) query += $"&categoryId={c}";
        var res = await father.GetAsync($"/api/v1/creator/exercises{query}");
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }
}
