using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Student;

/// <summary>
/// Self-service info for the logged-in child: own point balance (wallet) and short profile.
/// Closes the gap that the cross-account point balance would otherwise only be readable by the father
/// (<see cref="Supervisor.ChildrenController"/> is <c>father</c>-only).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Student + "/me")]
[Tags("Student – Me")]
[Produces("application/json")]
[Authorize(Roles = Roles.Student)]
public class MeController(PuglingDbContext db, GamificationService gamification,
    WalletService wallet, ShopService shop, PositionProgressService progress) : ControllerBase
{
    /// <summary>Own wallet balance (coins + gems). The individual ledger entries live under <c>points/entries</c>.</summary>
    [HttpGet("points")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WalletResponse>> Points(CancellationToken ct = default)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();

        var (coins, gems) = await wallet.BalancesAsync(cid.Value, ct);
        return new WalletResponse(cid.Value, coins, gems);
    }

    /// <summary>Own point ledger entries (newest first), paged.</summary>
    /// <param name="skip">Number of ledger entries to skip (paging).</param>
    /// <param name="take">Maximum number of ledger entries (1..500). Total count in the <c>X-Total-Count</c> header.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("points/entries")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<MyPointsEntryResponse>>> PointsEntries(
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake,
        CancellationToken ct = default)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();

        return await db.ChildPointsEntries
            .AsNoTracking()
            .Where(p => p.ChildId == cid)
            .OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
            .Select(p => new MyPointsEntryResponse(p.Id, p.Amount, p.Kind, p.Reason, p.CreatedAt))
            .ToPagedListAsync(Response, skip, take, ct);
    }

    /// <summary>A single own point ledger entry (detail view for the list under <c>points/entries</c>).</summary>
    [HttpGet("points/entries/{entryId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MyPointsEntryResponse>> PointsEntry(int entryId, CancellationToken ct = default)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();

        var entry = await db.ChildPointsEntries
            .AsNoTracking()
            .Where(p => p.Id == entryId && p.ChildId == cid)
            .Select(p => new MyPointsEntryResponse(p.Id, p.Amount, p.Kind, p.Reason, p.CreatedAt))
            .FirstOrDefaultAsync(ct);

        return entry is null ? NotFound() : entry;
    }

    /// <summary>Own missions (daily/weekly/extra goals) with current progress (read-only view), paged.</summary>
    /// <param name="skip">Number of missions to skip (paging).</param>
    /// <param name="take">Maximum number of missions (1..500). Total count in the <c>X-Total-Count</c> header.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("missions")]
    [Tags("Student – Missions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<MissionStatus>>> Missions(
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake,
        CancellationToken ct = default)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();
        var (items, total) = await gamification.MissionStatusesAsync(cid.Value, DateOnly.FromDateTime(DateTime.UtcNow),
            Math.Max(skip, 0), Math.Clamp(take, 0, PagingExtensions.MaxTake), ct);
        Response.Headers["X-Total-Count"] = total.ToString();
        return Ok(items);
    }

    /// <summary>A single own mission (detail view for the list under <c>missions</c>).</summary>
    [HttpGet("missions/{missionId:int}")]
    [Tags("Student – Missions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MissionStatus>> Mission(int missionId, CancellationToken ct = default)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();
        var status = await gamification.MissionStatusAsync(cid.Value, missionId, DateOnly.FromDateTime(DateTime.UtcNow), ct);
        return status is null ? NotFound() : status;
    }

    /// <summary>Own awards (badges): achieved and still open, achieved first (read-only view), paged.</summary>
    /// <param name="skip">Number of awards to skip (paging).</param>
    /// <param name="take">Maximum number of awards (1..500). Total count in the <c>X-Total-Count</c> header.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("achievements")]
    [Tags("Student – Achievements")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AchievementStatus>>> Achievements(
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake,
        CancellationToken ct = default)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();
        var (items, total) = await gamification.AchievementStatusesAsync(cid.Value, DateOnly.FromDateTime(DateTime.UtcNow),
            Math.Max(skip, 0), Math.Clamp(take, 0, PagingExtensions.MaxTake), ct);
        Response.Headers["X-Total-Count"] = total.ToString();
        return Ok(items);
    }

    /// <summary>A single own award (detail view for the list under <c>achievements</c>).</summary>
    [HttpGet("achievements/{achievementId:int}")]
    [Tags("Student – Achievements")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AchievementStatus>> Achievement(int achievementId, CancellationToken ct = default)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();
        var status = await gamification.AchievementStatusAsync(cid.Value, achievementId, DateOnly.FromDateTime(DateTime.UtcNow), ct);
        return status is null ? NotFound() : status;
    }

    /// <summary>Own skin state: gem balance, equipped skin and unlocked skins.</summary>
    [HttpGet("skins")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SkinStateResponse>> Skins(CancellationToken ct = default)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();
        return await SkinStateAsync(cid.Value, ct);
    }

    /// <summary>
    /// Unlocks a skin for the logged-in child: books the cost as a negative point ledger entry
    /// and equips it right away. Cost and ownership are authoritative server-side (no client cheating).
    /// Debit and unlock are committed in one <c>SaveChanges</c>; the concurrency token on the
    /// child prevents two parallel purchases (double-click/retry) from both passing the balance check –
    /// the second one then fails and returns 409 instead of debiting twice.
    /// </summary>
    [HttpPost("skins/{skinId}/purchase")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SkinStateResponse>> PurchaseSkin(string skinId, CancellationToken ct = default)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();

        var cost = SkinCatalog.CostOf(skinId);
        if (cost is null) return this.ProblemWithCode(ApiErrors.NotFound, $"Unknown skin '{skinId}'.");

        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == cid, ct);
        if (child is null) return Forbid();
        if (child.OwnedSkins.Contains(skinId))
            return this.ProblemWithCode(ApiErrors.SkinAlreadyUnlocked, "This skin is already unlocked.");

        var gems = await wallet.GemsAsync(cid.Value, ct);
        if (gems < cost)
            return this.ProblemWithCode(ApiErrors.InsufficientGems, $"Not enough gems: {gems}/{cost} for '{skinId}'.");

        db.ChildPointsEntries.Add(new ChildPointsEntry
        {
            ChildId = cid.Value,
            Amount = -cost.Value,
            Kind = PointKind.SkinPurchase,
            Reason = $"Skin freigeschaltet: {skinId}",
        });
        child.OwnedSkins = [.. child.OwnedSkins, skinId]; // reassign: JSON column, no in-place mutation
        child.SelectedSkin = skinId;                       // a purchased skin is equipped right away
        child.ConcurrencyStamp = Guid.NewGuid();           // bump the token → a parallel second entry fails

        if (!await TrySaveAsync(ct))
            return this.ProblemWithCode(ApiErrors.ConcurrencyConflict, "Purchase conflicted with a concurrent action — please try again.");

        return await SkinStateAsync(cid.Value, ct);
    }

    /// <summary>Equips an already unlocked skin (persisted on the child across devices).</summary>
    [HttpPost("skins/{skinId}/equip")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SkinStateResponse>> EquipSkin(string skinId, CancellationToken ct = default)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();

        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == cid, ct);
        if (child is null) return Forbid();
        if (!child.OwnedSkins.Contains(skinId))
            return this.ProblemWithCode(ApiErrors.SkinNotUnlocked, "This skin is not unlocked yet.");

        child.SelectedSkin = skinId;
        child.ConcurrencyStamp = Guid.NewGuid();

        if (!await TrySaveAsync(ct))
            return this.ProblemWithCode(ApiErrors.ConcurrencyConflict, "Equipping conflicted with a concurrent action — please try again.");
        return await SkinStateAsync(cid.Value, ct);
    }

    /// <summary>Saves and catches a concurrency collision (token): false = collided, nothing committed.</summary>
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

    /// <summary>
    /// Family shop: active listings of the father, aggregated inventory and purchase history of the child.
    /// The purchase history is paged (<paramref name="purchaseSkip"/>/<paramref name="purchaseTake"/>), total
    /// count in the <c>X-Total-Count</c> header - a fixed <c>Take(50)</c> used to end the history silently
    /// (B-99).
    /// </summary>
    [HttpGet("shop")]
    [Tags("Student – Shop")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ShopViewResponse>> Shop(
        [FromQuery] int purchaseSkip = 0, [FromQuery] int purchaseTake = PagingExtensions.DefaultTake,
        CancellationToken ct = default)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();
        return await ShopViewAsync(cid.Value, purchaseSkip, purchaseTake, ct);
    }

    /// <summary>
    /// Buys a family shop listing: coins/gems are debited immediately, the child's aggregated inventory
    /// for the associated article is increased by <c>UnitsPerPurchase</c>.
    /// </summary>
    [HttpPost("shop/listings/{listingId:int}/purchase")]
    [Tags("Student – Shop")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ShopViewResponse>> PurchaseShopListing(int listingId, CancellationToken ct = default)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();

        // Settle open mandatory periods first: any penalty must reduce the coin balance BEFORE the funds check
        // applies - otherwise the child could dodge the stick by buying quickly.
        await progress.SettleClosedPeriodsAsync(cid.Value, DateOnly.FromDateTime(DateTime.UtcNow), ct);

        var result = await shop.PurchaseAsync(cid.Value, listingId, DateTime.UtcNow, ct);
        return result.Error switch
        {
            // The freshly bought item sorts first anyway (Owned-first, then newest) - the first page is right.
            ShopService.ShopError.None => await ShopViewAsync(cid.Value, 0, PagingExtensions.DefaultTake, ct),
            ShopService.ShopError.NotFound => this.ProblemWithCode(ShopService.ToApiError(result.Error), "Shop listing not found."),
            ShopService.ShopError.ListingInactive => this.ProblemWithCode(ShopService.ToApiError(result.Error), "This shop listing is no longer available."),
            ShopService.ShopError.InsufficientStock => this.ProblemWithCode(ShopService.ToApiError(result.Error), "This shop listing is out of stock."),
            ShopService.ShopError.InsufficientCoins => this.ProblemWithCode(ShopService.ToApiError(result.Error), "Not enough coins for this shop listing."),
            ShopService.ShopError.InsufficientGems => this.ProblemWithCode(ShopService.ToApiError(result.Error), "Not enough gems for this shop listing."),
            _ => this.ProblemWithCode(ShopService.ToApiError(result.Error), "Purchase conflicted with a concurrent action — please try again."),
        };
    }

    /// <summary>
    /// Own aggregated inventory: the total available quantity per article type (only what is &gt; 0).
    /// Counterpart to the activation <c>POST</c> and the father view (<c>children/{childId}/shop/inventory</c>);
    /// the same data is also available bundled in <c>GET me/shop</c>.
    /// </summary>
    /// <param name="skip">Number of entries skipped (offset, default 0).</param>
    /// <param name="take">Maximum entries (1..500). Total count in the <c>X-Total-Count</c> header.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("shop/inventory")]
    [Tags("Student – Shop")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<MyInventoryItemResponse>>> MyInventory(
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake,
        CancellationToken ct = default)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();

        // From the snapshot, not from the navigation: paid units survive the deletion of the article (FK
        // SetNull), and `ShopArticle!.ArticleNumber` would then be NULL - the sort would silently have pulled
        // the position to the front and the display would be nameless.
        return await db.ChildInventories.AsNoTracking()
            .Where(i => i.ChildId == cid && i.Quantity > 0)
            .OrderBy(i => i.ArticleNumber)
            .Select(i => new MyInventoryItemResponse(
                i.ShopArticleId, i.ArticleNumber, i.ArticleTitle,
                i.UnitType, i.ActionType, i.Quantity))
            .ToPagedListAsync(Response, skip, take, ct);
    }

    /// <summary>
    /// Submits an activation request: the child wants to consume <c>quantity</c> units of the article.
    /// The father approves or rejects; the inventory is only reduced on approval.
    /// </summary>
    [HttpPost("shop/inventory/{articleId:int}/activate")]
    [Tags("Student – Shop")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MyActivationResponse>> RequestActivation(
        int articleId, [FromBody] ActivateDto dto, CancellationToken ct = default)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();
        if (dto.Quantity <= 0) return this.ProblemWithCode(ApiErrors.ValidationError, "Quantity must be at least 1.");

        var result = await shop.RequestActivationAsync(cid.Value, articleId, dto.Quantity, DateTime.UtcNow, ct);
        return result.Error switch
        {
            ShopService.ShopError.None => MapActivation(result.Value!),
            ShopService.ShopError.NotFound => this.ProblemWithCode(ShopService.ToApiError(result.Error), "Article not found in your family shop."),
            ShopService.ShopError.InsufficientInventory => this.ProblemWithCode(ShopService.ToApiError(result.Error), "Not enough units in your inventory."),
            ShopService.ShopError.InvalidQuantity => this.ProblemWithCode(ShopService.ToApiError(result.Error), "Quantity must be at least 1."),
            _ => this.ProblemWithCode(ShopService.ToApiError(result.Error), "The activation request could not be saved — please try again."),
        };
    }


    /// <summary>Own activation requests (newest first), optionally filtered by status.</summary>
    [HttpGet("shop/activations")]
    [Tags("Student – Shop")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<MyActivationResponse>>> MyActivations(
        [FromQuery] ActivationRequestStatus? status,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake,
        CancellationToken ct = default)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();

        var query = db.ActivationRequests.AsNoTracking().Where(r => r.ChildId == cid);
        if (status is not null) query = query.Where(r => r.Status == status);

        return await query
            .OrderBy(r => r.Status == ActivationRequestStatus.Pending ? 0 : 1)
            .ThenByDescending(r => r.RequestedAt)
            .Select(r => MapActivation(r))
            .ToPagedListAsync(Response, skip, take, ct);
    }

    private static MyActivationResponse MapActivation(ActivationRequest r) =>
        new(r.Id, r.ShopArticleId, r.ArticleTitle, r.UnitType, r.ActionType,
            r.RequestedQuantity, r.Status, r.RequestedAt, r.ClosedAt);

    private async Task<ShopViewResponse> ShopViewAsync(int childId, int purchaseSkip, int purchaseTake, CancellationToken ct)
    {
        var balances = await wallet.BalancesAsync(childId, ct);
        var now = DateTime.UtcNow;

        // The child's shared shop view: listings of ALL its supervisors.
        var listings = await shop.ListingsForStudentAsync(childId, activeOnly: true, now, ct);
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

        // As in `MyInventory`: the snapshot carries the display, which makes the `Include` obsolete.
        var inventory = await db.ChildInventories.AsNoTracking()
            .Where(i => i.ChildId == childId && i.Quantity > 0)
            .OrderBy(i => i.ArticleNumber)
            .Select(i => new MyInventoryItemResponse(
                i.ShopArticleId, i.ArticleNumber, i.ArticleTitle,
                i.UnitType, i.ActionType, i.Quantity))
            .ToListAsync(ct);

        // Paged instead of a fixed cutoff (B-99): the history otherwise ended silently once a child had
        // bought enough - X-Total-Count lets the frontend show "51 of 137" and load the rest.
        var purchases = await db.ShopPurchases.AsNoTracking()
            .Where(p => p.ChildId == childId)
            .OrderBy(p => p.Status == ShopPurchaseStatus.Owned ? 0 : 1)
            .ThenByDescending(p => p.PurchasedAt).ThenByDescending(p => p.Id)
            .Select(p => new MyShopPurchaseResponse(
                p.Id, p.ShopListingId, p.ArticleNumber, p.Title,
                p.CoinPrice, p.GemPrice, p.UnitsPerPurchase, p.Status, p.PurchasedAt, p.ClosedAt))
            .ToPagedListAsync(Response, purchaseSkip, purchaseTake, ct);

        return new ShopViewResponse(balances.Coins, balances.Gems, available, inventory, purchases);
    }

    private async Task<SkinStateResponse> SkinStateAsync(int childId, CancellationToken ct)
    {
        var child = await db.Children.AsNoTracking().FirstAsync(c => c.Id == childId, ct);
        var gems = await wallet.GemsAsync(childId, ct);
        return new SkinStateResponse(gems, child.SelectedSkin, child.OwnedSkins);
    }
}
