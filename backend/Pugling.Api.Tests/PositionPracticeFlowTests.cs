using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// End-to-End des neuen positions-basierten Übens (Etappe 3): eine Katalog-Übung wird über eine
/// Lehrplan-Position gespielt, Inhalt kommt aus der Übungs-Config, Leitner-Fortschritt läuft über
/// <see cref="PositionItemProgress"/>. Die Position wird direkt geseedet (Positions-CRUD folgt in Etappe 5).
/// </summary>
public class PositionPracticeFlowTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    [Fact]
    public async Task Vokabel_Position_RichtigGetippt_BringtPunkteUndBoxAufstieg()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/study-plans/{planId}/positions/{positionId}/practice-sessions";

        // Sitzung starten
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { }));

        // Karten: beide Items sind neu → fällig; getippte Stufe → keine Lösung mitgeliefert.
        var cards = await (await child.GetAsync($"{baseUrl}/{sessionId}/cards"))
            .Content.ReadFromJsonAsync<List<JsonElement>>();
        Assert.Equal(2, cards!.Count);
        Assert.Equal("hello", cards[0].GetProperty("prompt").GetString());
        Assert.Equal(JsonValueKind.Null, cards[0].GetProperty("reveal").ValueKind);

        // Richtige Antwort auf Item 0 → gewertet
        var review = await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/review",
            new { itemIndex = 0, givenAnswer = "hallo" });
        var outcome = await review.Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.True(outcome, "wasCorrect");
        Assert.True(outcome.GetProperty("awarded").GetInt32() > 0);
        Assert.Equal(2, outcome.GetProperty("box").GetInt32()); // Box 1 → 2

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var prog = db.PositionItemProgress.Single(p => p.PlanPositionId == positionId && p.ItemIndex == 0);
        Assert.Equal(2, prog.Box);
        Assert.NotNull(prog.IntroducedAt);
        Assert.True(db.ChildPoints.Where(e => e.ChildId == 1 && e.Kind == PointKind.Base).Sum(e => e.Amount) > 0);
    }

    [Fact]
    public async Task Vokabel_Position_ZweiteWertungAmSelbenTag_WirdNichtGewertet()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/study-plans/{planId}/positions/{positionId}/practice-sessions";
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { }));

        // Erste Wertung: 200 + Ergebnis
        var first = await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/review", new { itemIndex = 0, givenAnswer = "hallo" });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        // Zweite Wertung derselben Karte am selben Tag: nur protokolliert, keine weiteren Punkte (Anti-Farming).
        // Der Cursor läuft weiter (200), aber es fließen keine Punkte.
        var second = await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/review", new { itemIndex = 0, givenAnswer = "hallo" });
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var secondOutcome = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, secondOutcome.GetProperty("awarded").GetInt32());
    }

    [Fact]
    public async Task Vokabel_Position_FalscheAntwort_BleibtInBox1UndFaellig()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/study-plans/{planId}/positions/{positionId}/practice-sessions";
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { }));

        var review = await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/review", new { itemIndex = 0, givenAnswer = "falsch" });
        var outcome = await review.Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.False(outcome, "wasCorrect");
        Assert.Equal(0, outcome.GetProperty("awarded").GetInt32());
        Assert.Equal(1, outcome.GetProperty("box").GetInt32());
    }

    [Fact]
    public async Task Position_UnbekanntFuerDenPlan_LiefertNotFound()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);

        // Position, die es (in diesem Plan) nicht gibt → Start muss 404 liefern, nicht ins Leere spielen.
        var res = await child.PostAsJsonAsync(
            $"/api/v1/study-plans/{planId}/positions/{positionId + 999}/practice-sessions", new { });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
