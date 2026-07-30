namespace Pugling.Api.Services.Shared;

// GeneratedProblem lebt im Vertrags-Projekt (Pugling.Contracts.Shared).

/// <summary>
/// Generates random arithmetic problems from the rules of an <see cref="ArithmeticDrillConfig"/>.
/// Deliberately stateless: the random source is passed in so that calls with a fixed seed are
/// reproducible – and therefore testable.
/// </summary>
public class ArithmeticProblemGenerator
{
    /// <summary>Generates <see cref="ArithmeticDrillConfig.ProblemCount"/> problems according to the configuration's rules.</summary>
    /// <param name="config">The validated generation rules.</param>
    /// <param name="random">Random source; create with a fixed seed for reproducible sets.</param>
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

    /// <summary>Problem with two operands within the configured range and the given operator.</summary>
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

    /// <summary>A random operand within the range [MinOperand, MaxOperand] (both inclusive).</summary>
    private static int Operand(ArithmeticDrillConfig c, Random r) => r.Next(c.MinOperand, c.MaxOperand + 1);
}
