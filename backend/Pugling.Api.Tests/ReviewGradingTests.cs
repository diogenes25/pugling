using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Sichert die server-autoritative Bewertung der Leitner-Übungsschleife (<c>/review</c>) ab:
/// der Sohn schickt seine Antwort, der Server prüft gegen die Lösung und vergibt darauf Punkte.
/// Ein gefälschtes "richtig" ist nicht mehr möglich, und die Übungskarten kommen lösungsfrei.
/// </summary>
public class ReviewGradingTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    // Leitner-Plan mit wählbarer Fahrplan-Stufe (Default Freitext=4, getippt → echte serverseitige Prüfung).
    private static async Task<int> LeitnerPlanAsync(HttpClient father, int defaultStage = 4, bool requireTyped = false) =>
        await TestApi.IdAsync(await father.PostAsJsonAsync("/api/study-plans", new
        {
            childId = 1,
            title = "Freitext-Leitner",
            method = "Vocabulary",
            durationDays = 5,
            contentKeys = new[] { "en_house_de_haus", "en_go_de_gehen" },
            useLeitner = true,
            defaultStage,
            requireTypedTest = requireTyped,
        }));

    private static async Task<(int session, List<int> ids)> StartAsync(HttpClient child, int planId)
    {
        var plan = await (await child.GetAsync($"/api/study-plans/{planId}")).Content.ReadFromJsonAsync<JsonElement>();
        var ids = plan.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("contentId").GetInt32()).ToList();
        var sid = (await (await child.PostAsJsonAsync($"/api/study-plans/{planId}/practice-sessions", new { }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        return (sid, ids);
    }

    private static Task<HttpResponseMessage> ReviewAsync(HttpClient child, int planId, int sid, int contentId, string? given) =>
        child.PostAsJsonAsync($"/api/study-plans/{planId}/practice-sessions/{sid}/review",
            new { contentId, givenAnswer = given });

    [Fact]
    public async Task RichtigeAntwort_WirdServerseitigGewertet_UndBringtPunkte()
    {
        var father = await TestApi.FatherAsync(factory);
        var planId = await LeitnerPlanAsync(father);
        var child = await TestApi.ChildAsync(factory);
        var (sid, ids) = await StartAsync(child, planId);

        // "house" → Übersetzung "Haus"; Normalisierung macht Groß-/Kleinschreibung egal.
        var res = await ReviewAsync(child, planId, sid, ids[0], "haus");
        res.EnsureSuccessStatusCode();
        var outcome = await res.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(outcome.GetProperty("wasCorrect").GetBoolean());
        Assert.Equal("Haus", outcome.GetProperty("expected").GetString());
        Assert.True(outcome.GetProperty("awarded").GetInt32() > 0);
        Assert.Equal(2, outcome.GetProperty("box").GetInt32()); // Box 1 → 2 nach richtiger Antwort
    }

    [Fact]
    public async Task FalscheAntwort_TrotzManipulationsversuch_BringtKeinePunkte()
    {
        var father = await TestApi.FatherAsync(factory);
        var planId = await LeitnerPlanAsync(father);
        var child = await TestApi.ChildAsync(factory);
        var (sid, ids) = await StartAsync(child, planId);

        // Der Client könnte früher "wasCorrect=true" behaupten – jetzt zählt nur die tatsächliche Antwort.
        var res = await ReviewAsync(child, planId, sid, ids[0], "falschlösung");
        res.EnsureSuccessStatusCode();
        var outcome = await res.Content.ReadFromJsonAsync<JsonElement>();

        Assert.False(outcome.GetProperty("wasCorrect").GetBoolean());
        Assert.Equal(0, outcome.GetProperty("awarded").GetInt32());
        Assert.Equal(1, outcome.GetProperty("box").GetInt32()); // falsch → zurück in Box 1
        Assert.Equal(0, outcome.GetProperty("combo").GetInt32());
    }

    [Fact]
    public async Task Selbsteinschaetzung_BeiRequireTypedTest_BringtKeinePunkte()
    {
        var father = await TestApi.FatherAsync(factory);
        // Fahrplan-Stufe SelfAssess (2), aber RequireTypedTest → Selbsteinschätzung zählt nicht.
        var planId = await LeitnerPlanAsync(father, defaultStage: 2, requireTyped: true);
        var child = await TestApi.ChildAsync(factory);
        var (sid, ids) = await StartAsync(child, planId);

        // wasKnown wird nur protokolliert (204), keine Punkte/Box-Bewegung.
        var res = await child.PostAsJsonAsync($"/api/study-plans/{planId}/practice-sessions/{sid}/review",
            new { contentId = ids[0], wasKnown = true });
        Assert.Equal(System.Net.HttpStatusCode.NoContent, res.StatusCode);

        // Gegenprobe: die Karte steht weiterhin in Box 1 (keine Bewegung).
        var plan = await (await child.GetAsync($"/api/study-plans/{planId}")).Content.ReadFromJsonAsync<JsonElement>();
        var box = plan.GetProperty("items").EnumerateArray()
            .First(i => i.GetProperty("contentId").GetInt32() == ids[0]).GetProperty("box").GetInt32();
        Assert.Equal(1, box);
    }

    [Fact]
    public async Task Stufe_NichtVomClientWaehlbar_KeinDowngradeAufSelbsteinschaetzung()
    {
        var father = await TestApi.FatherAsync(factory);
        var planId = await LeitnerPlanAsync(father); // Fahrplan-Stufe Freitext (getippt)
        var child = await TestApi.ChildAsync(factory);
        var (sid, ids) = await StartAsync(child, planId);

        // Manipulationsversuch: nur wasKnown ohne getippte Antwort. Der Server erzwingt die Freitext-Stufe
        // und bewertet gegen die Lösung → ohne givenAnswer schlicht falsch, keine Gratis-Punkte.
        var res = await child.PostAsJsonAsync($"/api/study-plans/{planId}/practice-sessions/{sid}/review",
            new { contentId = ids[0], wasKnown = true });
        res.EnsureSuccessStatusCode();
        var outcome = await res.Content.ReadFromJsonAsync<JsonElement>();

        Assert.False(outcome.GetProperty("wasCorrect").GetBoolean());
        Assert.Equal(0, outcome.GetProperty("awarded").GetInt32());
    }

    [Fact]
    public async Task Cards_LiefernKeineLoesung_FuerGetippteStufe()
    {
        var father = await TestApi.FatherAsync(factory);
        var planId = await LeitnerPlanAsync(father);
        var child = await TestApi.ChildAsync(factory);
        var (sid, _) = await StartAsync(child, planId);

        var res = await child.GetAsync($"/api/study-plans/{planId}/practice-sessions/{sid}/cards");
        res.EnsureSuccessStatusCode();
        var cards = await res.Content.ReadFromJsonAsync<JsonElement>();

        Assert.NotEmpty(cards.EnumerateArray());
        foreach (var card in cards.EnumerateArray())
        {
            // Freitext-Stufe: Prompt (Wort) ja, Lösung (Übersetzung) nein.
            Assert.False(string.IsNullOrEmpty(card.GetProperty("prompt").GetString()));
            Assert.Equal(JsonValueKind.Null, card.GetProperty("translation").ValueKind);
        }
    }
}
