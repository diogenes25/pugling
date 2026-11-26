using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// Business logic of the family shop: the adult manages the article catalog and listings, the child buys
/// from their own wallet (coins/gems debited immediately), stock is reduced and the child's aggregated
/// inventory increased. The adult approves or rejects the child's activation requests.
/// </summary>
public class ShopService(PuglingDbContext db, WalletService wallet, LearningDay learningDay)
{
    /// <summary>Cause of failure for a shop operation (None = successful).</summary>
    public enum ShopError
    {
        /// <summary>No error – the operation was successful.</summary>
        None = 0,
        /// <summary>The referenced object (article, listing, request, …) does not exist.</summary>
        NotFound,
        /// <summary>The listing is deactivated and must not be purchased.</summary>
        ListingInactive,
        /// <summary>The listing's stock is not sufficient for the requested quantity.</summary>
        InsufficientStock,
        /// <summary>The child's wallet does not have enough coins for the purchase.</summary>
        InsufficientCoins,
        /// <summary>The child's wallet does not have enough gems for the purchase.</summary>
        InsufficientGems,
        /// <summary>The child's inventory does not contain enough units of the article.</summary>
        InsufficientInventory,
        /// <summary>The requested quantity is not positive or otherwise invalid.</summary>
        InvalidQuantity,
        /// <summary>The listing is not meant for this child (it lies outside the listing's audience).</summary>
        NotInAudience,
        /// <summary>The purchase is not currently possible (e.g. listing outside its time slot).</summary>
        NotOpen,
        /// <summary>The activation request is no longer open (already approved/rejected).</summary>
        NotPending,
        /// <summary>A concurrent write has changed the expected data state (concurrency).</summary>
        Conflict,
    }

    /// <summary>Result with an optional payload.</summary>
    public record Result<T>(ShopError Error, T? Value) where T : class
    {
        /// <summary>Creates a successful result with the given payload.</summary>
        public static Result<T> Ok(T value) => new(ShopError.None, value);
        /// <summary>Creates an error result without a payload.</summary>
        public static Result<T> Fail(ShopError error) => new(error, null);
    }

    /// <summary>Canonical mapping <see cref="ShopError"/> → <see cref="ApiError"/>.</summary>
    public static ApiError ToApiError(ShopError error) => error switch
    {
        ShopError.NotFound => ApiErrors.NotFound,
        ShopError.ListingInactive => ApiErrors.ShopListingInactive,
        ShopError.InsufficientStock => ApiErrors.ShopInsufficientStock,
        ShopError.InsufficientCoins => ApiErrors.InsufficientCoins,
        ShopError.InsufficientGems => ApiErrors.InsufficientGems,
        ShopError.InsufficientInventory => ApiErrors.InsufficientInventory,
        ShopError.InvalidQuantity => ApiErrors.ValidationError,
        ShopError.NotInAudience => ApiErrors.ValidationError,
        ShopError.NotOpen => ApiErrors.PurchaseNotOpen,
        ShopError.NotPending => ApiErrors.ActivationNotPending,
        _ => ApiErrors.ConcurrencyConflict,
    };

    /// <summary>One child's own stock of a listing as the views show it.</summary>
    public record StockView(int ChildId, string ChildName, int CurrentStock, DateTime? LastRefilledAtUtc, DateTime? NextRefillAtUtc);

    /// <summary>A listing with its audience and the stock of every child it is meant for (supervisor view).</summary>
    public record ListingView(ShopListing Listing, IReadOnlyList<int> AudienceChildIds, IReadOnlyList<StockView> Stocks);

    /// <summary>A listing from one child's seat: its own stock and the next refill (student view).</summary>
    public record ChildListingView(ShopListing Listing, int CurrentStock, DateTime? NextRefillAtUtc);

    /// <summary>
    /// Loads all listings of the adult with every eligible child's stock, creating missing stocks by the start rule and
    /// applying due refills (B-645). A lost concurrency race re-reads, so the answer is the stored state, not the
    /// unsaved copy of the loser.
    /// </summary>
    public async Task<IReadOnlyList<ListingView>> ListingsForSupervisorAsync(
        int supervisorId, bool activeOnly, DateTime nowUtc, CancellationToken ct)
    {
        var linked = await LinkedChildrenAsync(supervisorId, ct);
        var listings = await SupervisorListingsQuery(supervisorId, activeOnly, tracked: true).ToListAsync(ct);
        if (await MaterializeStocksAsync(listings, l => Eligible(l, linked.Keys), nowUtc, ct) && !await TrySaveAsync(ct))
        {
            db.ChangeTracker.Clear();
            listings = await SupervisorListingsQuery(supervisorId, activeOnly, tracked: false).ToListAsync(ct);
            await MaterializeStocksAsync(listings, l => Eligible(l, linked.Keys), nowUtc, ct);
        }
        return listings.Select(l => ToView(l, linked, nowUtc)).ToList();
    }

