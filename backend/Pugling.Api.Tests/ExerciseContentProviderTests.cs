using System.Text.Json;
using Pugling.Api.Exercises;

namespace Pugling.Api.Tests;

/// <summary>Unit tests of content extraction from the exercise config (stateless, without DB/HTTP).</summary>
public class ExerciseContentProviderTests
{
    // A registry from the built-in types - the provider delegates the projection to them.
    private static readonly ExerciseTypeRegistry Registry = new(
    [
        new VocabularyExerciseType(), new ReadingExerciseType(), new ClozeExerciseType(),
        new EssayExerciseType(), new ListeningExerciseType(), new GrammarExerciseType(),
        new MatchingExerciseType(), new TranslationExerciseType(), new ArithmeticExerciseType(),
        new ArithmeticDrillExerciseType(new ArithmeticProblemGenerator()), new ListExerciseType(),
        new BirkenbihlExerciseType(),
    ]);
    private readonly ExerciseContentProvider _provider = new(Registry);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static string Json<T>(T config) => JsonSerializer.Serialize(config, JsonOptions);

    // ---- Vocabulary ---- ----

    /// <summary>
    /// For vocabulary the stateless provider deliberately returns <b>nothing</b>: its contents live in the
    /// <c>ExerciseItem</c> table, and <c>VocabularyConfig.Items</c>/<c>.Refs</c> are a pure <b>input shape</b>
    /// (after creation <c>AfterSaveAsync</c> clears them).
    /// <para>
    /// The counter-check used to sit here - this test checked the config projection, i.e. the <i>second</i>
    /// content path of the same type. Two paths mean two truths: one carries stable ItemIds and with them the
    /// cross-plan learning state, the other does not. It had long been unreachable; now it is gone, and this
    /// test keeps it that way.
    /// </para>
    /// </summary>
    [Fact]
    public void Vocabulary_LiefertKeineItemsAusDerConfig()
    {
        var config = new VocabularyConfig
        {
            Items =
            {
                new VocabItem("hello", "hallo"),
                new VocabItem("please", "bitte", "Höflichkeit"),
            }
        };

        Assert.Empty(_provider.ItemsOf(ExerciseTypeKeys.Vocabulary, Json(config)));
    }

    // ---- Cloze ---- ----

    [Fact]
    public void Cloze_EinItemJeLuecke_MitGapIndexUndAlternativen()
    {
        var config = new ClozeConfig
        {
            Text = "A: {{1}}, how are you? B: I'm {{2}}.",
            Gaps =
            {
                new Gap(1, "Hello", new() { "Hi" }),
                new Gap(2, "fine", new() { "good", "well" }),
            },
        };

        var items = _provider.ItemsOf(ExerciseTypeKeys.Cloze, Json(config));

        Assert.Equal(2, items.Count);
        // The prompt is the carrier text, GapIndex the {{n}} number.
        Assert.Equal(config.Text, items[0].Prompt);
        Assert.Equal(1, items[0].GapIndex);
        Assert.Equal("Hello", items[0].Answer);
        Assert.Equal(["Hello", "Hi"], items[0].AcceptedAnswers);
        Assert.Equal(["fine", "good", "well"], items[1].AcceptedAnswers);
        Assert.Equal(2, items[1].GapIndex);
    }

    // ---- Matching ---- ----

    [Fact]
    public void Matching_LinksIstPromptRechtsIstAntwort()
    {
        var config = new MatchingConfig { Pairs = { new("Bayern", "München"), new("Hessen", "Wiesbaden") } };

        var items = _provider.ItemsOf(ExerciseTypeKeys.Matching, Json(config));

        Assert.Equal(2, items.Count);
        Assert.Equal("Bayern", items[0].Prompt);
        Assert.Equal("München", items[0].Answer);
    }

    // ---- List ---- ----

