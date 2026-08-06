namespace Pugling.Contracts;

/// <summary>
/// Kind of an <c>Objective</c> – determines the tone and currency of the reward. <see cref="Committed"/> is a
/// binding goal (reward in 🪙 coins = real-world privileges); <see cref="Stretch"/> is a
/// "stretch goal" (reward in 💎 gems = cosmetic). Deliberately, there is <b>no penalty</b> on objectives –
/// the "stick" lives solely on the mandatory goal of the <c>PlanPosition</c> (see <c>PenaltyCoins</c>).
/// </summary>
public enum ObjectiveKind
{
    /// <summary>Binding goal; reward in coins (Currency.Coins).</summary>
    Committed = 0,
    /// <summary>Stretch goal; reward in gems (Currency.Gems).</summary>
    Stretch = 1,
}

/// <summary>
/// Metric by which a <c>KeyResult</c> is measured. Deliberately only <b>trick-proof</b> quantities:
/// the outcome-/Leitner-based ones (<see cref="AvgMastery"/>/<see cref="MasteredPercent"/>/<see cref="MaxWeakItems"/>,
/// from <c>MasteryRollup</c>) and the class test grade entered by the supervisor
/// (<see cref="ClassTestGrade"/>). Pure activity counters (minutes/repetitions) are deliberately missing –
/// they reward repeating rather than proficiency and would be farmable. The coverage-based "Coverage" (value 1 of
/// the former <c>LearnGoalMetric</c>) is deliberately missing here: it already rises from merely seeing
/// vocabulary – it was the only thing the dropped learning-goal level added, and a farmable one at that.
/// </summary>
public enum KeyResultMetric
{
    /// <summary>Average mastery in percent across introduced items (goal: ≥ target value).</summary>
    AvgMastery = 0,
    /// <summary>Share of mastered items in percent (box ≥ MaxBox / existing items) (goal: ≥ target value).</summary>
    MasteredPercent = 2,
    /// <summary>Maximum number of weak items (mastery &lt; 50%) – "no more than N" (goal: ≤ target value).</summary>
    MaxWeakItems = 3,
    /// <summary>Best class test grade in the subject as grade×10 (e.g. 20 = "at least 2.0"; goal: ≤ target value).</summary>
    ClassTestGrade = 4,
}

/// <summary>
/// Live-computed status of an <c>Objective</c>/<c>KeyResult</c> – never a stored column
/// (<c>ObjectiveEvaluationService.StatusOf</c> computes it on every read).
/// </summary>
public enum GoalStatus
{
    /// <summary>Neither achieved nor overdue yet.</summary>
    Open = 0,
    /// <summary>The target value has been reached.</summary>
    Achieved = 1,
    /// <summary>Not reached, and the objective's end date has passed.</summary>
    Overdue = 2,
}

/// <summary>
/// Catalog level a <c>KeyResult</c>'s target is scoped to – never a stored column
/// (<c>ObjectiveService.KrScope</c> computes it on every read).
/// </summary>
public enum KeyResultScope
{
    /// <summary>Scoped to a single exercise.</summary>
    Exercise = 0,
    /// <summary>Scoped to a series unit (chapter).</summary>
    SeriesUnit = 1,
    /// <summary>Scoped to a whole subject.</summary>
    Subject = 2,
}
