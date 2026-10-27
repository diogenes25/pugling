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
                publisherId = (int?)null,
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

    /// <summary>
    /// B-05/B-283: the scramble stage (7) in the preview hands out the tiles, withholds the solution and the
    /// letter-box facets, and does not reshuffle between two calls (the exercise id is the seed). Red if the preview
    /// drops <c>LetterTiles</c> from its projection.
    /// </summary>
    [Fact]
    public async Task Preview_Ordnen_LiefertKacheln_FestJeUebung_OhneLoesung()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ("butterfly", "Schmetterlin"));

        var data = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}/preview?stage=7");
        Assert.Equal(7, data.GetProperty("stage").GetInt32());
        JsonAssert.True(data, "typed");
        var item = data.GetProperty("items").EnumerateArray().Single();
        var tiles = item.GetProperty("letterTiles").EnumerateArray().Select(t => t.GetString()).ToList();
        Assert.Equal("schmetterlin".Order(), string.Concat(tiles).Order());
        Assert.NotEqual("schmetterlin", string.Concat(tiles));
        Assert.Equal(JsonValueKind.Null, item.GetProperty("reveal").ValueKind);
        Assert.Equal(JsonValueKind.Null, item.GetProperty("answerLength").ValueKind);

        var again = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}/preview?stage=7");
        Assert.Equal(tiles, again.GetProperty("items").EnumerateArray().Single().GetProperty("letterTiles").EnumerateArray().Select(t => t.GetString()).ToList());
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

    private static async Task<string> UnitBaseAsync(HttpClient father, string label)
    {
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = $"{label}-{Guid.NewGuid():N}"[..20] }));
        var (seriesId, seriesUnitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        return $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}";
    }

    /// <summary>
    /// B-15: an essay has no gradable atoms, but the author still gets to SEE what the child will read - the
    /// writing task, the word limits and the rubric he entered. It is a view: not gradable, and the check
    /// keeps answering "nothing to check".
    /// Red without the essay's <c>StaticView</c> (400 instead of 200), with <c>Gradable</c> left at its default
    /// <c>true</c>, or with the hint dropping limits or rubric.
    /// </summary>
    [Fact]
    public async Task Aufsatz_Preview_ZeigtAuftragGrenzenUndRubrik_OhneBewertung()
    {
        var father = await TestApi.AdultAsync(_factory);
        var basis = await UnitBaseAsync(father, "Aufsatz-Vorschau");
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync($"{basis}/essays", new
        {
            title = "Mein Hobby",
            orderIndex = 1,
            rewardPoints = 10,
            config = new
            {
                prompt = "Schreibe über dein Hobby.",
                minWords = 80,
                maxWords = 200,
                rubric = new[] { new { criterion = "Aufbau", maxScore = 5 }, new { criterion = "Wortschatz", maxScore = 3 } },
            },
        }));

        var res = await father.GetAsync($"/api/v1/creator/exercises/{exerciseId}/preview");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var data = await res.Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.False(data, "gradable");
        JsonAssert.False(data, "typed");
        var item = Assert.Single(data.GetProperty("items").EnumerateArray().ToList());
        Assert.Equal("Schreibe über dein Hobby.", item.GetProperty("prompt").GetString());
        Assert.Equal("Länge: 80–200 Wörter · Bewertung: Aufbau (5 P.), Wortschatz (3 P.)", item.GetProperty("hint").GetString());

        // AC 2: nothing to grade - the check stays exactly where it was.
        var check = await father.PostAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/preview/check",
            new { answers = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.BadRequest, check.StatusCode);
        Assert.Equal("no_checkable_content", (await check.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    /// <summary>
    /// B-15 AC 3: the view hook stays null for every other type. Two atom-less cases that must keep their 400:
    /// a reading text without questions (same type family, no view) and the arithmetic drill (own generate path).
    /// This asserts the state before B-15, so on its own it cannot fail; it turns red only under the mutant
    /// "the base <c>StaticView</c> returns a view for every type" (measured 2026-10-23) - a guard, not proof of B-15.
    /// </summary>
    [Fact]
    public async Task Preview_OhneAtome_BleibtFuerAndereTypenBeiNoCheckableContent()
    {
        var father = await TestApi.AdultAsync(_factory);
        var basis = await UnitBaseAsync(father, "Ohne-Atome");
        var reading = await TestApi.IdAsync(await father.PostAsJsonAsync($"{basis}/reading", new
        {
            title = "Leseverstehen (leer)",
            orderIndex = 1,
            rewardPoints = 5,
            config = new { text = "A short text without questions.", questions = Array.Empty<object>() },
        }));
        var drill = await TestApi.IdAsync(await father.PostAsJsonAsync($"{basis}/arithmetic-drill", new
        {
            title = "Kopfrechnen",
            orderIndex = 2,
            rewardPoints = 10,
            config = new { operations = new[] { "Addition" }, minOperand = 2, maxOperand = 9, problemCount = 4 },
        }));

        foreach (var id in new[] { reading, drill })
        {
            var res = await father.GetAsync($"/api/v1/creator/exercises/{id}/preview");
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
            Assert.Equal("no_checkable_content", (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        }
    }
}