    /// <summary>
    /// One listing of the adult with every eligible child's stock - computed for display only, nothing is written: a
    /// GET has no write side effect. The list call or a purchase persists the same refill.
    /// </summary>
    public async Task<ListingView?> ListingForSupervisorAsync(
        int supervisorId, int articleId, int listingId, DateTime nowUtc, CancellationToken ct)
    {
        var listing = await SupervisorListingsQuery(supervisorId, activeOnly: false, tracked: false)
            .FirstOrDefaultAsync(l => l.Id == listingId && l.ShopArticleId == articleId, ct);
        return listing is null ? null : await ToViewAsync(supervisorId, listing, nowUtc, ct);
    }

    /// <summary>
    /// Loads a listing of the adult tracked, with its audience and every stock row - for the master-data writes of the
    /// controller (PATCH, audience), which then answer through <see cref="ToViewAsync"/>.
    /// </summary>
    public Task<ShopListing?> LoadListingForUpdateAsync(int supervisorId, int articleId, int listingId, CancellationToken ct) =>
        SupervisorListingsQuery(supervisorId, activeOnly: false, tracked: true)
            .FirstOrDefaultAsync(l => l.Id == listingId && l.ShopArticleId == articleId, ct);

    /// <summary>The supervisor view of a loaded listing; missing stocks are computed for display, nothing is saved here.</summary>
    public async Task<ListingView> ToViewAsync(int supervisorId, ShopListing listing, DateTime nowUtc, CancellationToken ct)
    {
        var linked = await LinkedChildrenAsync(supervisorId, ct);
        await MaterializeStocksAsync([listing], l => Eligible(l, linked.Keys), nowUtc, ct);
        return ToView(listing, linked, nowUtc);
    }

    /// <summary>The ids of the children linked to the supervisor - the pool an audience may be cut from.</summary>
    public async Task<IReadOnlyCollection<int>> LinkedChildIdsAsync(int supervisorId, CancellationToken ct) =>
        (await LinkedChildrenAsync(supervisorId, ct)).Keys;

    /// <summary>
    /// Sets one child's own stock of a listing (the supervisor's manual "fill up"). <see cref="ShopError.NotFound"/>
    /// for an unknown listing or a child not linked to the issuer, <see cref="ShopError.NotInAudience"/> for a child
    /// outside the audience, <see cref="ShopError.InvalidQuantity"/> for a value outside <c>0..MaxStock</c>.
    /// </summary>
    public async Task<Result<ListingView>> SetStockAsync(
        int supervisorId, int articleId, int listingId, int childId, int currentStock, DateTime nowUtc, CancellationToken ct)
    {
        var linked = await LinkedChildrenAsync(supervisorId, ct);
        if (!linked.ContainsKey(childId)) return Result<ListingView>.Fail(ShopError.NotFound);
        var listing = await LoadListingForUpdateAsync(supervisorId, articleId, listingId, ct);
        if (listing is null) return Result<ListingView>.Fail(ShopError.NotFound);
        if (!Eligible(listing, linked.Keys).Contains(childId)) return Result<ListingView>.Fail(ShopError.NotInAudience);
        if (currentStock < 0 || listing.MaxStock > 0 && currentStock > listing.MaxStock)
            return Result<ListingView>.Fail(ShopError.InvalidQuantity);

        await MaterializeStocksAsync([listing], _ => [childId], nowUtc, ct);
        var row = listing.Stocks.First(s => s.ChildId == childId);
        row.CurrentStock = currentStock;
        row.ConcurrencyStamp = Guid.NewGuid();
        if (!await TrySaveAsync(ct)) return Result<ListingView>.Fail(ShopError.Conflict);
        // B-772: only this child's row is written, so a sibling's purchase cannot turn the father's write into a conflict;
        // the answer still shows every sibling with its due refill and start stock, computed for display only.
        return Result<ListingView>.Ok(await ToViewAsync(supervisorId, listing, nowUtc, ct));
    }

