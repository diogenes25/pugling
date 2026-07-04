using Pugling.Api.Models;

namespace Pugling.Api.Services;

/// <summary>Ein erzeugter Rechenausdruck samt Lösung.</summary>
public record GeneratedProblem(string Prompt, decimal Answer);

/// <summary>
/// Erzeugt zufällige Rechenaufgaben aus den Regeln einer <see cref="ArithmeticDrillConfig"/>.
/// Bewusst zustandslos: die Zufallsquelle wird übergeben, damit Aufrufe mit festem Seed
/// reproduzierbar – und damit testbar – sind.
/// </summary>
public class ArithmeticProblemGenerator
{
    /// <summary>Erzeugt <see cref="ArithmeticDrillConfig.ProblemCount"/> Aufgaben nach den Regeln der Konfiguration.</summary>
    /// <param name="config">Die geprüften Erzeugungsregeln.</param>
    /// <param name="random">Zufallsquelle; für reproduzierbare Sätze mit festem Seed anlegen.</param>
    public IReadOnlyList<GeneratedProblem> Generate(ArithmeticDrillConfig config, Random random)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(random);
        if (config.Operations.Count == 0)
            throw new ArgumentException("Mindestens eine Rechenart ist erforderlich.", nameof(config));
        if (config.MaxOperand < config.MinOperand)
            throw new ArgumentException("MaxOperand muss ≥ MinOperand sein.", nameof(config));

        var problems = new List<GeneratedProblem>(config.ProblemCount);
        for (int i = 0; i < config.ProblemCount; i++)
        {
            var operation = config.Operations[random.Next(config.Operations.Count)];
            problems.Add(Create(operation, config, random));
        }
        return problems;
    }

    private static GeneratedProblem Create(ArithmeticOperation operation, ArithmeticDrillConfig config, Random random) =>
        operation switch
        {
            ArithmeticOperation.Addition => Binary(config, random, "+", (a, b) => a + b),
            ArithmeticOperation.Multiplication => Binary(config, random, "×", (a, b) => a * b),
            ArithmeticOperation.Subtraction => Subtraction(config, random),
            ArithmeticOperation.Division => Division(config, random),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unbekannte Rechenart."),
        };

    /// <summary>Aufgabe mit zwei Operanden im konfigurierten Bereich und der gegebenen Verknüpfung.</summary>
    private static GeneratedProblem Binary(ArithmeticDrillConfig c, Random r, string symbol, Func<int, int, int> op)
    {
        int a = Operand(c, r), b = Operand(c, r);
        return new GeneratedProblem($"{a} {symbol} {b}", op(a, b));
    }

    private static GeneratedProblem Subtraction(ArithmeticDrillConfig c, Random r)
    {
        int a = Operand(c, r), b = Operand(c, r);
        if (!c.AllowNegativeResults && b > a)
            (a, b) = (b, a);   // größere Zahl nach vorn – so bleibt das Ergebnis ≥ 0
        return new GeneratedProblem($"{a} − {b}", a - b);
    }

    private static GeneratedProblem Division(ArithmeticDrillConfig c, Random r)
    {
        // Divisor immer ≥ 1, damit nie durch null geteilt wird.
        int divisor = r.Next(Math.Max(1, c.MinOperand), Math.Max(1, c.MaxOperand) + 1);

        if (c.DivisionMustBeWhole)
        {
            // Rückwärts konstruieren: Dividend = Divisor × Quotient garantiert ein glattes Ergebnis.
            int quotient = Operand(c, r);
            return new GeneratedProblem($"{divisor * quotient} ÷ {divisor}", quotient);
        }

        int dividend = Operand(c, r);
        return new GeneratedProblem($"{dividend} ÷ {divisor}", Math.Round((decimal)dividend / divisor, 2));
    }

    /// <summary>Ein zufälliger Operand im Bereich [MinOperand, MaxOperand] (beide inklusive).</summary>
    private static int Operand(ArithmeticDrillConfig c, Random r) => r.Next(c.MinOperand, c.MaxOperand + 1);
}
