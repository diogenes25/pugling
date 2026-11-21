using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-613: a duty counts for streak, missions, timeline and penalty only from the day it existed (created, or switched from
/// free to a cadence) and only on days its plan was switched on. Every case names the change that makes it red. Plans that
/// must run in the past are set up in the database (the API only plays today); the change under test always goes through
/// the API the father uses.
/// </summary>
public class RueckwirkendePflichtTests : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory factory;

    public RueckwirkendePflichtTests(PuglingWebAppFactory factory)
    {
        this.factory = factory;
        // Noon of a fixed day: the cases walk days backward from today and must not straddle a UTC midnight.
        factory.Clock.FreezeAt(new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero));
    }

    private DateOnly Today => DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime);

    private sealed record Setup(HttpClient Father, HttpClient Child, int ChildId, int ExerciseId, int PlanA, int PositionA);

    // A fresh child with plan A: one daily goal since three days ago, met on each of them and today - a streak of 4.
    private async Task<Setup> SetupAsync(bool metToday = true)
    {
        var father = await TestApi.AdultAsync(factory);
        var pin = "6" + Random.Shared.Next(100, 999);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Serien-Kind"), pin }));
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father); // hello→hallo, goodbye→tschüss
        var (planA, positionA) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText, childId: childId);
        Backdate(planA, positionA, Today.AddDays(-3));
        for (var d = -3; d <= (metToday ? 0 : -1); d++) SeedPassedTest(planA, positionA, Today.AddDays(d));
        return new Setup(father, await TestApi.ChildAsync(factory, childId, pin), childId, exerciseId, planA, positionA);
    }

    // The plan runs since start and its position exists since then.
    private void Backdate(int planId, int positionId, DateOnly start)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        db.StudyPlans.Single(p => p.Id == planId).StartDate = start;
        db.PlanPositions.Single(p => p.Id == positionId).CreatedAt = start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        db.SaveChanges();
    }

    private void Change<T>(int id, Action<T> change) where T : class
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        change(db.Set<T>().Find(id)!);
        db.SaveChanges();
    }

    private T Read<T>(Func<PuglingDbContext, T> read)
    {
        using var scope = factory.Services.CreateScope();
        return read(scope.ServiceProvider.GetRequiredService<PuglingDbContext>());
    }

    // A passed test on a day, written straight into the database: the API only tests today.
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

    private static async Task PassTestAsync(HttpClient child, int planId, int positionId)
    {
        var testsUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";
        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(testsUrl, new { }), "attemptId");
        var answers = new[] { new { itemIndex = 0, givenAnswer = "hello" }, new { itemIndex = 1, givenAnswer = "goodbye" } };
        (await child.PostAsJsonAsync($"{testsUrl}/{attemptId}/submit", new { answers })).EnsureSuccessStatusCode();
    }

    private static async Task<JsonElement> OkJsonAsync(HttpResponseMessage res)
    {
        Assert.True(res.IsSuccessStatusCode, $"{(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<int> StreakAsync(Setup s) =>
        (await OkJsonAsync(await s.Child.GetAsync($"/api/v1/student/children/{s.ChildId}/today")))
        .GetProperty("currentStreak").GetInt32();

    private async Task<int> MetricAsync(int childId, ProgressMetric metric)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<MetricsService>()
            .ValueAsync(childId, metric, null, null, Today, CancellationToken.None);
    }

    private async Task<int> SettleAsync(int childId)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<PositionProgressService>()
            .SettleClosedPeriodsAsync(childId, Today, CancellationToken.None);
    }

    private List<DateOnly> PenaltyDays(int positionId) =>
        Read(db => db.PositionGoalPenalties.Where(p => p.PlanPositionId == positionId).Select(p => p.PeriodStart)
            .OrderBy(d => d).ToList());

    // The supervisor's timeline, one row per day (B-558).
    private async Task<List<JsonElement>> TimelineAsync(Setup s, DateOnly from, DateOnly to) =>
    [
        .. (await OkJsonAsync(await s.Father.GetAsync(
                $"/api/v1/supervisor/children/overview?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&granularity=Day&childId={s.ChildId}")))
            .GetProperty("children")[0].GetProperty("periods").EnumerateArray(),
    ];

    // Plan B of another subject, created through the API with a start three days back.
    private async Task<int> CreateBackdatedPlanAsync(Setup s)
    {
        var subjectId = await TestApi.IdAsync(await s.Father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Mathe-Serie") }));
        return await TestApi.IdAsync(await s.Father.PostAsJsonAsync("/api/v1/supervisor/study-plans", new
        {
            childId = s.ChildId,
            title = "Plan B",
            subjectId,
            startDate = Today.AddDays(-3),
            durationDays = 10,
        }));
    }

    // A daily goal added through the API today, its words already introduced so the child can take the test.
    private async Task<int> AddDailyPositionAsync(Setup s, int planId)
    {
        var positionId = await TestApi.IdAsync(await s.Father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId = s.ExerciseId, cadence = "Daily", stage = (int)TestStage.FreeText, useLeitner = true }));
        TestApi.MarkIntroduced(factory, positionId);
        return positionId;
    }

    /// <summary>
    /// AK 1: plan B started three days back, its daily goal added today; both plans met today ⇒ the streak stays 4. Red
    /// without the obligation filter in <c>DutyStateAsync</c> (the new goal is open on the three past days ⇒ 1).
    /// </summary>
    [Fact]
    public async Task Rueckdatierter_Plan_Bricht_Die_Serie_Nicht()
    {
        var s = await SetupAsync();
        var planB = await CreateBackdatedPlanAsync(s);
        var positionB = await AddDailyPositionAsync(s, planB);
        Assert.Equal(0, await StreakAsync(s)); // the streak counts from today, and today plan B is still open
        await PassTestAsync(s.Child, planB, positionB);

        Assert.Equal(4, await StreakAsync(s));
    }

    /// <summary>
    /// AK 2: a daily goal added today to the running plan A ⇒ streak 4 once met. Red without the obligation filter. The
    /// fallback to <c>CreatedAt</c> would hold this case even without <c>DutySince</c>: both say "today".
    /// </summary>
    [Fact]
    public async Task Nachgetragene_Position_Bricht_Die_Serie_Nicht()
    {
        var s = await SetupAsync();
        var added = await AddDailyPositionAsync(s, s.PlanA);
        await PassTestAsync(s.Child, s.PlanA, added);

        Assert.Equal(4, await StreakAsync(s));
    }

    /// <summary>
    /// AK 2, second path: the weak-word review (B-584) always lays a daily duty into the chosen plan. Red without the
    /// obligation filter, exactly like the plain create.
    /// </summary>
    [Fact]
    public async Task Schwaechen_Wiederholung_Bricht_Die_Serie_Nicht()
    {
        var s = await SetupAsync();
        // The review files its exercise in the unit the words come from, so the plan has to share that subject.
        var subjectId = Read(db => db.Exercises.Where(e => e.Id == s.ExerciseId).Select(e => e.SeriesUnit!.Series!.SubjectId).Single());
        Change<StudyPlan>(s.PlanA, p => p.SubjectId = subjectId);
        var word = Read(db => db.ExerciseItems.Where(i => i.ExerciseId == s.ExerciseId).OrderBy(i => i.OrderIndex).First());
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            db.ItemProgress.Add(new ItemProgress { ChildId = s.ChildId, ItemId = word.Id, ExerciseId = s.ExerciseId, VocabularyId = word.VocabularyId });
            db.SaveChanges();
        }
        var review = await OkJsonAsync(await s.Father.PostAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{s.PlanA}/positions/from-words", new { vocabularyIds = new[] { word.VocabularyId } }));
        var reviewId = review.GetProperty("id").GetInt32();
        Assert.Equal(Today, Read(db => db.PlanPositions.Single(p => p.Id == reviewId).DutySince)); // data, see below
        Assert.Equal(0, await StreakAsync(s)); // the review is today's open duty
        SeedPassedTest(s.PlanA, reviewId, Today);

        Assert.Equal(4, await StreakAsync(s));
    }

    /// <summary>
    /// AK 3: a daily goal created yesterday and left open yesterday breaks the streak there. Red if the lower bound lies a
    /// day late (<c>day &gt; DutyBegins</c>): yesterday would not count and the streak would be 4.
    /// </summary>
    [Fact]
    public async Task Gestern_Angelegte_Offene_Pflicht_Bricht_Gestern()
    {
        var s = await SetupAsync();
        var added = await AddDailyPositionAsync(s, s.PlanA);
        Change<PlanPosition>(added, p => p.CreatedAt = Today.AddDays(-1).ToDateTime(new TimeOnly(18, 0), DateTimeKind.Utc));
        Change<PlanPosition>(added, p => p.DutySince = Today.AddDays(-1));
        SeedPassedTest(s.PlanA, added, Today);

        Assert.Equal(1, await StreakAsync(s));
    }

    /// <summary>
    /// AK 4: the mission metrics keep their days when a backdated plan B with a goal from today arrives. Red without the
    /// obligation filter (<c>CompleteChildDaysAsync</c> shares <c>DutyStateAsync</c>): the three past days fall out.
    /// </summary>
    [Fact]
    public async Task Missionstage_Bleiben_Beim_Rueckdatierten_Plan()
    {
        var s = await SetupAsync();
        Assert.Equal(4, await MetricAsync(s.ChildId, ProgressMetric.DaysComplete));
        Assert.Equal(4, await MetricAsync(s.ChildId, ProgressMetric.StreakDays));

        var planB = await CreateBackdatedPlanAsync(s);
        var positionB = await AddDailyPositionAsync(s, planB);
        await PassTestAsync(s.Child, planB, positionB);

        Assert.Equal(4, await MetricAsync(s.ChildId, ProgressMetric.DaysComplete));
        Assert.Equal(4, await MetricAsync(s.ChildId, ProgressMetric.StreakDays));
    }

    /// <summary>
    /// AK 5: six met days of plan A, then today a backdated plan B with a goal from today; meeting both today opens the
    /// chest of the seventh streak day (tier ×1.5 from 7 days). Red without the obligation filter: streak 1, tier ×1.
    /// </summary>
    [Fact]
    public async Task Truhe_Am_Siebten_Serientag_Trotz_Rueckdatiertem_Plan()
    {
        var s = await SetupAsync(metToday: false);
        Backdate(s.PlanA, s.PositionA, Today.AddDays(-6));
        for (var d = -6; d <= -4; d++) SeedPassedTest(s.PlanA, s.PositionA, Today.AddDays(d));
        var planB = await CreateBackdatedPlanAsync(s);
        var positionB = await AddDailyPositionAsync(s, planB);

        await PassTestAsync(s.Child, s.PlanA, s.PositionA);
        await PassTestAsync(s.Child, planB, positionB);

        var today = Today;
        var claim = Read(db => db.DailyBoxClaims.AsNoTracking().Single(c => c.ChildId == s.ChildId && c.Day == today));
        Assert.Equal(7, claim.StreakAtClaim);
    }

    /// <summary>
    /// AK 6: in the supervisor's timeline a day before the goal existed carries no open duty of it. Plan A met every day;
    /// plan B, started three days back, gets its goal today ⇒ the past days count only A's goal (1) and are done. Red
    /// without the mask in <c>ComputeDayAsync</c> (goals 2, duty open).
    /// </summary>
    [Fact]
    public async Task Zeitachse_Kennt_Keine_Pflicht_Vor_Dem_Anlegetag()
    {
        var s = await SetupAsync();
        var planB = await CreateBackdatedPlanAsync(s);
        await AddDailyPositionAsync(s, planB);

        var days = await TimelineAsync(s, Today.AddDays(-2), Today.AddDays(-1));
        // Plan A met both days, plan B had no goal yet: one goal each day, and it is met.
        Assert.All(days, d =>
        {
            Assert.Equal(1, d.GetProperty("goalsTotal").GetInt32());
            JsonAssert.True(d, "dutyDone");
        });
    }

    /// <summary>
    /// AK 7: plan B (same subject) was off with an open daily goal since three days; plan A ran and was met. The father
    /// switches B back on (A goes off) and the child meets B today ⇒ the days B was off stay complete, in the streak and
    /// in the timeline. Red without <c>ActivatedAt</c> in <c>DutyStateAsync</c> (streak 1) and in the timeline
    /// (<c>ChildrenDashboardService</c>: the days read open).
    /// </summary>
    [Fact]
    public async Task Wieder_Eingeschalteter_Plan_Bricht_Die_Serie_Nicht()
    {
        var s = await SetupAsync();
        var (planB, positionB) = TestApi.SeedLeitnerPosition(factory, s.ExerciseId, (int)TestStage.FreeText, childId: s.ChildId);
        Backdate(planB, positionB, Today.AddDays(-3));
        Change<StudyPlan>(planB, p => p.Active = false);
        var subjectId = Read(db => db.StudyPlans.Where(p => p.Id == s.PlanA).Select(p => p.SubjectId).Single());
        Change<StudyPlan>(planB, p => p.SubjectId = subjectId);

        await OkJsonAsync(await s.Father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{planB}", new { active = true }));
        Assert.False(Read(db => db.StudyPlans.Single(p => p.Id == s.PlanA).Active)); // one active plan per subject
        await PassTestAsync(s.Child, planB, positionB);

        Assert.Equal(4, await StreakAsync(s));
        Assert.All(await TimelineAsync(s, Today.AddDays(-3), Today.AddDays(-1)), d => JsonAssert.True(d, "dutyDone"));
    }

    /// <summary>
    /// AK 8: as AK 7 with a penalty on B's goal and the child having played B before it was switched off: the next
    /// settlement books nothing for the days B was off. Red without <c>ActivatedAt</c> in <c>PlanDueForPeriod</c> (three
    /// penalties).
    /// </summary>
    [Fact]
    public async Task Wieder_Eingeschalteter_Plan_Bucht_Keinen_Rueckwirkenden_Malus()
    {
        var s = await SetupAsync();
        var (planB, positionB) = TestApi.SeedLeitnerPosition(factory, s.ExerciseId, (int)TestStage.FreeText, childId: s.ChildId);
        Backdate(planB, positionB, Today.AddDays(-4));
        SeedPassedTest(planB, positionB, Today.AddDays(-4)); // played (and met) before it was switched off
        Change<StudyPlan>(planB, p => p.Active = false);
        Change<PlanPosition>(positionB, p => p.PenaltyCoins = 10);

        await OkJsonAsync(await s.Father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{planB}", new { active = true }));
        await SettleAsync(s.ChildId);

        Assert.Empty(PenaltyDays(positionB));
    }

    /// <summary>
    /// Counter-probe to AK 7/8: a second <c>active: true</c> on a plan already on does not move its switch-on day. Plan A
    /// with a penalty, met three days back and two, open yesterday ⇒ the settlement charges yesterday. Red if the setter
    /// stamps every <c>active: true</c>: the plan would count as on since today and owe nothing.
    /// </summary>
    [Fact]
    public async Task Erneutes_Einschalten_Verschiebt_Das_Einschaltdatum_Nicht()
    {
        var s = await SetupAsync(metToday: false);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            db.TestAttempts.RemoveRange(db.TestAttempts.Where(t => t.PlanPositionId == s.PositionA && t.Day == Today.AddDays(-1)));
            db.PlanPositions.Single(p => p.Id == s.PositionA).PenaltyCoins = 10;
            db.SaveChanges();
        }

        await OkJsonAsync(await s.Father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{s.PlanA}", new { active = true }));
        await SettleAsync(s.ChildId);

        Assert.Equal([Today.AddDays(-1)], PenaltyDays(s.PositionA));
    }

    /// <summary>
    /// AK 9: a free position created three days back is switched to daily today. Streak and mission days of the three
    /// days before stay, and the settlement books no penalty for them; from today it is a duty (open until met). Red
    /// without <c>DutySince</c> set on the switch: the fallback to <c>CreatedAt</c> makes it a duty for the three days ⇒
    /// streak 1 and three penalties.
    /// </summary>
    [Fact]
    public async Task Kuer_Wird_Pflicht_Erst_Ab_Der_Umstellung()
    {
        var s = await SetupAsync();
        var free = await TestApi.IdAsync(await s.Father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{s.PlanA}/positions",
            new { exerciseId = s.ExerciseId, stage = (int)TestStage.FreeText, useLeitner = true, penaltyCoins = 10 }));
        Change<PlanPosition>(free, p =>
        {
            p.CreatedAt = Today.AddDays(-3).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            p.DutySince = Today.AddDays(-3);
        });

        await OkJsonAsync(await s.Father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{s.PlanA}/positions/{free}",
            new { cadence = "Daily" }));
        Assert.Equal(0, await StreakAsync(s)); // today the new duty is open
        SeedPassedTest(s.PlanA, free, Today);

        Assert.Equal(4, await StreakAsync(s));
        Assert.Equal(4, await MetricAsync(s.ChildId, ProgressMetric.DaysComplete));
        await SettleAsync(s.ChildId);
        Assert.Empty(PenaltyDays(free));
    }

    /// <summary>
    /// AK 9, second case: saving a goal that is already daily with <c>cadence: Daily</c> (the position form sends it on
    /// every save) does not move "duty since". Red if the setter does not check the switch from free: the goal would be a
    /// duty since today only, the three days before would carry no duty and the streak fall to 1.
    /// </summary>
    [Fact]
    public async Task Speichern_Mit_Gleichem_Takt_Verschiebt_Pflicht_Seit_Nicht()
    {
        var s = await SetupAsync();
        Change<PlanPosition>(s.PositionA, p => p.DutySince = Today.AddDays(-3));

        await OkJsonAsync(await s.Father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{s.PlanA}/positions/{s.PositionA}",
            new { cadence = "Daily" }));

        Assert.Equal(4, await StreakAsync(s));
    }

    /// <summary>
    /// AK 10/11 at the penalty: a plan on since its start keeps owing its first, partial week (B-432). Created through the
    /// API with a Wednesday start (so its switch-on day is that Wednesday), a weekly goal with a penalty, played but not met
    /// in that week ⇒ the settlement charges the week starting on its Monday. Red if the switch-on rule applied to every
    /// plan (<c>from &gt;= ActivatedAt</c> without the <c>ActivatedAt &lt;= StartDate</c> guard): the week would start before
    /// the switch-on day and be waived.
    /// </summary>
    [Fact]
    public async Task Erste_Angebrochene_Woche_Bleibt_Faellig()
    {
        var s = await SetupAsync();
        var wednesday = Today.AddDays(-7); // the frozen today is a Wednesday
        var monday = PositionProgressService.WeekMonday(wednesday);
        Assert.NotEqual(monday, wednesday);
        var subjectId = await TestApi.IdAsync(await s.Father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Wochen-Fach") }));
        var planB = await TestApi.IdAsync(await s.Father.PostAsJsonAsync("/api/v1/supervisor/study-plans", new
        {
            childId = s.ChildId,
            title = "Plan B",
            subjectId,
            startDate = wednesday,
            durationDays = 20,
        }));
        var positionB = await TestApi.IdAsync(await s.Father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planB}/positions",
            new { exerciseId = s.ExerciseId, cadence = "Weekly", stage = (int)TestStage.FreeText, useLeitner = true, penaltyCoins = 10 }));
        Change<PlanPosition>(positionB, p =>
        {
            p.CreatedAt = wednesday.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            p.DutySince = wednesday;
        });
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            // Played on its first day, but not passed: the week's goal stays open.
            db.TestAttempts.Add(new TestAttempt
            {
                StudyPlanId = planB,
                PlanPositionId = positionB,
                Day = wednesday,
                StartedAt = wednesday.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc),
            });
            db.SaveChanges();
        }

        await SettleAsync(s.ChildId);

        Assert.Equal([monday], PenaltyDays(positionB));
    }

    /// <summary>
    /// Review finding to B-613: a future plan whose start the father pulls forward to today is on from today - a missed
    /// day after that costs its penalty. Red without the switch-on day following the moved start: the plan would count
    /// as switched on only at its old start, and the missed day would be waived.
    /// </summary>
    [Fact]
    public async Task Vorgezogener_Start_Gilt_Ab_Heute_Als_Eingeschaltet()
    {
        var s = await SetupAsync();
        var subjectId = await TestApi.IdAsync(await s.Father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Zukunfts-Fach") }));
        var planB = await TestApi.IdAsync(await s.Father.PostAsJsonAsync("/api/v1/supervisor/study-plans", new
        {
            childId = s.ChildId,
            title = "Plan B",
            subjectId,
            startDate = Today.AddDays(5),
            durationDays = 10,
        }));
        await OkJsonAsync(await s.Father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{planB}",
            new { startDate = Today }));
        var positionB = await TestApi.IdAsync(await s.Father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planB}/positions",
            new { exerciseId = s.ExerciseId, cadence = "Daily", stage = (int)TestStage.FreeText, useLeitner = true, penaltyCoins = 10 }));
        var day0 = Today;
        SeedPassedTest(planB, positionB, day0); // played and met on its first day

        factory.Clock.Advance(TimeSpan.FromDays(2));
        await SettleAsync(s.ChildId);

        Assert.Equal([day0.AddDays(1)], PenaltyDays(positionB));
    }

    // B-672: plan B of a fresh subject created through the API, starting at the given day.
    private async Task<int> CreatePlanAsync(Setup s, DateOnly start)
    {
        var subjectId = await TestApi.IdAsync(await s.Father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Verlege-Fach") }));
        return await TestApi.IdAsync(await s.Father.PostAsJsonAsync("/api/v1/supervisor/study-plans", new
        {
            childId = s.ChildId,
            title = "Plan B",
            subjectId,
            startDate = start,
            durationDays = 30,
        }));
    }

    private async Task<int> AddPenaltyPositionAsync(Setup s, int planId)
    {
        var positionId = await TestApi.IdAsync(await s.Father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId = s.ExerciseId, cadence = "Daily", stage = (int)TestStage.FreeText, useLeitner = true, penaltyCoins = 10 }));
        TestApi.MarkIntroduced(factory, positionId);
        return positionId;
    }

    private async Task MoveStartAsync(Setup s, int planId, DateOnly start) =>
        await OkJsonAsync(await s.Father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}", new { startDate = start }));

    private DateOnly ActivatedAt(int planId) => Read(db => db.StudyPlans.Single(p => p.Id == planId).ActivatedAt);

    /// <summary>
    /// B-672 AK 1: a plan running since T is pushed to T+5, and at T+7 pulled back to T. The days T+1..T+4 were never
    /// playable (old start T+5), so the settlement charges only T+5 and T+6. Red without raising <c>ActivatedAt</c> to
    /// <c>min(old start, today)</c> (T+1..T+6 charged), and red when the raise takes today instead (nothing charged).
    /// </summary>
    [Fact]
    public async Task Zurueckverlegter_Start_Bucht_Keinen_Malus_Fuer_Unspielbare_Tage()
    {
        var s = await SetupAsync();
        var t = Today;
        var planB = await CreatePlanAsync(s, t);
        var positionB = await AddPenaltyPositionAsync(s, planB);
        SeedPassedTest(planB, positionB, t);
        await MoveStartAsync(s, planB, t.AddDays(5));

        factory.Clock.Advance(TimeSpan.FromDays(7));
        await MoveStartAsync(s, planB, t);
        await SettleAsync(s.ChildId);

        Assert.Equal([t.AddDays(5), t.AddDays(6)], PenaltyDays(positionB));
    }

    /// <summary>
    /// B-672 AK 2: plan B starts at T+13 but is switched on at T. At T+7 the father moves the start to T+6, a day that was
    /// not playable (old start T+13); plan A is met every day and B is met today ⇒ the streak runs on through T+6 (11).
    /// Red without the raise (T+6 counts as an open duty day ⇒ 1), and red when the old start is read after the dates are
    /// overwritten (<c>min</c> then yields the new start T+6).
    /// </summary>
    [Fact]
    public async Task Zurueckverlegter_Start_Bricht_Die_Serie_Nicht()
    {
        var s = await SetupAsync();
        var t = Today;
        var planB = await CreatePlanAsync(s, t.AddDays(13));
        Change<StudyPlan>(planB, p => p.ActivatedAt = t); // switched on today, ahead of its start
        var positionB = await AddDailyPositionAsync(s, planB);
        Change<StudyPlan>(s.PlanA, p => p.EndDate = t.AddDays(30)); // the seeded plan A would end at T+5
        for (var d = 1; d <= 7; d++) SeedPassedTest(s.PlanA, s.PositionA, t.AddDays(d));

        factory.Clock.Advance(TimeSpan.FromDays(7));
        await MoveStartAsync(s, planB, t.AddDays(6));
        await PassTestAsync(s.Child, planB, positionB);

        Assert.Equal(11, await StreakAsync(s));
        Assert.Equal(t.AddDays(7), ActivatedAt(planB));
    }

    /// <summary>
    /// B-672 AK 4, a data assertion (behaviour is the same, the days lie before the start): the raise is limited to a
    /// moved start of an active plan. A plan on since T with start T+10, at T+12: a PATCH without a start, one with the
    /// unchanged start, and a moved start on a switched-off plan all leave <c>ActivatedAt</c> at T. Red when the
    /// <c>startMoved</c> or the <c>Active</c> gate falls (it would then read T+10).
    /// </summary>
    [Fact]
    public async Task Ohne_Verlegung_Wird_Nichts_Angehoben()
    {
        var s = await SetupAsync();
        var t = Today;
        var planB = await CreatePlanAsync(s, t.AddDays(10));
        Change<StudyPlan>(planB, p => p.ActivatedAt = t);
        var planC = await CreatePlanAsync(s, t.AddDays(10));
        Change<StudyPlan>(planC, p => { p.ActivatedAt = t; p.Active = false; });

        factory.Clock.Advance(TimeSpan.FromDays(12));
        await OkJsonAsync(await s.Father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{planB}", new { title = "Plan B2" }));
        Assert.Equal(t, ActivatedAt(planB));
        await MoveStartAsync(s, planB, t.AddDays(10));
        Assert.Equal(t, ActivatedAt(planB));
        await MoveStartAsync(s, planC, t.AddDays(11));
        Assert.Equal(t, ActivatedAt(planC));
    }

    /// <summary>
    /// B-672 AK 5: a plan running since T, at T+3 its start is set to T+2 - later, but not after today. T+2 was playable
    /// (old start T), so missing it still costs: the settlement at T+4 charges T+2 and T+3. Red with the rejected short
    /// rule <c>ActivatedAt = playableFrom</c> on every move, or a raise past <c>min(old start, today)</c> (only T+3).
    /// </summary>
    [Fact]
    public async Task Spielbare_Tage_Bleiben_Pflicht()
    {
        var s = await SetupAsync();
        var t = Today;
        var planB = await CreatePlanAsync(s, t);
        var positionB = await AddPenaltyPositionAsync(s, planB);

        factory.Clock.Advance(TimeSpan.FromDays(3));
        await MoveStartAsync(s, planB, t.AddDays(2));
        factory.Clock.Advance(TimeSpan.FromDays(1));
        await SettleAsync(s.ChildId);

        Assert.Equal([t.AddDays(2), t.AddDays(3)], PenaltyDays(positionB));
    }

    private async Task MoveEndAsync(Setup s, int planId, DateOnly end) =>
        await OkJsonAsync(await s.Father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}", new { endDate = end }));

    // B-783: plan B with a penalised daily goal, running T..T+5 and met on each of those days.
    private async Task<(int Plan, int Position)> ExpiredPlanAsync(Setup s, DateOnly t)
    {
        var planB = await CreatePlanAsync(s, t);
        await MoveEndAsync(s, planB, t.AddDays(5));
        var positionB = await AddPenaltyPositionAsync(s, planB);
        for (var d = 0; d <= 5; d++) SeedPassedTest(planB, positionB, t.AddDays(d));
        return (planB, positionB);
    }

    /// <summary>
    /// B-783 AK 1: plan B ran T..T+5 and expired; at T+10 the father extends it to T+30. T+6..T+9 were not playable (past
    /// the old end), so the settlement charges nothing. Red without raising <c>ActivatedAt</c> on the extension (four
    /// penalties), and red with the raise to <c>min(old end + 1, today)</c> (the gap would still lie after it).
    /// </summary>
    [Fact]
    public async Task Verlaengerter_Abgelaufener_Plan_Bucht_Keinen_Malus_Fuer_Die_Luecke()
    {
        var s = await SetupAsync();
        var t = Today;
        var (planB, positionB) = await ExpiredPlanAsync(s, t);

        factory.Clock.Advance(TimeSpan.FromDays(10));
        await MoveEndAsync(s, planB, t.AddDays(30));
        await SettleAsync(s.ChildId);

        Assert.Empty(PenaltyDays(positionB));
    }

    /// <summary>
    /// B-783 AK 2: as AK 1, plan A met every day and B met today ⇒ the gap days T+6..T+9 do not break the streak (14 =
    /// T-3..T+10). Red without the raise (T+9 is an open duty of an active plan ⇒ 1).
    /// </summary>
    [Fact]
    public async Task Verlaengerter_Abgelaufener_Plan_Bricht_Die_Serie_Nicht()
    {
        var s = await SetupAsync();
        var t = Today;
        var (planB, positionB) = await ExpiredPlanAsync(s, t);
        Change<StudyPlan>(s.PlanA, p => p.EndDate = t.AddDays(30)); // the seeded plan A would end at T+5
        for (var d = 1; d <= 10; d++) SeedPassedTest(s.PlanA, s.PositionA, t.AddDays(d));

        factory.Clock.Advance(TimeSpan.FromDays(10));
        await MoveEndAsync(s, planB, t.AddDays(30));
        await PassTestAsync(s.Child, planB, positionB);

        Assert.Equal(14, await StreakAsync(s));
    }

    /// <summary>
    /// B-783 AK 3: a plan that ended yesterday has no gap - extending it frees nothing. Plan B T..T+9, T+8 missed, at T+10
    /// extended ⇒ T+8 is still charged. Red when the rule fires for an end of yesterday too (it would raise to today and
    /// free T+8).
    /// </summary>
    [Fact]
    public async Task Gestern_Abgelaufener_Plan_Bleibt_Pflicht()
    {
        var s = await SetupAsync();
        var t = Today;
        var planB = await CreatePlanAsync(s, t);
        await MoveEndAsync(s, planB, t.AddDays(9));
        var positionB = await AddPenaltyPositionAsync(s, planB);
        for (var d = 0; d <= 9; d++) if (d != 8) SeedPassedTest(planB, positionB, t.AddDays(d));

        factory.Clock.Advance(TimeSpan.FromDays(10));
        await MoveEndAsync(s, planB, t.AddDays(30));
        await SettleAsync(s.ChildId);

        Assert.Equal([t.AddDays(8)], PenaltyDays(positionB));
    }

    /// <summary>
    /// B-783 AK 3b, the other side of AK 3: the shortest gap is one day. Plan B T..T+8 (all met), extended at T+10 ⇒ T+9
    /// was not playable and is not charged. Red when the gap condition demands more than one day (<c>oldEnd + 2 &lt; today</c>).
    /// </summary>
    [Fact]
    public async Task Eintaegige_Luecke_Bucht_Keinen_Malus()
    {
        var s = await SetupAsync();
        var t = Today;
        var planB = await CreatePlanAsync(s, t);
        await MoveEndAsync(s, planB, t.AddDays(8));
        var positionB = await AddPenaltyPositionAsync(s, planB);
        for (var d = 0; d <= 8; d++) SeedPassedTest(planB, positionB, t.AddDays(d));

        factory.Clock.Advance(TimeSpan.FromDays(10));
        await MoveEndAsync(s, planB, t.AddDays(30));
        await SettleAsync(s.ChildId);

        Assert.Empty(PenaltyDays(positionB));
    }

    /// <summary>
    /// B-783 AK 4, a data assertion: only an extension of an active plan raises <c>ActivatedAt</c>. An expired plan whose
    /// title is edited, or whose end is moved but not later, keeps it; so does an extended switched-off plan (switching
    /// on sets it anyway). Red when the "later end" condition falls (the title edit would raise it to today) or the
    /// <c>Active</c> gate falls.
    /// </summary>
    [Fact]
    public async Task Ohne_Verlaengerung_Wird_Nichts_Angehoben()
    {
        var s = await SetupAsync();
        var t = Today;
        var (planB, _) = await ExpiredPlanAsync(s, t);
        var (planC, _) = await ExpiredPlanAsync(s, t);
        Change<StudyPlan>(planC, p => p.Active = false);

        factory.Clock.Advance(TimeSpan.FromDays(10));
        await OkJsonAsync(await s.Father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{planB}", new { title = "Plan B2" }));
        Assert.Equal(t, ActivatedAt(planB));
        await MoveEndAsync(s, planB, t.AddDays(4));
        Assert.Equal(t, ActivatedAt(planB));
        await MoveEndAsync(s, planC, t.AddDays(30));
        Assert.Equal(t, ActivatedAt(planC));
    }

    /// <summary>
    /// The creating write paths store the dates the backfill would give (B-613, risk 5): a plan is on since its start, a
    /// position a duty since its creation day - plan and position create, and the seed (the weak-word review is asserted in
    /// its own case above). Behaviourally the empty default falls back to the same values, so these are
    /// data assertions; red when a path stops setting its field (it then reads <c>0001-01-01</c>).
    /// </summary>
    [Fact]
    public async Task Anlegende_Schreibpfade_Setzen_Die_Daten()
    {
        var s = await SetupAsync();
        var planB = await CreateBackdatedPlanAsync(s);
        var positionB = await AddDailyPositionAsync(s, planB);

        Assert.Equal(Today.AddDays(-3), Read(db => db.StudyPlans.Single(p => p.Id == planB).ActivatedAt));
        Assert.Equal(Today, Read(db => db.PlanPositions.Single(p => p.Id == positionB).DutySince));
        // The seeded cross-section plan (Seed.cs) is created the same way.
        var seeded = Read(db => db.StudyPlans.Where(p => p.Description!.StartsWith("Automatisch geseedeter"))
            .Select(p => new { p.StartDate, p.ActivatedAt, Since = p.Positions.Select(x => x.DutySince).ToList() }).First());
        Assert.Equal(seeded.StartDate, seeded.ActivatedAt);
        Assert.All(seeded.Since, d => Assert.Equal(seeded.StartDate, d));
    }
}
