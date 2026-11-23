using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-849: a passed test of the level a position plays raises it by one on its build path, at once in the data and played
/// from the next learning day (German time, hence the Berlin host). The attempt carries the level it examined; only a test
/// of the target meets the goal.
/// </summary>
public class AufbauStufeAufstiegTests(BerlinWebAppFactory factory) : IClassFixture<BerlinWebAppFactory>
{
    // 15 July 2027 is summer time (UTC+2): 23:30 local is 21:30Z, 00:30 local on the 16th is 22:30Z on the 15th.
    private static DateTimeOffset Local(int day, int hour, int minute = 0) =>
        new DateTimeOffset(2027, 7, day, hour, minute, 0, TimeSpan.FromHours(2)).ToUniversalTime();

    private static readonly DateOnly Day15 = new(2027, 7, 15);

    private static string Unit(int seriesId, int unitId) =>
        $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/arithmetic";

    private static object Arithmetic(string title, string prompt, int answer, int? level = null, int? target = null) => new
    {
        title,
        orderIndex = 1,
        rewardPoints = 10,
        config = new { problems = new[] { new { prompt, answer, tolerance = 0 } } },
        buildLevel = level,
        buildTargetExerciseId = target,
    };

    private sealed record Path(HttpClient Father, HttpClient Child, int ChildId, int PlanId, int PositionId);

