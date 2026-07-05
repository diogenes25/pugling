using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>Integrationstests der serverseitigen Anti-Selbstbetrug-Zusagen im Positions-Motor.</summary>
public class AntiCheatTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private async Task<(int planId, int positionId)> SetupAsync(int stage = (int)TestStage.SelfAssess)
    {
        var father = await TestApi.FatherAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        return TestApi.SeedLeitnerPosition(factory, exerciseId, stage);
    }

    [Fact]
    public async Task Heartbeat_ClamptUebertriebeneSekunden()
    {
        var (planId, positionId) = await SetupAsync();
        var father = await TestApi.FatherAsync(factory);
        var sid = await TestApi.StartPositionSessionAsync(father, planId, positionId);

        var hb = await father.PostAsJsonAsync(
            $"{TestApi.PracticeBase(planId, positionId)}/{sid}/heartbeat", new { seconds = 1200, active = true });
        var session = await hb.Content.ReadFromJsonAsync<JsonElement>();

        // 1200 s wären 20 min; pro Heartbeat sind höchstens 120 s anrechenbar.
        Assert.Equal(120, session.GetProperty("activeSeconds").GetInt32());
    }

    [Fact]
    public async Task Sohn_KannTeststufeNichtWaehlen_FahrplanStufeErzwungen()
    {
        var (planId, positionId) = await SetupAsync(stage: (int)TestStage.SelfAssess);
        var child = await TestApi.ChildAsync(factory);

        // Sohn fordert die Gratis-Anzeige-Stufe "ShowBoth" (1) an …
        var res = await child.PostAsJsonAsync(
            $"/api/v1/study-plans/{planId}/positions/{positionId}/tests", new { stage = (int)TestStage.ShowBoth });
        res.EnsureSuccessStatusCode();
        var attempt = await res.Content.ReadFromJsonAsync<JsonElement>();

        // … erzwungen wird aber die Positions-/Fahrplan-Stufe (SelfAssess = 2).
        Assert.Equal((int)TestStage.SelfAssess, attempt.GetProperty("stage").GetInt32());
    }

    [Fact]
    public async Task Sohn_KannFremdenTagNichtNachtragen_403()
    {
        var (planId, positionId) = await SetupAsync();
        var child = await TestApi.ChildAsync(factory);
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1).ToString("yyyy-MM-dd");

        var res = await child.PostAsJsonAsync(
            $"/api/v1/study-plans/{planId}/positions/{positionId}/tests", new { day = yesterday });

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Vater_DarfFremdenTagNachtragen()
    {
        var (planId, positionId) = await SetupAsync();
        var father = await TestApi.FatherAsync(factory);
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1).ToString("yyyy-MM-dd");

        var res = await father.PostAsJsonAsync(
            $"/api/v1/study-plans/{planId}/positions/{positionId}/tests", new { day = yesterday });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }
}
