using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Services;

/// <summary>
/// Geschäftslogik des Familien-Shops: Vater verwaltet Artikel-Katalog und Angebote, der Sohn kauft
/// aus eigenem Wallet (Coins/Gems sofort abgebucht), der Bestand wird reduziert und das aggregierte
/// Inventar des Kindes erhöht. Aktivierungsanfragen des Sohns genehmigt oder lehnt der Vater ab.
/// </summary>
public class ShopService(PuglingDbContext db, WalletService wallet)
{
    /// <summary>Fehlerursache eines Shop-Vorgangs (None = erfolgreich).</summary>
    public enum ShopError
    {
        None = 0,
        NotFound,
        ListingInactive,
        InsufficientStock,
        InsufficientCoins,
        InsufficientGems,
        InsufficientInventory,
        NotOpen,
        NotPending,
        Conflict,
    }

    /// <summary>Ergebnis mit optionaler Nutzlast.</summary>
    public record Result<T>(ShopError Error, T? Value) where T : class
    {
        public static Result<T> Ok(T value) => new(ShopError.None, value);
        public static Result<T> Fail(ShopError error) => new(error, null);
    }

    /// <summary>Kanonische Zuordnung <see cref="ShopError"/> → <see cref="ApiError"/>.</summary>
    public static ApiError ToApiError(ShopError error) => error switch
    {
        ShopError.NotFound => ApiErrors.NotFound,
        ShopError.ListingInactive => ApiErrors.ShopListingInactive,
        ShopError.InsufficientStock => ApiErrors.ShopInsufficientStock,
        ShopError.InsufficientCoins => ApiErrors.InsufficientCoins,
        ShopError.InsufficientGems => ApiErrors.InsufficientGems,
        ShopError.InsufficientInventory => ApiErrors.InsufficientInventory,
        ShopError.NotOpen => ApiErrors.PurchaseNotOpen,
        ShopError.NotPending => ApiErrors.ActivationNotPending,
        _ => ApiErrors.ConcurrencyConflict,
    };

