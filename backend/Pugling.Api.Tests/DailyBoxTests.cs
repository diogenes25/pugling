using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Daily reward box (B-105): earned once per day with met reward goals, with a coin/gem draw that scales with the
/// streak - and since B-527 a chest the child opens: the draw is booked on opening, not on earning. The positive
/// counterpart to <see cref="PositionGoalPenalty"/>.
/// </summary>
public class DailyBoxTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static async Task PassTestAsync(HttpClient child, int planId, int positionId)
    {
        var testsUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";
        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(testsUrl, new { }), "attemptId");
        var answers = new[]
        {
            new { itemIndex = 0, givenAnswer = "hallo" },
            new { itemIndex = 1, givenAnswer = "tschüss" },
        };
        (await child.PostAsJsonAsync($"{testsUrl}/{attemptId}/submit", new { answers })).EnsureSuccessStatusCode();
    }

    private static async Task<JsonElement> BoxAsync(HttpClient child, int planId) =>
        (await (await child.GetAsync($"/api/v1/student/study-plans/{planId}/overview"))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("dailyBox");

    private static Task<HttpResponseMessage> OpenAsync(HttpClient client, int childId) =>
        client.PostAsync($"/api/v1/student/children/{childId}/daily-box/open", null);

    private int LedgerSum(int childId, PointKind kind)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        return db.ChildPointsEntries.Where(e => e.ChildId == childId && e.Kind == kind).Sum(e => e.Amount);
    }

    private int LedgerRows(int childId, PointKind kind)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        return db.ChildPointsEntries.Count(e => e.ChildId == childId && e.Kind == kind);
    }

    private async Task<(HttpClient Father, HttpClient Child, int ChildId, int PlanId, int PositionId)> FreshChildAsync(string pin)
    {
        var father = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Truhen-Kind"), pin }));
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father); // hello→hallo, goodbye→tschüss
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText, childId: childId);
        return (father, await TestApi.ChildAsync(factory, childId, pin), childId, planId, positionId);
    }

    /// <summary>
    /// AK 1 and AK 2 (B-527): finishing the day's goals earns the chest but books nothing, and the overview keeps its
    /// content secret; opening books exactly the drawn values and reveals them. Also: one chest per day.
    /// </summary>
    [Fact]
    public async Task BestandenerPositionsTest_VerdientTruhe_Oeffnen_Bucht_Den_Inhalt()
    {
        var (_, child, childId, planId, positionId) = await FreshChildAsync("8311");

        // Before the duty is met: the overview shows no chest yet.
        var before = await BoxAsync(child, planId);
        JsonAssert.False(before, "claimedToday");
        Assert.Equal(0, before.GetProperty("waiting").GetInt32());

        await PassTestAsync(child, planId, positionId);

        // AK 1: earned, not opened, content withheld. Red when EvaluateAndAwardAsync still books the ledger rows
        // (the two LedgerRows asserts) or StatusAsync hands out the content before opening (the two Null asserts).
        var earned = await BoxAsync(child, planId);
        JsonAssert.True(earned, "claimedToday");
        JsonAssert.False(earned, "openedToday");
        Assert.Equal(1, earned.GetProperty("waiting").GetInt32());
        JsonAssert.Null(earned, "coinsAwarded");
        JsonAssert.Null(earned, "gemsAwarded");
        Assert.Equal(1, earned.GetProperty("streakAtClaim").GetInt32());
        Assert.Equal(0, LedgerRows(childId, PointKind.DailyBoxCoins));
        Assert.Equal(0, LedgerRows(childId, PointKind.DailyBoxGems));

        // AK 2: opening books exactly the draw. Exact values, not the appsettings ranges: PuglingWebAppFactory pins
        // the draw to 20/2. Red when OpenAsync does not add the ledger rows (sums stay 0).
        var res = await OpenAsync(child, childId);
        Assert.True(res.IsSuccessStatusCode, $"{(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        var opened = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(20, opened.GetProperty("coins").GetInt32());
        Assert.Equal(2, opened.GetProperty("gems").GetInt32());
        Assert.Equal(0, opened.GetProperty("waitingAfter").GetInt32());
        Assert.Equal(20, LedgerSum(childId, PointKind.DailyBoxCoins));
        Assert.Equal(2, LedgerSum(childId, PointKind.DailyBoxGems));

        var after = await BoxAsync(child, planId);
        JsonAssert.True(after, "openedToday");
        Assert.Equal(0, after.GetProperty("waiting").GetInt32());
        Assert.Equal(20, after.GetProperty("coinsAwarded").GetInt32());
        Assert.Equal(2, after.GetProperty("gemsAwarded").GetInt32());

        // A second passed test on the same day: no second chest (idempotent per day), and nothing left to open.
        await PassTestAsync(child, planId, positionId);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            var claim = db.DailyBoxClaims.Single(c => c.ChildId == childId);
            Assert.NotNull(claim.OpenedAt);
        }
        var again = await OpenAsync(child, childId);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("daily_box_none", (await again.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        Assert.Equal(20, LedgerSum(childId, PointKind.DailyBoxCoins));
    }

    /// <summary>
    /// AK 3: four taps at once on one waiting chest book it exactly once; every other tap gets a 409
    /// (<c>daily_box_none</c> from the conditional update, or <c>concurrency_conflict</c> from the child's stamp).
    /// Red only when BOTH guards are gone (the <c>OpenedAt == null</c> condition of the ExecuteUpdate and the stamp bump),
    /// since each alone still holds the line. It is a race: a missing guard shows up as two bookings only when two taps
    /// overlap, which four parallel requests make likely but cannot force.
    /// </summary>
    [Fact]
    public async Task Paralleles_Oeffnen_Bucht_Genau_Einmal()
    {
        var (_, child, childId, planId, positionId) = await FreshChildAsync("8312");
        await PassTestAsync(child, planId, positionId);

        var responses = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => OpenAsync(child, childId)));

        Assert.Single(responses, r => r.IsSuccessStatusCode);
        foreach (var r in responses.Where(r => !r.IsSuccessStatusCode))
        {
            Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
            var code = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();
            Assert.Contains(code, new[] { "daily_box_none", "concurrency_conflict" });
        }
        Assert.Equal(1, LedgerRows(childId, PointKind.DailyBoxCoins));
        Assert.Equal(20, LedgerSum(childId, PointKind.DailyBoxCoins));
    }

    /// <summary>
    /// AK 4 and decision 2: a chest never lapses, and with two waiting the oldest opens first. Yesterday's chest is seeded
    /// with a draw (11/1) different from today's pinned one (20/2), so the opened values name the day. Red when the
    /// waiting chest is chosen by <c>Day</c> descending (today's 20 would come out first).
    /// </summary>
    [Fact]
    public async Task Zwei_Wartende_Truhen_Oeffnen_Die_Aelteste_Zuerst()
    {
        var (_, child, childId, planId, positionId) = await FreshChildAsync("8313");
        var today = DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            db.DailyBoxClaims.Add(new DailyBoxClaim
            {
                ChildId = childId,
                Day = today.AddDays(-1),
                CoinsAwarded = 11,
                GemsAwarded = 1,
                StreakAtClaim = 3,
                AwardedAt = DateTime.UtcNow.AddDays(-1),
            });
            db.SaveChanges();
        }
        await PassTestAsync(child, planId, positionId);
        Assert.Equal(2, (await BoxAsync(child, planId)).GetProperty("waiting").GetInt32());

        var first = await (await OpenAsync(child, childId)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(today.AddDays(-1).ToString("yyyy-MM-dd"), first.GetProperty("day").GetString());
        Assert.Equal(11, first.GetProperty("coins").GetInt32());
        Assert.Equal(3, first.GetProperty("streakAtClaim").GetInt32());
        Assert.Equal(1, first.GetProperty("waitingAfter").GetInt32());
        // Today's chest still waits: opening yesterday's does not reveal today's content.
        var box = await BoxAsync(child, planId);
        JsonAssert.False(box, "openedToday");
        JsonAssert.Null(box, "coinsAwarded");

        var second = await (await OpenAsync(child, childId)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(20, second.GetProperty("coins").GetInt32());
        Assert.Equal(0, second.GetProperty("waitingAfter").GetInt32());
        Assert.Equal(31, LedgerSum(childId, PointKind.DailyBoxCoins));
    }

    /// <summary>
    /// AK 5 and decision 4: only the child opens its chest. The supervisor owns the child and passes the ownership filter,
    /// yet gets 403 and nothing is booked. Red without the <c>IsStudent</c> check in the controller (a chest is waiting,
    /// so the call would book it with 200).
    /// </summary>
    [Fact]
    public async Task Supervisor_Darf_Die_Truhe_Nicht_Oeffnen()
    {
        var (father, child, childId, planId, positionId) = await FreshChildAsync("8314");
        await PassTestAsync(child, planId, positionId);

        var res = await OpenAsync(father, childId);

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.Equal(0, LedgerRows(childId, PointKind.DailyBoxCoins));
        Assert.Equal(1, (await BoxAsync(child, planId)).GetProperty("waiting").GetInt32());
    }

    /// <summary>
    /// Review finding (B-527): a child cannot open another child's chest - the ownership filter answers 404 like an unknown
    /// child, and nothing is booked to the other child. Red when <c>DailyBoxController</c> loses its
    /// <c>ChildOwnershipFilter</c> (the student role alone would then reach the service and book B's chest).
    /// </summary>
    [Fact]
    public async Task Fremdes_Kind_Kann_Die_Truhe_Nicht_Oeffnen()
    {
        var a = await FreshChildAsync("8315");
        var b = await FreshChildAsync("8316");
        await PassTestAsync(b.Child, b.PlanId, b.PositionId);

        var res = await OpenAsync(a.Child, b.ChildId);

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        Assert.Equal(0, LedgerRows(b.ChildId, PointKind.DailyBoxCoins));
        Assert.Equal(1, (await BoxAsync(b.Child, b.PlanId)).GetProperty("waiting").GetInt32());
    }

    /// <summary>
    /// Review finding (B-527): an account that is the student of child A and the supervisor of child B passes the role
    /// check and the OR-combined ownership filter for B - yet only B itself opens B's chest. Red when the controller checks
    /// only <c>User.IsStudent()</c> without comparing the token's own child id (the call would book B's chest with 200).
    /// </summary>
    [Fact]
    public async Task Konto_Mit_Zwei_Profilen_Oeffnet_Nur_Die_Eigene_Truhe()
    {
        var a = await FreshChildAsync("8317");
        var b = await FreshChildAsync("8318");
        await PassTestAsync(b.Child, b.PlanId, b.PositionId);

        // Adult 1 supervises B (it created the child); the token also carries A's student profile.
        var tokens = factory.Services.GetRequiredService<Pugling.Api.Auth.TokenService>();
        var (token, _) = tokens.IssueForAccount(new Account { Id = 1, DisplayName = "Doppelrolle" },
        [
            new AccountProfile { Role = ProfileRole.Supervisor, AdultId = 1 },
            new AccountProfile { Role = ProfileRole.Student, ChildId = a.ChildId },
        ]);
        var dual = factory.CreateClient();
        dual.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var res = await OpenAsync(dual, b.ChildId);

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.Equal(0, LedgerRows(b.ChildId, PointKind.DailyBoxCoins));
    }

    /// <summary>
    /// A streak of 7 consecutive fully met days (six seeded past days plus today) crosses the first
    /// escalation tier (×1.5) - the box's coin draw must then come out multiplied.
    /// Past days are seeded via passed <see cref="TestAttempt"/> rows, exactly the evidence
    /// <c>PositionProgressService.IsGoalMetAsync</c> itself reads for a closed daily period.
    /// </summary>
    [Fact]
    public async Task SiebenTageStreak_SkaliertDieBoxUeberDieBasisspanneHinaus()
    {
        var father = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = "Streak-Kind", pin = "8302" }));
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText, childId: childId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            var plan = db.StudyPlans.First(p => p.Id == planId);
            // Widen the plan's runtime backward far enough for the 6 seeded past days plus today.
            plan.StartDate = today.AddDays(-10);
            for (var i = 1; i <= 6; i++)
            {
                db.TestAttempts.Add(new TestAttempt
                {
                    StudyPlanId = planId,
                    PlanPositionId = positionId,
                    Day = today.AddDays(-i),
                    CompletedAt = DateTime.UtcNow.AddDays(-i),
                    Passed = true,
                    TotalItems = 2,
                    CorrectItems = 2,
                    ScorePercent = 100,
                });
            }
            db.SaveChanges();
        }

        var child = await TestApi.ChildAsync(factory, childId, "8302");
        var testsUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";
        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(testsUrl, new { }), "attemptId");
        var answers = new[]
        {
            new { itemIndex = 0, givenAnswer = "hallo" },
            new { itemIndex = 1, givenAnswer = "tschüss" },
        };
        (await child.PostAsJsonAsync($"{testsUrl}/{attemptId}/submit", new { answers })).EnsureSuccessStatusCode();

        var overview = await (await child.GetAsync($"/api/v1/student/study-plans/{planId}/overview"))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(7, overview.GetProperty("currentStreak").GetInt32());

        var box = overview.GetProperty("dailyBox");
        JsonAssert.True(box, "claimedToday");
        Assert.Equal(7, box.GetProperty("streakAtClaim").GetInt32());
        // PuglingWebAppFactory pins the coin draw to 20, so the ×1.5 tier from streak 7 is provable as an exact
        // value instead of a range that a non-escalated draw could also satisfy. The content shows once opened (B-527).
        var opened = await (await OpenAsync(child, childId)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(30, opened.GetProperty("coins").GetInt32());
        Assert.Equal(30, (await BoxAsync(child, planId)).GetProperty("coinsAwarded").GetInt32());

        using var check = factory.Services.CreateScope();
        var checkDb = check.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var claim = checkDb.DailyBoxClaims.Single(c => c.ChildId == childId && c.Day == today);
        Assert.Equal(7, claim.StreakAtClaim);
    }
}
