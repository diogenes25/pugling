using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// The father's cross-child daily dashboard: shows for each child whether the daily quota is open or done,
/// and updates as soon as the child passes their position test.
/// </summary>
public class ChildrenDashboardTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    private static JsonElement ChildRow(JsonElement dashboard, int childId) =>
        dashboard.GetProperty("children").EnumerateArray().First(c => c.GetProperty("childId").GetInt32() == childId);

    [Fact]
    public async Task Dashboard_SpiegeltTagessoll_VorUndNachTestabschluss()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father); // hello→hallo, goodbye→tschüss
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);

        // Before: the child is visible, the day's target exists but is open.
        var before = await (await father.GetAsync("/api/v1/supervisor/children/daily-overview"))
            .Content.ReadFromJsonAsync<JsonElement>();
        var rowBefore = ChildRow(before, 1);
        Assert.True(rowBefore.GetProperty("goalsTotal").GetInt32() >= 1);
        JsonAssert.False(rowBefore, "dutyDone");

        // Pass the position test → the daily goal is met, the goal points are booked.
        var testsUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";
        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(testsUrl, new { }), "attemptId");
        var answers = new[]
        {
            new { itemIndex = 0, givenAnswer = "hallo" },   // hello → hallo
            new { itemIndex = 1, givenAnswer = "tschüss" }, // goodbye → tschüss
        };
        await child.PostAsJsonAsync($"{testsUrl}/{attemptId}/submit", new { answers });

        // After: the obligation is done, the day's points are visible, marked as "practiced".
        var after = await (await father.GetAsync("/api/v1/supervisor/children/daily-overview"))
            .Content.ReadFromJsonAsync<JsonElement>();
        var rowAfter = ChildRow(after, 1);
        JsonAssert.True(rowAfter, "dutyDone");
        Assert.True(rowAfter.GetProperty("pointsToday").GetInt32() > 0);
        JsonAssert.True(rowAfter, "practiced");
    }
}
