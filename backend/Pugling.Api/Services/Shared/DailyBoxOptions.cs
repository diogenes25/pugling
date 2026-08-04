namespace Pugling.Api.Services.Shared;

/// <summary>
/// Daily reward box settings from the configuration (section <c>Gamification:DailyBox</c>) - the positive
/// counterpart to the fixed <see cref="Models.PlanPosition.PenaltyCoins"/> stick. Coins/gems are each
/// drawn uniformly from their own [Min,Max] range; a streak tier then multiplies both draws once the
/// child's current streak reaches its threshold (see B-105, decision 6 - fixed for the first cut,
/// deliberately not supervisor-configurable).
/// </summary>
public class DailyBoxOptions
{
    /// <summary>Configuration section.</summary>
    public const string SectionName = "Gamification:DailyBox";

    /// <summary>Lower bound (inclusive) of the coin draw.</summary>
    public int MinCoins { get; set; } = 10;
    /// <summary>Upper bound (inclusive) of the coin draw.</summary>
    public int MaxCoins { get; set; } = 30;
    /// <summary>Lower bound (inclusive) of the gem draw.</summary>
    public int MinGems { get; set; }
    /// <summary>Upper bound (inclusive) of the gem draw.</summary>
    public int MaxGems { get; set; } = 2;

    /// <summary>
    /// Streak-length tiers with their reward multiplier (e.g. 7 days → ×1.5, 30 days → ×2). The highest
    /// threshold at or below the current streak wins - tiers do not stack.
    /// </summary>
    public List<DailyBoxStreakTier> StreakTiers { get; set; } = [];
}

/// <summary>One streak-length tier: from <see cref="FromStreak"/> consecutive fully met days onward, the
/// daily box's coin/gem draw is multiplied by <see cref="Multiplier"/>.</summary>
public class DailyBoxStreakTier
{
    /// <summary>Consecutive-days threshold from which this tier applies.</summary>
    public int FromStreak { get; set; }
    /// <summary>Multiplier applied to both the coin and the gem draw.</summary>
    public double Multiplier { get; set; } = 1.0;
}
