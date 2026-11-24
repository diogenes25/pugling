using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-768: a feedback the supervisor judges justified corrects the reported test over a correction trail and books what
/// the right grading would have booked - the missed duty's penalty taken back by a positive <c>GoalPenalty</c>, a missing
/// goal reward paid - once, with the child's wallet stamp bumped. Every case runs on its own child and plan; the test sat
/// yesterday is seeded (a closed period is what the settlement punishes), the report and the judgement go through the
/// API. Case by case the change that turns it red is named.
/// </summary>
public class MeldungBuchtNachTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private const int Penalty = 5;
    private const int GoalPoints = 20;

    private sealed record Kid(HttpClient Father, HttpClient Child, int ChildId, int PlanId, int PositionId, DateOnly Today)
    {
        public string FeedbackUrl => $"/api/v1/student/study-plans/{PlanId}/positions/{PositionId}/feedback";
        public DateOnly Yesterday => Today.AddDays(-1);
    }

    private T WithDb<T>(Func<PuglingDbContext, T> body)
    {
        using var scope = factory.Services.CreateScope();
        return body(scope.ServiceProvider.GetRequiredService<PuglingDbContext>());
    }

    private async Task<T> WithScopeAsync<T>(Func<IServiceProvider, Task<T>> body)
    {
        using var scope = factory.Services.CreateScope();
        return await body(scope.ServiceProvider);
    }

    // Own child with a daily duty on a two-card vocabulary exercise (hello/goodbye), running since five days.
    private async Task<Kid> KidAsync(int penaltyCoins = Penalty)
    {
        var father = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Nachbucher"), pin = "4712" }));
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        DateOnly today;
        using (var scope = factory.Services.CreateScope()) today = scope.ServiceProvider.GetRequiredService<LearningDay>().Today;
        var (planId, positionId) = WithDb(db =>
        {
            var start = today.AddDays(-5);
            var plan = new StudyPlan { ChildId = childId, Title = "Nachbuch-Plan", StartDate = start, EndDate = today.AddDays(5), Active = true };
            plan.Positions.Add(new PlanPosition
            {
                ExerciseId = exerciseId,
                Cadence = GoalCadence.Daily,
                PointsGoalMet = GoalPoints,
                PenaltyCoins = penaltyCoins,
                UseLeitner = true,
                CreatedAt = start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            });
            db.StudyPlans.Add(plan);
            db.SaveChanges();
            return (plan.Id, plan.Positions[0].Id);
        });
        return new Kid(father, await TestApi.ChildAsync(factory, childId, "4712"), childId, planId, positionId, today);
    }

    // A completed typed test of the child on `day` with the given answers (index, answer, graded right?), passed by the
    // position's threshold of now (80 % without one).
    private int SeedAttempt(Kid k, DateOnly day, params (int Index, string Given, bool Right)[] answers)
    {
        var threshold = WithDb(db => db.PlanPositions.Single(p => p.Id == k.PositionId).GoalThreshold) ?? 80;
        return WithDb(db => SeedAttempt(db, k, day, threshold, answers));
    }

    private static int SeedAttempt(PuglingDbContext db, Kid k, DateOnly day, int threshold,
        (int Index, string Given, bool Right)[] answers)
    {
        var correct = answers.Count(a => a.Right);
        var score = (int)Math.Round(100.0 * correct / answers.Length);
        var attempt = new TestAttempt
        {
            StudyPlanId = k.PlanId,
            PlanPositionId = k.PositionId,
            Day = day,
            StageValue = (int)TestStage.FreeText,
            Graded = true,
            StartedAt = day.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc),
            CompletedAt = day.ToDateTime(new TimeOnly(10, 5), DateTimeKind.Utc),
            TotalItems = answers.Length,
            CorrectItems = correct,
            ScorePercent = score,
            Passed = score >= threshold,
            Order = [.. answers.Select(a => a.Index)],
            Cursor = answers.Length,
            Results = [.. answers.Select(a => new TestItemResult
            {
                ItemIndex = a.Index, StageValue = (int)TestStage.FreeText, GivenAnswer = a.Given, WasCorrect = a.Right,
            })],
        };
        db.TestAttempts.Add(attempt);
        db.SaveChanges();
        return attempt.Id;
    }

    private Task<int> SettleAsync(Kid k) => WithScopeAsync(sp =>
        sp.GetRequiredService<PositionProgressService>().SettleClosedPeriodsAsync(k.ChildId, k.Today, CancellationToken.None));

    private async Task<int> ReportAsync(Kid k, int itemIndex, string reason = "TooStrict")
    {
        var res = await k.Child.PostAsJsonAsync(k.FeedbackUrl, new { reasons = new[] { reason }, itemIndex });
        Assert.True(res.IsSuccessStatusCode, $"{(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("ids")[0].GetInt32();
    }

    private static async Task JustifyAsync(Kid k, int feedbackId)
    {
        var res = await k.Father.PatchAsJsonAsync($"/api/v1/supervisor/feedback/{feedbackId}",
            new { status = "Done", outcome = "Justified" });
        Assert.True(res.IsSuccessStatusCode, $"{(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
    }

    private TestAttempt Attempt(int id) =>
        WithDb(db => db.TestAttempts.AsNoTracking().Include(t => t.Results).Single(t => t.Id == id));

    private List<ChildPointsEntry> Ledger(Kid k, PointKind kind) =>
        WithDb(db => db.ChildPointsEntries.AsNoTracking().Where(e => e.ChildId == k.ChildId && e.Kind == kind).ToList());

    private Guid Stamp(Kid k) => WithDb(db => db.Children.AsNoTracking().Single(c => c.Id == k.ChildId).ConcurrencyStamp);

    private PositionGoalPenalty PenaltyRow(Kid k) =>
        WithDb(db => db.PositionGoalPenalties.AsNoTracking().Single(p => p.PlanPositionId == k.PositionId));

    /// <summary>
    /// AK 1: "justified" corrects the reported attempt over the trail - one corrected answer, passed after correction -
    /// while the attempt's own finding (CorrectItems, ScorePercent, Passed, the answer's WasCorrect) stays, and another
    /// attempt of the same child with a wrong answer to the same card is untouched. AK 8 (statement of intent): the box
    /// of the card is not booked back; the starting state holds it, it turns red only if the correction reaches into
    /// PositionItemProgress. Red if the result is overwritten, or if the correction hits every answer to the card.
    /// </summary>
    [Fact]
    public async Task Berechtigt_rechnet_den_Versuch_ueber_die_Korrekturspur_neu()
    {
        var k = await KidAsync();
        var other = SeedAttempt(k, k.Today.AddDays(-2), (0, "halo", false), (1, "tschüss", true));
        var reported = SeedAttempt(k, k.Yesterday, (0, "hallö", false), (1, "tschüss", true));
        var boxes = WithDb(db => db.PositionItemProgress.Where(p => p.PlanPositionId == k.PositionId).Sum(p => (int?)p.Box) ?? 0);

        await JustifyAsync(k, await ReportAsync(k, 0));

        var a = Attempt(reported);
        Assert.Equal((1, 50, false, 1, (bool?)true), (a.CorrectItems, a.ScorePercent, a.Passed, a.CorrectedItems, a.PassedAfterCorrection));
        Assert.False(a.Results.Single(r => r.ItemIndex == 0).WasCorrect);
        var o = Attempt(other);
        Assert.Equal(((int?)null, (bool?)null), (o.CorrectedItems, o.PassedAfterCorrection));
        // Review finding: the attempt view shows the trail beside its own finding - else it says "not passed" while the duty
        // counts the test as passed.
        var detail = await k.Child.GetFromJsonAsync<JsonElement>(
            $"/api/v1/student/study-plans/{k.PlanId}/positions/{k.PositionId}/tests/{reported}");
        Assert.Equal((false, 1, true), (detail.GetProperty("passed").GetBoolean(), detail.GetProperty("correctedItems").GetInt32(),
            detail.GetProperty("passedAfterCorrection").GetBoolean()));
        Assert.Equal(boxes, WithDb(db => db.PositionItemProgress.Where(p => p.PlanPositionId == k.PositionId).Sum(p => (int?)p.Box) ?? 0));
    }

    /// <summary>
    /// AK 2: the duty and the mission count read "passed" through the same expression - after the correction yesterday's
    /// goal is met and the tests-passed metric counts the attempt. Red if IsGoalMetAsync or MetricsService still reads
    /// <c>Passed</c> alone (each one turns its own assertion red).
    /// </summary>
    [Fact]
    public async Task Pflicht_und_Missionszahl_lesen_bestanden_nach_Korrektur()
    {
        var k = await KidAsync(penaltyCoins: 0);
        SeedAttempt(k, k.Yesterday, (0, "halo", false), (1, "tschüss", true));
        Task<bool> GoalMetAsync() => WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<PuglingDbContext>();
            var pos = await db.PlanPositions.Include(p => p.Exercise).Include(p => p.StudyPlan).SingleAsync(p => p.Id == k.PositionId);
            return await sp.GetRequiredService<PositionProgressService>().IsGoalMetAsync(pos, pos.StudyPlan!, k.Yesterday, CancellationToken.None);
        });
        Task<int> PassedCountAsync() => WithScopeAsync(sp => sp.GetRequiredService<MetricsService>()
            .ValueAsync(k.ChildId, ProgressMetric.TestsPassed, null, null, k.Today, CancellationToken.None));
        Assert.False(await GoalMetAsync());
        Assert.Equal(0, await PassedCountAsync());

        await JustifyAsync(k, await ReportAsync(k, 0));

        Assert.True(await GoalMetAsync());
        Assert.Equal(1, await PassedCountAsync());
    }

    /// <summary>
    /// AK 3, AK 9: a penalty that hung only on this failed test is taken back by a positive GoalPenalty of the same size;
    /// the penalty row stays (marked refunded, by this feedback), so a second settlement books nothing; the missing goal
    /// reward is paid (decision 2); the wallet stamp moved; and the week's earned coins grow by the reward only, not by the
    /// refund. Red if the row is deleted (the settlement books the penalty again), the stamp stays, or the refund counts
    /// as earned.
    /// </summary>
    [Fact]
    public async Task Malus_wird_durch_eine_positive_Gegenbuchung_zurueckgenommen()
    {
        var k = await KidAsync();
        SeedAttempt(k, k.Yesterday, (0, "halo", false), (1, "tschüss", true));
        Assert.Equal(Penalty, await SettleAsync(k));
        Task<int> EarnedAsync() => WithScopeAsync(async sp => (await sp.GetRequiredService<WalletService>()
            .WeekCoinsAsync(k.ChildId, DateTime.UtcNow, CancellationToken.None)).ThisWeek);
        var earnedBefore = await EarnedAsync();
        var stampBefore = Stamp(k);

        var feedbackId = await ReportAsync(k, 0);
        await JustifyAsync(k, feedbackId);

        Assert.Equal([-Penalty, Penalty], Ledger(k, PointKind.GoalPenalty).Select(e => e.Amount).Order());
        var row = PenaltyRow(k);
        Assert.NotNull(row.RefundedAt);
        Assert.Equal(feedbackId, row.RefundedByFeedbackId);
        Assert.Equal(0, await SettleAsync(k));
        Assert.Equal([GoalPoints], Ledger(k, PointKind.Goal).Select(e => e.Amount));
        Assert.NotEqual(stampBefore, Stamp(k));
        Assert.Equal(earnedBefore + GoalPoints, await EarnedAsync());
    }

    /// <summary>
    /// AK 4, AK 9: when the period stays missed after the correction (both answers wrong, one justified: 50 %), the
    /// penalty stays and nothing is booked - but the trail is written and the stamp still moves (every justified path
    /// bumps). Red if the correction lifts the penalty blindly, or if this path leaves the stamp alone.
    /// </summary>
    [Fact]
    public async Task Malus_bleibt_wenn_die_Periode_trotz_Korrektur_verfehlt_ist()
    {
        var k = await KidAsync();
        var attempt = SeedAttempt(k, k.Yesterday, (0, "halo", false), (1, "tschüs", false));
        Assert.Equal(Penalty, await SettleAsync(k));
        var stampBefore = Stamp(k);

        await JustifyAsync(k, await ReportAsync(k, 0));

        Assert.Equal((1, (bool?)false), (Attempt(attempt).CorrectedItems, Attempt(attempt).PassedAfterCorrection));
        Assert.Null(PenaltyRow(k).RefundedAt);
        Assert.Equal([-Penalty], Ledger(k, PointKind.GoalPenalty).Select(e => e.Amount));
        Assert.Empty(Ledger(k, PointKind.Goal));
        Assert.NotEqual(stampBefore, Stamp(k));
    }

    /// <summary>
    /// AK 5, AK 6 (first half), AK 9: a period without penalty but without its reward gets the reward once; confirming the
    /// same feedback again books nothing more. The stamp moves on this adding-only path too. Red if a second confirmation
    /// writes a second reward, or if the reward path skips the bump.
    /// </summary>
    [Fact]
    public async Task Fehlende_Belohnung_wird_einmal_nachgebucht()
    {
        var k = await KidAsync(penaltyCoins: 0);
        SeedAttempt(k, k.Yesterday, (0, "halo", false), (1, "tschüss", true));
        var stampBefore = Stamp(k);

        var feedbackId = await ReportAsync(k, 0);
        await JustifyAsync(k, feedbackId);
        Assert.NotEqual(stampBefore, Stamp(k));
        await JustifyAsync(k, feedbackId);

        Assert.Equal([GoalPoints], Ledger(k, PointKind.Goal).Select(e => e.Amount));
        Assert.Single(WithDb(db => db.PositionGoalRewards.Where(r => r.PlanPositionId == k.PositionId).ToList()));
    }

    /// <summary>
    /// AK 6 (decision 6): two feedbacks about two answers of the same period - two failed attempts yesterday - refund the
    /// one penalty row once. Red if the refund mark sat only on the feedback (each feedback would refund on its own).
    /// </summary>
    [Fact]
    public async Task Zwei_Meldungen_derselben_Periode_erstatten_die_Strafe_nur_einmal()
    {
        var k = await KidAsync();
        SeedAttempt(k, k.Yesterday, (0, "halo", false), (1, "tschüss", true));
        Assert.Equal(Penalty, await SettleAsync(k));
        var first = await ReportAsync(k, 0);
        SeedAttempt(k, k.Yesterday, (0, "hallö", false), (1, "tschüss", true));
        var second = await ReportAsync(k, 0);
        Assert.NotEqual(first, second);

        await JustifyAsync(k, first);
        await JustifyAsync(k, second);

        Assert.Equal([-Penalty, Penalty], Ledger(k, PointKind.GoalPenalty).Select(e => e.Amount).Order());
        Assert.Equal(first, PenaltyRow(k).RefundedByFeedbackId);
    }

    /// <summary>
    /// AK 7: the child cannot confirm (403) and a foreign supervisor gets 404 - and neither books anything. Red if the role
    /// or ownership check is missing.
    /// </summary>
    [Fact]
    public async Task Nur_der_Betreuer_bestaetigt_und_bucht()
    {
        var k = await KidAsync();
        SeedAttempt(k, k.Yesterday, (0, "halo", false), (1, "tschüss", true));
        await SettleAsync(k);
        var feedbackId = await ReportAsync(k, 0);
        var strangerId = await TestApi.IdAsync(await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = TestApi.UniqueName("Fremd"), pin = "6767" }));
        var stranger = await TestApi.AdultAsync(factory, strangerId, "6767");
        var body = new { status = "Done", outcome = "Justified" };

        Assert.Equal(HttpStatusCode.Forbidden, (await k.Child.PatchAsJsonAsync($"/api/v1/supervisor/feedback/{feedbackId}", body)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PatchAsJsonAsync($"/api/v1/supervisor/feedback/{feedbackId}", body)).StatusCode);

        Assert.Equal([-Penalty], Ledger(k, PointKind.GoalPenalty).Select(e => e.Amount));
        Assert.Null(PenaltyRow(k).RefundedAt);
    }

    /// <summary>
    /// Decision 7: "passed after correction" is computed with the threshold of the confirmation - but a correction never
    /// takes a pass away. A test passed at 30 % (1 of 3) stays passed when the threshold was raised to 90 % since and the
    /// correction only reaches 67 %. Red if the trail is computed from the score alone (the duty met by that test would
    /// fall back to missed).
    /// </summary>
    [Fact]
    public async Task Korrektur_nimmt_ein_Bestehen_nicht_weg()
    {
        var k = await KidAsync(penaltyCoins: 0);
        WithDb(db => db.PlanPositions.Where(p => p.Id == k.PositionId).ExecuteUpdate(u => u.SetProperty(p => p.GoalThreshold, 30)));
        // Three result rows on two cards (card 1 asked twice) give the 1-of-3 a two-card exercise cannot; the report binds
        // the newest row of card 1.
        var attempt = SeedAttempt(k, k.Yesterday, (0, "hallo", true), (1, "tschüs", false), (1, "tschüs", false));
        Assert.True(Attempt(attempt).Passed);
        WithDb(db => db.PlanPositions.Where(p => p.Id == k.PositionId).ExecuteUpdate(u => u.SetProperty(p => p.GoalThreshold, 90)));

        await JustifyAsync(k, await ReportAsync(k, 1));

        Assert.Equal((1, (bool?)true), (Attempt(attempt).CorrectedItems, Attempt(attempt).PassedAfterCorrection));
    }
}
