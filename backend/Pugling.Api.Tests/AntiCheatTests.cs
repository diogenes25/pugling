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

    [Fact]
    public async Task Sohn_KannInaktivenPlanNichtUeben_403()
    {
        var (planId, positionId) = await SetupAsync();
        var father = await TestApi.FatherAsync(factory);
        (await father.PatchAsJsonAsync($"/api/v1/study-plans/{planId}", new { active = false })).EnsureSuccessStatusCode();

        // Kein Cherry-Picking: den deaktivierten Plan kann der Sohn nicht mehr üben.
        var child = await TestApi.ChildAsync(factory);
        var res = await child.PostAsJsonAsync(TestApi.PracticeBase(planId, positionId), new { });

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Sohn_KannInaktivenPlanNichtTesten_403()
    {
        var (planId, positionId) = await SetupAsync();
        var father = await TestApi.FatherAsync(factory);
        (await father.PatchAsJsonAsync($"/api/v1/study-plans/{planId}", new { active = false })).EnsureSuccessStatusCode();

        var child = await TestApi.ChildAsync(factory);
        var res = await child.PostAsJsonAsync($"/api/v1/study-plans/{planId}/positions/{positionId}/tests", new { });

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Vater_DarfInaktivenPlanTrotzdemDurchspielen()
    {
        var (planId, positionId) = await SetupAsync();
        var father = await TestApi.FatherAsync(factory);
        (await father.PatchAsJsonAsync($"/api/v1/study-plans/{planId}", new { active = false })).EnsureSuccessStatusCode();

        // Der Vater bleibt für Vorschau/Nachtrag ausgenommen – auch bei inaktivem Plan.
        var res = await father.PostAsJsonAsync(TestApi.PracticeBase(planId, positionId), new { });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Sohn_ListeZeigtInaktivenPlanNicht()
    {
        var (planId, _) = await SetupAsync();
        var father = await TestApi.FatherAsync(factory);
        (await father.PatchAsJsonAsync($"/api/v1/study-plans/{planId}", new { active = false })).EnsureSuccessStatusCode();

        var child = await TestApi.ChildAsync(factory);
        var plans = await (await child.GetAsync("/api/v1/study-plans")).Content.ReadFromJsonAsync<JsonElement>();

        Assert.DoesNotContain(plans.EnumerateArray(), p => p.GetProperty("id").GetInt32() == planId);
    }

    [Fact]
    public async Task AktivierenEinesPlans_DeaktiviertAndereDesKindes()
    {
        var father = await TestApi.FatherAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planA, _) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.SelfAssess);
        var (planB, _) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.SelfAssess);

        // Beide werden direkt aktiv geseedet; das Aktivieren von A muss B stilllegen (ein aktiver Plan je Kind).
        (await father.PatchAsJsonAsync($"/api/v1/study-plans/{planA}", new { active = true })).EnsureSuccessStatusCode();

        var b = await (await father.GetAsync($"/api/v1/study-plans/{planB}")).Content.ReadFromJsonAsync<JsonElement>();
        var a = await (await father.GetAsync($"/api/v1/study-plans/{planA}")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(b.GetProperty("active").GetBoolean());
        Assert.True(a.GetProperty("active").GetBoolean());
    }

    [Fact]
    public async Task Sohn_KannMitOffenerSession_InaktivenPlanNichtWeiterUeben_403()
    {
        var (planId, positionId) = await SetupAsync();
        var child = await TestApi.ChildAsync(factory);
        // Session wird gestartet, solange der Plan noch aktiv ist …
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);

        // … dann legt der Vater den Plan still (oder er läuft ab).
        var father = await TestApi.FatherAsync(factory);
        (await father.PatchAsJsonAsync($"/api/v1/study-plans/{planId}", new { active = false })).EnsureSuccessStatusCode();

        // Über die noch offene Session darf der Sohn nicht weiter bepunktet werden.
        var res = await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 0, wasKnown: true);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Sohn_KannOffenenTestversuch_AufInaktivemPlanNichtAbschliessen_403()
    {
        var (planId, positionId) = await SetupAsync();
        var child = await TestApi.ChildAsync(factory);
        // Testversuch wird gestartet, solange der Plan aktiv ist …
        var start = await child.PostAsJsonAsync($"/api/v1/study-plans/{planId}/positions/{positionId}/tests", new { });
        start.EnsureSuccessStatusCode();
        var attempt = await start.Content.ReadFromJsonAsync<JsonElement>();
        var attemptId = attempt.GetProperty("attemptId").GetInt32();
        var answers = attempt.GetProperty("items").EnumerateArray()
            .Select(i => new { itemIndex = i.GetProperty("itemIndex").GetInt32(), wasKnown = true }).ToArray();

        // … dann wird der Plan stillgelegt.
        var father = await TestApi.FatherAsync(factory);
        (await father.PatchAsJsonAsync($"/api/v1/study-plans/{planId}", new { active = false })).EnsureSuccessStatusCode();

        // Das Einreichen (und Bepunkten) des offenen Versuchs muss scheitern.
        var res = await child.PostAsJsonAsync(
            $"/api/v1/study-plans/{planId}/positions/{positionId}/tests/{attemptId}/submit", new { answers });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Hoerstufe_RueckwaertsRichtung_GibtKeineAudioquellePreis()
    {
        var father = await TestApi.FatherAsync(factory);
        var (id, key) = await TestApi.CreateStoreVocabAsync(father, "hello", "hallo");
        (await father.PatchAsJsonAsync($"/api/v1/learn/vocabulary/{id}",
            new { pronunciationAudioUrl = "https://example.test/hello.mp3" })).EnsureSuccessStatusCode();

        // Rückwärts-Übung: nach dem Tausch ist das (vorgelesene) Wort die Lösung. Die Hör-Stufe darf die
        // Audioquelle dann NICHT mitgeben, sonst spräche sie die Antwort vor.
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/learn/subjects", new { name = "Audio-Ref" }));
        var chapterId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/learn/subjects/{subjectId}/chapters", new { name = "U1", orderIndex = 1 }));
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/learn/subjects/{subjectId}/chapters/{chapterId}/vocabulary", new
            {
                title = "Rueckwaerts",
                orderIndex = 1,
                rewardPoints = 10,
                config = new { direction = "back-to-front", refs = new[] { key } },
            }));

        var data = await father.GetFromJsonAsync<JsonElement>($"/api/v1/learn/exercises/{exerciseId}/preview?stage=5");
        Assert.Equal(5, data.GetProperty("stage").GetInt32());
        var item = data.GetProperty("items").EnumerateArray().First();
        Assert.Equal(JsonValueKind.Null, item.GetProperty("audioUrl").ValueKind);
    }
}
