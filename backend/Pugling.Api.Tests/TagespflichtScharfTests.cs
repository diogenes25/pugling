using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-1036: on a day judged by the day planner's enforced tasks, the tasks are the child's duty - day view, streak, chest, goal
/// points and the old penalty read them instead of the positions' cadence. Enforced plans are seeded in the database (the API
/// cannot switch the mode before B-737). Today is Monday 2026-11-30, noon; Sunday the 29th is free. The child has one plan with
/// four positions: D daily, W weekly, F free, E daily whose duty ended on Sunday. Each case names the change that turns it red.
/// </summary>
public class TagespflichtScharfTests : IClassFixture<LerntageWebAppFactory>
{
    private static readonly DateOnly Today = new(2026, 11, 30);
    private static readonly DateOnly Sunday = Today.AddDays(-1);
    private static readonly DateOnly Saturday = Today.AddDays(-2);
    private static readonly DateOnly Friday = Today.AddDays(-3);
    private static readonly DateOnly Thursday = Today.AddDays(-4);

    private readonly LerntageWebAppFactory factory;

    public TagespflichtScharfTests(LerntageWebAppFactory factory)
    {
        this.factory = factory;
        factory.Clock.FreezeAt(new DateTimeOffset(2026, 11, 30, 12, 0, 0, TimeSpan.Zero));
    }

    private sealed record Setup(HttpClient Father, int ChildId, int PlanId, int D, int W, int F, int E);

