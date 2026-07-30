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

    /// <summary>Directly seeds a plan with a mandatory position including penalty and (possibly past) start date.</summary>
    private static (int planId, int positionId) SeedPenaltyPlan(PuglingWebAppFactory f, int childId, int exerciseId,
        DateOnly start, int penaltyCoins, bool active = true, GoalCadence cadence = GoalCadence.Daily,
        string title = "Malus-Plan", int durationDays = 10)
    {
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var plan = new StudyPlan { ChildId = childId, Title = title, StartDate = start, EndDate = start.AddDays(durationDays), Active = active };
        plan.Positions.Add(new PlanPosition
        {
            ExerciseId = exerciseId,
            Order = 0,
            Cadence = cadence,
            PointsGoalMet = 20,
            PenaltyCoins = penaltyCoins,
            UseLeitner = true,
        });
        db.StudyPlans.Add(plan);
        db.SaveChanges();
        return (plan.Id, plan.Positions[0].Id);
    }

    private static async Task<int> FreshChildIdAsync(HttpClient father, string pin) =>
        await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Malus-Kind", pin }));

    [Fact]
    public async Task GerissenePflicht_ZiehtMuenzMalusAb_ErlaubtSchuld_UndIstIdempotent()
    {
        var father = await TestApi.FatherAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var childId = await FreshChildIdAsync(father, "7001");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var (_, positionId) = SeedPenaltyPlan(factory, childId, exerciseId, today.AddDays(-2), penaltyCoins: 50);

        // 60 Münzen Startguthaben; nie geübt → die zwei geschlossenen Tage (heute-2, heute-1) sind gerissen.
        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/points", new { amount = 60, reason = "Start" }))
            .EnsureSuccessStatusCode();

        // Der Kind-Login rechnet nach: 2 × 50 = 100 Malus → 60 - 100 = -40 (Schuld erlaubt).
        var child = await TestApi.ChildAsync(factory, childId, "7001");
        var wallet = await JsonAsync(await child.GetAsync("/api/v1/student/me/points"));
        Assert.Equal(-40, wallet.GetProperty("coins").GetInt32());

        // Genau zwei Malus-Buchungen à -50, Kategorie GoalPenalty.
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

        // Zweiter Login → kein erneuter Abzug (Unique-Index + Existenz-Check).
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
        var father = await TestApi.FatherAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var childId = await FreshChildIdAsync(father, "7004");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        SeedPenaltyPlan(factory, childId, exerciseId, today.AddDays(-2), penaltyCoins: 5, title: "Tages-Plan");
        // Eine Woche zählt erst als gerissen, wenn sie voll abgeschlossen ist (Sonntag < heute) – der Start
        // liegt darum 14 Tage zurück, damit mindestens eine geschlossene Woche in der Laufzeit liegt.
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
        // Nicht vakuum-grün: beide Rhythmen müssen überhaupt abgerechnet worden sein.
        Assert.Contains(reasons, r => r.Contains("[Tages-Plan"));
        Assert.Contains(reasons, r => r.Contains("[Wochen-Plan"));
    }

    [Fact]
    public async Task InaktiverPlan_ErzeugtKeinenMalus()
    {
        var father = await TestApi.FatherAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var childId = await FreshChildIdAsync(father, "7003");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        SeedPenaltyPlan(factory, childId, exerciseId, today.AddDays(-2), penaltyCoins: 50, active: false);

        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/points", new { amount = 60, reason = "Start" }))
            .EnsureSuccessStatusCode();

        // Der Vater hatte den Plan aus – kein Malus für Tage, an denen nicht gelernt werden durfte.
        var child = await TestApi.ChildAsync(factory, childId, "7003");
        var wallet = await JsonAsync(await child.GetAsync("/api/v1/student/me/points"));
        Assert.Equal(60, wallet.GetProperty("coins").GetInt32());
    }

    [Fact]
    public async Task VaterSchenkt_MuenzenUndGems_LandenImWallet()
    {
        var father = await TestApi.FatherAsync(factory);
        var childId = await FreshChildIdAsync(father, "7002");

        // Münzen (Default-Währung) und – neu – Gems über denselben Endpunkt verschenken.
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