    /// <summary>
    /// Lädt alle Angebote (<see cref="ShopListing"/>s) des Vaters inkl. ihres Artikels und wendet
    /// fällige Refill-Regeln idempotent an.
    /// </summary>
    public async Task<IReadOnlyList<ShopListing>> ListingsForFatherAsync(
        int fatherId, bool activeOnly, DateTime nowUtc, CancellationToken ct = default)
    {
        var query = db.ShopListings
            .Include(l => l.ShopArticle)
            .Where(l => l.ShopArticle!.FatherId == fatherId);
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
            .Where(l => l.ShopArticle!.FatherId == fatherId);
        if (activeOnly) fresh = fresh.Where(l => l.Active);
        return await fresh
            .OrderByDescending(l => l.Active)
            .ThenBy(l => l.ShopArticle!.ArticleNumber)
            .ThenBy(l => l.Id)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Kauft ein Angebot für das Kind: prüft Familienzugehörigkeit, Aktiv-Status, Bestand und beide
    /// Wallet-Salden, bucht Coins/Gems ab, reduziert den Lagerbestand, legt die Kaufbuchung an und
    /// erhöht das aggregierte <see cref="ChildInventory"/> des Kinds für den zugehörigen Artikel.
    /// </summary>
    public async Task<Result<ShopPurchase>> PurchaseAsync(
        int childId, int listingId, DateTime nowUtc, CancellationToken ct = default)
    {
        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == childId, ct);
        if (child is null) return Result<ShopPurchase>.Fail(ShopError.NotFound);

        var listing = await db.ShopListings
            .Include(l => l.ShopArticle)
            .FirstOrDefaultAsync(l => l.Id == listingId && l.ShopArticle!.FatherId == child.FatherId, ct);
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
                ChildId = childId, Amount = -listing.CoinPrice, Kind = PointKind.ShopCoins,
                Reason = $"Shop-Angebot gekauft: {article.Title}", CreatedAt = nowUtc,
            });
        if (listing.GemPrice > 0)
            db.ChildPoints.Add(new ChildPointsEntry
            {
                ChildId = childId, Amount = -listing.GemPrice, Kind = PointKind.ShopGems,
                Reason = $"Shop-Angebot gekauft: {article.Title}", CreatedAt = nowUtc,
            });

        listing.CurrentStock -= 1;
        listing.ConcurrencyStamp = Guid.NewGuid();

        var title = string.IsNullOrWhiteSpace(listing.Title) ? article.Title : listing.Title;
        var purchase = new ShopPurchase
        {
            ChildId = childId,
            ShopListingId = listing.Id,
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
                ChildId = childId, ShopArticleId = article.Id,
                Quantity = listing.UnitsPerPurchase,
            });
        else
        {
            inventory.Quantity += listing.UnitsPerPurchase;
            inventory.ConcurrencyStamp = Guid.NewGuid();
        }

        return await TrySaveAsync(ct)
            ? Result<ShopPurchase>.Ok(purchase)
            : Result<ShopPurchase>.Fail(ShopError.Conflict);
    }

    /// <summary>
    /// Storniert einen offenen Kauf: erstattet Coins/Gems zurück und reduziert das Inventar um
    /// <see cref="ShopPurchase.UnitsPerPurchase"/> (mindestens 0).
    /// </summary>
    public async Task<Result<ShopPurchase>> CancelPurchaseAsync(
        int childId, int purchaseId, DateTime nowUtc, CancellationToken ct = default)
    {
        var purchase = await LoadOpenPurchaseAsync(childId, purchaseId, ct);
        if (purchase is null) return await MissOrNotOpenAsync(childId, purchaseId, ct);

        purchase.Status = ShopPurchaseStatus.Cancelled;
        purchase.ClosedAt = nowUtc;
        purchase.ConcurrencyStamp = Guid.NewGuid();

        if (purchase.CoinPrice > 0)
            db.ChildPoints.Add(new ChildPointsEntry
            {
                ChildId = childId, Amount = purchase.CoinPrice, Kind = PointKind.ShopCoins,
                Reason = $"Shop-Kauf storniert (Rückerstattung): {purchase.Title}", CreatedAt = nowUtc,
            });
        if (purchase.GemPrice > 0)
            db.ChildPoints.Add(new ChildPointsEntry
            {
                ChildId = childId, Amount = purchase.GemPrice, Kind = PointKind.ShopGems,
                Reason = $"Shop-Kauf storniert (Rückerstattung): {purchase.Title}", CreatedAt = nowUtc,
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
    /// Stellt eine Aktivierungsanfrage des Sohns: prüft, ob genug Einheiten im Inventar sind, und
    /// legt eine <see cref="ActivationRequest"/> mit Status <see cref="ActivationRequestStatus.Pending"/> an.
    /// Das Inventar wird erst bei Genehmigung (<see cref="ApproveActivationAsync"/>) reduziert.
    /// </summary>
    public async Task<Result<ActivationRequest>> RequestActivationAsync(
        int childId, int articleId, int quantity, DateTime nowUtc, CancellationToken ct = default)
    {
        if (quantity <= 0) return Result<ActivationRequest>.Fail(ShopError.NotFound);

        var child = await db.Children.AsNoTracking().FirstOrDefaultAsync(c => c.Id == childId, ct);
        if (child is null) return Result<ActivationRequest>.Fail(ShopError.NotFound);

        var article = await db.ShopArticles.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == articleId && a.FatherId == child.FatherId, ct);
        if (article is null) return Result<ActivationRequest>.Fail(ShopError.NotFound);

        var inventory = await db.ChildInventories
            .FirstOrDefaultAsync(i => i.ChildId == childId && i.ShopArticleId == articleId, ct);
        if (inventory is null || inventory.Quantity < quantity)
            return Result<ActivationRequest>.Fail(ShopError.InsufficientInventory);

        var request = new ActivationRequest
        {
            ChildId = childId,
            ShopArticleId = articleId,
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
    /// Vater genehmigt eine offene Aktivierungsanfrage: Status → Approved, Inventar wird um
    /// <see cref="ActivationRequest.RequestedQuantity"/> reduziert (min. 0). Concurrency-Token am
    /// Inventar verhindert parallele Überziehung.
    /// </summary>
    public async Task<Result<ActivationRequest>> ApproveActivationAsync(
        int childId, int requestId, DateTime nowUtc, CancellationToken ct = default)
    {
        var request = await LoadPendingActivationAsync(childId, requestId, ct);
        if (request is null) return await MissOrNotPendingAsync(childId, requestId, ct);

        request.Status = ActivationRequestStatus.Approved;
        request.ClosedAt = nowUtc;

        if (request.ShopArticleId is not null)
        {
            var inv = await db.ChildInventories
                .FirstOrDefaultAsync(i => i.ChildId == childId && i.ShopArticleId == request.ShopArticleId, ct);
            if (inv is not null)
            {
                inv.Quantity = Math.Max(0, inv.Quantity - request.RequestedQuantity);
                inv.ConcurrencyStamp = Guid.NewGuid();
            }
        }

        return await TrySaveAsync(ct)
            ? Result<ActivationRequest>.Ok(request)
            : Result<ActivationRequest>.Fail(ShopError.Conflict);
    }

    /// <summary>
    /// Vater lehnt eine offene Aktivierungsanfrage ab: Status → Rejected. Das Inventar bleibt
    /// unverändert – die Einheiten verbleiben beim Sohn.
    /// </summary>
    public async Task<Result<ActivationRequest>> RejectActivationAsync(
        int childId, int requestId, DateTime nowUtc, CancellationToken ct = default)
    {
        var request = await LoadPendingActivationAsync(childId, requestId, ct);
        if (request is null) return await MissOrNotPendingAsync(childId, requestId, ct);

        request.Status = ActivationRequestStatus.Rejected;
        request.ClosedAt = nowUtc;

        return await TrySaveAsync(ct)
            ? Result<ActivationRequest>.Ok(request)
            : Result<ActivationRequest>.Fail(ShopError.Conflict);
    }

    /// <summary>Wendet eine fällige Refill-Regel idempotent an: fällige Angebote werden auf MaxStock gesetzt.</summary>
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

    private Task<ShopPurchase?> LoadOpenPurchaseAsync(int childId, int purchaseId, CancellationToken ct) =>
        db.ShopPurchases.FirstOrDefaultAsync(
            p => p.Id == purchaseId && p.ChildId == childId && p.Status == ShopPurchaseStatus.Owned, ct);

    private Task<ActivationRequest?> LoadPendingActivationAsync(int childId, int requestId, CancellationToken ct) =>
        db.ActivationRequests.FirstOrDefaultAsync(
            r => r.Id == requestId && r.ChildId == childId && r.Status == ActivationRequestStatus.Pending, ct);

    private async Task<Result<ShopPurchase>> MissOrNotOpenAsync(int childId, int purchaseId, CancellationToken ct)
    {
        var exists = await db.ShopPurchases.AnyAsync(p => p.Id == purchaseId && p.ChildId == childId, ct);
        return Result<ShopPurchase>.Fail(exists ? ShopError.NotOpen : ShopError.NotFound);
    }

    private async Task<Result<ActivationRequest>> MissOrNotPendingAsync(int childId, int requestId, CancellationToken ct)
    {
        var exists = await db.ActivationRequests.AnyAsync(r => r.Id == requestId && r.ChildId == childId, ct);
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
