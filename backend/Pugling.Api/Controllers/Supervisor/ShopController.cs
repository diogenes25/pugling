using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;
using Pugling.Api.Services;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// Familien-Shop des Vaters: Artikel-Katalog und Angebote verwalten; kindbezogene Käufe,
/// Inventar und Aktivierungsanfragen einsehen und entscheiden.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/shop")]
[Tags("Admin – Shop")]
[Produces("application/json")]
[Authorize(Roles = Roles.Vater)]
public class ShopController(PuglingDbContext db, ShopService shop) : ControllerBase
{
    // ─── DTOs ────────────────────────────────────────────────────────────────

    public record ShopArticleDto(int Id, string ArticleNumber, string Title, string Description,
        UnitType UnitType, ActionType ActionType, DateTime CreatedAt);

    public record ShopListingDto(int Id, int ShopArticleId, string ArticleNumber, string ArticleTitle,
        string Title, string Description, int CoinPrice, int GemPrice, int UnitsPerPurchase,
        bool Active, int CurrentStock, int MaxStock, ShopRefillKind RefillKind,
        DateTime? RefillAtUtc, DayOfWeek? RefillDayOfWeek, DateTime? LastRefilledAtUtc, DateTime CreatedAt);

    public record ShopPurchaseDto(int Id, int ChildId, int? ShopListingId, string ArticleNumber,
        string Title, string Description, int CoinPrice, int GemPrice, int UnitsPerPurchase,
        ShopPurchaseStatus Status, DateTime PurchasedAt, DateTime? ClosedAt)
    {
        /// <summary>Darf der Vater diesen Kauf jetzt stornieren und erstatten?</summary>
        public bool CanCancel { get; init; }
    }

    public record ActivationRequestDto(int Id, int ChildId, int? ShopArticleId, string ArticleTitle,
        UnitType UnitType, ActionType ActionType, int RequestedQuantity,
        ActivationRequestStatus Status, DateTime RequestedAt, DateTime? ClosedAt)
    {
        /// <summary>Darf der Vater diese Anfrage jetzt genehmigen?</summary>
        public bool CanApprove { get; init; }
        /// <summary>Darf der Vater diese Anfrage jetzt ablehnen?</summary>
        public bool CanReject { get; init; }
    }

    public record CreateShopArticleDto(string ArticleNumber, string Title, string? Description,
        UnitType UnitType, ActionType ActionType);

    public record UpdateShopArticleDto(string? ArticleNumber, string? Title, string? Description,
        UnitType? UnitType, ActionType? ActionType);

    public record CreateShopListingDto(string? Title, string? Description,
        int CoinPrice, int GemPrice, int UnitsPerPurchase, int CurrentStock, int MaxStock,
        ShopRefillKind RefillKind = ShopRefillKind.None,
        DateTime? RefillAtUtc = null, DayOfWeek? RefillDayOfWeek = null);

    public record UpdateShopListingDto(string? Title, string? Description,
        int? CoinPrice, int? GemPrice, int? UnitsPerPurchase, bool? Active,
        int? CurrentStock, int? MaxStock, ShopRefillKind? RefillKind,
        DateTime? RefillAtUtc, DayOfWeek? RefillDayOfWeek);

    // ─── Mapping ─────────────────────────────────────────────────────────────

    private static ShopArticleDto MapArticle(ShopArticle a) =>
        new(a.Id, a.ArticleNumber, a.Title, a.Description, a.UnitType, a.ActionType, a.CreatedAt);

    private static ShopListingDto MapListing(ShopListing l) =>
        new(l.Id, l.ShopArticleId, l.ShopArticle?.ArticleNumber ?? "",
            l.ShopArticle?.Title ?? "", l.Title, l.Description,
            l.CoinPrice, l.GemPrice, l.UnitsPerPurchase, l.Active,
            l.CurrentStock, l.MaxStock, l.RefillKind,
            l.RefillAtUtc, l.RefillDayOfWeek, l.LastRefilledAtUtc, l.CreatedAt);