    /// <summary>
    /// Loads the listings of ALL supervisors of the student that are meant for it (audience empty or containing it),
    /// with the child's own stock - created by the start rule when missing, refilled when due. With
    /// <paramref name="persist"/> false the stock is computed for display only and nothing is written: the savings goal
    /// (B-593) reads the shop from a GET a supervisor may call too, and a GET has no write side effect.
    /// </summary>
    public async Task<IReadOnlyList<ChildListingView>> ListingsForStudentAsync(
        int childId, bool activeOnly, bool persist, DateTime nowUtc, CancellationToken ct)
    {
        var listings = await StudentListingsQuery(childId, activeOnly, tracked: persist).ToListAsync(ct);
        if (await MaterializeStocksAsync(listings, _ => [childId], nowUtc, ct) && persist && !await TrySaveAsync(ct))
        {
            // Re-read after a lost race: the unsaved copy would show a refill on top of the winner's (4 + 4 = 8 shown,
            // 4 stored) - B-645, risk 3.
            db.ChangeTracker.Clear();
            listings = await StudentListingsQuery(childId, activeOnly, tracked: false).ToListAsync(ct);
            await MaterializeStocksAsync(listings, _ => [childId], nowUtc, ct);
        }
        return listings.Select(l =>
        {
            var row = l.Stocks.First(s => s.ChildId == childId);
            return new ChildListingView(l, row.CurrentStock, NextRefill(l, row, nowUtc));
        }).ToList();
    }

    /// <summary>
    /// Purchases a listing for the child: checks family membership, active status, stock and both
    /// wallet balances, debits coins/gems, reduces stock, creates the purchase ledger entry and
    /// increases the child's aggregated <see cref="ChildInventory"/> for the associated article.
    /// </summary>
    public async Task<Result<ShopPurchase>> PurchaseAsync(
        int childId, int listingId, DateTime nowUtc, CancellationToken ct = default)
    {
        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == childId, ct);
        if (child is null) return Result<ShopPurchase>.Fail(ShopError.NotFound);

        // The student may buy from the shop of ANY of their supervisors (a shared wallet) - but only what is meant for
        // it: a listing outside its audience is as unknown as a foreign family's.
        var listing = await StudentListingsQuery(childId, activeOnly: false, tracked: true)
            .FirstOrDefaultAsync(l => l.Id == listingId, ct);
        if (listing is null) return Result<ShopPurchase>.Fail(ShopError.NotFound);

        await MaterializeStocksAsync([listing], _ => [childId], nowUtc, ct);
        var stock = listing.Stocks.First(s => s.ChildId == childId);
        if (!listing.Active) return Result<ShopPurchase>.Fail(ShopError.ListingInactive);
        if (stock.CurrentStock < 1) return Result<ShopPurchase>.Fail(ShopError.InsufficientStock);

        var balances = await wallet.BalancesAsync(childId, ct);
        if (balances.Coins < listing.CoinPrice) return Result<ShopPurchase>.Fail(ShopError.InsufficientCoins);
        if (balances.Gems < listing.GemPrice) return Result<ShopPurchase>.Fail(ShopError.InsufficientGems);

        var article = listing.ShopArticle!;

        if (listing.CoinPrice > 0)
            db.ChildPointsEntries.Add(new ChildPointsEntry
            {
                ChildId = childId,
                Amount = -listing.CoinPrice,
                Kind = PointKind.ShopCoins,
                Reason = $"Shop-Angebot gekauft: {article.Title}",
                CreatedAt = nowUtc,
            });
        if (listing.GemPrice > 0)
            db.ChildPointsEntries.Add(new ChildPointsEntry
            {
                ChildId = childId,
                Amount = -listing.GemPrice,
                Kind = PointKind.ShopGems,
                Reason = $"Shop-Angebot gekauft: {article.Title}",
                CreatedAt = nowUtc,
            });

        // The child's own stock row is the lock (B-645): a sibling's purchase touches another row.
        stock.CurrentStock -= 1;
        stock.ConcurrencyStamp = Guid.NewGuid();

