namespace Pugling.Api.Services.Shared;

/// <summary>
/// The one conversion from a share (0 to 100 percent) into a whole school grade 1 to 6 (B-740). Every caller that
/// turns a share into a grade goes through here with the table from <see cref="GradingOptions"/>; a second
/// conversion elsewhere would let the subject level and a mock exam grade the same performance differently.
/// <para>
/// Pure and table-driven, so tests pass their own table and the configuration stays the only source of the numbers.
/// Shares are compared as <see cref="decimal"/>, so a count exactly on a bound - also a fractional one such as 87.5 -
/// lands in the better grade without binary rounding deciding it.
/// </para>
/// </summary>
public static class GradeKey
{
    /// <summary>The best grade.</summary>
    public const int BestGrade = 1;

    /// <summary>The worst grade; every share below the last bound of the table.</summary>
    public const int WorstGrade = 6;

    /// <summary>
    /// Converts a share in percent into a grade. A share on a bound earns the better grade; shares outside 0 to 100
    /// are clamped to the edge, so nonsense input still yields a grade between 1 and 6.
    /// </summary>
    public static int PercentToGrade(decimal percent, IReadOnlyList<decimal> thresholds)
    {
        Validate(thresholds);
        var clamped = Math.Clamp(percent, 0m, 100m);
        for (var i = 0; i < thresholds.Count; i++)
            if (clamped >= thresholds[i]) return BestGrade + i;
        return WorstGrade;
    }

    /// <summary>
    /// Grade of <paramref name="part"/> out of <paramref name="total"/> (for instance mastered words of the material).
    /// An empty total has no share and therefore no grade - callers decide what "no measure" means.
    /// </summary>
    public static int ShareToGrade(int part, int total, IReadOnlyList<decimal> thresholds)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(total);
        return PercentToGrade(part * 100m / total, thresholds);
    }

    /// <summary>
    /// The inverse for one grade: the smallest part out of <paramref name="total"/> that earns <paramref name="grade"/>
    /// or better. Used to tell a child how many more words lift it to the next level (B-742), so that the promise and
    /// <see cref="ShareToGrade"/> can never disagree at a bound.
    /// </summary>
    public static int MinPartForGrade(int grade, int total, IReadOnlyList<decimal> thresholds)
    {
        Validate(thresholds);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(total);
        ArgumentOutOfRangeException.ThrowIfLessThan(grade, BestGrade);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(grade, WorstGrade);
        if (grade == WorstGrade) return 0;
        return (int)Math.Ceiling(thresholds[grade - 1] * total / 100m);
    }

    // A broken table must fail loudly: a silently shorter table would hand out grades from the wrong bounds.
    private static void Validate(IReadOnlyList<decimal> thresholds)
    {
        if (thresholds.Count != WorstGrade - BestGrade)
            throw new InvalidOperationException(
                $"Der Notenschlüssel (Grading:Thresholds) braucht genau {WorstGrade - BestGrade} Grenzen, hat aber {thresholds.Count}.");
        for (var i = 0; i < thresholds.Count; i++)
        {
            if (thresholds[i] is <= 0m or > 100m)
                throw new InvalidOperationException($"Grenze {thresholds[i]} im Notenschlüssel liegt nicht zwischen 0 und 100.");
            if (i > 0 && thresholds[i] >= thresholds[i - 1])
                throw new InvalidOperationException("Die Grenzen des Notenschlüssels müssen streng fallen (Note 1 zuerst).");
        }
    }
}