    private static ShopPurchaseDto MapPurchase(ShopPurchase p) =>
        new(p.Id, p.ChildId, p.ShopListingId, p.ArticleNumber, p.Title, p.Description,
            p.CoinPrice, p.GemPrice, p.UnitsPerPurchase, p.Status, p.PurchasedAt, p.ClosedAt)
        { CanCancel = p.Status == ShopPurchaseStatus.Owned };

    private static ActivationRequestDto MapActivation(ActivationRequest r) =>
        new(r.Id, r.ChildId, r.ShopArticleId, r.ArticleTitle, r.UnitType, r.ActionType,
            r.RequestedQuantity, r.Status, r.RequestedAt, r.ClosedAt)
        {
            CanApprove = r.Status == ActivationRequestStatus.Pending,
            CanReject = r.Status == ActivationRequestStatus.Pending,
        };

    // ─── Artikel-CRUD ────────────────────────────────────────────────────────

    /// <summary>Familien-Shop-Artikel des angemeldeten Vaters (ohne Bestands-/Preisdetails).</summary>
    /// <param name="search">Freitext-Suche in Artikelnummer und Titel (Teilstring, optional).</param>
    /// <param name="skip">Anzahl übersprungener Einträge (Offset, Standard 0).</param>
    /// <param name="take">Maximale Anzahl zurückgegebener Einträge (Standard 100, Max 500).</param>
    [HttpGet("articles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ShopArticleDto>>> Articles(
        [FromQuery] string? search,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake)
    {
        var fatherId = User.FatherId()!.Value;
        var query = db.ShopArticles
            .AsNoTracking()
            .Where(a => a.FatherId == fatherId);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.Title.Contains(search) || a.ArticleNumber.Contains(search));
        return (await query
            .OrderBy(a => a.ArticleNumber)
            .ToPagedListAsync(Response, skip, take))
            .Select(MapArticle)
            .ToList();
    }

