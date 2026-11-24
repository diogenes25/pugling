using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-39: the father's children over time - <c>GET supervisor/children/overview</c> sums the daily rollup per day, week
/// (Monday to Sunday) or calendar month, pages over the children and never shows a child he does not supervise.
/// </summary>
public class ChildrenOverviewTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private const string Url = "/api/v1/supervisor/children/overview";

    // A fresh supervisor with its own children, so counts are not shared with other tests of the fixture.
    private async Task<(HttpClient Father, int AdultId)> FreshAdultAsync()
    {
        var pin = "8" + Random.Shared.Next(100, 999);
        var res = await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults", new { name = TestApi.UniqueName("Zeitachse"), pin });
        res.EnsureSuccessStatusCode();
        var id = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        return (await TestApi.AdultAsync(factory, id, pin), id);
    }

    private static async Task<int> ChildAsync(HttpClient father, string name) =>
        await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children", new { name, pin = "5" + Random.Shared.Next(100, 999) }));

    // A daily vocabulary duty for the child that runs through March and April 2026 - a goal on every day of the range.
    private async Task SeedMarchAprilDutyAsync(HttpClient father, int childId)
    {
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText, childId: childId);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        await db.StudyPlans.Where(p => p.Id == planId).ExecuteUpdateAsync(u => u
            .SetProperty(p => p.StartDate, new DateOnly(2026, 3, 1)).SetProperty(p => p.EndDate, new DateOnly(2026, 4, 30)));
        await db.PlanPositions.Where(p => p.Id == positionId).ExecuteUpdateAsync(u => u
            .SetProperty(p => p.CreatedAt, new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)));
    }

    private static List<(string Start, string End, int Goals)> Periods(JsonElement overview) =>
    [
        .. overview.GetProperty("children")[0].GetProperty("periods").EnumerateArray()
            .Select(p => (p.GetProperty("start").GetString()!, p.GetProperty("end").GetString()!, p.GetProperty("goalsTotal").GetInt32())),
    ];

    /// <summary>
    /// AK 1 and 2: Sunday 29 March, Monday 30 March, Tuesday 31 March, Wednesday 1 April 2026. By day: four periods. By
    /// week: Sunday closes the week of 23 March, Monday opens the next. By month: March and April, although all four days
    /// after Sunday lie in one week. Red when Sunday falls into the following week, or when a month is grouped like a week
    /// (or as 4 × 7 days).
    /// </summary>
    [Fact]
    public async Task Gruppiert_Nach_Tag_Woche_Und_Kalendermonat()
    {
        var (father, _) = await FreshAdultAsync();
        var childId = await ChildAsync(father, "Anna");
        await SeedMarchAprilDutyAsync(father, childId);

        async Task<List<(string, string, int)>> GroupedAsync(string granularity) =>
            Periods(await father.GetFromJsonAsync<JsonElement>($"{Url}?from=2026-03-29&to=2026-04-01&granularity={granularity}"));

        Assert.Equal(4, (await GroupedAsync("Day")).Count);
        Assert.Equal([("2026-03-29", "2026-03-29", 1), ("2026-03-30", "2026-04-01", 3)], await GroupedAsync("Week"));
        Assert.Equal([("2026-03-29", "2026-03-31", 3), ("2026-04-01", "2026-04-01", 1)], await GroupedAsync("Month"));
    }

    /// <summary>
    /// AK 1: the page runs over the children (X-Total-Count = number of children), and a child of another supervisor never
    /// appears, not even when asked for by id. Red when the total counts periods, or the supervisor filter is missing.
    /// </summary>
    [Fact]
    public async Task Blaettert_Ueber_Die_Kinder_Und_Zeigt_Kein_Fremdes()
    {
        var (father, _) = await FreshAdultAsync();
        await ChildAsync(father, "Anna");
        await ChildAsync(father, "Bert");
        var (stranger, _) = await FreshAdultAsync();
        var foreignChild = await ChildAsync(stranger, "Fremd");

        var page = await father.GetAsync($"{Url}?from=2026-03-01&to=2026-03-07&take=1");
        Assert.Equal("2", page.Headers.GetValues("X-Total-Count").Single());
        var body = await page.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Anna", body.GetProperty("children")[0].GetProperty("name").GetString());
        Assert.Equal(1, body.GetProperty("children").GetArrayLength());

        var foreign = await father.GetFromJsonAsync<JsonElement>($"{Url}?from=2026-03-01&to=2026-03-07&childId={foreignChild}");
        Assert.Equal(0, foreign.GetProperty("children").GetArrayLength());

        // A child without any plan had no goal on any day: its periods are not "done". Red without the at-least-one-goal rule.
        Assert.All(body.GetProperty("children")[0].GetProperty("periods").EnumerateArray(), p => JsonAssert.False(p, "dutyDone"));
    }

    /// <summary>
    /// A range into the future ends today - a day still to come would read as a missed duty; an unknown granularity is
    /// refused. Red when the clamp or the enum check is missing.
    /// </summary>
    [Fact]
    public async Task Zukunft_Endet_Heute_Und_Raster_Wird_Geprueft()
    {
        var (father, _) = await FreshAdultAsync();
        var today = DateOnly.FromDateTime(factory.Services.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime);
        var res = await father.GetFromJsonAsync<JsonElement>($"{Url}?from={today.AddDays(-3):yyyy-MM-dd}&to={today.AddDays(10):yyyy-MM-dd}");
        Assert.Equal(today.ToString("yyyy-MM-dd"), res.GetProperty("to").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await father.GetAsync($"{Url}?granularity=7")).StatusCode);
    }

    /// <summary>AK 1 (decision 9): more than 92 days, or from after to, is refused. Red when the bounds are missing.</summary>
    [Fact]
    public async Task Zeitraum_Ist_Begrenzt()
    {
        var (father, _) = await FreshAdultAsync();
        Assert.Equal(HttpStatusCode.OK, (await father.GetAsync($"{Url}?from=2026-01-01&to=2026-04-02")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await father.GetAsync($"{Url}?from=2026-01-01&to=2026-04-03")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await father.GetAsync($"{Url}?from=2026-03-02&to=2026-03-01")).StatusCode);
    }

    /// <summary>
    /// AK 2: a period's duty is done when every day with a daily goal was met. Today, after a passed test, the day reads
    /// done; the open days of March do not. Red when <c>dutyDone</c> ignores the days, or reads an open day as done.
    /// </summary>
    [Fact]
    public async Task Pflicht_Der_Periode_Folgt_Den_Tagen()
    {
        var (father, _) = await FreshAdultAsync();
        var pin = "7" + Random.Shared.Next(100, 999);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Carla", pin }));
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText, childId: childId);
        var child = await TestApi.ChildAsync(factory, childId, pin);
        var testsUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";
        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(testsUrl, new { }), "attemptId");
        (await child.PostAsJsonAsync($"{testsUrl}/{attemptId}/submit", new
        {
            answers = new[] { new { itemIndex = 0, givenAnswer = "hello" }, new { itemIndex = 1, givenAnswer = "goodbye" } },
        })).EnsureSuccessStatusCode();

        var today = await father.GetFromJsonAsync<JsonElement>($"{Url}?granularity=Day&childId={childId}");
        var last = today.GetProperty("children")[0].GetProperty("periods").EnumerateArray().Last();
        JsonAssert.True(last, "dutyDone");
        Assert.True(last.GetProperty("coins").GetInt32() > 0);

        var (other, _) = await FreshAdultAsync();
        var openChild = await ChildAsync(other, "Dora");
        await SeedMarchAprilDutyAsync(other, openChild);
        var open = await other.GetFromJsonAsync<JsonElement>($"{Url}?from=2026-03-29&to=2026-04-01&granularity=Week");
        Assert.All(open.GetProperty("children")[0].GetProperty("periods").EnumerateArray(), p => JsonAssert.False(p, "dutyDone"));
    }

    // ---- B-558: plans switched off since count only in the child's favour ---- ----

    private DateOnly Today => DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime);

    // A fresh child with a vocabulary exercise; plans are seeded per test, two in parallel only work in the database.
    private async Task<(HttpClient Father, int ChildId, int ExerciseId)> InactivePlanSetupAsync()
    {
        var (father, _) = await FreshAdultAsync();
        var childId = await ChildAsync(father, TestApi.UniqueName("Kapitelwechsel"));
        return (father, childId, await TestApi.CreateVocabExerciseAsync(father));
    }

    // A plan with one daily goal for the child, running from the given day, active or switched off.
    private (int PlanId, int PositionId) SeedPlan(int childId, int exerciseId, DateOnly start, bool active)
    {
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText, childId: childId);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var plan = db.StudyPlans.Single(p => p.Id == planId);
        plan.StartDate = start;
        plan.Active = active;
        // The goal exists since the plan's start (B-613: a duty counts only from the day it was created).
        db.PlanPositions.Single(p => p.Id == positionId).CreatedAt = start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        db.SaveChanges();
        return (planId, positionId);
    }

    // A second daily goal in an existing plan, created on the plan's first day.
    private int SeedSecondPosition(int planId, int exerciseId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var first = db.PlanPositions.Single(p => p.StudyPlanId == planId);
        var pos = new PlanPosition
        {
            StudyPlanId = planId,
            CreatedAt = first.CreatedAt,
            ExerciseId = exerciseId,
            Order = 1,
            Stage = first.Stage,
            Cadence = GoalCadence.Daily,
            UseLeitner = true,
            PointsGoalMet = 20,
        };
        db.PlanPositions.Add(pos);
        db.SaveChanges();
        return pos.Id;
    }

    // A passed test on a past day plus its goal reward and the reward's ledger entry (the timeline's coins read the
    // ledger, B-954), written straight into the database: the API only plays today.
    private void SeedMetDay(int planId, int positionId, DateOnly day, int points = 20)
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
        db.PositionGoalRewards.Add(new PositionGoalReward
        {
            PlanPositionId = positionId,
            Cadence = GoalCadence.Daily,
            PeriodStart = day,
            Day = day,
            Points = points,
            AwardedAt = at,
        });
        var childId = db.StudyPlans.Where(p => p.Id == planId).Select(p => p.ChildId).Single();
        db.ChildPointsEntries.Add(new ChildPointsEntry { ChildId = childId, Amount = points, Kind = PointKind.Goal, Reason = "Test", CreatedAt = at });
        db.SaveChanges();
    }

    private async Task<List<JsonElement>> DaysAsync(HttpClient father, int childId, DateOnly from, DateOnly to) =>
    [
        .. (await father.GetFromJsonAsync<JsonElement>(
                $"{Url}?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&granularity=Day&childId={childId}"))
            .GetProperty("children")[0].GetProperty("periods").EnumerateArray(),
    ];

    /// <summary>
    /// B-558 AK 1: plan A was learned on day 1 and its duty met, then switched off for plan B (starting on day 2). Day 1
    /// still shows A's goal, points and the met duty. Red with the <c>p.Active</c> filter in <c>TimelineAsync</c> (day 1
    /// then has no plan: 0 goals, 0 points, duty not done).
    /// </summary>
    [Fact]
    public async Task Abgeschalteter_Plan_Bleibt_In_Der_Zeitachse()
    {
        var (father, childId, exerciseId) = await InactivePlanSetupAsync();
        var day1 = Today.AddDays(-2);
        var (planA, positionA) = SeedPlan(childId, exerciseId, day1, active: false);
        SeedPlan(childId, exerciseId, day1.AddDays(1), active: true);
        SeedMetDay(planA, positionA, day1, points: 20);

        var day = Assert.Single(await DaysAsync(father, childId, day1, day1));
        Assert.Equal(1, day.GetProperty("goalsTotal").GetInt32());
        Assert.Equal(1, day.GetProperty("goalsMet").GetInt32());
        Assert.Equal(20, day.GetProperty("coins").GetInt32());
        JsonAssert.True(day, "dutyDone");
    }

    /// <summary>
    /// B-558 AK 2: a never used, switched-off draft with a daily goal runs over the whole range next to an active plan met
    /// every day. No day turns open and the goals total stays that of the active plan alone (measured before the draft
    /// exists). Red when inactive plans count like active ones (total 2, duty open).
    /// </summary>
    [Fact]
    public async Task Nie_Benutzter_Entwurf_Erscheint_Nicht_Als_Offene_Pflicht()
    {
        var (father, childId, exerciseId) = await InactivePlanSetupAsync();
        var day1 = Today.AddDays(-2);
        var day2 = Today.AddDays(-1);
        var (planB, positionB) = SeedPlan(childId, exerciseId, day1, active: true);
        SeedMetDay(planB, positionB, day1);
        SeedMetDay(planB, positionB, day2);

        var without = await DaysAsync(father, childId, day1, day2);
        SeedPlan(childId, exerciseId, day1, active: false);
        var with = await DaysAsync(father, childId, day1, day2);

        Assert.Equal(2, with.Count);
        Assert.Equal(without.Select(d => d.GetProperty("goalsTotal").GetInt32()), with.Select(d => d.GetProperty("goalsTotal").GetInt32()));
        Assert.All(without, d => Assert.Equal(1, d.GetProperty("goalsTotal").GetInt32()));
        Assert.All(with, d => JsonAssert.True(d, "dutyDone"));
    }

    /// <summary>
    /// B-558 AK 3: on day 2 the child learned one of two goals of a switched-off plan (its duty open) while the active
    /// plan's duty is met: day 2 counts as met, and the used inactive plan brings its full goals total. Red when an open
    /// duty of an inactive plan makes the day open (the <c>p.Active</c> condition in <c>IsCompleteDay</c>).
    /// </summary>
    [Fact]
    public async Task Offene_Pflicht_Eines_Abgeschalteten_Plans_Macht_Den_Tag_Nicht_Offen()
    {
        var (father, childId, exerciseId) = await InactivePlanSetupAsync();
        var day2 = Today.AddDays(-1);
        var (planA, positionA) = SeedPlan(childId, exerciseId, day2.AddDays(-1), active: true);
        var (planC, positionC1) = SeedPlan(childId, exerciseId, day2.AddDays(-1), active: false);
        SeedSecondPosition(planC, exerciseId);
        SeedMetDay(planA, positionA, day2);
        SeedMetDay(planC, positionC1, day2);

        var day = Assert.Single(await DaysAsync(father, childId, day2, day2));
        Assert.Equal(3, day.GetProperty("goalsTotal").GetInt32());
        Assert.Equal(2, day.GetProperty("goalsMet").GetInt32());
        JsonAssert.True(day, "dutyDone");
    }

    /// <summary>
    /// B-558: with no active plan, an untouched day of a switched-off plan carries no duty (internally <c>null</c>, not
    /// <c>false</c>), so the week it shares with a met day still reads done. Red when <c>TimelineDutyDone</c> drops
    /// <c>(p.Active || p.Done)</c> and counts every plan with a duty (<c>Any(p =&gt; p.HasDuty)</c>): day 2 becomes an
    /// open duty and breaks the week.
    /// </summary>
    [Fact]
    public async Task Unberuehrter_Tag_Eines_Abgeschalteten_Plans_Ist_Keine_Pflicht()
    {
        var (father, childId, exerciseId) = await InactivePlanSetupAsync();
        // Monday and Tuesday of last week: both days lie in one Monday-to-Sunday period, whatever today is.
        var daysSinceMonday = ((int)Today.DayOfWeek + 6) % 7;
        var monday = Today.AddDays(-daysSinceMonday - 7);
        var day2 = monday.AddDays(1);
        var (planA, positionA) = SeedPlan(childId, exerciseId, monday, active: false);
        SeedMetDay(planA, positionA, monday);

        var days = await DaysAsync(father, childId, monday, day2);
        JsonAssert.True(days[0], "dutyDone");
        // The contract's dutyDone is a plain bool: a single day without duty reads false, the same as an open one. The
        // difference (no duty vs. open duty) only shows in the week below, where a duty-free day does not count.
        JsonAssert.False(days[1], "dutyDone");
        Assert.Equal(0, days[1].GetProperty("goalsTotal").GetInt32());

        var week = Assert.Single((await father.GetFromJsonAsync<JsonElement>(
                $"{Url}?from={monday:yyyy-MM-dd}&to={monday.AddDays(6):yyyy-MM-dd}&granularity=Week&childId={childId}"))
            .GetProperty("children")[0].GetProperty("periods").EnumerateArray());
        JsonAssert.True(week, "dutyDone");
    }
}
