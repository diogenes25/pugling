using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-611: the reward goals (daily chest, bonus training) are chosen over the child's whole day - the daily goals of all
/// active plans as soon as any plan has one, else every goal - not plan by plan. B-612 AK 1: <c>today</c> carries count,
/// met and kind of exactly that list. Two active plans are set up in the database, as in <see cref="KindTagessichtTests"/>.
/// Every case names the change that makes it red.
/// </summary>
public class BelohnungUeberPlaeneTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private sealed record Kid(HttpClient Father, HttpClient Child, int ChildId, int ExerciseId);

    private DateOnly Today => DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime);

    // A fresh child (the chest is once per child and day) with a two-word vocabulary exercise.
    private async Task<Kid> KidAsync()
    {
        var father = await TestApi.AdultAsync(factory);
        var pin = "5" + Random.Shared.Next(100, 999);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Belohnungs-Kind"), pin }));
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father); // hello→hallo, goodbye→tschüss
        return new Kid(father, await TestApi.ChildAsync(factory, childId, pin), childId, exerciseId);
    }

    // An active plan of the child with one goal of the cadence; both plans of a case stay active (database setup).
    private (int PlanId, int PositionId) Plan(Kid k, GoalCadence cadence) =>
        TestApi.SeedLeitnerPosition(factory, k.ExerciseId, (int)TestStage.FreeText, childId: k.ChildId, cadence: cadence);

    // A further goal in an existing plan, created on the plan's first day like the first one.
    private int AddPosition(int planId, GoalCadence cadence)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var first = db.PlanPositions.First(p => p.StudyPlanId == planId);
        var pos = new PlanPosition
        {
            StudyPlanId = planId,
            CreatedAt = first.CreatedAt,
            ExerciseId = first.ExerciseId,
            Order = 1,
            Stage = first.Stage,
            Cadence = cadence,
            UseLeitner = true,
            PointsGoalMet = 20,
        };
        db.PlanPositions.Add(pos);
        db.SaveChanges();
        TestApi.MarkIntroduced(factory, pos.Id);
        return pos.Id;
    }

    // Meets the goal through the API: a passed test, the completion that earns the chest.
    private static async Task PassTestAsync(Kid k, int planId, int positionId)
    {
        var testsUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";
        var attemptId = await TestApi.IdWithKeyAsync(await k.Child.PostAsJsonAsync(testsUrl, new { }), "attemptId");
        var answers = new[] { new { itemIndex = 0, givenAnswer = "hello" }, new { itemIndex = 1, givenAnswer = "goodbye" } };
        (await k.Child.PostAsJsonAsync($"{testsUrl}/{attemptId}/submit", new { answers })).EnsureSuccessStatusCode();
    }

    // A passed test written straight into the database: meets the goal without a completion (no chest is evaluated).
    private void SeedPassedTest(int planId, int positionId, DateOnly day)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var at = day.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc);
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

    private static async Task<JsonElement> GetAsync(HttpClient c, string url)
    {
        var res = await c.GetAsync(url);
        Assert.True(res.IsSuccessStatusCode, $"{(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static Task<JsonElement> TodayAsync(Kid k) => GetAsync(k.Child, $"/api/v1/student/children/{k.ChildId}/today");

    private static Task<JsonElement> BonusAsync(Kid k) =>
        GetAsync(k.Child, $"/api/v1/student/children/{k.ChildId}/extra-training");

    private int ClaimCount(int childId)
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<PuglingDbContext>().DailyBoxClaims.Count(c => c.ChildId == childId);
    }

    private static void AssertRewardGoals(JsonElement today, string cadence, int met, int total)
    {
        Assert.Equal(cadence, today.GetProperty("rewardCadence").GetString());
        Assert.Equal(met, today.GetProperty("rewardGoalsMet").GetInt32());
        Assert.Equal(total, today.GetProperty("rewardGoalsTotal").GetInt32());
        // B-612 AK 1: the figures and the unlock come from one list.
        Assert.Equal(total > 0 && met == total, today.GetProperty("rewardsUnlocked").GetBoolean());
    }

    /// <summary>
    /// B-611 AK 1 and B-612 AK 1: plan A with two daily goals, plan B with one open weekly goal. Before playing the reward
    /// goals are A's two, 0 of 2; once both are met the chest is earned exactly once, <c>today</c> says unlocked (2 of 2,
    /// kind daily) while the duty stays open, and the bonus training is open with nothing listed. Red with the choice per
    /// plan (B's weekly goal joins the reward goals: 2 of 3, locked, no chest) and red when the figures are counted
    /// apart from <c>RewardObligations</c>.
    /// </summary>
    [Fact]
    public async Task Tagesziele_Erfuellt_Wochenziel_Im_Anderen_Plan_Offen_Truhe_Und_Bonus_Offen()
    {
        var k = await KidAsync();
        var (planA, a1) = Plan(k, GoalCadence.Daily);
        var a2 = AddPosition(planA, GoalCadence.Daily);
        Plan(k, GoalCadence.Weekly);
        AssertRewardGoals(await TodayAsync(k), "Daily", 0, 2);

        await PassTestAsync(k, planA, a1);
        await PassTestAsync(k, planA, a2);

        var today = await TodayAsync(k);
        AssertRewardGoals(today, "Daily", 2, 2);
        JsonAssert.False(today, "dutyDone");
        Assert.Equal(1, ClaimCount(k.ChildId));
        var bonus = await BonusAsync(k);
        JsonAssert.True(bonus, "unlocked");
        Assert.Empty(bonus.GetProperty("outstanding").EnumerateArray());
        Assert.Equal("Daily", bonus.GetProperty("rewardCadence").GetString());
    }

    /// <summary>
    /// B-611 AK 2: the same plans, the weekly goal met and the daily goals not ⇒ no chest, the bonus locked on the daily
    /// goals. Red if a weekly goal may stand in for a daily one, or if the choice were "any plan done".
    /// </summary>
    [Fact]
    public async Task Wochenziel_Erfuellt_Tagesziele_Offen_Keine_Truhe()
    {
        var k = await KidAsync();
        var (planA, _) = Plan(k, GoalCadence.Daily);
        AddPosition(planA, GoalCadence.Daily);
        var (planB, b) = Plan(k, GoalCadence.Weekly);

        await PassTestAsync(k, planB, b);

        AssertRewardGoals(await TodayAsync(k), "Daily", 0, 2);
        Assert.Equal(0, ClaimCount(k.ChildId));
        var bonus = await BonusAsync(k);
        JsonAssert.False(bonus, "unlocked");
        var outstanding = bonus.GetProperty("outstanding").EnumerateArray().Select(o => o.GetString()!).ToList();
        Assert.Equal(2, outstanding.Count);
        Assert.All(outstanding, o => Assert.Contains("(Tagesziel)", o));
    }

    /// <summary>
    /// B-611 AK 3: two plans with weekly goals only - one met, one open ⇒ no chest and the bonus locked; both met ⇒
    /// exactly one chest. Red if "no daily goal" read as "nothing to do" (the chest for nothing, B-535 AK 2).
    /// </summary>
    [Fact]
    public async Task Nur_Wochenziele_Ueber_Zwei_Plaene_Brauchen_Beide()
    {
        var k = await KidAsync();
        var (planA, a) = Plan(k, GoalCadence.Weekly);
        var (planB, b) = Plan(k, GoalCadence.Weekly);

        await PassTestAsync(k, planA, a);
        Assert.Equal(0, ClaimCount(k.ChildId));
        JsonAssert.False(await BonusAsync(k), "unlocked");
        AssertRewardGoals(await TodayAsync(k), "Weekly", 1, 2);

        await PassTestAsync(k, planB, b);
        Assert.Equal(1, ClaimCount(k.ChildId));
    }

    /// <summary>
    /// B-611 AK 4: a plan without any duty next to a plan with an open weekly goal ⇒ no chest, even after a completion in
    /// the free plan. Red if the free plan waved the day through as "no daily goal, so free".
    /// </summary>
    [Fact]
    public async Task Pflichtloser_Plan_Winkt_Den_Tag_Nicht_Durch()
    {
        var k = await KidAsync();
        var (freePlan, free) = Plan(k, GoalCadence.None);
        Plan(k, GoalCadence.Weekly);

        await PassTestAsync(k, freePlan, free);

        Assert.Equal(0, ClaimCount(k.ChildId));
        AssertRewardGoals(await TodayAsync(k), "Weekly", 0, 1);
    }

    /// <summary>
    /// B-611 AK 5: duty and streak stay untouched. Plan A (daily) ran since yesterday and was met then; plan B with an
    /// open weekly goal starts today. Once A is met today the chest opens, but the day is not complete: the duty stays
    /// open and today does not join the streak (0, as the streak counts from today). Red if the new choice also narrowed
    /// <c>DutyDone</c>/the streak to the daily goals (<c>dutyDone</c> true, streak 2 with yesterday).
    /// </summary>
    [Fact]
    public async Task Pflicht_Und_Serie_Bleiben_Unberuehrt()
    {
        var k = await KidAsync();
        var (planA, a) = Plan(k, GoalCadence.Daily);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            db.StudyPlans.Single(p => p.Id == planA).StartDate = Today.AddDays(-1);
            db.PlanPositions.Single(p => p.Id == a).CreatedAt = Today.AddDays(-1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            db.SaveChanges();
        }
        SeedPassedTest(planA, a, Today.AddDays(-1));
        Plan(k, GoalCadence.Weekly);

        await PassTestAsync(k, planA, a);

        var today = await TodayAsync(k);
        JsonAssert.True(today, "rewardsUnlocked");
        JsonAssert.False(today, "dutyDone");
        // The streak counts from today, and today is open: 0. Narrowed to the daily goals today would be complete - 2.
        Assert.Equal(0, today.GetProperty("currentStreak").GetInt32());
    }

    /// <summary>
    /// B-612 AK 5: the choice cases the start page used to compute itself (<c>belohnungsZiele</c>), now at the server with
    /// one plan - daily next to weekly ⇒ the daily goal, 1 of 1; weekly next to free ⇒ the weekly goal, 1 of 1; free only
    /// ⇒ kind none, 0 of 0. Red if the kind came from the plan's first goal instead of the chosen list, or if free goals
    /// counted.
    /// </summary>
    [Theory]
    [InlineData(GoalCadence.Daily, GoalCadence.Weekly, "Daily", 1)]
    [InlineData(GoalCadence.Weekly, GoalCadence.None, "Weekly", 1)]
    [InlineData(GoalCadence.None, GoalCadence.None, "None", 0)]
    public async Task Auswahl_In_Einem_Plan(GoalCadence first, GoalCadence second, string cadence, int total)
    {
        var k = await KidAsync();
        var (plan, p1) = Plan(k, second);
        var p2 = AddPosition(plan, first);
        // Meet every goal that has a cadence, so "met" equals the count of reward goals.
        foreach (var (pos, c) in new[] { (p1, second), (p2, first) })
            if (c != GoalCadence.None) SeedPassedTest(plan, pos, Today);

        AssertRewardGoals(await TodayAsync(k), cadence, total, total);
    }

    /// <summary>
    /// B-612 AK 5, the remediation case the start page held (B-636): a remediation "daily, until done" is a daily goal, so
    /// next to a met weekly goal the reward goals are the open remediation, kind daily, 0 of 1. Red if the kind or the
    /// choice treated "until done" as a mere goal with a cadence (then weekly, 1 of 2).
    /// </summary>
    [Fact]
    public async Task Nacharbeit_Zaehlt_Als_Tagesziel()
    {
        var k = await KidAsync();
        var (plan, weekly) = Plan(k, GoalCadence.Weekly);
        var remediation = AddPosition(plan, GoalCadence.UntilDone);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            db.PlanPositions.Single(p => p.Id == remediation).Remediation = true;
            db.SaveChanges();
        }
        SeedPassedTest(plan, weekly, Today);

        AssertRewardGoals(await TodayAsync(k), "Daily", 0, 1);
    }

    /// <summary>
    /// B-612 AK 4, server side: the bonus training names the kind of its lock - a single plan of weekly goals says
    /// <c>Weekly</c>. Red with the field missing or taken from anything but the day's reward goals.
    /// </summary>
    [Fact]
    public async Task Bonus_Nennt_Die_Zielart_Der_Sperre()
    {
        var k = await KidAsync();
        Plan(k, GoalCadence.Weekly);

        var bonus = await BonusAsync(k);
        JsonAssert.False(bonus, "unlocked");
        Assert.Equal("Weekly", bonus.GetProperty("rewardCadence").GetString());
    }
}
