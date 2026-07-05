using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Models;
using Pugling.Api.Services;

namespace Pugling.Api.Controllers.Admin;

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
    public record RedemptionDto(int Id, int ChildId, int? RewardId, string Title, int Cost,
        RewardRedemptionStatus Status, DateTime PurchasedAt, DateTime? FulfilledAt);

    private static RewardDto Map(Reward r) => new(r.Id, r.Title, r.Cost, r.Period, r.Quantity, r.Active,
        r.StudyPlanId, r.ExerciseId, r.StudyPlan?.Title, r.Exercise?.Title);
    private static RedemptionDto MapRedemption(RewardRedemption r) =>
        new(r.Id, r.ChildId, r.RewardId, r.Title, r.Cost, r.Status, r.PurchasedAt, r.FulfilledAt);

    /// <summary>Alle Angebote des Kindes (Definitionen zur Verwaltung).</summary>
    [HttpGet]
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
        if (string.IsNullOrWhiteSpace(dto.Title)) return Problem(statusCode: 400, detail: "Title ist erforderlich.");
        if (dto.Cost <= 0) return Problem(statusCode: 400, detail: "Cost muss positiv sein.");
        if (dto.Quantity < 1) return Problem(statusCode: 400, detail: "Quantity muss mindestens 1 sein.");
        // Kontext-Bezug prüfen: der Plan muss diesem Kind gehören, die Übung muss existieren.
        if (dto.StudyPlanId is { } spid && !await db.StudyPlans.AnyAsync(p => p.Id == spid && p.ChildId == childId))
            return Problem(statusCode: 400, detail: "Lehrplan gehört nicht zu diesem Kind.");
        if (dto.ExerciseId is { } exid && !await db.Exercises.AnyAsync(e => e.Id == exid))
            return Problem(statusCode: 400, detail: "Übung nicht gefunden.");

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

    /// <summary>Ändert ein Angebot (partiell).</summary>
    [HttpPatch("{rewardId:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RewardDto>> Update(int childId, int rewardId, UpdateRewardDto dto)
    {
        var reward = await db.Rewards.FirstOrDefaultAsync(r => r.Id == rewardId && r.ChildId == childId);
        if (reward is null) return NotFound();
        if (dto.Quantity is < 1) return Problem(statusCode: 400, detail: "Quantity muss mindestens 1 sein.");

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
    [HttpGet("redemptions")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<RedemptionDto>>> Redemptions(
        int childId, [FromQuery] RewardRedemptionStatus? status)
    {
        var query = db.RewardRedemptions.AsNoTracking().Where(r => r.ChildId == childId);
        if (status is not null) query = query.Where(r => r.Status == status);

        return await query
            .OrderBy(r => r.Status == RewardRedemptionStatus.Purchased ? 0 : 1)
            .ThenByDescending(r => r.PurchasedAt)
            .Select(r => MapRedemption(r)).ToListAsync();
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
        OfferService.OfferError.NotOpen => Problem(statusCode: 409, detail: "Dieser Kauf ist nicht mehr offen."),
        _ => Problem(statusCode: 409, detail: "Der Vorgang konnte nicht abgeschlossen werden."),
    };
}
