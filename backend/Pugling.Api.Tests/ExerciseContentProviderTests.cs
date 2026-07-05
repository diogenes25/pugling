using System.Text.Json;
using Pugling.Api.Models;
using Pugling.Api.Services;

namespace Pugling.Api.Tests;

/// <summary>Unit-Tests der Inhalts-Extraktion aus der Übungs-Config (zustandslos, ohne DB/HTTP).</summary>
public class ExerciseContentProviderTests
{
    private readonly ExerciseContentProvider _provider = new();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static string Json<T>(T config) => JsonSerializer.Serialize(config, JsonOptions);

    // ---- Vokabeln ----

    [Fact]
    public void Vocabulary_ProjiziertVorderseiteRueckseiteUndHinweis()
    {
        var config = new VocabularyConfig
        {
            Items =
            {
                new VocabItem("hello", "hallo"),
                new VocabItem("please", "bitte", "Höflichkeit"),
            }
        };

        var items = _provider.ItemsOf(ExerciseType.Vocabulary, Json(config));

        Assert.Equal(2, items.Count);
        Assert.Equal(0, items[0].Index);
        Assert.Equal("hello", items[0].Prompt);
        Assert.Equal("hallo", items[0].Answer);
        Assert.Equal(["hallo"], items[0].AcceptedAnswers);
        Assert.Null(items[0].Hint);
        Assert.Equal("Höflichkeit", items[1].Hint);
    }

    // ---- Lückentext ----

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

        var items = _provider.ItemsOf(ExerciseType.Cloze, Json(config));

        Assert.Equal(2, items.Count);
        // Prompt ist der Trägertext, GapIndex die {{n}}-Nummer.
        Assert.Equal(config.Text, items[0].Prompt);
        Assert.Equal(1, items[0].GapIndex);
        Assert.Equal("Hello", items[0].Answer);
        Assert.Equal(["Hello", "Hi"], items[0].AcceptedAnswers);
        Assert.Equal(["fine", "good", "well"], items[1].AcceptedAnswers);
        Assert.Equal(2, items[1].GapIndex);
    }

    // ---- Zuordnung ----

    [Fact]
    public void Matching_LinksIstPromptRechtsIstAntwort()
    {
        var config = new MatchingConfig { Pairs = { new("Bayern", "München"), new("Hessen", "Wiesbaden") } };

        var items = _provider.ItemsOf(ExerciseType.Matching, Json(config));

        Assert.Equal(2, items.Count);
        Assert.Equal("Bayern", items[0].Prompt);
        Assert.Equal("München", items[0].Answer);
    }

    // ---- Liste ----

    [Fact]
    public void List_NutztInstructionAlsPromptUndUebernimmtAlternativen()
    {
        var config = new ListConfig
        {
            Instruction = "Nenne alle Bundesländer.",
            Items = { new("Nordrhein-Westfalen", new() { "NRW" }), new("Bayern") },
        };

        var items = _provider.ItemsOf(ExerciseType.List, Json(config));

        Assert.Equal("Nenne alle Bundesländer.", items[0].Prompt);
        Assert.Equal(["Nordrhein-Westfalen", "NRW"], items[0].AcceptedAnswers);
        Assert.Equal(["Bayern"], items[1].AcceptedAnswers);
    }

    // ---- Rechnen (feste Aufgaben) ----

    [Fact]
    public void Arithmetic_AntwortAlsInvarianteDezimalzahl()
    {
        var config = new ArithmeticConfig { Problems = { new("7 × 6", 42m), new("10 ÷ 4", 2.5m) } };

        var items = _provider.ItemsOf(ExerciseType.Arithmetic, Json(config));

        Assert.Equal("7 × 6", items[0].Prompt);
        Assert.Equal("42", items[0].Answer);
        // Dezimaltrennzeichen bleibt kulturunabhängig ein Punkt (wie in ExerciseAnswerChecker).
        Assert.Equal("2.5", items[1].Answer);
    }

    // ---- Grammatik / Übersetzung / Fragen ----

    [Fact]
    public void Grammar_UebernimmtRegelHinweisAlsHint()
    {
        var config = new GrammarConfig { Tasks = { new GrammarTask("go (past)", "went", "unregelmäßig") } };

        var item = Assert.Single(_provider.ItemsOf(ExerciseType.Grammar, Json(config)));

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

        var item = Assert.Single(_provider.ItemsOf(ExerciseType.Translation, Json(config)));

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

        var items = _provider.ItemsOf(ExerciseType.Reading, Json(config));

        Assert.Equal(2, items.Count);
        Assert.Equal("Who?", items[0].Prompt);
        Assert.Equal("Tom", items[0].Answer);
    }

    // ---- Birkenbihl (reine Inhaltsübung) ----

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

        var item = Assert.Single(_provider.ItemsOf(ExerciseType.Birkenbihl, Json(config)));

        Assert.Equal("What is your name?", item.Prompt);
        Assert.Equal("Wie heißt du?", item.Answer);
    }

    // ---- Typen ohne feste Items ----

    [Theory]
    [InlineData(ExerciseType.Essay)]
    [InlineData(ExerciseType.ArithmeticDrill)]
    public void OhneFesteItems_LiefertLeereListe(ExerciseType type)
    {
        // Essay = freier Text; ArithmeticDrill = pro Abruf erzeugt – beide haben keine abzählbaren Inhalte.
        Assert.Empty(_provider.ItemsOf(type, "{}"));
    }

    // ---- Robustheit ----

    [Theory]
    [InlineData("")]
    [InlineData("{}")]
    public void LeereOderInhaltsloseConfig_LiefertLeereListe(string configJson)
    {
        Assert.Empty(_provider.ItemsOf(ExerciseType.Vocabulary, configJson));
    }
}
