namespace Pugling.Contracts.Supervisor;

// Contract around the supervised child: master data including the exercise-independent profile, the
// co-supervisors (a student may have several) and the shared wallet including manual supervisor entries.

/// <summary>
/// A supervised child with profile and balance of both currencies. <c>Interests</c> is the <b>free-form</b>
/// part of the profile (language for the AI creator); the weighted, referenced interests used for
/// image selection live under <c>children/{id}/interests</c>. <c>AllowedContentRating</c> is the
/// upper bound of image suitability – only the supervisor may raise it.
/// </summary>
public record ChildResponse(int Id, string Name, int? BirthYear, int? Grade,
    SchoolTypes SchoolType, Gender Gender, IReadOnlyList<string> Interests, string? ProfileNotes,
    ContentRating AllowedContentRating, DateTime CreatedAt, int Coins, int Gems);

/// <summary>Input for creating a child.</summary>
public record CreateChildDto(string Name, int? BirthYear, int? Grade, SchoolTypes? SchoolType, string? Pin,
    Gender? Gender = null, List<string>? Interests = null, string? ProfileNotes = null,
    ContentRating? AllowedContentRating = null);

/// <summary>
/// Partial change to a child; omitted fields stay unchanged. <c>null</c> means "not specified"
/// and therefore cannot <b>clear</b> anything – that is what the <c>Clear…</c> switches are for (cf.
/// <c>ClearGrade</c> on the class test): <c>ClearBirthYear</c> removes the birth year,
/// <c>ClearGrade</c> the grade level (the child then drops out of grade filters).
/// </summary>
public record UpdateChildDto(string? Name, int? BirthYear, int? Grade, SchoolTypes? SchoolType, string? Pin,
    Gender? Gender = null, List<string>? Interests = null, string? ProfileNotes = null,
    ContentRating? AllowedContentRating = null,
    bool ClearBirthYear = false, bool ClearGrade = false);

/// <summary>A supervision relationship: which supervisor supervises the student since when.</summary>
public record SupervisorLinkResponse(int SupervisorId, string SupervisorName, SupervisorRelation Relation, DateTime CreatedAt);

/// <summary>Input for adding another supervisor (e.g. mother/grandmother).</summary>
public record AddSupervisorDto(int SupervisorId, SupervisorRelation Relation = SupervisorRelation.Other);

/// <summary>A points ledger entry of the child.</summary>
public record PointsEntryResponse(int Id, int ChildId, int Amount, PointKind Kind, string Reason, DateTime CreatedAt);

/// <summary>Balance of the child (both currencies) with a page of its ledger entries.</summary>
public record ChildPointsResponse(int ChildId, int Coins, int Gems, IEnumerable<PointsEntryResponse> Entries);

/// <summary>
/// Manual supervisor ledger entry: positive amount = credit/gift, negative = deduct.
/// Via the currency, the supervisor can also <b>gift gems</b> alongside coins
/// (reward outside the app, debt forgiveness).
/// </summary>
/// <param name="Amount">Amount; positive = credit/gift, negative = deduct.</param>
/// <param name="Reason">Free-text justification for the ledger.</param>
/// <param name="Currency">Target currency of the entry (default coins).</param>
public record PointsEntryDto(int Amount, string Reason, Currency Currency = Currency.Coins);
