namespace Pugling.Contracts;

/// <summary>Kinship role of a supervisor to the student (purely descriptive).</summary>
public enum SupervisorRelation
{
    /// <summary>Father. Here "father" really does mean father – the underlying domain line is an <c>Adult</c>.</summary>
    Father = 0,
    /// <summary>Mother.</summary>
    Mother = 1,
    /// <summary>Grandmother.</summary>
    Grandma = 2,
    /// <summary>Grandfather.</summary>
    Grandpa = 3,
    /// <summary>Legal guardian with no kinship (guardian, foster parents).</summary>
    Guardian = 4,
    /// <summary>Other supervising person – e.g. a teacher with a supervisory mandate.</summary>
    Other = 5,
}

/// <summary>Gender of the child (purely descriptive). Part of the exercise-independent profile; a later
/// study plan generator uses it at most for the language of address, never for filtering the material.</summary>
public enum Gender
{
    /// <summary>Not specified. Default – the UI then addresses the child in a gender-neutral way.</summary>
    None = 0,
    /// <summary>Male.</summary>
    Male = 1,
    /// <summary>Female.</summary>
    Female = 2,
    /// <summary>Diverse.</summary>
    Diverse = 3,
}

/// <summary>
/// Category of a points ledger entry – makes bonuses evaluable/cappable (e.g. "how many points
/// came from combo vs. time of day?"). <see cref="Base"/> is the default for legacy entries.
/// </summary>
public enum PointKind
{
    /// <summary>Base points for a correct repetition (incl. time-slot factor).</summary>
    Base = 0,
    /// <summary>Manual supervisor ledger entry (credit/redemption).</summary>
    Manual = 1,
    /// <summary>Combo bonus (hits in a row).</summary>
    Combo = 2,
    /// <summary>Bonus for a fast answer.</summary>
    Speed = 3,
    /// <summary>Reward for a completed mission (daily/weekly/extra goal).</summary>
    Mission = 4,
    /// <summary>Reward for an achieved award.</summary>
    Achievement = 5,
    /// <summary>Redemption of coins for a skin (negative entry).</summary>
    SkinPurchase = 6,
    /// <summary>Goal of a study plan position reached (daily/weekly goal of the exercise).</summary>
    Goal = 7,
    /// <summary>Redemption of coins for a family shop item (negative entry).</summary>
    ShopCoins = 8,
    /// <summary>Redemption of gems for a family shop item (negative entry).</summary>
    ShopGems = 9,
    /// <summary>Manual supervisor ledger entry in gems (gem counterpart to <see cref="Manual"/>; gift/correction).</summary>
    ManualGems = 10,
    /// <summary>Penalty because a mandatory goal of a study plan position was missed in the period (negative entry).</summary>
    GoalPenalty = 11,
    /// <summary>Reward for an achieved committed learning goal/objective or one of its milestones (coins).</summary>
    ObjectiveCoins = 12,
    /// <summary>Reward for an achieved stretch objective or one of its milestones (gems).</summary>
    ObjectiveGems = 13,
}
