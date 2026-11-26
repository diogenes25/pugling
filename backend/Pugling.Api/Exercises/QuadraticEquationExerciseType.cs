using System.Globalization;
using Pugling.Api.Models;

namespace Pugling.Api.Exercises;

/// <summary>
/// "Gleichung lösen" (B-710): one atom per quadratic equation; the answer is its whole solution set, typed as
/// <c>2;4</c> or <c>keine Lösung</c>. The set comes from <see cref="QuadraticEquationSolver"/>, never from the config, and
/// is graded by the shared <see cref="AnswerGrader"/> through <see cref="ContentItem.NumericSet"/> - so the catalog check,
/// the test mode and playing in a position cannot grade the same answer differently.
/// </summary>
public sealed class QuadraticEquationExerciseType : ExerciseTypeBase
{
    /// <summary>
    /// The card's hint while the child types into the plain text field (B-710, decision 9): the separator rule stands
    /// nowhere else on the card. It names the form of the answer, never a solution.
    /// </summary>
    public const string SeparatorHint = "Mehrere Lösungen mit ; trennen, ohne Lösung: keine Lösung";

    /// <summary>Largest magnitude a coefficient may have - keeps the exact decimal discriminant far from overflow.</summary>
    public const decimal MaxCoefficient = 1_000_000m;

    /// <summary>
    /// Smallest magnitude of <c>A</c>: the choice stage divides by it (p = B/A) and squares the result, so a tiny A would
    /// overflow the decimal range on every card instead of being refused once when saving.
    /// </summary>
    public const decimal MinLeadingCoefficient = 0.001m;

    /// <summary>Finest tolerance above zero: solutions are kept to <see cref="QuadraticEquationSolver.Precision"/> decimals.</summary>
    public const decimal MinTolerance = 0.000001m;

    /// <summary>Widest tolerance - beyond it the task asks for nothing.</summary>
    public const decimal MaxTolerance = 1000m;

    // Stateless; the catalog check grades through the same rule as the play paths.
    private static readonly AnswerGrader Grader = new();

    /// <inheritdoc/>
    public override string Key => ExerciseTypeKeys.QuadraticEquation;

    /// <inheritdoc/>
    public override ExerciseTypeManifest Manifest { get; } = new(
        ExerciseTypeKeys.QuadraticEquation, "Gleichung lösen", "quadratic-equation", 1, "quadratic-equation",
        ExerciseCheckMode.CatalogCheck, null, null, ["tolerance", "solutionSet"],
        ItemNoun: "Gleichungen", ItemNounOne: "Gleichung");

    /// <inheritdoc/>
    public override int DefaultStage => (int)QuadraticEquationStage.Direct;
    /// <inheritdoc/>
    public override int PreviewStage => (int)QuadraticEquationStage.Direct;

    /// <inheritdoc/>
    public override IReadOnlyList<StageOption> StageOptions { get; } =
    [
        new((int)QuadraticEquationStage.Distractors, "Lösungsmenge auswählen"),
        new((int)QuadraticEquationStage.Direct, "Lösungsmenge tippen"),
    ];

    /// <summary>
    /// The separator hint belongs to the typed card only: next to buttons that already read "2;4" it would describe an
    /// input the child does not make.
    /// </summary>
    public override ContentItem CardForStage(ContentItem item, int stage) =>
        (QuadraticEquationStage)stage == QuadraticEquationStage.Distractors && item.Hint == SeparatorHint
            ? item with { Hint = null }
            : item;

    /// <summary>Most wrong sets on the choice stage, next to the right one (four buttons at most).</summary>
    public const int MaxWrongChoices = 3;

    /// <summary>
    /// The choice stage (B-711): the right set and up to <see cref="MaxWrongChoices"/> typical wrong ones from
    /// <see cref="QuadraticEquationSolver.WrongSets"/>, each written by <see cref="QuadraticEquationSolver.FormatSet"/> -
    /// the text the child sends back, so a picked option meets the set atom exactly as typed. A candidate the grader
    /// would accept as the right set is no wrong option and drops out, as does a repeat. The order is fixed per equation
    /// (a hash of the option text, never <c>Random</c>), so a reload shows the same list; the card shuffles per session on
    /// top. The direct stage offers nothing.
    /// </summary>
    public override IReadOnlyList<string>? Choices(string configJson, IReadOnlyList<ContentItem> items, ContentItem item, int stage)
    {
        if ((QuadraticEquationStage)stage != QuadraticEquationStage.Distractors || item.NumericSet is null) return null;
        var equations = Deserialize<QuadraticEquationConfig>(configJson).Equations;
        if (item.Index < 0 || item.Index >= equations.Count) return null;
        var equation = equations[item.Index];
        var options = new List<string> { item.Answer };
        foreach (var set in QuadraticEquationSolver.WrongSets(equation))
        {
            if (options.Count > MaxWrongChoices) break;
            var text = QuadraticEquationSolver.FormatSet(set, equation.Tolerance);
            if (options.Contains(text) || Grader.Matches(text, item)) continue;
            options.Add(text);
        }
        // A choice with nothing to choose from would be a free point - the generator always finds one, this keeps it a rule.
        if (options.Count < 2) return null;
        return [.. options.OrderBy(o => StableHash($"{item.Answer}|{o}")).ThenBy(o => o, StringComparer.Ordinal)];
    }