        var title = string.IsNullOrWhiteSpace(listing.Title) ? article.Title : listing.Title;
        var purchase = new ShopPurchase
        {
            ChildId = childId,
            ShopListingId = listing.Id,
            SupervisorId = article.AdultId, // record the issuer: only they cancel.
            ArticleNumber = article.ArticleNumber,
            Title = title,
            Description = listing.Description,
            CoinPrice = listing.CoinPrice,
            GemPrice = listing.GemPrice,
            UnitsPerPurchase = listing.UnitsPerPurchase,
            Status = ShopPurchaseStatus.Owned,
            PurchasedAt = nowUtc,
        };
        db.ShopPurchases.Add(purchase);

        // Increase the aggregated inventory (upsert)
        var inventory = await db.ChildInventories
            .FirstOrDefaultAsync(i => i.ChildId == childId && i.ShopArticleId == article.Id, ct);
        if (inventory is null)
            db.ChildInventories.Add(new ChildInventory
            {
                ChildId = childId,
                ShopArticleId = article.Id,
                // A snapshot as on the purchase record: it carries the display and the supervisor filter once
                // the article is deleted (FK SetNull) - paid units are money.
                SupervisorId = article.AdultId,
                ArticleNumber = article.ArticleNumber,
                ArticleTitle = article.Title,
                UnitType = article.UnitType,
                ActionType = article.ActionType,
                Quantity = listing.UnitsPerPurchase,
            });
        else
        {
            inventory.Quantity += listing.UnitsPerPurchase;
            inventory.ConcurrencyStamp = Guid.NewGuid();
        }

        // Balance protection as with listings/skins: the stock stamp serializes the same stock only - the
        // wallet is shared across all buying paths. Bumping the child token makes a second purchase started in
        // parallel (another listing or a skin) fail with a conflict, so the funds check cannot be bypassed twice
        // and the balance cannot go negative.
        child.ConcurrencyStamp = Guid.NewGuid();

