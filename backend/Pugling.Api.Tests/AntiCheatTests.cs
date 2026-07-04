using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>Integrationstests der serverseitigen Anti-Selbstbetrug-Zusagen (gegen Rückfall abgesichert).</summary>
public class AntiCheatTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task Heartbeat_ClamptUebertriebeneSekunden()
    {
        var father = await TestApi.FatherAsync(factory);
        var planId = await TestApi.CreateVocabPlanAsync(father);
        var startRes = await father.PostAsJsonAsync($"/api/study-plans/{planId}/practice-sessions", new { });
        var sid = (await startRes.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var hb = await father.PostAsJsonAsync(
            $"/api/study-plans/{planId}/practice-sessions/{sid}/heartbeat", new { seconds = 1200, active = true });
        var progress = await hb.Content.ReadFromJsonAsync<JsonElement>();

        // 1200 s wären 20 min; pro Heartbeat sind höchstens 120 s (= 2 min) anrechenbar.
        Assert.Equal(2, progress.GetProperty("minutesPracticed").GetInt32());
    }

    [Fact]
    public async Task Sohn_KannTeststufeNichtWaehlen_FahrplanStufeErzwungen()
    {
        var father = await TestApi.FatherAsync(factory);
        var planId = await TestApi.CreateVocabPlanAsync(father);
        var child = await TestApi.ChildAsync(factory);

        // Sohn fordert die Gratis-Anzeige-Stufe "ShowBoth" an …
        var res = await child.PostAsJsonAsync($"/api/study-plans/{planId}/tests", new { stage = "ShowBoth" });
        res.EnsureSuccessStatusCode();
        var attempt = await res.Content.ReadFromJsonAsync<JsonElement>();

        // … erzwungen wird aber die Fahrplan-/Default-Stufe (SelfAssess).
        Assert.Equal("SelfAssess", attempt.GetProperty("stage").GetString());
    }

    [Fact]
    public async Task Sohn_KannFremdenTagNichtNachtragen_403()
    {
        var father = await TestApi.FatherAsync(factory);
        var planId = await TestApi.CreateVocabPlanAsync(father);
        var child = await TestApi.ChildAsync(factory);
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1).ToString("yyyy-MM-dd");

        var res = await child.PostAsJsonAsync($"/api/study-plans/{planId}/tests", new { stage = "SelfAssess", day = yesterday });

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Vater_DarfFremdenTagNachtragen()
    {
        var father = await TestApi.FatherAsync(factory);
        var planId = await TestApi.CreateVocabPlanAsync(father);
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1).ToString("yyyy-MM-dd");

        // Gegenprobe: der Vater darf einen vergangenen Tag nachtragen.
        var res = await father.PostAsJsonAsync($"/api/study-plans/{planId}/tests", new { stage = "SelfAssess", day = yesterday });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }
}
