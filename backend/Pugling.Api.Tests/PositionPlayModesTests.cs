using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Server-getriebene Ausspiel-Modi (Info / Lern / Klausur): eingefrorene Reihenfolge + Cursor. Prüft die
/// neue One-at-a-time-Auslieferung (<c>/next</c>), das mitgelieferte „nächste Karte"-Feedback in
/// <c>/review</c>, den feedbacklosen Info-Modus und den strikt server-getriebenen Klausur-Fluss.
/// </summary>
public class PositionPlayModesTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    private static readonly (string, string)[] ThreeWords = [("a", "1"), ("b", "2"), ("c", "3")];

    // ---- Lern-Modus: Server-Cursor + „nächste Karte" ----

    [Fact]
    public async Task LernModus_LiefertKartenEinzelnUeberCursor_UndSchliesstAbMitDone()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ThreeWords);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = TestApi.PracticeBase(planId, positionId);

        var start = await (await child.PostAsJsonAsync(baseUrl, new { mode = "Lern" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Lern", start.GetProperty("mode").GetString());
        Assert.Equal(3, start.GetProperty("total").GetInt32());
        var sessionId = start.GetProperty("id").GetInt32();

        // Erste Karte kommt server-geführt über /next (nicht als Batch).
        var next = await child.GetFromJsonAsync<JsonElement>($"{baseUrl}/{sessionId}/next");
        JsonAssert.False(next, "done");
        Assert.Equal(0, next.GetProperty("cursor").GetInt32());
        var answers = new[] { "1", "2", "3" };
        var card = next.GetProperty("card");

        // Der ganze Lauf über /review – jede Antwort trägt die nächste Karte bzw. das Abschluss-Signal.
        for (var i = 0; i < 3; i++)
        {
            var idx = card.GetProperty("itemIndex").GetInt32();
            var outcome = await (await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, idx, givenAnswer: answers[idx]))
                .Content.ReadFromJsonAsync<JsonElement>();
            JsonAssert.True(outcome, "wasCorrect");
            var done = outcome.GetProperty("done").GetBoolean();
            if (i < 2)
            {
                Assert.False(done);
                Assert.Equal(JsonValueKind.Object, outcome.GetProperty("next").ValueKind); // nächste Karte liegt bei
                card = outcome.GetProperty("next");
            }
            else
            {
                Assert.True(done); // letzte Karte → Lauf zu Ende
                Assert.Equal(JsonValueKind.Null, outcome.GetProperty("next").ValueKind);
            }
        }

        // Cursor steht am Ende; /next meldet done.
        var end = await child.GetFromJsonAsync<JsonElement>($"{baseUrl}/{sessionId}/next");
        JsonAssert.True(end, "done");
        Assert.Equal(JsonValueKind.Null, end.GetProperty("card").ValueKind);
    }

    // ---- Info-Modus: freies Üben, kein Feedback ----

    [Fact]
    public async Task InfoModus_LiefertAlleKartenAlsBatch_UndSchreibtKeinFeedback()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ThreeWords);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = TestApi.PracticeBase(planId, positionId);

        var start = await (await child.PostAsJsonAsync(baseUrl, new { mode = "Info" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Info", start.GetProperty("mode").GetString());
        var sessionId = start.GetProperty("id").GetInt32();

        // Alle Karten am Stück abrufbar.
        var cards = await child.GetFromJsonAsync<JsonElement>($"{baseUrl}/{sessionId}/cards");
        Assert.Equal(3, cards.GetArrayLength());

        // /review schreibt im Info-Modus nichts (204) – kein Fortschritt, keine Punkte.
        var res = await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 0, givenAnswer: "1");
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        Assert.Empty(db.PositionItemProgress.Where(p => p.PlanPositionId == positionId));
        Assert.Empty(db.ReviewEvents.Where(r => r.PracticeSessionId == sessionId));
    }

    [Fact]
    public async Task InfoModus_ErfuelltDasTagesziel_Nicht()
    {
        var father = await TestApi.FatherAsync(_factory);
        // Reine Inhaltsübung (Leseverstehen) → Ziel „erledigt", sobald eine echte Lern-Sitzung existiert.
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/learn/subjects", new { name = "Info-Fach" }));
        var chapterId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/learn/subjects/{subjectId}/chapters", new { name = "K1", orderIndex = 1 }));
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/learn/subjects/{subjectId}/chapters/{chapterId}/reading",
            new { title = "Text", orderIndex = 1, rewardPoints = 5, config = new { text = "Ein kurzer Text.", questions = Array.Empty<object>() } }));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText, useLeitner: false);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = TestApi.PracticeBase(planId, positionId);

        // Info-Sitzung mit echter Aktivität (Heartbeat) → darf das Tagesziel NICHT erfüllen.
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { mode = "Info" }));
        await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/heartbeat", new { seconds = 60, active = true });
        await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/end", new { });

        var overview = await child.GetFromJsonAsync<JsonElement>($"/api/v1/study-plans/{planId}/overview");
        JsonAssert.False(overview.GetProperty("today"), "dutyDone");
    }

    // ---- Klausur-Modus: strikt server-getrieben ----

    [Fact]
    public async Task KlausurModus_StartOhneAufgaben_FragenEinzelnOhneKorrektheit_AbschlussMitScorecard()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ThreeWords);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var testsUrl = $"/api/v1/study-plans/{planId}/positions/{positionId}/tests";

        // Start liefert NUR Metadaten – keine Aufgaben im Bulk (strikt server-getrieben).
        var start = await (await child.PostAsJsonAsync(testsUrl, new { })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, start.GetProperty("totalItems").GetInt32());
        Assert.False(start.TryGetProperty("items", out _)); // keine Aufgaben vorab
        var attemptId = start.GetProperty("attemptId").GetInt32();

        // Fragen einzeln holen, beantworten – die Antwort verrät NICHT, ob sie korrekt war.
        var answersByPrompt = new Dictionary<string, string> { ["a"] = "1", ["b"] = "2", ["c"] = "wrong" };
        for (var i = 0; i < 3; i++)
        {
            var next = await child.GetFromJsonAsync<JsonElement>($"{testsUrl}/{attemptId}/next");
            JsonAssert.False(next, "done");
            var prompt = next.GetProperty("item").GetProperty("prompt").GetString()!;
            Assert.Equal(JsonValueKind.Null, next.GetProperty("item").GetProperty("reveal").ValueKind); // getippt: keine Lösung

            var ack = await (await child.PostAsJsonAsync($"{testsUrl}/{attemptId}/answer",
                new { givenAnswer = answersByPrompt[prompt] })).Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(ack.TryGetProperty("wasCorrect", out _)); // kein Feedback pro Frage
        }

        // Nach der letzten Frage: /next ist am Ende.
        var end = await child.GetFromJsonAsync<JsonElement>($"{testsUrl}/{attemptId}/next");
        JsonAssert.True(end, "done");

        // Abschluss: erst hier kommt die Auswertung (2 von 3 richtig).
        var submit = await (await child.PostAsJsonAsync($"{testsUrl}/{attemptId}/submit", new { }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, submit.GetProperty("totalItems").GetInt32());
        Assert.Equal(2, submit.GetProperty("correctItems").GetInt32());
    }

    // ---- Reihenfolge-Strategien ----

    [Fact]
    public async Task Strategie_Serial_SpieltStrengNachIndex()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, [("a", "1"), ("b", "2"), ("c", "3"), ("d", "4"), ("e", "5")]);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText,
            orderStrategy: PracticeOrder.Serial);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = TestApi.PracticeBase(planId, positionId);

        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { }));
        var cards = await child.GetFromJsonAsync<JsonElement>($"{baseUrl}/{sessionId}/cards");
        var order = cards.EnumerateArray().Select(c => c.GetProperty("itemIndex").GetInt32()).ToList();
        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, order);
    }

    // ---- Klausur: Aufzeichnung erst beim Abschluss (kein Leak/Doppelzähler durch Abbruch/Wiederholung) ----

    [Fact]
    public async Task KlausurModus_AbgebrochenerVersuch_SchreibtKeinenLernstand_WiederholungZaehltNichtDoppelt()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ThreeWords);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var testsUrl = $"/api/v1/study-plans/{planId}/positions/{positionId}/tests";
        var ans = new Dictionary<string, string> { ["a"] = "1", ["b"] = "2", ["c"] = "3" };

        // Versuch 1: eine Frage beantworten, dann ABBRECHEN (kein Submit).
        var a1 = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(testsUrl, new { }), "attemptId");
        await child.GetAsync($"{testsUrl}/{a1}/next");
        await child.PostAsJsonAsync($"{testsUrl}/{a1}/answer", new { givenAnswer = "1" });

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            // Der abgebrochene Versuch darf den plan-übergreifenden Lernstand NICHT verändert haben.
            Assert.Empty(db.ItemReviewEvents.Where(e => e.ExerciseId == exerciseId));
            Assert.Empty(db.ItemProgress.Where(p => p.ExerciseId == exerciseId));
        }

        // Versuch 2: vollständig durchspielen + abgeben.
        var a2 = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(testsUrl, new { }), "attemptId");
        for (var i = 0; i < 3; i++)
        {
            var nx = await child.GetFromJsonAsync<JsonElement>($"{testsUrl}/{a2}/next");
            var prompt = nx.GetProperty("item").GetProperty("prompt").GetString()!;
            await child.PostAsJsonAsync($"{testsUrl}/{a2}/answer", new { givenAnswer = ans[prompt] });
        }
        await child.PostAsJsonAsync($"{testsUrl}/{a2}/submit", new { });

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            // Genau EINE Aufzeichnung je Item (3), nicht 4 – die abgebrochene erste Antwort zählt nicht mit.
            Assert.Equal(3, db.ItemReviewEvents.Count(e => e.ExerciseId == exerciseId && e.Source == ItemReviewSource.Test));
        }
    }

    // ---- Info-Modus serviert den ganzen Pool (auch nicht-fällige Karten), Lern nur die fälligen ----

    [Fact]
    public async Task InfoModus_ServiertAuchNichtFaelligeKarten_ImGegensatzZuLern()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ThreeWords);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = TestApi.PracticeBase(planId, positionId);

        // Alle 3 Karten in einer Lern-Sitzung richtig → Box hoch, DueOn = später (heute nicht mehr fällig).
        var lern = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { mode = "Lern" }));
        foreach (var (idx, a) in new[] { (0, "1"), (1, "2"), (2, "3") })
            (await TestApi.PositionReviewAsync(child, planId, positionId, lern, idx, givenAnswer: a)).EnsureSuccessStatusCode();

        // Neue Lern-Sitzung: nichts mehr fällig → leer.
        var lern2 = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { mode = "Lern" }));
        var lernCards = await child.GetFromJsonAsync<JsonElement>($"{baseUrl}/{lern2}/cards");
        Assert.Equal(0, lernCards.GetArrayLength());

        // Info-Sitzung: der ganze Pool bleibt spielbar (freies Wiederholen), obwohl nichts fällig ist.
        var info = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { mode = "Info" }));
        var infoCards = await child.GetFromJsonAsync<JsonElement>($"{baseUrl}/{info}/cards");
        Assert.Equal(3, infoCards.GetArrayLength());
    }

    [Fact]
    public async Task Strategie_Random_SpieltAlleFaelligenGenauEinmal()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, [("a", "1"), ("b", "2"), ("c", "3"), ("d", "4"), ("e", "5")]);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText,
            orderStrategy: PracticeOrder.Random);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = TestApi.PracticeBase(planId, positionId);

        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { }));
        var cards = await child.GetFromJsonAsync<JsonElement>($"{baseUrl}/{sessionId}/cards");
        var order = cards.EnumerateArray().Select(c => c.GetProperty("itemIndex").GetInt32()).ToList();
        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, order.OrderBy(i => i).ToArray()); // Permutation ohne Verlust/Dubletten
    }
}
