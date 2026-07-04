using System.Globalization;
using Pugling.Api.Models;
using Pugling.Api.Services;

namespace Pugling.Api.Tests;

/// <summary>Unit-Tests der Zufalls-Aufgaben-Erzeugung: Reproduzierbarkeit, Grenzen, Selbstkonsistenz.</summary>
public class ArithmeticProblemGeneratorTests
{
    private readonly ArithmeticProblemGenerator _gen = new();

    private static ArithmeticDrillConfig Config(
        int count = 20, int min = 1, int max = 12, bool allowNeg = false, bool wholeDiv = true,
        params ArithmeticOperation[] ops) => new()
    {
        Operations = ops.Length > 0 ? [.. ops] : [ArithmeticOperation.Addition, ArithmeticOperation.Subtraction,
            ArithmeticOperation.Multiplication, ArithmeticOperation.Division],
        MinOperand = min,
        MaxOperand = max,
        ProblemCount = count,
        AllowNegativeResults = allowNeg,
        DivisionMustBeWhole = wholeDiv,
    };

    [Fact]
    public void GleicherSeed_LiefertIdentischenSatz()
    {
        var config = Config();

        var a = _gen.Generate(config, new Random(123));
        var b = _gen.Generate(config, new Random(123));

        // Kern-Vertrag: derselbe Seed erzeugt exakt denselben Satz (sonst wäre serverseitiges Auswerten unmöglich).
        Assert.Equal(a.Select(p => $"{p.Prompt}={p.Answer}"), b.Select(p => $"{p.Prompt}={p.Answer}"));
    }

    [Fact]
    public void ProblemCount_WirdEingehalten()
    {
        Assert.Equal(15, _gen.Generate(Config(count: 15), new Random(1)).Count);
    }

    [Fact]
    public void ErzeugteAntworten_WerdenVomChecker_AlsRichtigAkzeptiert()
    {
        // Selbstkonsistenz: die vom Generator gelieferte Lösung muss der Checker als korrekt werten.
        var problems = _gen.Generate(Config(count: 40), new Random(555));
        var answers = problems
            .Select((p, i) => new GivenAnswer(i, p.Answer.ToString(CultureInfo.InvariantCulture)))
            .ToList();

        var result = new ExerciseAnswerChecker().CheckGenerated(problems, answers);

        Assert.Equal(100, result.ScorePercent);
    }

    [Fact]
    public void Subtraktion_OhneNegative_LiefertNieNegativesErgebnis()
    {
        var problems = _gen.Generate(Config(count: 100, ops: ArithmeticOperation.Subtraction), new Random(7));

        Assert.All(problems, p => Assert.True(p.Answer >= 0, $"negativ: {p.Prompt} = {p.Answer}"));
    }

    [Fact]
    public void Division_MussAufgehen_LiefertGanzzahligesErgebnis()
    {
        var problems = _gen.Generate(Config(count: 100, ops: ArithmeticOperation.Division), new Random(9));

        Assert.All(problems, p => Assert.Equal(Math.Truncate(p.Answer), p.Answer));
    }

    [Fact]
    public void Addition_OperandenLiegenImBereich()
    {
        var problems = _gen.Generate(Config(count: 100, min: 3, max: 8, ops: ArithmeticOperation.Addition), new Random(11));

        foreach (var p in problems)
        {
            var parts = p.Prompt.Split(" + ");
            Assert.Equal(2, parts.Length);
            foreach (var part in parts)
            {
                var n = int.Parse(part, CultureInfo.InvariantCulture);
                Assert.InRange(n, 3, 8);
            }
        }
    }

    [Fact]
    public void OhneRechenart_WirftArgumentException()
    {
        var config = Config();
        config.Operations = [];

        Assert.Throws<ArgumentException>(() => _gen.Generate(config, new Random(1)));
    }

    [Fact]
    public void MaxKleinerMin_WirftArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _gen.Generate(Config(min: 10, max: 5), new Random(1)));
    }

    [Fact]
    public void NullArgumente_WerfenArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _gen.Generate(null!, new Random(1)));
        Assert.Throws<ArgumentNullException>(() => _gen.Generate(Config(), null!));
    }
}
