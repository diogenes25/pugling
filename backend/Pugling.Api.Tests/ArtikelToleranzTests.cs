using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-421 end to end: "ville" for "la ville" counts in practice, in the test (both ways of answering) and in the
/// supervisor's preview - one grader, one rule - unless the exercise trains the article (<c>articleRequired</c>). Since
/// B-597 the free-text card asks the learnt word whatever the direction, so the article forgiven there is the learnt
/// language's - on a front-to-back exercise through the reversed card the resolver builds.
/// </summary>
public class ArtikelToleranzTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    /// <summary>A one-word vocabulary exercise (fr → de) - one word, so every path asks exactly it.</summary>
    private async Task<int> ExerciseAsync(HttpClient father, bool? articleRequired = null, string direction = "front-to-back",
        (string Front, string Back)[]? words = null)
    {
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Französisch-Artikel") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        var config = new Dictionary<string, object?>
        {
            ["direction"] = direction,
            ["sourceLang"] = "fr",
            ["targetLang"] = "de",
            ["items"] = (words ?? [("la ville", "die Stadt")]).Select(w => new { front = w.Front, back = w.Back }).ToArray(),
        };
        if (articleRequired is { } required) config["articleRequired"] = required;
        return await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/vocabulary",
            new { title = "En ville", orderIndex = 1, rewardPoints = 10, config }));
    }

    private async Task<bool> PracticeAsync(int exerciseId, string given, int itemIndex = 0)
    {
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(factory);
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);
        var outcome = await (await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, itemIndex, givenAnswer: given))
            .Content.ReadFromJsonAsync<JsonElement>();
        return outcome.GetProperty("wasCorrect").GetBoolean();
    }

    // AK 1 in practice: the article may be left out.
    [Fact]
    public async Task Ueben_Wertet_Ville_Fuer_La_Ville_Als_Richtig()
    {
        var father = await TestApi.AdultAsync(factory);
        Assert.True(await PracticeAsync(await ExerciseAsync(father), "ville"));
    }

    // AK 5: an exercise that trains the article demands it again.
    [Fact]
    public async Task ArticleRequired_Verlangt_Den_Artikel_Wieder()
    {
        var father = await TestApi.AdultAsync(factory);
        Assert.False(await PracticeAsync(await ExerciseAsync(father, articleRequired: true), "ville"));
    }

    // The answer's language follows the direction: asked back to front, the answer is French and its article is "la".
    [Fact]
    public async Task Hinten_Nach_Vorne_Verzeiht_Den_Artikel_Der_Ausgangssprache()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await ExerciseAsync(father, direction: "back-to-front");
        Assert.True(await PracticeAsync(exerciseId, "ville"));
    }

    // Review finding: with "both" every odd card is asked the other way round - its answer is French, the even one German.
    // Since B-472 card 2i+1 is word i backwards, so card 1 asks "la ville" and card 3 "la rue".
    [Fact]
    public async Task Beide_Richtungen_Verzeihen_Je_Den_Artikel_Der_Antwortsprache()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await ExerciseAsync(father, direction: "both", words: [("la ville", "die Stadt"), ("la rue", "die Straße")]);
        Assert.True(await PracticeAsync(exerciseId, "Stadt", itemIndex: 0));
        Assert.True(await PracticeAsync(exerciseId, "ville", itemIndex: 1));
        Assert.True(await PracticeAsync(exerciseId, "rue", itemIndex: 3));
    }

    // AK 6: the test grades the same - the bulk submission ...
    [Fact]
    public async Task Test_Gesammelt_Wertet_Wie_Das_Ueben()
    {
        var father = await TestApi.AdultAsync(factory);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, await ExerciseAsync(father), (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";
        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");

        var res = await (await child.PostAsJsonAsync($"{baseUrl}/{attemptId}/submit",
            new { answers = new[] { new { itemIndex = 0, givenAnswer = "ville" } } })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, res.GetProperty("correctItems").GetInt32());
    }

    // ... and the one-at-a-time answer.
    [Fact]
    public async Task Test_Einzeln_Wertet_Wie_Das_Ueben()
    {
        var father = await TestApi.AdultAsync(factory);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, await ExerciseAsync(father), (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";
        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");

        (await child.PostAsJsonAsync($"{baseUrl}/{attemptId}/answer", new { itemIndex = 0, givenAnswer = "ville" })).EnsureSuccessStatusCode();
        var res = await (await child.PostAsJsonAsync($"{baseUrl}/{attemptId}/submit", new { }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, res.GetProperty("correctItems").GetInt32());
    }

    // The supervisor's preview grades like the child's test, so "Ausprobieren" does not promise another rule.
    [Fact]
    public async Task Vorschau_Wertet_Wie_Der_Test()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await ExerciseAsync(father);
        var res = await (await father.PostAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/preview/check",
            new { answers = new[] { new { itemIndex = 0, givenAnswer = "ville", wasKnown = (bool?)null } }, stage = (int)TestStage.FreeText }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, res.GetProperty("correct").GetInt32());
    }

    /// <summary>
    /// B-437: a store-backed exercise with one entry of the given part of speech - the tolerance follows the entry's
    /// part of speech, so the setup goes through the store rather than inline items (those are created as Other). The
    /// German side is made unique: the learnt word is the answer on free text (B-597) and must stay as typed.
    /// </summary>
    private static async Task<int> StoreExerciseAsync(HttpClient father, string src, string tgt, string word,
        string translation, PartOfSpeech partOfSpeech)
    {
        var res = await father.PostAsJsonAsync("/api/v1/creator/vocabulary",
            new { sourceLanguage = src, targetLanguage = tgt, word, translation = TestApi.UniqueName(translation), partOfSpeech });
        res.EnsureSuccessStatusCode();
        var key = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("key").GetString()!;
        return await TestApi.CreateVocabRefExerciseAsync(father, key);
    }

    // B-437 AK 1: "a" in "a few" is part of a quantity word, not the article of a noun - "few" means the opposite.
    // Red if the part-of-speech condition at the resolver is missing.
    [Theory]
    [InlineData(PartOfSpeech.Pronoun)]
    [InlineData(PartOfSpeech.Phrase)]
    public async Task Nicht_Nomen_Wertet_Few_Fuer_A_Few_Als_Falsch(PartOfSpeech partOfSpeech)
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await StoreExerciseAsync(father, "en", "de", "a few", "einige", partOfSpeech);
        Assert.False(await PracticeAsync(exerciseId, "few"));
        Assert.True(await PracticeAsync(exerciseId, "a few"));
    }

    // B-437 AK 2: a noun keeps the tolerance of B-421. Red if the condition excludes Noun.
    [Fact]
    public async Task Nomen_Wertet_Ville_Fuer_La_Ville_Weiter_Als_Richtig()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await StoreExerciseAsync(father, "fr", "de", "la ville", "die Stadt", PartOfSpeech.Noun);
        Assert.True(await PracticeAsync(exerciseId, "ville"));
    }

    // B-437 AK 3: an entry without a determined part of speech (the default on creation) stays tolerant. Red if the
    // condition admits only Noun.
    [Fact]
    public async Task Ohne_Wortart_Bleibt_Das_Weglassen_Des_Artikels_Richtig()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await StoreExerciseAsync(father, "fr", "de", "la rue", "die Straße", PartOfSpeech.Other);
        Assert.True(await PracticeAsync(exerciseId, "rue"));
    }

    // B-437 AK 4: the bulk test grades "few" like practice - the rule is restricted at its source, not at one reader.
    [Fact]
    public async Task Test_Gesammelt_Wertet_Few_Fuer_A_Few_Wie_Das_Ueben()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await StoreExerciseAsync(father, "en", "de", "a few", "einige", PartOfSpeech.Pronoun);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";
        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");

        var res = await (await child.PostAsJsonAsync($"{baseUrl}/{attemptId}/submit",
            new { answers = new[] { new { itemIndex = 0, givenAnswer = "few" } } })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, res.GetProperty("totalItems").GetInt32());
        Assert.Equal(0, res.GetProperty("correctItems").GetInt32());
    }

    // The settings round trip: the field is read back as saved.
    [Fact]
    public async Task ArticleRequired_Kommt_So_Zurueck_Wie_Gespeichert()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await ExerciseAsync(father, articleRequired: true);
        var ex = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}");
        Assert.True(ex.GetProperty("config").GetProperty("articleRequired").GetBoolean());
    }
}