        return await TrySaveAsync(ct)
            ? Result<ShopPurchase>.Ok(purchase)
            : Result<ShopPurchase>.Fail(ShopError.Conflict);
    }

    /// <summary>
    /// Cancels an open purchase: refunds coins/gems and reduces the inventory by
    /// <see cref="ShopPurchase.UnitsPerPurchase"/> (at least 0).
    /// </summary>
    public async Task<Result<ShopPurchase>> CancelPurchaseAsync(
        int supervisorId, int childId, int purchaseId, DateTime nowUtc, CancellationToken ct = default)
    {
        var purchase = await LoadOpenPurchaseAsync(supervisorId, childId, purchaseId, ct);
        if (purchase is null) return await MissOrNotOpenAsync(supervisorId, childId, purchaseId, ct);

        purchase.Status = ShopPurchaseStatus.Cancelled;
        purchase.ClosedAt = nowUtc;
        purchase.ConcurrencyStamp = Guid.NewGuid();

        if (purchase.CoinPrice > 0)
            db.ChildPointsEntries.Add(new ChildPointsEntry
            {
                ChildId = childId,
                Amount = purchase.CoinPrice,
                Kind = PointKind.ShopCoins,
                Reason = $"Shop-Kauf storniert (Rückerstattung): {purchase.Title}",
                CreatedAt = nowUtc,
            });
        if (purchase.GemPrice > 0)
            db.ChildPointsEntries.Add(new ChildPointsEntry
            {
                ChildId = childId,
                Amount = purchase.GemPrice,
                Kind = PointKind.ShopGems,
                Reason = $"Shop-Kauf storniert (Rückerstattung): {purchase.Title}",
                CreatedAt = nowUtc,
            });

        // Reduce the inventory by the cancelled quantity (as far as it is still there)
        if (purchase.ShopListingId is not null)
        {
            var listingArticleId = await db.ShopListings.AsNoTracking()
                .Where(l => l.Id == purchase.ShopListingId)
                .Select(l => (int?)l.ShopArticleId)
                .FirstOrDefaultAsync(ct);
            if (listingArticleId is not null)
            {
                var inv = await db.ChildInventories
                    .FirstOrDefaultAsync(i => i.ChildId == childId && i.ShopArticleId == listingArticleId, ct);
                if (inv is not null)
                {
                    inv.Quantity = Math.Max(0, inv.Quantity - purchase.UnitsPerPurchase);
                    inv.ConcurrencyStamp = Guid.NewGuid();
                }
            }
        }

        return await TrySaveAsync(ct)
            ? Result<ShopPurchase>.Ok(purchase)
            : Result<ShopPurchase>.Fail(ShopError.Conflict);
    }

    /// <summary>
    /// Files an activation request from the child: checks whether enough units are in the inventory, and
    /// creates an <see cref="ActivationRequest"/> with status <see cref="ActivationRequestStatus.Pending"/>.
    /// The inventory is only reduced upon approval (<see cref="ApproveActivationAsync"/>).
    /// </summary>
    public async Task<Result<ActivationRequest>> RequestActivationAsync(
        int childId, int articleId, int quantity, DateTime nowUtc, CancellationToken ct = default)
    {
        if (quantity <= 0) return Result<ActivationRequest>.Fail(ShopError.InvalidQuantity);

        var childExists = await db.Children.AsNoTracking().AnyAsync(c => c.Id == childId, ct);
        if (!childExists) return Result<ActivationRequest>.Fail(ShopError.NotFound);

        // Activation is only possible for an article of a supervising supervisor.
        var article = await db.ShopArticles.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == articleId
                && db.SupervisorLinks.Any(sl => sl.StudentId == childId && sl.SupervisorId == a.AdultId), ct);
        if (article is null) return Result<ActivationRequest>.Fail(ShopError.NotFound);

        var inventory = await db.ChildInventories
            .FirstOrDefaultAsync(i => i.ChildId == childId && i.ShopArticleId == articleId, ct);
        if (inventory is null || inventory.Quantity < quantity)
            return Result<ActivationRequest>.Fail(ShopError.InsufficientInventory);

        var request = new ActivationRequest
        {
            ChildId = childId,
            ShopArticleId = articleId,
            SupervisorId = article.AdultId, // record the issuer: only they approve/reject.
            RequestedQuantity = quantity,
            ArticleTitle = article.Title,
            UnitType = article.UnitType,
            ActionType = article.ActionType,
            RequestedAt = nowUtc,
        };
        db.ActivationRequests.Add(request);

        return await TrySaveAsync(ct)
            ? Result<ActivationRequest>.Ok(request)
            : Result<ActivationRequest>.Fail(ShopError.Conflict);
    }

    /// <summary>
    /// The adult approves an open activation request: checks that the inventory actually covers the
    /// requested quantity at approval time (otherwise <see cref="ShopError.InsufficientInventory"/> – the
    /// request stays open), reduces it by <see cref="ActivationRequest.RequestedQuantity"/> and sets
    /// the status to Approved. Since the request check is not transactional (multiple open requests
    /// can together exceed the inventory), this coverage check is the binding boundary. The concurrency
    /// token on the inventory prevents concurrent overdraw.
    /// </summary>
    public async Task<Result<ActivationRequest>> ApproveActivationAsync(
        int supervisorId, int childId, int requestId, DateTime nowUtc, CancellationToken ct = default)
    {
        var request = await LoadPendingActivationAsync(supervisorId, childId, requestId, ct);
        if (request is null) return await MissOrNotPendingAsync(supervisorId, childId, requestId, ct);

        // Only when the article reference is missing (the article was deleted later) is there no inventory to book.
        if (request.ShopArticleId is not null)
        {
            var inv = await db.ChildInventories
                .FirstOrDefaultAsync(i => i.ChildId == childId && i.ShopArticleId == request.ShopArticleId, ct);
            if (inv is null || inv.Quantity < request.RequestedQuantity)
                return Result<ActivationRequest>.Fail(ShopError.InsufficientInventory);

            inv.Quantity -= request.RequestedQuantity;
            inv.ConcurrencyStamp = Guid.NewGuid();
        }

        request.Status = ActivationRequestStatus.Approved;
        request.ClosedAt = nowUtc;

        return await TrySaveAsync(ct)
            ? Result<ActivationRequest>.Ok(request)
            : Result<ActivationRequest>.Fail(ShopError.Conflict);
    }

    /// <summary>
    /// The adult rejects an open activation request: status → Rejected. The inventory remains
    /// unchanged – the units remain with the child.
    /// </summary>
    public async Task<Result<ActivationRequest>> RejectActivationAsync(
        int supervisorId, int childId, int requestId, DateTime nowUtc, CancellationToken ct = default)
    {
        var request = await LoadPendingActivationAsync(supervisorId, childId, requestId, ct);
        if (request is null) return await MissOrNotPendingAsync(supervisorId, childId, requestId, ct);

        request.Status = ActivationRequestStatus.Rejected;
        request.ClosedAt = nowUtc;

        return await TrySaveAsync(ct)
            ? Result<ActivationRequest>.Ok(request)
            : Result<ActivationRequest>.Fail(ShopError.Conflict);
    }

    /// <summary>
    /// The supervisor redeems units straight from the child's inventory (B-665): the checks of a child's request
    /// (positive quantity, the caller's own article, the inventory covers it) and the booking of an approval, in one save
    /// - no pending step in between. The record names the supervisor as requester, so the child's list explains it.
    /// </summary>
    public async Task<Result<ActivationRequest>> RedeemForChildAsync(
        int supervisorId, int childId, int articleId, int quantity, DateTime nowUtc, CancellationToken ct)
    {
        if (quantity <= 0) return Result<ActivationRequest>.Fail(ShopError.InvalidQuantity);

        // Bound to the issuer like an approval: another supervisor's article (Grandma's) is not this one's to redeem.
        var article = await db.ShopArticles.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == articleId && a.AdultId == supervisorId, ct);
        if (article is null) return Result<ActivationRequest>.Fail(ShopError.NotFound);

        var inventory = await db.ChildInventories
            .FirstOrDefaultAsync(i => i.ChildId == childId && i.ShopArticleId == articleId, ct);
        if (inventory is null || inventory.Quantity < quantity)
            return Result<ActivationRequest>.Fail(ShopError.InsufficientInventory);

        inventory.Quantity -= quantity;
        inventory.ConcurrencyStamp = Guid.NewGuid();

        var request = new ActivationRequest
        {
            ChildId = childId,
            ShopArticleId = articleId,
            SupervisorId = supervisorId,
            RequestedQuantity = quantity,
            Status = ActivationRequestStatus.Approved,
            ArticleTitle = article.Title,
            UnitType = article.UnitType,
            ActionType = article.ActionType,
            RequestedAt = nowUtc,
            ClosedAt = nowUtc,
            RequestedBy = ActivationRequester.Supervisor,
        };
        db.ActivationRequests.Add(request);

        return await TrySaveAsync(ct)
            ? Result<ActivationRequest>.Ok(request)
            : Result<ActivationRequest>.Fail(ShopError.Conflict);
    }

    // ─── Stock per child (B-645) ─────────────────────────────────────────────

    // The supervisor's listings with article, audience and every stock row. Tracked for writes, untracked for display.
    private IQueryable<ShopListing> SupervisorListingsQuery(int supervisorId, bool activeOnly, bool tracked)
    {
        var query = db.ShopListings
            .Include(l => l.ShopArticle)
            .Include(l => l.Audience)
            .Include(l => l.Stocks)
            .Where(l => l.ShopArticle!.AdultId == supervisorId);
        if (!tracked) query = query.AsNoTracking();
        if (activeOnly) query = query.Where(l => l.Active);
        return query
            .OrderByDescending(l => l.Active)
            .ThenBy(l => l.ShopArticle!.ArticleNumber)
            .ThenBy(l => l.Id);
    }

    // The listings meant for the child: issued by one of its supervisors, audience empty or containing it. Only the
    // child's own stock row is loaded - the siblings' rows are none of its business and must not be written here.
    private IQueryable<ShopListing> StudentListingsQuery(int childId, bool activeOnly, bool tracked)
    {
        var query = db.ShopListings
            .Include(l => l.ShopArticle)
            .Include(l => l.Stocks.Where(s => s.ChildId == childId))
            .Where(l => db.SupervisorLinks.Any(sl => sl.StudentId == childId && sl.SupervisorId == l.ShopArticle!.AdultId))
            .Where(l => !l.Audience.Any() || l.Audience.Any(a => a.ChildId == childId));
        if (!tracked) query = query.AsNoTracking();
        if (activeOnly) query = query.Where(l => l.Active);
        return query
            .OrderBy(l => l.ShopArticle!.AdultId)
            .ThenByDescending(l => l.Active)
            .ThenBy(l => l.ShopArticle!.ArticleNumber)
            .ThenBy(l => l.Id);
    }

    // Children linked to the supervisor, id → name: the pool every audience is cut from.
    private async Task<Dictionary<int, string>> LinkedChildrenAsync(int supervisorId, CancellationToken ct) =>
        await db.SupervisorLinks.AsNoTracking()
            .Where(sl => sl.SupervisorId == supervisorId)
            .Select(sl => new { sl.StudentId, sl.Student!.Name })
            .ToDictionaryAsync(x => x.StudentId, x => x.Name, ct);

    // Who a listing is for: its audience, or every linked child when the audience is empty. An audience member who is no
    // longer linked drops out - the link, not the join row, decides whose shop it is.
    private static IEnumerable<int> Eligible(ShopListing listing, IReadOnlyCollection<int> linkedChildIds) =>
        listing.Audience.Count == 0
            ? linkedChildIds
            : listing.Audience.Select(a => a.ChildId).Where(linkedChildIds.Contains);

    /// <summary>
    /// The one sink for stock state: gives every (listing, child) pair a stock row - a missing one by the start rule
    /// (<see cref="ShopRefillSchedule.StartStock"/>, decision 11) - and applies the due refills to all of them. Returns
    /// whether anything changed. On tracked listings EF inserts/updates the rows on save; on untracked ones the result is
    /// display only.
    /// </summary>
    private async Task<bool> MaterializeStocksAsync(
        IReadOnlyList<ShopListing> listings, Func<ShopListing, IEnumerable<int>> childrenOf, DateTime nowUtc, CancellationToken ct)
    {
        var missing = listings
            .SelectMany(l => childrenOf(l).Where(c => l.Stocks.All(s => s.ChildId != c)).Select(c => (Listing: l, ChildId: c)))
            .ToList();

        var changed = false;
        if (missing.Count > 0)
        {
            var listingIds = missing.Select(m => m.Listing.Id).Distinct().ToList();
            var childIds = missing.Select(m => m.ChildId).Distinct().ToList();
            var purchases = await db.ShopPurchases.AsNoTracking()
                .Where(p => p.ShopListingId != null && listingIds.Contains(p.ShopListingId.Value)
                    && childIds.Contains(p.ChildId) && p.Status != ShopPurchaseStatus.Cancelled)
                .Select(p => new { ListingId = p.ShopListingId!.Value, p.ChildId, p.PurchasedAt })
                .ToListAsync(ct);

            foreach (var (listing, childId) in missing)
            {
                var periodStart = ShopRefillSchedule.PeriodStartUtc(
                    listing.RefillKind, listing.RefillDayOfWeek, listing.RefillAtUtc, listing.CreatedAt, nowUtc, learningDay.Zone);
                var own = purchases.Count(p => p.ListingId == listing.Id && p.ChildId == childId && p.PurchasedAt >= periodStart);
                var (stock, last) = ShopRefillSchedule.StartStock(listing.RefillKind, listing.RefillAtUtc, listing.MaxStock, own, nowUtc);
                listing.Stocks.Add(new ShopListingStock
                {
                    ShopListingId = listing.Id,
                    ChildId = childId,
                    CurrentStock = stock,
                    LastRefilledAtUtc = last,
                });
                changed = true;
            }
        }

        foreach (var listing in listings)
        {
            var eligible = childrenOf(listing).ToHashSet();
            foreach (var row in listing.Stocks.Where(s => eligible.Contains(s.ChildId)))
                changed |= ApplyDueRefill(listing, row, nowUtc);
        }
        return changed;
    }

    /// <summary>
    /// Applies the due refills to one child's stock: every refill instant since its last refill adds
    /// <see cref="ShopListing.RefillQuantity"/>, capped at <see cref="ShopListing.MaxStock"/> (B-645, decision 4).
    /// </summary>
    public bool ApplyDueRefill(ShopListing listing, ShopListingStock stock, DateTime nowUtc)
    {
        var due = ShopRefillSchedule.DueRefills(
            listing.RefillKind, listing.RefillDayOfWeek, listing.RefillAtUtc, stock.LastRefilledAtUtc, nowUtc, learningDay.Zone);
        if (due <= 0 || listing.MaxStock <= 0) return false;

        // A one-off fills to the cap, whatever the quantity says: a child whose row was created after the instant starts at
        // MaxStock (decision 11), and a sibling whose row existed before it must end up at the same value (AK 10).
        var quantity = listing.RefillKind == ShopRefillKind.Once ? listing.MaxStock : listing.RefillQuantity;
        stock.CurrentStock = ShopRefillSchedule.Refill(stock.CurrentStock, listing.MaxStock, quantity, due);
        stock.LastRefilledAtUtc = nowUtc;
        stock.ConcurrencyStamp = Guid.NewGuid();
        return true;
    }

    /// <summary>
    /// Starts the refill calendar of every existing stock row now (B-771): called when the supervisor switches a listing to
    /// a recurring kind. A row born under "none" or an unfired "once" has no refill instant, and without one the schedule
    /// owes a refill at once - the switch would hand out the next month's pocket money today. The caller books the old
    /// rule's due refills first, so nothing the child already earned is lost.
    /// </summary>
    public static void RestartRefillClock(ShopListing listing, DateTime nowUtc)
    {
        foreach (var stock in listing.Stocks)
        {
            stock.LastRefilledAtUtc = nowUtc;
            stock.ConcurrencyStamp = Guid.NewGuid();
        }
    }

    /// <summary>
    /// Empties the refill instant of every existing stock row (B-776): called when the supervisor switches a listing to
    /// "once" or gives it a new date. A one-off fires only on a row without an instant, so every child then fills on the
    /// date, as a child whose row is created later does. The caller books the old rule's due refills first.
    /// </summary>
    public static void ClearRefillInstant(ShopListing listing)
    {
        // A row without an instant is left alone: bumping its stamp would only widen the window for a 409.
        foreach (var stock in listing.Stocks.Where(s => s.LastRefilledAtUtc is not null))
        {
            stock.LastRefilledAtUtc = null;
            stock.ConcurrencyStamp = Guid.NewGuid();
        }
    }

    private DateTime? NextRefill(ShopListing listing, ShopListingStock stock, DateTime nowUtc) =>
        ShopRefillSchedule.NextRefillAtUtc(
            listing.RefillKind, listing.RefillDayOfWeek, listing.RefillAtUtc, stock.LastRefilledAtUtc, nowUtc, learningDay.Zone);

    private ListingView ToView(ShopListing listing, IReadOnlyDictionary<int, string> linked, DateTime nowUtc)
    {
        var eligible = Eligible(listing, linked.Keys.ToList()).ToHashSet();
        var stocks = listing.Stocks
            .Where(s => eligible.Contains(s.ChildId))
            .OrderBy(s => s.ChildId)
            .Select(s => new StockView(s.ChildId, linked[s.ChildId], s.CurrentStock, s.LastRefilledAtUtc, NextRefill(listing, s, nowUtc)))
            .ToList();
        var audience = listing.Audience.Select(a => a.ChildId).OrderBy(id => id).ToList();
        return new ListingView(listing, audience, stocks);
    }

    // Bound to the issuer: only the supervisor who issued the article/listing (SupervisorId snapshot) sees or
    // edits the purchase or the request. A case issued by someone else appears as NotFound.
    private Task<ShopPurchase?> LoadOpenPurchaseAsync(int supervisorId, int childId, int purchaseId, CancellationToken ct) =>
        db.ShopPurchases.FirstOrDefaultAsync(
            p => p.Id == purchaseId && p.ChildId == childId && p.SupervisorId == supervisorId && p.Status == ShopPurchaseStatus.Owned, ct);

    private Task<ActivationRequest?> LoadPendingActivationAsync(int supervisorId, int childId, int requestId, CancellationToken ct) =>
        db.ActivationRequests.FirstOrDefaultAsync(
            r => r.Id == requestId && r.ChildId == childId && r.SupervisorId == supervisorId && r.Status == ActivationRequestStatus.Pending, ct);

    private async Task<Result<ShopPurchase>> MissOrNotOpenAsync(int supervisorId, int childId, int purchaseId, CancellationToken ct)
    {
        var exists = await db.ShopPurchases.AnyAsync(p => p.Id == purchaseId && p.ChildId == childId && p.SupervisorId == supervisorId, ct);
        return Result<ShopPurchase>.Fail(exists ? ShopError.NotOpen : ShopError.NotFound);
    }

    private async Task<Result<ActivationRequest>> MissOrNotPendingAsync(int supervisorId, int childId, int requestId, CancellationToken ct)
    {
        var exists = await db.ActivationRequests.AnyAsync(r => r.Id == requestId && r.ChildId == childId && r.SupervisorId == supervisorId, ct);
        return Result<ActivationRequest>.Fail(exists ? ShopError.NotPending : ShopError.NotFound);
    }

    private async Task<bool> TrySaveAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
        catch (DbUpdateException e) when (e.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteUniqueViolation })
        {
            // Two first reads of the same child's stock raced to create its row; the unique index let one of them win.
            return false;
        }
    }

    // SQLITE_CONSTRAINT_UNIQUE (extended result code).
    private const int SqliteUniqueViolation = 2067;
}
