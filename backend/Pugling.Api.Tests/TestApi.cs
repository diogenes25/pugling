using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Pugling.Api.Tests;

/// <summary>Geteilte Helfer für die Integrationstests: Login (Vater/Sohn) und Plan-Anlage.</summary>
internal static class TestApi
{
    private static async Task<string> TokenAsync(HttpClient c, string role, object dto)
    {
        var res = await c.PostAsJsonAsync($"/api/v1/auth/{role}", dto);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }

    /// <summary>Client mit Vater-Token (Default: der geseedete Papa, id 1 / PIN 0000).</summary>
    public static async Task<HttpClient> FatherAsync(WebApplicationFactory<Program> f, int id = 1, string pin = "0000")
    {
        var c = f.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await TokenAsync(c, "father", new { fatherId = id, pin }));
        return c;
    }

    /// <summary>Client mit Sohn-Token (Default: der geseedete Sohn, id 1 / PIN 1111).</summary>
    public static async Task<HttpClient> ChildAsync(WebApplicationFactory<Program> f, int id = 1, string pin = "1111")
    {
        var c = f.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await TokenAsync(c, "child", new { childId = id, pin }));
        return c;
    }

    /// <summary>Liest die <c>id</c> aus einer erfolgreichen JSON-Antwort.</summary>
    public static async Task<int> IdAsync(HttpResponseMessage res)
    {
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    /// <summary>Legt (als Vater) einen Vokabel-Lehrplan mit zwei Seed-Vokabeln an und liefert dessen Id.</summary>
    public static async Task<int> CreateVocabPlanAsync(HttpClient father, int childId = 1, bool dailyTestRequired = true)
    {
        var res = await father.PostAsJsonAsync("/api/v1/study-plans", new
        {
            childId,
            title = "Test-Plan",
            method = "Vocabulary",
            durationDays = 5,
            contentKeys = new[] { "en_house_de_haus", "en_go_de_gehen" },
            dailyTestRequired,
        });
        return await IdAsync(res);
    }

    /// <summary>Legt (als Vater) Fach → Kapitel → eine Rechen-Übung an und liefert deren Ids.</summary>
    public static async Task<(int subjectId, int chapterId, int exerciseId)> CreateArithmeticExerciseAsync(HttpClient father)
    {
        var subjectId = await IdAsync(await father.PostAsJsonAsync("/api/v1/learn/subjects", new { name = "Katalog-Test" }));
        var chapterId = await IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/learn/subjects/{subjectId}/chapters", new { name = "Kapitel 1", orderIndex = 1 }));
        var exerciseId = await IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/learn/subjects/{subjectId}/chapters/{chapterId}/arithmetic", new
            {
                title = "Kleines 1×1",
                orderIndex = 1,
                rewardPoints = 10,
                config = new { problems = new[] { new { prompt = "7 × 6", answer = 42, tolerance = 0 } } },
            }));
        return (subjectId, chapterId, exerciseId);
    }
}
