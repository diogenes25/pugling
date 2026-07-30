namespace Pugling.Contracts.Supervisor;

// Vertrag des Familien-Shops – der einzige Münz-Ausgabeweg. Der Vater pflegt Artikel (die Art) und
// Angebote (Preis + Bestand); gekauft wird ins aggregierte Inventar, eingelöst per Aktivierungsanfrage.
// Käufe und Aktivierungen sind ausstellergebunden (SupervisorId-Snapshot).

/// <summary>A basic catalog article of the supervisor: the <em>kind</em> of article (unit + category).</summary>
public record ShopArticleDto(int Id, string ArticleNumber, string Title, string Description,
    UnitType UnitType, ActionType ActionType, DateTime CreatedAt);

/// <summary>A listing for an article: price in both currencies, stock, and refill rule.</summary>
public record ShopListingDto(int Id, int ShopArticleId, string ArticleNumber, string ArticleTitle,
    string Title, string Description, int CoinPrice, int GemPrice, int UnitsPerPurchase,
    bool Active, int CurrentStock, int MaxStock, ShopRefillKind RefillKind,
    DateTime? RefillAtUtc, DayOfWeek? RefillDayOfWeek, DateTime? LastRefilledAtUtc, DateTime CreatedAt);

/// <summary>A historical purchase entry of the child.</summary>
public record ShopPurchaseDto(int Id, int ChildId, int? ShopListingId, string ArticleNumber,
    string Title, string Description, int CoinPrice, int GemPrice, int UnitsPerPurchase,
    ShopPurchaseStatus Status, DateTime PurchasedAt, DateTime? ClosedAt)
{
    /// <summary>May the supervisor cancel and refund this purchase now?</summary>
    public bool CanCancel { get; init; }
}

/// <summary>An activation request from the child for units from their inventory.</summary>
public record ActivationRequestDto(int Id, int ChildId, int? ShopArticleId, string ArticleTitle,
    UnitType UnitType, ActionType ActionType, int RequestedQuantity,
    ActivationRequestStatus Status, DateTime RequestedAt, DateTime? ClosedAt)
{
    /// <summary>May the supervisor approve this request now?</summary>
    public bool CanApprove { get; init; }
    /// <summary>May the supervisor reject this request now?</summary>
    public bool CanReject { get; init; }
}

/// <summary>Input for creating a catalog article.</summary>
public record CreateShopArticleDto(string ArticleNumber, string Title, string? Description,
    UnitType UnitType, ActionType ActionType);

/// <summary>Partial change to a catalog article; omitted fields stay unchanged.</summary>
public record UpdateShopArticleDto(string? ArticleNumber, string? Title, string? Description,
    UnitType? UnitType, ActionType? ActionType);

/// <summary>Input for creating a listing for an article.</summary>
public record CreateShopListingDto(string? Title, string? Description,
    int CoinPrice, int GemPrice, int UnitsPerPurchase, int CurrentStock, int MaxStock,
    ShopRefillKind RefillKind = ShopRefillKind.None,
    DateTime? RefillAtUtc = null, DayOfWeek? RefillDayOfWeek = null);

/// <summary>Partial change to a listing; omitted fields stay unchanged.</summary>
public record UpdateShopListingDto(string? Title, string? Description,
    int? CoinPrice, int? GemPrice, int? UnitsPerPurchase, bool? Active,
    int? CurrentStock, int? MaxStock, ShopRefillKind? RefillKind,
    DateTime? RefillAtUtc, DayOfWeek? RefillDayOfWeek);

/// <summary>
/// An entry in the child's aggregated inventory. <c>ShopArticleId</c> is <c>null</c> if the
/// article was deleted after purchase – title and unit then come from the snapshot on the
/// inventory (as with <see cref="ActivationRequestDto"/>). Such an entry can no longer be activated:
/// activation is addressed via the article id.
/// </summary>
public record InventoryItemDto(int? ShopArticleId, string ArticleNumber, string Title,
    UnitType UnitType, ActionType ActionType, int Quantity);
