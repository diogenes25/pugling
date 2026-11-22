using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-659: a duty the father later frees (cadence <c>None</c>) keeps the days the child met it. The history reads the old
/// cadence up to <see cref="PlanPosition.DutyUntil"/>; the penalty does not (decision 5). Every case names the change that
/// makes it red; AK 2 and AK 4 are counter-probes (decision 6) - they hold in the starting state and only fall on a wrong
/// repair. Past days are seeded in the database (the API only plays today); the switch always goes through the father's
/// API. Today is Wednesday 2026-11-18.
/// </summary>
public class PflichtWirdKuerSerieTests : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory factory;

    public PflichtWirdKuerSerieTests(PuglingWebAppFactory factory)
    {
        this.factory = factory;
        // Noon of a fixed Wednesday: the weekly cases need a known week, and no case may straddle a UTC midnight.
        factory.Clock.FreezeAt(new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero));
    }

    private DateOnly Today => DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime);

    private sealed record Setup(HttpClient Father, HttpClient Child, int ChildId, int PlanId, int PositionId);

    // A fresh child with one plan and one position of the given cadence, running since `start`, with a passed test on
    // every day in `metDays`.
    private async Task<Setup> SetupAsync(GoalCadence cadence, DateOnly start, IEnumerable<DateOnly> metDays)
    {
        var father = await TestApi.AdultAsync(factory);
        var pin = "7" + Random.Shared.Next(100, 999);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Kuer-Kind"), pin }));
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText,
            childId: childId, cadence: cadence);
        Write(db =>
        {
            db.StudyPlans.Single(p => p.Id == planId).StartDate = start;
            db.PlanPositions.Single(p => p.Id == positionId).CreatedAt = start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        });
        foreach (var day in metDays) SeedPassedTest(planId, positionId, day);
        return new Setup(father, await TestApi.ChildAsync(factory, childId, pin), childId, planId, positionId);
    }

    // The daily case of the story: one daily duty since three days ago, met on each of them (and today, unless not).
    private Task<Setup> DailySetupAsync(bool metToday = true) =>
        SetupAsync(GoalCadence.Daily, Today.AddDays(-3),
            Enumerable.Range(-3, metToday ? 4 : 3).Select(d => Today.AddDays(d)));

    private void Write(Action<PuglingDbContext> change)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        change(db);
        db.SaveChanges();
    }

    private T Read<T>(Func<PuglingDbContext, T> read)
    {
        using var scope = factory.Services.CreateScope();
        return read(scope.ServiceProvider.GetRequiredService<PuglingDbContext>());
    }

    private PlanPosition Position(int positionId) => Read(db => db.PlanPositions.AsNoTracking().Single(p => p.Id == positionId));

    private void SeedPassedTest(int planId, int positionId, DateOnly day)
    {
        var at = day.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc);
        Write(db => db.TestAttempts.Add(new TestAttempt
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
        }));
    }

    private static async Task<JsonElement> OkJsonAsync(HttpResponseMessage res)
    {
        Assert.True(res.IsSuccessStatusCode, $"{(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task SetCadenceAsync(Setup s, string cadence) =>
        await OkJsonAsync(await s.Father.PatchAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{s.PlanId}/positions/{s.PositionId}", new { cadence }));

    private static async Task<int> StreakAsync(Setup s) =>
        (await OkJsonAsync(await s.Child.GetAsync($"/api/v1/student/children/{s.ChildId}/today")))
        .GetProperty("currentStreak").GetInt32();

    private async Task<IReadOnlySet<DateOnly>> CompleteDaysAsync(int childId, DateOnly until)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<PositionProgressService>()
            .CompleteChildDaysAsync(childId, until, CancellationToken.None);
    }

    private static IEnumerable<DateOnly> Days(DateOnly from, DateOnly to)
    {
        for (var d = from; d <= to; d = d.AddDays(1)) yield return d;
    }

    /// <summary>
    /// AK 1: the only daily duty, met four days in a row, is freed today - the four days stay complete and the streak stays
    /// 4. Red before B-659 (streak 0: today's cadence <c>None</c> erased every past duty); red as well when the switch
    /// does not write <see cref="PlanPosition.DutyUntil"/> or the history does not read it.
    /// </summary>
    [Fact]
    public async Task Erfuellte_Tage_Bleiben_Vollstaendig_Wenn_Die_Pflicht_Kuer_Wird()
    {
        var s = await DailySetupAsync();
        Assert.Equal(4, await StreakAsync(s));

        await SetCadenceAsync(s, "None");

        Assert.Equal(4, await StreakAsync(s));
        var complete = await CompleteDaysAsync(s.ChildId, Today);
        Assert.All(Days(Today.AddDays(-3), Today), d => Assert.Contains(d, complete));
    }

    /// <summary>
    /// The child's day view of today follows the same history: a met daily duty freed today still stands as today's met
    /// duty, an open one freed today is gone from the day. Red when the day view keeps reading today's raw cadence (then
    /// the met duty vanishes and the child's "done" turns into "nothing to do").
    /// </summary>
    [Theory]
    [InlineData(true, 1, true)]
    [InlineData(false, 0, false)]
    public async Task Tagesansicht_Folgt_Der_Historie(bool metToday, int goalsTotal, bool dutyDone)
    {
        var s = await DailySetupAsync(metToday);

        await SetCadenceAsync(s, "None");

        var today = await OkJsonAsync(await s.Child.GetAsync($"/api/v1/student/children/{s.ChildId}/today"));
        Assert.Equal(goalsTotal, today.GetProperty("goalsTotal").GetInt32());
        Assert.Equal(dutyDone, today.GetProperty("dutyDone").GetBoolean());
    }

    /// <summary>
    /// AK 2, counter-probe: the day after the switch has no duty left and stays incomplete (B-606). Holds in the starting
    /// state; red only when AK 1 is "repaired" by counting a day without any duty as complete.
    /// </summary>
    [Fact]
    public async Task Gegenprobe_Der_Tag_Nach_Dem_Wechsel_Ohne_Pflicht_Bleibt_Unvollstaendig()
    {
        var s = await DailySetupAsync();
        await SetCadenceAsync(s, "None");

        Assert.DoesNotContain(Today.AddDays(1), await CompleteDaysAsync(s.ChildId, Today.AddDays(1)));
    }

    /// <summary>
    /// AK 3: a weekly duty met on Wednesday of this week (and in the week before), freed the same Wednesday - Thursday to
    /// Sunday of this week stay complete, and so does the week before. Red when the old cadence is not kept
    /// (<see cref="PlanPosition.CadenceUntil"/>: the span would read today's <c>None</c>, and no day counts), or when
    /// <see cref="PlanPosition.DutyUntil"/> points at today instead of the period's Sunday.
    /// </summary>
    [Fact]
    public async Task Erfuellte_Woche_Bleibt_Bis_Sonntag_Vollstaendig()
    {
        var monday = new DateOnly(2026, 11, 9);
        var s = await SetupAsync(GoalCadence.Weekly, monday, [new DateOnly(2026, 11, 11), Today]);

        await SetCadenceAsync(s, "None");

        var sunday = new DateOnly(2026, 11, 22);
        Assert.Equal(sunday, Position(s.PositionId).DutyUntil);
        Assert.Equal(GoalCadence.Weekly, Position(s.PositionId).CadenceUntil);
        var complete = await CompleteDaysAsync(s.ChildId, sunday);
        Assert.All(Days(monday, sunday), d => Assert.Contains(d, complete));
    }

    /// <summary>
    /// AK 7, the open period: today's daily goal is not met when the father frees it, so today does not count - the duty
    /// ends yesterday (the day before the period begins). Red when the switch always writes today. The met period
    /// (<see cref="Erfuellte_Tage_Bleiben_Vollstaendig_Wenn_Die_Pflicht_Kuer_Wird"/> reads <c>DutyUntil = today</c> below)
    /// is red when it always writes yesterday.
    /// </summary>
    [Fact]
    public async Task Offene_Periode_Endet_Am_Tag_Davor()
    {
        var s = await DailySetupAsync(metToday: false);

        await SetCadenceAsync(s, "None");

        Assert.Equal(Today.AddDays(-1), Position(s.PositionId).DutyUntil);
        Assert.Equal(GoalCadence.Daily, Position(s.PositionId).CadenceUntil);
        var complete = await CompleteDaysAsync(s.ChildId, Today);
        Assert.All(Days(Today.AddDays(-3), Today.AddDays(-1)), d => Assert.Contains(d, complete));
        Assert.DoesNotContain(Today, complete);
    }

    /// <summary>AK 7, the met period: <c>DutyUntil</c> is the last day of the period - for a daily goal today itself.</summary>
    [Fact]
    public async Task Erfuellte_Periode_Endet_Mit_Ihrem_Letzten_Tag()
    {
        var s = await DailySetupAsync();

        await SetCadenceAsync(s, "None");

        Assert.Equal(Today, Position(s.PositionId).DutyUntil);
    }

    /// <summary>
    /// AK 7, an open week: the weekly goal of this week is not met (only last week's was), so the duty ends on the Sunday
    /// before this week. Red when the switch writes today or yesterday instead of the day before the period begins.
    /// </summary>
    [Fact]
    public async Task Offene_Woche_Endet_Am_Sonntag_Davor()
    {
        var s = await SetupAsync(GoalCadence.Weekly, new DateOnly(2026, 11, 9), [new DateOnly(2026, 11, 11)]);

        await SetCadenceAsync(s, "None");

        Assert.Equal(new DateOnly(2026, 11, 15), Position(s.PositionId).DutyUntil);
    }

    /// <summary>
    /// AK 8: the position form sends <c>cadence</c> with every save. Saving <c>None</c> on an already free position, or
    /// the unchanged cadence on a duty, must not move the history. Red when every PATCH carrying <c>cadence</c> rewrites
    /// it (the second save of <c>None</c> a day later would then push <c>DutyUntil</c> to a day the child never met).
    /// </summary>
    [Fact]
    public async Task Speichern_Ohne_Wechsel_Verschiebt_Die_Historie_Nicht()
    {
        var s = await DailySetupAsync();
        await SetCadenceAsync(s, "Daily");
        Assert.Null(Position(s.PositionId).DutyUntil);

        await SetCadenceAsync(s, "None");
        factory.Clock.Advance(TimeSpan.FromDays(2));
        await SetCadenceAsync(s, "None");

        Assert.Equal(new DateOnly(2026, 11, 18), Position(s.PositionId).DutyUntil);
        Assert.Equal(GoalCadence.Daily, Position(s.PositionId).CadenceUntil);
    }

    /// <summary>
    /// AK 9: free, then a duty again - the position owes from today on, and the earlier span is gone (decision 3). Red when
    /// the switch back keeps <c>DutyUntil</c>: the history would read "after the span, no duty" and the new duty would
    /// never count.
    /// </summary>
    [Fact]
    public async Task Zurueck_Zur_Pflicht_Loescht_Die_Spanne()
    {
        var s = await DailySetupAsync();
        await SetCadenceAsync(s, "None");
        factory.Clock.Advance(TimeSpan.FromDays(2));

        await SetCadenceAsync(s, "Daily");

        var pos = Position(s.PositionId);
        Assert.Null(pos.DutyUntil);
        Assert.Null(pos.CadenceUntil);
        Assert.True(PositionProgressService.IsObligationOn(pos, Today));
    }

    /// <summary>
    /// Decision 2, the helper alone: after <see cref="PlanPosition.DutyUntil"/> there is no duty, whatever the position
    /// carries today. Through the father's API a span only exists while the cadence is <c>None</c>, so no case above can
    /// tell "none" from "the current cadence" - B-248 will end a duty while its cadence stays, and then it matters. Red
    /// when the helper falls back to the current cadence after the span.
    /// </summary>
    [Fact]
    public void Nach_Der_Spanne_Ist_Keine_Pflicht()
    {
        var until = new DateOnly(2026, 11, 18);
        var pos = new PlanPosition { Cadence = GoalCadence.Daily, DutyUntil = until, CadenceUntil = GoalCadence.Weekly };

        Assert.Equal(GoalCadence.Weekly, PositionProgressService.DutyCadenceOn(pos, until));
        Assert.Equal(GoalCadence.None, PositionProgressService.DutyCadenceOn(pos, until.AddDays(1)));
    }

    /// <summary>
    /// Risk from the estimate: a weekly goal met and rewarded on Wednesday, freed afterwards, must not pay a second time on
    /// Thursday. Red when the reward booking keeps reading the raw cadence: the period would become "Thursday, None",
    /// the same-period check would miss the Wednesday row, and the goal would pay twice.
    /// </summary>
    [Fact]
    public async Task Woechentliches_Ziel_Zahlt_Nach_Dem_Wechsel_Nicht_Doppelt()
    {
        var s = await SetupAsync(GoalCadence.Weekly, new DateOnly(2026, 11, 16), [Today]);
        using (var scope = factory.Services.CreateScope())
        {
            var plan = scope.ServiceProvider.GetRequiredService<PuglingDbContext>().StudyPlans.Single(p => p.Id == s.PlanId);
            await scope.ServiceProvider.GetRequiredService<PositionProgressService>().EvaluateAndAwardAsync(plan, Today);
        }
        Assert.Equal(1, Read(db => db.PositionGoalRewards.Count(r => r.PlanPositionId == s.PositionId)));

        await SetCadenceAsync(s, "None");
        using (var scope = factory.Services.CreateScope())
        {
            var plan = scope.ServiceProvider.GetRequiredService<PuglingDbContext>().StudyPlans.Single(p => p.Id == s.PlanId);
            await scope.ServiceProvider.GetRequiredService<PositionProgressService>().EvaluateAndAwardAsync(plan, Today.AddDays(1));
        }

        Assert.Equal(1, Read(db => db.PositionGoalRewards.Count(r => r.PlanPositionId == s.PositionId)));
    }

    // A daily duty with a penalty, met on the first two days, missed yesterday, met today - one unsettled missed day.
    private async Task<Setup> MissedYesterdaySetupAsync()
    {
        var s = await SetupAsync(GoalCadence.Daily, Today.AddDays(-3), [Today.AddDays(-3), Today.AddDays(-2), Today]);
        Write(db => db.PlanPositions.Single(p => p.Id == s.PositionId).PenaltyCoins = 5);
        return s;
    }

    private async Task<List<DateOnly>> SettleAndReadPenaltiesAsync(Setup s)
    {
        using (var scope = factory.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<PositionProgressService>()
                .SettleClosedPeriodsAsync(s.ChildId, Today, CancellationToken.None);
        return Read(db => db.PositionGoalPenalties.Where(p => p.PlanPositionId == s.PositionId).Select(p => p.PeriodStart).ToList());
    }

    /// <summary>
    /// AK 4, the yardstick for the counter-probe below: without the switch the missed day is charged. Red when the setup
    /// stops producing a penalty - then the counter-probe would hold for the wrong reason.
    /// </summary>
    [Fact]
    public async Task Ohne_Wechsel_Kostet_Der_Verpasste_Tag()
    {
        var s = await MissedYesterdaySetupAsync();

        Assert.Equal([Today.AddDays(-1)], await SettleAndReadPenaltiesAsync(s));
    }

    /// <summary>
    /// AK 4, counter-probe: freed before the settlement, the missed day stays uncharged - the history does not reach the
    /// penalty (decision 5). Holds in the starting state; red only when the history leaks into the settlement filter or
    /// someone settles before the switch.
    /// </summary>
    [Fact]
    public async Task Gegenprobe_Kuer_Vor_Der_Abrechnung_Kostet_Nichts()
    {
        var s = await MissedYesterdaySetupAsync();

        await SetCadenceAsync(s, "None");

        Assert.Empty(await SettleAndReadPenaltiesAsync(s));
    }

    /// <summary>
    /// AK 5: a plan the child has played cannot be deleted (409 <c>plan_has_data</c>), and the refused delete leaves the
    /// streak alone. Red while the plan delete is unlocked: the cascade takes the plan and its met days with it. The way
    /// out the message names - switching the plan off - is taken here, and the met days survive it as well.
    /// </summary>
    [Fact]
    public async Task Gespielter_Plan_Laesst_Sich_Nicht_Loeschen()
    {
        var s = await DailySetupAsync();

        var res = await s.Father.DeleteAsync($"/api/v1/supervisor/study-plans/{s.PlanId}");

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("plan_has_data", (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.OK, (await s.Father.GetAsync($"/api/v1/supervisor/study-plans/{s.PlanId}")).StatusCode);
        Assert.Equal(4, await StreakAsync(s));

        await OkJsonAsync(await s.Father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{s.PlanId}", new { active = false }));
        var complete = await CompleteDaysAsync(s.ChildId, Today);
        Assert.All(Days(Today.AddDays(-3), Today), d => Assert.Contains(d, complete));
    }
}
