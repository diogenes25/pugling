using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Learning report per position (father's view "which vocabulary sticks/doesn't stick"): reflects the
/// Leitner state (box → mastery) and the test hit rate per content item. Replaces the plan-wide
/// report that was dropped in the study plan rebuild.
/// </summary>
public class PositionReportTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    [Fact]
    public async Task Report_SpiegeltLeitnerBoxUndTesttreffer_ProItem()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father); // hello→hallo, goodbye→tschüss
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var reportUrl = $"/api/v1/supervisor/study-plans/{planId}/positions/{positionId}/report";

        // Practice: both contents correct once → one box up each and introduced.
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);
        await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 0, givenAnswer: "hallo");
        await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 1, givenAnswer: "tschüss");

        var report = await (await father.GetAsync(reportUrl)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, report.GetProperty("totalItems").GetInt32());
        Assert.Equal(2, report.GetProperty("introducedItems").GetInt32());
        Assert.Equal(0, report.GetProperty("masteredItems").GetInt32()); // MaxBox 5, not reached after one round

        var items = report.GetProperty("items").EnumerateArray().ToList();
        var good = items.First(i => i.GetProperty("prompt").GetString() == "hello");
        JsonAssert.True(good, "introduced");
        Assert.True(good.GetProperty("box").GetInt32() > 1);
        Assert.True(good.GetProperty("masteryPercent").GetInt32() > 0);
        Assert.Equal("hallo", good.GetProperty("answer").GetString()); // the solution is visible to the supervisor

        // A test produces the test hit rate per item (item 0 correct, item 1 wrong).
        var testsUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";
        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(testsUrl, new { }), "attemptId");
        var answers = new[]
        {
            new { itemIndex = 0, givenAnswer = "hallo" },  // hello → hallo (richtig)
            new { itemIndex = 1, givenAnswer = "falsch" }, // goodbye → falsch
        };
        await child.PostAsJsonAsync($"{testsUrl}/{attemptId}/submit", new { answers });

        var report2 = await (await father.GetAsync(reportUrl)).Content.ReadFromJsonAsync<JsonElement>();
        var items2 = report2.GetProperty("items").EnumerateArray().ToList();
        var good2 = items2.First(i => i.GetProperty("prompt").GetString() == "hello");
        var bad2 = items2.First(i => i.GetProperty("prompt").GetString() == "goodbye");
        Assert.Equal(1, good2.GetProperty("testsSeen").GetInt32());
        Assert.Equal(1, good2.GetProperty("testsCorrect").GetInt32());
        Assert.Equal(1, bad2.GetProperty("testsSeen").GetInt32());
        Assert.Equal(0, bad2.GetProperty("testsCorrect").GetInt32());
    }

    [Fact]
    public async Task Report_UnbekanntePosition_Liefert404()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);

        var res = await father.GetAsync($"/api/v1/supervisor/study-plans/{planId}/positions/{positionId + 999}/report");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
