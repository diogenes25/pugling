using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>Happy-Path des Zuordnungs-Verfahrens: Matching-Lehrplan → Test → Submit.</summary>
public class MatchingTestFlowTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task Matching_Plan_Test_Bestehen()
    {
        var father = await TestApi.FatherAsync(factory);
        var planId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/study-plans", new
        {
            childId = 1,
            title = "Matching-Plan",
            method = "Matching",
            durationDays = 5,
            contentKeys = new[] { "en_house_de_haus", "en_go_de_gehen" },
            dailyTestRequired = true,
        }));

        var start = await father.PostAsJsonAsync($"/api/v1/study-plans/{planId}/matching-tests", new { stage = "Direct" });
        Assert.Equal(HttpStatusCode.Created, start.StatusCode);
        var attempt = await start.Content.ReadFromJsonAsync<JsonElement>();
        var attemptId = attempt.GetProperty("attemptId").GetInt32();

        // Direct-Stufe: Prompt = Wort, erwartete Antwort = Übersetzung.
        var expected = new Dictionary<string, string> { ["house"] = "Haus", ["go"] = "gehen" };
        var answers = attempt.GetProperty("items").EnumerateArray().Select(i => new
        {
            vocabularyId = i.GetProperty("vocabularyId").GetInt32(),
            chosenAnswer = expected[i.GetProperty("prompt").GetString()!],
        }).ToArray();

        var submit = await father.PostAsJsonAsync($"/api/v1/study-plans/{planId}/matching-tests/{attemptId}/submit", new { answers });
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);
        var result = await submit.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(100, result.GetProperty("scorePercent").GetInt32());
        Assert.True(result.GetProperty("passed").GetBoolean());
    }
}
