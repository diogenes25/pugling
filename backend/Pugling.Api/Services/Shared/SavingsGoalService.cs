using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// The child's savings goal (B-593): a pointer to one family shop listing plus a title snapshot on the child. The goal
/// only reads - pinning, replacing and removing book nothing and leave the balance, the ledger and the child's
/// concurrency stamp alone (decision 10), which is why the two columns are written with <c>ExecuteUpdateAsync</c>
/// instead of a tracked child: a tracked save would hang on the stamp and fail a pin against a parallel purchase.
/// </summary>
public class SavingsGoalService(PuglingDbContext db, ShopService shop, WalletService wallet, LearningDay learningDay)
{
    // SQLITE_CONSTRAINT: the primary result code of every constraint failure, the foreign key's among them.
    private const int SqliteConstraintViolation = 19;

    /// <summary>The span the pace is averaged over: the seven full learning days before today (decision 1).</summary>
    public const int PaceDays = 7;

    /// <summary>The goal of the child, or <c>null</c> when none is set.</summary>
    public async Task<SavingsGoalResponse?> ReadAsync(int childId, DateTime nowUtc, CancellationToken ct)
    {
        var goal = await db.Children.AsNoTracking().Where(c => c.Id == childId)
            .Select(c => new { c.SavingsGoalListingId, c.SavingsGoalTitle })
            .FirstOrDefaultAsync(ct);
        if (goal is null || (goal.SavingsGoalListingId is null && goal.SavingsGoalTitle is null)) return null;

        var balances = await wallet.BalancesAsync(childId, ct);
        // Only a listing the child's own shop still offers counts (risk 4): a listing outside its audience reads as
        // "no longer available", like a deleted one. Inactive listings are looked up so their price stays visible.
        var view = goal.SavingsGoalListingId is { } listingId
            ? (await shop.ListingsForStudentAsync(childId, activeOnly: false, persist: false, nowUtc, ct))
                .FirstOrDefault(v => v.Listing.Id == listingId)
            : null;
        if (view is null)
            return new SavingsGoalResponse(goal.SavingsGoalListingId, goal.SavingsGoalTitle ?? "", false, null, null,
                balances.Coins, balances.Gems, null, null, null, null, null);

        var listing = view.Listing;
        var available = listing.Active;
        // From the real balance, debts included (decision 9): a goal that hides the penalty would be dishonest.
        var missingCoins = Math.Max(0, listing.CoinPrice - balances.Coins);
        var missingGems = Math.Max(0, listing.GemPrice - balances.Gems);
        // Only the coin share is converted into days (decision 7): gems hang on bonuses, not on pace.
        var days = available && listing.CoinPrice > 0
            ? await DaysAtPaceAsync(childId, missingCoins, learningDay.DayOf(nowUtc), ct)
            : null;
        return new SavingsGoalResponse(listing.Id, DisplayTitle(listing), available, listing.CoinPrice,
            listing.GemPrice, balances.Coins, balances.Gems, missingCoins, missingGems, days, view.CurrentStock,
            view.NextRefillAtUtc);
    }

    /// <summary>
    /// Pins <paramref name="listingId"/> as the goal, replacing any earlier one (decision 3). Only an active listing of
    /// the child's own shop view qualifies; anything else - a foreign family's, outside the audience, inactive - is
    /// unknown and yields <c>false</c> (404).
    /// </summary>
    public async Task<bool> SetAsync(int childId, int listingId, DateTime nowUtc, CancellationToken ct)
    {
        var view = (await shop.ListingsForStudentAsync(childId, activeOnly: true, persist: false, nowUtc, ct))
            .FirstOrDefault(v => v.Listing.Id == listingId);
        if (view is null) return false;

        var title = DisplayTitle(view.Listing);
        try
        {
            await db.Children.Where(c => c.Id == childId).ExecuteUpdateAsync(s => s
                .SetProperty(c => c.SavingsGoalListingId, listingId)
                .SetProperty(c => c.SavingsGoalTitle, title), ct);
        }
        catch (SqliteException e) when (e.SqliteErrorCode == SqliteConstraintViolation)
        {
            // The listing was deleted between the check and the write (FK violation): as unknown as any other. Only the
            // constraint: a busy or broken database is a server error, not a missing listing.
            return false;
        }
        return true;
    }

    /// <summary>Removes the goal: both the pointer and the title snapshot, so nothing reads as "no longer available".</summary>
    public Task ClearAsync(int childId, CancellationToken ct) =>
        db.Children.Where(c => c.Id == childId).ExecuteUpdateAsync(s => s
            .SetProperty(c => c.SavingsGoalListingId, (int?)null)
            .SetProperty(c => c.SavingsGoalTitle, (string?)null), ct);

    /// <summary>
    /// Rounded-up days to close <paramref name="missingCoins"/> at the average earned coins per calendar day of the
    /// seven full days before today. <c>null</c> when nothing was earned there or when the child's first ledger entry
    /// is younger than those seven days (decision 2): one day of history divided by seven would be far too pessimistic,
    /// divided by one far too rosy - no number beats a wrong one.
    /// </summary>
    private async Task<int?> DaysAtPaceAsync(int childId, int missingCoins, DateOnly today, CancellationToken ct)
    {
        var from = today.AddDays(-PaceDays);
        var first = await db.ChildPointsEntries.AsNoTracking().Where(p => p.ChildId == childId)
            .MinAsync(p => (DateTime?)p.CreatedAt, ct);
        if (first is null || learningDay.DayOf(first.Value) > from) return null;

        // The same notion of "earned" as the week comparison and the supervisor's daily figure (EarnedCoinKinds).
        var earned = (await wallet.EarnedCoinsByDayAsync(childId, from, today.AddDays(-1), ct)).Values.Sum();
        if (earned <= 0) return null;
        // ceil(missing / (earned / 7)) in integers.
        return (int)((missingCoins * (long)PaceDays + earned - 1) / earned);
    }

    private static string DisplayTitle(ShopListing listing) =>
        string.IsNullOrWhiteSpace(listing.Title) ? listing.ShopArticle!.Title : listing.Title;
}
