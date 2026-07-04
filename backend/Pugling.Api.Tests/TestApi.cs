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
        var res = await c.PostAsJsonAsync($"/api/auth/{role}", dto);
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

    /// <summary>Legt (als Vater) einen Vokabel-Lehrplan mit zwei Seed-Vokabeln an und liefert dessen Id.</summary>
    public static async Task<int> CreateVocabPlanAsync(HttpClient father, int childId = 1, bool dailyTestRequired = true)
    {
        var res = await father.PostAsJsonAsync("/api/study-plans", new
        {
            childId,
            title = "Test-Plan",
            method = "Vocabulary",
            durationDays = 5,
            contentKeys = new[] { "en_house_de_haus", "en_go_de_gehen" },
            dailyTestRequired,
        });
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }
}
