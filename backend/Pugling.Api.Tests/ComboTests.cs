using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Prüft den serverseitig gezählten Combo-Bonus einer Lehrplan-Position und dass er über die
/// Positions-Einstellungen (<c>ComboThreshold</c>/<c>ComboBonusPoints</c>) konfigurierbar bzw.
/// abschaltbar ist. Stufe 2 = SelfAssess: ohne RequireTypedTest zählt das WasKnown-Flag voll.
/// </summary>
public class ComboTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private async Task<(int planId, int positionId, int sessionId)> SetupAsync(int threshold, int bonus)
    {
        var father = await TestApi.FatherAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father); // hello, goodbye → 2 fällige Items
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.SelfAssess,
            comboThreshold: threshold, comboBonusPoints: bonus);
        var child = await TestApi.ChildAsync(factory);
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);
        return (planId, positionId, sessionId);
    }

    private static async Task<JsonElement> ReviewAsync(HttpClient child, int planId, int positionId, int sid, int itemIndex) =>
        await (await TestApi.PositionReviewAsync(child, planId, positionId, sid, itemIndex, wasKnown: true))
            .Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    public async Task ComboBonus_LautPositionsEinstellung_BeiSchwelleErreicht()
    {
        var (planId, positionId, sid) = await SetupAsync(threshold: 2, bonus: 7);
        var child = await TestApi.ChildAsync(factory);

        var first = await ReviewAsync(child, planId, positionId, sid, 0);
        Assert.Equal(1, first.GetProperty("combo").GetInt32());
        Assert.Equal(0, first.GetProperty("comboBonus").GetInt32());

        var second = await ReviewAsync(child, planId, positionId, sid, 1);
        Assert.Equal(2, second.GetProperty("combo").GetInt32());
        Assert.Equal(7, second.GetProperty("comboBonus").GetInt32()); // Basis 7 × Meilenstein 1
    }

    [Fact]
    public async Task ComboBonus_WirdAlsEigenerPointKindGebucht()
    {
        var (planId, positionId, sid) = await SetupAsync(threshold: 2, bonus: 7);
        var child = await TestApi.ChildAsync(factory);

        await ReviewAsync(child, planId, positionId, sid, 0);
        await ReviewAsync(child, planId, positionId, sid, 1); // Schwelle 2 erreicht → Combo-Bonus

        var father = await TestApi.FatherAsync(factory);
        var points = await (await father.GetAsync("/api/v1/children/1/points"))
            .Content.ReadFromJsonAsync<JsonElement>();
        var combo = points.GetProperty("entries").EnumerateArray()
            .First(e => e.GetProperty("kind").GetString() == "Combo");
        Assert.Equal(7, combo.GetProperty("amount").GetInt32());
    }

    [Fact]
    public async Task ComboBonus_AbgeschaltetBeiSchwelleNull()
    {
        var (planId, positionId, sid) = await SetupAsync(threshold: 0, bonus: 7);
        var child = await TestApi.ChildAsync(factory);

        await ReviewAsync(child, planId, positionId, sid, 0);
        var second = await ReviewAsync(child, planId, positionId, sid, 1);

        Assert.Equal(2, second.GetProperty("combo").GetInt32());
        Assert.Equal(0, second.GetProperty("comboBonus").GetInt32()); // Feature aus
    }
}
