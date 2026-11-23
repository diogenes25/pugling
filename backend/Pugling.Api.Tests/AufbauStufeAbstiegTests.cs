using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-702: the second test in a row below 50 % lowers a position by one level on its build path, at once (the user's
/// choice), and books nothing. A test from 50 % on, or a passed one, resets the count; the father's tests do not count, nor
/// do tests of a level the position no longer plays. German time, hence the Berlin host.
/// </summary>
public class AufbauStufeAbstiegTests(BerlinWebAppFactory factory) : IClassFixture<BerlinWebAppFactory>
{
    private static DateTimeOffset Local(int day, int hour, int minute = 0) =>
        new DateTimeOffset(2027, 7, day, hour, minute, 0, TimeSpan.FromHours(2)).ToUniversalTime();

    private static readonly DateOnly Day15 = new(2027, 7, 15);
    private static readonly DateOnly Day16 = new(2027, 7, 16);

    private static string Unit(int seriesId, int unitId) =>
        $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/arithmetic";

    private static object Arithmetic(string title, (string Prompt, int Answer)[] problems, int? level = null, int? target = null) => new
    {
        title,
        orderIndex = 1,
        rewardPoints = 10,
        config = new { problems = problems.Select(p => new { prompt = p.Prompt, answer = p.Answer, tolerance = 0 }).ToArray() },
        buildLevel = level,
        buildTargetExerciseId = target,
    };

    private sealed record Path(HttpClient Father, HttpClient Child, int ChildId, int PlanId, int PositionId);

    // Three levels, two problems on level 2 and three on the target, so a test can score 0, 33, 50 or 100 %:
    // level 1 "1 + 1"; level 2 "2 + 2", "3 + 3"; target "2 + 3", "4 + 4", "5 + 5".
    private async Task<Path> PathAsync(int startLevel, int? threshold = null)
    {
        var father = await TestApi.AdultAsync(factory);
        const string pin = "7021";
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Abstieg-Kind"), pin }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, await TestApi.DefaultSubjectIdAsync(father));
        var targetId = await TestApi.IdAsync(await father.PostAsJsonAsync(Unit(seriesId, unitId),
            Arithmetic("Ziel", [("2 + 3", 5), ("4 + 4", 8), ("5 + 5", 10)])));
        await TestApi.IdAsync(await father.PostAsJsonAsync(Unit(seriesId, unitId),
            Arithmetic("Stufe 1", [("1 + 1", 2)], 1, targetId)));
        await TestApi.IdAsync(await father.PostAsJsonAsync(Unit(seriesId, unitId),
            Arithmetic("Stufe 2", [("2 + 2", 4), ("3 + 3", 6)], 2, targetId)));
        var planId = await TestApi.CreateEmptyPlanAsync(father, childId);
        var positionId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId = targetId, currentBuildLevel = startLevel, cadence = "None", goalThreshold = threshold }));
        return new Path(father, await TestApi.ChildAsync(factory, childId, pin), childId, planId, positionId);
    }

    private static string Tests(Path p) => $"/api/v1/student/study-plans/{p.PlanId}/positions/{p.PositionId}/tests";

    private static async Task<int> StartAsync(HttpClient client, Path p)
    {
        var started = await client.PostAsJsonAsync(Tests(p), new { });
        Assert.True(started.IsSuccessStatusCode, await started.Content.ReadAsStringAsync());
        return await TestApi.IdWithKeyAsync(started, "attemptId");
    }

    // One test, answered in bulk: one answer per problem in order ("0" is wrong everywhere).
    private static async Task<JsonElement> TestAsync(HttpClient client, Path p, params string[] answers)
    {
        var attemptId = await StartAsync(client, p);
        var submit = await client.PostAsJsonAsync($"{Tests(p)}/{attemptId}/submit",
            new { answers = answers.Select((a, i) => new { itemIndex = i, givenAnswer = a, wasKnown = (bool?)null }).ToArray() });
        Assert.True(submit.IsSuccessStatusCode, await submit.Content.ReadAsStringAsync());
        return await submit.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<int> ScoreAsync(HttpClient client, Path p, params string[] answers) =>
        (await TestAsync(client, p, answers)).GetProperty("scorePercent").GetInt32();

    private (int? Level, DateOnly? From, int? Failures, DateOnly? Since) StateOf(int positionId)
    {
        using var scope = factory.Services.CreateScope();
        var pos = scope.ServiceProvider.GetRequiredService<PuglingDbContext>().PlanPositions.AsNoTracking()
            .Single(x => x.Id == positionId);
        return (pos.CurrentBuildLevel, pos.BuildLevelEffectiveOn, pos.FailedBuildTestsInRow, pos.CurrentBuildLevelSince);
    }

    private async Task<string> PlayedPromptAsync(Path p)
    {
        var started = await p.Child.PostAsJsonAsync(TestApi.PracticeBase(p.PlanId, p.PositionId), new { mode = "Info" });
        Assert.True(started.IsSuccessStatusCode, await started.Content.ReadAsStringAsync());
        var sessionId = await TestApi.IdAsync(started);
        var next = await p.Child.GetFromJsonAsync<JsonElement>(
            $"{TestApi.PracticeBase(p.PlanId, p.PositionId)}/{sessionId}/next");
        return next.GetProperty("card").GetProperty("prompt").GetString()!;
    }

    /// <summary>
    /// AC 1: the second test in a row below 50 % lowers level 2 to level 1, played at once; the first one only counts. Red:
    /// no descent (stays 2), a descent after one failure (the first step already 1), or one that waits for the next day
    /// (<c>BuildLevelEffectiveOn</c> set, today still "2 + 2").
    /// </summary>
    [Fact]
    public async Task Zweiter_Test_Unter_50_Senkt_Die_Stufe_Sofort()
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var p = await PathAsync(startLevel: 2);
        Assert.Equal(0, await ScoreAsync(p.Child, p, "0", "0"));
        Assert.Equal((2, (DateOnly?)null, 1), (StateOf(p.PositionId).Level, StateOf(p.PositionId).From, StateOf(p.PositionId).Failures));

        Assert.Equal(0, await ScoreAsync(p.Child, p, "0", "0"));
        Assert.Equal((1, (DateOnly?)null, 0, Day15), StateOf(p.PositionId));
        Assert.Equal("1 + 1", await PlayedPromptAsync(p));
    }

    /// <summary>
    /// AC 1: a test at 50 % that misses the bar of 80 % does not count and resets the count. Red: counting "not passed"
    /// instead of "below 50 %" (the 50 % test is the second failure and lowers the level), or a count that never resets
    /// (the failure on the next day is the second and lowers it).
    /// </summary>
    [Fact]
    public async Task Test_Ab_50_Setzt_Die_Zaehlung_Zurueck()
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var p = await PathAsync(startLevel: 2, threshold: 80);
        Assert.Equal(0, await ScoreAsync(p.Child, p, "0", "0"));
        Assert.Equal(50, await ScoreAsync(p.Child, p, "4", "0"));
        Assert.Equal((2, 0), (StateOf(p.PositionId).Level, StateOf(p.PositionId).Failures));

        factory.Clock.FreezeAt(Local(16, 10));
        Assert.Equal(0, await ScoreAsync(p.Child, p, "0", "0"));
        Assert.Equal((2, 1), (StateOf(p.PositionId).Level, StateOf(p.PositionId).Failures));
    }

    /// <summary>
    /// A passed test resets the count even below 50 % (a bar of 30 %, one of three right on the target, where no promotion
    /// takes over the reset). Red: a reset only from 50 % on (the passed 33 % would be the second failure and lower the
    /// target to level 2).
    /// </summary>
    [Fact]
    public async Task Bestandener_Test_Unter_50_Setzt_Zurueck()
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var p = await PathAsync(startLevel: Exercise.BuildLevelTarget, threshold: 30);
        Assert.Equal(0, await ScoreAsync(p.Child, p, "0", "0", "0"));
        var passed = await TestAsync(p.Child, p, "5", "0", "0");
        Assert.Equal((33, true), (passed.GetProperty("scorePercent").GetInt32(), passed.GetProperty("passed").GetBoolean()));
        Assert.Equal((Exercise.BuildLevelTarget, 0), (StateOf(p.PositionId).Level, StateOf(p.PositionId).Failures));
    }

    /// <summary>
    /// AC 2: the father's tests count neither way. Red: a count without the <c>BySupervisor</c> filter (his two failures
    /// lower the level).
    /// </summary>
    [Fact]
    public async Task Tests_Des_Vaters_Zaehlen_Nicht()
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var p = await PathAsync(startLevel: 2);
        Assert.Equal(0, await ScoreAsync(p.Father, p, "0", "0"));
        Assert.Equal(0, await ScoreAsync(p.Father, p, "0", "0"));
        Assert.Equal((2, 0), (StateOf(p.PositionId).Level, StateOf(p.PositionId).Failures));
    }

    /// <summary>
    /// AC 3: the lowest level is the floor. Red: a descent without the floor (it would leave the path or throw).
    /// </summary>
    [Fact]
    public async Task Auf_Stufe_1_Sinkt_Nichts()
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var p = await PathAsync(startLevel: 1);
        Assert.Equal(0, await ScoreAsync(p.Child, p, "0"));
        Assert.Equal(0, await ScoreAsync(p.Child, p, "0"));
        Assert.Equal(1, StateOf(p.PositionId).Level);
        Assert.Equal("1 + 1", await PlayedPromptAsync(p));
    }

    /// <summary>
    /// AC 4: the descent books nothing - no points, no penalty. A statement of intent: the descent has no booking code a
    /// mutant could remove; this turns red only when someone adds one (a ledger entry or a <c>PositionGoalPenalty</c>).
    /// </summary>
    [Fact]
    public async Task Abstieg_Bucht_Nichts()
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var p = await PathAsync(startLevel: 2);
        (int Entries, int Penalties) Rows()
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            return (db.ChildPointsEntries.Count(e => e.ChildId == p.ChildId),
                db.PositionGoalPenalties.Count(x => x.PlanPositionId == p.PositionId));
        }
        var before = Rows();
        await ScoreAsync(p.Child, p, "0", "0");
        await ScoreAsync(p.Child, p, "0", "0");
        Assert.Equal(1, StateOf(p.PositionId).Level);
        Assert.Equal(before, Rows());
    }

    /// <summary>
    /// AC 5 (counter-check): a missed period of a staged position still costs its <c>PenaltyCoins</c>, once - the descent
    /// on that day exempts nothing. A staged position takes no daily duty through the API (B-848), so it is set in the
    /// database. Red: staged positions excluded from the settlement, or the descent waiving the period.
    /// </summary>
    [Fact]
    public async Task Verpasster_Tag_Kostet_Trotz_Abstieg()
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var p = await PathAsync(startLevel: 2);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            var pos = db.PlanPositions.Single(x => x.Id == p.PositionId);
            pos.Cadence = GoalCadence.Daily;
            pos.PenaltyCoins = 5;
            db.SaveChanges();
        }
        await ScoreAsync(p.Child, p, "0", "0");
        await ScoreAsync(p.Child, p, "0", "0");
        Assert.Equal(1, StateOf(p.PositionId).Level);

        factory.Clock.FreezeAt(Local(16, 10));
        await TestApi.ChildAsync(factory, p.ChildId, "7021");
        await TestApi.ChildAsync(factory, p.ChildId, "7021");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            var penalties = db.ChildPointsEntries.Where(e => e.ChildId == p.ChildId && e.Kind == PointKind.GoalPenalty)
                .Select(e => e.Amount).ToList();
            Assert.Equal(-5, Assert.Single(penalties));
        }
    }

    /// <summary>
    /// A test of a level the position no longer plays counts neither way: on the day a promotion waits, the child's
    /// second test still examines level 1, and its failure must not carry into level 2. Red: a count without the check
    /// against the current level (one failure on level 2 the next day would then be the second and lower it).
    /// </summary>
    [Fact]
    public async Task Test_Einer_Alten_Stufe_Zaehlt_Nicht()
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var p = await PathAsync(startLevel: 1);
        Assert.True((await TestAsync(p.Child, p, "2")).GetProperty("passed").GetBoolean());
        Assert.Equal((2, (DateOnly?)Day16), (StateOf(p.PositionId).Level, StateOf(p.PositionId).From));
        Assert.Equal(0, await ScoreAsync(p.Child, p, "0"));
        Assert.Equal(0, StateOf(p.PositionId).Failures);

        factory.Clock.FreezeAt(Local(16, 10));
        Assert.Equal(0, await ScoreAsync(p.Child, p, "0", "0"));
        Assert.Equal((2, 1), (StateOf(p.PositionId).Level, StateOf(p.PositionId).Failures));
    }

    /// <summary>
    /// A promotion starts the count over as well: one failure on level 1 and one on level 2 the next day do not make two.
    /// Red: a promotion that sets level and day without <c>SetBuildLevel</c> (the failure from level 1 carries over and the
    /// first failure on level 2 lowers it again).
    /// </summary>
    [Fact]
    public async Task Aufstieg_Setzt_Die_Zaehlung_Zurueck()
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var p = await PathAsync(startLevel: 1);
        Assert.Equal(0, await ScoreAsync(p.Child, p, "0"));
        Assert.True((await TestAsync(p.Child, p, "2")).GetProperty("passed").GetBoolean());
        Assert.Equal((2, 0), (StateOf(p.PositionId).Level, StateOf(p.PositionId).Failures));

        factory.Clock.FreezeAt(Local(16, 10));
        Assert.Equal(0, await ScoreAsync(p.Child, p, "0", "0"));
        Assert.Equal((2, 1), (StateOf(p.PositionId).Level, StateOf(p.PositionId).Failures));
    }

    /// <summary>
    /// The father's level change starts the count over: one failure before it and one after do not make two. Red: a level
    /// change that keeps <c>FailedBuildTestsInRow</c>.
    /// </summary>
    [Fact]
    public async Task Stufe_Des_Vaters_Setzt_Die_Zaehlung_Zurueck()
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var p = await PathAsync(startLevel: Exercise.BuildLevelTarget);
        Assert.Equal(0, await ScoreAsync(p.Child, p, "0", "0", "0"));
        var patch = await p.Father.PatchAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{p.PlanId}/positions/{p.PositionId}", new { currentBuildLevel = 2 });
        Assert.True(patch.IsSuccessStatusCode, await patch.Content.ReadAsStringAsync());
        Assert.Equal((2, 0), (StateOf(p.PositionId).Level, StateOf(p.PositionId).Failures));
        Assert.Equal(0, await ScoreAsync(p.Child, p, "0", "0"));
        Assert.Equal((2, 1), (StateOf(p.PositionId).Level, StateOf(p.PositionId).Failures));
    }

    /// <summary>
    /// A self-assessed test under "typed tests only" counts as little as it could meet the goal - the same admission as the
    /// promotion. Red: a count without <c>MovesBuildPath</c>'s graded check (tapping "not known" twice would lower the
    /// level).
    /// </summary>
    [Fact]
    public async Task Selbsteinschaetzung_Zaehlt_Nicht_Wenn_Getippt_Verlangt_Ist()
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var father = await TestApi.AdultAsync(factory);
        const string pin = "7022";
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Abstieg-Kind"), pin }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, await TestApi.DefaultSubjectIdAsync(father));
        // The target is typed (a self-assessed target cannot take "typed tests only"); level 2 assesses itself, as in
        // the promotion's case.
        var targetId = await TestApi.IdAsync(await father.PostAsJsonAsync(Unit(seriesId, unitId), Arithmetic("Ziel", [("2 + 3", 5)])));
        await TestApi.IdAsync(await father.PostAsJsonAsync(Unit(seriesId, unitId), Arithmetic("Stufe 1", [("1 + 1", 2)], 1, targetId)));
        await TestApi.IdAsync(await father.PostAsJsonAsync($"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/vocabulary",
            new
            {
                title = "Stufe 2",
                orderIndex = 2,
                rewardPoints = 10,
                defaultStage = (int)TestStage.SelfAssess,
                config = new { direction = "front-to-back", sourceLang = "en", targetLang = "de", items = new[] { new { front = "cat", back = "Katze" } } },
                buildLevel = 2,
                buildTargetExerciseId = targetId,
            }));
        var planId = await TestApi.CreateEmptyPlanAsync(father, childId);
        var created = await father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId = targetId, currentBuildLevel = 2, cadence = "None", requireTypedTest = true });
        Assert.True(created.IsSuccessStatusCode, await created.Content.ReadAsStringAsync());
        var positionId = await TestApi.IdAsync(created);
        var p = new Path(father, await TestApi.ChildAsync(factory, childId, pin), childId, planId, positionId);

        for (var i = 0; i < 2; i++)
        {
            var attemptId = await StartAsync(p.Child, p);
            var submit = await p.Child.PostAsJsonAsync($"{Tests(p)}/{attemptId}/submit",
                new { answers = new[] { new { itemIndex = 0, givenAnswer = (string?)null, wasKnown = (bool?)false } } });
            Assert.True(submit.IsSuccessStatusCode, await submit.Content.ReadAsStringAsync());
        }
        using (var scope = factory.Services.CreateScope())
            Assert.All(scope.ServiceProvider.GetRequiredService<PuglingDbContext>().TestAttempts
                .Where(t => t.PlanPositionId == positionId), t => Assert.False(t.Graded));
        Assert.Equal((2, 0), (StateOf(positionId).Level, StateOf(positionId).Failures ?? 0));
    }

    /// <summary>
    /// AC 9: a submit that loses the race for the same attempt counts nothing. A parallel winner is played by an interceptor:
    /// right before the loser writes the attempt, the attempt is completed on the loser's own connection - the state the
    /// winner's commit leaves. A parallel pair of real requests does not reach that window on the test server (measured:
    /// the second one always met the early check). Red: a completion without the conditional claim (the loser answers 200
    /// and counts the attempt towards a descent).
    /// </summary>
    [Fact]
    public async Task Verlierer_Einer_Doppelten_Abgabe_Zaehlt_Nichts()
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var p = await PathAsync(startLevel: 2);
        var attemptId = await StartAsync(p.Child, p);
        var winner = new CompletingWinner(attemptId);
        using var host = factory.WithWebHostBuilder(b =>
            b.ConfigureServices(s => s.ConfigureDbContext<PuglingDbContext>(o => o.AddInterceptors(winner))));
        var loser = host.CreateClient();
        loser.DefaultRequestHeaders.Authorization = p.Child.DefaultRequestHeaders.Authorization;

        var submit = await loser.PostAsJsonAsync($"{Tests(p)}/{attemptId}/submit",
            new { answers = new[] { new { itemIndex = 0, givenAnswer = "0", wasKnown = (bool?)null } } });
        Assert.True(winner.Fired, "the winner must have completed the attempt inside the loser's request");
        Assert.Equal(HttpStatusCode.BadRequest, submit.StatusCode);
        Assert.Contains("test_already_submitted", await submit.Content.ReadAsStringAsync());
        Assert.Equal((2, 0), (StateOf(p.PositionId).Level, StateOf(p.PositionId).Failures ?? 0));
    }

    // Completes the attempt on the request's own connection and transaction right before its first write to TestAttempts -
    // what a parallel winner's commit would leave. Its own connection would wait on the request's write lock.
    private sealed class CompletingWinner(int attemptId) : DbCommandInterceptor
    {
        public bool Fired { get; private set; }

        private async Task CompleteFirstAsync(DbCommand command, CancellationToken ct)
        {
            if (Fired || !command.CommandText.Contains("UPDATE \"TestAttempts\"", StringComparison.Ordinal)) return;
            Fired = true;
            await using var win = command.Connection!.CreateCommand();
            win.Transaction = command.Transaction;
            win.CommandText = $"UPDATE TestAttempts SET CompletedAt = '2027-07-15 08:00:00' WHERE Id = {attemptId}";
            await win.ExecuteNonQueryAsync(ct);
        }

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            await CompleteFirstAsync(command, cancellationToken);
            return result;
        }

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            await CompleteFirstAsync(command, cancellationToken);
            return result;
        }
    }

    /// <summary>
    /// Decision 3: the father reads since when the child plays its level - the creation day, the day of a descent, and none
    /// while a promotion waits (the child still plays the level below, whose start is no longer kept). Red: a level change
    /// that leaves <c>CurrentBuildLevelSince</c>, or a response that shows the waiting level's day.
    /// </summary>
    [Fact]
    public async Task Vater_Sieht_Seit_Wann_Die_Stufe_Gilt()
    {
        factory.Clock.FreezeAt(Local(14, 10));
        var p = await PathAsync(startLevel: 2);
        async Task<string?> SinceAsync()
        {
            var pos = await p.Father.GetFromJsonAsync<JsonElement>(
                $"/api/v1/supervisor/study-plans/{p.PlanId}/positions/{p.PositionId}");
            var since = pos.GetProperty("build").GetProperty("since");
            return since.ValueKind == JsonValueKind.Null ? null : since.GetString();
        }
        Assert.Equal("2027-07-14", await SinceAsync());

        factory.Clock.FreezeAt(Local(15, 10));
        await ScoreAsync(p.Child, p, "0", "0");
        await ScoreAsync(p.Child, p, "0", "0");
        Assert.Equal("2027-07-15", await SinceAsync());

        factory.Clock.FreezeAt(Local(16, 10));
        Assert.True((await TestAsync(p.Child, p, "2")).GetProperty("passed").GetBoolean());
        Assert.Null(await SinceAsync());
        factory.Clock.FreezeAt(Local(17, 10));
        Assert.Equal("2027-07-17", await SinceAsync());
    }
}
