using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Student;

/// <summary>
/// Selbstauskunft für den angemeldeten Sohn: eigener Punktestand (Wallet) und Kurzprofil.
/// Schließt die Lücke, dass der kontoübergreifende Punktestand sonst nur der Vater lesen kann
/// (<see cref="Supervisor.ChildrenController"/> ist <c>Vater</c>-only).
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
    /// <summary>Eigener Kontostand (Münzen + Gems). Die einzelnen Buchungen liegen unter <c>points/entries</c>.</summary>
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

    /// <summary>Eigene Punkte-Buchungen (neueste zuerst), seitenweise.</summary>
    /// <param name="skip">Anzahl zu überspringender Buchungen (Paging).</param>
    /// <param name="take">Maximale Buchungszahl (1..500). Gesamtzahl im Header <c>X-Total-Count</c>.</param>
    /// <param name="ct">Abbruch-Token.</param>
    [HttpGet("points/entries")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<MyPointsEntryResponse>>> PointsEntries(
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake,
        CancellationToken ct = default)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();

        return await db.ChildPoints
            .AsNoTracking()
            .Where(p => p.ChildId == cid)
            .OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
            .Select(p => new MyPointsEntryResponse(p.Id, p.Amount, p.Kind, p.Reason, p.CreatedAt))
            .ToPagedListAsync(Response, skip, take, ct);
    }

    /// <summary>Eine einzelne eigene Punkte-Buchung (Einzelansicht zur Liste unter <c>points/entries</c>).</summary>
    [HttpGet("points/entries/{entryId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MyPointsEntryResponse>> PointsEntry(int entryId, CancellationToken ct = default)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();

        var entry = await db.ChildPoints
            .AsNoTracking()
            .Where(p => p.Id == entryId && p.ChildId == cid)
            .Select(p => new MyPointsEntryResponse(p.Id, p.Amount, p.Kind, p.Reason, p.CreatedAt))
            .FirstOrDefaultAsync(ct);

        return entry is null ? NotFound() : entry;
    }

    /// <summary>Eigene Missionen (Tages-/Wochen-/Zusatzziele) mit aktuellem Fortschritt (reine Lesesicht), seitenweise.</summary>
    /// <param name="skip">Anzahl zu überspringender Missionen (Paging).</param>
    /// <param name="take">Maximale Missions-Zahl (1..500). Gesamtzahl im Header <c>X-Total-Count</c>.</param>
    /// <param name="ct">Abbruch-Token.</param>
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

    /// <summary>Eine einzelne eigene Mission (Einzelansicht zur Liste unter <c>missions</c>).</summary>
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

    /// <summary>Eigene Auszeichnungen (Badges): erreichte und noch offene, erreichte zuerst (reine Lesesicht), seitenweise.</summary>
    /// <param name="skip">Anzahl zu überspringender Auszeichnungen (Paging).</param>
    /// <param name="take">Maximale Auszeichnungs-Zahl (1..500). Gesamtzahl im Header <c>X-Total-Count</c>.</param>
    /// <param name="ct">Abbruch-Token.</param>
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

    /// <summary>Eine einzelne eigene Auszeichnung (Einzelansicht zur Liste unter <c>achievements</c>).</summary>
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

    /// <summary>Eigener Skin-Zustand: Gem-Stand, ausgerüsteter Skin und freigeschaltete Skins.</summary>
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

        if (!await TrySaveAsync(ct))
            return this.ProblemWithCode(ApiErrors.ConcurrencyConflict, "Purchase conflicted with a concurrent action — please try again.");

        return await SkinStateAsync(cid.Value, ct);
    }

    /// <summary>Rüstet einen bereits freigeschalteten Skin aus (persistiert geräteübergreifend am Kind).</summary>
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

    /// <summary>Speichert und fängt eine Nebenläufigkeits-Kollision (Token) ab: false = kollidiert, nichts committet.</summary>
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
    /// Familien-Shop: aktive Angebote des Vaters, aggregiertes Inventar und Kaufhistorie des Sohns.
    /// </summary>
    [HttpGet("shop")]
    [Tags("Student – Shop")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ShopViewResponse>> Shop(CancellationToken ct = default)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();
        return await ShopViewAsync(cid.Value, ct);
    }

    /// <summary>
    /// Kauft ein Familien-Shop-Angebot: Coins/Gems werden sofort abgebucht, das aggregierte Inventar
    /// des Sohns für den zugehörigen Artikel wird um <c>UnitsPerPurchase</c> erhöht.
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

        // Erst offene Pflicht-Perioden abrechnen: ein etwaiger Malus muss den Münz-Saldo mindern, BEVOR der
        // Deckungs-Check greift – sonst könnte der Sohn den Stick durch schnelles Kaufen umgehen.
        await progress.SettleClosedPeriodsAsync(cid.Value, DateOnly.FromDateTime(DateTime.UtcNow), ct);

        var result = await shop.PurchaseAsync(cid.Value, listingId, DateTime.UtcNow, ct);
        return result.Error switch
        {
            ShopService.ShopError.None => await ShopViewAsync(cid.Value, ct),
            ShopService.ShopError.NotFound => this.ProblemWithCode(ShopService.ToApiError(result.Error), "Shop listing not found."),
            ShopService.ShopError.ListingInactive => this.ProblemWithCode(ShopService.ToApiError(result.Error), "This shop listing is no longer available."),
            ShopService.ShopError.InsufficientStock => this.ProblemWithCode(ShopService.ToApiError(result.Error), "This shop listing is out of stock."),
            ShopService.ShopError.InsufficientCoins => this.ProblemWithCode(ShopService.ToApiError(result.Error), "Not enough coins for this shop listing."),
            ShopService.ShopError.InsufficientGems => this.ProblemWithCode(ShopService.ToApiError(result.Error), "Not enough gems for this shop listing."),
            _ => this.ProblemWithCode(ShopService.ToApiError(result.Error), "Purchase conflicted with a concurrent action — please try again."),
        };
    }

    /// <summary>
    /// Eigenes aggregiertes Inventar: pro Artikel-Typ die verfügbare Gesamtmenge (nur was &gt; 0 ist).
    /// Gegenstück zum Aktivierungs-<c>POST</c> und zur Vater-Sicht (<c>children/{childId}/shop/inventory</c>);
    /// dieselben Daten stehen gebündelt auch in <c>GET me/shop</c>.
    /// </summary>
    /// <param name="skip">Anzahl übersprungener Einträge (Offset, Standard 0).</param>
    /// <param name="take">Maximale Einträge (1..500). Gesamtzahl im Header <c>X-Total-Count</c>.</param>
    /// <param name="ct">Abbruch-Token.</param>
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

        return await db.ChildInventories.AsNoTracking()
            .Where(i => i.ChildId == cid && i.Quantity > 0)
            .OrderBy(i => i.ShopArticle!.ArticleNumber)
            .Select(i => new MyInventoryItemResponse(
                i.ShopArticleId, i.ShopArticle!.ArticleNumber, i.ShopArticle!.Title,
                i.ShopArticle!.UnitType, i.ShopArticle!.ActionType, i.Quantity))
            .ToPagedListAsync(Response, skip, take, ct);
    }

    /// <summary>
    /// Stellt eine Aktivierungsanfrage: der Sohn möchte <c>quantity</c> Einheiten des Artikels
    /// verbrauchen. Der Vater genehmigt oder lehnt ab; das Inventar wird erst bei Genehmigung reduziert.
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


    /// <summary>Eigene Aktivierungsanfragen (neueste zuerst), optional nach Status gefiltert.</summary>
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

    private async Task<ShopViewResponse> ShopViewAsync(int childId, CancellationToken ct)
    {
        var balances = await wallet.BalancesAsync(childId, ct);
        var now = DateTime.UtcNow;

        // Gemeinsame Shop-Sicht des Kindes: Angebote ALLER seiner Supervisor.
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

        var inventory = await db.ChildInventories.AsNoTracking()
            .Include(i => i.ShopArticle)
            .Where(i => i.ChildId == childId && i.Quantity > 0)
            .OrderBy(i => i.ShopArticle!.ArticleNumber)
            .Select(i => new MyInventoryItemResponse(
                i.ShopArticleId, i.ShopArticle!.ArticleNumber, i.ShopArticle.Title,
                i.ShopArticle.UnitType, i.ShopArticle.ActionType, i.Quantity))
            .ToListAsync(ct);

        var purchases = await db.ShopPurchases.AsNoTracking()
            .Where(p => p.ChildId == childId)
            .OrderBy(p => p.Status == ShopPurchaseStatus.Owned ? 0 : 1)
            .ThenByDescending(p => p.PurchasedAt).ThenByDescending(p => p.Id)
            .Select(p => new MyShopPurchaseResponse(
                p.Id, p.ShopListingId, p.ArticleNumber, p.Title,
                p.CoinPrice, p.GemPrice, p.UnitsPerPurchase, p.Status, p.PurchasedAt, p.ClosedAt))
            .Take(50)
            .ToListAsync(ct);

        return new ShopViewResponse(balances.Coins, balances.Gems, available, inventory, purchases);
    }

    private async Task<SkinStateResponse> SkinStateAsync(int childId, CancellationToken ct)
    {
        var child = await db.Children.AsNoTracking().FirstAsync(c => c.Id == childId, ct);
        var gems = await wallet.GemsAsync(childId, ct);
        return new SkinStateResponse(gems, child.SelectedSkin, child.OwnedSkins);
    }
}
