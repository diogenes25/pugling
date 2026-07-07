using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Sichert die server-autoritative Bewertung der Leitner-Übungsschleife einer Position (<c>/review</c>) ab:
/// der Sohn schickt seine Antwort, der Server prüft gegen die Item-Lösung und vergibt darauf Punkte.
/// Ein gefälschtes "richtig" ist nicht möglich, und die Übungskarten kommen lösungsfrei.
/// </summary>
public class ReviewGradingTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    // Übung: hello→hallo, goodbye→tschüss. Fahrplan-Stufe wählbar (Freitext=4 → echte serverseitige Prüfung).
    private async Task<(int planId, int positionId, int sessionId)> SetupAsync(int stage = (int)TestStage.FreeText, bool requireTyped = false)
    {
        var father = await TestApi.FatherAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, stage, requireTypedTest: requireTyped);
        var child = await TestApi.ChildAsync(factory);
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);
        return (planId, positionId, sessionId);
    }

    private int BoxOf(int positionId, int itemIndex)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        return db.PositionItemProgress.SingleOrDefault(p => p.PlanPositionId == positionId && p.ItemIndex == itemIndex)?.Box ?? 1;
    }

    [Fact]
    public async Task RichtigeAntwort_WirdServerseitigGewertet_UndBringtPunkte()
    {
        var (planId, positionId, sid) = await SetupAsync();
        var child = await TestApi.ChildAsync(factory);

        // "hello" → Übersetzung "hallo"; Normalisierung macht Groß-/Kleinschreibung egal.
        var res = await TestApi.PositionReviewAsync(child, planId, positionId, sid, 0, givenAnswer: "hallo");
        res.EnsureSuccessStatusCode();
        var outcome = await res.Content.ReadFromJsonAsync<JsonElement>();

        JsonAssert.True(outcome, "wasCorrect");
        Assert.Equal("hallo", outcome.GetProperty("expected").GetString());
        Assert.True(outcome.GetProperty("awarded").GetInt32() > 0);
        Assert.Equal(2, outcome.GetProperty("box").GetInt32()); // Box 1 → 2 nach richtiger Antwort
    }

    [Fact]
    public async Task FalscheAntwort_TrotzManipulationsversuch_BringtKeinePunkte()
    {
        var (planId, positionId, sid) = await SetupAsync();
        var child = await TestApi.ChildAsync(factory);

        var res = await TestApi.PositionReviewAsync(child, planId, positionId, sid, 0, givenAnswer: "falschlösung");
        res.EnsureSuccessStatusCode();
        var outcome = await res.Content.ReadFromJsonAsync<JsonElement>();

        JsonAssert.False(outcome, "wasCorrect");
        Assert.Equal(0, outcome.GetProperty("awarded").GetInt32());
        Assert.Equal(1, outcome.GetProperty("box").GetInt32()); // falsch → zurück in Box 1
        Assert.Equal(0, outcome.GetProperty("combo").GetInt32());
    }

    [Fact]
    public async Task Selbsteinschaetzung_BeiRequireTypedTest_BringtKeinePunkte()
    {
        // Fahrplan-Stufe SelfAssess (2), aber RequireTypedTest → Selbsteinschätzung zählt nicht.
        var (planId, positionId, sid) = await SetupAsync(stage: (int)TestStage.SelfAssess, requireTyped: true);
        var child = await TestApi.ChildAsync(factory);

        var res = await TestApi.PositionReviewAsync(child, planId, positionId, sid, 0, wasKnown: true);
        res.EnsureSuccessStatusCode(); // Cursor läuft weiter, aber die Karte wird nicht gewertet …
        var outcome = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, outcome.GetProperty("awarded").GetInt32()); // … keine Punkte …
        Assert.Equal(0, outcome.GetProperty("comboBonus").GetInt32());

        Assert.Equal(1, BoxOf(positionId, 0)); // … und keine Box-Bewegung (bleibt Box 1)
    }

    [Fact]
    public async Task Stufe_NichtVomClientWaehlbar_KeinDowngradeAufSelbsteinschaetzung()
    {
        var (planId, positionId, sid) = await SetupAsync(); // Fahrplan-Stufe Freitext (getippt)
        var child = await TestApi.ChildAsync(factory);

        // Manipulationsversuch: nur wasKnown ohne getippte Antwort. Der Server erzwingt die Freitext-Stufe
        // und bewertet gegen die Lösung → ohne givenAnswer schlicht falsch, keine Gratis-Punkte.
        var res = await TestApi.PositionReviewAsync(child, planId, positionId, sid, 0, wasKnown: true);
        res.EnsureSuccessStatusCode();
        var outcome = await res.Content.ReadFromJsonAsync<JsonElement>();

        JsonAssert.False(outcome, "wasCorrect");
        Assert.Equal(0, outcome.GetProperty("awarded").GetInt32());
    }

    [Fact]
    public async Task Cards_LiefernKeineLoesung_FuerGetippteStufe()
    {
        var (planId, positionId, sid) = await SetupAsync();
        var child = await TestApi.ChildAsync(factory);

        var res = await child.GetAsync($"{TestApi.PracticeBase(planId, positionId)}/{sid}/cards");
        res.EnsureSuccessStatusCode();
        var cards = await res.Content.ReadFromJsonAsync<JsonElement>();

        Assert.NotEmpty(cards.EnumerateArray());
        foreach (var card in cards.EnumerateArray())
        {
            // Freitext-Stufe: Prompt (Wort) ja, Lösung (reveal) nein.
            Assert.False(string.IsNullOrEmpty(card.GetProperty("prompt").GetString()));
            Assert.Equal(JsonValueKind.Null, card.GetProperty("reveal").ValueKind);
        }
    }
}
