using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>Happy-Path des Lückentext-Verfahrens: Store → Lehrplan → Test → Submit.</summary>
public class ClozeFlowTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task Cloze_Store_Plan_Test_Bestehen()
    {
        var father = await TestApi.FatherAsync(factory);

        var createCloze = await father.PostAsJsonAsync("/api/v1/learn/cloze-texts", new
        {
            key = "en_greet_dialog",
            title = "Greetings",
            sourceLanguage = "en",
            targetLanguage = "de",
            text = "A: {{1}}, how are you? B: I'm {{2}}.",
            gaps = new object[]
            {
                new { index = 1, answer = "Hello", alternatives = new[] { "Hi" } },
                new { index = 2, answer = "fine" },
            },
        });
        Assert.Equal(HttpStatusCode.Created, createCloze.StatusCode);

        var planId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/study-plans", new
        {
            childId = 1,
            title = "Cloze-Plan",
            method = "Cloze",
            durationDays = 5,
            contentKeys = new[] { "en_greet_dialog" },
            dailyTestRequired = true,
        }));

        var start = await father.PostAsJsonAsync($"/api/v1/study-plans/{planId}/cloze-tests", new { stage = "FreeText" });
        Assert.Equal(HttpStatusCode.Created, start.StatusCode);
        var attempt = await start.Content.ReadFromJsonAsync<JsonElement>();
        var attemptId = attempt.GetProperty("attemptId").GetInt32();
        var clozeTextId = attempt.GetProperty("texts")[0].GetProperty("clozeTextId").GetInt32();

        // Kleinschreibung/Alternative werden durch die Normalisierung akzeptiert.
        var submit = await father.PostAsJsonAsync($"/api/v1/study-plans/{planId}/cloze-tests/{attemptId}/submit", new
        {
            answers = new object[]
            {
                new { clozeTextId, gapIndex = 1, givenAnswer = "hi" },
                new { clozeTextId, gapIndex = 2, givenAnswer = "FINE" },
            },
        });
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);
        var result = await submit.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(100, result.GetProperty("scorePercent").GetInt32());
        Assert.True(result.GetProperty("passed").GetBoolean());
    }
}
