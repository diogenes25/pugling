using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Verifies the fast-answer bonus of a position: measured server-side from the time since the last
/// answer, configurable per position setting (<c>SpeedThresholdSeconds</c>/<c>SpeedBonusPoints</c>).
/// The first card of a session has no predecessor and therefore no bonus.
/// <para>
/// The test host's clock is <b>frozen</b> here (<see cref="TestClock"/>): the answer time is fed in,
/// not hoped for. This used to have <c>Task.Delay(1200)</c> and – worse – the silent assumption that
/// two consecutive requests would fall under the anti-farming floor of one second. That broke under
/// load: the test failed alongside two injected defects that concerned the <c>ShopService</c> and had
/// nothing to do with bonus points (docs/testplan.md, stage 3). A flake right here is especially
/// costly, because it looks like a points regression.
/// </para>
/// </summary>
public class SpeedBonusTests : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory;

    public SpeedBonusTests(PuglingWebAppFactory factory)
    {
        _factory = factory;
        // Ab hier bewegt sich die Zeit dieses Hosts nur noch über Advance(...).
        _factory.Clock.FreezeNow();
    }

    // Combo bewusst aus (Schwelle 0), damit nur der Speed-Bonus wirkt.
    private async Task<(int planId, int positionId, int sessionId)> SetupAsync(int thresholdSeconds, int bonus)
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.SelfAssess,
            comboThreshold: 0, speedThresholdSeconds: thresholdSeconds, speedBonusPoints: bonus);
        var child = await TestApi.ChildAsync(_factory);
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);
        return (planId, positionId, sessionId);
    }

    private async Task<JsonElement> ReviewAsync(HttpClient child, int planId, int positionId, int sid, int itemIndex) =>
        await (await TestApi.PositionReviewAsync(child, planId, positionId, sid, itemIndex, wasKnown: true))
            .Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    public async Task SchnelleAntwort_ImZeitfenster_BringtBonus_ErsteKarteNicht()
    {
        var (planId, positionId, sid) = await SetupAsync(thresholdSeconds: 60, bonus: 4);
        var child = await TestApi.ChildAsync(_factory);

        // Erste Karte: kein Vorgänger → keine Messung → kein Speed-Bonus.
        var first = await ReviewAsync(child, planId, positionId, sid, 0);
        Assert.Equal(0, first.GetProperty("speedBonus").GetInt32());

        // Über der Anti-Cheat-Untergrenze (1s), aber weit unter der Schwelle (60s) → Bonus.
        _factory.Clock.Advance(TimeSpan.FromSeconds(2));
        var second = await ReviewAsync(child, planId, positionId, sid, 1);
        Assert.Equal(4, second.GetProperty("speedBonus").GetInt32());
    }

    [Fact]
    public async Task SpeedBonus_AbgeschaltetBeiSchwelleNull()
    {
        var (planId, positionId, sid) = await SetupAsync(thresholdSeconds: 0, bonus: 4);
        var child = await TestApi.ChildAsync(_factory);

        await ReviewAsync(child, planId, positionId, sid, 0);
        _factory.Clock.Advance(TimeSpan.FromSeconds(2));
        var second = await ReviewAsync(child, planId, positionId, sid, 1);
        Assert.Equal(0, second.GetProperty("speedBonus").GetInt32()); // Feature aus
    }

    /// <summary>
    /// The floor is an anti-farming rule: it prevents points from double-clicks. It is checked
    /// <b>right at</b> its boundary – 0.9s yields nothing, 1.0s yields the bonus. This exact pair was
    /// unreachable with a wall clock; without the second line, the boundary would only be covered on
    /// one side and could drift upward without any test failing.
    /// </summary>
    [Theory]
    [InlineData(900, 0)]
    [InlineData(1000, 4)]
    public async Task AntiCheatUntergrenze_GiltGenauAbEinerSekunde(int abstandMs, int erwarteterBonus)
    {
        var (planId, positionId, sid) = await SetupAsync(thresholdSeconds: 60, bonus: 4);
        var child = await TestApi.ChildAsync(_factory);

        await ReviewAsync(child, planId, positionId, sid, 0);
        _factory.Clock.Advance(TimeSpan.FromMilliseconds(abstandMs));
        var second = await ReviewAsync(child, planId, positionId, sid, 1);
        Assert.Equal(erwarteterBonus, second.GetProperty("speedBonus").GetInt32());
    }

    /// <summary>
    /// And above the threshold there is nothing left – the other edge of the same window.
    /// </summary>
    [Fact]
    public async Task ZuLangsameAntwort_UeberDerSchwelle_BringtKeinenBonus()
    {
        var (planId, positionId, sid) = await SetupAsync(thresholdSeconds: 10, bonus: 4);
        var child = await TestApi.ChildAsync(_factory);

        await ReviewAsync(child, planId, positionId, sid, 0);
        _factory.Clock.Advance(TimeSpan.FromSeconds(11));
        var second = await ReviewAsync(child, planId, positionId, sid, 1);
        Assert.Equal(0, second.GetProperty("speedBonus").GetInt32());
    }
}
