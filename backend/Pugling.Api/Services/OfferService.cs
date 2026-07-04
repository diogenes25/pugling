using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services;

/// <summary>
/// Kauf- und Erfüllungs-Logik für Angebote (<see cref="Reward"/>). Bündelt die Geschäftsregeln, damit die
/// Controller dünn bleiben: <b>Direktkauf</b> (Münzen sofort abgebucht) durch den Sohn, danach
/// <b>Erfüllen</b> oder <b>Stornieren</b> (Rückerstattung) durch den Vater. Das Kontingent (<see
/// cref="Reward.Quantity"/>) gilt je Perioden-Fenster (siehe <see cref="PeriodWindow"/>) und füllt sich
/// pro Periode wieder auf; stornierte Käufe geben ihren Slot frei.
/// </summary>
public class OfferService(PuglingDbContext db, WalletService wallet)
{
    /// <summary>Fehlerursache eines Kauf-/Erfüllungsvorgangs (None = erfolgreich).</summary>
    public enum OfferError
    {
        None = 0,
        /// <summary>Angebot bzw. Kauf nicht gefunden (falsche Id / fremdes Kind).</summary>
        NotFound,
        /// <summary>Angebot ist deaktiviert.</summary>
        Inactive,
        /// <summary>Kontingent dieser Periode ist erschöpft.</summary>
        QuotaExceeded,
        /// <summary>Zu wenig Münzen für den Kauf.</summary>
        InsufficientCoins,
        /// <summary>Kauf steht nicht (mehr) offen – schon erfüllt/storniert.</summary>
        NotOpen,
        /// <summary>Nebenläufige Kollision (Doppelklick/Retry) – bitte erneut versuchen.</summary>
        Conflict,
    }

    /// <summary>Ergebnis eines Vorgangs: bei <see cref="OfferError.None"/> trägt es den betroffenen Kauf.</summary>
    public record Result(OfferError Error, RewardRedemption? Redemption)
    {
        public static Result Ok(RewardRedemption r) => new(OfferError.None, r);
        public static Result Fail(OfferError e) => new(e, null);
    }

    /// <summary>
    /// Kauft ein Angebot für das Kind: prüft Aktiv-Status, Kontingent der aktuellen Periode und Deckung,
    /// bucht die Münzen sofort ab und legt den Kauf (Status <see cref="RewardRedemptionStatus.Purchased"/>)
    /// an. Das Concurrency-Token am Kind wird gebumpt, sodass ein paralleler Doppelkauf mit
    /// <see cref="OfferError.Conflict"/> scheitert (schützt Kontingent <b>und</b> Saldo).
    /// </summary>
    public async Task<Result> PurchaseAsync(int childId, int rewardId, DateTime nowUtc, CancellationToken ct = default)
    {
        var reward = await db.Rewards.FirstOrDefaultAsync(r => r.Id == rewardId && r.ChildId == childId, ct);
        if (reward is null) return Result.Fail(OfferError.NotFound);
        if (!reward.Active) return Result.Fail(OfferError.Inactive);

        var used = await CountInCurrentPeriodAsync(reward, nowUtc, ct);
        if (used >= reward.Quantity) return Result.Fail(OfferError.QuotaExceeded);

        var coins = await wallet.CoinsAsync(childId, ct);
        if (coins < reward.Cost) return Result.Fail(OfferError.InsufficientCoins);

        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == childId, ct);
        if (child is null) return Result.Fail(OfferError.NotFound);

        db.ChildPoints.Add(new ChildPointsEntry
        {
            ChildId = childId,
            Amount = -reward.Cost,
            Kind = PointKind.Reward,
            Reason = $"Angebot gekauft: {reward.Title}",
        });
        var redemption = new RewardRedemption
        {
            ChildId = childId,
            RewardId = reward.Id,
            Title = reward.Title,   // Momentaufnahme, stabil auch bei späterer Änderung/Löschung
            Cost = reward.Cost,
            Status = RewardRedemptionStatus.Purchased,
            PurchasedAt = nowUtc,
        };
        db.RewardRedemptions.Add(redemption);
        child.ConcurrencyStamp = Guid.NewGuid(); // Token bumpen → paralleler Zweitkauf scheitert (409)

        return await TrySaveAsync(ct) ? Result.Ok(redemption) : Result.Fail(OfferError.Conflict);
    }

    /// <summary>Markiert einen offenen Kauf als vom Vater erfüllt (reale Leistung erbracht).</summary>
    public async Task<Result> FulfillAsync(int childId, int redemptionId, DateTime nowUtc, CancellationToken ct = default)
    {
        var redemption = await LoadOpenAsync(childId, redemptionId, ct);
        if (redemption is null) return await MissOrNotOpenAsync(childId, redemptionId, ct);

        redemption.Status = RewardRedemptionStatus.Fulfilled;
        redemption.FulfilledAt = nowUtc;
        await db.SaveChangesAsync(ct);
        return Result.Ok(redemption);
    }

    /// <summary>
    /// Storniert einen offenen Kauf und erstattet die Münzen zurück (positive <c>PointKind.Reward</c>-Buchung).
    /// Der Kontingent-Slot wird dadurch wieder frei (stornierte Käufe zählen nicht mehr mit).
    /// </summary>
    public async Task<Result> CancelAsync(int childId, int redemptionId, DateTime nowUtc, CancellationToken ct = default)
    {
        var redemption = await LoadOpenAsync(childId, redemptionId, ct);
        if (redemption is null) return await MissOrNotOpenAsync(childId, redemptionId, ct);

        redemption.Status = RewardRedemptionStatus.Cancelled;
        redemption.FulfilledAt = nowUtc;
        db.ChildPoints.Add(new ChildPointsEntry
        {
            ChildId = childId,
            Amount = redemption.Cost, // Rückerstattung
            Kind = PointKind.Reward,
            Reason = $"Angebot storniert (Rückerstattung): {redemption.Title}",
        });
        await db.SaveChangesAsync(ct);
        return Result.Ok(redemption);
    }

    /// <summary>Wie oft das Angebot in seiner aktuellen Periode schon (nicht storniert) gekauft wurde.</summary>
    public Task<int> CountInCurrentPeriodAsync(Reward reward, DateTime nowUtc, CancellationToken ct = default)
    {
        var window = PeriodWindow.Of(reward.Period, nowUtc);
        var query = db.RewardRedemptions
            .Where(r => r.RewardId == reward.Id && r.Status != RewardRedemptionStatus.Cancelled);
        if (window.From is { } from) query = query.Where(r => r.PurchasedAt >= from);
        if (window.To is { } to) query = query.Where(r => r.PurchasedAt < to);
        return query.CountAsync(ct);
    }

    private Task<RewardRedemption?> LoadOpenAsync(int childId, int redemptionId, CancellationToken ct) =>
        db.RewardRedemptions.FirstOrDefaultAsync(
            r => r.Id == redemptionId && r.ChildId == childId && r.Status == RewardRedemptionStatus.Purchased, ct);

    /// <summary>Unterscheidet „gibt es nicht" von „nicht mehr offen" für eine präzise Fehlerantwort.</summary>
    private async Task<Result> MissOrNotOpenAsync(int childId, int redemptionId, CancellationToken ct)
    {
        var exists = await db.RewardRedemptions.AnyAsync(r => r.Id == redemptionId && r.ChildId == childId, ct);
        return Result.Fail(exists ? OfferError.NotOpen : OfferError.NotFound);
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
