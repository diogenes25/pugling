using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// The father's family shop: manage the article catalog and listings; view and decide on
/// child-related purchases, inventory and activation requests.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/shop")]
[Tags("Supervisor – Shop")]
[Produces("application/json")]
[Authorize(Roles = Roles.Supervisor)]
public class ShopController(PuglingDbContext db, ShopService shop, TimeProvider time) : ControllerBase
{
    // ─── DTOs ────────────────────────────────────────────────────────────────

    // ─── Mapping ─────────────────────────────────────────────────────────────

    private static ShopArticleDto MapArticle(ShopArticle a) =>
        new(a.Id, a.ArticleNumber, a.Title, a.Description, a.UnitType, a.ActionType, a.CreatedAt);

    private static ShopListingDto MapListing(ShopService.ListingView view)
    {
        var l = view.Listing;
        return new(l.Id, l.ShopArticleId, l.ShopArticle?.ArticleNumber ?? "",
            l.ShopArticle?.Title ?? "", l.Title, l.Description,
            l.CoinPrice, l.GemPrice, l.UnitsPerPurchase, l.Active,
            l.MaxStock, l.RefillQuantity, l.RefillKind,
            l.RefillAtUtc, l.RefillDayOfWeek, l.CreatedAt, view.AudienceChildIds,
            view.Stocks.Select(s => new ShopListingStockDto(
                s.ChildId, s.ChildName, s.CurrentStock, s.LastRefilledAtUtc, s.NextRefillAtUtc)).ToList());
    }

    private static ShopPurchaseDto MapPurchase(ShopPurchase p) =>
        new(p.Id, p.ChildId, p.ShopListingId, p.ArticleNumber, p.Title, p.Description,
            p.CoinPrice, p.GemPrice, p.UnitsPerPurchase, p.Status, p.PurchasedAt, p.ClosedAt)
        { CanCancel = p.Status == ShopPurchaseStatus.Owned };

    private static ActivationRequestDto MapActivation(ActivationRequest r) =>
        new(r.Id, r.ChildId, r.ShopArticleId, r.ArticleTitle, r.UnitType, r.ActionType,
            r.RequestedQuantity, r.Status, r.RequestedAt, r.ClosedAt, r.RequestedBy)
        {
            CanApprove = r.Status == ActivationRequestStatus.Pending,
            CanReject = r.Status == ActivationRequestStatus.Pending,
        };

    // ─── Article CRUD ────────────────────────────────────────────────────────

