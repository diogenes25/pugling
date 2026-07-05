using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Prüft den Schnelle-Antwort-Bonus einer Position: serverseitig aus der Zeit seit der letzten Antwort
/// gemessen, per Positions-Einstellung (<c>SpeedThresholdSeconds</c>/<c>SpeedBonusPoints</c>)
/// konfigurierbar. Die erste Karte einer Sitzung hat keinen Vorgänger und darum keinen Bonus.
/// </summary>
public class SpeedBonusTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    // Combo bewusst aus (Schwelle 0), damit nur der Speed-Bonus wirkt.
    private async Task<(int planId, int positionId, int sessionId)> SetupAsync(int thresholdSeconds, int bonus)
    {
        var father = await TestApi.FatherAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.SelfAssess,
            comboThreshold: 0, speedThresholdSeconds: thresholdSeconds, speedBonusPoints: bonus);
        var child = await TestApi.ChildAsync(factory);
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);
        return (planId, positionId, sessionId);
    }

    private static async Task<JsonElement> ReviewAsync(HttpClient child, int planId, int positionId, int sid, int itemIndex) =>
        await (await TestApi.PositionReviewAsync(child, planId, positionId, sid, itemIndex, wasKnown: true))
            .Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    public async Task SchnelleAntwort_ImZeitfenster_BringtBonus_ErsteKarteNicht()
    {
        var (planId, positionId, sid) = await SetupAsync(thresholdSeconds: 60, bonus: 4);
        var child = await TestApi.ChildAsync(factory);

        // Erste Karte: kein Vorgänger → keine Messung → kein Speed-Bonus.
        var first = await ReviewAsync(child, planId, positionId, sid, 0);
        Assert.Equal(0, first.GetProperty("speedBonus").GetInt32());

        // Über der Anti-Cheat-Untergrenze (1s), aber weit unter der Schwelle (60s) → Bonus.
        await Task.Delay(1200);
        var second = await ReviewAsync(child, planId, positionId, sid, 1);
        Assert.Equal(4, second.GetProperty("speedBonus").GetInt32());
    }

    [Fact]
    public async Task SpeedBonus_AbgeschaltetBeiSchwelleNull()
    {
        var (planId, positionId, sid) = await SetupAsync(thresholdSeconds: 0, bonus: 4);
        var child = await TestApi.ChildAsync(factory);

        await ReviewAsync(child, planId, positionId, sid, 0);
        await Task.Delay(1200);
        var second = await ReviewAsync(child, planId, positionId, sid, 1);
        Assert.Equal(0, second.GetProperty("speedBonus").GetInt32()); // Feature aus
    }
}
