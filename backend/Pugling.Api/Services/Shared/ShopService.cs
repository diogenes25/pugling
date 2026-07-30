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
public class ShopService(PuglingDbContext db, WalletService wallet)
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
        ShopError.NotOpen => ApiErrors.PurchaseNotOpen,
        ShopError.NotPending => ApiErrors.ActivationNotPending,
        _ => ApiErrors.ConcurrencyConflict,
    };

    /// <summary>
    /// Loads all listings (<see cref="ShopListing"/>s) of the adult incl. their article and applies
    /// due refill rules idempotently.
    /// </summary>
    public async Task<IReadOnlyList<ShopListing>> ListingsForFatherAsync(
        int fatherId, bool activeOnly, DateTime nowUtc, CancellationToken ct = default)
    {
        var query = db.ShopListings
            .Include(l => l.ShopArticle)
            .Where(l => l.ShopArticle!.AdultId == fatherId);
        if (activeOnly) query = query.Where(l => l.Active);

        var listings = await query
            .OrderByDescending(l => l.Active)
            .ThenBy(l => l.ShopArticle!.ArticleNumber)
            .ThenBy(l => l.Id)
            .ToListAsync(ct);

        var changed = false;
        foreach (var listing in listings) changed |= ApplyDueRefill(listing, nowUtc);
        if (!changed) return listings;

        if (await TrySaveAsync(ct)) return listings;

        db.ChangeTracker.Clear();
        var fresh = db.ShopListings.AsNoTracking()
            .Include(l => l.ShopArticle)
            .Where(l => l.ShopArticle!.AdultId == fatherId);
        if (activeOnly) fresh = fresh.Where(l => l.Active);
        return await fresh
            .OrderByDescending(l => l.Active)
            .ThenBy(l => l.ShopArticle!.ArticleNumber)
            .ThenBy(l => l.Id)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Loads the listings of ALL supervisors of the student (the child's shared shop view), applies due
    /// refill rules idempotently and returns them groupable per issuer (<see cref="ShopArticle.AdultId"/>).
    /// </summary>
    public async Task<IReadOnlyList<ShopListing>> ListingsForStudentAsync(
        int childId, bool activeOnly, DateTime nowUtc, CancellationToken ct = default)
    {
        var query = db.ShopListings
            .Include(l => l.ShopArticle)
            .Where(l => db.SupervisorLinks.Any(sl => sl.StudentId == childId && sl.SupervisorId == l.ShopArticle!.AdultId));
        if (activeOnly) query = query.Where(l => l.Active);

        var listings = await query
            .OrderBy(l => l.ShopArticle!.AdultId)
            .ThenByDescending(l => l.Active)
            .ThenBy(l => l.ShopArticle!.ArticleNumber)
            .ThenBy(l => l.Id)
            .ToListAsync(ct);

        var changed = false;
        foreach (var listing in listings) changed |= ApplyDueRefill(listing, nowUtc);
        if (changed && !await TrySaveAsync(ct)) db.ChangeTracker.Clear();
        return listings;
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

        // Der Student darf aus dem Shop JEDES seiner Supervisor kaufen (gemeinsames Wallet).
        var listing = await db.ShopListings
            .Include(l => l.ShopArticle)
            .FirstOrDefaultAsync(l => l.Id == listingId
                && db.SupervisorLinks.Any(sl => sl.StudentId == childId && sl.SupervisorId == l.ShopArticle!.AdultId), ct);
        if (listing is null) return Result<ShopPurchase>.Fail(ShopError.NotFound);

        ApplyDueRefill(listing, nowUtc);
        if (!listing.Active) return Result<ShopPurchase>.Fail(ShopError.ListingInactive);
        if (listing.CurrentStock < 1) return Result<ShopPurchase>.Fail(ShopError.InsufficientStock);

        var balances = await wallet.BalancesAsync(childId, ct);
        if (balances.Coins < listing.CoinPrice) return Result<ShopPurchase>.Fail(ShopError.InsufficientCoins);
        if (balances.Gems < listing.GemPrice) return Result<ShopPurchase>.Fail(ShopError.InsufficientGems);

        var article = listing.ShopArticle!;

        if (listing.CoinPrice > 0)
            db.ChildPoints.Add(new ChildPointsEntry
            {
                ChildId = childId,
                Amount = -listing.CoinPrice,
                Kind = PointKind.ShopCoins,
                Reason = $"Shop-Angebot gekauft: {article.Title}",
                CreatedAt = nowUtc,
            });
        if (listing.GemPrice > 0)
            db.ChildPoints.Add(new ChildPointsEntry
            {
                ChildId = childId,
                Amount = -listing.GemPrice,
                Kind = PointKind.ShopGems,
                Reason = $"Shop-Angebot gekauft: {article.Title}",
                CreatedAt = nowUtc,
            });

        listing.CurrentStock -= 1;
        listing.ConcurrencyStamp = Guid.NewGuid();

        var title = string.IsNullOrWhiteSpace(listing.Title) ? article.Title : listing.Title;
        var purchase = new ShopPurchase
        {
            ChildId = childId,
            ShopListingId = listing.Id,
            SupervisorId = article.AdultId, // Aussteller festhalten: nur er storniert.
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

        // Aggregiertes Inventar erhöhen (Upsert)
        var inventory = await db.ChildInventories
            .FirstOrDefaultAsync(i => i.ChildId == childId && i.ShopArticleId == article.Id, ct);
        if (inventory is null)
            db.ChildInventories.Add(new ChildInventory
            {
                ChildId = childId,
                ShopArticleId = article.Id,
                // Momentaufnahme wie am Kaufbeleg: sie trägt Anzeige und Vater-Filter, nachdem der
                // Artikel gelöscht ist (FK SetNull) – bezahlte Einheiten sind Geld.
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

        // Saldo-Schutz wie bei Angeboten/Skins: Der Listing-Stamp serialisiert nur denselben Bestand –
        // das Wallet ist über alle Kaufwege hinweg geteilt. Ein Bump des Kind-Tokens lässt einen parallel
        // gestarteten Zweitkauf (anderes Listing, Angebot oder Skin) mit Conflict scheitern, sodass der
        // Deckungs-Check nicht doppelt umgangen und der Saldo nicht negativ werden kann.
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
            db.ChildPoints.Add(new ChildPointsEntry
            {
                ChildId = childId,
                Amount = purchase.CoinPrice,
                Kind = PointKind.ShopCoins,
                Reason = $"Shop-Kauf storniert (Rückerstattung): {purchase.Title}",
                CreatedAt = nowUtc,
            });
        if (purchase.GemPrice > 0)
            db.ChildPoints.Add(new ChildPointsEntry
            {
                ChildId = childId,
                Amount = purchase.GemPrice,
                Kind = PointKind.ShopGems,
                Reason = $"Shop-Kauf storniert (Rückerstattung): {purchase.Title}",
                CreatedAt = nowUtc,
            });

        // Inventar um die stornierte Menge reduzieren (soweit noch vorhanden)
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

        // Aktivierung nur für einen Artikel eines betreuenden Supervisors möglich.
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
            SupervisorId = article.AdultId, // Aussteller festhalten: nur er genehmigt/lehnt ab.
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

        // Nur bei fehlendem Artikelbezug (Artikel nachträglich gelöscht) gibt es kein Inventar zu buchen.
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

    /// <summary>Applies a due refill rule idempotently: due listings are set to MaxStock.</summary>
    public static bool ApplyDueRefill(ShopListing listing, DateTime nowUtc)
    {
        if (listing.RefillKind == ShopRefillKind.None || listing.MaxStock <= 0) return false;
        if (!IsRefillDue(listing, nowUtc)) return false;

        listing.CurrentStock = Math.Max(listing.CurrentStock, listing.MaxStock);
        listing.LastRefilledAtUtc = nowUtc;
        listing.ConcurrencyStamp = Guid.NewGuid();
        return true;
    }

    private static bool IsRefillDue(ShopListing listing, DateTime nowUtc) => listing.RefillKind switch
    {
        ShopRefillKind.Once => listing.RefillAtUtc is { } at && nowUtc >= at && listing.LastRefilledAtUtc is null,
        ShopRefillKind.Daily => listing.LastRefilledAtUtc is null || listing.LastRefilledAtUtc.Value.Date < nowUtc.Date,
        ShopRefillKind.TwiceDaily => listing.LastRefilledAtUtc is null
            || listing.LastRefilledAtUtc.Value.Date < nowUtc.Date
            || listing.LastRefilledAtUtc.Value.Hour < 12 && nowUtc.Hour >= 12,
        ShopRefillKind.Weekly => listing.RefillDayOfWeek == nowUtc.DayOfWeek
            && (listing.LastRefilledAtUtc is null || listing.LastRefilledAtUtc.Value.Date < nowUtc.Date),
        _ => false,
    };

    // Aussteller-gebunden: nur der Supervisor, der den Artikel/das Angebot ausgestellt hat (SupervisorId-Snapshot),
    // sieht/bearbeitet den Kauf bzw. die Anfrage. Ein fremd ausgestellter Vorgang erscheint als NotFound.
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
    }
}
