using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Exercises;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-599: the cloze gets the input forms of the vocabulary cards - letter boxes, letter tiles and a choice per gap - each
/// with the gap's translation as the hint where there is one. Case by case the change that turns it red is named.
/// </summary>
public class LueckentextEingabeartenTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    // Question tags as in exercise no. 19: every solution is two words, so none can be scrambled.
    private static readonly string[] Tags = ["isn't it", "can't you", "didn't he", "won't they", "doesn't she", "aren't we"];

    private static string TagText() =>
        string.Join(" ", Tags.Select((_, i) => $"Sentence {i + 1}, {{{{{i + 1}}}}}?"));

    private async Task<int> ClozeAsync(HttpClient father, object config)
    {
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Englisch-Eingabeart") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        return await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/cloze",
            new { title = TestApi.UniqueName("Question tags"), orderIndex = 1, rewardPoints = 10, config }));
    }

    private Task<int> TagsAsync(HttpClient father) => ClozeAsync(father, new
    {
        text = TagText(),
        gaps = Tags.Select((t, i) => new { index = i + 1, answer = t }).ToArray(),
    });

    // The cards of a learn round on a position at the given stage, by item index.
    private async Task<Dictionary<int, JsonElement>> CardsAsync(int exerciseId, ClozeStage stage)
    {
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)stage);
        var child = await TestApi.ChildAsync(factory);
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);
        var cards = await child.GetFromJsonAsync<List<JsonElement>>($"{TestApi.PracticeBase(planId, positionId)}/{sessionId}/cards");
        return cards!.ToDictionary(c => c.GetProperty("itemIndex").GetInt32());
    }

    private static List<string> Choices(JsonElement card) =>
        card.GetProperty("choices").ValueKind == JsonValueKind.Array
            ? [.. card.GetProperty("choices").EnumerateArray().Select(c => c.GetString()!)]
            : [];

    /// <summary>
    /// AK 1 + Decision 1: the three stages are offered for the cloze and count as checked. Red if a stage misses the
    /// manifest (the father could not pick it) or is not typed (the solution would come as a reveal).
    /// </summary>
    [Theory]
    [InlineData(ClozeStage.LetterBoxes)]
    [InlineData(ClozeStage.LetterScramble)]
    [InlineData(ClozeStage.Choice)]
    public async Task Die_Stufe_Steht_Im_Manifest_Und_Ist_Geprueft(ClozeStage stage)
    {
        var father = await TestApi.AdultAsync(factory);
        var manifest = await father.GetFromJsonAsync<JsonElement>("/api/v1/creator/exercise-types");
        var cloze = manifest.EnumerateArray().Single(m => m.GetProperty("type").GetString() == ExerciseTypeKeys.Cloze);
        Assert.Contains(cloze.GetProperty("stages").EnumerateArray(), s => s.GetProperty("value").GetInt32() == (int)stage);
        Assert.True(StageMechanics.IsTyped(stage));
    }

    /// <summary>
    /// AK 3: letter boxes give length and separators of the solution and withhold it. Red without the cloze's letter-box
    /// facets (no length, no pattern - a free-text field).
    /// </summary>
    [Fact]
    public async Task Kaestchen_Zeigen_Laenge_Und_Trennzeichen()
    {
        var father = await TestApi.AdultAsync(factory);
        var card = (await CardsAsync(await TagsAsync(father), ClozeStage.LetterBoxes))[2];

        Assert.Equal(9, card.GetProperty("answerLength").GetInt32());
        Assert.Equal("____'_ __", card.GetProperty("answerPattern").GetString());
        Assert.Equal(JsonValueKind.Null, card.GetProperty("reveal").ValueKind);
    }

    /// <summary>
    /// AK 4: one word of 2 to 12 letters comes as scrambled tiles; anything else falls back to letter boxes on its own
    /// card. Red without the tile source (the one-word gap would come as boxes) or without the fallback (the two-word gap
    /// would come as bare free text).
    /// </summary>
    [Fact]
    public async Task Salat_Mischt_Ein_Wort_Und_Faellt_Sonst_Auf_Kaestchen_Zurueck()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await ClozeAsync(father, new
        {
            text = "I am {{1}}, {{2}}?",
            gaps = new[] { new { index = 1, answer = "tired" }, new { index = 2, answer = "aren't I" } },
        });

        var cards = await CardsAsync(exerciseId, ClozeStage.LetterScramble);

        var tiles = cards[0].GetProperty("letterTiles").EnumerateArray().Select(t => t.GetString()).ToList();
        Assert.Equal("tired".Order(), string.Concat(tiles).Order());
        Assert.Equal(JsonValueKind.Null, cards[0].GetProperty("answerLength").ValueKind);
        Assert.Equal(JsonValueKind.Null, cards[1].GetProperty("letterTiles").ValueKind);
        Assert.Equal("____'_ _", cards[1].GetProperty("answerPattern").GetString());
    }

    /// <summary>
    /// B-634 AK 1 + AK 3: a solution of several short words comes as tiles, each word's own letters in its own block and
    /// the word boundaries as the answer pattern; a word over 12 letters still falls back to letter boxes. The built
    /// answer is graded like any typed one. Red if several words fall back to boxes (old single-word rule), if the
    /// pattern is missing (the card could not show the boundaries) or if the long word is scrambled.
    /// </summary>
    [Fact]
    public async Task Salat_Mischt_Mehrere_Woerter_Je_Wort()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await ClozeAsync(father, new
        {
            text = "Call me {{1}} you can. {{2}}",
            gaps = new[] { new { index = 1, answer = "as soon as" }, new { index = 2, answer = "Western Mediterraneans" } },
        });

        var cards = await CardsAsync(exerciseId, ClozeStage.LetterScramble);

        var tiles = cards[0].GetProperty("letterTiles").EnumerateArray().Select(t => t.GetString()!).ToList();
        Assert.Equal("soon".Order(), string.Concat(tiles.Skip(2).Take(4)).Order());
        Assert.Equal("__ ____ __", cards[0].GetProperty("answerPattern").GetString());
        Assert.Equal(JsonValueKind.Null, cards[0].GetProperty("answerLength").ValueKind);
        Assert.Equal(JsonValueKind.Null, cards[1].GetProperty("letterTiles").ValueKind);
        Assert.Equal(22, cards[1].GetProperty("answerLength").GetInt32());
    }

    /// <summary>
    /// AK 5, derived distractors: the solution plus four other gaps' solutions, none twice. Red if the pool is not
    /// capped at four distractors (six options) or takes no other gaps' solutions (no options).
    /// </summary>
    [Fact]
    public async Task Auswahl_Bietet_Die_Loesung_Und_Vier_Andere_Luecken()
    {
        var father = await TestApi.AdultAsync(factory);
        var card = (await CardsAsync(await TagsAsync(father), ClozeStage.Choice))[0];

        var choices = Choices(card);
        Assert.Equal(1 + ClozeExerciseType.MaxGapDistractors, choices.Count);
        Assert.Contains("isn't it", choices);
        Assert.All(choices, c => Assert.Contains(c, Tags));
        Assert.Equal(choices.Count, choices.Distinct().Count());
    }

    /// <summary>
    /// AK 5 + AK 6, own distractors: a gap's own distractors replace the derived ones. Red if the derived ones were mixed
    /// in or taken instead (another gap's tag would stand among the options).
    /// </summary>
    [Fact]
    public async Task Auswahl_Nimmt_Die_Eigenen_Ablenker_Der_Luecke()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await ClozeAsync(father, new
        {
            text = "She works here, {{1}}? They came, {{2}}?",
            gaps = new object[]
            {
                new { index = 1, answer = "doesn't she", distractors = new[] { "don't she", "does she", "isn't she" } },
                new { index = 2, answer = "didn't they" },
            },
        });

        var choices = Choices((await CardsAsync(exerciseId, ClozeStage.Choice))[0]);

        Assert.Equal(["does she", "doesn't she", "don't she", "isn't she"], choices.Order());
    }

    /// <summary>
    /// AK 5, the floor: with fewer than two distractors the card is played as letter boxes. Red without the floor (a
    /// 50:50 choice) or without the fallback (a bare free-text field).
    /// </summary>
    [Fact]
    public async Task Auswahl_Unter_Zwei_Ablenkern_Wird_Zu_Kaestchen()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await ClozeAsync(father, new
        {
            text = "It is cold, {{1}}? You can swim, {{2}}?",
            gaps = new[] { new { index = 1, answer = "isn't it" }, new { index = 2, answer = "can't you" } },
        });

        var card = (await CardsAsync(exerciseId, ClozeStage.Choice))[0];

        Assert.Empty(Choices(card));
        Assert.Equal("___'_ __", card.GetProperty("answerPattern").GetString());
    }

    /// <summary>
    /// Risk 3: another gap's solution that this gap accepts is no distractor - here one of its declared alternatives
    /// ("is it not"), which only the grader knows - and two other gaps with one solution give one distractor. Red without
    /// the grader check (a second right button) or without the dedupe ("can't you" twice).
    /// </summary>
    [Fact]
    public async Task Auswahl_Zeigt_Keinen_Ablenker_Der_Auch_Richtig_Ist()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await ClozeAsync(father, new
        {
            text = "It is cold, {{1}}? It is late, {{2}} You can swim, {{3}}? He came, {{4}}? You can run, {{5}}",
            gaps = new object[]
            {
                new { index = 1, answer = "isn't it", alternatives = new[] { "is it not" } },
                new { index = 2, answer = "is it not" },
                new { index = 3, answer = "can't you" }, new { index = 4, answer = "didn't he" },
                new { index = 5, answer = "Can't you." },
            },
        });

        var choices = Choices((await CardsAsync(exerciseId, ClozeStage.Choice))[0]);

        Assert.Equal(["can't you", "didn't he", "isn't it"], choices.Order());
    }

    /// <summary>
    /// Risk 2: the options share one form, so none names itself - a gap opening a sentence gets all options capitalised,
    /// a gap inside one gets none capitalised and none with a closing mark. Red without the shaping ("Isn't it?" would
    /// stand out among lower-case tags).
    /// </summary>
    [Fact]
    public async Task Auswahl_Zeigt_Alle_Optionen_In_Einer_Form()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await ClozeAsync(father, new
        {
            text = "{{1}} You can swim, {{2}}? He came, {{3}}? They left, {{4}}?",
            gaps = new[]
            {
                new { index = 1, answer = "Isn't it?" }, new { index = 2, answer = "can't you" },
                new { index = 3, answer = "didn't he" }, new { index = 4, answer = "didn't they" },
            },
        });

        var cards = await CardsAsync(exerciseId, ClozeStage.Choice);

        Assert.All(Choices(cards[0]), c => Assert.True(char.IsUpper(c[0]), c));
        Assert.All(Choices(cards[1]), c => Assert.True(char.IsLower(c[0]) && !c.EndsWith('?'), c));
    }

    /// <summary>
    /// Review finding on risk 2: a capital is lowered only where every other option starts lower-case, as in the word
    /// bank. "I'm" opening a sentence beside the asked "I'll" keeps its capital - lowered, the solution would be the only
    /// option with one. Red when positional capitals are lowered whatever the rest looks like.
    /// </summary>
    [Fact]
    public async Task Auswahl_Senkt_Keinen_Grossbuchstaben_Der_Die_Loesung_Verriete()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await ClozeAsync(father, new
        {
            text = "{{1}} late. Yes, {{2}} going. She {{3}} here. They {{4}} there.",
            gaps = new[]
            {
                new { index = 1, answer = "I'm" }, new { index = 2, answer = "I'll" },
                new { index = 3, answer = "works" }, new { index = 4, answer = "are" },
            },
        });

        var choices = Choices((await CardsAsync(exerciseId, ClozeStage.Choice))[1]);

        Assert.Contains("I'm", choices);
        Assert.NotEqual(1, choices.Count(c => char.IsUpper(c[0])));
    }

    /// <summary>
    /// Review finding on risk 2: apostrophes and spaces are shown in one spelling - an author's distractors typed on a
    /// phone ("don’t she") must not differ from a solution with a straight one. Red without the folding.
    /// </summary>
    [Fact]
    public async Task Auswahl_Zeigt_Apostrophe_In_Einer_Schreibweise()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await ClozeAsync(father, new
        {
            text = "She works here, {{1}}? They came, {{2}}?",
            gaps = new object[]
            {
                new { index = 1, answer = "doesn't she", distractors = new[] { "don’t she", "isn’t  she" } },
                new { index = 2, answer = "didn't they" },
            },
        });

        var choices = Choices((await CardsAsync(exerciseId, ClozeStage.Choice))[0]);

        Assert.Equal(["doesn't she", "don't she", "isn't she"], choices.Order());
    }

    /// <summary>
    /// Decision 2, "own distractors take precedence": a gap with a single own distractor is topped up with another gap's
    /// solution instead of falling back to letter boxes. Red when the own ones exclude the derived ones (no choice) or
    /// when the derived ones fill up to four although the author wrote one (the curated one drowned).
    /// </summary>
    [Fact]
    public async Task Auswahl_Fuellt_Einen_Eigenen_Ablenker_Mit_Einer_Anderen_Luecke_Auf()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await ClozeAsync(father, new
        {
            text = TagText(),
            gaps = Tags.Select((t, i) => i == 0
                ? (object)new { index = 1, answer = t, distractors = new[] { "is it" } }
                : new { index = i + 1, answer = t }).ToArray(),
        });

        var choices = Choices((await CardsAsync(exerciseId, ClozeStage.Choice))[0]);

        Assert.Equal(3, choices.Count);
        Assert.Contains("is it", choices);
        Assert.Contains("can't you", choices);
    }

    /// <summary>
    /// Risk 1: the choice per gap is shuffled like any question's options - the solution is not always the first button.
    /// Red while the cloze's shared-pool rule covers this stage too (the solution stands first on every card).
    /// </summary>
    [Fact]
    public async Task Auswahl_Verraet_Die_Loesung_Nicht_Durch_Ihren_Platz()
    {
        var father = await TestApi.AdultAsync(factory);
        var cards = await CardsAsync(await TagsAsync(father), ClozeStage.Choice);

        var places = cards.Values.Select(c => Choices(c).IndexOf(Tags[c.GetProperty("itemIndex").GetInt32()])).ToList();

        Assert.DoesNotContain(-1, places);
        Assert.True(places.Distinct().Count() > 1, string.Join(",", places));
    }

    /// <summary>
    /// AK 2: every new stage shows the gap's translation as the hint where there is one, and none where there is none.
    /// Red if a stage dropped the hint of a store-backed gap.
    /// </summary>
    [Theory]
    [InlineData(ClozeStage.LetterBoxes)]
    [InlineData(ClozeStage.LetterScramble)]
    [InlineData(ClozeStage.Choice)]
    public async Task Die_Stufe_Zeigt_Die_Uebersetzung_Wo_Es_Eine_Gibt(ClozeStage stage)
    {
        var father = await TestApi.AdultAsync(factory);
        var word = TestApi.UniqueName("station").Replace(" ", "");
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, word, "Bahnhof");
        var exerciseId = await ClozeAsync(father, new
        {
            text = "Go to the {{1}}. It is cold, {{2}}? You can swim, {{3}}?",
            gaps = new object[]
            {
                new { index = 1, answer = "", vocabKey = key },
                new { index = 2, answer = "isn't it" },
                new { index = 3, answer = "can't you" },
            },
        });

        var cards = await CardsAsync(exerciseId, stage);

        Assert.Equal("Bahnhof", cards[0].GetProperty("hint").GetString());
        Assert.Equal(JsonValueKind.Null, cards[1].GetProperty("hint").ValueKind);
    }

    /// <summary>
    /// AK 7: the test plays the position's stage like practice and grades the pick the same way. Red if the test item lost
    /// the choice (the shared projection) or graded otherwise.
    /// </summary>
    [Fact]
    public async Task Test_Spielt_Die_Auswahl_Und_Wertet_Wie_Das_Ueben()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await TagsAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)ClozeStage.Choice);
        var child = await TestApi.ChildAsync(factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";
        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");

        var item = (await child.GetFromJsonAsync<JsonElement>($"{baseUrl}/{attemptId}/next")).GetProperty("item");
        var index = item.GetProperty("itemIndex").GetInt32();
        Assert.Equal(1 + ClozeExerciseType.MaxGapDistractors, Choices(item).Count);
        (await child.PostAsJsonAsync($"{baseUrl}/{attemptId}/answer", new { itemIndex = index, givenAnswer = Tags[index] }))
            .EnsureSuccessStatusCode();
        var result = await (await child.PostAsJsonAsync($"{baseUrl}/{attemptId}/submit", new { }))
            .Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(1, result.GetProperty("correctItems").GetInt32());
    }
}
