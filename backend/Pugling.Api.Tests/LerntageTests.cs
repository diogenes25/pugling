using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// The suite's other factories switch the free Sunday off (<c>LearningDay:SundayFree</c>), so a test that forms "today"
/// from the wall clock does not depend on the weekday it runs on. This one keeps the production rule for
/// <see cref="LerntageTests"/>.
/// </summary>
public sealed class LerntageWebAppFactory : PuglingWebAppFactoryBase
{
    /// <inheritdoc />
    protected override string EnvironmentName => "Development";

    /// <inheritdoc />
    protected override void ConfigureFactory(IWebHostBuilder builder)
    {
        builder.UseSetting("RateLimiting:LoginEnabled", "false");
        builder.UseSetting("Scoring:TimeSlotsEnabled", "false");
        builder.UseSetting("LearningDay:SundayFree", "true");
    }
}

/// <summary>
/// B-731: duty days are Monday to Saturday outside the child's pauses. Sunday and a pause neither count for the streak nor
/// break it, and cost no penalty; a weekly duty is still settled once after its Sunday. Past days are seeded in the
/// database (the API only plays today); pauses go through the supervisor's API. Today is Monday 2026-11-30, so Sunday is
/// the 29th and Saturday the 28th. Each case names the change that turns it red.
/// </summary>
public class LerntageTests : IClassFixture<LerntageWebAppFactory>
{
    private static readonly DateOnly Today = new(2026, 11, 30);
    private static readonly DateOnly Sunday = Today.AddDays(-1);
    private static readonly DateOnly Saturday = Today.AddDays(-2);
    private static readonly DateOnly Friday = Today.AddDays(-3);
    private static readonly DateOnly Thursday = Today.AddDays(-4);

    private readonly LerntageWebAppFactory factory;

    public LerntageTests(LerntageWebAppFactory factory)
    {
        this.factory = factory;
        factory.Clock.FreezeAt(new DateTimeOffset(2026, 11, 30, 12, 0, 0, TimeSpan.Zero));
    }

    private sealed record Setup(HttpClient Father, int ChildId, int PlanId, int PositionId);

