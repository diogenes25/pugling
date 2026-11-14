using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Secures the "penalty": a missed mandatory goal of a study plan position deducts the coin penalty once
/// during lazy settlement (child login / shop purchase) – debt (negative balance) is allowed, an
/// inactive plan is spared (fairness). In addition: the father can gift gems (gem twin of the manual
/// coin ledger entry) – the pressure valve against the debt death spiral.
/// </summary>
public class PflichtMalusTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static async Task<JsonElement> JsonAsync(HttpResponseMessage res)
    {
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    /// <summary>
    /// Directly seeds a plan with a mandatory position including penalty and (possibly past) start date. Without
    /// <paramref name="createdAt"/> the position exists since the plan start: settlement never charges a day before the
    /// position was created (B-432), so a position created "now" would owe nothing for the past days.
    /// </summary>
    private static (int planId, int positionId) SeedPenaltyPlan(PuglingWebAppFactory f, int childId, int exerciseId,
        DateOnly start, int penaltyCoins, bool active = true, GoalCadence cadence = GoalCadence.Daily,
        string title = "Malus-Plan", int durationDays = 10, DateTime? createdAt = null, int newItemsPerDay = 0,
        bool playedFromStart = true)
    {
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var plan = new StudyPlan { ChildId = childId, Title = title, StartDate = start, EndDate = start.AddDays(durationDays), Active = active };
        plan.Positions.Add(new PlanPosition
        {
            CreatedAt = createdAt ?? start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            NewItemsPerDay = newItemsPerDay,
            ExerciseId = exerciseId,
            Order = 0,
            Cadence = cadence,
            PointsGoalMet = 20,
            PenaltyCoins = penaltyCoins,
            UseLeitner = true,
        });
        db.StudyPlans.Add(plan);
        db.SaveChanges();
        // B-535: the penalty clock starts with the child's first play in the plan. These cases are about the penalty
        // itself, so the child starts on day one unless a case says otherwise.
        if (playedFromStart) Played(f, plan.Id, plan.Positions[0].Id, start);
        return (plan.Id, plan.Positions[0].Id);
    }

    /// <summary>Records that someone sat a test in the plan on <paramref name="day"/> - the child unless stated.</summary>
    private static void Played(PuglingWebAppFactory f, int planId, int positionId, DateOnly day, bool bySupervisor = false)
    {
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        db.TestAttempts.Add(new TestAttempt
        {
            StudyPlanId = planId, PlanPositionId = positionId, Day = day, BySupervisor = bySupervisor,
            StartedAt = day.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc),
        });
        db.SaveChanges();
    }

    private static async Task<int> FreshChildIdAsync(HttpClient father, string pin) =>
        await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Malus-Kind", pin }));

    [Fact]
    public async Task GerissenePflicht_ZiehtMuenzMalusAb_ErlaubtSchuld_UndIstIdempotent()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var childId = await FreshChildIdAsync(father, "7001");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var (_, positionId) = SeedPenaltyPlan(factory, childId, exerciseId, today.AddDays(-2), penaltyCoins: 50);

        // 60 coins starting balance; never practiced → the two closed days (today-2, today-1) are missed.
        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/points", new { amount = 60, reason = "Start" }))
            .EnsureSuccessStatusCode();

        // The child's login settles it: 2 × 50 penalty → 60 - 100 = -40 (debt is allowed).
        var child = await TestApi.ChildAsync(factory, childId, "7001");
        var wallet = await JsonAsync(await child.GetAsync("/api/v1/student/me/points"));
        Assert.Equal(-40, wallet.GetProperty("coins").GetInt32());

        // Exactly two penalty entries of -50, category GoalPenalty.
        var entries = await JsonAsync(await child.GetAsync("/api/v1/student/me/points/entries"));
        var penalties = entries.EnumerateArray()
            .Where(e => e.GetProperty("kind").GetString() == "GoalPenalty").ToList();
        Assert.Equal(2, penalties.Count);
        Assert.All(penalties, e => Assert.Equal(-50, e.GetProperty("amount").GetInt32()));

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            Assert.Equal(2, db.PositionGoalPenalties.Count(p => p.PlanPositionId == positionId));
        }

        // A second login → no further deduction (unique index + existence check).
        var childAgain = await TestApi.ChildAsync(factory, childId, "7001");
        var wallet2 = await JsonAsync(await childAgain.GetAsync("/api/v1/student/me/points"));
        Assert.Equal(-40, wallet2.GetProperty("coins").GetInt32());
    }

    /// <summary>
    /// The ledger text of the penalty names the <b>cadence</b> of the missed mandatory goal – "daily goal"
    /// or "weekly goal". That is all the child learns about the deduction in the points history; if the two
    /// are swapped, no entry is wrong, but the justification is a lie (docs/testplan.md, injection B08).
    /// <para>
    /// Both cadences run in <b>one</b> test against <b>one</b> child, and the assignment goes via the
    /// plan title in the text. Two separate checks "some entry says daily goal" and "some entry says
    /// weekly goal" would be blind to exactly the swap meant here – both texts would occur either way.
    /// </para>
    /// </summary>
    [Fact]
    public async Task MalusBuchungstext_BenenntTagesZielUndWochenziel_JeweilsRichtig()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var childId = await FreshChildIdAsync(father, "7004");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        SeedPenaltyPlan(factory, childId, exerciseId, today.AddDays(-2), penaltyCoins: 5, title: "Tages-Plan");
        // A week only counts as missed once it is fully closed (Sunday < today) - the start therefore lies 14
        // days back, so that at least one closed week falls within the runtime.
        SeedPenaltyPlan(factory, childId, exerciseId, today.AddDays(-14), penaltyCoins: 7,
            cadence: GoalCadence.Weekly, title: "Wochen-Plan", durationDays: 20);

        var child = await TestApi.ChildAsync(factory, childId, "7004");
        var entries = await JsonAsync(await child.GetAsync("/api/v1/student/me/points/entries"));
        var reasons = entries.EnumerateArray()
            .Where(e => e.GetProperty("kind").GetString() == "GoalPenalty")
            .Select(e => e.GetProperty("reason").GetString()!)
            .ToList();

        Assert.All(reasons.Where(r => r.Contains("[Tages-Plan")), r => Assert.Contains("Tagesziel gerissen", r));
        Assert.All(reasons.Where(r => r.Contains("[Wochen-Plan")), r => Assert.Contains("Wochenziel gerissen", r));
        // Not vacuously green: both cadences must have been settled at all.
        Assert.Contains(reasons, r => r.Contains("[Tages-Plan"));
        Assert.Contains(reasons, r => r.Contains("[Wochen-Plan"));
    }

    /// <summary>Marks the first <paramref name="count"/> items of the position as introduced on <paramref name="day"/>.</summary>
    private static void Introduce(PuglingWebAppFactory f, int positionId, int count, DateOnly day)
    {
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        for (var i = 0; i < count; i++)
            db.PositionItemProgress.Add(new PositionItemProgress { PlanPositionId = positionId, ItemIndex = i, IntroducedAt = day, DueOn = day });
        db.SaveChanges();
    }

    /// <summary>The period starts of the penalties booked for the position.</summary>
    private static List<DateOnly> PenaltyDays(PuglingWebAppFactory f, int positionId)
    {
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        return [.. db.PositionGoalPenalties.Where(p => p.PlanPositionId == positionId).Select(p => p.PeriodStart).OrderBy(d => d)];
    }

    private static readonly (string, string)[] FourWords = [("one", "eins"), ("two", "zwei"), ("three", "drei"), ("four", "vier")];

    /// <summary>
    /// B-432 AK 1 and 3: on the introduction day (the position's first due day) the child introduced every item but
    /// passed no test - no penalty for that day; the second day without a test is penalised as before. AK 1 is red when
    /// the exception is missing or checks the wrong day; the AK 3 half (green on the original code, intent statement)
    /// turns red when every day with introductions is spared.
    /// </summary>
    [Fact]
    public async Task Einfuehrungstag_MitAllenEingefuehrten_KostetKeinenMalus_DerZweiteTagSchon()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, FourWords);
        var childId = await FreshChildIdAsync(father, "7011");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var first = today.AddDays(-2);
        var (_, positionId) = SeedPenaltyPlan(factory, childId, exerciseId, first, penaltyCoins: 5,
            createdAt: first.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc));
        Introduce(factory, positionId, 4, first);
        // Day two: the child introduces nothing new (there is nothing left) and passes no test.
        await TestApi.ChildAsync(factory, childId, "7011");

        Assert.Equal([today.AddDays(-1)], PenaltyDays(factory, positionId));
    }

    /// <summary>
    /// B-432 AK 2: on the introduction day the child introduced less than the portion (3 of 4) - the penalty falls as
    /// before. Green on the original code (intent statement); red when the exception looks only at the date and not at
    /// the introductions.
    /// </summary>
    [Fact]
    public async Task Einfuehrungstag_OhneVolleEinfuehrung_KostetDenMalus()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, FourWords);
        var childId = await FreshChildIdAsync(father, "7012");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var first = today.AddDays(-1);
        var (_, positionId) = SeedPenaltyPlan(factory, childId, exerciseId, first, penaltyCoins: 5,
            createdAt: first.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc));
        Introduce(factory, positionId, 3, first);
        await TestApi.ChildAsync(factory, childId, "7012");

        Assert.Equal([first], PenaltyDays(factory, positionId));
    }

    /// <summary>
    /// B-432 AK 4: with a daily dose of 3 out of 10 items, introducing 3 on the introduction day is the whole portion.
    /// Red when the rule demands the full pool and thus never applies to dosed positions.
    /// </summary>
    [Fact]
    public async Task Einfuehrungstag_MitTagesdosis_GenuegtDiePortion()
    {
        var father = await TestApi.AdultAsync(factory);
        var words = Enumerable.Range(1, 10).Select(i => ($"word{i}", $"Wort{i}")).ToArray();
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, words);
        var childId = await FreshChildIdAsync(father, "7013");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var first = today.AddDays(-1);
        var (_, positionId) = SeedPenaltyPlan(factory, childId, exerciseId, first, penaltyCoins: 5,
            createdAt: first.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc), newItemsPerDay: 3);
        Introduce(factory, positionId, 3, first);
        await TestApi.ChildAsync(factory, childId, "7013");

        Assert.Empty(PenaltyDays(factory, positionId));
    }

    /// <summary>
    /// B-432 AK 6: a weekly position stays unchanged - its first week without a passed test is penalised even though
    /// every item was introduced on the first day. Green on the original code (intent statement); red when the exception
    /// skips the cadence check.
    /// </summary>
    [Fact]
    public async Task WoechentlichePosition_ErsteWocheOhneTest_KostetWeiterDenMalus()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, FourWords);
        var childId = await FreshChildIdAsync(father, "7014");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        // A Monday at least a full week back, so the first week is closed and starts on the introduction day.
        var first = today.AddDays(-7 - (((int)today.DayOfWeek + 6) % 7));
        var (_, positionId) = SeedPenaltyPlan(factory, childId, exerciseId, first, penaltyCoins: 5,
            cadence: GoalCadence.Weekly, durationDays: 20, createdAt: first.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc));
        Introduce(factory, positionId, 4, first);
        await TestApi.ChildAsync(factory, childId, "7014");

        Assert.Contains(first, PenaltyDays(factory, positionId));
    }

    /// <summary>
    /// B-432 AK 7 (review finding): a position created on Wednesday in a plan that started on Monday owes nothing for
    /// Monday and Tuesday - settlement starts at the position's first due day, as the introduction-day rule assumes.
    /// Red when the settlement window starts at the plan start again: then Monday and Tuesday are penalised.
    /// </summary>
    [Fact]
    public async Task Position_SpaeterAngelegt_ZahltKeinenMalusFuerTageDavor()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, FourWords);
        var childId = await FreshChildIdAsync(father, "7015");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        // The Monday of the previous week, so Monday to Sunday of that week are all closed.
        var monday = today.AddDays(-7 - (((int)today.DayOfWeek + 6) % 7));
        var wednesday = monday.AddDays(2);
        var (_, positionId) = SeedPenaltyPlan(factory, childId, exerciseId, monday, penaltyCoins: 5,
            createdAt: wednesday.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc));
        await TestApi.ChildAsync(factory, childId, "7015");

        var days = PenaltyDays(factory, positionId);
        Assert.DoesNotContain(monday, days);
        Assert.DoesNotContain(monday.AddDays(1), days);
        // Not vacuously green: settlement does run from creation on - Wednesday, the introduction day, is penalised
        // because nothing was introduced.
        Assert.Contains(wednesday, days);
    }

    /// <summary>
    /// B-535 AK 5: a plan set up days before the child first plays costs nothing for those days; from the first play
    /// day on every position counts, also one the child never opened. Red when the window starts at the plan start
    /// again (the days before the first play are penalised) or when it is taken per position (the second, never opened
    /// position goes unpunished).
    /// </summary>
    [Fact]
    public async Task Malus_BeginntErstMitDemErstenSpieltag_GiltDannFuerJedePosition()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, FourWords);
        var childId = await FreshChildIdAsync(father, "7016");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = today.AddDays(-6);
        var firstPlay = start.AddDays(3);
        var (planId, positionId) = SeedPenaltyPlan(factory, childId, exerciseId, start, penaltyCoins: 5,
            playedFromStart: false);
        int neverOpened;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            var second = new PlanPosition
            {
                StudyPlanId = planId, ExerciseId = exerciseId, Order = 1, Cadence = GoalCadence.Daily, PointsGoalMet = 20,
                PenaltyCoins = 5, UseLeitner = true, CreatedAt = start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            };
            db.PlanPositions.Add(second);
            db.SaveChanges();
            neverOpened = second.Id;
        }
        Played(factory, planId, positionId, firstPlay);
        await TestApi.ChildAsync(factory, childId, "7016");

        var days = PenaltyDays(factory, positionId);
        for (var d = start; d < firstPlay; d = d.AddDays(1)) Assert.DoesNotContain(d, days);
        Assert.Contains(firstPlay, days);
        Assert.Contains(firstPlay, PenaltyDays(factory, neverOpened));
        Assert.DoesNotContain(start, PenaltyDays(factory, neverOpened));
    }

    /// <summary>
    /// B-535 review finding: the introduction-day spare (B-432) moves with the penalty window. A plan set up days before
    /// the child first plays: on the first play day the child introduces the whole portion and owes nothing; the next
    /// missed day costs as usual. Red when the introduction day stays at the plan start (then the first play day, the
    /// first day settled at all, is penalised although the child did what that day asks).
    /// </summary>
    [Fact]
    public async Task Einfuehrungstag_Ist_Der_Erste_Spieltag_Wenn_Der_Plan_Frueher_Begann()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, FourWords);
        var childId = await FreshChildIdAsync(father, "7018");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = today.AddDays(-5);
        var firstPlay = start.AddDays(2);
        var (_, positionId) = SeedPenaltyPlan(factory, childId, exerciseId, start, penaltyCoins: 5, playedFromStart: false);
        Introduce(factory, positionId, 4, firstPlay);
        await TestApi.ChildAsync(factory, childId, "7018");

        var days = PenaltyDays(factory, positionId);
        Assert.DoesNotContain(firstPlay, days);
        Assert.Contains(firstPlay.AddDays(1), days);
    }

    /// <summary>
    /// B-535 AK 5, the empty end: a plan the child never played costs nothing - and the father's own preview test does
    /// not start the clock. Red when a supervisor attempt counts as play, or when no play day means "since the start".
    /// </summary>
    [Fact]
    public async Task Malus_OhneSpielDesKindes_AuchNachVaterTest_BleibtAus()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, FourWords);
        var childId = await FreshChildIdAsync(father, "7017");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = today.AddDays(-4);
        var (planId, positionId) = SeedPenaltyPlan(factory, childId, exerciseId, start, penaltyCoins: 5,
            playedFromStart: false);
        Played(factory, planId, positionId, start, bySupervisor: true);
        await TestApi.ChildAsync(factory, childId, "7017");

        Assert.Empty(PenaltyDays(factory, positionId));
    }

    [Fact]
    public async Task InaktiverPlan_ErzeugtKeinenMalus()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var childId = await FreshChildIdAsync(father, "7003");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        SeedPenaltyPlan(factory, childId, exerciseId, today.AddDays(-2), penaltyCoins: 50, active: false);

        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/points", new { amount = 60, reason = "Start" }))
            .EnsureSuccessStatusCode();

        // The supervisor had the plan switched off - no penalty for days on which learning was not allowed.
        var child = await TestApi.ChildAsync(factory, childId, "7003");
        var wallet = await JsonAsync(await child.GetAsync("/api/v1/student/me/points"));
        Assert.Equal(60, wallet.GetProperty("coins").GetInt32());
    }

    [Fact]
    public async Task VaterSchenkt_MuenzenUndGems_LandenImWallet()
    {
        var father = await TestApi.AdultAsync(factory);
        var childId = await FreshChildIdAsync(father, "7002");

        // Give away coins (the default currency) and - newly - gems through the same endpoint.
        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/points",
            new { amount = 25, reason = "Taschengeld", currency = "Coins" })).EnsureSuccessStatusCode();
        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/points",
            new { amount = 15, reason = "Extra-Gems", currency = "Gems" })).EnsureSuccessStatusCode();

        var child = await TestApi.ChildAsync(factory, childId, "7002");
        var wallet = await JsonAsync(await child.GetAsync("/api/v1/student/me/points"));
        Assert.Equal(25, wallet.GetProperty("coins").GetInt32());
        Assert.Equal(15, wallet.GetProperty("gems").GetInt32());
    }
}
