namespace Pugling.Api.Models;

// The motivation tier above the individual bonuses: missions (time-bound, repeatable goals) and awards
// (permanent milestones). Both measure the same progress metrics over a child's activity (see
// Services.MetricsService) and pay out through ChildPointsEntry.

// ProgressMetric/MissionPeriod/UnitType/ActionType/ShopRefillKind/ShopPurchaseStatus/
// ActivationRequestStatus live in the contract project (Pugling.Contracts).

/// <summary>
/// A goal defined by the supervisor for a child (daily/weekly/one-off goal). If the child reaches the
/// <see cref="Target"/> mark of the <see cref="Metric"/> within the respective period, it yields
/// <see cref="RewardPoints"/> once. Sensible templates are seeded but can be edited/deleted freely.
/// </summary>
public class Mission
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    public string Title { get; set; } = "";
    public ProgressMetric Metric { get; set; }
    /// <summary>Value of the metric to be reached within the period.</summary>
    public int Target { get; set; }
    public MissionPeriod Period { get; set; }
    /// <summary>Reward on completion (once per period).</summary>
    public int RewardPoints { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Records the one-off reward of a mission per period (idempotent, guards against double awarding).
/// <para>
/// The period is <b>(<see cref="Period"/>, <see cref="PeriodStart"/>)</b>. As with
/// <see cref="PositionGoalReward"/>, the kind is a <b>snapshot</b> of the mission: without it a switch
/// from daily to weekly would count the reward for a Monday as the reward of the week starting on it.
/// For <see cref="MissionPeriod.OneOff"/> there is no period – then <see cref="PeriodStart"/> is
/// <c>null</c>, and exactly that NULL is the discriminator of the two filtered unique indexes
/// (SQLite treats NULLs as distinct; a single unique index over a nullable column would <b>not</b> hold
/// the invariant – which is what made the former text key attractive).
/// </para>
/// </summary>
public class MissionAward
{
    public int Id { get; set; }
    public int MissionId { get; set; }
    public Mission? Mission { get; set; }
    /// <summary>Period kind at the time of the ledger entry (snapshot – see the class documentation).</summary>
    public MissionPeriod Period { get; set; }
    /// <summary>First day of the period (the day, or the week's Monday); <c>null</c> for <see cref="MissionPeriod.OneOff"/>.</summary>
    public DateOnly? PeriodStart { get; set; }
    public int Points { get; set; }
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// An award (badge) defined by the supervisor for a child: granted once from <see cref="Threshold"/> of
/// the <see cref="Metric"/> onwards (counted lifelong, or as the current streak), with an emoji icon and
/// an optional points reward. Duolingo-style milestones, freely configurable.
/// </summary>
public class Achievement
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    public string Title { get; set; } = "";
    /// <summary>Emoji or similar for the badge display (e.g. "🔥").</summary>
    public string? Icon { get; set; }
    public ProgressMetric Metric { get; set; }
    /// <summary>Threshold from which the award is reached.</summary>
    public int Threshold { get; set; }
    /// <summary>Optional points reward on reaching it (0 = badge only).</summary>
    public int RewardPoints { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Records when a child reached an award (exactly once, idempotent).</summary>
public class AchievementAward
{
    public int Id { get; set; }
    public int AchievementId { get; set; }
    public Achievement? Achievement { get; set; }
    public int Points { get; set; }
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Records the one-off daily reward box for a fully met day (all of the child's mandatory plan-position
/// goals reached) - the positive counterpart to <see cref="PositionGoalPenalty"/>. A unique index on
/// <c>(ChildId, Day)</c> guarantees the lazy evaluation at the practice/test-completion seams never grants
/// a second box on the same calendar day, exactly as <see cref="PositionGoalReward"/> does per position.
/// <see cref="StreakAtClaim"/> is a snapshot of the consecutive-fully-met-days streak at award time, so the
/// history stays traceable even as the (recomputed) live streak moves on. Deliberately coins/gems only -
/// no skin-drop rarity concept exists yet (see B-105).
/// </summary>
public class DailyBoxClaim
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    public DateOnly Day { get; set; }
    public int CoinsAwarded { get; set; }
    public int GemsAwarded { get; set; }
    /// <summary>Consecutive fully-met days up to and including <see cref="Day"/> (streak snapshot).</summary>
    public int StreakAtClaim { get; set; }
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
}

// Note: the former "offer" system (Reward/RewardRedemption/OfferPeriod) was removed - the family shop
// (ShopArticle/ShopListing/ShopPurchase/ActivationRequest) is the only way coins are spent.
// The ledger kind PointKind.Reward remains only as a tombstone for historical entries.

/// <summary>
/// The supervisor's base catalog article. It defines the <em>kind</em> of article (e.g. "watching TV" with
/// <see cref="UnitType"/> minute and <see cref="ActionType"/> TV). Price and stock live in
/// <see cref="ShopListing"/>s – one article can have several listings on different terms.
/// Article numbers are unique within the family.
/// </summary>
public class ShopArticle
{
    public int Id { get; set; }
    public int AdultId { get; set; }
    public Adult? Adult { get; set; }
    /// <summary>Family-internal article number/SKU, unique per adult.</summary>
    public string ArticleNumber { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    /// <summary>Unit of measure of the quantity (e.g. <see cref="UnitType.Minute"/> for TV time).</summary>
    public UnitType UnitType { get; set; }
    /// <summary>Action type (e.g. <see cref="ActionType.TV"/>); categorizes the article for the supervisor view.</summary>
    public ActionType ActionType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ShopListing> Listings { get; set; } = [];
}

/// <summary>
/// A purchasable listing for a <see cref="ShopArticle"/>. One article can have several listings with
/// different prices and quantities (e.g. "10 min TV for 50 coins" and "60 min TV for 250 coins").
/// <see cref="UnitsPerPurchase"/> states how many units (in the article's <see cref="UnitType"/>) one
/// purchase puts into the child's <see cref="ChildInventory"/>.
/// </summary>
public class ShopListing
{
    public int Id { get; set; }
    public int ShopArticleId { get; set; }
    public ShopArticle? ShopArticle { get; set; }
    /// <summary>Optional display title; if empty, the title of the owning article is used.</summary>
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    /// <summary>Price share in coins; may be 0 if gems are set.</summary>
    public int CoinPrice { get; set; }
    /// <summary>Price share in gems; may be 0 if coins are set.</summary>
    public int GemPrice { get; set; }
    /// <summary>Quantity (in the article's <see cref="UnitType"/>) per purchase, e.g. 30 for "30 minutes".</summary>
    public int UnitsPerPurchase { get; set; } = 1;
    public bool Active { get; set; } = true;
    /// <summary>Stock currently available for purchase.</summary>
    public int CurrentStock { get; set; }
    /// <summary>Target stock that automatic refills raise the stock to.</summary>
    public int MaxStock { get; set; }
    public ShopRefillKind RefillKind { get; set; } = ShopRefillKind.None;
    /// <summary>Optional one-off refill instant (UTC) for <see cref="ShopRefillKind.Once"/>.</summary>
    public DateTime? RefillAtUtc { get; set; }
    /// <summary>Optional weekday for <see cref="ShopRefillKind.Weekly"/>.</summary>
    public DayOfWeek? RefillDayOfWeek { get; set; }
    /// <summary>Last automatic refill applied; makes refilling idempotent.</summary>
    public DateTime? LastRefilledAtUtc { get; set; }
    /// <summary>Concurrency stamp for stock/refill: parallel purchases must not overdraw the stock.</summary>
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// A child's aggregated inventory for one <see cref="ShopArticle"/>. Several purchases of the same article
/// (through different <see cref="ShopListing"/>s or at different times) add up here. The child can raise
/// activation requests against this stock.
/// <para>
/// <b>Paid units are money and therefore survive catalog maintenance.</b> That is why the article
/// reference is optional (FK <c>SetNull</c>) and the display-bearing fields sit next to it as a
/// <b>snapshot</b> – the same pattern as in <see cref="ShopPurchase"/> and <see cref="ActivationRequest"/>.
/// Previously deleting an article cascaded down to here and destroyed purchased, not yet consumed units
/// while the purchase record remained next to it via <c>SetNull</c>: a receipt without any value behind it.
/// </para>
/// </summary>
public class ChildInventory
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    /// <summary>Reference to the article; set to null if the article is deleted later.</summary>
    public int? ShopArticleId { get; set; }
    public ShopArticle? ShopArticle { get; set; }
    /// <summary>
    /// Issuing supervisor (snapshot from <c>ShopArticle.AdultId</c>). It carries the supervisor view once
    /// the article is deleted – that view used to filter through <c>ShopArticle.AdultId</c> and would have
    /// made the position invisible, which is as good as deleted.
    /// </summary>
    public int SupervisorId { get; set; }
    // Snapshots (the stock stays readable and sortable even after the article is deleted)
    /// <summary>Article number at the time of purchase; it is also the sort key of both inventory views.</summary>
    public string ArticleNumber { get; set; } = "";
    public string ArticleTitle { get; set; } = "";
    /// <summary>Unit of measure of the quantity (e.g. <see cref="UnitType.Minute"/>).</summary>
    public UnitType UnitType { get; set; }
    /// <summary>Action type (e.g. <see cref="ActionType.TV"/>).</summary>
    public ActionType ActionType { get; set; }
    /// <summary>Total quantity available in the article's unit (e.g. 120 minutes of TV).</summary>
    public int Quantity { get; set; }
    /// <summary>Concurrency stamp: prevents simultaneous activations from overdrawing the stock.</summary>
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();
}

/// <summary>
/// Historical purchase entry for a <see cref="ShopListing"/>. Article number, title, prices and
/// <see cref="UnitsPerPurchase"/> are stored as a snapshot so the purchase history stays stable when the
/// supervisor later changes or deletes the listing.
/// </summary>
public class ShopPurchase
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    /// <summary>Reference to the listing; set to null if the listing is deleted later.</summary>
    public int? ShopListingId { get; set; }
    public ShopListing? ShopListing { get; set; }
    /// <summary>Issuing supervisor (snapshot from <c>ShopArticle.AdultId</c>): only they can cancel it.</summary>
    public int SupervisorId { get; set; }
    // Snapshots (a stable purchase history even after the listing changes or is deleted)
    public string ArticleNumber { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int CoinPrice { get; set; }
    public int GemPrice { get; set; }
    /// <summary>Quantity the inventory was increased by on purchase (snapshot of <see cref="ShopListing.UnitsPerPurchase"/>).</summary>
    public int UnitsPerPurchase { get; set; } = 1;
    public ShopPurchaseStatus Status { get; set; } = ShopPurchaseStatus.Owned;
    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    /// <summary>Concurrency stamp for cancelling, so a purchase is closed only once.</summary>
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();
}

/// <summary>
/// The child's activation request: it wants to consume <see cref="RequestedQuantity"/> units from its
/// aggregated inventory (<see cref="ChildInventory"/>). The supervisor approves or rejects; the inventory
/// is only reduced on approval. Title and unit are kept as a snapshot so the request history stays
/// readable even after the article is deleted.
/// </summary>
public class ActivationRequest
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    /// <summary>Reference to the article; set to null if the article is deleted later.</summary>
    public int? ShopArticleId { get; set; }
    public ShopArticle? ShopArticle { get; set; }
    /// <summary>Issuing supervisor (snapshot from <c>ShopArticle.AdultId</c>): only they approve/reject.</summary>
    public int SupervisorId { get; set; }
    /// <summary>Requested quantity in the article's unit (e.g. 10 minutes).</summary>
    public int RequestedQuantity { get; set; }
    public ActivationRequestStatus Status { get; set; } = ActivationRequestStatus.Pending;
    // Snapshots
    public string ArticleTitle { get; set; } = "";
    public UnitType UnitType { get; set; }
    public ActionType ActionType { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Instant of the supervisor's decision (null while still open).</summary>
    public DateTime? ClosedAt { get; set; }
}
