using System.Text.Json;
using Pugling.Api.Exercises;

namespace Pugling.Api.Tests;

/// <summary>Unit-Tests der Inhalts-Extraktion aus der Übungs-Config (zustandslos, ohne DB/HTTP).</summary>
public class ExerciseContentProviderTests
{
    // Registry aus den eingebauten Typen – der Provider delegiert die Projektion an sie.
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

    // ---- Vokabeln ----

    /// <summary>
    /// Der zustandslose Provider liefert für Vokabeln <b>bewusst nichts</b>: deren Inhalte liegen in der
    /// <c>ExerciseItem</c>-Tabelle, und <c>VocabularyConfig.Items</c>/<c>.Refs</c> sind reine
    /// <b>Eingabeform</b> (nach dem Anlegen leert <c>AfterSaveAsync</c> sie).
    /// <para>
    /// Vorher stand hier die Gegenprobe – dieser Test prüfte die Config-Projektion, also den <i>zweiten</i>
    /// Inhaltsweg desselben Typs. Zwei Wege heißen zwei Wahrheiten: der eine trägt stabile ItemIds und damit
    /// den plan-übergreifenden Lernstand, der andere nicht. Erreichbar war er längst nicht mehr; jetzt ist er
    /// weg, und dieser Test hält ihn zu.
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

        var items = _provider.ItemsOf(ExerciseTypeKeys.Cloze, Json(config));

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

        var items = _provider.ItemsOf(ExerciseTypeKeys.Matching, Json(config));

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

        var items = _provider.ItemsOf(ExerciseTypeKeys.List, Json(config));

        Assert.Equal("Nenne alle Bundesländer.", items[0].Prompt);
        Assert.Equal(["Nordrhein-Westfalen", "NRW"], items[0].AcceptedAnswers);
        Assert.Equal(["Bayern"], items[1].AcceptedAnswers);
    }

    // ---- Rechnen (feste Aufgaben) ----

    [Fact]
    public void Arithmetic_AntwortAlsInvarianteDezimalzahl()
    {
        var config = new ArithmeticConfig { Problems = { new("7 × 6", 42m), new("10 ÷ 4", 2.5m) } };

        var items = _provider.ItemsOf(ExerciseTypeKeys.Arithmetic, Json(config));

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

        var item = Assert.Single(_provider.ItemsOf(ExerciseTypeKeys.Birkenbihl, Json(config)));

        Assert.Equal("What is your name?", item.Prompt);
        Assert.Equal("Wie heißt du?", item.Answer);
    }

    // ---- Typen ohne feste Items ----

    [Theory]
    [InlineData(ExerciseTypeKeys.Essay)]
    [InlineData(ExerciseTypeKeys.ArithmeticDrill)]
    public void OhneFesteItems_LiefertLeereListe(string type)
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
        Assert.Empty(_provider.ItemsOf(ExerciseTypeKeys.Vocabulary, configJson));
    }
}