    // A fresh child with a plan and one position on a three-level path: "1 + 1" (level 1), "2 + 2" (level 2), "2 + 3" (the
    // target). Arithmetic throughout, so every level has an answer known to the test.
    private async Task<Path> PathAsync(int startLevel, bool secondLevel = true)
    {
        var father = await TestApi.AdultAsync(factory);
        const string pin = "8491";
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Stufen-Kind"), pin }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, await TestApi.DefaultSubjectIdAsync(father));
        var targetId = await TestApi.IdAsync(await father.PostAsJsonAsync(Unit(seriesId, unitId),
            Arithmetic("Ziel", "2 + 3", 5)));
        await TestApi.IdAsync(await father.PostAsJsonAsync(Unit(seriesId, unitId),
            Arithmetic("Stufe 1", "1 + 1", 2, 1, targetId)));
        if (secondLevel)
            await TestApi.IdAsync(await father.PostAsJsonAsync(Unit(seriesId, unitId),
                Arithmetic("Stufe 2", "2 + 2", 4, 2, targetId)));
        var planId = await TestApi.CreateEmptyPlanAsync(father, childId);
        var positionId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId = targetId, currentBuildLevel = startLevel, cadence = "None", useLeitner = true }));
        return new Path(father, await TestApi.ChildAsync(factory, childId, pin), childId, planId, positionId);
    }

    private static string Tests(Path p) => $"/api/v1/student/study-plans/{p.PlanId}/positions/{p.PositionId}/tests";

    // One test from start to submit, answering its single question; returns the submit result.
    private static async Task<JsonElement> TestAsync(HttpClient client, Path p, string answer)
    {
        var started = await client.PostAsJsonAsync(Tests(p), new { });
        Assert.True(started.IsSuccessStatusCode, await started.Content.ReadAsStringAsync());
        var attemptId = await TestApi.IdWithKeyAsync(started, "attemptId");
        (await client.GetAsync($"{Tests(p)}/{attemptId}/next")).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"{Tests(p)}/{attemptId}/answer", new { itemIndex = 0, givenAnswer = answer }))
            .EnsureSuccessStatusCode();
        var submit = await client.PostAsJsonAsync($"{Tests(p)}/{attemptId}/submit", new { });
        Assert.True(submit.IsSuccessStatusCode, await submit.Content.ReadAsStringAsync());
        return await submit.Content.ReadFromJsonAsync<JsonElement>();
    }

    // The prompt the child is served now: a free round (no due filter) and its first card.
    private static async Task<string> PlayedPromptAsync(Path p)
    {
        var started = await p.Child.PostAsJsonAsync(TestApi.PracticeBase(p.PlanId, p.PositionId), new { mode = "Info" });
        Assert.True(started.IsSuccessStatusCode, await started.Content.ReadAsStringAsync());
        var sessionId = await TestApi.IdAsync(started);
        var next = await p.Child.GetFromJsonAsync<JsonElement>(
            $"{TestApi.PracticeBase(p.PlanId, p.PositionId)}/{sessionId}/next");
        return next.GetProperty("card").GetProperty("prompt").GetString()!;
    }

    private (int? Level, DateOnly? From) LevelOf(int positionId)
    {
        using var scope = factory.Services.CreateScope();
        var pos = scope.ServiceProvider.GetRequiredService<PuglingDbContext>().PlanPositions.AsNoTracking()
            .Single(x => x.Id == positionId);
        return (pos.CurrentBuildLevel, pos.BuildLevelEffectiveOn);
    }

    private List<TestAttempt> AttemptsOf(int positionId)
    {
        using var scope = factory.Services.CreateScope();
        return [.. scope.ServiceProvider.GetRequiredService<PuglingDbContext>().TestAttempts.AsNoTracking()
            .Where(t => t.PlanPositionId == positionId).OrderBy(t => t.Id)];
    }

    /// <summary>
    /// AC 1: a passed test on level 1 raises the position to 2, played from tomorrow; a failed one leaves it. Red: a
    /// promotion that does not check <c>Passed</c> (the failed case rises), or none at all (the passed case stays).
    /// </summary>
    [Fact]
    public async Task Bestandener_Test_Hebt_Die_Stufe_Nicht_Bestandener_Nicht()
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var failed = await PathAsync(startLevel: 1);
        Assert.False((await TestAsync(failed.Child, failed, "3")).GetProperty("passed").GetBoolean());
        Assert.Equal((1, (DateOnly?)null), LevelOf(failed.PositionId));

        var passed = await PathAsync(startLevel: 1);
        Assert.True((await TestAsync(passed.Child, passed, "2")).GetProperty("passed").GetBoolean());
        Assert.Equal((2, Day15.AddDays(1)), LevelOf(passed.PositionId));
    }

    /// <summary>
    /// AC 1, the path's next level: without a level 2 a passed level 1 raises to the target, not to a level with no
    /// exercise. Red: a promotion by <c>+ 1</c> instead of the next level on the path.
    /// </summary>
    [Fact]
    public async Task Aufstieg_Springt_Zur_Naechsten_Stufe_Des_Wegs()
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var p = await PathAsync(startLevel: 1, secondLevel: false);
        await TestAsync(p.Child, p, "2");
        Assert.Equal(Exercise.BuildLevelTarget, LevelOf(p.PositionId).Level);
    }

    /// <summary>AC 2: on the target nothing rises any more. Red: a missing cap (the target would look for a level above).</summary>
    [Fact]
    public async Task Auf_Der_Ziel_Stufe_Steigt_Nichts()
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var p = await PathAsync(startLevel: Exercise.BuildLevelTarget);
        Assert.True((await TestAsync(p.Child, p, "5")).GetProperty("passed").GetBoolean());
        Assert.Equal((Exercise.BuildLevelTarget, (DateOnly?)null), LevelOf(p.PositionId));
    }

    /// <summary>
    /// AC 3, passed at 23:30: the rest of the day plays level 1, from 00:30 the new level. Red: a played exercise that does
    /// not read <c>BuildLevelEffectiveOn</c> (23:45 already plays level 2).
    /// </summary>
    [Fact]
    public async Task Aufstieg_Um_2330_Gilt_Ab_Mitternacht()
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var p = await PathAsync(startLevel: 1);
        factory.Clock.FreezeAt(Local(15, 23, 30));
        await TestAsync(p.Child, p, "2");

        factory.Clock.FreezeAt(Local(15, 23, 45));
        Assert.Equal("1 + 1", await PlayedPromptAsync(p));
        factory.Clock.FreezeAt(Local(16, 0, 30));
        Assert.Equal("2 + 2", await PlayedPromptAsync(p));
    }

    /// <summary>
    /// AC 3, passed at 00:30 local (still the 15th in UTC): the new level begins on the 17th, not on the 16th. Red: a
    /// promotion day taken from the UTC date (it would start the level at once, on the 16th).
    /// </summary>
    [Fact]
    public async Task Aufstieg_Um_0030_Gilt_Ab_Dem_Naechsten_Lerntag()
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var p = await PathAsync(startLevel: 1);
        factory.Clock.FreezeAt(Local(16, 0, 30));
        await TestAsync(p.Child, p, "2");
        Assert.Equal((2, new DateOnly(2027, 7, 17)), LevelOf(p.PositionId));

        factory.Clock.FreezeAt(Local(16, 10));
        Assert.Equal("1 + 1", await PlayedPromptAsync(p));
        factory.Clock.FreezeAt(Local(17, 0, 30));
        Assert.Equal("2 + 2", await PlayedPromptAsync(p));
    }

    /// <summary>
    /// A round runs on the day it began: a free round started at 23:50 on the day of the raise still serves level 1 after
    /// midnight. Red: a practice path that resolves the exercise for "now" instead of <c>session.Day</c>.
    /// </summary>
    [Fact]
    public async Task Runde_Ueber_Mitternacht_Bleibt_Auf_Ihrer_Stufe()
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var p = await PathAsync(startLevel: 1);
        await TestAsync(p.Child, p, "2");

        factory.Clock.FreezeAt(Local(15, 23, 50));
        var started = await p.Child.PostAsJsonAsync(TestApi.PracticeBase(p.PlanId, p.PositionId), new { mode = "Info" });
        var sessionId = await TestApi.IdAsync(started);
        factory.Clock.FreezeAt(Local(16, 0, 10));
        var next = await p.Child.GetFromJsonAsync<JsonElement>(
            $"{TestApi.PracticeBase(p.PlanId, p.PositionId)}/{sessionId}/next");
        Assert.Equal("1 + 1", next.GetProperty("card").GetProperty("prompt").GetString());
    }

    /// <summary>
    /// A test runs on the day it began: started at 23:50 on the day of the raise and answered after midnight, it is graded
    /// on level 1. Red: a test path that resolves the exercise for "now" instead of <c>attempt.Day</c> (the answer "2" would
    /// be graded against "2 + 2").
    /// </summary>
    [Fact]
    public async Task Test_Ueber_Mitternacht_Wird_Auf_Seiner_Stufe_Gewertet()
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var p = await PathAsync(startLevel: 1);
        await TestAsync(p.Child, p, "2");

        factory.Clock.FreezeAt(Local(15, 23, 50));
        var started = await p.Child.PostAsJsonAsync(Tests(p), new { });
        Assert.True(started.IsSuccessStatusCode, await started.Content.ReadAsStringAsync());
        var attemptId = await TestApi.IdWithKeyAsync(started, "attemptId");
        factory.Clock.FreezeAt(Local(16, 0, 10));
        var item = (await p.Child.GetFromJsonAsync<JsonElement>($"{Tests(p)}/{attemptId}/next")).GetProperty("item");
        Assert.Equal("1 + 1", item.GetProperty("prompt").GetString());
        (await p.Child.PostAsJsonAsync($"{Tests(p)}/{attemptId}/answer", new { itemIndex = 0, givenAnswer = "2" }))
            .EnsureSuccessStatusCode();
        var submit = await p.Child.PostAsJsonAsync($"{Tests(p)}/{attemptId}/submit", new { });
        Assert.True((await submit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("passed").GetBoolean());
    }

    /// <summary>
    /// AC 4: the attempt carries the level it examined - the level played that day, also after the raise - and an attempt
    /// on a free position none. Red: a stamp from the position's level (the second attempt would read 2) or a stamp on a
    /// free position.
    /// </summary>
    [Fact]
    public async Task Versuch_Traegt_Die_Gespielte_Stufe()
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var p = await PathAsync(startLevel: 1);
        await TestAsync(p.Child, p, "2");
        await TestAsync(p.Child, p, "3");
        Assert.Equal([1, 1], AttemptsOf(p.PositionId).Select(a => a.BuildLevel));

        var exerciseId = await TestApi.CreateVocabExerciseAsync(p.Father, "en", "de", [("dog", "Hund")]);
        var free = await TestApi.IdAsync(await p.Father.PostAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{p.PlanId}/positions", new { exerciseId, cadence = "None" }));
        var started = await p.Child.PostAsJsonAsync($"/api/v1/student/study-plans/{p.PlanId}/positions/{free}/tests", new { });
        started.EnsureSuccessStatusCode();
        Assert.Null(Assert.Single(AttemptsOf(free)).BuildLevel);
    }

    /// <summary>
    /// AC 5: the raise books nothing - no coins, no penalty. Red: a booking in the promotion. A position without a goal pays
    /// nothing for the test itself, so every new ledger row would come from the raise.
    /// </summary>
    [Fact]
    public async Task Aufstieg_Bucht_Nichts()
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var p = await PathAsync(startLevel: 1);
        int Rows()
        {
            using var scope = factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<PuglingDbContext>().ChildPointsEntries.Count(e => e.ChildId == p.ChildId);
        }
        var before = Rows();
        await TestAsync(p.Child, p, "2");
        Assert.Equal(2, LevelOf(p.PositionId).Level);
        Assert.Equal(before, Rows());
    }

    /// <summary>
    /// AC 6 (decision 4): a passed test of a prerequisite meets no goal, a passed test of the target does. A staged position
    /// takes no daily duty through the API (B-848), so the duty is set in the database - the rule lives in
    /// <c>IsGoalMetAsync</c>, which a duty "until done" never asks about tests (it is measured by remediation rounds). Red:
    /// a goal that ignores <c>TestAttempt.BuildLevel</c> (the prerequisite case meets it).
    /// </summary>
    [Theory]
    [InlineData(1, "2", false)]
    [InlineData(Exercise.BuildLevelTarget, "5", true)]
    public async Task Nur_Der_Test_Des_Ziels_Erfuellt_Das_Pflichtziel(int level, string answer, bool met)
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var p = await PathAsync(startLevel: level);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            db.PlanPositions.Single(x => x.Id == p.PositionId).Cadence = GoalCadence.Daily;
            db.SaveChanges();
        }
        Assert.True((await TestAsync(p.Child, p, answer)).GetProperty("passed").GetBoolean());

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            var pos = db.PlanPositions.Include(x => x.Exercise).ThenInclude(e => e!.Prerequisites).Single(x => x.Id == p.PositionId);
            var plan = db.StudyPlans.Single(x => x.Id == p.PlanId);
            var progress = scope.ServiceProvider.GetRequiredService<Pugling.Api.Services.Shared.PositionProgressService>();
            Assert.Equal(met, await progress.IsGoalMetAsync(pos, plan, Day15));
        }
    }

    /// <summary>
    /// AC 7: the raise is idempotent over the examined level - a second pass the same day (still on level 1) raises
    /// nothing more. Red: a promotion from the position's level instead of the attempt's (the second pass would reach 3).
    /// </summary>
    [Fact]
    public async Task Zweiter_Bestandener_Test_Am_Selben_Tag_Hebt_Nicht_Noch_Einmal()
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var p = await PathAsync(startLevel: 1);
        await TestAsync(p.Child, p, "2");
        Assert.True((await TestAsync(p.Child, p, "2")).GetProperty("passed").GetBoolean());
        Assert.Equal((2, Day15.AddDays(1)), LevelOf(p.PositionId));
    }

    /// <summary>
    /// AC 7, two completions of one attempt at once raise once. Intent more than proof: both requests load the position on
    /// level 1, so even a promotion from the position's level would land on 2 here; the sequential case above carries the
    /// red.
    /// </summary>
    [Fact]
    public async Task Parallele_Abgaben_Heben_Einmal()
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var p = await PathAsync(startLevel: 1);
        var started = await p.Child.PostAsJsonAsync(Tests(p), new { });
        var attemptId = await TestApi.IdWithKeyAsync(started, "attemptId");
        (await p.Child.PostAsJsonAsync($"{Tests(p)}/{attemptId}/answer", new { itemIndex = 0, givenAnswer = "2" }))
            .EnsureSuccessStatusCode();
        await Task.WhenAll(
            p.Child.PostAsJsonAsync($"{Tests(p)}/{attemptId}/submit", new { }),
            p.Child.PostAsJsonAsync($"{Tests(p)}/{attemptId}/submit", new { }));
        Assert.Equal(2, LevelOf(p.PositionId).Level);
    }

    /// <summary>
    /// A supervisor's passed preview raises nothing: it stays out of the child's path like out of its attempt cap. Red: a
    /// promotion without the <c>BySupervisor</c> check.
    /// </summary>
    [Fact]
    public async Task Vorschau_Des_Vaters_Hebt_Nicht()
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var p = await PathAsync(startLevel: 1);
        Assert.True((await TestAsync(p.Father, p, "2")).GetProperty("passed").GetBoolean());
        Assert.Equal((1, (DateOnly?)null), LevelOf(p.PositionId));
    }

    /// <summary>
    /// An attempt the father's level change has overtaken can no longer be answered: started on level 2, the father lowers
    /// the position to level 1 the next day - its questions are level 2's. Red: an answer or a submit without the check
    /// (graded against level 1, "2" passes and raises the position from the examined level 2 to the target).
    /// </summary>
    [Fact]
    public async Task Ueberholter_Versuch_Wird_Nicht_Mehr_Gewertet()
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var p = await PathAsync(startLevel: 2);
        var started = await p.Child.PostAsJsonAsync(Tests(p), new { });
        var attemptId = await TestApi.IdWithKeyAsync(started, "attemptId");

        factory.Clock.FreezeAt(Local(16, 10));
        var patch = await p.Father.PatchAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{p.PlanId}/positions/{p.PositionId}", new { currentBuildLevel = 1 });
        Assert.True(patch.IsSuccessStatusCode, await patch.Content.ReadAsStringAsync());

        var answer = await p.Child.PostAsJsonAsync($"{Tests(p)}/{attemptId}/answer", new { itemIndex = 0, givenAnswer = "2" });
        Assert.Equal(System.Net.HttpStatusCode.NotFound, answer.StatusCode);
        var submit = await p.Child.PostAsJsonAsync($"{Tests(p)}/{attemptId}/submit", new { });
        Assert.Equal(System.Net.HttpStatusCode.NotFound, submit.StatusCode);
        Assert.Equal(1, LevelOf(p.PositionId).Level);
    }

    /// <summary>
    /// B-881 AC 1 and 2: a round the father's level change has overtaken is no longer played - started yesterday on level
    /// 2, the father sets level 1 today. Every path that serves or grades its cards answers 404, and level 1's learning
    /// state stays untouched. Red: any one of the five paths without the check.
    /// </summary>
    [Fact]
    public async Task Ueberholte_Runde_Wird_Nicht_Mehr_Gespielt()
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var p = await PathAsync(startLevel: 2);
        var sessionId = await TestApi.StartPositionSessionAsync(p.Child, p.PlanId, p.PositionId);
        var round = $"{TestApi.PracticeBase(p.PlanId, p.PositionId)}/{sessionId}";

        factory.Clock.FreezeAt(Local(16, 10));
        var patch = await p.Father.PatchAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{p.PlanId}/positions/{p.PositionId}", new { currentBuildLevel = 1 });
        Assert.True(patch.IsSuccessStatusCode, await patch.Content.ReadAsStringAsync());

        // The round's own refusal, not just any 404: an arithmetic card has no vocabulary, so the image path answers 404
        // further down even without the check.
        static async Task Overtaken(HttpResponseMessage res, string path)
        {
            Assert.Equal(System.Net.HttpStatusCode.NotFound, res.StatusCode);
            var body = await res.Content.ReadAsStringAsync();
            Assert.True(body.Contains("no longer plays", StringComparison.Ordinal), $"{path}: {body}");
        }
        await Overtaken(await p.Child.GetAsync($"{round}/next"), "next");
        await Overtaken(await p.Child.GetAsync($"{round}/cards"), "cards");
        await Overtaken(await p.Child.PostAsJsonAsync($"{round}/easier", new { itemIndex = 0 }), "easier");
        await Overtaken(await p.Child.PostAsync($"{round}/cards/0/image/reshuffle", null), "reshuffle");
        await Overtaken(await TestApi.PositionReviewAsync(p.Child, p.PlanId, p.PositionId, sessionId, 0, givenAnswer: "2"),
            "review");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        Assert.Equal(0, db.PositionItemProgress.Count(x => x.PlanPositionId == p.PositionId && x.LastReviewedAt != null));
    }

    /// <summary>
    /// B-881, remediation: the open round of the day is continued (B-635) - but not one the father's level change has
    /// overtaken, or every start would hand back a round that answers 404 and lock the child out for the day. The round is
    /// ended first (as the app's cleanup does), so the father's lock does not hold. Red: continuing without the check (the
    /// second start returns the first round).
    /// </summary>
    [Fact]
    public async Task Nacharbeit_Setzt_Keine_Ueberholte_Runde_Fort()
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var father = await TestApi.AdultAsync(factory);
        const string pin = "8493";
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Stufen-Kind"), pin }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, await TestApi.DefaultSubjectIdAsync(father));
        var cloze = $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/cloze";
        object Cloze(string title, string answer, int? level = null, int? target = null) => new
        {
            title,
            orderIndex = 1,
            rewardPoints = 10,
            config = new { text = "I fell {{1}}.", gaps = new object[] { new { index = 1, answer, prompt = "Ich schlief ein." } } },
            buildLevel = level,
            buildTargetExerciseId = target,
        };
        var targetId = await TestApi.IdAsync(await father.PostAsJsonAsync(cloze, Cloze("Ziel", "asleep")));
        await TestApi.IdAsync(await father.PostAsJsonAsync(cloze, Cloze("Stufe 1", "down", 1, targetId)));
        await TestApi.IdAsync(await father.PostAsJsonAsync(cloze, Cloze("Stufe 2", "over", 2, targetId)));
        var planId = await TestApi.CreateEmptyPlanAsync(father, childId);
        var created = await father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId = targetId, currentBuildLevel = 2, remediation = true });
        Assert.True(created.IsSuccessStatusCode, await created.Content.ReadAsStringAsync());
        var positionId = await TestApi.IdAsync(created);
        var child = await TestApi.ChildAsync(factory, childId, pin);

        var first = await TestApi.StartPositionSessionAsync(child, planId, positionId);
        (await child.PostAsync($"{TestApi.PracticeBase(planId, positionId)}/{first}/end", null)).EnsureSuccessStatusCode();
        var patch = await father.PatchAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions/{positionId}", new { currentBuildLevel = 1 });
        Assert.True(patch.IsSuccessStatusCode, await patch.Content.ReadAsStringAsync());

        var second = await TestApi.StartPositionSessionAsync(child, planId, positionId);
        Assert.NotEqual(first, second);
        var next = await child.GetAsync($"{TestApi.PracticeBase(planId, positionId)}/{second}/next");
        Assert.True(next.IsSuccessStatusCode, await next.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// Under "typed tests only" a self-assessed pass raises nothing - it could not meet the goal either. The prerequisite
    /// plays its own default stage (self-assessment), the position demands typed tests. Red: a promotion without the
    /// graded check (tapping "known" would climb the path).
    /// </summary>
    [Fact]
    public async Task Selbsteinschaetzung_Hebt_Nicht_Wenn_Getippt_Verlangt_Ist()
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var father = await TestApi.AdultAsync(factory);
        const string pin = "8492";
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Stufen-Kind"), pin }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, await TestApi.DefaultSubjectIdAsync(father));
        var targetId = await TestApi.IdAsync(await father.PostAsJsonAsync(Unit(seriesId, unitId), Arithmetic("Ziel", "2 + 3", 5)));
        await TestApi.IdAsync(await father.PostAsJsonAsync($"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/vocabulary",
            new
            {
                title = "Stufe 1",
                orderIndex = 2,
                rewardPoints = 10,
                defaultStage = (int)TestStage.SelfAssess,
                config = new { direction = "front-to-back", sourceLang = "en", targetLang = "de", items = new[] { new { front = "dog", back = "Hund" } } },
                buildLevel = 1,
                buildTargetExerciseId = targetId,
            }));
        var planId = await TestApi.CreateEmptyPlanAsync(father, childId);
        var positionId = await TestApi.IdAsync(await father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId = targetId, currentBuildLevel = 1, cadence = "None", requireTypedTest = true }));
        var p = new Path(father, await TestApi.ChildAsync(factory, childId, pin), childId, planId, positionId);

        var started = await p.Child.PostAsJsonAsync(Tests(p), new { });
        Assert.True(started.IsSuccessStatusCode, await started.Content.ReadAsStringAsync());
        var attemptId = await TestApi.IdWithKeyAsync(started, "attemptId");
        (await p.Child.PostAsJsonAsync($"{Tests(p)}/{attemptId}/answer", new { itemIndex = 0, wasKnown = true }))
            .EnsureSuccessStatusCode();
        var submit = await (await p.Child.PostAsJsonAsync($"{Tests(p)}/{attemptId}/submit", new { }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(submit.GetProperty("passed").GetBoolean(), "the self-assessed test passes");
        Assert.False(Assert.Single(AttemptsOf(positionId)).Graded);
        Assert.Equal((1, (DateOnly?)null), LevelOf(positionId));
    }

    /// <summary>
    /// The father's report keeps the target's tests from before the path was set: they examined the same exercise. Red: a
    /// filter on the exact level (an unstamped attempt would vanish once the father sets level 3).
    /// </summary>
    [Fact]
    public async Task Bericht_Behaelt_Zieltests_Von_Vor_Dem_Weg()
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var p = await PathAsync(startLevel: Exercise.BuildLevelTarget);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            db.PlanPositions.Single(x => x.Id == p.PositionId).CurrentBuildLevel = null;
            db.SaveChanges();
        }
        await TestAsync(p.Child, p, "5");
        Assert.Null(Assert.Single(AttemptsOf(p.PositionId)).BuildLevel);
        var patch = await p.Father.PatchAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{p.PlanId}/positions/{p.PositionId}", new { currentBuildLevel = Exercise.BuildLevelTarget });
        Assert.True(patch.IsSuccessStatusCode, await patch.Content.ReadAsStringAsync());

        var report = await p.Father.GetFromJsonAsync<JsonElement>(
            $"/api/v1/supervisor/study-plans/{p.PlanId}/positions/{p.PositionId}/report");
        Assert.Equal(1, report.GetProperty("items")[0].GetProperty("testsSeen").GetInt32());
    }

    /// <summary>
    /// The father's level counts at once and replaces a raise waiting for tomorrow. Red: a level change that keeps
    /// <c>BuildLevelEffectiveOn</c> (the father's level 2 would wait for tomorrow and today still play level 1).
    /// </summary>
    [Fact]
    public async Task Stufe_Des_Vaters_Gilt_Sofort()
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var p = await PathAsync(startLevel: 1, secondLevel: true);
        await TestAsync(p.Child, p, "2");
        var patch = await p.Father.PatchAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{p.PlanId}/positions/{p.PositionId}", new { currentBuildLevel = Exercise.BuildLevelTarget });
        Assert.True(patch.IsSuccessStatusCode, await patch.Content.ReadAsStringAsync());
        Assert.Equal((Exercise.BuildLevelTarget, (DateOnly?)null), LevelOf(p.PositionId));
        Assert.Equal("2 + 3", await PlayedPromptAsync(p));
    }

    /// <summary>
    /// The father's report counts test hits of the level shown: on the new level the hit from level 1 on the same index is
    /// another exercise's. Red: a report over every attempt of the position (it shows one test seen).
    /// </summary>
    [Fact]
    public async Task Bericht_Zaehlt_Nur_Tests_Der_Gezeigten_Stufe()
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var p = await PathAsync(startLevel: 1);
        await TestAsync(p.Child, p, "2");
        var report = $"/api/v1/supervisor/study-plans/{p.PlanId}/positions/{p.PositionId}/report";
        var today = (await p.Father.GetFromJsonAsync<JsonElement>(report)).GetProperty("items")[0];
        Assert.Equal(("1 + 1", 1), (today.GetProperty("prompt").GetString(), today.GetProperty("testsSeen").GetInt32()));

        factory.Clock.FreezeAt(Local(16, 10));
        var tomorrow = (await p.Father.GetFromJsonAsync<JsonElement>(report)).GetProperty("items")[0];
        Assert.Equal(("2 + 2", 0), (tomorrow.GetProperty("prompt").GetString(), tomorrow.GetProperty("testsSeen").GetInt32()));
    }

    /// <summary>
    /// The child's answer shown beside a feedback comes from a test of the exercise reported on: a report on the level-2
    /// card does not show the level-1 answer at the same index. Red: an answer lookup over every attempt of the position.
    /// </summary>
    [Fact]
    public async Task Rueckmeldung_Zeigt_Keine_Antwort_Einer_Anderen_Stufe()
    {
        factory.Clock.FreezeAt(Local(15, 10));
        var p = await PathAsync(startLevel: 1);
        await TestAsync(p.Child, p, "2");
        var feedback = $"/api/v1/student/study-plans/{p.PlanId}/positions/{p.PositionId}/feedback";
        (await p.Child.PostAsJsonAsync(feedback, new { text = "heute", itemIndex = 0 })).EnsureSuccessStatusCode();

        factory.Clock.FreezeAt(Local(16, 10));
        (await p.Child.PostAsJsonAsync(feedback, new { text = "morgen", itemIndex = 0 })).EnsureSuccessStatusCode();

        var list = await p.Father.GetFromJsonAsync<JsonElement>($"/api/v1/supervisor/feedback?childId={p.ChildId}");
        var answers = list.EnumerateArray().ToDictionary(f => f.GetProperty("cue").GetString()!,
            f => f.GetProperty("childAnswer").GetString());
        Assert.Equal("2", answers["1 + 1"]);
        Assert.Null(answers["2 + 2"]);
    }
}
