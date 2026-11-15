using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-538 AK 1: a plan position without a duty (<see cref="GoalCadence.None"/>) pays no coins for practice - no base, no
/// combo, no speed, no new-content points - while its box and learning state move as before. It was a second, uncapped way
/// to coins next to the bonus training. The counter-case (a daily position pays) keeps the test from passing on a run that
/// books nothing anyway.
/// </summary>
public class KuerOhneMuenzenTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private async Task<(int Awarded, int Box, int Coins, bool NewBonusSpent)> PlayOneRightAnswerAsync(GoalCadence cadence)
    {
        var father = await TestApi.AdultAsync(factory);
        var pin = "6" + Random.Shared.Next(100, 999);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Kuer-Kind"), pin }));
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText,
            childId: childId, cadence: cadence, comboThreshold: 1, comboBonusPoints: 5);
        var child = await TestApi.ChildAsync(factory, childId, pin);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/practice-sessions";
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { }));

        var outcome = await (await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/review",
            new { itemIndex = 0, givenAnswer = "hallo" })).Content.ReadFromJsonAsync<JsonElement>();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var coins = db.ChildPointsEntries.Where(e => e.ChildId == childId).Sum(e => e.Amount);
        var newBonusSpent = db.PositionItemProgress.Single(p => p.PlanPositionId == positionId && p.ItemIndex == 0).NewBonusPaidAt is not null;
        return (outcome.GetProperty("awarded").GetInt32() + outcome.GetProperty("comboBonus").GetInt32(),
            outcome.GetProperty("box").GetInt32(), coins, newBonusSpent);
    }

    /// <summary>
    /// Red when the cadence check is dropped (the free position would book base and combo coins), or when the free hit
    /// spends the new-content points it did not pay - a later duty on the same position would then never pay them.
    /// </summary>
    [Fact]
    public async Task Position_Ohne_Pflicht_Zahlt_Keine_Muenzen_Bewegt_Aber_Die_Box()
    {
        var (awarded, box, coins, newBonusSpent) = await PlayOneRightAnswerAsync(GoalCadence.None);

        Assert.Equal(0, awarded);
        Assert.Equal(0, coins);
        Assert.Equal(TestApi.ClimbedBox + 1, box);
        Assert.False(newBonusSpent);
    }

    /// <summary>Counter-case: the same answer on a daily position pays. Red when the check blocks every cadence.</summary>
    [Fact]
    public async Task Position_Mit_Pflicht_Zahlt_Weiter()
    {
        var (awarded, box, coins, newBonusSpent) = await PlayOneRightAnswerAsync(GoalCadence.Daily);

        Assert.True(awarded > 0);
        Assert.True(coins > 0);
        Assert.Equal(TestApi.ClimbedBox + 1, box);
        Assert.True(newBonusSpent);
    }
}
