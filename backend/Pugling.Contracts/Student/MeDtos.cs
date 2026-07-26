namespace Pugling.Contracts.Student;

// Vertrag der Sohn-Selbstsicht (api/v1/student/me/…): Kontostand, Skins und der Familien-Shop
// aus seiner Perspektive. Die „My…"-Namen grenzen die Sohn-Sicht bewusst von der gleichnamigen
// Vater-Sicht ab – dieselbe Sache, anderer Ausschnitt (der Sohn sieht z. B. keine ChildId).

/// <summary>Eine einzelne Punkte-Buchung (Gutschrift positiv, Abzug negativ) mit Kategorie.</summary>
public record MyPointsEntryResponse(int Id, int Amount, PointKind Kind, string Reason, DateTime CreatedAt);

/// <summary>Kontostand (Wallet) des Kindes je Währung. Die Buchungen liegen unter <c>points/entries</c>.</summary>
public record WalletResponse(int ChildId, int Coins, int Gems);

/// <summary>Skin-Zustand des Kindes: aktueller Gem-Stand, ausgerüsteter und freigeschaltete Skins.</summary>
public record SkinStateResponse(int Gems, string Selected, IReadOnlyList<string> Owned);

/// <summary>Ein kaufbares Angebot aus dem Familien-Shop aus Sohn-Sicht (Listing-Ebene).</summary>
public record ShopListingResponse(int Id, int ShopArticleId, string ArticleNumber, string ArticleTitle,
    UnitType UnitType, ActionType ActionType, string Title, string Description,
    int CoinPrice, int GemPrice, int UnitsPerPurchase, int CurrentStock, bool Affordable);

/// <summary>Ein Eintrag im aggregierten Sohn-Inventar: Artikel-Typ → Gesamtmenge.</summary>
public record MyInventoryItemResponse(int ShopArticleId, string ArticleNumber, string Title,
    UnitType UnitType, ActionType ActionType, int Quantity);

/// <summary>Eigene Kaufbuchung im Sohn-Kassenbuch.</summary>
public record MyShopPurchaseResponse(int Id, int? ShopListingId, string ArticleNumber, string Title,
    int CoinPrice, int GemPrice, int UnitsPerPurchase, ShopPurchaseStatus Status,
    DateTime PurchasedAt, DateTime? ClosedAt);

/// <summary>Eigene Aktivierungsanfrage aus Sohn-Sicht.</summary>
public record MyActivationResponse(int Id, int? ShopArticleId, string ArticleTitle,
    UnitType UnitType, ActionType ActionType, int RequestedQuantity,
    ActivationRequestStatus Status, DateTime RequestedAt, DateTime? ClosedAt);

/// <summary>Shop-Sicht des Sohns: Wallet, kaufbare Angebote, aggregiertes Inventar und Kaufhistorie.</summary>
public record ShopViewResponse(int Coins, int Gems,
    IReadOnlyList<ShopListingResponse> Available,
    IReadOnlyList<MyInventoryItemResponse> Inventory,
    IReadOnlyList<MyShopPurchaseResponse> Purchases);

/// <summary>Eingabe der Aktivierungsanfrage: wie viele Einheiten aus dem Inventar eingelöst werden sollen.</summary>
public record ActivateDto(int Quantity);
