using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-606 AK 1: duty, daily chest, bonus lock and streak are judged over all active, running plans of the child. Two
/// active plans can only be set up in the database today - creating a plan through the API switches its siblings off.
/// </summary>
public class KindTagessichtTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private sealed record Setup(HttpClient Child, int ChildId, int PlanA, int PositionA, int PlanB, int PositionB);

    // A fresh child (the chest is once per child and day) with two active plans, each with one daily goal, both
    // running since two days ago.
    private async Task<Setup> SetupAsync()
    {
        var father = await TestApi.AdultAsync(factory);
        var pin = "7" + Random.Shared.Next(100, 999);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Zwei-Plaene-Kind"), pin }));
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father); // hello→hallo, goodbye→tschüss
        var (planA, positionA) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText, childId: childId);
        var (planB, positionB) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText, childId: childId);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        foreach (var plan in db.StudyPlans.Where(p => p.Id == planA || p.Id == planB))
        {
            plan.StartDate = Today.AddDays(-2);
            plan.Title = plan.Id == planA ? "Plan A" : "Plan B";
        }
        // The goals exist since the plans' start (B-613: a duty counts only from the day it was created).
        foreach (var pos in db.PlanPositions.Where(p => p.Id == positionA || p.Id == positionB))
            pos.CreatedAt = Today.AddDays(-2).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        db.SaveChanges();
        return new Setup(await TestApi.ChildAsync(factory, childId, pin), childId, planA, positionA, planB, positionB);
    }

    private DateOnly Today => DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime);

    private static async Task PassTestAsync(Setup s, int planId, int positionId)
    {
        var testsUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";
        var attemptId = await TestApi.IdWithKeyAsync(await s.Child.PostAsJsonAsync(testsUrl, new { }), "attemptId");
        var answers = new[] { new { itemIndex = 0, givenAnswer = "hello" }, new { itemIndex = 1, givenAnswer = "goodbye" } };
        (await s.Child.PostAsJsonAsync($"{testsUrl}/{attemptId}/submit", new { answers })).EnsureSuccessStatusCode();
    }

    // A passed test on a past day, written straight into the database: the API only tests today.
    private void SeedPassedTest(int planId, int positionId, DateOnly day)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var at = day.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc);
        db.TestAttempts.Add(new TestAttempt
        {
            StudyPlanId = planId,
            PlanPositionId = positionId,
            Day = day,
            StageValue = (int)TestStage.FreeText,
            Graded = true,
            StartedAt = at,
            CompletedAt = at,
            TotalItems = 2,
            CorrectItems = 2,
            ScorePercent = 100,
            Passed = true,
        });
        db.SaveChanges();
    }

    private void Deactivate(int planId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        db.StudyPlans.Single(p => p.Id == planId).Active = false;
        db.SaveChanges();
    }

    private static async Task<JsonElement> GetAsync(HttpClient c, string url)
    {
        var res = await c.GetAsync(url);
        Assert.True(res.IsSuccessStatusCode, $"{(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static Task<JsonElement> TodayAsync(Setup s) => GetAsync(s.Child, $"/api/v1/student/children/{s.ChildId}/today");

    private int ClaimCount(int childId)
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<PuglingDbContext>().DailyBoxClaims.Count(c => c.ChildId == childId);
    }

    private async Task<int> StreakAsync(int childId, DateOnly day)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<PositionProgressService>()
            .StreakBoundedAsync(childId, day, CancellationToken.None);
    }

    private async Task<int> MetricAsync(int childId, ProgressMetric metric)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<MetricsService>()
            .ValueAsync(childId, metric, null, null, Today, CancellationToken.None);
    }

    /// <summary>
    /// Plan A's daily goal met, plan B's open: the day view says the duty is open, no chest is earned and the bonus
    /// stays locked; once B is met too, exactly one chest. Red with the chest earned per played plan (a chest after A
    /// alone), and red without the day view (its 404). The bonus lock (<c>Assert.Single(outstanding)</c>) already
    /// judged all active plans before B-606 - that line guards it, it is no red probe for this story.
    /// </summary>
    [Fact]
    public async Task Ein_Plan_Offen_Keine_Truhe_Bonus_Gesperrt_Beide_Erfuellt_Genau_Eine_Truhe()
    {
        var s = await SetupAsync();

        await PassTestAsync(s, s.PlanA, s.PositionA);

        var half = await TodayAsync(s);
        JsonAssert.False(half, "dutyDone");
        JsonAssert.False(half, "rewardsUnlocked");
        Assert.Equal(2, half.GetProperty("goalsTotal").GetInt32());
        Assert.Equal(1, half.GetProperty("goalsMet").GetInt32());
        JsonAssert.False(half.GetProperty("dailyBox"), "claimedToday");
        Assert.Equal(0, ClaimCount(s.ChildId));
        var bonus = await GetAsync(s.Child, $"/api/v1/student/children/{s.ChildId}/extra-training");
        JsonAssert.False(bonus, "unlocked");
        Assert.Single(bonus.GetProperty("outstanding").EnumerateArray());

        // Each position names its plan, so the start page can lead into the right path.
        var positions = half.GetProperty("positions").EnumerateArray().ToList();
        Assert.Equal(2, positions.Count);
        var open = Assert.Single(positions, p => !p.GetProperty("goalMet").GetBoolean());
        Assert.Equal(s.PlanB, open.GetProperty("planId").GetInt32());
        Assert.Equal("Plan B", open.GetProperty("planTitle").GetString());
        Assert.StartsWith("Englisch-Pos", open.GetProperty("subjectName").GetString());

        await PassTestAsync(s, s.PlanB, s.PositionB);

        var full = await TodayAsync(s);
        JsonAssert.True(full, "dutyDone");
        JsonAssert.True(full, "rewardsUnlocked");
        JsonAssert.True(full.GetProperty("dailyBox"), "claimedToday");
        Assert.Equal(1, ClaimCount(s.ChildId));
        JsonAssert.True(await GetAsync(s.Child, $"/api/v1/student/children/{s.ChildId}/extra-training"), "unlocked");
    }

    /// <summary>
    /// Day 1 both plans met, day 2 only plan A: the streak breaks on day 2. Once plan B is switched off, day 2 counts
    /// (decision 6: an inactive plan only counts in the child's favour). Red with the streak of one plan (2 on day 2
    /// while B is open), and red if inactive plans keep breaking the streak (still 0 after switching B off).
    /// </summary>
    [Fact]
    public async Task Serie_Bricht_Am_Offenen_Plan_Und_Zaehlt_Nach_Dessen_Abschalten()
    {
        var s = await SetupAsync();
        var day1 = Today.AddDays(-2);
        var day2 = Today.AddDays(-1);
        SeedPassedTest(s.PlanA, s.PositionA, day1);
        SeedPassedTest(s.PlanB, s.PositionB, day1);
        SeedPassedTest(s.PlanA, s.PositionA, day2);

        Assert.Equal(0, await StreakAsync(s.ChildId, day2));
        Assert.Equal(1, await StreakAsync(s.ChildId, day1));

        // Today both are met through the API: the streak counts today, then stops at the open day 2.
        await PassTestAsync(s, s.PlanA, s.PositionA);
        await PassTestAsync(s, s.PlanB, s.PositionB);
        Assert.Equal(1, (await TodayAsync(s)).GetProperty("currentStreak").GetInt32());
        var overview = await GetAsync(s.Child, $"/api/v1/student/study-plans/{s.PlanA}/overview");
        Assert.Equal(1, overview.GetProperty("currentStreak").GetInt32());
        // The history view carries the same child streak - plan A alone met all three days and would say 3.
        var history = await GetAsync(s.Child, $"/api/v1/student/study-plans/{s.PlanA}/overview/progress");
        Assert.Equal(1, history.GetProperty("currentStreak").GetInt32());

        Deactivate(s.PlanB);

        Assert.Equal(2, await StreakAsync(s.ChildId, day2));
        Assert.Equal(3, (await TodayAsync(s)).GetProperty("currentStreak").GetInt32());
    }

    /// <summary>
    /// Plan B is switched off but still running, with its daily goal open; plan A is active and met: the duty is done,
    /// exactly one chest is earned and the bonus opens - an inactive plan never holds the child's day back. Red when
    /// the day rollup (<c>ComputeChildDayAsync</c>) reads every running plan instead of only the active ones.
    /// </summary>
    [Fact]
    public async Task Abgeschalteter_Laufender_Plan_Haelt_Den_Tag_Nicht_Auf()
    {
        var s = await SetupAsync();
        Deactivate(s.PlanB);

        await PassTestAsync(s, s.PlanA, s.PositionA);

        var today = await TodayAsync(s);
        JsonAssert.True(today, "dutyDone");
        JsonAssert.True(today, "rewardsUnlocked");
        Assert.Equal(1, ClaimCount(s.ChildId));
        JsonAssert.True(await GetAsync(s.Child, $"/api/v1/student/children/{s.ChildId}/extra-training"), "unlocked");
    }

    /// <summary>
    /// The day count and streak behind missions and achievements follow the same rule: day 2 with plan B open is no
    /// complete day while B is active, and is one once B is switched off. Red with the union over all plans in
    /// <c>MetricsService</c> (day 2 counted because plan A met it).
    /// </summary>
    [Fact]
    public async Task Missions_Metrik_Zaehlt_Nur_Tage_Ohne_Offenen_Aktiven_Plan()
    {
        var s = await SetupAsync();
        SeedPassedTest(s.PlanA, s.PositionA, Today.AddDays(-2));
        SeedPassedTest(s.PlanB, s.PositionB, Today.AddDays(-2));
        SeedPassedTest(s.PlanA, s.PositionA, Today.AddDays(-1));

        Assert.Equal(1, await MetricAsync(s.ChildId, ProgressMetric.DaysComplete));
        Assert.Equal(0, await MetricAsync(s.ChildId, ProgressMetric.StreakDays));

        Deactivate(s.PlanB);

        Assert.Equal(2, await MetricAsync(s.ChildId, ProgressMetric.DaysComplete));
        Assert.Equal(2, await MetricAsync(s.ChildId, ProgressMetric.StreakDays));
    }
}
