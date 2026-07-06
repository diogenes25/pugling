using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;
using Pugling.Api.Services;

namespace Pugling.Api.Controllers;

/// <summary>
/// Selbstauskunft für den angemeldeten Sohn: eigener Punktestand (Wallet) und Kurzprofil.
/// Schließt die Lücke, dass der kontoübergreifende Punktestand sonst nur der Vater lesen kann
/// (<see cref="Admin.ChildrenController"/> ist <c>Vater</c>-only).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.V1 + "/me")]
[Tags("Me")]
[Produces("application/json")]
[Authorize(Roles = Roles.Sohn)]
public class MeController(PuglingDbContext db, GamificationService gamification,
    WalletService wallet, OfferService offers, ShopService shop) : ControllerBase
{
    /// <summary>Eine einzelne Punkte-Buchung (Gutschrift positiv, Abzug negativ) mit Kategorie.</summary>
    public record PointsEntryResponse(int Id, int Amount, PointKind Kind, string Reason, DateTime CreatedAt);
    /// <summary>Kontostand (Wallet) des Kindes je Währung samt der letzten Buchungen.</summary>
    public record WalletResponse(int ChildId, int Coins, int Gems, IReadOnlyList<PointsEntryResponse> Entries);

    /// <summary>Skin-Zustand des Kindes: aktueller Gem-Stand, ausgerüsteter und freigeschaltete Skins.</summary>
    public record SkinStateResponse(int Gems, string Selected, IReadOnlyList<string> Owned);

    /// <summary>Ein kaufbares Angebot aus Sohn-Sicht: Preis, Wiederkehr, Restkontingent dieser Periode und ob bezahlbar.</summary>
    public record RewardOfferResponse(int Id, string Title, int Cost, OfferPeriod Period, int Quantity,
        int RemainingThisPeriod, bool Affordable, string? PlanTitle, string? ExerciseTitle);
    /// <summary>Ein eigener Kauf im Konto mit aktuellem Status („gekauft am … – erfüllt am …").</summary>
    public record MyRedemptionResponse(int Id, int? RewardId, string Title, int Cost,
        RewardRedemptionStatus Status, DateTime PurchasedAt, DateTime? FulfilledAt);
    /// <summary>Angebots-Sicht des Sohns: Münzstand, verfügbare Angebote und eigene Käufe.</summary>
    public record RewardsViewResponse(int Coins, IReadOnlyList<RewardOfferResponse> Available,
        IReadOnlyList<MyRedemptionResponse> Redemptions);
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

    /// <summary>Eigener Kontostand (Münzen + Gems) samt der letzten Buchungen (neueste zuerst).</summary>
    /// <param name="skip">Anzahl zu überspringender Buchungen (Paging).</param>
    /// <param name="take">Maximale Buchungszahl (1..500). Gesamtzahl im Header <c>X-Total-Count</c>.</param>
    [HttpGet("points")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WalletResponse>> Points(
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();

        var entries = await db.ChildPoints
            .AsNoTracking()
            .Where(p => p.ChildId == cid)
            .OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
            .Select(p => new PointsEntryResponse(p.Id, p.Amount, p.Kind, p.Reason, p.CreatedAt))
            .ToPagedListAsync(Response, skip, take);

        var (coins, gems) = await wallet.BalancesAsync(cid.Value);
        return new WalletResponse(cid.Value, coins, gems, entries);
    }

    /// <summary>Eigene Missionen (Tages-/Wochen-/Zusatzziele) mit aktuellem Fortschritt (reine Lesesicht).</summary>
    [HttpGet("missions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<GamificationService.MissionStatus>>> Missions()
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();
        return Ok(await gamification.MissionStatusesAsync(cid.Value, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    /// <summary>Eigene Auszeichnungen (Badges): erreichte und noch offene, erreichte zuerst (reine Lesesicht).</summary>
    [HttpGet("achievements")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<GamificationService.AchievementStatus>>> Achievements()
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();
        return Ok(await gamification.AchievementStatusesAsync(cid.Value, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    /// <summary>Eigener Skin-Zustand: Gem-Stand, ausgerüsteter Skin und freigeschaltete Skins.</summary>
    [HttpGet("skins")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SkinStateResponse>> Skins()
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();
        return await SkinStateAsync(cid.Value);
    }

    /// <summary>
    /// Schaltet einen Skin für den angemeldeten Sohn frei: bucht die Kosten als negative Punkte-Buchung
    /// ab und rüstet ihn direkt aus. Kosten und Besitz sind serverseitig autoritativ (kein Client-Betrug).
    /// Abbuchung und Freischaltung werden in einem <c>SaveChanges</c> committet; das Concurrency-Token am
    /// Kind verhindert, dass zwei parallele Käufe (Doppelklick/Retry) beide den Deckungs-Check bestehen –
    /// der zweite scheitert dann und liefert 409 statt doppelt abzubuchen.
    /// </summary>
    [HttpPost("skins/{skinId}/purchase")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SkinStateResponse>> PurchaseSkin(string skinId)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();

        var cost = SkinCatalog.CostOf(skinId);
        if (cost is null) return this.ProblemWithCode(ApiErrors.NotFound, $"Unknown skin '{skinId}'.");

        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == cid);
        if (child is null) return Forbid();
        if (child.OwnedSkins.Contains(skinId))
            return this.ProblemWithCode(ApiErrors.SkinAlreadyUnlocked, "This skin is already unlocked.");

        var gems = await wallet.GemsAsync(cid.Value);
        if (gems < cost)
            return this.ProblemWithCode(ApiErrors.InsufficientGems, $"Not enough gems: {gems}/{cost} for '{skinId}'.");

        db.ChildPoints.Add(new ChildPointsEntry
        {
            ChildId = cid.Value,
            Amount = -cost.Value,
            Kind = PointKind.SkinPurchase,
            Reason = $"Skin freigeschaltet: {skinId}",
        });
        child.OwnedSkins = [.. child.OwnedSkins, skinId]; // Neuzuweisung: JSON-Spalte, kein In-Place-Mutieren
        child.SelectedSkin = skinId;                       // gekaufter Skin wird direkt ausgerüstet
        child.ConcurrencyStamp = Guid.NewGuid();           // Token bumpen → parallele Zweitbuchung scheitert

        if (!await TrySaveAsync())
            return this.ProblemWithCode(ApiErrors.ConcurrencyConflict, "Purchase conflicted with a concurrent action — please try again.");

        return await SkinStateAsync(cid.Value);
    }

    /// <summary>Rüstet einen bereits freigeschalteten Skin aus (persistiert geräteübergreifend am Kind).</summary>
    [HttpPost("skins/{skinId}/equip")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SkinStateResponse>> EquipSkin(string skinId)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();

        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == cid);
        if (child is null) return Forbid();
        if (!child.OwnedSkins.Contains(skinId))
            return this.ProblemWithCode(ApiErrors.SkinNotUnlocked, "This skin is not unlocked yet.");

        child.SelectedSkin = skinId;
        child.ConcurrencyStamp = Guid.NewGuid();

        if (!await TrySaveAsync())
            return this.ProblemWithCode(ApiErrors.ConcurrencyConflict, "Equipping conflicted with a concurrent action — please try again.");
        return await SkinStateAsync(cid.Value);
    }

    /// <summary>Speichert und fängt eine Nebenläufigkeits-Kollision (Token) ab: false = kollidiert, nichts committet.</summary>
    private async Task<bool> TrySaveAsync()
    {
        try
        {
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    /// <summary>Eigene Angebots-Sicht: Münzstand, verfügbare (aktive) Angebote und die eigenen Käufe.</summary>
    [HttpGet("rewards")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RewardsViewResponse>> Rewards()
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();
        return await RewardsViewAsync(cid.Value);
    }

    /// <summary>
    /// Kauft ein Angebot: die Münzen werden <b>sofort</b> abgebucht (Direktkauf), der Vater erfüllt seinen
    /// Teil später. Das Kontingent der aktuellen Periode und die Deckung werden serverseitig geprüft; ein
    /// paralleler Doppelkauf scheitert per Concurrency-Token mit 409.
    /// </summary>
    [HttpPost("rewards/{rewardId:int}/purchase")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RewardsViewResponse>> Purchase(int rewardId)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();

        var result = await offers.PurchaseAsync(cid.Value, rewardId, DateTime.UtcNow);
        return result.Error switch
        {
            OfferService.OfferError.None => await RewardsViewAsync(cid.Value),
            OfferService.OfferError.NotFound => this.ProblemWithCode(OfferService.ToApiError(result.Error), "Offer not found."),
            OfferService.OfferError.Inactive => this.ProblemWithCode(OfferService.ToApiError(result.Error), "This offer is no longer available."),
            OfferService.OfferError.QuotaExceeded => this.ProblemWithCode(OfferService.ToApiError(result.Error), "The quota for this period is exhausted."),
            OfferService.OfferError.InsufficientCoins => this.ProblemWithCode(OfferService.ToApiError(result.Error), "Not enough coins for this offer."),
            _ => this.ProblemWithCode(OfferService.ToApiError(result.Error), "Purchase conflicted with a concurrent action — please try again."),
        };
    }

    /// <summary>
    /// Familien-Shop: aktive Angebote des Vaters, aggregiertes Inventar und Kaufhistorie des Sohns.
    /// </summary>
    [HttpGet("shop")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ShopViewResponse>> Shop()
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();
        return await ShopViewAsync(cid.Value);
    }

    /// <summary>
    /// Kauft ein Familien-Shop-Angebot: Coins/Gems werden sofort abgebucht, das aggregierte Inventar
    /// des Sohns für den zugehörigen Artikel wird um <c>UnitsPerPurchase</c> erhöht.
    /// </summary>
    [HttpPost("shop/listings/{listingId:int}/purchase")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ShopViewResponse>> PurchaseShopListing(int listingId)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();

        var result = await shop.PurchaseAsync(cid.Value, listingId, DateTime.UtcNow);
        return result.Error switch
        {
            ShopService.ShopError.None => await ShopViewAsync(cid.Value),
            ShopService.ShopError.NotFound => this.ProblemWithCode(ShopService.ToApiError(result.Error), "Shop listing not found."),
            ShopService.ShopError.ListingInactive => this.ProblemWithCode(ShopService.ToApiError(result.Error), "This shop listing is no longer available."),
            ShopService.ShopError.InsufficientStock => this.ProblemWithCode(ShopService.ToApiError(result.Error), "This shop listing is out of stock."),
            ShopService.ShopError.InsufficientCoins => this.ProblemWithCode(ShopService.ToApiError(result.Error), "Not enough coins for this shop listing."),
            ShopService.ShopError.InsufficientGems => this.ProblemWithCode(ShopService.ToApiError(result.Error), "Not enough gems for this shop listing."),
            _ => this.ProblemWithCode(ShopService.ToApiError(result.Error), "Purchase conflicted with a concurrent action — please try again."),
        };
    }

    /// <summary>
    /// Stellt eine Aktivierungsanfrage: der Sohn möchte <c>quantity</c> Einheiten des Artikels
    /// verbrauchen. Der Vater genehmigt oder lehnt ab; das Inventar wird erst bei Genehmigung reduziert.
    /// </summary>
    [HttpPost("shop/inventory/{articleId:int}/activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MyActivationResponse>> RequestActivation(
        int articleId, [FromBody] ActivateDto dto)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();
        if (dto.Quantity <= 0) return this.ProblemWithCode(ApiErrors.ValidationError, "Quantity must be at least 1.");

        var result = await shop.RequestActivationAsync(cid.Value, articleId, dto.Quantity, DateTime.UtcNow);
        return result.Error switch
        {
            ShopService.ShopError.None => MapActivation(result.Value!),
            ShopService.ShopError.NotFound => this.ProblemWithCode(ShopService.ToApiError(result.Error), "Article not found in your family shop."),
            ShopService.ShopError.InsufficientInventory => this.ProblemWithCode(ShopService.ToApiError(result.Error), "Not enough units in your inventory."),
            ShopService.ShopError.InvalidQuantity => this.ProblemWithCode(ShopService.ToApiError(result.Error), "Quantity must be at least 1."),
            _ => this.ProblemWithCode(ShopService.ToApiError(result.Error), "The activation request could not be saved — please try again."),
        };
    }

    public record ActivateDto(int Quantity);

    /// <summary>Eigene Aktivierungsanfragen (neueste zuerst), optional nach Status gefiltert.</summary>
    [HttpGet("shop/activations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<MyActivationResponse>>> MyActivations(
        [FromQuery] ActivationRequestStatus? status,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();

        var query = db.ActivationRequests.AsNoTracking().Where(r => r.ChildId == cid);
        if (status is not null) query = query.Where(r => r.Status == status);

        return await query
            .OrderBy(r => r.Status == ActivationRequestStatus.Pending ? 0 : 1)
            .ThenByDescending(r => r.RequestedAt)
            .Select(r => MapActivation(r))
            .ToPagedListAsync(Response, skip, take);
    }

    private static MyActivationResponse MapActivation(ActivationRequest r) =>
        new(r.Id, r.ShopArticleId, r.ArticleTitle, r.UnitType, r.ActionType,
            r.RequestedQuantity, r.Status, r.RequestedAt, r.ClosedAt);

    private async Task<RewardsViewResponse> RewardsViewAsync(int childId)
    {
        var coins = await wallet.CoinsAsync(childId);

        var offersList = await db.Rewards.AsNoTracking()
            .Include(r => r.StudyPlan).Include(r => r.Exercise)
            .Where(r => r.ChildId == childId && r.Active)
            .OrderBy(r => r.Cost).ThenBy(r => r.Id)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var available = new List<RewardOfferResponse>(offersList.Count);
        foreach (var r in offersList)
        {
            var used = await offers.CountInCurrentPeriodAsync(r, now);
            var remaining = Math.Max(0, r.Quantity - used);
            available.Add(new RewardOfferResponse(r.Id, r.Title, r.Cost, r.Period, r.Quantity,
                remaining, coins >= r.Cost && remaining > 0, r.StudyPlan?.Title, r.Exercise?.Title));
        }

        var redemptions = await db.RewardRedemptions.AsNoTracking()
            .Where(r => r.ChildId == childId)
            .OrderBy(r => r.Status == RewardRedemptionStatus.Purchased ? 0 : 1)
            .ThenByDescending(r => r.PurchasedAt)
            .Select(r => new MyRedemptionResponse(r.Id, r.RewardId, r.Title, r.Cost, r.Status, r.PurchasedAt, r.FulfilledAt))
            .ToListAsync();

        return new RewardsViewResponse(coins, available, redemptions);
    }

    private async Task<ShopViewResponse> ShopViewAsync(int childId)
    {
        var child = await db.Children.AsNoTracking().FirstAsync(c => c.Id == childId);
        var balances = await wallet.BalancesAsync(childId);
        var now = DateTime.UtcNow;

        var listings = await shop.ListingsForFatherAsync(child.FatherId, activeOnly: true, now);
        var available = listings
            .OrderBy(l => l.ShopArticle!.ArticleNumber).ThenBy(l => l.Id)
            .Select(l =>
            {
                var art = l.ShopArticle!;
                var title = string.IsNullOrWhiteSpace(l.Title) ? art.Title : l.Title;
                return new ShopListingResponse(
                    l.Id, art.Id, art.ArticleNumber, art.Title, art.UnitType, art.ActionType,
                    title, l.Description, l.CoinPrice, l.GemPrice, l.UnitsPerPurchase, l.CurrentStock,
                    l.CurrentStock > 0 && balances.Coins >= l.CoinPrice && balances.Gems >= l.GemPrice);
            })
            .ToList();

        var inventory = await db.ChildInventories.AsNoTracking()
            .Include(i => i.ShopArticle)
            .Where(i => i.ChildId == childId && i.Quantity > 0)
            .OrderBy(i => i.ShopArticle!.ArticleNumber)
            .Select(i => new MyInventoryItemResponse(
                i.ShopArticleId, i.ShopArticle!.ArticleNumber, i.ShopArticle.Title,
                i.ShopArticle.UnitType, i.ShopArticle.ActionType, i.Quantity))
            .ToListAsync();

        var purchases = await db.ShopPurchases.AsNoTracking()
            .Where(p => p.ChildId == childId)
            .OrderBy(p => p.Status == ShopPurchaseStatus.Owned ? 0 : 1)
            .ThenByDescending(p => p.PurchasedAt).ThenByDescending(p => p.Id)
            .Select(p => new MyShopPurchaseResponse(
                p.Id, p.ShopListingId, p.ArticleNumber, p.Title,
                p.CoinPrice, p.GemPrice, p.UnitsPerPurchase, p.Status, p.PurchasedAt, p.ClosedAt))
            .Take(50)
            .ToListAsync();

        return new ShopViewResponse(balances.Coins, balances.Gems, available, inventory, purchases);
    }

    private async Task<SkinStateResponse> SkinStateAsync(int childId)
    {
        var child = await db.Children.AsNoTracking().FirstAsync(c => c.Id == childId);
        var gems = await wallet.GemsAsync(childId);
        return new SkinStateResponse(gems, child.SelectedSkin, child.OwnedSkins);
    }
}
