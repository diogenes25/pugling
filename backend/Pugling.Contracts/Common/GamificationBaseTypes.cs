namespace Pugling.Contracts;

/// <summary>
/// Measurable quantity of a child's learning activity – shared basis for missions and awards.
/// All values are computed server-side from the existing tables (no client trust).
/// </summary>
public enum ProgressMetric
{
    /// <summary>Newly introduced content (PositionItemProgress.IntroducedAt).</summary>
    NewWords = 0,
    /// <summary>Correct Leitner repetitions (ReviewEvent.WasCorrect).</summary>
    CorrectReviews = 1,
    /// <summary>Passed final tests (TestAttempt.Passed).</summary>
    TestsPassed = 2,
    /// <summary>Minutes practiced (PracticeSession.ActiveSeconds).</summary>
    MinutesPracticed = 3,
    /// <summary>Fully completed days per the day rule of the <c>PositionProgressService</c>.</summary>
    DaysComplete = 4,
    /// <summary>Current streak of consecutive complete days (only meaningful for awards).</summary>
    StreakDays = 5,
}

/// <summary>Period over which a mission counts and renews.</summary>
public enum MissionPeriod
{
    /// <summary>Per calendar day (UTC); renews daily.</summary>
    Daily = 0,
    /// <summary>Per ISO week (Mon–Sun); renews weekly.</summary>
    Weekly = 1,
    /// <summary>One-off; completed once and then permanently done.</summary>
    OneOff = 2,
}

/// <summary>Unit of measurement of the item – determines how quantities are displayed in the inventory and upon activation.</summary>
public enum UnitType
{
    /// <summary>Units without a specific unit of measurement (piece count).</summary>
    Stueck = 0,
    /// <summary>Time unit minutes (e.g. "30 minutes of TV").</summary>
    Minute = 1,
    /// <summary>Time unit hours.</summary>
    Stunde = 2,
    /// <summary>Weight unit grams (e.g. candy).</summary>
    Gramm = 3,
    /// <summary>Generic count of times (e.g. "3 times having ice cream").</summary>
    Mal = 4,
}

/// <summary>Type of action the item represents – categorizes the item for the supervisor.</summary>
public enum ActionType
{
    /// <summary>Other / not categorized.</summary>
    Sonstiges = 0,
    /// <summary>TV / media consumption.</summary>
    TV = 1,
    /// <summary>Video gaming.</summary>
    Zocken = 2,
    /// <summary>Candy / snacks.</summary>
    Suessigkeit = 3,
    /// <summary>Outing / leisure activity.</summary>
    Ausflug = 4,
}

/// <summary>Automatic restock rule for a shop listing (<c>ShopListing</c>).</summary>
public enum ShopRefillKind
{
    /// <summary>No automatic restocking; stock is only changed by the supervisor.</summary>
    None = 0,
    /// <summary>Restock once at a fixed point in time.</summary>
    Once = 1,
    /// <summary>Restock once daily.</summary>
    Daily = 2,
    /// <summary>Restock twice daily.</summary>
    TwiceDaily = 3,
    /// <summary>Restock once weekly on a fixed weekday.</summary>
    Weekly = 4,
}

/// <summary>State of a historical shop purchase entry.</summary>
public enum ShopPurchaseStatus
{
    /// <summary>Purchase active – the acquired units are in the child's aggregated inventory (<c>ChildInventory</c>).</summary>
    Owned = 0,
    /// <summary>Purchase cancelled by the supervisor; currency refunded, inventory reduced accordingly.</summary>
    Cancelled = 1,
}

/// <summary>Status of a child's activation request.</summary>
public enum ActivationRequestStatus
{
    /// <summary>Request submitted – awaiting the supervisor's decision.</summary>
    Pending = 0,
    /// <summary>Approved by the supervisor – units taken from the inventory.</summary>
    Approved = 1,
    /// <summary>Rejected by the supervisor – units remain in the inventory.</summary>
    Rejected = 2,
}
