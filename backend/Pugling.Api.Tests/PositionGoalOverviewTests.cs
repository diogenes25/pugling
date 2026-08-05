using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Goal/points engine of the position model (stage 4): a passed position test fulfills the
/// position's daily goal, books the goal points once (<see cref="PointKind.Goal"/>) and lets the
/// daily mission (<c>overview</c>) count as done. A second completion does not pay out twice.
/// </summary>
public class PositionGoalOverviewTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    [Fact]
    public async Task BestandenerPositionsTest_ErfuelltTagesziel_UndBuchtZielpunkteEinmalig()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father); // hello→hallo, goodbye→tschüss
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var testsUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        // The daily mission before the test: the goal is open, the obligation not done.
        var before = await (await child.GetAsync($"/api/v1/student/study-plans/{planId}/overview"))
            .Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.False(before.GetProperty("today"), "dutyDone");

        // Start the test, submit all answers correctly.
        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(testsUrl, new { }), "attemptId");
        var answers = new[]
        {
            new { itemIndex = 0, givenAnswer = "hallo" },   // hello → hallo
            new { itemIndex = 1, givenAnswer = "tschüss" }, // goodbye → tschüss
        };

        var submit = await (await child.PostAsJsonAsync($"{testsUrl}/{attemptId}/submit", new { answers }))
            .Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.True(submit, "passed");

        // The goal points are booked once (scoped to positionId, since the class DB is shared with other tests).
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            Assert.Equal(1, db.PositionGoalRewards.Count(r => r.PlanPositionId == positionId));
            Assert.Equal(20, db.PositionGoalRewards.Where(r => r.PlanPositionId == positionId).Sum(r => r.Points));
        }

        var after = await (await child.GetAsync($"/api/v1/student/study-plans/{planId}/overview"))
            .Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.True(after.GetProperty("today"), "dutyDone");

        // A second passed test on the same day → no double goal points (idempotent per period).
        var attempt2 = await (await child.PostAsJsonAsync(testsUrl, new { }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var attemptId2 = attempt2.GetProperty("attemptId").GetInt32();
        // The status of the second submit belongs checked, not discarded: idempotency hangs on TWO things - the
        // existence check in the code and the unique index (PlanPositionId, Cadence, PeriodStart). If the check
        // is gone, the index keeps the count down, but `EvaluateAndAwardAsync` has no `catch
        // (DbUpdateException)` - the violation becomes a **500**. Without this line exactly that stayed
        // unnoticed (docs/testplan.md, injection D13): the reward count was still 1, the error invisible.
        (await child.PostAsJsonAsync($"{testsUrl}/{attemptId2}/submit", new { answers }))
            .EnsureSuccessStatusCode();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            // Idempotency: despite the second attempt still only 1 reward for this position.
            Assert.Equal(1, db.PositionGoalRewards.Count(r => r.PlanPositionId == positionId));
        }
    }

    /// <summary>
    /// A position may carry a mandatory goal <b>without</b> a reward (<c>PointsGoalMet == 0</c>): the
    /// mandatory goal applies, there is just nothing for it. Then nothing may be booked either - neither a
    /// reward row nor a ledger entry of 0. The balance would stay the same, but history and
    /// reporting would fill up with zero rows that <i>claim</i> a reward (docs/testplan.md,
    /// injection B06). Own child, so that "no goal booking" can be checked across the whole account.
    /// </summary>
    [Fact]
    public async Task ZielOhnePunkte_ErfuelltDiePflicht_BuchtAberNichts()
    {
        var father = await TestApi.AdultAsync(_factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = "Nullpunkt-Kind", pin = "7101" }));
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText,
            childId: childId, pointsGoalMet: 0);
        var child = await TestApi.ChildAsync(_factory, childId, "7101");
        var testsUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(testsUrl, new { }), "attemptId");
        var submit = await (await child.PostAsJsonAsync($"{testsUrl}/{attemptId}/submit", new
        {
            answers = new[]
            {
                new { itemIndex = 0, givenAnswer = "hallo" },
                new { itemIndex = 1, givenAnswer = "tschüss" },
            },
        })).Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.True(submit, "passed");

        // The obligation is met - without that the test would be vacuously green (it would then only check that
        // an unreached goal books nothing).
        var after = await (await child.GetAsync($"/api/v1/student/study-plans/{planId}/overview"))
            .Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.True(after.GetProperty("today"), "dutyDone");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        Assert.Empty(db.PositionGoalRewards.Where(r => r.PlanPositionId == positionId));
        Assert.Equal(0, db.ChildPointsEntries.Count(p => p.ChildId == childId && p.Kind == PointKind.Goal));
    }

    /// <summary>
    /// The <b>loser of a concurrent goal completion</b> must not get an error. Two
    /// simultaneous completions of the same period (double-tap on "submit", two open tabs) both run
    /// through the existence check; the second then hits the unique index
    /// <c>(PlanPositionId, Cadence, PeriodStart)</c>. Nothing is open from a business standpoint – the reward
    /// is settled, it is unique per period – so a 500 on a successful completion would be the only effect.
    /// <para>
    /// The race is staged <b>deterministically</b> here instead of sending two submits in parallel: the
    /// window between the check and <c>SaveChanges</c> is a fraction of a millisecond wide, a
    /// parallel double submit would almost never hit it and would pass green without ever exercising the path.
    /// Instead the loser's state is constructed directly: the reward is already <i>committed</i> by the real
    /// winner (the submit above), and a second context still holds the entry <i>unsaved</i>
    /// - exactly the situation in which its check ran before the winner's commit.
    /// </para>
    /// </summary>
    [Fact]
    public async Task NebenlaeufigeZielbuchung_VerliertDasRennen_OhneFehlerUndOhneDoppelbuchung()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var testsUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        // The winner: a passed test → the goal is reached, the goal points are recorded.
        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(testsUrl, new { }), "attemptId");
        var answers = new[]
        {
            new { itemIndex = 0, givenAnswer = "hallo" },
            new { itemIndex = 1, givenAnswer = "tschüss" },
        };
        (await child.PostAsJsonAsync($"{testsUrl}/{attemptId}/submit", new { answers })).EnsureSuccessStatusCode();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var ledgerBefore = db.ChildPointsEntries.Count(p => p.Kind == PointKind.Goal);

        // The loser: the same period, the entry sits in the ChangeTracker and is not written yet.
        var plan = db.StudyPlans.First(p => p.Id == planId);
        db.PositionGoalRewards.Add(new PositionGoalReward
        {
            PlanPositionId = positionId,
            Cadence = GoalCadence.Daily,
            PeriodStart = today,
            Day = today,
            Points = 20,
        });
        db.ChildPointsEntries.Add(new ChildPointsEntry
        {
            ChildId = plan.ChildId,
            Kind = PointKind.Goal,
            Amount = 20,
            Reason = "[Rennen] Tagesziel erreicht",
        });

        // It has to run through and return the current state - not throw.
        var overview = await scope.ServiceProvider.GetRequiredService<PositionProgressService>()
            .EvaluateAndAwardAsync(plan, today);
        Assert.True(overview.DutyDone);

        using var check = _factory.Services.CreateScope();
        var fresh = check.ServiceProvider.GetRequiredService<PuglingDbContext>();
        // Neither half nor double booked: the conflict discards the loser's whole transaction.
        Assert.Equal(1, fresh.PositionGoalRewards.Count(r => r.PlanPositionId == positionId));
        Assert.Equal(ledgerBefore, fresh.ChildPointsEntries.Count(p => p.Kind == PointKind.Goal));
    }

    /// <summary>
    /// A <b>weekly goal</b>, once reached, must count exactly once in the history (<c>overview/progress</c>) -
    /// not on every day of the week. Regression: the reward carries the week's Monday as the period key,
    /// so the daily rollup must sum over the actual booking day, otherwise TotalPoints is inflated (up to 7x).
    /// </summary>
    [Fact]
    public async Task Wochenziel_WirdImVerlauf_NurEinmalGezaehlt()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText,
            cadence: GoalCadence.Weekly, pointsGoalMet: 20);
        var child = await TestApi.ChildAsync(_factory);
        var testsUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        // Meet the weekly goal through a passed test.
        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(testsUrl, new { }), "attemptId");
        var answers = new[]
        {
            new { itemIndex = 0, givenAnswer = "hallo" },   // hello → hallo
            new { itemIndex = 1, givenAnswer = "tschüss" }, // goodbye → tschüss
        };
        var submit = await (await child.PostAsJsonAsync($"{testsUrl}/{attemptId}/submit", new { answers }))
            .Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.True(submit, "passed");

        // Exactly one reward of 20 for this position (the class DB is shared with other tests → check scoped to the position).
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            Assert.Equal(1, db.PositionGoalRewards.Count(r => r.PlanPositionId == positionId));
            Assert.Equal(20, db.PositionGoalRewards.Where(r => r.PlanPositionId == positionId).Sum(r => r.Points));
        }

        // History over the whole runtime: TotalPoints = 20 (not × the number of weekdays in the plan).
        var progress = await (await child.GetAsync($"/api/v1/student/study-plans/{planId}/overview/progress"))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(20, progress.GetProperty("totalPoints").GetInt32());
    }

    /// <summary>
    /// The history (<c>overview/progress</c>) supports filtering (date range), sorting and paging.
    /// Important: paging/filtering only affect the <c>days</c> list; the metrics (<c>totalDays</c> etc.)
    /// stay stable across the whole run, and <c>X-Total-Count</c> reflects the <b>filtered</b> total count.
    /// </summary>
    [Fact]
    public async Task Verlauf_Progress_UnterstuetztFilterSortUndPaging()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, _) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText); // plan: today..today+5 = 6 days
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/overview/progress";

        // The full history: 6 days, X-Total-Count = 6.
        var full = await child.GetAsync(baseUrl);
        var fullBody = await full.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("6", full.Headers.GetValues("X-Total-Count").Single());
        Assert.Equal(6, fullBody.GetProperty("days").GetArrayLength());

        // Paging: take=2 → 2 days in the body, the header still counts all 6.
        var paged = await child.GetAsync($"{baseUrl}?take=2");
        var pagedBody = await paged.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("6", paged.Headers.GetValues("X-Total-Count").Single());
        Assert.Equal(2, pagedBody.GetProperty("days").GetArrayLength());

        // Sorting -day: the first day is the plan's end date.
        var desc = await (await child.GetAsync($"{baseUrl}?sort=-day"))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(desc.GetProperty("endDate").GetString(),
            desc.GetProperty("days")[0].GetProperty("day").GetString());

        // Filter from=start+3 → only the last 3 days; the figures stay over the full runtime.
        var start = DateOnly.Parse(fullBody.GetProperty("startDate").GetString()!);
        var filtered = await child.GetAsync($"{baseUrl}?from={start.AddDays(3):yyyy-MM-dd}");
        var filteredBody = await filtered.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("3", filtered.Headers.GetValues("X-Total-Count").Single());
        Assert.Equal(3, filteredBody.GetProperty("days").GetArrayLength());
        Assert.Equal(6, filteredBody.GetProperty("totalDays").GetInt32());
    }
}
