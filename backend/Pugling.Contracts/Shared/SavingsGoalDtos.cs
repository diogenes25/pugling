namespace Pugling.Contracts.Shared;

// The child's savings goal (B-593): one pinned family shop listing and how far away it is. The child sets it under
// student/me/savings-goal, its supervisor reads it under supervisor/children/{childId}/savings-goal.

/// <summary>
/// The child's savings goal. <c>ListingId</c> is <c>null</c> when the listing was deleted - then only the
/// <c>Title</c> snapshot is left and <c>Available</c> is false. Prices are read live from the listing (<c>null</c>
/// when it is gone or no longer in the child's shop). <c>MissingCoins</c> counts from the real balance, debts
/// included. <c>DaysAtPace</c> is a forecast "at your pace, about N days": the missing coins divided by the average
/// earned coins per calendar day of the seven full days before today; <c>null</c> without a coin price, when the
/// goal is not available, when nothing was earned in those days or when the child's history is shorter than seven
/// days. <c>CurrentStock</c>/<c>NextRefillAtUtc</c> are the child's own stock of the listing, as in the shop.
/// </summary>
public record SavingsGoalResponse(int? ListingId, string Title, bool Available, int? CoinPrice, int? GemPrice,
    int Coins, int Gems, int? MissingCoins, int? MissingGems, int? DaysAtPace, int? CurrentStock,
    DateTime? NextRefillAtUtc);

/// <summary>Pins a listing of the child's own family shop as its savings goal; a second pin replaces the first.</summary>
public record SetSavingsGoalDto(int ListingId);
