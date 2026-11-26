using System.Text.Json;
using Pugling.Api.Exercises;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-711: the choice stage of "Gleichung lösen" - the right set among up to three typical wrong ones, written like the
/// typed answer, in a fixed order. Each case names the change that turns it red.
/// </summary>
public class QuadraticEquationChoicesTests
{
    private readonly AnswerGrader _grader = new();
    private readonly QuadraticEquationExerciseType _type = new();
    private const int Pick = (int)QuadraticEquationStage.Distractors;
    private const int Type = (int)QuadraticEquationStage.Direct;

    private static string Config(params QuadraticEquation[] equations) =>
        JsonSerializer.Serialize(new QuadraticEquationConfig { Equations = [.. equations] },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private IReadOnlyList<string>? Choices(QuadraticEquation equation, int stage)
    {
        var config = Config(equation);
        var items = _type.ItemsOf(config);
        return _type.Choices(config, items, items[0], stage);
    }

    // AK 1: the choice stage offers the right set once and up to three wrong ones, all different; the typed stage
    // offers nothing. Red when the right set is doubled, a wrong set repeats or the direct stage gets options.
    [Theory]
    [InlineData(1, -6, 8)]
    [InlineData(1, -6, 9)]
    [InlineData(1, 0, 1)]
    [InlineData(1, -4, 0)]
    [InlineData(3, -22, 35)]
    [InlineData(2, 0, 0)]
    public void Auswahl_Hat_Die_Richtige_Einmal_Und_Verschiedene_Falsche(double a, double b, double c)
    {
        var equation = new QuadraticEquation((decimal)a, (decimal)b, (decimal)c);
        var item = _type.ItemsOf(Config(equation))[0];
        var choices = Choices(equation, Pick)!;

        Assert.InRange(choices.Count, 2, 4);
        Assert.Equal(choices.Count, choices.Distinct().Count());
        Assert.Single(choices, o => _grader.Matches(o, item));
        Assert.Contains(item.Answer, choices);
        Assert.Null(Choices(equation, Type));
    }

    // AK 2: the same coefficients give the same list in the same order. Red when Random or a time value enters the order.
    [Fact]
    public void Reihenfolge_Ist_Fest()
    {
        var equation = new QuadraticEquation(1, -6, 8);
        Assert.Equal(Choices(equation, Pick), Choices(equation, Pick));
        Assert.Equal(Choices(equation, Pick), new QuadraticEquationExerciseType().Choices(Config(equation),
            _type.ItemsOf(Config(equation)), _type.ItemsOf(Config(equation))[0], Pick));
    }

    // AK 3, one case per model: x² - 6x + 8 = 0 offers the swapped signs, one solution alone (the larger one comes first:
    // a child who stops after the "+" root) and the p-instead-of-p/2 result (6 ± √28). Red when a model is struck from the
    // generator - for the single solution "4" is pinned, because the smaller one also fills in later as a filler.
    [Fact]
    public void Fehlermodelle_Bei_Zwei_Loesungen()
    {
        var choices = Choices(new QuadraticEquation(1, -6, 8), Pick)!;
        Assert.Contains("-4;-2", choices);
        Assert.Contains("4", choices);
        Assert.Contains("0.71;11.29", choices);
    }

    // AK 1 with a wide tolerance: for the double root 0.02 (tolerance 0.05) the swapped sign "-0.02" reads differently but
    // lies within the tolerance - it would be a second right button. Red when the generator drops repeats by text only
    // instead of by the grader's rule.
    [Fact]
    public void Kein_Zweiter_Richtiger_Knopf_In_Der_Toleranz()
    {
        var equation = new QuadraticEquation(1, -0.04m, 0.0004m, 0.05m);
        var item = _type.ItemsOf(Config(equation))[0];
        Assert.Equal("0.02", item.Answer);
        Assert.Single(Choices(equation, Pick)!, o => _grader.Matches(o, item));
    }

    // AK 3: without a solution the right option is "keine Lösung" and every wrong one is a set of numbers. Red when the
    // fillers (wrong radicand sign) are missing - only the right option would remain - or a filler is the empty set.
    [Fact]
    public void Ohne_Loesung_Sind_Die_Falschen_Mengen()
    {
        var choices = Choices(new QuadraticEquation(1, 0, 1), Pick)!;
        Assert.Contains("keine Lösung", choices);
        var wrong = choices.Where(o => o != "keine Lösung").ToList();
        Assert.NotEmpty(wrong);
        Assert.Contains("-1;1", wrong);
        Assert.All(wrong, o => Assert.True(o.Split(';').All(x => decimal.TryParse(x,
            System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out _)), o));
    }

    // Risk "Lösung 0 und Doppellösung": swapping the signs of {0} gives {0} again, of {3} gives {-3}. Red when the
    // generator does not drop a candidate equal to the right set (AK 1 would see the right set twice).
    [Fact]
    public void Doppelloesung_Und_Null()
    {
        var zero = Choices(new QuadraticEquation(2, 0, 0), Pick)!;
        Assert.Single(zero, o => o == "0");
        var doubled = Choices(new QuadraticEquation(1, -6, 9), Pick)!;
        Assert.Contains("-3", doubled);
        Assert.Contains("3", doubled);
    }

    // AK 4: a picked option is graded like the typed text - the right one right, every other wrong. Red when the options
    // are written another way than the set atom ("2.00", "{2, 4}", a comma).
    [Theory]
    [InlineData(1, -6, 8)]
    [InlineData(3, -22, 35)]
    [InlineData(1, 0, 1)]
    public void Gewaehlte_Option_Wird_Gewertet_Wie_Getippt(double a, double b, double c)
    {
        var equation = new QuadraticEquation((decimal)a, (decimal)b, (decimal)c, 0.05m);
        var item = _type.ItemsOf(Config(equation))[0];
        foreach (var option in Choices(equation, Pick)!)
            Assert.Equal(option == item.Answer, _grader.Matches(option, item));
    }

    // AK 6 (statement of intent): a position without a stage plays the typed stage. Red when the default flips to choices.
    [Fact]
    public void Vorgabe_Ist_Tippen()
    {
        Assert.Equal((int)QuadraticEquationStage.Direct, _type.DefaultStage);
        Assert.True(_type.IsTypedStage(Pick));
        Assert.True(_type.IsTypedStage(Type));
    }

    // The separator hint stays on the typed card and leaves the choice card. Red when the choice card keeps it or the
    // typed card loses it.
    [Fact]
    public void Hinweis_Nur_Auf_Der_Tippkarte()
    {
        var item = _type.ItemsOf(Config(new QuadraticEquation(1, -6, 8)))[0];
        Assert.Null(_type.CardForStage(item, Pick).Hint);
        Assert.Equal(QuadraticEquationExerciseType.SeparatorHint, _type.CardForStage(item, Type).Hint);
    }
}
