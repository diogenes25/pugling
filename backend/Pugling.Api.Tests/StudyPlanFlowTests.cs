using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>Integrationstests für den Ownership-Filter und den Vokabel-Test-Flow (Plan → Test → Submit).</summary>
public class StudyPlanFlowTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private async Task<HttpClient> FatherClientAsync()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/father", new { fatherId = 1, pin = "0000" });
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task PlanSubroute_OhneToken_Liefert401()
    {
        var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/study-plans/999/tests", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task FehlenderPlan_MitToken_Liefert404()
    {
        // Beweist den zentralen PlanOwnershipFilter: existiert nicht / nicht meiner → einheitlich 404.
        var client = await FatherClientAsync();
        var res = await client.PostAsJsonAsync("/api/study-plans/999/tests", new { });

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task VokabelTest_SelfAssess_BestehtUndVergibtPunkte()
    {
        var client = await FatherClientAsync();

        var planRes = await client.PostAsJsonAsync("/api/study-plans", new
        {
            childId = 1,
            title = "Integrationstest-Plan",
            method = "Vocabulary",
            durationDays = 5,
            contentKeys = new[] { "en_house_de_haus", "en_go_de_gehen" },
            dailyTestRequired = true,
        });
        Assert.Equal(HttpStatusCode.Created, planRes.StatusCode);
        var planId = (await planRes.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var startRes = await client.PostAsJsonAsync($"/api/study-plans/{planId}/tests", new { stage = "SelfAssess" });
        Assert.Equal(HttpStatusCode.Created, startRes.StatusCode);
        var attempt = await startRes.Content.ReadFromJsonAsync<JsonElement>();
        var attemptId = attempt.GetProperty("attemptId").GetInt32();
        var answers = attempt.GetProperty("items").EnumerateArray()
            .Select(i => new { vocabularyId = i.GetProperty("vocabularyId").GetInt32(), wasKnown = true })
            .ToArray();

        var submitRes = await client.PostAsJsonAsync($"/api/study-plans/{planId}/tests/{attemptId}/submit", new { answers });
        Assert.Equal(HttpStatusCode.OK, submitRes.StatusCode);
        var result = await submitRes.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(result.GetProperty("passed").GetBoolean());
        Assert.Equal(100, result.GetProperty("scorePercent").GetInt32());
        Assert.True(result.GetProperty("dayProgress").GetProperty("pointsAwarded").GetInt32() > 0);
    }
}
