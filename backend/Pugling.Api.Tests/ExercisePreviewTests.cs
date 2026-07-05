using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;

namespace Pugling.Api.Tests;

/// <summary>
/// Testmodus („Ausprobieren"): Der Vater spielt eine Übung server-autoritativ durch – die Bewertung ist
/// dieselbe wie beim Kind, aber der Durchlauf ist vollständig nebenwirkungsfrei (keine Punkte, kein
/// TestAttempt, kein Positions-Fortschritt, keine Session).
/// </summary>
public class ExercisePreviewTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    private (int childPoints, int attempts, int progress, int sessions) Counts()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        return (db.ChildPoints.Count(), db.TestAttempts.Count(),
            db.PositionItemProgress.Count(), db.PracticeSessions.Count());
    }

    [Fact]
    public async Task Vokabel_Preview_LiefertAufgabenOhneLoesung_UndBewertetOhneNebenwirkung()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ("hello", "hallo"), ("goodbye", "tschüss"));

        var before = Counts();

        // GET preview: getippte Endstufe → Lösung wird NICHT aufgedeckt.
        var data = await father.GetFromJsonAsync<JsonElement>($"/api/v1/learn/exercises/{exerciseId}/preview");
        Assert.True(data.GetProperty("typed").GetBoolean());
        var items = data.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal("hello", items[0].GetProperty("prompt").GetString());
        Assert.Equal(JsonValueKind.Null, items[0].GetProperty("reveal").ValueKind);

        // POST check: erste Antwort richtig, zweite falsch → 50 %.
        var res = await father.PostAsJsonAsync($"/api/v1/learn/exercises/{exerciseId}/preview/check", new
        {
            answers = new[]
            {
                new { itemIndex = 0, givenAnswer = "hallo", wasKnown = (bool?)null },
                new { itemIndex = 1, givenAnswer = "falsch", wasKnown = (bool?)null },
            },
        });
        res.EnsureSuccessStatusCode();
        var result = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, result.GetProperty("total").GetInt32());
        Assert.Equal(1, result.GetProperty("correct").GetInt32());
        Assert.Equal(50, result.GetProperty("scorePercent").GetInt32());
        var outItems = result.GetProperty("items").EnumerateArray().ToList();
        Assert.True(outItems[0].GetProperty("wasCorrect").GetBoolean());
        Assert.Equal("hallo", outItems[0].GetProperty("expected").GetString()); // im Ergebnis wird die Lösung offengelegt
        Assert.False(outItems[1].GetProperty("wasCorrect").GetBoolean());

        // Kern der Zusicherung: nichts wurde persistiert – keine Punkte, kein Versuch, kein Fortschritt, keine Session.
        Assert.Equal(before, Counts());
    }

    [Fact]
    public async Task Rechen_Preview_BewertetGetippteAntwort()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (_, _, exerciseId) = await TestApi.CreateArithmeticExerciseAsync(father);

        var before = Counts();

        var res = await father.PostAsJsonAsync($"/api/v1/learn/exercises/{exerciseId}/preview/check", new
        {
            answers = new[] { new { itemIndex = 0, givenAnswer = "42", wasKnown = (bool?)null } },
        });
        res.EnsureSuccessStatusCode();
        var result = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(100, result.GetProperty("scorePercent").GetInt32());

        Assert.Equal(before, Counts());
    }

    [Fact]
    public async Task Preview_UnbekannteUebung_Liefert404()
    {
        var father = await TestApi.FatherAsync(_factory);
        var res = await father.GetAsync("/api/v1/learn/exercises/999999/preview");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Preview_NurFuerVater()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var child = await TestApi.ChildAsync(_factory);

        var res = await child.GetAsync($"/api/v1/learn/exercises/{exerciseId}/preview");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }
}
