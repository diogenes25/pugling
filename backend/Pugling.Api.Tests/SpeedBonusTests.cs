using System.Net;
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
        // From here on this host's time only moves through Advance(...).
        _factory.Clock.FreezeNow();
    }

    // Combo deliberately off (threshold 0), so that only the speed bonus takes effect.
    private async Task<(int planId, int positionId, int sessionId)> SetupAsync(int thresholdSeconds, int bonus)
    {
        var father = await TestApi.AdultAsync(_factory);
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

        // The first card: no predecessor → no measurement → no speed bonus.
        var first = await ReviewAsync(child, planId, positionId, sid, 0);
        Assert.Equal(0, first.GetProperty("speedBonus").GetInt32());

        // Above the anti-cheat lower bound (1s) but far below the threshold (60s) → a bonus.
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

    // ---- B-530: the answer time counts from the visible card, not from the previous answer ----

    private Task<HttpResponseMessage> ShownAsync(HttpClient client, int planId, int positionId, int sid) =>
        client.PostAsync($"{TestApi.PracticeBase(planId, positionId)}/{sid}/shown", null);

    /// <summary>
    /// B-530 AK 1: ten seconds of reading the sheet, then "Weiter" stamps the card as shown, then a two-second answer
    /// earns the bonus (threshold 5 s). Red when the review still measures from the previous answer (12 s).
    /// </summary>
    [Fact]
    public async Task Antwortzeit_Zaehlt_Ab_Der_Sichtbaren_Karte()
    {
        var (planId, positionId, sid) = await SetupAsync(thresholdSeconds: 5, bonus: 4);
        var child = await TestApi.ChildAsync(_factory);

        await ReviewAsync(child, planId, positionId, sid, 0);
        _factory.Clock.Advance(TimeSpan.FromSeconds(10));
        Assert.Equal(HttpStatusCode.NoContent, (await ShownAsync(child, planId, positionId, sid)).StatusCode);
        _factory.Clock.Advance(TimeSpan.FromSeconds(2));
        var second = await ReviewAsync(child, planId, positionId, sid, 1);

        Assert.Equal(4, second.GetProperty("speedBonus").GetInt32());
    }

    /// <summary>
    /// B-530 AK 2: the anti-farming floor of one second also holds after the stamp - half a second from the visible
    /// card pays nothing. Red when the floor no longer applies to the stamped measurement.
    /// </summary>
    [Fact]
    public async Task Untergrenze_Gilt_Auch_Ab_Dem_Stempel()
    {
        var (planId, positionId, sid) = await SetupAsync(thresholdSeconds: 5, bonus: 4);
        var child = await TestApi.ChildAsync(_factory);

        await ReviewAsync(child, planId, positionId, sid, 0);
        _factory.Clock.Advance(TimeSpan.FromSeconds(2));
        await ShownAsync(child, planId, positionId, sid);
        _factory.Clock.Advance(TimeSpan.FromSeconds(0.5));
        var second = await ReviewAsync(child, planId, positionId, sid, 1);

        Assert.Equal(0, second.GetProperty("speedBonus").GetInt32());
    }

    /// <summary>
    /// B-530 AK 3: one stamp per card. A second "shown" five seconds later must not restart the clock - measured from
    /// the first stamp the answer took 7 s, over the threshold. Red when a later stamp overwrites the earlier one
    /// (then it would be 2 s and pay).
    /// </summary>
    [Fact]
    public async Task Zweiter_Stempel_Aendert_Nichts()
    {
        var (planId, positionId, sid) = await SetupAsync(thresholdSeconds: 5, bonus: 4);
        var child = await TestApi.ChildAsync(_factory);

        await ReviewAsync(child, planId, positionId, sid, 0);
        _factory.Clock.Advance(TimeSpan.FromSeconds(10));
        await ShownAsync(child, planId, positionId, sid);
        _factory.Clock.Advance(TimeSpan.FromSeconds(5));
        await ShownAsync(child, planId, positionId, sid);
        _factory.Clock.Advance(TimeSpan.FromSeconds(2));
        var second = await ReviewAsync(child, planId, positionId, sid, 1);

        Assert.Equal(0, second.GetProperty("speedBonus").GetInt32());
    }

    /// <summary>
    /// B-530 AK 4: without a stamp (old client, lost call) the old rule holds - three seconds after the previous answer
    /// pay. Red when the missing stamp changes the measurement.
    /// </summary>
    [Fact]
    public async Task Ohne_Stempel_Misst_Ab_Der_Vorigen_Antwort()
    {
        var (planId, positionId, sid) = await SetupAsync(thresholdSeconds: 5, bonus: 4);
        var child = await TestApi.ChildAsync(_factory);

        await ReviewAsync(child, planId, positionId, sid, 0);
        _factory.Clock.Advance(TimeSpan.FromSeconds(3));
        var second = await ReviewAsync(child, planId, positionId, sid, 1);

        Assert.Equal(4, second.GetProperty("speedBonus").GetInt32());
    }

    /// <summary>
    /// B-530 AK 5: the supervisor does not stamp (a preview pays nothing, and his click must not shape the child's
    /// measurement). Red when the role check is missing: the stamp would turn the child's 12 s into 2 s and pay.
    /// </summary>
    [Fact]
    public async Task Supervisor_Stempelt_Nicht()
    {
        var (planId, positionId, sid) = await SetupAsync(thresholdSeconds: 5, bonus: 4);
        var child = await TestApi.ChildAsync(_factory);
        var father = await TestApi.AdultAsync(_factory);

        await ReviewAsync(child, planId, positionId, sid, 0);
        _factory.Clock.Advance(TimeSpan.FromSeconds(10));
        Assert.Equal(HttpStatusCode.NoContent, (await ShownAsync(father, planId, positionId, sid)).StatusCode);
        _factory.Clock.Advance(TimeSpan.FromSeconds(2));
        var second = await ReviewAsync(child, planId, positionId, sid, 1);

        Assert.Equal(0, second.GetProperty("speedBonus").GetInt32());
    }
}
