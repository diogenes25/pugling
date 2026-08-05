using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Answer choices on the way to the child (B-73). The creator could type them into a reading or listening
/// question and the child never saw them: the contract carried <c>Question.Choices</c>, both frontends
/// rendered <c>choices</c>, but no exercise type ever answered the question "what are the options here?" for
/// these types. Same shape for matching, whose <c>MatchStage.Distractors</c> delivered cards identical to
/// <c>Direct</c>.
/// <para>
/// Every test here fails against the state before that story – that is the point: the wiring is invisible
/// from the outside, so only a test that reads the card can hold it.
/// </para>
/// </summary>
public class ExerciseChoicesTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    private static readonly string[] Cities = ["Leeds", "York", "Hull"];

    private static async Task<int> CreateAsync(HttpClient father, string route, object config)
    {
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName($"Auswahl-{route}") }));
        var seriesId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new
            {
                name = TestApi.UniqueName($"Reihe-{route}"),
                publisher = (string?)null,
                subjectName = (string?)null,
                subjectId,
                schoolTypes = (string?)null,
                sourceLanguage = (string?)null,
                targetLanguage = (string?)null,
                notes = (string?)null,
            }));
        var seriesUnitId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units", new { label = "Unit 1", orderIndex = 1 }));
        return await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/{route}",
            new { title = TestApi.UniqueName("Auswahl-Übung"), orderIndex = 1, rewardPoints = 5, config }));
    }

    private static async Task<JsonElement> CardsAsync(HttpClient child, int planId, int positionId)
    {
        var baseUrl = TestApi.PracticeBase(planId, positionId);
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { mode = "Info" }));
        return await child.GetFromJsonAsync<JsonElement>($"{baseUrl}/{sessionId}/cards");
    }

    private async Task<JsonElement> PlayAsync(string route, object config, int stage)
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await CreateAsync(father, route, config);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, stage);
        var child = await TestApi.ChildAsync(_factory);
        return await CardsAsync(child, planId, positionId);
    }

    private static string[] ChoicesOf(JsonElement card) =>
        [.. card.GetProperty("choices").EnumerateArray().Select(c => c.GetString()!)];

    // ---- Reading and listening: the creator's list, per question ----

    /// <summary>
    /// The options arrive in the author's order. They are curated content, not a generated pool – a
    /// rotation would scramble an order that can carry meaning (a timeline, a "none of these" at the end).
    /// </summary>
    [Fact]
    public async Task Leseverstehen_LiefertDieAntwortmoeglichkeiten()
    {
        var cards = await PlayAsync("reading", new
        {
            text = "B lives in York and works in Leeds.",
            questions = new[] { new { prompt = "Where does B live?", choices = Cities, answer = "York" } },
        }, (int)TestStage.FreeText);

        Assert.Equal(Cities, ChoicesOf(cards.EnumerateArray().Single()));
    }

    [Fact]
    public async Task Hoerverstehen_LiefertDieAntwortmoeglichkeiten()
    {
        var cards = await PlayAsync("listening", new
        {
            audioUrl = "https://example.invalid/dialogue.mp3",
            questions = new[] { new { prompt = "Where is B from?", choices = Cities, answer = "Leeds" } },
        }, (int)TestStage.FreeText);

        Assert.Equal(Cities, ChoicesOf(cards.EnumerateArray().Single()));
    }

    /// <summary>
    /// Mixed within one exercise: the decision falls per question, so a run may hold a multiple-choice
    /// question next to a free-text one. A per-position switch would have forced the author to split them.
    /// </summary>
    [Fact]
    public async Task GemischteFragen_NurDieMitMoeglichkeitenBekommenSie()
    {
        var cards = await PlayAsync("reading", new
        {
            text = "B lives in York.",
            questions = new object[]
            {
                new { prompt = "Where does B live?", choices = Cities, answer = "York" },
                new { prompt = "Spell the town.", answer = "York" },
            },
        }, (int)TestStage.FreeText);

        var byPrompt = cards.EnumerateArray().ToDictionary(c => c.GetProperty("prompt").GetString()!);
        Assert.Equal(Cities, ChoicesOf(byPrompt["Where does B live?"]));
        JsonAssert.Null(byPrompt["Spell the town."], "choices");
    }

    /// <summary>
    /// Blank entries drop out. The editor's repeat field can leave an empty row behind, and an option
    /// without a label would arrive as an unlabelled button the child can still pick.
    /// </summary>
    [Fact]
    public async Task LeereMoeglichkeiten_KommenNichtAlsOption()
    {
        var cards = await PlayAsync("reading", new
        {
            text = "B lives in York.",
            questions = new[]
            {
                new { prompt = "Where does B live?", choices = new[] { "York", "   ", "Hull" }, answer = "York" },
            },
        }, (int)TestStage.FreeText);

        Assert.Equal(["York", "Hull"], ChoicesOf(cards.EnumerateArray().Single()));
    }

    /// <summary>
    /// The options never come with the solution. Reading and listening are typed on every stage (the base
    /// class answers <c>true</c>), so <c>Reveal</c> stays empty – picking from a list next to the printed
    /// answer would be a click, not a question.
    /// </summary>
    [Fact]
    public async Task Antwortmoeglichkeiten_DeckenDieLoesungNichtAuf()
    {
        var cards = await PlayAsync("reading", new
        {
            text = "B lives in York.",
            questions = new[] { new { prompt = "Where does B live?", choices = Cities, answer = "York" } },
        }, (int)TestStage.SelfAssess);

        var card = cards.EnumerateArray().Single();
        JsonAssert.NotNull(card, "choices");
        JsonAssert.Null(card, "reveal");
    }

    // ---- The exam and the supervisor's preview run the same projection ----

    [Fact]
    public async Task Klausur_LiefertDieAntwortmoeglichkeitenEbenfalls()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await CreateAsync(father, "reading", new
        {
            text = "B lives in York.",
            questions = new[] { new { prompt = "Where does B live?", choices = Cities, answer = "York" } },
        });
        var (planId, positionId) = TestApi.SeedLeitnerPosition(
            _factory, exerciseId, (int)TestStage.FreeText, requireTypedTest: true);
        var child = await TestApi.ChildAsync(_factory);

        var testUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";
        var attemptId = await TestApi.IdWithKeyAsync(
            await child.PostAsJsonAsync(testUrl, new { stage = (int)TestStage.FreeText }), "attemptId");
        var next = await child.GetFromJsonAsync<JsonElement>($"{testUrl}/{attemptId}/next");

        Assert.Equal(Cities, ChoicesOf(next.GetProperty("item")));
    }

    /// <summary>
    /// The supervisor tries out the exercise before assigning it – so the preview has to show the form the
    /// child gets, options included, or the try-out would not reveal a broken question form.
    /// </summary>
    [Fact]
    public async Task Testmodus_ZeigtDieAntwortmoeglichkeiten()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await CreateAsync(father, "reading", new
        {
            text = "B lives in York.",
            questions = new[] { new { prompt = "Where does B live?", choices = Cities, answer = "York" } },
        });

        var preview = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}/preview");

        Assert.Equal(Cities, ChoicesOf(preview.GetProperty("items").EnumerateArray().Single()));
    }

    // ---- Matching: the pool is the other column, and only on the stage named after it ----

    /// <summary>
    /// <see cref="MatchStage.Distractors"/> offers the counterparts of the other pairs; the solution is one
    /// of them and never labelled as such. Deliberately no curated distractor list in the config: the
    /// plausible wrong options of a matching exercise <i>are</i> its other right-hand entries.
    /// </summary>
    [Fact]
    public async Task Zuordnung_MitAblenkern_LiefertDieGegenstueckeDerAnderenPaare()
    {
        var pairs = new Dictionary<string, string>
        {
            ["Hund"] = "dog",
            ["Katze"] = "cat",
            ["Maus"] = "mouse",
        };

        var cards = await PlayAsync("matching", new
        {
            instruction = "Ordne zu.",
            pairs = pairs.Select(p => new { left = p.Key, right = p.Value }),
        }, (int)MatchStage.Distractors);

        Assert.Equal(3, cards.GetArrayLength());
        foreach (var card in cards.EnumerateArray())
        {
            var choices = ChoicesOf(card);
            // Every card offers the whole right-hand column here (three pairs, up to three distractors), so
            // the pool must hold the own solution and nothing invented.
            Assert.Contains(pairs[card.GetProperty("prompt").GetString()!], choices);
            Assert.Equal(3, choices.Distinct().Count());
            Assert.All(choices, c => Assert.Contains(c, pairs.Values));
            // The solution must not sit in front on every card, otherwise the first option is the answer key.
            JsonAssert.Null(card, "reveal");
        }

        // That is what the rotation is for: with the solution always first, position alone would give it away.
        var firsts = cards.EnumerateArray().Select(c => ChoicesOf(c)[0]).Distinct().Count();
        Assert.True(firsts > 1, "the first option must not be the solution on every card");
    }

    /// <summary>
    /// One pair, no distractor to offer – so no pool at all. Otherwise the card would carry exactly one
    /// option, and that option is the solution: the child taps the only button and passes. Same for an
    /// exercise whose pairs all share one counterpart, where deduplication eats the last candidate.
    /// </summary>
    [Theory]
    [InlineData("ein einziges Paar")]
    [InlineData("alle Paare mit derselben rechten Spalte")]
    public async Task Zuordnung_OhneEchtenAblenker_LiefertKeineAuswahl(string fall)
    {
        var pairs = fall == "ein einziges Paar"
            ? new[] { new { left = "Hund", right = "dog" } }
            : [new { left = "Hund", right = "dog" }, new { left = "Köter", right = "dog" }];

        var cards = await PlayAsync("matching", new { instruction = "Ordne zu.", pairs },
            (int)MatchStage.Distractors);

        Assert.All(cards.EnumerateArray(), card => JsonAssert.Null(card, "choices"));
    }

    /// <summary>
    /// A duplicated counterpart is never offered twice: two pairs sharing "dog" plus a third leave one
    /// distinct distractor, not two identical buttons.
    /// </summary>
    [Fact]
    public async Task Zuordnung_DoppelteRechteSpalte_BietetKeineOptionZweimal()
    {
        var cards = await PlayAsync("matching", new
        {
            instruction = "Ordne zu.",
            pairs = new[]
            {
                new { left = "Hund", right = "dog" },
                new { left = "Köter", right = "dog" },
                new { left = "Katze", right = "cat" },
            },
        }, (int)MatchStage.Distractors);

        foreach (var card in cards.EnumerateArray())
        {
            var choices = ChoicesOf(card);
            Assert.Equal(choices.Length, choices.Distinct().Count());
        }
    }

    /// <summary>
    /// Listening in the exam and in the preview. Reading covers both paths above; the second comprehension
    /// type goes through its own <c>Choices</c> override, so "the sibling works" proves nothing about it.
    /// </summary>
    [Fact]
    public async Task Hoerverstehen_KlausurUndTestmodus_LiefernDieMoeglichkeiten()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await CreateAsync(father, "listening", new
        {
            audioUrl = "https://example.invalid/dialogue.mp3",
            questions = new[] { new { prompt = "Where is B from?", choices = Cities, answer = "Leeds" } },
        });

        var preview = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}/preview");
        Assert.Equal(Cities, ChoicesOf(preview.GetProperty("items").EnumerateArray().Single()));

        var (planId, positionId) = TestApi.SeedLeitnerPosition(
            _factory, exerciseId, (int)TestStage.FreeText, requireTypedTest: true);
        var child = await TestApi.ChildAsync(_factory);
        var testUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";
        var attemptId = await TestApi.IdWithKeyAsync(
            await child.PostAsJsonAsync(testUrl, new { stage = (int)TestStage.FreeText }), "attemptId");
        var next = await child.GetFromJsonAsync<JsonElement>($"{testUrl}/{attemptId}/next");

        Assert.Equal(Cities, ChoicesOf(next.GetProperty("item")));
    }

    /// <summary>The other stage keeps free text – <c>Direct</c> is the harder form and stays that way.</summary>
    [Fact]
    public async Task Zuordnung_OhneAblenker_BleibtOhneOptionen()
    {
        var cards = await PlayAsync("matching", new
        {
            instruction = "Ordne zu.",
            pairs = new[] { new { left = "Hund", right = "dog" }, new { left = "Katze", right = "cat" } },
        }, (int)MatchStage.Direct);

        Assert.All(cards.EnumerateArray(), card => JsonAssert.Null(card, "choices"));
    }
}
