using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Admin;

/// <summary>
/// Einlösbare Prämien eines Kindes verwalten (nur Vater, nur eigene Kinder): reale Belohnungen wie
/// Fernseh-/Spielzeit mit Münz-Preis. Der Sohn fragt eine Einlösung über <c>api/v1/me/rewards</c> an;
/// der Vater genehmigt sie hier (dann wird abgebucht) oder lehnt sie ab. Eigentum sichert der
/// <see cref="ChildOwnershipFilter"/>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.V1 + "/children/{childId:int}/rewards")]
[Tags("Admin – Rewards")]
[Produces("application/json")]
[Authorize(Roles = Roles.Vater)]
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class RewardsController(PuglingDbContext db) : ControllerBase
{
    public record RewardDto(int Id, string Title, int Cost, bool Active);
    public record RedemptionDto(int Id, int ChildId, int? RewardId, string Title, int Cost,
        RewardRedemptionStatus Status, DateTime RequestedAt, DateTime? DecidedAt);

    private static RewardDto Map(Reward r) => new(r.Id, r.Title, r.Cost, r.Active);
    private static RedemptionDto MapRedemption(RewardRedemption r) =>
        new(r.Id, r.ChildId, r.RewardId, r.Title, r.Cost, r.Status, r.RequestedAt, r.DecidedAt);

    /// <summary>Alle Prämien des Kindes (Definitionen zur Verwaltung).</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<RewardDto>>> List(int childId) =>
        await db.Rewards.AsNoTracking().Where(r => r.ChildId == childId)
            .OrderByDescending(r => r.Active).ThenBy(r => r.Cost).ThenBy(r => r.Id)
            .Select(r => Map(r)).ToListAsync();

    public record CreateRewardDto(string Title, int Cost);

    /// <summary>Legt eine einlösbare Prämie für das Kind an.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RewardDto>> Create(int childId, CreateRewardDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title)) return Problem(statusCode: 400, detail: "Title ist erforderlich.");
        if (dto.Cost <= 0) return Problem(statusCode: 400, detail: "Cost muss positiv sein.");

        var reward = new Reward { ChildId = childId, Title = dto.Title.Trim(), Cost = dto.Cost };
        db.Rewards.Add(reward);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(List), new { childId }, Map(reward));
    }

    public record UpdateRewardDto(string? Title, int? Cost, bool? Active);

    /// <summary>Ändert eine Prämie (partiell).</summary>
    [HttpPatch("{rewardId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RewardDto>> Update(int childId, int rewardId, UpdateRewardDto dto)
    {
        var reward = await db.Rewards.FirstOrDefaultAsync(r => r.Id == rewardId && r.ChildId == childId);
        if (reward is null) return NotFound();

        if (dto.Title is not null) reward.Title = dto.Title.Trim();
        if (dto.Cost is > 0) reward.Cost = dto.Cost.Value;
        if (dto.Active is not null) reward.Active = dto.Active.Value;
        await db.SaveChangesAsync();
        return Map(reward);
    }

    /// <summary>Löscht eine Prämie. Bereits gestellte Einlöse-Anfragen bleiben (mit Titel/Kosten als Momentaufnahme) erhalten.</summary>
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

    // ---- Einlöse-Anfragen: Vater-Sicht und Entscheidung ----

    /// <summary>Einlöse-Anfragen des Kindes, optional nach Status gefiltert (offene zuerst, dann neueste).</summary>
    [HttpGet("redemptions")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<RedemptionDto>>> Redemptions(
        int childId, [FromQuery] RewardRedemptionStatus? status)
    {
        var query = db.RewardRedemptions.AsNoTracking().Where(r => r.ChildId == childId);
        if (status is not null) query = query.Where(r => r.Status == status);

        return await query
            .OrderBy(r => r.Status == RewardRedemptionStatus.Requested ? 0 : 1)
            .ThenByDescending(r => r.RequestedAt)
            .Select(r => MapRedemption(r)).ToListAsync();
    }

    /// <summary>
    /// Genehmigt eine offene Einlöse-Anfrage: bucht die Münzen jetzt ab (negative <c>PointKind.Reward</c>-
    /// Buchung) und markiert sie als genehmigt. Reicht das Guthaben nicht (mehr), schlägt es mit 400 fehl.
    /// </summary>
    [HttpPost("redemptions/{redemptionId:int}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RedemptionDto>> Approve(int childId, int redemptionId)
    {
        var redemption = await db.RewardRedemptions.FirstOrDefaultAsync(r => r.Id == redemptionId && r.ChildId == childId);
        if (redemption is null) return NotFound();
        if (redemption.Status != RewardRedemptionStatus.Requested)
            return Problem(statusCode: 409, detail: "Diese Anfrage wurde bereits entschieden.");

        var balance = await db.ChildPoints.Where(p => p.ChildId == childId).SumAsync(p => (int?)p.Amount) ?? 0;
        if (balance < redemption.Cost)
            return Problem(statusCode: 400, detail: $"Zu wenig Münzen: {balance}/{redemption.Cost}.");

        db.ChildPoints.Add(new ChildPointsEntry
        {
            ChildId = childId,
            Amount = -redemption.Cost,
            Kind = PointKind.Reward,
            Reason = $"Prämie eingelöst: {redemption.Title}",
        });
        redemption.Status = RewardRedemptionStatus.Approved;
        redemption.DecidedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return MapRedemption(redemption);
    }

    /// <summary>Lehnt eine offene Einlöse-Anfrage ab (keine Abbuchung).</summary>
    [HttpPost("redemptions/{redemptionId:int}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RedemptionDto>> Reject(int childId, int redemptionId)
    {
        var redemption = await db.RewardRedemptions.FirstOrDefaultAsync(r => r.Id == redemptionId && r.ChildId == childId);
        if (redemption is null) return NotFound();
        if (redemption.Status != RewardRedemptionStatus.Requested)
            return Problem(statusCode: 409, detail: "Diese Anfrage wurde bereits entschieden.");

        redemption.Status = RewardRedemptionStatus.Rejected;
        redemption.DecidedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return MapRedemption(redemption);
    }
}
