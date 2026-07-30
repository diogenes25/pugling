namespace Pugling.Contracts.Student;

// Vertrag der Sohn-Selbstsicht (api/v1/student/me/…): Kontostand, Skins und der Familien-Shop
// aus seiner Perspektive. Die „My…"-Namen grenzen die Sohn-Sicht bewusst von der gleichnamigen
// Vater-Sicht ab – dieselbe Sache, anderer Ausschnitt (der Sohn sieht z. B. keine ChildId).

/// <summary>A single points ledger entry (credit positive, deduction negative) with category.</summary>
public record MyPointsEntryResponse(int Id, int Amount, PointKind Kind, string Reason, DateTime CreatedAt);

/// <summary>Account balance (wallet) of the child per currency. The ledger entries are under <c>points/entries</c>.</summary>
public record WalletResponse(int ChildId, int Coins, int Gems);

/// <summary>Skin state of the child: current gem balance, equipped and unlocked skins.</summary>
public record SkinStateResponse(int Gems, string Selected, IReadOnlyList<string> Owned);

/// <summary>A purchasable offer from the family shop from the child's perspective (listing level).</summary>
public record ShopListingResponse(int Id, int ShopArticleId, string ArticleNumber, string ArticleTitle,
    UnitType UnitType, ActionType ActionType, string Title, string Description,
    int CoinPrice, int GemPrice, int UnitsPerPurchase, int CurrentStock, bool Affordable);

/// <summary>
/// An entry in the child's aggregated inventory: article type → total quantity. <c>ShopArticleId</c> is
/// <c>null</c> if the article was deleted after purchase; title and unit then come from the
/// snapshot on the inventory. What was paid for remains visible – the child just can no longer redeem it,
/// because activation is addressed via the article id.
/// </summary>
public record MyInventoryItemResponse(int? ShopArticleId, string ArticleNumber, string Title,
    UnitType UnitType, ActionType ActionType, int Quantity);

/// <summary>The child's own purchase ledger entry in their cashbook.</summary>
public record MyShopPurchaseResponse(int Id, int? ShopListingId, string ArticleNumber, string Title,
    int CoinPrice, int GemPrice, int UnitsPerPurchase, ShopPurchaseStatus Status,
    DateTime PurchasedAt, DateTime? ClosedAt);

/// <summary>The child's own activation request from their perspective.</summary>
public record MyActivationResponse(int Id, int? ShopArticleId, string ArticleTitle,
    UnitType UnitType, ActionType ActionType, int RequestedQuantity,
    ActivationRequestStatus Status, DateTime RequestedAt, DateTime? ClosedAt);

/// <summary>The child's shop view: wallet, purchasable offers, aggregated inventory and purchase history.</summary>
public record ShopViewResponse(int Coins, int Gems,
    IReadOnlyList<ShopListingResponse> Available,
    IReadOnlyList<MyInventoryItemResponse> Inventory,
    IReadOnlyList<MyShopPurchaseResponse> Purchases);

/// <summary>Input for the activation request: how many units from the inventory should be redeemed.</summary>
public record ActivateDto(int Quantity);