    // Invariant digits for an English message, whatever the server's culture.
    private static string Plain(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    // FNV-1a over the text: the same on every run and machine, unlike string.GetHashCode.
    private static uint StableHash(string text)
    {
        var hash = 2166136261u;
        foreach (var c in text) hash = unchecked((hash ^ c) * 16777619u);
        return hash;
    }

    /// <inheritdoc/>
    public override IReadOnlyList<ContentItem> ItemsOf(string configJson)
    {
        var c = Deserialize<QuadraticEquationConfig>(configJson);
        return [.. c.Equations.Select((e, i) => ItemOf(i, e))];
    }

    /// <summary>The atom of one equation; an equation the validation would reject (a = 0) has no set, and no answer
    /// matches an empty accepted list.</summary>
    internal static ContentItem ItemOf(int index, QuadraticEquation equation)
    {
        if (equation.A == 0)
            return new ContentItem(index, QuadraticEquationSolver.Prompt(equation), "", []);
        var set = QuadraticEquationSolver.Solve(equation.A, equation.B, equation.C);
        var answer = QuadraticEquationSolver.FormatSet(set, equation.Tolerance);
        return new ContentItem(index, QuadraticEquationSolver.Prompt(equation), answer, [answer], Hint: SeparatorHint,
            NumericSet: new NumericSetTarget(set, equation.Tolerance));
    }

    /// <inheritdoc/>
    public override CheckResult Check(string configJson, IReadOnlyList<GivenAnswer> answers, int? seed)
    {
        var given = AnswerChecking.ByIndex(answers);
        var items = ItemsOf(configJson).Select(item =>
        {
            var value = AnswerChecking.Value(given, item.Index);
            return new ItemCheck(item.Index, item.Prompt, value, item.Answer, Grader.Matches(value, item));
        });
        return AnswerChecking.Aggregate(items);
    }

    /// <summary>
    /// Checks a config before it is saved; the error message or <c>null</c>. Rejects a = 0 (no quadratic equation), a
    /// negative tolerance, coefficients beyond <see cref="MaxCoefficient"/>, and a tolerance so wide that one number
    /// could hit both solutions (2 × tolerance ≥ their distance, B-710 decision 3).
    /// </summary>
    public static string? Validate(QuadraticEquationConfig config)
    {
        if (config.Equations.Count == 0) return "At least one equation is required.";
        for (var i = 0; i < config.Equations.Count; i++)
        {
            var e = config.Equations[i];
            var at = $"Equation {i + 1}";
            if (e.A == 0) return $"{at}: A must not be 0 - without x² it is not a quadratic equation.";
            if (Math.Abs(e.A) < MinLeadingCoefficient)
                return $"{at}: A must be at least {Plain(MinLeadingCoefficient)} in magnitude.";
            if (new[] { e.A, e.B, e.C }.Any(v => Math.Abs(v) > MaxCoefficient))
                return $"{at}: coefficients must lie between -{Plain(MaxCoefficient)} and {Plain(MaxCoefficient)}.";
            if (e.Tolerance < 0) return $"{at}: Tolerance must not be negative.";
            if (e.Tolerance > MaxTolerance) return $"{at}: Tolerance must not exceed {Plain(MaxTolerance)}.";
            if (e.Tolerance > 0 && e.Tolerance < MinTolerance)
                return $"{at}: Tolerance must be 0 or at least {Plain(MinTolerance)} - solutions are kept to six decimals.";
            var set = QuadraticEquationSolver.Solve(e.A, e.B, e.C);
            // Two real roots that the six-decimal rounding merged into one: the child could not name both.
            if (set.Count == 1 && e.B * e.B - 4 * e.A * e.C > 0)
                return $"{at}: the two solutions lie closer than six decimals apart.";
            if (set.Count == 2 && 2 * e.Tolerance >= set[1] - set[0])
                return $"{at}: the tolerance is too wide - one number could hit both solutions ({Plain(set[0])} and {Plain(set[1])}).";
        }
        return null;
    }
}
