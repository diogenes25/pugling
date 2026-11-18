using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-657 reverses B-538 AK 1: a plan position without a duty (<see cref="GoalCadence.None"/>, an extra exercise) pays for
/// practice like a duty does - base, combo and new-content points - and its box moves as before. The father's rule:
/// everything pays. Both cases compare against the same answer, so neither passes on a run that books nothing.
/// </summary>
public class KuerMitMuenzenTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private async Task<(int Awarded, int Box, int Coins, bool NewBonusSpent)> PlayOneRightAnswerAsync(GoalCadence cadence,
        bool useLeitner = true, int answers = 1)
    {
        var father = await TestApi.AdultAsync(factory);
        var pin = "6" + Random.Shared.Next(100, 999);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Kuer-Kind"), pin }));
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText,
            childId: childId, cadence: cadence, comboThreshold: 1, comboBonusPoints: 5, useLeitner: useLeitner);
        var child = await TestApi.ChildAsync(factory, childId, pin);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/practice-sessions";
        // The same word, right, in `answers` rounds of the same day - only the outcome of the last one is returned.
        var outcome = default(JsonElement);
        for (var i = 0; i < answers; i++)
        {
            var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { }));
            outcome = await (await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/review",
                new { itemIndex = 0, givenAnswer = "hello" })).Content.ReadFromJsonAsync<JsonElement>();
        }

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var coins = db.ChildPointsEntries.Where(e => e.ChildId == childId).Sum(e => e.Amount);
        var newBonusSpent = db.PositionItemProgress.Single(p => p.PlanPositionId == positionId && p.ItemIndex == 0).NewBonusPaidAt is not null;
        return (outcome.GetProperty("awarded").GetInt32() + outcome.GetProperty("comboBonus").GetInt32(),
            outcome.GetProperty("box").GetInt32(), coins, newBonusSpent);
    }

    /// <summary>
    /// Red when a position without a duty is fenced off from coins again, or pays less than the same answer on a duty.
    /// </summary>
    [Fact]
    public async Task Position_Ohne_Pflicht_Zahlt_Wie_Eine_Pflicht()
    {
        var free = await PlayOneRightAnswerAsync(GoalCadence.None);
        var duty = await PlayOneRightAnswerAsync(GoalCadence.Daily);

        Assert.True(free.Awarded > 0);
        Assert.Equal(duty.Awarded, free.Awarded);
        Assert.Equal(duty.Coins, free.Coins);
        Assert.Equal(TestApi.ClimbedBox + 1, free.Box);
        Assert.True(free.NewBonusSpent);
    }

    /// <summary>
    /// B-657: a position without a card box pays as well - Testjunge's extra exercises are such - and the word pays once
    /// a day, as on a box. Red if a hit without a box pays nothing, or if the same word pays again in the next round.
    /// </summary>
    [Fact]
    public async Task Position_Ohne_Karteikasten_Zahlt_Einmal_Am_Tag()
    {
        var first = await PlayOneRightAnswerAsync(GoalCadence.None, useLeitner: false);
        Assert.True(first.Awarded > 0);
        Assert.True(first.Coins > 0);
        Assert.True(first.NewBonusSpent);

        var twice = await PlayOneRightAnswerAsync(GoalCadence.None, useLeitner: false, answers: 2);
        Assert.Equal(0, twice.Awarded);
        Assert.Equal(first.Coins, twice.Coins);
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