    private async Task<Setup> SetupAsync()
    {
        var father = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Scharf-Kind"), pin = "7" + Random.Shared.Next(100, 999) }));
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, d) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText, childId: childId,
            cadence: GoalCadence.Daily);
        int w = 0, f = 0, e = 0;
        Write(db =>
        {
            var plan = db.StudyPlans.Single(p => p.Id == planId);
            plan.StartDate = Thursday.AddDays(-7);
            plan.EndDate = Today.AddDays(30);
            var daily = db.PlanPositions.Single(p => p.Id == d);
            daily.CreatedAt = plan.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            daily.PenaltyCoins = 5;
            daily.PointsGoalMet = 10;
            PlanPosition Copy(GoalCadence cadence, int order) => new()
            {
                StudyPlanId = planId,
                ExerciseId = exerciseId,
                Order = order,
                Stage = daily.Stage,
                Cadence = cadence,
                UseLeitner = true,
                PointsGoalMet = 10,
                PenaltyCoins = 5,
                CreatedAt = daily.CreatedAt,
            };
            var weekly = Copy(GoalCadence.Weekly, 1);
            var free = Copy(GoalCadence.None, 2);
            var ended = Copy(GoalCadence.Daily, 3);
            ended.DutyUntil = Sunday;
            ended.CadenceUntil = GoalCadence.Daily;
            db.PlanPositions.AddRange(weekly, free, ended);
            db.SaveChanges();
            (w, f, e) = (weekly.Id, free.Id, ended.Id);
        });
        return new Setup(father, childId, planId, d, w, f, e);
    }

    private void Enforce(Setup s, DateOnly since, int penalty = 0) => Write(db =>
    {
        var child = db.Children.Single(c => c.Id == s.ChildId);
        child.PlannerMode = PlannerMode.Enforced;
        child.PlannerSince = since;
        child.PenaltyCoinsPerMissedTask = penalty;
    });

    private void DayPlan(Setup s, DateOnly day, PlannerMode mode, params int[] positions) => Write(db =>
        db.DailyPlans.Add(new DailyPlan
        {
            ChildId = s.ChildId,
            Day = day,
            Mode = mode,
            NewWordsBudget = 2,
            CreatedAt = day.ToDateTime(new TimeOnly(6, 0), DateTimeKind.Utc),
            Tasks = [.. positions.Select((p, i) => new DailyTask
            {
                PlanPositionId = p, ExerciseId = TestApi.ExerciseOf(db, p), ExerciseTitle = "Aufgabe", Order = i + 1, Reason = "Test",
            })],
        }));

    // A graded test of the position on `day`: passed meets its goal, failed starts the old penalty clock (B-535).
    private void Test(Setup s, int positionId, DateOnly day, bool passed = true)
    {
        var at = day.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc);
        Write(db => db.TestAttempts.Add(new TestAttempt
        {
            StudyPlanId = s.PlanId,
            PlanPositionId = positionId,
            Day = day,
            StageValue = (int)TestStage.FreeText,
            Graded = true,
            StartedAt = at,
            CompletedAt = at,
            TotalItems = 2,
            CorrectItems = passed ? 2 : 0,
            ScorePercent = passed ? 100 : 0,
            Passed = passed,
        }));
    }

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

    private async Task<T> ServiceAsync<TService, T>(Func<TService, Task<T>> call) where TService : notnull
    {
        using var scope = factory.Services.CreateScope();
        return await call(scope.ServiceProvider.GetRequiredService<TService>());
    }

    private static async Task<JsonElement> TodayAsync(Setup s)
    {
        var res = await s.Father.GetAsync($"/api/v1/student/children/{s.ChildId}/today");
        Assert.True(res.IsSuccessStatusCode, $"{(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static JsonElement Position(JsonElement today, int positionId) =>
        today.GetProperty("positions").EnumerateArray().Single(p => p.GetProperty("positionId").GetInt32() == positionId);

    private Task<int> StreakAsync(Setup s) =>
        ServiceAsync<PositionProgressService, int>(p => p.ChildStreakAsync(s.ChildId, Today, int.MaxValue, CancellationToken.None));

    // ---- AK 1: only a task is the day's duty ----

    /// <summary>
    /// AK 1: today's enforced plan holds W (weekly) and F (free). The day has two goals - W and F as daily duties - and D, a daily
    /// position without a task, is no duty. Neither shows the position's penalty (stand-in 5: no price before B-1037). Red when
    /// the day view reads the positions' cadence (D a duty, F free), or a task advertises the position's 5 coins.
    /// </summary>
    [Fact]
    public async Task Nur_Die_Aufgaben_Sind_Die_Pflicht_Des_Tages()
    {
        var s = await SetupAsync();
        Enforce(s, Thursday);
        DayPlan(s, Today, PlannerMode.Enforced, s.W, s.F);

        var today = await TodayAsync(s);

        Assert.Equal(2, today.GetProperty("goalsTotal").GetInt32());
        Assert.Equal("None", Position(today, s.D).GetProperty("cadence").GetString());
        Assert.Equal("Daily", Position(today, s.W).GetProperty("cadence").GetString());
        Assert.Equal("Daily", Position(today, s.F).GetProperty("cadence").GetString());
        Assert.Equal(0, Position(today, s.W).GetProperty("penaltyCoins").GetInt32());
        Assert.True(Position(today, s.W).GetProperty("owesDuty").GetBoolean());
        Assert.False(Position(today, s.D).GetProperty("owesDuty").GetBoolean());
    }

    // ---- AK 2: shadow mode changes nothing ----

    /// <summary>
    /// AK 2: the same plan in shadow mode (switched on since Thursday) leaves the day to the positions: D and W are duties
    /// (2 goals), F is free, W shows its penalty. Red when the calendar reads a shadow plan as enforced, or a shadow child's
    /// days as judged by tasks.
    /// </summary>
    [Fact]
    public async Task Schattenplan_Aendert_Die_Pflicht_Nicht()
    {
        var s = await SetupAsync();
        Write(db =>
        {
            var child = db.Children.Single(c => c.Id == s.ChildId);
            child.PlannerMode = PlannerMode.Shadow;
            child.PlannerSince = Thursday;
        });
        DayPlan(s, Today, PlannerMode.Shadow, s.W, s.F);

        var today = await TodayAsync(s);

        Assert.Equal(2, today.GetProperty("goalsTotal").GetInt32());
        Assert.Equal("Daily", Position(today, s.D).GetProperty("cadence").GetString());
        Assert.Equal("None", Position(today, s.F).GetProperty("cadence").GetString());
        Assert.Equal(5, Position(today, s.W).GetProperty("penaltyCoins").GetInt32());
    }

    // ---- AK 3 and 4: the streak ----

    /// <summary>
    /// AK 3: Thursday's task met, Friday enforced without a plan (the server never ran), Saturday a plan without tasks, Sunday
    /// free, today's task met - the streak is 2: the empty days neither count nor break, like a pause. Red when a day judged by
    /// tasks without any owes a duty (streak 1).
    /// </summary>
    [Fact]
    public async Task Tag_Ohne_Plan_Und_Plan_Ohne_Aufgaben_Halten_Die_Serie()
    {
        var s = await SetupAsync();
        Enforce(s, Thursday);
        DayPlan(s, Thursday, PlannerMode.Enforced, s.D);
        DayPlan(s, Saturday, PlannerMode.Enforced);
        DayPlan(s, Today, PlannerMode.Enforced, s.D);
        Test(s, s.D, Thursday);
        Test(s, s.D, Today);

        Assert.Equal(2, await StreakAsync(s));
        // The missions' streak reads the same rule (MetricsService).
        Assert.Equal(2, await ServiceAsync<MetricsService, int>(m =>
            m.ValueAsync(s.ChildId, ProgressMetric.StreakDays, null, null, Today, CancellationToken.None)));
    }

    /// <summary>
    /// AK 3, timeline: enforced since Monday 23 November, Monday to Thursday each with task D met, Friday without a plan, Saturday a
    /// plan without tasks - the supervisor's week reads as done: the empty days show no duty. Red when such a day counts as an
    /// open duty (the week reads not done).
    /// </summary>
    [Fact]
    public async Task Zeitachse_Zeigt_Tage_Ohne_Aufgabe_Ohne_Pflicht()
    {
        var s = await SetupAsync();
        var monday = Today.AddDays(-7);
        Enforce(s, monday);
        for (var d = monday; d <= Thursday; d = d.AddDays(1))
        {
            DayPlan(s, d, PlannerMode.Enforced, s.D);
            Test(s, s.D, d);
        }
        DayPlan(s, Saturday, PlannerMode.Enforced);

        var overview = await s.Father.GetFromJsonAsync<JsonElement>(
            $"/api/v1/supervisor/children/overview?from={monday:yyyy-MM-dd}&to={Sunday:yyyy-MM-dd}&granularity=Week&childId={s.ChildId}");
        var week = overview.GetProperty("children").EnumerateArray().Single().GetProperty("periods").EnumerateArray().Single();

        Assert.True(week.GetProperty("dutyDone").GetBoolean());
    }

    /// <summary>
    /// Stand-in assumption 1, the snapshot: the planner was enforced from Thursday to Saturday and is back in shadow mode since
    /// today. Those days stay judged by their tasks, so D's open daily duty costs no old penalty there. Red when the enforced
    /// plans count only while the child is still enforced (Thursday to Saturday charged).
    /// </summary>
    [Fact]
    public async Task Scharfe_Tage_Bleiben_Scharf_Nach_Dem_Zurueckschalten()
    {
        var s = await SetupAsync();
        foreach (var d in new[] { Thursday, Friday, Saturday }) DayPlan(s, d, PlannerMode.Enforced, s.D);
        Write(db =>
        {
            var child = db.Children.Single(c => c.Id == s.ChildId);
            child.PlannerMode = PlannerMode.Shadow;
            child.PlannerSince = Today;
        });
        Test(s, s.D, Thursday, passed: false);

        await ServiceAsync<PositionProgressService, int>(p => p.SettleClosedPeriodsAsync(s.ChildId, Today, CancellationToken.None));

        Assert.Empty(Read(db => db.PositionGoalPenalties.Where(p => p.PlanPositionId == s.D).ToList()));
    }

    /// <summary>
    /// AK 4: Thursday's task D met, Friday's task D open, Saturday's task W met while D is open, today's task D met - the streak
    /// is 2 (today, Saturday) and breaks at Friday. Red when the streak reads the positions (Saturday breaks on D: 1) or ignores
    /// the open task (Friday counts: 4).
    /// </summary>
    [Fact]
    public async Task Serie_Zaehlt_Die_Aufgaben_Und_Bricht_An_Der_Offenen()
    {
        var s = await SetupAsync();
        Enforce(s, Thursday);
        DayPlan(s, Thursday, PlannerMode.Enforced, s.D);
        DayPlan(s, Friday, PlannerMode.Enforced, s.D);
        DayPlan(s, Saturday, PlannerMode.Enforced, s.W);
        DayPlan(s, Today, PlannerMode.Enforced, s.D);
        Test(s, s.D, Thursday);
        Test(s, s.W, Saturday);
        Test(s, s.D, Today);

        Assert.Equal(2, await StreakAsync(s));
    }

    /// <summary>
    /// AK 4 with AK 6: Wednesday 2 December. Monday's task W met on Monday, Tuesday's task W open, Wednesday's task D met - the
    /// streak is 1: Tuesday's task is measured on Tuesday. Red when the streak measures W by its week (Monday's test meets
    /// Tuesday: 3).
    /// </summary>
    [Fact]
    public async Task Serie_Misst_Die_Aufgabe_Am_Tag()
    {
        var s = await SetupAsync();
        var wednesday = Today.AddDays(2);
        factory.Clock.FreezeAt(new DateTimeOffset(2026, 12, 2, 12, 0, 0, TimeSpan.Zero));
        Enforce(s, Thursday);
        DayPlan(s, Today, PlannerMode.Enforced, s.W);
        DayPlan(s, Today.AddDays(1), PlannerMode.Enforced, s.W);
        DayPlan(s, wednesday, PlannerMode.Enforced, s.D);
        Test(s, s.W, Today);
        Test(s, s.D, wednesday);

        Assert.Equal(1, await ServiceAsync<PositionProgressService, int>(p =>
            p.ChildStreakAsync(s.ChildId, wednesday, int.MaxValue, CancellationToken.None)));
    }

    /// <summary>
    /// AK 1, remediation: a position "daily, until done" as today's task keeps its own day rule - one round through its open
    /// tasks meets it, no test needed. Red when a task always takes the daily goal rule (the round is no passed test: open).
    /// </summary>
    [Fact]
    public async Task Nacharbeit_Als_Aufgabe_Behaelt_Ihre_Tagesregel()
    {
        var s = await SetupAsync();
        var r = 0;
        Write(db =>
        {
            var d = db.PlanPositions.AsNoTracking().Single(p => p.Id == s.D);
            var remediation = new PlanPosition
            {
                StudyPlanId = s.PlanId,
                ExerciseId = d.ExerciseId,
                Order = 4,
                Stage = d.Stage,
                Cadence = GoalCadence.UntilDone,
                UseLeitner = true,
                CreatedAt = d.CreatedAt,
            };
            db.PlanPositions.Add(remediation);
            db.SaveChanges();
            r = remediation.Id;
            db.PracticeSessions.Add(new PracticeSession
            {
                StudyPlanId = s.PlanId,
                PlanPositionId = r,
                Day = Today,
                Mode = PlayMode.Lern,
                Order = [0, 1],
                Cursor = 2,
                StartedAt = Today.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc),
            });
        });
        Enforce(s, Thursday);
        DayPlan(s, Today, PlannerMode.Enforced, r);

        var task = Position(await TodayAsync(s), r);

        Assert.Equal("UntilDone", task.GetProperty("cadence").GetString());
        Assert.True(task.GetProperty("goalMet").GetBoolean());
    }

    // ---- AK 5: chest and bonus ----

    /// <summary>
    /// AK 5: today's only task is W. With W met and D (daily, no task) open the rewards open; with W open and D met they stay
    /// shut. Red when the reward lock reads the positions' daily goals (the first locked, the second open).
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Truhe_Oeffnet_Nach_Den_Aufgaben(bool taskMet)
    {
        var s = await SetupAsync();
        Enforce(s, Thursday);
        DayPlan(s, Today, PlannerMode.Enforced, s.W);
        Test(s, taskMet ? s.W : s.D, Today);

        Assert.Equal(taskMet, (await TodayAsync(s)).GetProperty("rewardsUnlocked").GetBoolean());
    }

    // ---- AK 6: a weekly position's task is met on its day ----

    /// <summary>
    /// AK 6: Wednesday 2 December, W passed on Tuesday of the same week, today's task is W - it is open. Red when the task is
    /// measured by the week (met).
    /// </summary>
    [Fact]
    public async Task Aufgabe_Einer_Wochenposition_Gilt_Am_Tag()
    {
        var s = await SetupAsync();
        var wednesday = Today.AddDays(2);
        factory.Clock.FreezeAt(new DateTimeOffset(2026, 12, 2, 12, 0, 0, TimeSpan.Zero));
        Enforce(s, Thursday);
        DayPlan(s, wednesday, PlannerMode.Enforced, s.W);
        Test(s, s.W, wednesday.AddDays(-1));

        Assert.False(Position(await TodayAsync(s), s.W).GetProperty("goalMet").GetBoolean());
    }

    // ---- AK 7: goal points ----

    /// <summary>
    /// AK 7: today's task is W; D and W are both met. The award pays W once as a daily goal and D nothing, also when it runs
    /// twice. Red when D (no task) is paid, W is paid as a weekly goal or twice.
    /// </summary>
    [Fact]
    public async Task Zielpunkte_Nur_Fuer_Die_Aufgabe_Einmal_Je_Tag()
    {
        var s = await SetupAsync();
        Enforce(s, Thursday);
        DayPlan(s, Today, PlannerMode.Enforced, s.W);
        Test(s, s.D, Today);
        Test(s, s.W, Today);

        for (var i = 0; i < 2; i++)
            await ServiceAsync<PositionProgressService, bool>(async p =>
            {
                var plan = Read(db => db.StudyPlans.AsNoTracking().Single(x => x.Id == s.PlanId));
                await p.EvaluateAndAwardAsync(plan, Today, CancellationToken.None);
                return true;
            });

        var rewards = Read(db => db.PositionGoalRewards.Where(r => r.PlanPosition!.StudyPlanId == s.PlanId)
            .Select(r => new { r.PlanPositionId, r.Cadence, r.PeriodStart }).ToList());
        var only = Assert.Single(rewards);
        Assert.Equal((s.W, GoalCadence.Daily, Today), (only.PlanPositionId, only.Cadence, only.PeriodStart));
    }

    // ---- AK 8: the old penalty stays off enforced days ----

    /// <summary>
    /// AK 8: enforced since Friday, D's daily duty open from Thursday on, Friday and Saturday planned with task D - only Thursday
    /// (judged by the position) costs the old penalty. Red when the settlement books an enforced day (Friday, Saturday).
    /// </summary>
    [Fact]
    public async Task Alter_Malus_Bucht_Keinen_Scharfen_Tag()
    {
        var s = await SetupAsync();
        Enforce(s, Friday);
        DayPlan(s, Friday, PlannerMode.Enforced, s.D);
        DayPlan(s, Saturday, PlannerMode.Enforced, s.D);
        Test(s, s.D, Thursday, passed: false);

        await ServiceAsync<PositionProgressService, int>(p => p.SettleClosedPeriodsAsync(s.ChildId, Today, CancellationToken.None));

        Assert.Equal([Thursday], Read(db => db.PositionGoalPenalties.Where(p => p.PlanPositionId == s.D)
            .Select(p => p.PeriodStart).OrderBy(d => d).ToList()));
    }

    // ---- AK 9: free practice never becomes a task ----

    /// <summary>
    /// AK 9: the planner in shadow mode, with a floor of four tasks, plans today from D, W, F and E: D and W become tasks, the free
    /// F and E (its duty ended on Sunday) do not, though the floor would take them. Red when the planner takes every position.
    /// </summary>
    [Fact]
    public async Task Kuer_Wird_Nie_Tagesaufgabe()
    {
        var s = await SetupAsync();
        var res = await s.Father.PatchAsJsonAsync($"/api/v1/supervisor/children/{s.ChildId}", new
        {
            dailyTasksMin = 4,
            dailyTasksMax = 5,
            dailyNewWordsMin = 1,
            dailyNewWordsMax = 4,
            plannerMode = "Shadow",
        });
        Assert.True(res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());

        var plans = await s.Father.GetFromJsonAsync<JsonElement>($"/api/v1/supervisor/children/{s.ChildId}/daily-plans");
        var tasks = plans.EnumerateArray().Single(p => p.GetProperty("day").GetString() == "2026-11-30")
            .GetProperty("tasks").EnumerateArray().Select(t => t.GetProperty("planPositionId").GetInt32()).Order().ToList();

        Assert.Equal(new[] { s.D, s.W }.Order(), tasks);
    }

    // ---- AK 10: the API cannot switch it on yet ----

    /// <summary>
    /// AK 10: the enforced mode is refused with <c>validation_error</c> before the cutoff-day reset (B-737), even with all limits
    /// set; the child stays off. Red when the mode is settable.
    /// </summary>
    [Fact]
    public async Task Scharfer_Modus_Ist_Ueber_Die_Api_Nicht_Setzbar()
    {
        var s = await SetupAsync();
        var res = await s.Father.PatchAsJsonAsync($"/api/v1/supervisor/children/{s.ChildId}", new
        {
            dailyTasksMin = 1,
            dailyTasksMax = 5,
            dailyNewWordsMin = 1,
            dailyNewWordsMax = 4,
            plannerMode = "Enforced",
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("validation_error", (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        Assert.Equal(PlannerMode.Off, Read(db => db.Children.Single(c => c.Id == s.ChildId).PlannerMode));
    }

    // ---- B-1037: the penalty per missed task ----

    private Task SettleAsync(Setup s) =>
        ServiceAsync<PositionProgressService, int>(p => p.SettleClosedPeriodsAsync(s.ChildId, Today, CancellationToken.None));

    private List<DateOnly> TaskPenaltyDays(Setup s) => Read(db => db.DailyTaskPenalties
        .Where(p => p.DailyTask!.DailyPlan!.ChildId == s.ChildId).Select(p => p.DailyTask!.DailyPlan!.Day).OrderBy(d => d).ToList());

    private List<(int Amount, string Reason)> PenaltyLedger(Setup s) => Read(db => db.ChildPointsEntries
        .Where(e => e.ChildId == s.ChildId && e.Kind == PointKind.GoalPenalty).OrderBy(e => e.Id)
        .Select(e => new { e.Amount, e.Reason }).ToList().Select(e => (e.Amount, e.Reason)).ToList());

    /// <summary>
    /// B-736 AK 3: Friday's tasks are D (open) and W (met). Settled twice, the child pays the amount 3 once, for D's task, with the
    /// day in the ledger text; the met task costs nothing, and the old position penalty stays off. Red when a met task is charged,
    /// a second run charges again, or the amount is not the child's.
    /// </summary>
    [Fact]
    public async Task Offene_Aufgabe_Kostet_Einmal_Den_Betrag_Des_Kindes()
    {
        var s = await SetupAsync();
        Enforce(s, Thursday, penalty: 3);
        DayPlan(s, Friday, PlannerMode.Enforced, s.D, s.W);
        Test(s, s.W, Friday);

        await SettleAsync(s);
        await SettleAsync(s);

        Assert.Equal([Friday], TaskPenaltyDays(s));
        Assert.Equal(3, Read(db => db.DailyTaskPenalties.Single(p => p.DailyTask!.DailyPlan!.ChildId == s.ChildId).Points));
        var entry = Assert.Single(PenaltyLedger(s));
        Assert.Equal(-3, entry.Amount);
        Assert.Equal("[Tagesplan · Aufgabe] Tagesziel vom Fr 27.11. verpasst", entry.Reason);
    }

    /// <summary>
    /// B-736 AK 3: a task stamped done costs nothing, even without a met goal in the trail - the stamp is read first. Red when the
    /// settlement ignores <c>DoneAt</c>.
    /// </summary>
    [Fact]
    public async Task Erledigt_Gestempelte_Aufgabe_Kostet_Nichts()
    {
        var s = await SetupAsync();
        Enforce(s, Thursday, penalty: 3);
        DayPlan(s, Friday, PlannerMode.Enforced, s.D);
        Write(db =>
        {
            foreach (var t in db.DailyTasks.Where(t => t.DailyPlan!.ChildId == s.ChildId)) t.DoneAt = new DateTime(2026, 11, 27, 15, 0, 0, DateTimeKind.Utc);
        });

        await SettleAsync(s);

        Assert.Empty(TaskPenaltyDays(s));
    }

    /// <summary>
    /// B-1037: the path that books the goal stamps the task - today's D met, W open: after the evaluation D carries <c>DoneAt</c>,
    /// W does not. Red when the stamp is missing or stamps an open task.
    /// </summary>
    [Fact]
    public async Task Zielpfad_Stempelt_Die_Erledigte_Aufgabe()
    {
        var s = await SetupAsync();
        Enforce(s, Thursday, penalty: 3);
        DayPlan(s, Today, PlannerMode.Enforced, s.D, s.W);
        Test(s, s.D, Today);

        await ServiceAsync<PositionProgressService, bool>(async p =>
        {
            await p.EvaluateAndAwardAsync(Read(db => db.StudyPlans.AsNoTracking().Single(x => x.Id == s.PlanId)), Today,
                CancellationToken.None);
            return true;
        });

        var done = Read(db => db.DailyTasks.Where(t => t.DailyPlan!.ChildId == s.ChildId)
            .Select(t => new { t.PlanPositionId, t.DoneAt }).ToList());
        Assert.NotNull(done.Single(t => t.PlanPositionId == s.D).DoneAt);
        Assert.Null(done.Single(t => t.PlanPositionId == s.W).DoneAt);
    }

    /// <summary>
    /// B-736 AK 4: a booked task penalty bumps the child's concurrency stamp, the wallet's serialization point. Red when the bump
    /// is missing on this path.
    /// </summary>
    [Fact]
    public async Task Aufgaben_Malus_Bumpt_Den_Stamp()
    {
        var s = await SetupAsync();
        Enforce(s, Thursday, penalty: 3);
        DayPlan(s, Friday, PlannerMode.Enforced, s.D);
        var before = Read(db => db.Children.Single(c => c.Id == s.ChildId).ConcurrencyStamp);

        await SettleAsync(s);

        Assert.NotEqual(before, Read(db => db.Children.Single(c => c.Id == s.ChildId).ConcurrencyStamp));
        Assert.Equal([Friday], TaskPenaltyDays(s));
    }

    /// <summary>
    /// B-736 AK 5: Thursday's task open costs; Friday enforced without a plan, Saturday's task open but paused afterwards, and today
    /// (not closed yet) cost nothing. Red when a day without a plan, a paused day or today is charged.
    /// </summary>
    [Fact]
    public async Task Tag_Ohne_Plan_Pause_Und_Heute_Kosten_Nichts()
    {
        var s = await SetupAsync();
        Enforce(s, Thursday, penalty: 3);
        DayPlan(s, Thursday, PlannerMode.Enforced, s.D);
        DayPlan(s, Saturday, PlannerMode.Enforced, s.D);
        DayPlan(s, Today, PlannerMode.Enforced, s.D);
        var pause = await s.Father.PostAsJsonAsync($"/api/v1/supervisor/children/{s.ChildId}/pauses",
            new { from = Saturday, until = Saturday, note = "krank" });
        Assert.True(pause.IsSuccessStatusCode, await pause.Content.ReadAsStringAsync());

        await SettleAsync(s);

        Assert.Equal([Thursday], TaskPenaltyDays(s));
    }

    /// <summary>
    /// B-1037: a task the child could not play costs nothing - its plan switched off, or its position deleted. Red when the
    /// settlement charges either.
    /// </summary>
    [Fact]
    public async Task Abgeschalteter_Plan_Und_Geloeschte_Position_Kosten_Nichts()
    {
        var s = await SetupAsync();
        Enforce(s, Thursday, penalty: 3);
        DayPlan(s, Thursday, PlannerMode.Enforced, s.D);
        DayPlan(s, Friday, PlannerMode.Enforced, s.F);
        Write(db => db.StudyPlans.Single(p => p.Id == s.PlanId).Active = false);
        Write(db => db.PlanPositions.Remove(db.PlanPositions.Single(p => p.Id == s.F)));

        await SettleAsync(s);

        Assert.Empty(TaskPenaltyDays(s));
    }

    /// <summary>
    /// B-1037: the day view shows a task's price as the child's amount per missed task - what the settlement books. Red when the
    /// task shows 0 or the position's own penalty.
    /// </summary>
    [Fact]
    public async Task Aufgabe_Zeigt_Den_Betrag_Des_Kindes()
    {
        var s = await SetupAsync();
        Enforce(s, Thursday, penalty: 3);
        DayPlan(s, Today, PlannerMode.Enforced, s.W);

        Assert.Equal(3, Position(await TodayAsync(s), s.W).GetProperty("penaltyCoins").GetInt32());
    }

    /// <summary>
    /// B-1037: the timer settles an enforced child's missed tasks from the plan's hour on, so the penalty does not wait for a
    /// login; at half past midnight it leaves them, a round begun before midnight may still end. Red when the timer
    /// leaves them at noon or charges them at night.
    /// </summary>
    [Theory]
    [InlineData(12, true)]
    [InlineData(0, false)]
    public async Task Zeitgeber_Rechnet_Verpasste_Aufgaben_Ab(int hour, bool charged)
    {
        var s = await SetupAsync();
        // The suite's learning day is the UTC day: at 00:30 today is Monday, Friday is closed, the plan's hour is not reached.
        factory.Clock.FreezeAt(new DateTimeOffset(Today.ToDateTime(new TimeOnly(hour, 30)), TimeSpan.Zero));
        Enforce(s, Thursday, penalty: 3);
        DayPlan(s, Friday, PlannerMode.Enforced, s.D);

        await ActivatorUtilities.CreateInstance<Pugling.Api.Services.Supervisor.DailyPlanTimer>(factory.Services)
            .RunOnceAsync(CancellationToken.None);

        Assert.Equal(charged ? [Friday] : [], TaskPenaltyDays(s));
    }

    /// <summary>
    /// B-736 AK 7: Saturday's shadow plan left W open; today's shadow plan brings W back and says why, D (met by its test, never
    /// stamped) does not. Red when the planner does not read yesterday's open tasks, or takes a missing stamp for a missed task.
    /// </summary>
    [Fact]
    public async Task Offene_Aufgabe_Kommt_Am_Naechsten_Lerntag_Wieder()
    {
        var s = await SetupAsync();
        var res = await s.Father.PatchAsJsonAsync($"/api/v1/supervisor/children/{s.ChildId}", new
        {
            dailyTasksMin = 2,
            dailyTasksMax = 5,
            dailyNewWordsMin = 1,
            dailyNewWordsMax = 4,
            plannerMode = "Shadow",
        });
        Assert.True(res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
        DayPlan(s, Saturday, PlannerMode.Shadow, s.D, s.W);
        // D was met on Saturday without a stamp (a round never closed): the rule decides, not the missing stamp.
        Test(s, s.D, Saturday);

        var plans = await s.Father.GetFromJsonAsync<JsonElement>($"/api/v1/supervisor/children/{s.ChildId}/daily-plans");
        var tasks = plans.EnumerateArray().Single(p => p.GetProperty("day").GetString() == "2026-11-30").GetProperty("tasks")
            .EnumerateArray().ToDictionary(t => t.GetProperty("planPositionId").GetInt32(), t => t.GetProperty("reason").GetString()!);

        Assert.Contains("am letzten Lerntag offen geblieben", tasks[s.W]);
        Assert.DoesNotContain("offen geblieben", tasks[s.D]);
    }

    /// <summary>
    /// B-1037: without an amount (0) and in a shadow plan an open task costs nothing, and a task older than the look-back of 14 days
    /// is not settled any more. Red when the settlement books a 0-coin row (beside Thursday's old position penalty in the same run),
    /// charges a shadow task, or reaches past the look-back.
    /// </summary>
    [Theory]
    [InlineData("ohne Betrag")]
    [InlineData("Schattenplan")]
    [InlineData("zu alt")]
    public async Task Ohne_Betrag_Im_Schatten_Und_Zu_Alt_Kostet_Nichts(string fall)
    {
        var s = await SetupAsync();
        var day = fall == "zu alt" ? Today.AddDays(-16) : Friday;
        // The plan runs before the day (else the plan's own start would keep it free), and an old position penalty on Thursday
        // makes the run save: a 0-coin task row would ride along with it.
        Write(db => db.StudyPlans.Single(p => p.Id == s.PlanId).StartDate = day.AddDays(-7));
        Enforce(s, fall == "ohne Betrag" ? Friday : day.AddDays(-1), penalty: fall == "ohne Betrag" ? 0 : 3);
        if (fall == "ohne Betrag") Test(s, s.D, Thursday, passed: false);
        DayPlan(s, day, fall == "Schattenplan" ? PlannerMode.Shadow : PlannerMode.Enforced, s.D);
        if (fall == "Schattenplan") Write(db => db.Children.Single(c => c.Id == s.ChildId).PlannerMode = PlannerMode.Shadow);

        await SettleAsync(s);

        Assert.Empty(TaskPenaltyDays(s));
        Assert.DoesNotContain(PenaltyLedger(s), e => e.Reason.StartsWith("[Tagesplan", StringComparison.Ordinal));
    }
}
