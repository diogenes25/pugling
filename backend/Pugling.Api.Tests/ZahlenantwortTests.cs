using System.Text.Json;
using Pugling.Api.Exercises;

namespace Pugling.Api.Tests;

/// <summary>
/// B-639: a number answer is compared as a number on both grading paths - the catalog check (<c>Check</c>, the father's
/// test mode) and playing in a position (<see cref="AnswerGrader.Matches(string?, ContentItem)"/> on the atom from
/// <c>ItemsOf</c>). Point and comma are both decimal separators, there is no thousands separator, and the task's
/// tolerance applies on both paths. Each case names the change that turns it red.
/// </summary>
public class ZahlenantwortTests
{
    private readonly AnswerGrader _grader = new();
    private readonly ArithmeticExerciseType _type = new();

    private static string Config(decimal answer, decimal tolerance = 0m) =>
        JsonSerializer.Serialize(new ArithmeticConfig { Problems = [new ArithmeticProblem("Aufgabe", answer, tolerance)] },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private bool Played(string config, string given) => _grader.Matches(given, _type.ItemsOf(config)[0]);

    private bool Checked(string config, string given) =>
        _type.Check(config, [new GivenAnswer(0, given)], null).Items[0].Correct;

    // AK 1, 2, 3, 5 and the minus sign: the same number in each spelling counts on both paths. Red on the play path while
    // it compares text ("2,5" against "2.5"), red on the check path while the comma is a thousands separator ("2,5" reads
    // as 25 - the red probe of AK 2). "2.5", "1.234" and "3" given as stored are statements of intent: they held on both
    // paths before the fix. A trailing full stop is forgiven as the text comparison did (Normalize).
    [Theory]
    [InlineData("2.5", "2,5")]
    [InlineData("2.5", "2.5")]
    [InlineData("2.5", "2.50")]
    [InlineData("2.5", "2,50")]
    [InlineData("2.5", " 2,5 ")]
    [InlineData("1.234", "1.234")]
    [InlineData("1.234", "1,234")]
    [InlineData("3", "+3")]
    [InlineData("3", "3")]
    [InlineData("-0.4", "-0,4")]
    [InlineData("-0.4", "−0,4")]
    [InlineData("0.6", ",6")]
    [InlineData("2.5", "2,5.")]
    public void Dieselbe_Zahl_Zaehlt_Auf_Beiden_Pfaden(string answer, string given)
    {
        var config = Config(decimal.Parse(answer, System.Globalization.CultureInfo.InvariantCulture));
        Assert.True(Played(config, given), $"play path: {given}");
        Assert.True(Checked(config, given), $"check path: {given}");
    }

    // AK 2, 3, 5: another number or no number is wrong on both paths. Statement of intent and leniency guard: every case
    // was already false before the fix (the red probe of AK 2 is "2,5" in the theory above). They turn red if the parser
    // grows too lenient - stripping separators or inner spaces would accept "1.2.3", "3,0,0" or "2 5".
    [Theory]
    [InlineData("2.5", "25")]
    [InlineData("1.234", "1234")]
    [InlineData("3", "")]
    [InlineData("3", "drei")]
    [InlineData("3", "1.2.3")]
    [InlineData("3", "3,0,0")]
    [InlineData("2.5", "2 5")]
    public void Andere_Oder_Keine_Zahl_Ist_Falsch(string answer, string given)
    {
        var config = Config(decimal.Parse(answer, System.Globalization.CultureInfo.InvariantCulture));
        Assert.False(Played(config, given), $"play path: {given}");
        Assert.False(Checked(config, given), $"check path: {given}");
    }

    // AK 4: the tolerance holds when playing, not only in the check. Red on the play path while the tolerance is ignored
    // there ("2,3" would be wrong); "2,2" is red when the tolerance is applied too wide or the bound is not inclusive.
    [Theory]
    [InlineData("2,3", true)]
    [InlineData("2,38", true)]
    [InlineData("2,28", true)]
    [InlineData("2,2", false)]
    [InlineData("2,39", false)]
    public void Toleranz_Gilt_Beim_Spielen(string given, bool correct)
    {
        var config = Config(2.33m, 0.05m);
        Assert.Equal(correct, Played(config, given));
        Assert.Equal(correct, Checked(config, given));
    }

    // AK 6: the drill grades a rounded result with comma as with point. "3,33" is red while the comma is a thousands
    // separator; "3.33" held before and is the statement of intent.
    [Theory]
    [InlineData("3,33")]
    [InlineData("3.33")]
    public void Drill_Wertet_Komma_Wie_Punkt(string given)
    {
        var result = AnswerChecking.CheckGenerated([new GeneratedProblem("10 : 3", 3.333m)], [new GivenAnswer(0, given)]);
        Assert.True(result.Items[0].Correct);
    }

    // A wrong number is no spelling slip: "2,4" for 2.5 is one character off, but it is another number. Red when a number
    // atom goes through the letter distance.
    [Fact]
    public void Falsche_Zahl_Ist_Kein_Tippfehler()
    {
        var items = _type.ItemsOf(Config(2.5m));
        Assert.False(_grader.IsMisspelled("2.4", items[0], items));
    }

    // A swapped card asks its prompt, not the number: the copy must not keep the number target, or it would be graded
    // against the old number. Red when Swap keeps Numeric.
    [Fact]
    public void Vertauschte_Karte_Ist_Kein_Zahlen_Atom() =>
        Assert.Null(ExerciseContentProvider.Swap(_type.ItemsOf(Config(2.5m))[0]).Numeric);

    // AK 7: text types compare unchanged - "2,5" as a vocabulary answer does not become a number. Red when the number rule
    // is applied to every atom instead of only to number atoms.
    [Fact]
    public void Text_Atom_Bleibt_Textvergleich()
    {
        var text = new ContentItem(0, "zwei Komma fünf", "2.5", ["2.5"]);
        Assert.False(_grader.Matches("2,5", text));
        Assert.True(_grader.Matches("2.5", text));
    }
}