    /// <summary>Einen einzelnen Familien-Shop-Artikel des Vaters lesen.</summary>
    [HttpGet("articles/{articleId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShopArticleDto>> Article(int articleId)
    {
        var fatherId = User.FatherId()!.Value;
        var article = await db.ShopArticles.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == articleId && a.FatherId == fatherId);
        if (article is null) return NotFound();
        return MapArticle(article);
    }

    /// <summary>Legt einen Artikel im Familien-Shop an (Typ-Definition ohne Preis/Bestand).</summary>
    [HttpPost("articles")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ShopArticleDto>> CreateArticle(CreateShopArticleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ArticleNumber))
            return this.ProblemWithCode(ApiErrors.ValidationError, "Article number is required.");
        if (string.IsNullOrWhiteSpace(dto.Title))
            return this.ProblemWithCode(ApiErrors.ValidationError, "Title is required.");

        var fatherId = User.FatherId()!.Value;
        var articleNumber = dto.ArticleNumber.Trim();
        if (await db.ShopArticles.AnyAsync(a => a.FatherId == fatherId && a.ArticleNumber == articleNumber))
            return this.ProblemWithCode(ApiErrors.DuplicateKey, "Article number already exists in this family shop.");

        var article = new ShopArticle
        {
            FatherId = fatherId,
            ArticleNumber = articleNumber,
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim() ?? "",
            UnitType = dto.UnitType,
            ActionType = dto.ActionType,
        };
        db.ShopArticles.Add(article);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Article), new { articleId = article.Id }, MapArticle(article));
    }

    /// <summary>Ändert einen Familien-Shop-Artikel partiell.</summary>
    [HttpPatch("articles/{articleId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ShopArticleDto>> UpdateArticle(int articleId, UpdateShopArticleDto dto)
    {
        var fatherId = User.FatherId()!.Value;
        var article = await db.ShopArticles.FirstOrDefaultAsync(a => a.Id == articleId && a.FatherId == fatherId);
        if (article is null) return NotFound();

        var nextNumber = dto.ArticleNumber?.Trim() ?? article.ArticleNumber;
        if (dto.ArticleNumber is not null && string.IsNullOrWhiteSpace(dto.ArticleNumber))
            return this.ProblemWithCode(ApiErrors.ValidationError, "Article number must not be empty.");
        if (dto.Title is not null && string.IsNullOrWhiteSpace(dto.Title))
            return this.ProblemWithCode(ApiErrors.ValidationError, "Title must not be empty.");

        if (nextNumber != article.ArticleNumber
            && await db.ShopArticles.AnyAsync(a => a.FatherId == fatherId && a.ArticleNumber == nextNumber && a.Id != articleId))
            return this.ProblemWithCode(ApiErrors.DuplicateKey, "Article number already exists in this family shop.");

        article.ArticleNumber = nextNumber;
        if (dto.Title is not null) article.Title = dto.Title.Trim();
        if (dto.Description is not null) article.Description = dto.Description.Trim();
        if (dto.UnitType is not null) article.UnitType = dto.UnitType.Value;
        if (dto.ActionType is not null) article.ActionType = dto.ActionType.Value;
        await db.SaveChangesAsync();
        return MapArticle(article);
    }

    /// <summary>Löscht einen Artikel samt aller zugehörigen Angebote. Kaufhistorie bleibt als Snapshot erhalten.</summary>
    [HttpDelete("articles/{articleId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteArticle(int articleId)
    {
        var fatherId = User.FatherId()!.Value;
        var article = await db.ShopArticles.FirstOrDefaultAsync(a => a.Id == articleId && a.FatherId == fatherId);
        if (article is null) return NotFound();
        db.ShopArticles.Remove(article);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ─── Angebots-CRUD (Listings pro Artikel) ────────────────────────────────

    /// <summary>Alle Angebote zu einem Artikel des Vaters.</summary>
    [HttpGet("articles/{articleId:int}/listings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ShopListingDto>>> Listings(int articleId)
    {
        var fatherId = User.FatherId()!.Value;
        var article = await db.ShopArticles.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == articleId && a.FatherId == fatherId);
        if (article is null) return NotFound();

        var listings = await shop.ListingsForFatherAsync(fatherId, activeOnly: false, DateTime.UtcNow);
        return listings.Where(l => l.ShopArticleId == articleId).Select(MapListing).ToList();
    }

    /// <summary>Ein einzelnes Angebot eines Artikels lesen.</summary>
    [HttpGet("articles/{articleId:int}/listings/{listingId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShopListingDto>> Listing(int articleId, int listingId)
    {
        var fatherId = User.FatherId()!.Value;
        // Nur DIESES Angebot laden (kein Voll-Scan aller Vater-Angebote). AsNoTracking + rein
        // anzeigeseitige Auffüllung (ApplyDueRefill mutiert nur das nicht getrackte Objekt, kein
        // SaveChanges) – ein GET darf keinen Schreib-Nebeneffekt haben. Persistiert wird die Auffüllung
        // beim Listen-Abruf bzw. beim Kauf; die Darstellung hier ist dieselbe.
        var listing = await db.ShopListings.AsNoTracking()
            .Include(l => l.ShopArticle)
            .FirstOrDefaultAsync(l => l.Id == listingId && l.ShopArticleId == articleId
                && l.ShopArticle!.FatherId == fatherId);
        if (listing is null) return NotFound();

        ShopService.ApplyDueRefill(listing, DateTime.UtcNow);
        return MapListing(listing);
    }

    /// <summary>Legt ein neues Angebot für einen Artikel an (mit Preis, Menge und Bestand).</summary>
    [HttpPost("articles/{articleId:int}/listings")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShopListingDto>> CreateListing(int articleId, CreateShopListingDto dto)
    {
        var fatherId = User.FatherId()!.Value;
        var article = await db.ShopArticles.FirstOrDefaultAsync(a => a.Id == articleId && a.FatherId == fatherId);
        if (article is null) return NotFound();

        var validation = ValidateListing(dto.CoinPrice, dto.GemPrice, dto.UnitsPerPurchase,
            dto.CurrentStock, dto.MaxStock, dto.RefillKind, dto.RefillAtUtc, dto.RefillDayOfWeek);
        if (validation is not null) return validation;

        var listing = new ShopListing
        {
            ShopArticleId = articleId,
            Title = dto.Title?.Trim() ?? "",
            Description = dto.Description?.Trim() ?? "",
            CoinPrice = dto.CoinPrice,
            GemPrice = dto.GemPrice,
            UnitsPerPurchase = dto.UnitsPerPurchase,
            CurrentStock = dto.CurrentStock,
            MaxStock = dto.MaxStock,
            RefillKind = dto.RefillKind,
            RefillAtUtc = dto.RefillAtUtc,
            RefillDayOfWeek = dto.RefillDayOfWeek,
        };
        db.ShopListings.Add(listing);
        await db.SaveChangesAsync();
        listing.ShopArticle = article;
        return CreatedAtAction(nameof(Listing), new { articleId, listingId = listing.Id }, MapListing(listing));
    }

    /// <summary>Ändert ein Angebot partiell (Preis, Menge, Bestand, Aktiv-Status).</summary>
    [HttpPatch("articles/{articleId:int}/listings/{listingId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShopListingDto>> UpdateListing(
        int articleId, int listingId, UpdateShopListingDto dto)
    {
        var fatherId = User.FatherId()!.Value;
        var listing = await db.ShopListings
            .Include(l => l.ShopArticle)
            .FirstOrDefaultAsync(l => l.Id == listingId && l.ShopArticleId == articleId
                && l.ShopArticle!.FatherId == fatherId);
        if (listing is null) return NotFound();

        var nextCoin = dto.CoinPrice ?? listing.CoinPrice;
        var nextGem = dto.GemPrice ?? listing.GemPrice;
        var nextUnits = dto.UnitsPerPurchase ?? listing.UnitsPerPurchase;
        var nextStock = dto.CurrentStock ?? listing.CurrentStock;
        var nextMaxStock = dto.MaxStock ?? listing.MaxStock;
        var nextRefillKind = dto.RefillKind ?? listing.RefillKind;
        var nextRefillAt = dto.RefillAtUtc ?? listing.RefillAtUtc;
        var nextRefillDay = dto.RefillDayOfWeek ?? listing.RefillDayOfWeek;

        var validation = ValidateListing(nextCoin, nextGem, nextUnits,
            nextStock, nextMaxStock, nextRefillKind, nextRefillAt, nextRefillDay);
        if (validation is not null) return validation;

        if (dto.Title is not null) listing.Title = dto.Title.Trim();
        if (dto.Description is not null) listing.Description = dto.Description.Trim();
        listing.CoinPrice = nextCoin;
        listing.GemPrice = nextGem;
        listing.UnitsPerPurchase = nextUnits;
        listing.CurrentStock = nextStock;
        listing.MaxStock = nextMaxStock;
        listing.RefillKind = nextRefillKind;
        listing.RefillAtUtc = nextRefillAt;
        listing.RefillDayOfWeek = nextRefillDay;
        if (dto.Active is not null) listing.Active = dto.Active.Value;
        listing.ConcurrencyStamp = Guid.NewGuid();
        await db.SaveChangesAsync();
        return MapListing(listing);
    }

    /// <summary>Löscht ein Angebot. Bereits getätigte Käufe bleiben als Snapshot im Inventar erhalten.</summary>
    [HttpDelete("articles/{articleId:int}/listings/{listingId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteListing(int articleId, int listingId)
    {
        var fatherId = User.FatherId()!.Value;
        var listing = await db.ShopListings
            .Include(l => l.ShopArticle)
            .FirstOrDefaultAsync(l => l.Id == listingId && l.ShopArticleId == articleId
                && l.ShopArticle!.FatherId == fatherId);
        if (listing is null) return NotFound();
        db.ShopListings.Remove(listing);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ─── Kind-Inventar ───────────────────────────────────────────────────────

    /// <summary>Aggregiertes Inventar eines Kindes: pro Artikel-Typ die verfügbare Gesamtmenge.</summary>
    /// <param name="childId">Id des Kindes.</param>
    /// <param name="skip">Anzahl übersprungener Einträge (Offset, Standard 0).</param>
    /// <param name="take">Maximale Anzahl zurückgegebener Einträge (Standard 100, Max 500).</param>
    [HttpGet("~/" + ApiRoutes.Supervisor + "/children/{childId:int}/shop/inventory")]
    [ServiceFilter(typeof(ChildOwnershipFilter))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<InventoryItemDto>>> ChildInventory(int childId,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake)
    {
        var query = db.ChildInventories.AsNoTracking()
            .Where(i => i.ChildId == childId && i.Quantity > 0)
            .OrderBy(i => i.ShopArticle!.ArticleNumber)
            .Select(i => new InventoryItemDto(
                i.ShopArticleId, i.ShopArticle!.ArticleNumber, i.ShopArticle!.Title,
                i.ShopArticle!.UnitType, i.ShopArticle!.ActionType, i.Quantity));
        return await query.ToPagedListAsync(Response, skip, take);
    }

    public record InventoryItemDto(int ShopArticleId, string ArticleNumber, string Title,
        UnitType UnitType, ActionType ActionType, int Quantity);

    // ─── Kaufhistorie ────────────────────────────────────────────────────────

    /// <summary>Kaufhistorie eines Kindes, optional nach Status gefiltert.</summary>
    [HttpGet("~/" + ApiRoutes.Supervisor + "/children/{childId:int}/shop/purchases")]
    [ServiceFilter(typeof(ChildOwnershipFilter))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ShopPurchaseDto>>> ChildPurchases(int childId,
        [FromQuery] ShopPurchaseStatus? status,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake)
    {
        var query = db.ShopPurchases.AsNoTracking().Where(p => p.ChildId == childId);
        if (status is not null) query = query.Where(p => p.Status == status);

        return await query
            .OrderBy(p => p.Status == ShopPurchaseStatus.Owned ? 0 : 1)
            .ThenByDescending(p => p.PurchasedAt).ThenByDescending(p => p.Id)
            .Select(p => MapPurchase(p))
            .ToPagedListAsync(Response, skip, take);
    }

    /// <summary>Storniert einen offenen Kauf und erstattet Coins/Gems zurück.</summary>
    [HttpPost("~/" + ApiRoutes.Supervisor + "/children/{childId:int}/shop/purchases/{purchaseId:int}/cancel")]
    [ServiceFilter(typeof(ChildOwnershipFilter))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ShopPurchaseDto>> CancelPurchase(int childId, int purchaseId)
    {
        var result = await shop.CancelPurchaseAsync(childId, purchaseId, DateTime.UtcNow);
        return result.Error switch
        {
            ShopService.ShopError.None => MapPurchase(result.Value!),
            ShopService.ShopError.NotFound => NotFound(),
            ShopService.ShopError.NotOpen => this.ProblemWithCode(ShopService.ToApiError(result.Error), "This shop purchase is no longer open."),
            _ => this.ProblemWithCode(ShopService.ToApiError(result.Error), "The shop operation could not be completed."),
        };
    }

    // ─── Aktivierungsanfragen ────────────────────────────────────────────────

    /// <summary>Aktivierungsanfragen eines Kindes, optional nach Status gefiltert (offene zuerst).</summary>
    [HttpGet("~/" + ApiRoutes.Supervisor + "/children/{childId:int}/shop/activations")]
    [ServiceFilter(typeof(ChildOwnershipFilter))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ActivationRequestDto>>> ChildActivations(int childId,
        [FromQuery] ActivationRequestStatus? status,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake)
    {
        var query = db.ActivationRequests.AsNoTracking().Where(r => r.ChildId == childId);
        if (status is not null) query = query.Where(r => r.Status == status);

        return await query
            .OrderBy(r => r.Status == ActivationRequestStatus.Pending ? 0 : 1)
            .ThenByDescending(r => r.RequestedAt)
            .Select(r => MapActivation(r))
            .ToPagedListAsync(Response, skip, take);
    }

    /// <summary>Genehmigt eine offene Aktivierungsanfrage; das Inventar des Kindes wird reduziert.</summary>
    [HttpPost("~/" + ApiRoutes.Supervisor + "/children/{childId:int}/shop/activations/{requestId:int}/approve")]
    [ServiceFilter(typeof(ChildOwnershipFilter))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ActivationRequestDto>> ApproveActivation(int childId, int requestId)
    {
        var result = await shop.ApproveActivationAsync(childId, requestId, DateTime.UtcNow);
        return ActivationResult(result);
    }

    /// <summary>Lehnt eine offene Aktivierungsanfrage ab; das Inventar des Kindes bleibt unverändert.</summary>
    [HttpPost("~/" + ApiRoutes.Supervisor + "/children/{childId:int}/shop/activations/{requestId:int}/reject")]
    [ServiceFilter(typeof(ChildOwnershipFilter))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ActivationRequestDto>> RejectActivation(int childId, int requestId)
    {
        var result = await shop.RejectActivationAsync(childId, requestId, DateTime.UtcNow);
        return ActivationResult(result);
    }

    // ─── Hilfsmethoden ───────────────────────────────────────────────────────

    private ActionResult<ActivationRequestDto> ActivationResult(ShopService.Result<ActivationRequest> result) =>
        result.Error switch
        {
            ShopService.ShopError.None => MapActivation(result.Value!),
            ShopService.ShopError.NotFound => NotFound(),
            ShopService.ShopError.NotPending => this.ProblemWithCode(ShopService.ToApiError(result.Error), "This activation request is not pending."),
            ShopService.ShopError.InsufficientInventory => this.ProblemWithCode(ShopService.ToApiError(result.Error), "Not enough units left in the child's inventory to approve this request."),
            _ => this.ProblemWithCode(ShopService.ToApiError(result.Error), "The activation could not be processed."),
        };

    private ActionResult? ValidateListing(int coinPrice, int gemPrice, int unitsPerPurchase,
        int currentStock, int maxStock, ShopRefillKind refillKind,
        DateTime? refillAtUtc, DayOfWeek? refillDayOfWeek)
    {
        if (coinPrice < 0 || gemPrice < 0) return this.ProblemWithCode(ApiErrors.ValidationError, "Prices must not be negative.");
        if (coinPrice == 0 && gemPrice == 0) return this.ProblemWithCode(ApiErrors.ValidationError, "At least one price must be positive.");
        if (unitsPerPurchase <= 0) return this.ProblemWithCode(ApiErrors.ValidationError, "UnitsPerPurchase must be at least 1.");
        if (currentStock < 0 || maxStock < 0) return this.ProblemWithCode(ApiErrors.ValidationError, "Stock values must not be negative.");
        if (currentStock > maxStock && maxStock > 0) return this.ProblemWithCode(ApiErrors.ValidationError, "Current stock must not exceed max stock.");
        if (refillKind == ShopRefillKind.Once && refillAtUtc is null)
            return this.ProblemWithCode(ApiErrors.ValidationError, "RefillAtUtc is required for one-time refills.");
        if (refillKind == ShopRefillKind.Weekly && refillDayOfWeek is null)
            return this.ProblemWithCode(ApiErrors.ValidationError, "RefillDayOfWeek is required for weekly refills.");
        return null;
    }
}
