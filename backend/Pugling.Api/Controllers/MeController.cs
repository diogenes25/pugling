using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
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
public class MeController(PuglingDbContext db, GamificationService gamification) : ControllerBase
{
    /// <summary>Eine einzelne Punkte-Buchung (Gutschrift positiv, Abzug negativ) mit Kategorie.</summary>
    public record PointsEntryResponse(int Id, int Amount, PointKind Kind, string Reason, DateTime CreatedAt);
    /// <summary>Punktestand (Wallet) des Kindes samt der letzten Buchungen.</summary>
    public record WalletResponse(int ChildId, int Balance, IReadOnlyList<PointsEntryResponse> Entries);

    /// <summary>Skin-Zustand des Kindes: aktueller Münzstand, ausgerüsteter und freigeschaltete Skins.</summary>
    public record SkinStateResponse(int Balance, string Selected, IReadOnlyList<string> Owned);

    /// <summary>Eine einlösbare Prämie aus Sohn-Sicht: Titel, Preis, ob bezahlbar und ob bereits offen angefragt.</summary>
    public record RewardOfferResponse(int Id, string Title, int Cost, bool Affordable, bool AlreadyRequested);
    /// <summary>Eine eigene Einlöse-Anfrage mit aktuellem Status.</summary>
    public record MyRedemptionResponse(int Id, int? RewardId, string Title, int Cost,
        RewardRedemptionStatus Status, DateTime RequestedAt, DateTime? DecidedAt);
    /// <summary>Prämien-Sicht des Sohns: Münzstand, verfügbare Prämien und eigene Anfragen.</summary>
    public record RewardsViewResponse(int Balance, IReadOnlyList<RewardOfferResponse> Available,
        IReadOnlyList<MyRedemptionResponse> Redemptions);

    /// <summary>Eigener Punktestand (Wallet) samt der letzten Buchungen.</summary>
    [HttpGet("points")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WalletResponse>> Points()
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();

        var entries = await db.ChildPoints
            .AsNoTracking()
            .Where(p => p.ChildId == cid)
            .OrderByDescending(p => p.CreatedAt)
            .Take(50)
            .Select(p => new PointsEntryResponse(p.Id, p.Amount, p.Kind, p.Reason, p.CreatedAt))
            .ToListAsync();

        var balance = await db.ChildPoints.Where(p => p.ChildId == cid).SumAsync(p => (int?)p.Amount) ?? 0;
        return new WalletResponse(cid.Value, balance, entries);
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
        if (cost is null) return Problem(statusCode: 404, detail: $"Unbekannter Skin '{skinId}'.");

        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == cid);
        if (child is null) return Forbid();
        if (child.OwnedSkins.Contains(skinId))
            return Problem(statusCode: 409, detail: "Dieser Skin ist bereits freigeschaltet.");

        var balance = await db.ChildPoints.Where(p => p.ChildId == cid).SumAsync(p => (int?)p.Amount) ?? 0;
        if (balance < cost)
            return Problem(statusCode: 400, detail: $"Zu wenig Münzen: {balance}/{cost} für '{skinId}'.");

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
            return Problem(statusCode: 409, detail: "Kauf kollidierte mit einer parallelen Aktion – bitte erneut versuchen.");

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
            return Problem(statusCode: 400, detail: "Dieser Skin ist noch nicht freigeschaltet.");

        child.SelectedSkin = skinId;
        child.ConcurrencyStamp = Guid.NewGuid();

        if (!await TrySaveAsync())
            return Problem(statusCode: 409, detail: "Ausrüsten kollidierte mit einer parallelen Aktion – bitte erneut versuchen.");
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

    /// <summary>Eigene Prämien-Sicht: Münzstand, verfügbare (aktive) Prämien und die eigenen Einlöse-Anfragen.</summary>
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
    /// Fragt eine Prämie zum Einlösen an. Es wird noch <b>nichts abgebucht</b> – der Vater entscheidet
    /// (siehe Admin-<c>RewardsController</c>). Doppelte offene Anfragen für dieselbe Prämie sind gesperrt.
    /// </summary>
    [HttpPost("rewards/{rewardId:int}/redeem")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RewardsViewResponse>> Redeem(int rewardId)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();

        var reward = await db.Rewards.FirstOrDefaultAsync(r => r.Id == rewardId && r.ChildId == cid);
        if (reward is null) return Problem(statusCode: 404, detail: "Prämie nicht gefunden.");
        if (!reward.Active) return Problem(statusCode: 400, detail: "Diese Prämie ist nicht (mehr) verfügbar.");

        var alreadyOpen = await db.RewardRedemptions.AnyAsync(r =>
            r.ChildId == cid && r.RewardId == rewardId && r.Status == RewardRedemptionStatus.Requested);
        if (alreadyOpen) return Problem(statusCode: 409, detail: "Diese Prämie ist bereits angefragt und wartet auf Papa.");

        db.RewardRedemptions.Add(new RewardRedemption
        {
            ChildId = cid.Value,
            RewardId = reward.Id,
            Title = reward.Title,   // Momentaufnahme, stabil auch bei späterer Änderung/Löschung
            Cost = reward.Cost,
        });
        await db.SaveChangesAsync();
        return await RewardsViewAsync(cid.Value);
    }

    private async Task<RewardsViewResponse> RewardsViewAsync(int childId)
    {
        var balance = await db.ChildPoints.Where(p => p.ChildId == childId).SumAsync(p => (int?)p.Amount) ?? 0;

        var openRewardIds = await db.RewardRedemptions
            .Where(r => r.ChildId == childId && r.Status == RewardRedemptionStatus.Requested && r.RewardId != null)
            .Select(r => r.RewardId!.Value).ToListAsync();

        var available = await db.Rewards.AsNoTracking()
            .Where(r => r.ChildId == childId && r.Active)
            .OrderBy(r => r.Cost).ThenBy(r => r.Id)
            .Select(r => new RewardOfferResponse(r.Id, r.Title, r.Cost, balance >= r.Cost, openRewardIds.Contains(r.Id)))
            .ToListAsync();

        var redemptions = await db.RewardRedemptions.AsNoTracking()
            .Where(r => r.ChildId == childId)
            .OrderBy(r => r.Status == RewardRedemptionStatus.Requested ? 0 : 1)
            .ThenByDescending(r => r.RequestedAt)
            .Select(r => new MyRedemptionResponse(r.Id, r.RewardId, r.Title, r.Cost, r.Status, r.RequestedAt, r.DecidedAt))
            .ToListAsync();

        return new RewardsViewResponse(balance, available, redemptions);
    }

    private async Task<SkinStateResponse> SkinStateAsync(int childId)
    {
        var child = await db.Children.AsNoTracking().FirstAsync(c => c.Id == childId);
        var balance = await db.ChildPoints.Where(p => p.ChildId == childId).SumAsync(p => (int?)p.Amount) ?? 0;
        return new SkinStateResponse(balance, child.SelectedSkin, child.OwnedSkins);
    }
}