    /// <summary>Family shop articles of the logged-in father (without stock/price details).</summary>
    /// <param name="search">Free-text search in article number and title (substring, optional).</param>
    /// <param name="skip">Number of entries skipped (offset, default 0).</param>
    /// <param name="take">Maximum number of entries returned (default 100, max 500).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("articles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ShopArticleDto>>> Articles(
        [FromQuery] string? search,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake,
        CancellationToken ct = default)
    {
        var supervisorId = User.AdultId()!.Value;
        var query = db.ShopArticles
            .AsNoTracking()
            .Where(a => a.AdultId == supervisorId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            // LIKE, not Contains: EF maps Contains to SQLite's byte-exact instr() - see SearchPattern (B-135).
            // The article number folds case as well: it is the supervisor's own shorthand for finding the
            // article again ("TV-001"), not a system key - so "tv-001" has to hit it.
            var pattern = SearchPattern.Contains(search);
            query = query.Where(a => EF.Functions.Like(a.Title, pattern, SearchPattern.Escape)
                                     || EF.Functions.Like(a.ArticleNumber, pattern, SearchPattern.Escape));
        }
        return (await query
            .OrderBy(a => a.ArticleNumber)
            .ToPagedListAsync(Response, skip, take, ct))
            .Select(MapArticle)
            .ToList();
    }

    /// <summary>Read a single family shop article of the father.</summary>
    [HttpGet("articles/{articleId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShopArticleDto>> Article(int articleId, CancellationToken ct = default)
    {
        var supervisorId = User.AdultId()!.Value;
        var article = await db.ShopArticles.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == articleId && a.AdultId == supervisorId, ct);
        if (article is null) return NotFound();
        return MapArticle(article);
    }

    /// <summary>Creates an article in the family shop (type definition without price/stock).</summary>
    [HttpPost("articles")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ShopArticleDto>> CreateArticle(CreateShopArticleDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.ArticleNumber))
            return this.ProblemWithCode(ApiErrors.ValidationError, "Article number is required.");
        if (string.IsNullOrWhiteSpace(dto.Title))
            return this.ProblemWithCode(ApiErrors.ValidationError, "Title is required.");

        var supervisorId = User.AdultId()!.Value;
        var articleNumber = dto.ArticleNumber.Trim();
        if (await db.ShopArticles.AnyAsync(a => a.AdultId == supervisorId && a.ArticleNumber == articleNumber, ct))
            return this.ProblemWithCode(ApiErrors.DuplicateKey, "Article number already exists in this family shop.");

        var article = new ShopArticle
        {
            AdultId = supervisorId,
            ArticleNumber = articleNumber,
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim() ?? "",
            UnitType = dto.UnitType,
            ActionType = dto.ActionType,
        };
        db.ShopArticles.Add(article);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Article), new { articleId = article.Id }, MapArticle(article));
    }

    /// <summary>Partially changes a family shop article.</summary>
    [HttpPatch("articles/{articleId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ShopArticleDto>> UpdateArticle(
        int articleId, UpdateShopArticleDto dto, CancellationToken ct = default)
    {
        var supervisorId = User.AdultId()!.Value;
        var article = await db.ShopArticles.FirstOrDefaultAsync(a => a.Id == articleId && a.AdultId == supervisorId, ct);
        if (article is null) return NotFound();

        var nextNumber = dto.ArticleNumber?.Trim() ?? article.ArticleNumber;
        if (dto.ArticleNumber is not null && string.IsNullOrWhiteSpace(dto.ArticleNumber))
            return this.ProblemWithCode(ApiErrors.ValidationError, "Article number must not be empty.");
        if (dto.Title is not null && string.IsNullOrWhiteSpace(dto.Title))
            return this.ProblemWithCode(ApiErrors.ValidationError, "Title must not be empty.");

        if (nextNumber != article.ArticleNumber
            && await db.ShopArticles.AnyAsync(a => a.AdultId == supervisorId && a.ArticleNumber == nextNumber && a.Id != articleId, ct))
            return this.ProblemWithCode(ApiErrors.DuplicateKey, "Article number already exists in this family shop.");

        article.ArticleNumber = nextNumber;
        if (dto.Title is not null) article.Title = dto.Title.Trim();
        if (dto.Description is not null) article.Description = dto.Description.Trim();
        if (dto.UnitType is not null) article.UnitType = dto.UnitType.Value;
        if (dto.ActionType is not null) article.ActionType = dto.ActionType.Value;
        await db.SaveChangesAsync(ct);
        return MapArticle(article);
    }

    /// <summary>Deletes an article together with all its listings. Purchase history is kept as a snapshot.</summary>
    [HttpDelete("articles/{articleId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteArticle(int articleId, CancellationToken ct = default)
    {
        var supervisorId = User.AdultId()!.Value;
        var article = await db.ShopArticles.FirstOrDefaultAsync(a => a.Id == articleId && a.AdultId == supervisorId, ct);
        if (article is null) return NotFound();
        db.ShopArticles.Remove(article);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ─── Listing CRUD (listings per article) ─────────────────────────────────

    /// <summary>All listings for an article of the father.</summary>
    [HttpGet("articles/{articleId:int}/listings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ShopListingDto>>> Listings(int articleId, CancellationToken ct = default)
    {
        var supervisorId = User.AdultId()!.Value;
        var article = await db.ShopArticles.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == articleId && a.AdultId == supervisorId, ct);
        if (article is null) return NotFound();

        var listings = await shop.ListingsForSupervisorAsync(supervisorId, activeOnly: false, time.GetUtcNow().UtcDateTime, ct);
        return listings.Where(v => v.Listing.ShopArticleId == articleId).Select(MapListing).ToList();
    }

    /// <summary>Read a single listing of an article.</summary>
    [HttpGet("articles/{articleId:int}/listings/{listingId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShopListingDto>> Listing(int articleId, int listingId, CancellationToken ct = default)
    {
        // Display only: the service computes every child's stock without saving - a GET has no write side effect.
        var view = await shop.ListingForSupervisorAsync(User.AdultId()!.Value, articleId, listingId, time.GetUtcNow().UtcDateTime, ct);
        if (view is null) return NotFound();
        return MapListing(view);
    }

    /// <summary>
    /// Creates a new listing for an article (price, quantity, refill plan, audience). Every child it is meant for starts
    /// with its own stock: <c>MaxStock</c> minus its own purchases of the current period.
    /// </summary>
    [HttpPost("articles/{articleId:int}/listings")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShopListingDto>> CreateListing(
        int articleId, CreateShopListingDto dto, CancellationToken ct = default)
    {
        var supervisorId = User.AdultId()!.Value;
        var article = await db.ShopArticles.FirstOrDefaultAsync(a => a.Id == articleId && a.AdultId == supervisorId, ct);
        if (article is null) return NotFound();

        var refillQuantity = dto.RefillQuantity ?? dto.MaxStock;
        var validation = ValidateListing(dto.CoinPrice, dto.GemPrice, dto.UnitsPerPurchase,
            dto.MaxStock, refillQuantity, dto.RefillKind, dto.RefillAtUtc, dto.RefillDayOfWeek);
        if (validation is not null) return validation;
        var (audience, audienceError) = await ResolveAudienceAsync(supervisorId, dto.ChildIds, ct);
        if (audienceError is not null) return audienceError;

        var listing = new ShopListing
        {
            ShopArticleId = articleId,
            Title = dto.Title?.Trim() ?? "",
            Description = dto.Description?.Trim() ?? "",
            CoinPrice = dto.CoinPrice,
            GemPrice = dto.GemPrice,
            UnitsPerPurchase = dto.UnitsPerPurchase,
            MaxStock = dto.MaxStock,
            RefillQuantity = refillQuantity,
            RefillKind = dto.RefillKind,
            RefillAtUtc = dto.RefillAtUtc,
            RefillDayOfWeek = dto.RefillDayOfWeek,
        };
        foreach (var childId in audience ?? []) listing.Audience.Add(new ShopListingAudience { ChildId = childId });
        db.ShopListings.Add(listing);
        await db.SaveChangesAsync(ct);
        listing.ShopArticle = article;
        var view = await shop.ToViewAsync(supervisorId, listing, time.GetUtcNow().UtcDateTime, ct);
        return CreatedAtAction(nameof(Listing), new { articleId, listingId = listing.Id }, MapListing(view));
    }

    /// <summary>
    /// Partially changes a listing (price, quantity, refill plan, audience, active status). A child's stock is set
    /// through <c>…/stock/{childId}</c>. Changing only <c>MaxStock</c> of a listing whose leftovers expire
    /// (<c>RefillQuantity = MaxStock</c>) keeps it that way.
    /// </summary>
    [HttpPatch("articles/{articleId:int}/listings/{listingId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ShopListingDto>> UpdateListing(
        int articleId, int listingId, UpdateShopListingDto dto, CancellationToken ct = default)
    {
        var supervisorId = User.AdultId()!.Value;
        var listing = await shop.LoadListingForUpdateAsync(supervisorId, articleId, listingId, ct);
        if (listing is null) return NotFound();

        var nextCoin = dto.CoinPrice ?? listing.CoinPrice;
        var nextGem = dto.GemPrice ?? listing.GemPrice;
        var nextUnits = dto.UnitsPerPurchase ?? listing.UnitsPerPurchase;
        var nextMaxStock = dto.MaxStock ?? listing.MaxStock;
        // An omitted quantity keeps the listing's mode: "expires" (quantity = MaxStock) follows a new MaxStock, a saving
        // quantity stays as it is - clamping it to a lower cap would silently make it "expires", so a cap below the
        // quantity is refused by the validation instead.
        var nextQuantity = dto.RefillQuantity
            ?? (listing.RefillQuantity == listing.MaxStock ? nextMaxStock : listing.RefillQuantity);
        var nextRefillKind = dto.RefillKind ?? listing.RefillKind;
        var nextRefillAt = dto.RefillAtUtc ?? listing.RefillAtUtc;
        var nextRefillDay = dto.RefillDayOfWeek ?? listing.RefillDayOfWeek;

        // B-776: a switch to "once" fires on the date it carries; a date left over from an earlier one-off would fire at once.
        if (nextRefillKind == ShopRefillKind.Once && listing.RefillKind != ShopRefillKind.Once && dto.RefillAtUtc is null)
            return this.ProblemWithCode(ApiErrors.ValidationError, "RefillAtUtc is required when switching to a one-time refill.");
        var validation = ValidateListing(nextCoin, nextGem, nextUnits,
            nextMaxStock, nextQuantity, nextRefillKind, nextRefillAt, nextRefillDay);
        if (validation is not null) return validation;
        var (audience, audienceError) = await ResolveAudienceAsync(supervisorId, dto.ChildIds, ct);
        if (audienceError is not null) return audienceError;

        var now = time.GetUtcNow().UtcDateTime;
        // B-771: a switch to a recurring kind - or a new weekday of a weekly one - restarts every child's refill clock, after
        // booking what the old rule still owes while the listing carries it. Otherwise the new calendar counts an instant
        // that lies before the switch and refills at once (a Monday stock moved to Friday on a Saturday would refill twice in
        // one week).
        var restartsRefillClock = ShopRefillSchedule.IsRecurring(nextRefillKind)
            && (nextRefillKind != listing.RefillKind || nextRefillDay != listing.RefillDayOfWeek);
        // B-776: "once" fires only on an empty instant, so a switch to it - or a new date - empties every stock's instant;
        // a stock born under a recurring kind or a fired date would otherwise never fill. An unchanged date empties nothing,
        // or each later price change would hand the stock out again.
        var resetsOnceInstant = nextRefillKind == ShopRefillKind.Once
            && (listing.RefillKind != ShopRefillKind.Once || nextRefillAt != listing.RefillAtUtc);
        if (restartsRefillClock || resetsOnceInstant)
            foreach (var stock in listing.Stocks) shop.ApplyDueRefill(listing, stock, now);

        if (dto.Title is not null) listing.Title = dto.Title.Trim();
        if (dto.Description is not null) listing.Description = dto.Description.Trim();
        listing.CoinPrice = nextCoin;
        listing.GemPrice = nextGem;
        listing.UnitsPerPurchase = nextUnits;
        listing.MaxStock = nextMaxStock;
        listing.RefillQuantity = nextQuantity;
        listing.RefillKind = nextRefillKind;
        listing.RefillAtUtc = nextRefillAt;
        listing.RefillDayOfWeek = nextRefillDay;
        if (dto.Active is not null) listing.Active = dto.Active.Value;
        if (audience is not null)
        {
            // Diff instead of clear-and-add: the join row's key is (listing, child), and re-adding a removed key in the
            // same context collides. A child taken out keeps its stock row and its purchases; it just no longer sees it.
            listing.Audience.RemoveAll(a => !audience.Contains(a.ChildId));
            foreach (var childId in audience.Where(id => listing.Audience.All(a => a.ChildId != id)))
                listing.Audience.Add(new ShopListingAudience { ChildId = childId });
        }
        if (restartsRefillClock) ShopService.RestartRefillClock(listing, now);
        if (resetsOnceInstant) ShopService.ClearRefillInstant(listing);
        listing.ConcurrencyStamp = Guid.NewGuid();
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A purchase or a refill of the same stock rows (or another PATCH) committed first; the whole PATCH rolled back.
            return this.ProblemWithCode(ApiErrors.ConcurrencyConflict, "The listing changed meanwhile - reload it and try again.");
        }
        return MapListing(await shop.ToViewAsync(supervisorId, listing, now, ct));
    }

    /// <summary>
    /// Sets one child's own stock of a listing - the supervisor's manual "fill up" (B-645). The value lies in
    /// <c>0..MaxStock</c>; the child must be one the listing is meant for.
    /// </summary>
    [HttpPut("articles/{articleId:int}/listings/{listingId:int}/stock/{childId:int}")]
    [ServiceFilter(typeof(ChildOwnershipFilter))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ShopListingDto>> SetStock(
        int articleId, int listingId, int childId, SetShopListingStockRequest dto, CancellationToken ct = default)
    {
        var result = await shop.SetStockAsync(User.AdultId()!.Value, articleId, listingId, childId, dto.CurrentStock,
            time.GetUtcNow().UtcDateTime, ct);
        return result.Error switch
        {
            ShopService.ShopError.None => MapListing(result.Value!),
            ShopService.ShopError.NotFound => NotFound(),
            ShopService.ShopError.InvalidQuantity => this.ProblemWithCode(ApiErrors.ValidationError,
                "The stock must not be negative and not exceed the listing's MaxStock."),
            ShopService.ShopError.NotInAudience => this.ProblemWithCode(ApiErrors.ValidationError,
                "This listing is not meant for this child; add the child to the listing's ChildIds first."),
            _ => this.ProblemWithCode(ShopService.ToApiError(result.Error), "The stock changed concurrently — please try again."),
        };
    }

    /// <summary>Deletes a listing. Purchases already made are kept as a snapshot in the inventory.</summary>
    [HttpDelete("articles/{articleId:int}/listings/{listingId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteListing(int articleId, int listingId, CancellationToken ct = default)
    {
        var supervisorId = User.AdultId()!.Value;
        var listing = await db.ShopListings
            .Include(l => l.ShopArticle)
            .FirstOrDefaultAsync(l => l.Id == listingId && l.ShopArticleId == articleId
                && l.ShopArticle!.AdultId == supervisorId, ct);
        if (listing is null) return NotFound();
        db.ShopListings.Remove(listing);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ─── Child inventory ─────────────────────────────────────────────────────

    /// <summary>Aggregated inventory of a child: the total available quantity per article type.</summary>
    /// <param name="childId">Id of the child.</param>
    /// <param name="skip">Number of entries skipped (offset, default 0).</param>
    /// <param name="take">Maximum number of entries returned (default 100, max 500).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("~/" + ApiRoutes.Supervisor + "/children/{childId:int}/shop/inventory")]
    [ServiceFilter(typeof(ChildOwnershipFilter))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<InventoryItemDto>>> ChildInventory(int childId,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake,
        CancellationToken ct = default)
    {
        var fid = User.AdultId();
        // Filtering and projecting go through the snapshot, not through the navigation: the article may be
        // deleted (FK SetNull), the paid stock stays. Through `ShopArticle.AdultId` the position would then
        // drop out of this list - and invisible is as good as deleted for the supervisor.
        var query = db.ChildInventories.AsNoTracking()
            .Where(i => i.ChildId == childId && i.Quantity > 0 && i.SupervisorId == fid) // own articles only
            .OrderBy(i => i.ArticleNumber)
            .Select(i => new InventoryItemDto(
                i.ShopArticleId, i.ArticleNumber, i.ArticleTitle,
                i.UnitType, i.ActionType, i.Quantity));
        return await query.ToPagedListAsync(Response, skip, take, ct);
    }

    // ─── Purchase history ────────────────────────────────────────────────────

    /// <summary>Purchase history of a child, optionally filtered by status.</summary>
    [HttpGet("~/" + ApiRoutes.Supervisor + "/children/{childId:int}/shop/purchases")]
    [ServiceFilter(typeof(ChildOwnershipFilter))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ShopPurchaseDto>>> ChildPurchases(int childId,
        [FromQuery] ShopPurchaseStatus? status,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake,
        CancellationToken ct = default)
    {
        var fid = User.AdultId();
        var query = db.ShopPurchases.AsNoTracking().Where(p => p.ChildId == childId && p.SupervisorId == fid);
        if (status is not null) query = query.Where(p => p.Status == status);

        // The ordering must not be able to MOVE a row (B-110/B-113): offset paging is only sound over a
        // stable sequence, and grouping cancelled purchases last did exactly that - approving/cancelling
        // between two page requests shifted rows and skipped one for good. Purchase time is immutable;
        // the client marks cancellations with a pill instead of a position.
        return await query
            .OrderByDescending(p => p.PurchasedAt).ThenByDescending(p => p.Id)
            .Select(p => MapPurchase(p))
            .ToPagedListAsync(Response, skip, take, ct);
    }

    /// <summary>Cancels an open purchase and refunds coins/gems.</summary>
    [HttpPost("~/" + ApiRoutes.Supervisor + "/children/{childId:int}/shop/purchases/{purchaseId:int}/cancel")]
    [ServiceFilter(typeof(ChildOwnershipFilter))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ShopPurchaseDto>> CancelPurchase(
        int childId, int purchaseId, CancellationToken ct = default)
    {
        var result = await shop.CancelPurchaseAsync(User.AdultId()!.Value, childId, purchaseId, time.GetUtcNow().UtcDateTime, ct);
        return result.Error switch
        {
            ShopService.ShopError.None => MapPurchase(result.Value!),
            ShopService.ShopError.NotFound => NotFound(),
            ShopService.ShopError.NotOpen => this.ProblemWithCode(ShopService.ToApiError(result.Error), "This shop purchase is no longer open."),
            _ => this.ProblemWithCode(ShopService.ToApiError(result.Error), "The shop operation could not be completed."),
        };
    }

    /// <summary>
    /// Redeems units straight from the child's inventory (B-665) - the exception for a child without its phone. The
    /// result is an approved request marked <c>RequestedBy = Supervisor</c>; the inventory drops at once.
    /// </summary>
    [HttpPost("~/" + ApiRoutes.Supervisor + "/children/{childId:int}/shop/inventory/{articleId:int}/redeem")]
    [ServiceFilter(typeof(ChildOwnershipFilter))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ActivationRequestDto>> RedeemFromInventory(
        int childId, int articleId, RedeemInventoryRequest dto, CancellationToken ct = default)
    {
        var result = await shop.RedeemForChildAsync(User.AdultId()!.Value, childId, articleId, dto.Quantity,
            time.GetUtcNow().UtcDateTime, ct);
        return result.Error switch
        {
            ShopService.ShopError.None => MapActivation(result.Value!),
            ShopService.ShopError.NotFound => NotFound(),
            ShopService.ShopError.InvalidQuantity => this.ProblemWithCode(ShopService.ToApiError(result.Error), "Quantity must be at least 1."),
            ShopService.ShopError.InsufficientInventory => this.ProblemWithCode(ShopService.ToApiError(result.Error),
                "Not enough units in the child's inventory to redeem this quantity."),
            _ => this.ProblemWithCode(ShopService.ToApiError(result.Error), "The inventory changed concurrently — please try again."),
        };
    }

    // ─── Activation requests ─────────────────────────────────────────────────

    /// <summary>Activation requests of a child, optionally filtered by status.</summary>
    [HttpGet("~/" + ApiRoutes.Supervisor + "/children/{childId:int}/shop/activations")]
    [ServiceFilter(typeof(ChildOwnershipFilter))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ActivationRequestDto>>> ChildActivations(int childId,
        [FromQuery] ActivationRequestStatus? status,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake,
        CancellationToken ct = default)
    {
        var fid = User.AdultId();
        var query = db.ActivationRequests.AsNoTracking().Where(r => r.ChildId == childId && r.SupervisorId == fid);
        if (status is not null) query = query.Where(r => r.Status == status);

        // Same rule as the purchase history above (B-113): the order must not move under approve/reject,
        // and two requests within the same second need a tiebreaker or repeated page reads could disagree.
        return await query
            .OrderByDescending(r => r.RequestedAt).ThenByDescending(r => r.Id)
            .Select(r => MapActivation(r))
            .ToPagedListAsync(Response, skip, take, ct);
    }

    /// <summary>
    /// Activation requests of all of the father's children in one call, optionally filtered by status
    /// (e.g. <c>Pending</c> for the approval list); <c>X-Total-Count</c> carries the filtered total.
    /// </summary>
    [HttpGet("activations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ActivationRequestDto>>> AllActivations(
        [FromQuery] ActivationRequestStatus? status,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake,
        CancellationToken ct = default)
    {
        var fid = User.AdultId();
        // No child id in the path, so no ChildOwnershipFilter: the request must be issued by this father
        // (redemption is issuer-bound) AND the child must still be linked to him – an unlinked child's
        // old requests are not his to decide anymore.
        var query = db.ActivationRequests.AsNoTracking()
            .Where(r => r.SupervisorId == fid
                && db.SupervisorLinks.Any(l => l.SupervisorId == fid && l.StudentId == r.ChildId));
        if (status is not null) query = query.Where(r => r.Status == status);

        // Same stable order as the per-child list (B-113).
        return await query
            .OrderByDescending(r => r.RequestedAt).ThenByDescending(r => r.Id)
            .Select(r => MapActivation(r))
            .ToPagedListAsync(Response, skip, take, ct);
    }

    /// <summary>Approves an open activation request; the child's inventory is reduced.</summary>
    [HttpPost("~/" + ApiRoutes.Supervisor + "/children/{childId:int}/shop/activations/{requestId:int}/approve")]
    [ServiceFilter(typeof(ChildOwnershipFilter))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ActivationRequestDto>> ApproveActivation(
        int childId, int requestId, CancellationToken ct = default)
    {
        var result = await shop.ApproveActivationAsync(User.AdultId()!.Value, childId, requestId, time.GetUtcNow().UtcDateTime, ct);
        return ActivationResult(result);
    }

    /// <summary>Rejects an open activation request; the child's inventory stays unchanged.</summary>
    [HttpPost("~/" + ApiRoutes.Supervisor + "/children/{childId:int}/shop/activations/{requestId:int}/reject")]
    [ServiceFilter(typeof(ChildOwnershipFilter))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ActivationRequestDto>> RejectActivation(
        int childId, int requestId, CancellationToken ct = default)
    {
        var result = await shop.RejectActivationAsync(User.AdultId()!.Value, childId, requestId, time.GetUtcNow().UtcDateTime, ct);
        return ActivationResult(result);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

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
        int maxStock, int refillQuantity, ShopRefillKind refillKind,
        DateTime? refillAtUtc, DayOfWeek? refillDayOfWeek)
    {
        if (coinPrice < 0 || gemPrice < 0) return this.ProblemWithCode(ApiErrors.ValidationError, "Prices must not be negative.");
        if (coinPrice == 0 && gemPrice == 0) return this.ProblemWithCode(ApiErrors.ValidationError, "At least one price must be positive.");
        if (unitsPerPurchase <= 0) return this.ProblemWithCode(ApiErrors.ValidationError, "UnitsPerPurchase must be at least 1.");
        if (maxStock < 0) return this.ProblemWithCode(ApiErrors.ValidationError, "MaxStock must not be negative.");
        if (refillQuantity < 0) return this.ProblemWithCode(ApiErrors.ValidationError, "RefillQuantity must not be negative.");
        // A refilling listing needs a cap to refill up to and a step within it; without refill both are unused.
        if (refillKind != ShopRefillKind.None && maxStock < 1)
            return this.ProblemWithCode(ApiErrors.ValidationError, "MaxStock must be at least 1 for a listing that refills.");
        if (refillKind != ShopRefillKind.None && (refillQuantity < 1 || refillQuantity > maxStock))
            return this.ProblemWithCode(ApiErrors.ValidationError, "RefillQuantity must lie between 1 and MaxStock.");
        if (refillKind == ShopRefillKind.Once && refillAtUtc is null)
            return this.ProblemWithCode(ApiErrors.ValidationError, "RefillAtUtc is required for one-time refills.");
        if (refillKind == ShopRefillKind.Weekly && refillDayOfWeek is null)
            return this.ProblemWithCode(ApiErrors.ValidationError, "RefillDayOfWeek is required for weekly refills.");
        return null;
    }

    // The audience of a listing: null = unchanged/everyone, otherwise distinct ids that must all be children of the
    // caller. A foreign child id is refused rather than ignored - a silently dropped id would hide a typo.
    private async Task<(List<int>? Ids, ActionResult? Error)> ResolveAudienceAsync(
        int supervisorId, IReadOnlyList<int>? childIds, CancellationToken ct)
    {
        if (childIds is null) return (null, null);
        var linked = await shop.LinkedChildIdsAsync(supervisorId, ct);
        var ids = childIds.Distinct().ToList();
        if (ids.Any(id => !linked.Contains(id)))
            return (null, this.ProblemWithCode(ApiErrors.ValidationError, "ChildIds may only name children you supervise."));
        return (ids, null);
    }
}
