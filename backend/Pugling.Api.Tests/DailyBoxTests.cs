using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Daily reward box (B-105): granted once per fully met day, alongside the position goal reward, with a
/// coin/gem draw that scales with the streak. The positive counterpart to <see cref="PositionGoalPenalty"/>.
/// </summary>
public class DailyBoxTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task BestandenerPositionsTest_ErfuelltTagesziel_GewaehrtGenauEineBoxProTag()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father); // hello→hallo, goodbye→tschüss
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(factory);
        var testsUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        // Before the duty is met: the overview shows no box yet.
        var before = await (await child.GetAsync($"/api/v1/student/study-plans/{planId}/overview"))
            .Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.False(before.GetProperty("dailyBox"), "claimedToday");

        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(testsUrl, new { }), "attemptId");
        var answers = new[]
        {
            new { itemIndex = 0, givenAnswer = "hallo" },
            new { itemIndex = 1, givenAnswer = "tschüss" },
        };
        (await child.PostAsJsonAsync($"{testsUrl}/{attemptId}/submit", new { answers })).EnsureSuccessStatusCode();

        var after = await (await child.GetAsync($"/api/v1/student/study-plans/{planId}/overview"))
            .Content.ReadFromJsonAsync<JsonElement>();
        var box = after.GetProperty("dailyBox");
        JsonAssert.True(box, "claimedToday");
        var coins = box.GetProperty("coinsAwarded").GetInt32();
        var gems = box.GetProperty("gemsAwarded").GetInt32();
        Assert.InRange(coins, 10, 30);
        Assert.InRange(gems, 0, 2);
        Assert.Equal(1, box.GetProperty("streakAtClaim").GetInt32());

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            Assert.Equal(1, db.DailyBoxClaims.Count(c => c.ChildId == 1 && c.Day == DateOnly.FromDateTime(DateTime.UtcNow)));
        }

        // A second passed test on the same day → no second box (idempotent per day).
        var attempt2 = await (await child.PostAsJsonAsync(testsUrl, new { }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var attemptId2 = attempt2.GetProperty("attemptId").GetInt32();
        (await child.PostAsJsonAsync($"{testsUrl}/{attemptId2}/submit", new { answers })).EnsureSuccessStatusCode();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            Assert.Equal(1, db.DailyBoxClaims.Count(c => c.ChildId == 1 && c.Day == DateOnly.FromDateTime(DateTime.UtcNow)));
        }
    }

    /// <summary>
    /// A streak of 7 consecutive fully met days (six seeded past days plus today) crosses the first
    /// escalation tier (×1.5) - the box's coin draw must then exceed the base range's maximum.
    /// Past days are seeded via passed <see cref="TestAttempt"/> rows, exactly the evidence
    /// <c>PositionProgressService.IsGoalMetAsync</c> itself reads for a closed daily period.
    /// </summary>
    [Fact]
    public async Task SiebenTageStreak_SkaliertDieBoxUeberDieBasisspanneHinaus()
    {
        var father = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = "Streak-Kind", pin = "8302" }));
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText, childId: childId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            var plan = db.StudyPlans.First(p => p.Id == planId);
            // Widen the plan's runtime backward far enough for the 6 seeded past days plus today.
            plan.StartDate = today.AddDays(-10);
            for (var i = 1; i <= 6; i++)
            {
                db.TestAttempts.Add(new TestAttempt
                {
                    StudyPlanId = planId,
                    PlanPositionId = positionId,
                    Day = today.AddDays(-i),
                    CompletedAt = DateTime.UtcNow.AddDays(-i),
                    Passed = true,
                    TotalItems = 2,
                    CorrectItems = 2,
                    ScorePercent = 100,
                });
            }
            db.SaveChanges();
        }

        var child = await TestApi.ChildAsync(factory, childId, "8302");
        var testsUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";
        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(testsUrl, new { }), "attemptId");
        var answers = new[]
        {
            new { itemIndex = 0, givenAnswer = "hallo" },
            new { itemIndex = 1, givenAnswer = "tschüss" },
        };
        (await child.PostAsJsonAsync($"{testsUrl}/{attemptId}/submit", new { answers })).EnsureSuccessStatusCode();

        var overview = await (await child.GetAsync($"/api/v1/student/study-plans/{planId}/overview"))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(7, overview.GetProperty("currentStreak").GetInt32());

        var box = overview.GetProperty("dailyBox");
        JsonAssert.True(box, "claimedToday");
        Assert.Equal(7, box.GetProperty("streakAtClaim").GetInt32());
        // Base range is [10,30]; the ×1.5 tier from streak 7 pushes the ceiling to 45.
        Assert.InRange(box.GetProperty("coinsAwarded").GetInt32(), 10, 45);

        using var check = factory.Services.CreateScope();
        var checkDb = check.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var claim = checkDb.DailyBoxClaims.Single(c => c.ChildId == childId && c.Day == today);
        Assert.Equal(7, claim.StreakAtClaim);
    }
}
