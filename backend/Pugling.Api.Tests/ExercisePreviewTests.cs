using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;

namespace Pugling.Api.Tests;

/// <summary>
/// Test mode ("try it out"): the father plays through an exercise server-authoritatively – the grading
/// is the same as for the child, but the run is completely side-effect-free (no points, no
/// TestAttempt, no position progress, no session).
/// </summary>
public class ExercisePreviewTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    private (int childPoints, int attempts, int progress, int sessions) Counts()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        return (db.ChildPointsEntries.Count(), db.TestAttempts.Count(),
            db.PositionItemProgress.Count(), db.PracticeSessions.Count());
    }

    [Fact]
    public async Task Vokabel_Preview_LiefertAufgabenOhneLoesung_UndBewertetOhneNebenwirkung()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ("hello", "hallo"), ("goodbye", "tschüss"));

        var before = Counts();

        // GET preview: a typed final stage → the solution is NOT revealed.
        var data = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}/preview");
        JsonAssert.True(data, "typed");
        var items = data.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal("hello", items[0].GetProperty("prompt").GetString());
        Assert.Equal(JsonValueKind.Null, items[0].GetProperty("reveal").ValueKind);

        // POST check: the first answer correct, the second wrong → 50 %.
        var res = await father.PostAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/preview/check", new
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
        JsonAssert.True(outItems[0], "wasCorrect");
        Assert.Equal("hallo", outItems[0].GetProperty("expected").GetString()); // in the result the solution is disclosed
        JsonAssert.False(outItems[1], "wasCorrect");

        // The core of the assurance: nothing was persisted - no points, no attempt, no progress, no session.
        Assert.Equal(before, Counts());
    }

    [Fact]
    public async Task Rechen_Preview_BewertetGetippteAntwort()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (_, _, exerciseId) = await TestApi.CreateArithmeticExerciseAsync(father);

        var before = Counts();

        var res = await father.PostAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/preview/check", new
        {
            answers = new[] { new { itemIndex = 0, givenAnswer = "42", wasKnown = (bool?)null } },
        });
        res.EnsureSuccessStatusCode();
        var result = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(100, result.GetProperty("scorePercent").GetInt32());

        Assert.Equal(before, Counts());
    }

    /// <summary>
    /// B-77/R5: the trial run is what the supervisor assigns an exercise on, so it must grade an unordered
    /// list as a <b>set</b> just like the child's exam – and say so on the card. If it graded card by card,
    /// the supervisor would see a broken exercise and "fix" a list that is fine.
    /// </summary>
    [Fact]
    public async Task Liste_Preview_BewertetAlsMenge_UndWeistDieRegelAus()
    {
        var father = await TestApi.AdultAsync(_factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Vorschau-Liste") }));
        var seriesId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new
            {
                name = TestApi.UniqueName("Reihe-Vorschau-Liste"),
                publisher = (string?)null,
                subjectName = (string?)null,
                subjectId,
                schoolTypes = (string?)null,
                sourceLanguage = (string?)null,
                targetLanguage = (string?)null,
                notes = (string?)null,
            }));
        var seriesUnitId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units", new { label = "Kapitel", orderIndex = 1 }));
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/list", new
            {
                title = TestApi.UniqueName("Drei Bundesländer (Vorschau)"),
                orderIndex = 1,
                rewardPoints = 10,
                config = new
                {
                    instruction = "Nenne drei Bundesländer.",
                    ordered = false,
                    items = new object[] { new { value = "Bayern" }, new { value = "Hessen" }, new { value = "Berlin" } },
                },
            }));

        var before = Counts();

        var data = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}/preview");
        Assert.All(data.GetProperty("items").EnumerateArray(), i => JsonAssert.True(i, "anyOrder"));

        // Answered in reverse - under a per-card grading this would be 1 of 3.
        var res = await father.PostAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/preview/check", new
        {
            answers = new[]
            {
                new { itemIndex = 0, givenAnswer = "Berlin", wasKnown = (bool?)null },
                new { itemIndex = 1, givenAnswer = "Hessen", wasKnown = (bool?)null },
                new { itemIndex = 2, givenAnswer = "Bayern", wasKnown = (bool?)null },
            },
        });
        res.EnsureSuccessStatusCode();
        var result = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(100, result.GetProperty("scorePercent").GetInt32());

        Assert.Equal(before, Counts());
    }

    [Fact]
    public async Task Preview_UnbekannteUebung_Liefert404()
    {
        var father = await TestApi.AdultAsync(_factory);
        var res = await father.GetAsync("/api/v1/creator/exercises/999999/preview");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Preview_NurFuerVater()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var child = await TestApi.ChildAsync(_factory);

        var res = await child.GetAsync($"/api/v1/creator/exercises/{exerciseId}/preview");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Preview_StufeUmschalten_MultipleChoice_LiefertAuswahl()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ("hello", "hallo"), ("goodbye", "tschüss"), ("cat", "Katze"));

        // stage=6 (multiple choice): typed, every task carries choices; the switchable stages come along.
        var data = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}/preview?stage=6");
        Assert.Equal(6, data.GetProperty("stage").GetInt32());
        JsonAssert.True(data, "typed");
        Assert.Equal("Vocabulary", data.GetProperty("type").GetString());
        Assert.Contains(data.GetProperty("stages").EnumerateArray(), s => s.GetProperty("value").GetInt32() == 5); // the listening stage is selectable
        var items = data.GetProperty("items").EnumerateArray().ToList();
        var choices = items[0].GetProperty("choices").EnumerateArray().Select(c => c.GetString()).ToList();
        Assert.Contains("hallo", choices); // the correct solution is among the choices
        Assert.True(choices.Count > 1);

        // A check with the same stage: the right choice → 100 % with one item.
        var res = await father.PostAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/preview/check", new
        {
            answers = new[] { new { itemIndex = 0, givenAnswer = "hallo", wasKnown = (bool?)null } },
            stage = 6,
        });
        res.EnsureSuccessStatusCode();
        var result = await res.Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.True(result.GetProperty("items").EnumerateArray().First(), "wasCorrect");
    }

    [Fact]
    public async Task Preview_Hoerstufe_LiefertAudioquelle_OhneLoesung()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (id, key) = await TestApi.CreateStoreVocabAsync(father, "hello", "hallo");
        // Add pronunciation audio (PATCH) - only then can the listening stage "read out" the word.
        var patch = await father.PatchAsJsonAsync($"/api/v1/creator/vocabulary/{id}",
            new { pronunciationAudioUrl = "https://example.test/hello.mp3" });
        patch.EnsureSuccessStatusCode();
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, key);

        // stage=5 (listening): typed, the solution hidden, but the audio source is passed to the client.
        var data = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}/preview?stage=5");
        Assert.Equal(5, data.GetProperty("stage").GetInt32());
        JsonAssert.True(data, "typed");
        var item = data.GetProperty("items").EnumerateArray().First();
        Assert.Equal("https://example.test/hello.mp3", item.GetProperty("audioUrl").GetString());
        Assert.Equal(JsonValueKind.Null, item.GetProperty("reveal").ValueKind);

        // The free-text stage (4) on the same exercise: no audio source (only the listening stage reads out).
        var freeText = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}/preview?stage=4");
        Assert.Equal(JsonValueKind.Null, freeText.GetProperty("items").EnumerateArray().First().GetProperty("audioUrl").ValueKind);
    }
}
