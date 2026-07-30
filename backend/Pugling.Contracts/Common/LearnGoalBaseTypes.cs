namespace Pugling.Contracts;

/// <summary>
/// Metric by which a <c>LearnGoal</c> is measured – each one maps directly to a field of the
/// aggregated learning progress (see <c>MasteryRollup</c>).
/// </summary>
public enum LearnGoalMetric
{
    /// <summary>Average mastery in percent across introduced items (goal: ≥ target value).</summary>
    AvgMastery = 0,
    /// <summary>Coverage in percent: introduced / existing items (goal: ≥ target value).</summary>
    Coverage = 1,
    /// <summary>Share of mastered items in percent: box ≥ MaxBox / existing items (goal: ≥ target value).</summary>
    MasteredPercent = 2,
    /// <summary>Maximum number of weak items (mastery &lt; 50%) – "no more than N" (goal: ≤ target value).</summary>
    MaxWeakItems = 3,
}