    [Fact]
    public void List_NutztInstructionAlsPromptUndUebernimmtAlternativen()
    {
        var config = new ListConfig
        {
            Instruction = "Nenne alle Bundesländer.",
            Items = { new("Nordrhein-Westfalen", new() { "NRW" }), new("Bayern") },
        };

        var items = _provider.ItemsOf(ExerciseTypeKeys.List, Json(config));

        Assert.Equal("Nenne alle Bundesländer.", items[0].Prompt);
        Assert.Equal(["Nordrhein-Westfalen", "NRW"], items[0].AcceptedAnswers);
        Assert.Equal(["Bayern"], items[1].AcceptedAnswers);
    }

    // ---- Arithmetic (fixed tasks) ---- ----

    [Fact]
    public void Arithmetic_AntwortAlsInvarianteDezimalzahl()
    {
        var config = new ArithmeticConfig { Problems = { new("7 × 6", 42m), new("10 ÷ 4", 2.5m) } };

        var items = _provider.ItemsOf(ExerciseTypeKeys.Arithmetic, Json(config));

        Assert.Equal("7 × 6", items[0].Prompt);
        Assert.Equal("42", items[0].Answer);
        // The decimal separator stays a dot regardless of culture (as in ExerciseAnswerChecker).
        Assert.Equal("2.5", items[1].Answer);
    }

    // ---- Grammar / translation / questions ---- ----

    [Fact]
    public void Grammar_UebernimmtRegelHinweisAlsHint()
    {
        var config = new GrammarConfig { Tasks = { new GrammarTask("go (past)", "went", "unregelmäßig") } };

        var item = Assert.Single(_provider.ItemsOf(ExerciseTypeKeys.Grammar, Json(config)));

        Assert.Equal("went", item.Answer);
        Assert.Equal("unregelmäßig", item.Hint);
    }

    [Fact]
    public void Translation_QuelleIstPromptZielInklAlternativen()
    {
        var config = new TranslationConfig
        {
            Items = { new TranslationItem("Guten Tag", "Good day", new() { "Hello" }) }
        };

        var item = Assert.Single(_provider.ItemsOf(ExerciseTypeKeys.Translation, Json(config)));

        Assert.Equal("Guten Tag", item.Prompt);
        Assert.Equal(["Good day", "Hello"], item.AcceptedAnswers);
    }

    [Fact]
    public void Reading_ProjiziertVerstaendnisfragen()
    {
        var config = new ReadingConfig
        {
            Text = "A short text.",
            Questions = { new Question("Who?", null, "Tom"), new Question("Where?", new() { "A", "B" }, "A") },
        };

        var items = _provider.ItemsOf(ExerciseTypeKeys.Reading, Json(config));

        Assert.Equal(2, items.Count);
        Assert.Equal("Who?", items[0].Prompt);
        Assert.Equal("Tom", items[0].Answer);
    }

    // ---- Birkenbihl (a pure content exercise) ---- ----

    [Fact]
    public void Birkenbihl_SatzIstPromptNatuerlicheUebersetzungIstAntwort()
    {
        var config = new BirkenbihlConfig
        {
            Sentences =
            {
                new BirkenbihlSentence(1, "What is your name?", "Wie heißt du?",
                    [new WordPair(1, "What", "Was", null), new WordPair(2, "is", "ist", null)]),
            }
        };

        var item = Assert.Single(_provider.ItemsOf(ExerciseTypeKeys.Birkenbihl, Json(config)));

        Assert.Equal("What is your name?", item.Prompt);
        Assert.Equal("Wie heißt du?", item.Answer);
    }

    // ---- Types without fixed items ---- ----

    [Theory]
    [InlineData(ExerciseTypeKeys.Essay)]
    [InlineData(ExerciseTypeKeys.ArithmeticDrill)]
    public void OhneFesteItems_LiefertLeereListe(string type)
    {
        // Essay = free text; ArithmeticDrill = generated per request - neither has countable contents.
        Assert.Empty(_provider.ItemsOf(type, "{}"));
    }

    // ---- Robustness ---- ----

    [Theory]
    [InlineData("")]
    [InlineData("{}")]
    public void LeereOderInhaltsloseConfig_LiefertLeereListe(string configJson)
    {
        Assert.Empty(_provider.ItemsOf(ExerciseTypeKeys.Vocabulary, configJson));
    }
}
