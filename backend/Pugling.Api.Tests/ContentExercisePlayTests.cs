using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Playing the content-driven exercise types (B-75): reading, listening, grammar. Their content atom is a
/// question, but the thing the question is <i>about</i> – the text, the recording, the instruction covering
/// all tasks – belongs to the exercise. Until now it was dropped on the way to the card, so the child got a
/// question without anything to answer it from. <c>PositionPlayModesTests</c> plays a reading position five
/// times over and never looks inside the card; this class does nothing else.
/// </summary>
public class ContentExercisePlayTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    private const string Text = "Tom goes to Brighton in July.";
    private const string Audio = "https://example.invalid/dialogue.mp3";

    private static async Task<int> CreateAsync(HttpClient father, string route, object config)
    {
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName($"Inhalt-{route}") }));
        var chapterId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "Unit 1", orderIndex = 1 }));
        return await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/{route}",
            new { title = TestApi.UniqueName("Inhalts-Übung"), orderIndex = 1, rewardPoints = 5, config }));
    }

    private static async Task<JsonElement> CardsAsync(HttpClient child, int planId, int positionId)
    {
        var baseUrl = TestApi.PracticeBase(planId, positionId);
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { mode = "Info" }));
        return await child.GetFromJsonAsync<JsonElement>($"{baseUrl}/{sessionId}/cards");
    }

    private async Task<JsonElement> PlayAsync(string route, object config)
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await CreateAsync(father, route, config);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        return await CardsAsync(child, planId, positionId);
    }

    // ---- Reading: the text belongs on the card, not in the question ---- ----

    [Fact]
    public async Task Leseverstehen_LiefertDenTextZuJederFrage()
    {
        var cards = await PlayAsync("reading", new
        {
            text = Text,
            questions = new[]
            {
                new { prompt = "Where does Tom go?", answer = "Brighton" },
                new { prompt = "When?", answer = "July" },
            },
        });

        Assert.Equal(2, cards.GetArrayLength());
        foreach (var card in cards.EnumerateArray())
        {
            Assert.Equal(Text, card.GetProperty("passage").GetString());
            // The question stays the question - the text must not be folded into it, or every evaluation
            // line (ItemOutcome, history) would carry a paragraph of prose.
            Assert.DoesNotContain("Brighton in July", card.GetProperty("prompt").GetString()!);
        }
    }

    // ---- Listening: the recording, and NOT the transcript ---- ----

    [Fact]
    public async Task Hoerverstehen_LiefertDieAufnahme_AberNichtDasTranskript()
    {
        var cards = await PlayAsync("listening", new
        {
            audioUrl = Audio,
            transcript = "A: Where are you from? B: I'm from Leeds.",
            questions = new[] { new { prompt = "Where is B from?", answer = "Leeds" } },
        });

        var card = cards.EnumerateArray().Single();
        Assert.Equal(Audio, card.GetProperty("audioUrl").GetString());
        // The child needs BOTH here: without the question the recording is just noise.
        Assert.Equal("Where is B from?", card.GetProperty("prompt").GetString());
        // Anti-cheat: the transcript is for the creator. It must not arrive anywhere on the card.
        Assert.DoesNotContain("Leeds", JsonSerializer.Serialize(card).Replace("\"answer\"", ""));
        JsonAssert.Null(card, "passage");
    }

    // ---- Grammar: the instruction covering all tasks ---- ----

    [Fact]
    public async Task Grammatik_LiefertDieUebergreifendeAnweisung()
    {
        var cards = await PlayAsync("grammar", new
        {
            instruction = "Setze das Verb ins Simple Past.",
            tasks = new[] { new { prompt = "He ___ (go) to school.", answer = "went" } },
        });

        var card = cards.EnumerateArray().Single();
        Assert.Equal("Setze das Verb ins Simple Past.", card.GetProperty("passage").GetString());
        Assert.Equal("He ___ (go) to school.", card.GetProperty("prompt").GetString());
    }

    // ---- E3: the anti-cheat rule moves from the frontend to the server ---- ----

    [Fact]
    public async Task VokabelHoerstufe_LiefertKeinenPrompt_AndereStufenSchon()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, TestApi.UniqueName("hello"), "hallo");
        var vocabId = await TestApi.ResolveVocabIdAsync(father, key);
        (await father.PatchAsJsonAsync($"/api/v1/creator/vocabulary/{vocabId}",
            new { pronunciationAudioUrl = Audio })).EnsureSuccessStatusCode();
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, key);
        var child = await TestApi.ChildAsync(_factory);

        var (audioPlan, audioPos) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.Audio);
        var listening = (await CardsAsync(child, audioPlan, audioPos)).EnumerateArray().Single();
        // Hearing AND reading the word would make "listen, then type" a reading task. The server decides
        // that, not the renderer - a frontend that hides the prompt itself is an anti-cheat rule in the
        // wrong place.
        Assert.Equal(Audio, listening.GetProperty("audioUrl").GetString());
        JsonAssert.Null(listening, "prompt");

        var (typePlan, typePos) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var typing = (await CardsAsync(child, typePlan, typePos)).EnumerateArray().Single();
        Assert.False(IsNull(typing, "prompt"));
    }

    /// <summary>
    /// The safety net behind E3: without a recording the prompt stays, or the listening stage would hand out
    /// a card that is blank on both counts – no word to read and nothing to hear.
    /// </summary>
    [Fact]
    public async Task Hoerstufe_OhneAufnahme_BehaeltDenPrompt()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ("silent", "still"));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.Audio);
        var child = await TestApi.ChildAsync(_factory);

        var card = (await CardsAsync(child, planId, positionId)).EnumerateArray().Single();

        JsonAssert.Null(card, "audioUrl");
        Assert.Equal("silent", card.GetProperty("prompt").GetString());
    }

    private static bool IsNull(JsonElement el, string property) =>
        el.GetProperty(property).ValueKind == JsonValueKind.Null;

    // ---- E4: the supervisor's preview shows what the child sees ---- ----

    [Fact]
    public async Task Testmodus_ZeigtDenselbenInhaltWieDieKarte()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await CreateAsync(father, "reading", new
        {
            text = Text,
            questions = new[] { new { prompt = "Where does Tom go?", answer = "Brighton" } },
        });

        var preview = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}/preview");
        var item = preview.GetProperty("items").EnumerateArray().Single();

        Assert.Equal(Text, item.GetProperty("passage").GetString());
    }

    // ---- The exam pulls along ---- ----

    [Fact]
    public async Task Klausur_LiefertDenTextEbenfalls()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await CreateAsync(father, "reading", new
        {
            text = Text,
            questions = new[] { new { prompt = "Where does Tom go?", answer = "Brighton" } },
        });
        var (planId, positionId) = TestApi.SeedLeitnerPosition(
            _factory, exerciseId, (int)TestStage.FreeText, requireTypedTest: true);
        var child = await TestApi.ChildAsync(_factory);

        var testUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";
        var attemptId = await TestApi.IdWithKeyAsync(
            await child.PostAsJsonAsync(testUrl, new { stage = (int)TestStage.FreeText }), "attemptId");
        var next = await child.GetFromJsonAsync<JsonElement>($"{testUrl}/{attemptId}/next");

        Assert.Equal(Text, next.GetProperty("item").GetProperty("passage").GetString());
    }
}
