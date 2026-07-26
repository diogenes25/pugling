namespace Pugling.Contracts.Supervisor;

// Vertrag des Familien-Shops – der einzige Münz-Ausgabeweg. Der Vater pflegt Artikel (die Art) und
// Angebote (Preis + Bestand); gekauft wird ins aggregierte Inventar, eingelöst per Aktivierungsanfrage.
// Käufe und Aktivierungen sind ausstellergebunden (SupervisorId-Snapshot).

/// <summary>Ein Basis-Katalogartikel des Vaters: die <em>Art</em> des Artikels (Einheit + Kategorie).</summary>
public record ShopArticleDto(int Id, string ArticleNumber, string Title, string Description,
    UnitType UnitType, ActionType ActionType, DateTime CreatedAt);

/// <summary>Ein Angebot zu einem Artikel: Preis in beiden Währungen, Bestand und Auffüll-Regel.</summary>
public record ShopListingDto(int Id, int ShopArticleId, string ArticleNumber, string ArticleTitle,
    string Title, string Description, int CoinPrice, int GemPrice, int UnitsPerPurchase,
    bool Active, int CurrentStock, int MaxStock, ShopRefillKind RefillKind,
    DateTime? RefillAtUtc, DayOfWeek? RefillDayOfWeek, DateTime? LastRefilledAtUtc, DateTime CreatedAt);

/// <summary>Eine historische Kaufbuchung des Kindes.</summary>
public record ShopPurchaseDto(int Id, int ChildId, int? ShopListingId, string ArticleNumber,
    string Title, string Description, int CoinPrice, int GemPrice, int UnitsPerPurchase,
    ShopPurchaseStatus Status, DateTime PurchasedAt, DateTime? ClosedAt)
{
    /// <summary>Darf der Vater diesen Kauf jetzt stornieren und erstatten?</summary>
    public bool CanCancel { get; init; }
}

/// <summary>Eine Aktivierungsanfrage des Sohns auf Einheiten aus seinem Inventar.</summary>
public record ActivationRequestDto(int Id, int ChildId, int? ShopArticleId, string ArticleTitle,
    UnitType UnitType, ActionType ActionType, int RequestedQuantity,
    ActivationRequestStatus Status, DateTime RequestedAt, DateTime? ClosedAt)
{
    /// <summary>Darf der Vater diese Anfrage jetzt genehmigen?</summary>
    public bool CanApprove { get; init; }
    /// <summary>Darf der Vater diese Anfrage jetzt ablehnen?</summary>
    public bool CanReject { get; init; }
}

/// <summary>Eingabe zum Anlegen eines Katalogartikels.</summary>
public record CreateShopArticleDto(string ArticleNumber, string Title, string? Description,
    UnitType UnitType, ActionType ActionType);

/// <summary>Partielle Änderung eines Katalogartikels; weggelassene Felder bleiben unverändert.</summary>
public record UpdateShopArticleDto(string? ArticleNumber, string? Title, string? Description,
    UnitType? UnitType, ActionType? ActionType);

/// <summary>Eingabe zum Anlegen eines Angebots zu einem Artikel.</summary>
public record CreateShopListingDto(string? Title, string? Description,
    int CoinPrice, int GemPrice, int UnitsPerPurchase, int CurrentStock, int MaxStock,
    ShopRefillKind RefillKind = ShopRefillKind.None,
    DateTime? RefillAtUtc = null, DayOfWeek? RefillDayOfWeek = null);

/// <summary>Partielle Änderung eines Angebots; weggelassene Felder bleiben unverändert.</summary>
public record UpdateShopListingDto(string? Title, string? Description,
    int? CoinPrice, int? GemPrice, int? UnitsPerPurchase, bool? Active,
    int? CurrentStock, int? MaxStock, ShopRefillKind? RefillKind,
    DateTime? RefillAtUtc, DayOfWeek? RefillDayOfWeek);

/// <summary>Eine Position im aggregierten Inventar des Kindes.</summary>
public record InventoryItemDto(int ShopArticleId, string ArticleNumber, string Title,
    UnitType UnitType, ActionType ActionType, int Quantity);
