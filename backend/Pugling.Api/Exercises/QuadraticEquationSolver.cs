using System.Globalization;
using System.Text;

namespace Pugling.Api.Exercises;

/// <summary>
/// The arithmetic behind "Gleichung lösen" (B-710): the real solution set of <c>a·x² + b·x + c = 0</c>, the text a set is
/// written as, and the equation as a formula. Pure and stateless - the type class, the validation and the choice stage
/// all read the set from here, so the answer can never differ from the options or from what the author was told.
/// </summary>
public static class QuadraticEquationSolver
{
    /// <summary>Decimals every solution is rounded to: far below any tolerance a task names, far above double noise.</summary>
    public const int Precision = 6;

    /// <summary>
    /// The distinct real solutions, ascending, each rounded to <see cref="Precision"/> decimals. A double root counts once
    /// (B-710, decision 8); no real root gives the empty list. The discriminant is exact (decimal), so a double root is
    /// recognized as one instead of splitting into two neighbours. Requires <paramref name="a"/> ≠ 0.
    /// </summary>
    public static IReadOnlyList<decimal> Solve(decimal a, decimal b, decimal c)
    {
        if (a == 0) throw new ArgumentException("The coefficient a must not be zero.", nameof(a));
        var discriminant = b * b - 4 * a * c;
        if (discriminant < 0) return [];
        if (discriminant == 0) return [Round(-b / (2 * a))];
        // The stable form: the root with the larger magnitude from the sum, the other through Vieta (x1·x2 = c/a), so a
        // small root is not lost to cancellation of two nearly equal numbers.
        var root = Sqrt(discriminant);
        var q = -(b + (b < 0 ? -root : root)) / 2;
        var first = q / a;
        var second = q == 0 ? -first : c / q;
        return [.. new[] { Round(first), Round(second) }.Distinct().Order()];
    }

    /// <summary>
    /// Decimals the shown solution carries: those of the tolerance, so the shown text is itself within it (the rounding
    /// error of d decimals is at most half of the tolerance's last digit). A tolerance of 0 shows the full precision.
    /// </summary>
    public static int DisplayDecimals(decimal tolerance)
    {
        if (tolerance <= 0) return Precision;
        var scale = (tolerance / 1.000000000000000000000000000000000m).Scale;
        return Math.Min((int)scale, Precision);
    }

    /// <summary>
    /// A set as the child types it: ascending, separated by <see cref="NumericAnswer.SetSeparator"/>, decimal
    /// point, no trailing zeros; the empty set is <see cref="NumericAnswer.NoSolution"/>. The one writer of
    /// that text, so the solution, the reveal and every option of the choice stage read alike.
    /// </summary>
    public static string FormatSet(IEnumerable<decimal> values, decimal tolerance)
    {
        var decimals = DisplayDecimals(tolerance);
        var shown = values.Select(v => Math.Round(v, decimals, MidpointRounding.AwayFromZero)).Distinct().Order().ToList();
        return shown.Count == 0
            ? NumericAnswer.NoSolution
            : string.Join(NumericAnswer.SetSeparator, shown.Select(Plain));
    }

    /// <summary>
    /// The equation as a formula in LaTeX (<c>$x^2 - 6x + 8 = 0$</c>): coefficient 1 is left out, a minus becomes the
    /// operator, zero terms vanish, and a decimal comma is written <c>{,}</c> like in the course material. The author
    /// never types LaTeX.
    /// </summary>
    public static string Prompt(QuadraticEquation equation)
    {
        var text = new StringBuilder("$");
        Term(text, equation.A, "x^2", first: true);
        Term(text, equation.B, "x", first: false);
        Term(text, equation.C, "", first: false);
        return text.Append(" = 0$").ToString();
    }

    /// <summary>
    /// Wrong solution sets a child typically arrives at with the pq formula (B-711, decision 2), most telling first: the
    /// signs swapped, only one of two solutions, the formula computed with p instead of p/2; then fillers - the radicand
    /// with the wrong sign, its single elements, the vertex alone, one solution ±1 and the empty set. Unfiltered: a
    /// candidate may equal the right set or another candidate; the caller drops those by the grader's rule.
    /// </summary>
    public static IEnumerable<IReadOnlyList<decimal>> WrongSets(QuadraticEquation equation)
    {
        var right = Solve(equation.A, equation.B, equation.C);
        var p = equation.B / equation.A;
        var q = equation.C / equation.A;
        if (right.Count > 0) yield return [.. right.Select(x => -x)];
        if (right.Count == 2) yield return [right[1]];
        // x = -p ± √(p² - q): the roots of x² + 2p·x + q = 0.
        yield return Solve(1, 2 * p, q);
        if (right.Count == 2) yield return [right[0]];
        // x = -p/2 ± √(q - p²/4): the roots of x² + p·x + (p²/2 - q) = 0.
        var flipped = Solve(1, p, p * p / 2 - q);
        yield return flipped;
        foreach (var x in flipped) yield return [x];
        yield return [Round(-p / 2)];
        if (right.Count > 0)
        {
            yield return [right[0] + 1];
            yield return [right[0] - 1];
            yield return [];
        }
    }

    // One term with its operator: the leading one carries its own minus, every later one a spaced "+" or "-".
    private static void Term(StringBuilder text, decimal coefficient, string variable, bool first)
    {
        if (coefficient == 0) return;
        var magnitude = Math.Abs(coefficient);
        if (first) text.Append(coefficient < 0 ? "-" : "");
        else text.Append(coefficient < 0 ? " - " : " + ");
        // "x", not "1x" - but a constant 1 stays a 1.
        if (magnitude != 1 || variable.Length == 0) text.Append(Plain(magnitude).Replace(".", "{,}"));
        text.Append(variable);
    }

    // Invariant digits without trailing zeros; a rounded negative zero is plain zero.
    private static string Plain(decimal value) =>
        (value == 0 ? 0m : value).ToString("0.############################", CultureInfo.InvariantCulture);

    private static decimal Round(decimal value)
    {
        var rounded = Math.Round(value, Precision, MidpointRounding.AwayFromZero);
        return rounded == 0 ? 0m : rounded;
    }

    // Square root in decimal: a double estimate, refined by Newton steps until it settles.
    private static decimal Sqrt(decimal value)
    {
        var x = (decimal)Math.Sqrt((double)value);
        if (x == 0) return 0;
        for (var i = 0; i < 8; i++)
        {
            var next = (x + value / x) / 2;
            if (next == x) break;
            x = next;
        }
        return x;
    }
}
