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
/// Kaufbare Angebote eines Kindes verwalten (nur Vater, nur eigene Kinder): reale Belohnungen wie
/// Spielzeit oder Taschengeld mit Münz-Preis, Wiederkehr und Kontingent. Der Sohn kauft über
/// <c>api/v1/me/rewards</c> direkt (Münzen werden sofort abgebucht); der Vater <b>erfüllt</b> den Kauf
/// hier real oder <b>storniert</b> ihn (Rückerstattung). Eigentum sichert der <see cref="ChildOwnershipFilter"/>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.V1 + "/children/{childId:int}/rewards")]
[Tags("Admin – Rewards")]
[Produces("application/json")]
[Authorize(Roles = Roles.Vater)]
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class RewardsController(PuglingDbContext db, OfferService offers) : ControllerBase
{
    public record RewardDto(int Id, string Title, int Cost, OfferPeriod Period, int Quantity, bool Active,
        int? StudyPlanId, int? ExerciseId, string? PlanTitle, string? ExerciseTitle);

    /// <summary>
    /// Ein Kauf in der Vater-Sicht. <c>CanFulfill</c>/<c>CanCancel</c> sind server-autoritative
    /// Affordances: Sie sagen dem Frontend, ob die Aktionen <c>fulfill</c>/<c>cancel</c> für diesen Kauf gerade zulässig
    /// sind (nur bei offenem Kauf, Status <see cref="RewardRedemptionStatus.Purchased"/>), damit die Buttons nicht die
    /// Server-Regel nachbilden müssen.
    /// </summary>
    public record RedemptionDto(int Id, int ChildId, int? RewardId, string Title, int Cost,
        RewardRedemptionStatus Status, DateTime PurchasedAt, DateTime? FulfilledAt)
    {
        /// <summary>Darf der Vater diesen Kauf jetzt als erfüllt markieren? (nur solange er offen ist)</summary>
        public bool CanFulfill { get; init; }
        /// <summary>Darf der Vater diesen Kauf jetzt stornieren (Rückerstattung)? (nur solange er offen ist)</summary>
        public bool CanCancel { get; init; }
    }

    private static RewardDto Map(Reward r) => new(r.Id, r.Title, r.Cost, r.Period, r.Quantity, r.Active,
        r.StudyPlanId, r.ExerciseId, r.StudyPlan?.Title, r.Exercise?.Title);
    private static RedemptionDto MapRedemption(RewardRedemption r) =>
        new(r.Id, r.ChildId, r.RewardId, r.Title, r.Cost, r.Status, r.PurchasedAt, r.FulfilledAt)
        {
            // Offen = erfüll- und stornierbar (dieselbe Bedingung wie in OfferService.Fulfill/Cancel).
            CanFulfill = r.Status == RewardRedemptionStatus.Purchased,
            CanCancel = r.Status == RewardRedemptionStatus.Purchased,
        };

    /// <summary>Alle Angebote des Kindes (Definitionen zur Verwaltung).</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<RewardDto>>> List(int childId)
    {
        var rewards = await db.Rewards.AsNoTracking()
            .Include(r => r.StudyPlan).Include(r => r.Exercise)
            .Where(r => r.ChildId == childId)
            .OrderByDescending(r => r.Active).ThenBy(r => r.Cost).ThenBy(r => r.Id)
            .ToListAsync();
        return rewards.Select(Map).ToList();
    }

    /// <summary>
    /// Anlegen eines Angebots. <paramref name="StudyPlanId"/>/<paramref name="ExerciseId"/> sind optional und
    /// binden das Angebot an einen Plan bzw. eine Übung des Kindes (null = kindweit für alles gültig).
    /// </summary>
    public record CreateRewardDto(string Title, int Cost, OfferPeriod Period = OfferPeriod.OneOff, int Quantity = 1,
        int? StudyPlanId = null, int? ExerciseId = null);

    /// <summary>Legt ein kaufbares Angebot für das Kind an (mit Wiederkehr, Kontingent und optionalem Plan-/Übungs-Kontext).</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RewardDto>> Create(int childId, CreateRewardDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title)) return this.ProblemWithCode(ApiErrors.ValidationError, "Title is required.");
        if (dto.Cost <= 0) return this.ProblemWithCode(ApiErrors.ValidationError, "Cost must be positive.");
        if (dto.Quantity < 1) return this.ProblemWithCode(ApiErrors.ValidationError, "Quantity must be at least 1.");
        // Kontext-Bezug prüfen: der Plan muss diesem Kind gehören, die Übung muss existieren.
        if (dto.StudyPlanId is { } spid && !await db.StudyPlans.AnyAsync(p => p.Id == spid && p.ChildId == childId))
            return this.ProblemWithCode(ApiErrors.InvalidReference, "The study plan does not belong to this child.");
        if (dto.ExerciseId is { } exid && !await db.Exercises.AnyAsync(e => e.Id == exid))
            return this.ProblemWithCode(ApiErrors.InvalidReference, "Exercise not found.");

        var reward = new Reward
        {
            ChildId = childId,
            Title = dto.Title.Trim(),
            Cost = dto.Cost,
            Period = dto.Period,
            Quantity = dto.Quantity,
            StudyPlanId = dto.StudyPlanId,
            ExerciseId = dto.ExerciseId,
        };
        db.Rewards.Add(reward);
        await db.SaveChangesAsync();
        await LoadContextAsync(reward);
        return CreatedAtAction(nameof(List), new { childId }, Map(reward));
    }

    /// <summary>Lädt Plan-/Übungs-Kontext einer getrackten Prämie für die Antwort-Projektion nach.</summary>
    private async Task LoadContextAsync(Reward reward)
    {
        if (reward.StudyPlanId is not null) await db.Entry(reward).Reference(r => r.StudyPlan).LoadAsync();
        if (reward.ExerciseId is not null) await db.Entry(reward).Reference(r => r.Exercise).LoadAsync();
    }

    public record UpdateRewardDto(string? Title, int? Cost, OfferPeriod? Period, int? Quantity, bool? Active);

    /// <summary>Ändert ein Angebot (partiell). Title darf nicht leer sein, Cost muss positiv sein, Quantity mindestens 1.</summary>
    [HttpPatch("{rewardId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RewardDto>> Update(int childId, int rewardId, UpdateRewardDto dto)
    {
        var reward = await db.Rewards.FirstOrDefaultAsync(r => r.Id == rewardId && r.ChildId == childId);
        if (reward is null) return NotFound();
        if (dto.Title is not null && string.IsNullOrWhiteSpace(dto.Title))
            return this.ProblemWithCode(ApiErrors.ValidationError, "Title must not be empty.");
        if (dto.Cost is <= 0) return this.ProblemWithCode(ApiErrors.ValidationError, "Cost must be positive.");
        if (dto.Quantity is < 1) return this.ProblemWithCode(ApiErrors.ValidationError, "Quantity must be at least 1.");

        if (dto.Title is not null) reward.Title = dto.Title.Trim();
        if (dto.Cost is > 0) reward.Cost = dto.Cost.Value;
        if (dto.Period is not null) reward.Period = dto.Period.Value;
        if (dto.Quantity is not null) reward.Quantity = dto.Quantity.Value;
        if (dto.Active is not null) reward.Active = dto.Active.Value;
        await db.SaveChangesAsync();
        await LoadContextAsync(reward);
        return Map(reward);
    }

    /// <summary>Löscht ein Angebot. Bereits getätigte Käufe bleiben (mit Titel/Kosten als Momentaufnahme) im Konto erhalten.</summary>
    [HttpDelete("{rewardId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int childId, int rewardId)
    {
        var reward = await db.Rewards.FirstOrDefaultAsync(r => r.Id == rewardId && r.ChildId == childId);
        if (reward is null) return NotFound();
        db.Rewards.Remove(reward);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ---- Käufe im Konto: Vater-Sicht und Erfüllung ----

    /// <summary>Käufe des Kindes, optional nach Status gefiltert (offene zuerst, dann neueste).</summary>
    /// <param name="childId">Kind, dessen Käufe gelesen werden.</param>
    /// <param name="status">Optionaler Statusfilter.</param>
    /// <param name="skip">Anzahl zu überspringender Käufe (Paging).</param>
    /// <param name="take">Maximale Käufe-Zahl (1..500). Gesamtzahl im Header <c>X-Total-Count</c>.</param>
    [HttpGet("redemptions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<RedemptionDto>>> Redemptions(
        int childId, [FromQuery] RewardRedemptionStatus? status,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake)
    {
        var query = db.RewardRedemptions.AsNoTracking().Where(r => r.ChildId == childId);
        if (status is not null) query = query.Where(r => r.Status == status);

        return await query
            .OrderBy(r => r.Status == RewardRedemptionStatus.Purchased ? 0 : 1)
            .ThenByDescending(r => r.PurchasedAt).ThenByDescending(r => r.Id)
            .Select(r => MapRedemption(r)).ToPagedListAsync(Response, skip, take);
    }

    /// <summary>Markiert einen offenen Kauf als real erfüllt (der Vater hat seinen Teil der Abmachung erbracht).</summary>
    [HttpPost("redemptions/{redemptionId:int}/fulfill")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RedemptionDto>> Fulfill(int childId, int redemptionId) =>
        ToResult(await offers.FulfillAsync(childId, redemptionId, DateTime.UtcNow));

    /// <summary>Storniert einen offenen Kauf und erstattet die Münzen zurück (Kontingent-Slot wird wieder frei).</summary>
    [HttpPost("redemptions/{redemptionId:int}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RedemptionDto>> Cancel(int childId, int redemptionId) =>
        ToResult(await offers.CancelAsync(childId, redemptionId, DateTime.UtcNow));

    /// <summary>Übersetzt das Service-Ergebnis in eine HTTP-Antwort (einheitliche Fehler-Behandlung).</summary>
    private ActionResult<RedemptionDto> ToResult(OfferService.Result result) => result.Error switch
    {
        OfferService.OfferError.None => MapRedemption(result.Redemption!),
        OfferService.OfferError.NotFound => NotFound(),
        OfferService.OfferError.NotOpen => this.ProblemWithCode(OfferService.ToApiError(result.Error), "This purchase is no longer open."),
        _ => this.ProblemWithCode(OfferService.ToApiError(result.Error), "The operation could not be completed."),
    };
}
