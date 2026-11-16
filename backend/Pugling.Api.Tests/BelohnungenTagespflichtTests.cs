using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-535: the rewards that pull a child in - the daily box and the bonus training - hang on the daily goals only, while
/// streak and evaluation keep counting every goal; and a new word pays its new-content points on its first graded hit,
/// once, however often a wrong try or a free rung came before.
/// </summary>
public class BelohnungenTagespflichtTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private sealed record Setup(HttpClient Child, int ChildId, int PlanId, int DailyId, int WeeklyId);

    // A fresh child (the box is once per child and day) with one plan; `withDaily` adds a daily position next to the
    // weekly one. Both play the same two-word exercise at the free-text stage.
    private async Task<Setup> SetupAsync(bool withDaily)
    {
        var father = await TestApi.AdultAsync(factory);
        var pin = "6" + Random.Shared.Next(100, 999);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Kisten-Kind"), pin }));
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father); // hello→hallo, goodbye→tschüss
        var (planId, weeklyId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText,
            childId: childId, cadence: GoalCadence.Weekly);
        var dailyId = 0;
        if (withDaily)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            var weekly = db.PlanPositions.AsNoTracking().Single(p => p.Id == weeklyId);
            var daily = new PlanPosition
            {
                StudyPlanId = planId,
                ExerciseId = exerciseId,
                Order = 1,
                Stage = weekly.Stage,
                Cadence = GoalCadence.Daily,
                UseLeitner = true,
                NewContentPoints = 10,
                PointsGoalMet = 20,
                CreatedAt = weekly.CreatedAt,
            };
            db.PlanPositions.Add(daily);
            db.SaveChanges();
            dailyId = daily.Id;
            TestApi.MarkIntroduced(factory, dailyId);
        }
        return new Setup(await TestApi.ChildAsync(factory, childId, pin), childId, planId, dailyId, weeklyId);
    }

    private static async Task PassTestAsync(Setup s, int positionId)
    {
        var testsUrl = $"/api/v1/student/study-plans/{s.PlanId}/positions/{positionId}/tests";
        var attemptId = await TestApi.IdWithKeyAsync(await s.Child.PostAsJsonAsync(testsUrl, new { }), "attemptId");
        var answers = new[] { new { itemIndex = 0, givenAnswer = "hello" }, new { itemIndex = 1, givenAnswer = "goodbye" } };
        (await s.Child.PostAsJsonAsync($"{testsUrl}/{attemptId}/submit", new { answers })).EnsureSuccessStatusCode();
    }

    private static async Task<JsonElement> GetAsync(HttpClient c, string url)
    {
        var res = await c.GetAsync(url);
        Assert.True(res.IsSuccessStatusCode, $"{(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static Task<JsonElement> OverviewAsync(Setup s) =>
        GetAsync(s.Child, $"/api/v1/student/study-plans/{s.PlanId}/overview");

    private static Task<JsonElement> BonusAsync(Setup s) =>
        GetAsync(s.Child, $"/api/v1/student/children/{s.ChildId}/extra-training");

    /// <summary>
    /// AK 1 and AK 3: all daily goals met, a weekly goal still open - the box is booked and the bonus training opens,
    /// while the duty (streak, evaluation) stays open. Before the daily goal the box stays shut and the bonus lock names
    /// only the daily goal. Red when the box or the bonus lock read <c>DutyDone</c> again (no box, bonus locked), when
    /// <c>RewardsUnlocked</c> counts the weekly goal, or when <c>DutyDone</c> is narrowed to the daily goals too.
    /// </summary>
    [Fact]
    public async Task Tagesziele_Erfuellt_Wochenziel_Offen_Kiste_Und_Bonus_Offen_Pflicht_Bleibt_Offen()
    {
        var s = await SetupAsync(withDaily: true);

        var before = await OverviewAsync(s);
        JsonAssert.False(before.GetProperty("today"), "rewardsUnlocked");
        JsonAssert.False(before.GetProperty("dailyBox"), "claimedToday");
        var lockedBonus = await BonusAsync(s);
        JsonAssert.False(lockedBonus, "unlocked");
        var outstanding = lockedBonus.GetProperty("outstanding").EnumerateArray().Select(e => e.GetString()!).ToList();
        Assert.Single(outstanding);
        Assert.Contains("Tagesziel", outstanding[0]);

        await PassTestAsync(s, s.DailyId);

        var after = await OverviewAsync(s);
        var today = after.GetProperty("today");
        JsonAssert.True(today, "rewardsUnlocked");
        JsonAssert.False(today, "dutyDone");
        Assert.Equal(2, today.GetProperty("goalsTotal").GetInt32());
        Assert.Equal(1, today.GetProperty("goalsMet").GetInt32());
        JsonAssert.True(after.GetProperty("dailyBox"), "claimedToday");
        JsonAssert.True(await BonusAsync(s), "unlocked");

        // AK 3: the streak still counts every goal - a day with an open weekly goal is no streak day.
        using var scope = factory.Services.CreateScope();
        var progress = scope.ServiceProvider.GetRequiredService<PositionProgressService>();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var plan = db.StudyPlans.AsNoTracking().Single(p => p.Id == s.PlanId);
        var day = DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime);
        Assert.Equal(0, await progress.StreakBoundedAsync(plan, day, CancellationToken.None));
    }

    /// <summary>
    /// AK 2: a plan of weekly goals only keeps the old rule - no box and no bonus until every weekly goal is met.
    /// Red when <c>RewardsUnlocked</c> treats "no daily goal" as "nothing to do" (the box for nothing).
    /// </summary>
    [Fact]
    public async Task Nur_Wochenziele_Kiste_Erst_Wenn_Alle_Erfuellt()
    {
        var s = await SetupAsync(withDaily: false);

        JsonAssert.False((await OverviewAsync(s)).GetProperty("dailyBox"), "claimedToday");
        JsonAssert.False(await BonusAsync(s), "unlocked");

        await PassTestAsync(s, s.WeeklyId);

        var after = await OverviewAsync(s);
        JsonAssert.True(after.GetProperty("today"), "rewardsUnlocked");
        JsonAssert.True(after.GetProperty("dailyBox"), "claimedToday");
        JsonAssert.True(await BonusAsync(s), "unlocked");
    }

    private void SetProgress(int positionId, int itemIndex, int reviewCount, DateTime? newBonusPaidAt)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var prog = db.PositionItemProgress.Single(p => p.PlanPositionId == positionId && p.ItemIndex == itemIndex);
        prog.ReviewCount = reviewCount;
        prog.NewBonusPaidAt = newBonusPaidAt;
        prog.LastReviewedAt = reviewCount > 0 ? factory.Clock.GetUtcNow().UtcDateTime.AddDays(-2) : null;
        prog.DueOn = null;
        db.SaveChanges();
    }

    private async Task<int> ReviewAsync(HttpClient child, int planId, int positionId, int itemIndex, string answer)
    {
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);
        var res = await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, itemIndex, givenAnswer: answer);
        Assert.True(res.IsSuccessStatusCode, $"{(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("awarded").GetInt32();
    }

    /// <summary>
    /// AK 4: a word whose first graded try was wrong (review count 1, bonus unpaid) still pays the new-content points
    /// on its first hit, and marks them paid; a word that already paid them gets the repetition value. Red when "new"
    /// is keyed on the review count again (the first case pays 5), or when the payment is not marked (a later hit
    /// would pay 10 again) or keyed on nothing (the second case pays 10).
    /// </summary>
    [Fact]
    public async Task Neues_Wort_Zahlt_Den_Neu_Bonus_Beim_Ersten_Treffer_Genau_Einmal()
    {
        var father = await TestApi.AdultAsync(factory);
        var pin = "6" + Random.Shared.Next(100, 999);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Neu-Kind"), pin }));
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father); // hello→hallo, goodbye→tschüss
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText, childId: childId);
        var child = await TestApi.ChildAsync(factory, childId, pin);
        var paidBefore = factory.Clock.GetUtcNow().UtcDateTime.AddDays(-2);
        SetProgress(positionId, 0, reviewCount: 1, newBonusPaidAt: null);
        SetProgress(positionId, 1, reviewCount: 1, newBonusPaidAt: paidBefore);

        Assert.Equal(10, await ReviewAsync(child, planId, positionId, 0, "hello"));
        // Box 3 before the hit: the repetition value is max(2, 8 - 3) = 5.
        Assert.Equal(5, await ReviewAsync(child, planId, positionId, 1, "goodbye"));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var progs = db.PositionItemProgress.AsNoTracking().Where(p => p.PlanPositionId == positionId).ToDictionary(p => p.ItemIndex);
        Assert.NotNull(progs[0].NewBonusPaidAt);
        Assert.Equal(paidBefore, progs[1].NewBonusPaidAt);
    }
}
