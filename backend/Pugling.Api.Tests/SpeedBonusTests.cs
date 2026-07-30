using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Prüft den Schnelle-Antwort-Bonus einer Position: serverseitig aus der Zeit seit der letzten Antwort
/// gemessen, per Positions-Einstellung (<c>SpeedThresholdSeconds</c>/<c>SpeedBonusPoints</c>)
/// konfigurierbar. Die erste Karte einer Sitzung hat keinen Vorgänger und darum keinen Bonus.
/// <para>
/// Die Uhr des Testhosts ist hier <b>eingefroren</b> (<see cref="TestClock"/>): die Antwortzeit wird
/// eingespeist, nicht erhofft. Vorher standen hier <c>Task.Delay(1200)</c> und – schlimmer – die stille
/// Annahme, zwei aufeinanderfolgende Requests lägen unter der Anti-Farming-Untergrenze von einer Sekunde.
/// Das riss unter Last: der Test fiel bei zwei Injektionen mit, die den <c>ShopService</c> betrafen und mit
/// Bonuspunkten nichts zu tun hatten (docs/testplan.md, Etappe 3). Ein Flake genau hier ist besonders
/// teuer, weil er wie ein Punkte-Regress aussieht.
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
    /// Die Untergrenze ist eine Anti-Farming-Regel: sie verhindert Punkte durch Doppelklicks. Geprüft wird
    /// sie <b>an</b> ihrer Grenze – 0,9 s bringt nichts, 1,0 s bringt den Bonus. Genau dieses Paar war mit
    /// der Wanduhr nicht erreichbar; ohne die zweite Zeile wäre die Grenze nur einseitig belegt und dürfte
    /// nach oben wandern, ohne dass ein Test fällt.
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
    /// Und oberhalb der Schwelle gibt es nichts mehr – die andere Kante desselben Fensters.
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