    // A fresh child with one plan of one position of the given cadence and penalty, running since `start`, with a passed
    // test on every day in `metDays`. A failed test on the start day starts the penalty clock (B-535) without meeting it.
    private async Task<Setup> SetupAsync(GoalCadence cadence, DateOnly start, IEnumerable<DateOnly> metDays, int penalty = 0)
    {
        var father = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Lerntag-Kind"), pin = "7" + Random.Shared.Next(100, 999) }));
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText,
            childId: childId, cadence: cadence);
        Write(db =>
        {
            var plan = db.StudyPlans.Single(p => p.Id == planId);
            plan.StartDate = start;
            plan.EndDate = Today.AddDays(30);
            var pos = db.PlanPositions.Single(p => p.Id == positionId);
            pos.CreatedAt = start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            pos.PenaltyCoins = penalty;
        });
        SeedTest(planId, positionId, start, passed: false);
        foreach (var day in metDays) SeedTest(planId, positionId, day, passed: true);
        return new Setup(father, childId, planId, positionId);
    }

    private void SeedTest(int planId, int positionId, DateOnly day, bool passed)
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

    private async Task<int> StreakAsync(int childId)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<PositionProgressService>()
            .ChildStreakAsync(childId, Today, int.MaxValue, CancellationToken.None);
    }

    private async Task SettleAsync(int childId, DateOnly? today = null)
    {
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<PositionProgressService>()
            .SettleClosedPeriodsAsync(childId, today ?? Today, CancellationToken.None);
    }

    // Every item of the position introduced on `day` - the portion of a position without a daily dose.
    private void IntroduceAllOn(int positionId, DateOnly day) => Write(db =>
    {
        foreach (var p in db.PositionItemProgress.Where(p => p.PlanPositionId == positionId)) p.IntroducedAt = day;
    });

    private List<DateOnly> PenaltyPeriods(int positionId) => Read(db => db.PositionGoalPenalties
        .Where(p => p.PlanPositionId == positionId).Select(p => p.PeriodStart).OrderBy(d => d).ToList());

    private static async Task PauseAsync(Setup s, DateOnly from, DateOnly until)
    {
        var res = await s.Father.PostAsJsonAsync($"/api/v1/supervisor/children/{s.ChildId}/pauses",
            new { from, until, note = "krank" });
        Assert.True(res.IsSuccessStatusCode, $"{(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
    }

    // ---- AK 1: Sunday in the streak ----

    /// <summary>
    /// AK 1: Saturday and Monday met, Sunday empty or met - the streak is 2 either way (Friday is open). Red when the
    /// streak breaks on the empty Sunday (1), or counts a met Sunday (3).
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Sonntag_Bricht_Die_Serie_Nicht_Und_Zaehlt_Nicht(bool sundayMet)
    {
        var met = sundayMet ? new[] { Saturday, Sunday, Today } : [Saturday, Today];
        var s = await SetupAsync(GoalCadence.Daily, Thursday, met);

        Assert.Equal(2, await StreakAsync(s.ChildId));
    }

    // ---- AK 2 and 3: Sunday in the penalty ----

    /// <summary>
    /// AK 2: a daily duty open from Friday to Sunday costs Friday and Saturday, not Sunday. Red when the settlement books
    /// the Sunday (three periods).
    /// </summary>
    [Fact]
    public async Task Offene_Tagespflicht_Kostet_Am_Sonntag_Nichts()
    {
        var s = await SetupAsync(GoalCadence.Daily, Friday, [], penalty: 5);

        await SettleAsync(s.ChildId);

        Assert.Equal([Friday, Saturday], PenaltyPeriods(s.PositionId));
    }

    /// <summary>
    /// AK 3: a weekly duty (Monday to Sunday) open all week is settled exactly once after its Sunday, also when settled
    /// twice. Red when the Sunday rule drops the week (no period) or books it again.
    /// </summary>
    [Fact]
    public async Task Wochenpflicht_Wird_Genau_Einmal_Abgerechnet()
    {
        var monday = Today.AddDays(-7);
        var s = await SetupAsync(GoalCadence.Weekly, monday, [], penalty: 5);

        await SettleAsync(s.ChildId);
        await SettleAsync(s.ChildId);

        Assert.Equal([monday], PenaltyPeriods(s.PositionId));
    }

    // ---- AK 4: the pause ----

    /// <summary>
    /// AK 4, penalty: a pause from Friday to Saturday, entered after the fact, frees both days; Thursday still costs. Red
    /// when the settlement ignores the pause (Friday and Saturday booked).
    /// </summary>
    [Fact]
    public async Task Pause_Kostet_Keinen_Malus()
    {
        var s = await SetupAsync(GoalCadence.Daily, Thursday, [], penalty: 5);
        await PauseAsync(s, Friday, Saturday);

        await SettleAsync(s.ChildId);

        Assert.Equal([Thursday], PenaltyPeriods(s.PositionId));
    }

    /// <summary>
    /// AK 4, streak: Thursday and Monday met, Friday and Saturday paused, Sunday free - the streak is 2: the pause neither
    /// breaks it nor counts. Red when the streak breaks in the pause (1) or counts the paused days.
    /// </summary>
    [Fact]
    public async Task Pause_Haelt_Die_Serie()
    {
        var s = await SetupAsync(GoalCadence.Daily, Thursday, [Thursday, Today]);
        await PauseAsync(s, Friday, Saturday);

        Assert.Equal(2, await StreakAsync(s.ChildId));
    }

    /// <summary>
    /// AK 4, today: Thursday to Saturday met, today open and paused after the fact - the streak stays 3. Without the pause
    /// the open today ends it (0). Red when the pause for today is not read.
    /// </summary>
    [Fact]
    public async Task Pause_Fuer_Heute_Rueckwirkend()
    {
        var s = await SetupAsync(GoalCadence.Daily, Thursday, [Thursday, Friday, Saturday]);
        Assert.Equal(0, await StreakAsync(s.ChildId));

        await PauseAsync(s, Today, Today);

        Assert.Equal(3, await StreakAsync(s.ChildId));
    }

    /// <summary>
    /// AK 4, week: a weekly duty in a week touched by a pause owes nothing (stand-in decision). Red when the weekly
    /// settlement does not read the pause.
    /// </summary>
    [Fact]
    public async Task Wochenpflicht_In_Einer_Pausenwoche_Kostet_Nichts()
    {
        var monday = Today.AddDays(-7);
        var s = await SetupAsync(GoalCadence.Weekly, monday, [], penalty: 5);
        await PauseAsync(s, monday.AddDays(2), monday.AddDays(2));

        await SettleAsync(s.ChildId);

        Assert.Empty(PenaltyPeriods(s.PositionId));
    }

    // ---- B-1021: the spared introduction day is a duty day ----

    /// <summary>
    /// B-1021: a position created on a Sunday, its words introduced on Monday, settled on Tuesday - Monday is the
    /// introduction day and spared, as on any other first day. Red when the introduction day stays on the free Sunday
    /// (Monday is booked).
    /// </summary>
    [Fact]
    public async Task Einfuehrungstag_Am_Sonntag_Rueckt_Auf_Den_Montag()
    {
        var s = await SetupAsync(GoalCadence.Daily, Sunday, [], penalty: 5);
        IntroduceAllOn(s.PositionId, Today);

        await SettleAsync(s.ChildId, Today.AddDays(1));

        Assert.Empty(PenaltyPeriods(s.PositionId));
    }

    /// <summary>
    /// B-1021 with a pause: created Thursday, Thursday and Friday paused, the words introduced on Saturday - Saturday is
    /// the introduction day and spared. Red when the introduction day stays inside the pause (Saturday is booked).
    /// </summary>
    [Fact]
    public async Task Einfuehrungstag_In_Der_Pause_Rueckt_Hinter_Die_Pause()
    {
        var s = await SetupAsync(GoalCadence.Daily, Thursday, [], penalty: 5);
        await PauseAsync(s, Thursday, Friday);
        IntroduceAllOn(s.PositionId, Saturday);

        await SettleAsync(s.ChildId);

        Assert.Empty(PenaltyPeriods(s.PositionId));
    }

    /// <summary>
    /// B-1021, decision 2: the portion counts from the position's first due day - words introduced on the free Sunday
    /// spare the Monday the child did not play. Red when only introductions on the introduction day itself count (Monday
    /// is booked).
    /// </summary>
    [Fact]
    public async Task Einfuehrung_Am_Freien_Sonntag_Schont_Den_Montag()
    {
        var s = await SetupAsync(GoalCadence.Daily, Sunday, [], penalty: 5);
        IntroduceAllOn(s.PositionId, Sunday);

        await SettleAsync(s.ChildId, Today.AddDays(1));

        Assert.Empty(PenaltyPeriods(s.PositionId));
    }

    // ---- B-1022: the child sees a price only where the settlement books one ----

    private async Task<JsonElement> TodayPositionAsync(Setup s)
    {
        var today = await s.Father.GetFromJsonAsync<JsonElement>($"/api/v1/student/children/{s.ChildId}/today");
        return today.GetProperty("positions").EnumerateArray().Single(p => p.GetProperty("positionId").GetInt32() == s.PositionId);
    }

    /// <summary>
    /// B-1022 with B-613: a plan switched back on mid-week owes nothing for that week (the child could not play its first
    /// days), so the weekly duty shows no price. Wednesday, the plan reactivated on Tuesday. Red when <c>owesDuty</c> skips
    /// <c>PlanDueForPeriod</c>.
    /// </summary>
    [Fact]
    public async Task Mitten_In_Der_Woche_Wieder_Eingeschaltet_Droht_Die_Woche_Nichts_An()
    {
        var s = await SetupAsync(GoalCadence.Weekly, Thursday, [], penalty: 5);
        Write(db => db.StudyPlans.Single(p => p.Id == s.PlanId).ActivatedAt = Today.AddDays(1));
        factory.Clock.FreezeAt(new DateTimeOffset(2026, 12, 2, 12, 0, 0, TimeSpan.Zero));

        Assert.False((await TodayPositionAsync(s)).GetProperty("owesDuty").GetBoolean());
    }

    /// <summary>
    /// B-1022: <c>owesDuty</c> on the child's day view follows the settlement - a daily duty owes on a plain Monday, not on
    /// a Sunday and not on a paused day; a weekly duty owes in a plain week, not in a week touched by a pause. Red when the
    /// field only reads the cadence (the false rows turn true), or when it ignores the week rule (the weekly pause row).
    /// </summary>
    [Theory]
    [InlineData(GoalCadence.Daily, null, false, true)]
    [InlineData(GoalCadence.Daily, 0, false, false)]
    [InlineData(GoalCadence.Daily, null, true, false)]
    [InlineData(GoalCadence.Weekly, null, false, true)]
    [InlineData(GoalCadence.Weekly, 2, false, false)]
    public async Task Heute_Sagt_Ob_Die_Pflicht_Etwas_Kostet(GoalCadence cadence, int? pauseDaysAfterMonday, bool onSunday,
        bool expected)
    {
        var s = await SetupAsync(cadence, Thursday, [], penalty: 5);
        if (pauseDaysAfterMonday is { } offset) await PauseAsync(s, Today.AddDays(offset), Today.AddDays(offset));
        if (onSunday) factory.Clock.FreezeAt(new DateTimeOffset(2026, 11, 29, 12, 0, 0, TimeSpan.Zero));

        var today = await s.Father.GetFromJsonAsync<JsonElement>($"/api/v1/student/children/{s.ChildId}/today");
        var position = today.GetProperty("positions").EnumerateArray().Single(p => p.GetProperty("positionId").GetInt32() == s.PositionId);

        Assert.Equal(5, position.GetProperty("penaltyCoins").GetInt32());
        Assert.Equal(expected, position.GetProperty("owesDuty").GetBoolean());
    }

    // ---- Shared readers of the duty days ----

    /// <summary>
    /// The supervisor's timeline: a week met Monday to Saturday reads as done although its Sunday stayed open - and so does
    /// one whose Saturday was paused instead of met. Red when the timeline reads the open Sunday (both rows false) or the
    /// paused Saturday (second row false) as a missed duty.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Zeitachse_Liest_Sonntag_Und_Pause_Nicht_Als_Versaeumt(bool saturdayPaused)
    {
        var monday = Today.AddDays(-7);
        var met = Enumerable.Range(0, saturdayPaused ? 5 : 6).Select(i => monday.AddDays(i));
        var s = await SetupAsync(GoalCadence.Daily, monday, met);
        if (saturdayPaused) await PauseAsync(s, Saturday, Saturday);

        var overview = await s.Father.GetFromJsonAsync<JsonElement>(
            $"/api/v1/supervisor/children/overview?from={monday:yyyy-MM-dd}&to={Sunday:yyyy-MM-dd}&granularity=Week&childId={s.ChildId}");
        var week = overview.GetProperty("children")[0].GetProperty("periods")[0];

        Assert.True(week.GetProperty("dutyDone").GetBoolean());
    }

    /// <summary>
    /// The streak mission metric reads the same rule as the child's streak: Saturday and Monday met, Sunday empty - 2.
    /// Red when the metric's own walk breaks on Sunday (1).
    /// </summary>
    [Fact]
    public async Task Serien_Mission_Liest_Dieselben_Lerntage()
    {
        var s = await SetupAsync(GoalCadence.Daily, Thursday, [Saturday, Today]);

        using var scope = factory.Services.CreateScope();
        var metrics = scope.ServiceProvider.GetRequiredService<MetricsService>();
        Assert.Equal(2, await metrics.ValueAsync(s.ChildId, ProgressMetric.StreakDays, null, null, Today, CancellationToken.None));
    }

    // ---- AK 5: the pause endpoints ----

    /// <summary>
    /// AK 5: the pauses take the supervisor claim and ownership - a creator-only account gets 403, a foreign father 404 -
    /// and refuse a pause ending before it starts with 400 validation_error; the owner lists and deletes. Red when a gate or
    /// the check is missing.
    /// </summary>
    [Fact]
    public async Task Pausen_Endpunkte_Pruefen_Rolle_Eigentum_Und_Zeitraum()
    {
        var s = await SetupAsync(GoalCadence.Daily, Thursday, []);
        var url = $"/api/v1/supervisor/children/{s.ChildId}/pauses";
        var body = new { from = Friday, until = Saturday, note = (string?)null };

        var teacher = await TestApi.AdultAsync(factory, id: 2, pin: "9999");
        Assert.Equal(HttpStatusCode.Forbidden, (await teacher.PostAsJsonAsync(url, body)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await teacher.GetAsync(url)).StatusCode);
        var stranger = await TestApi.AdultAsync(factory, id: 3, pin: "0001");
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PostAsJsonAsync(url, body)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync(url)).StatusCode);

        var reversed = await s.Father.PostAsJsonAsync(url, new { from = Saturday, until = Friday });
        Assert.Equal(HttpStatusCode.BadRequest, reversed.StatusCode);
        Assert.Equal("validation_error", (await reversed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        var created = await s.Father.PostAsJsonAsync(url, body);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var list = await s.Father.GetFromJsonAsync<JsonElement>(url);
        Assert.Equal((Friday, Saturday), (DateOnly.Parse(list[0].GetProperty("from").GetString()!),
            DateOnly.Parse(list[0].GetProperty("until").GetString()!)));
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.DeleteAsync($"{url}/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await s.Father.DeleteAsync($"{url}/{id}")).StatusCode);
        Assert.Equal(0, (await s.Father.GetFromJsonAsync<JsonElement>(url)).GetArrayLength());
    }

    // ---- AK 6: the father's limits ----

    /// <summary>
    /// AK 6: the five limits are stored and read back; floor 0, floor above ceiling (also against a stored ceiling) and a
    /// negative penalty are 400 and change nothing; clearing removes all five. Red when one of these values is stored.
    /// </summary>
    [Fact]
    public async Task Leitplanken_Werden_Gespeichert_Und_Geprueft()
    {
        var s = await SetupAsync(GoalCadence.Daily, Thursday, []);
        var url = $"/api/v1/supervisor/children/{s.ChildId}";

        var saved = await s.Father.PatchAsJsonAsync(url, new
        {
            dailyTasksMin = 2,
            dailyTasksMax = 5,
            dailyNewWordsMin = 3,
            dailyNewWordsMax = 8,
            penaltyCoinsPerMissedTask = 4,
        });
        Assert.True(saved.IsSuccessStatusCode, await saved.Content.ReadAsStringAsync());
        var child = await s.Father.GetFromJsonAsync<JsonElement>(url);
        Assert.Equal((2, 5, 3, 8, 4), (child.GetProperty("dailyTasksMin").GetInt32(), child.GetProperty("dailyTasksMax").GetInt32(),
            child.GetProperty("dailyNewWordsMin").GetInt32(), child.GetProperty("dailyNewWordsMax").GetInt32(),
            child.GetProperty("penaltyCoinsPerMissedTask").GetInt32()));

        foreach (var bad in new object[]
        {
            new { dailyTasksMin = 0 },
            new { dailyNewWordsMin = 0 },
            new { dailyTasksMin = 6 },
            new { dailyNewWordsMax = 2 },
            new { penaltyCoinsPerMissedTask = -1 },
        })
        {
            var res = await s.Father.PatchAsJsonAsync(url, bad);
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
            Assert.Equal("validation_error", (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        }
        var unchanged = await s.Father.GetFromJsonAsync<JsonElement>(url);
        Assert.Equal((2, 5, 3, 8, 4), (unchanged.GetProperty("dailyTasksMin").GetInt32(), unchanged.GetProperty("dailyTasksMax").GetInt32(),
            unchanged.GetProperty("dailyNewWordsMin").GetInt32(), unchanged.GetProperty("dailyNewWordsMax").GetInt32(),
            unchanged.GetProperty("penaltyCoinsPerMissedTask").GetInt32()));

        var cleared = await s.Father.PatchAsJsonAsync(url, new { clearPlannerLimits = true });
        Assert.True(cleared.IsSuccessStatusCode);
        var empty = await cleared.Content.ReadFromJsonAsync<JsonElement>();
        foreach (var field in new[] { "dailyTasksMin", "dailyTasksMax", "dailyNewWordsMin", "dailyNewWordsMax", "penaltyCoinsPerMissedTask" })
            Assert.Equal(JsonValueKind.Null, empty.GetProperty(field).ValueKind);
    }

    // ---- AK 7: the migration ----

    /// <summary>
    /// AK 7: the migration leaves an existing child's values as they were and its five limits empty; the pause table comes
    /// up empty. Taken back one step and forward again. Red when the migration fills a limit or touches a child's column.
    /// </summary>
    [Fact]
    public async Task Migration_Laesst_Bestandskinder_Unveraendert()
    {
        const string before = "20261125213822_AnswerErrorKind";
        var target = Read(db => db.Database.GetMigrations().Last());
        var dbPath = Path.Combine(Path.GetTempPath(), $"pugling-lerntage-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";
        try
        {
            var options = new DbContextOptionsBuilder<PuglingDbContext>().UseSqlite(connectionString).Options;
            int childId;
            await using (var db = new PuglingDbContext(options))
            {
                await db.Database.MigrateAsync();
                var child = new Child { Name = "Bestand", BonusDailyCoinCap = 7, HomeworkCoins = 3, DailyTasksMin = 2 };
                db.Children.Add(child);
                await db.SaveChangesAsync();
                childId = child.Id;

                var migrator = db.GetService<IMigrator>();
                await migrator.MigrateAsync(before);
                await migrator.MigrateAsync(target);
            }

            await using (var db = new PuglingDbContext(options))
            {
                var child = await db.Children.AsNoTracking().SingleAsync(c => c.Id == childId);
                Assert.Equal(("Bestand", (int?)7, (int?)3), (child.Name, child.BonusDailyCoinCap, child.HomeworkCoins));
                Assert.Equal(new int?[] { null, null, null, null, null },
                    [child.DailyTasksMin, child.DailyTasksMax, child.DailyNewWordsMin, child.DailyNewWordsMax, child.PenaltyCoinsPerMissedTask]);
                Assert.Equal(0, await db.ChildPauses.CountAsync());
            }
        }
        finally
        {
            using (var own = new SqliteConnection(connectionString)) SqliteConnection.ClearPool(own);
            File.Delete(dbPath);
        }
    }
}
