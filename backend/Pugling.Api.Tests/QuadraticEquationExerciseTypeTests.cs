using System.Text.Json;
using Pugling.Api.Exercises;

namespace Pugling.Api.Tests;

/// <summary>
/// B-710: the type "Gleichung lösen" - solver, prompt, hint, validation and the set grading on both grading paths (the
/// catalog check and the atom the play paths grade through <see cref="AnswerGrader.Matches(string?, ContentItem)"/>).
/// Each case names the change that turns it red.
/// </summary>
public class QuadraticEquationExerciseTypeTests
{
    private readonly AnswerGrader _grader = new();
    private readonly QuadraticEquationExerciseType _type = new();

    private static string Config(params QuadraticEquation[] equations) =>
        JsonSerializer.Serialize(new QuadraticEquationConfig { Equations = [.. equations] },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private bool Played(string config, string given) => _grader.Matches(given, _type.ItemsOf(config)[0]);

    private bool Checked(string config, string given) =>
        _type.Check(config, [new GivenAnswer(0, given)], null).Items[0].Correct;

    // AK 1: the answer comes from the solver and the prompt from the coefficients. Red when ItemsOf takes another answer
    // or the prompt is built differently.
    [Fact]
    public void ItemsOf_Rechnet_Menge_Und_Prompt()
    {
        var item = _type.ItemsOf(Config(new QuadraticEquation(1, -6, 8)))[0];
        Assert.Equal("2;4", item.Answer);
        Assert.Equal("$x^2 - 6x + 8 = 0$", item.Prompt);
        Assert.Equal([2m, 4m], item.NumericSet!.Values);
    }

    // AK 1, risks "Koeffizienten im Prompt": one case per special form. Red when a coefficient 1/-1 is written ("1x"), a
    // zero term stays, the sign is doubled ("+ -6x") or the decimal comma is not braced.
    [Theory]
    [InlineData(-1, 1, 0, "$-x^2 + x = 0$")]
    [InlineData(2, 0, -8, "$2x^2 - 8 = 0$")]
    [InlineData(1, -1, 1, "$x^2 - x + 1 = 0$")]
    [InlineData(0.5, -1, -1.5, "$0{,}5x^2 - x - 1{,}5 = 0$")]
    [InlineData(1, 0, 0, "$x^2 = 0$")]
    public void Prompt_Setzt_Vorzeichen_Und_Komma(double a, double b, double c, string expected) =>
        Assert.Equal(expected, QuadraticEquationSolver.Prompt(new QuadraticEquation((decimal)a, (decimal)b, (decimal)c)));

    // Solver: two, one (double), none, a zero root, and a tiny root next to a huge one. Red when the double root splits,
    // a negative discriminant yields numbers, the order is not ascending or cancellation eats the small root.
    [Theory]
    [InlineData(1, -6, 8, "2;4")]
    [InlineData(1, -6, 9, "3")]
    [InlineData(1, 0, 1, "")]
    [InlineData(1, -4, 0, "0;4")]
    [InlineData(-1, 0, 4, "-2;2")]
    [InlineData(1, -1000000, 1, "0.000001;999999.999999")]
    public void Loeser_Liefert_Die_Menge(double a, double b, double c, string expected)
    {
        var set = QuadraticEquationSolver.Solve((decimal)a, (decimal)b, (decimal)c);
        Assert.Equal(expected, string.Join(';', set.Select(v => v.ToString(System.Globalization.CultureInfo.InvariantCulture))));
    }

    // AK 2: the set in any order is right, an incomplete, a padded, a doubled and a comma-separated one wrong - on both
    // paths. "4;2" is red when the given set is not sorted; "2" when only one element is compared; "2;4;5" when extra
    // elements are allowed; "2;2" when the second element is never compared; "2,4" when the comma separates.
    [Theory]
    [InlineData("2;4", true)]
    [InlineData("4;2", true)]
    [InlineData(" 2 ; 4 ", true)]
    [InlineData("2", false)]
    [InlineData("2;4;5", false)]
    [InlineData("2;2", false)]
    [InlineData("2,4", false)]
    [InlineData("2 und 4", false)]
    [InlineData("keine Lösung", false)]
    public void Menge_Wird_Als_Menge_Gewertet(string given, bool correct)
    {
        var config = Config(new QuadraticEquation(1, -6, 8));
        Assert.Equal(correct, Played(config, given));
        Assert.Equal(correct, Checked(config, given));
    }

    // AK 3: the empty set is the fixed text, never an empty input or a zero. Red when an empty answer counts as the empty
    // set or "0" as no solution.
    [Theory]
    [InlineData("keine Lösung", true)]
    [InlineData("Keine Lösung.", true)]
    [InlineData("", false)]
    [InlineData("0", false)]
    public void Leere_Menge_Ist_Keine_Loesung(string given, bool correct)
    {
        var config = Config(new QuadraticEquation(1, 0, 1));
        Assert.Equal("keine Lösung", _type.ItemsOf(config)[0].Answer);
        Assert.Equal(correct, Played(config, given));
        Assert.Equal(correct, Checked(config, given));
    }

    // AK 4: the tolerance holds for every element. 3x² - 22x + 35 = 0 has {7/3, 5}: the first element rides on the
    // tolerance. 3x² - 10x + 7 = 0 has {1, 7/3}: the second does - red when the tolerance only applies to the first.
    [Theory]
    [InlineData(3, -22, 35, "2.3;5", true)]
    [InlineData(3, -22, 35, "2,3;5", true)]
    [InlineData(3, -22, 35, "2.2;5", false)]
    [InlineData(3, -10, 7, "1;2.3", true)]
    [InlineData(3, -10, 7, "1;2.2", false)]
    public void Toleranz_Gilt_Je_Element(double a, double b, double c, string given, bool correct)
    {
        var config = Config(new QuadraticEquation((decimal)a, (decimal)b, (decimal)c, 0.05m));
        Assert.Equal(correct, Played(config, given));
        Assert.Equal(correct, Checked(config, given));
    }

    // The shown answer carries the tolerance's decimals and is itself right. Red when the reveal shows the full precision
    // or a rounding the grader rejects.
    [Fact]
    public void Gezeigte_Loesung_Ist_Gerundet_Und_Richtig()
    {
        var config = Config(new QuadraticEquation(3, -22, 35, 0.05m));
        var item = _type.ItemsOf(config)[0];
        Assert.Equal("2.33;5", item.Answer);
        Assert.True(_grader.Matches(item.Answer, item));
    }

    // AK 5: a double root is one element. Red when the set counts multiplicity ("3;3" right, "3" wrong).
    [Theory]
    [InlineData("3", true)]
    [InlineData("3;3", false)]
    [InlineData("keine Lösung", false)]
    public void Doppelloesung_Zaehlt_Einmal(string given, bool correct)
    {
        var config = Config(new QuadraticEquation(1, -6, 9));
        Assert.Equal(correct, Played(config, given));
        Assert.Equal(correct, Checked(config, given));
    }

    // AK 6: a = 0, a negative tolerance and a tolerance reaching both solutions are rejected; 0.99 for a distance of 2 is
    // not. Red when the bound is not inclusive (tolerance 1 would pass) or a case is missing. Review: a tiny A (the choice
    // stage would overflow on every card), a tolerance finer than the six kept decimals or wider than 1000, and two roots
    // merged by the rounding (1 ± 0.0000003) are refused - red when the matching bound is removed.
    [Theory]
    [InlineData(0, -6, 8, 0.01, false)]
    [InlineData(1, -6, 8, -0.01, false)]
    [InlineData(1, -6, 8, 1, false)]
    [InlineData(1, -6, 8, 0.99, true)]
    [InlineData(1, -6, 9, 5, true)]
    [InlineData(2000000, 0, 1, 0.01, false)]
    [InlineData(0.000000001, 1000000, 0, 0.01, false)]
    [InlineData(0.001, 1000000, 0, 0.01, true)]
    [InlineData(1, -6, 8, 0.0000001, false)]
    [InlineData(1, -6, 9, 1001, false)]
    [InlineData(1, -2, 0.9999999999999, 0, false)]
    public void Validierung(double a, double b, double c, double tolerance, bool valid)
    {
        var config = new QuadraticEquationConfig { Equations = [new((decimal)a, (decimal)b, (decimal)c, (decimal)tolerance)] };
        Assert.Equal(valid, QuadraticEquationExerciseType.Validate(config) is null);
    }

    // AK 6: a = 0 is named as such - the bound on a tiny A would refuse it too, but with a message about magnitude the
    // author would not understand. Red when the a = 0 check is dropped.
    [Fact]
    public void A_Null_Wird_Benannt() =>
        Assert.Contains("A must not be 0", QuadraticEquationExerciseType.Validate(
            new QuadraticEquationConfig { Equations = [new(0, -6, 8)] }));

    // AK 7: the card carries the separator hint, and it holds no solution. Red when ItemsOf sets no hint or the hint
    // carries the set.
    [Fact]
    public void Hinweis_Nennt_Die_Regel_Nicht_Die_Loesung()
    {
        var item = _type.ItemsOf(Config(new QuadraticEquation(1, -6, 8)))[0];
        Assert.Equal(QuadraticEquationExerciseType.SeparatorHint, item.Hint);
        Assert.DoesNotContain("2", item.Hint);
        Assert.DoesNotContain("4", item.Hint);
    }

    // Risk "IsMisspelled": a wrong set is no spelling slip and no accent slip. Red when a set atom goes through the letter
    // distance ("2;5" is one character off "2;4").
    [Fact]
    public void Falsche_Menge_Ist_Kein_Tippfehler()
    {
        var items = _type.ItemsOf(Config(new QuadraticEquation(1, -6, 8)));
        Assert.False(_grader.IsMisspelled("2;5", items[0], items));
        Assert.False(_grader.IsAccentSlip("keine Losung", _type.ItemsOf(Config(new QuadraticEquation(1, 0, 1)))[0]));
    }

    // Text atoms stay text: "2;4" as a list answer is compared as text. Red when the set rule reaches atoms without a set.
    [Fact]
    public void Text_Atom_Bleibt_Textvergleich()
    {
        var text = new ContentItem(0, "Zahlen", "2;4", ["2;4"]);
        Assert.False(_grader.Matches("4;2", text));
        Assert.True(_grader.Matches("2;4", text));
    }
}
