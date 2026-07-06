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
    WalletService wallet, OfferService offers) : ControllerBase
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

    /// <summary>Eigener Skin-Zustand: Münzstand, ausgerüsteter Skin und freigeschaltete Skins.</summary>
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

    private async Task<SkinStateResponse> SkinStateAsync(int childId)
    {
        var child = await db.Children.AsNoTracking().FirstAsync(c => c.Id == childId);
        var gems = await wallet.GemsAsync(childId);
        return new SkinStateResponse(gems, child.SelectedSkin, child.OwnedSkins);
    }
}
