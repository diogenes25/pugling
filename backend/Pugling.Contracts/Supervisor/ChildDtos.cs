namespace Pugling.Contracts.Supervisor;

// Contract around the supervised child: master data including the exercise-independent profile, the
// co-supervisors (a student may have several) and the shared wallet including manual supervisor entries.

/// <summary>
/// A supervised child with profile and balance of both currencies. <c>Interests</c> is the <b>free-form</b>
/// part of the profile (language for the AI creator); the weighted, referenced interests used for
/// image selection live under <c>children/{id}/interests</c>. <c>AllowedContentRating</c> is the
/// upper bound of image suitability – only the supervisor may raise it. <c>BonusDailyCoinCap</c> is the child's own
/// daily cap of bonus-training coins, <c>TowerGameMinutesPerDay</c> its play minutes per day in the tower game (0 = off),
/// <c>HomeworkCoins</c> what an accepted homework books and <c>HomeworkPenaltyCoins</c> what a homework without a hand-in by
/// its due day costs (B-760); <c>null</c> = the server default for each. <c>DailyTasksMin</c>/<c>DailyTasksMax</c>,
/// <c>DailyNewWordsMin</c>/<c>DailyNewWordsMax</c> and <c>PenaltyCoinsPerMissedTask</c> are the supervisor's limits for the
/// day planner (B-731): floor and ceiling of duty tasks and new words a day, and the penalty per missed task; all
/// <c>null</c> = the planner is not set up. <c>PlannerMode</c> says whether the planner makes daily plans for the child (B-734),
/// <c>PlannerSince</c> the learning day it was last switched on (<c>null</c> while it is off).
/// </summary>
public record ChildResponse(int Id, string Name, int? BirthYear, int? Grade,
    SchoolTypes SchoolType, Gender Gender, IReadOnlyList<string> Interests, string? ProfileNotes,
    ContentRating AllowedContentRating, DateTime CreatedAt, int Coins, int Gems, int? BonusDailyCoinCap,
    int? TowerGameMinutesPerDay, int? HomeworkCoins, int? HomeworkPenaltyCoins,
    int? DailyTasksMin, int? DailyTasksMax, int? DailyNewWordsMin, int? DailyNewWordsMax, int? PenaltyCoinsPerMissedTask,
    PlannerMode PlannerMode, DateOnly? PlannerSince);

/// <summary>
/// Input for creating a child. <c>SchoolType</c> is a single value or <c>None</c> ("not specified"): a child attends one
/// school, so a combination of flags or an undefined bit is rejected with <c>validation_error</c> (B-152).
/// </summary>
public record CreateChildDto(string Name, int? BirthYear, int? Grade, SchoolTypes? SchoolType, string? Pin,
    Gender? Gender = null, List<string>? Interests = null, string? ProfileNotes = null,
    ContentRating? AllowedContentRating = null, int? BonusDailyCoinCap = null, int? TowerGameMinutesPerDay = null,
    int? HomeworkCoins = null, int? HomeworkPenaltyCoins = null);

/// <summary>
/// Partial change to a child; omitted fields stay unchanged. <c>null</c> means "not specified"
/// and therefore cannot <b>clear</b> anything – that is what the <c>Clear…</c> switches are for (cf.
/// <c>ClearGrade</c> on the class test): <c>ClearBirthYear</c> removes the birth year,
/// <c>ClearGrade</c> the grade level (the child then drops out of grade filters), <c>ClearBonusDailyCoinCap</c> returns
/// the bonus cap, <c>ClearTowerGameMinutesPerDay</c> the tower-game minutes and <c>ClearHomeworkCoins</c> /
/// <c>ClearHomeworkPenaltyCoins</c> the homework amounts to the server default. A sent <c>SchoolType</c> must be a single value or <c>None</c> (B-152); a stored
/// combination from before that rule survives a PATCH that does not send the field.
/// The planner limits (B-731) are checked on the values the child has after the change: a floor is at least 1 and not above
/// its ceiling, the penalty at least 0, otherwise <c>validation_error</c>. <c>ClearPlannerLimits</c> removes all five.
/// <c>PlannerMode</c> switches the day planner (B-734); a mode other than <c>Off</c> needs all four limits of tasks and new
/// words after the change, else <c>validation_error</c> - also when a PATCH clears the limits of a running planner.
/// </summary>
public record UpdateChildDto(string? Name, int? BirthYear, int? Grade, SchoolTypes? SchoolType, string? Pin,
    Gender? Gender = null, List<string>? Interests = null, string? ProfileNotes = null,
    ContentRating? AllowedContentRating = null,
    bool ClearBirthYear = false, bool ClearGrade = false, int? BonusDailyCoinCap = null, bool ClearBonusDailyCoinCap = false,
    int? TowerGameMinutesPerDay = null, bool ClearTowerGameMinutesPerDay = false,
    int? HomeworkCoins = null, bool ClearHomeworkCoins = false, int? HomeworkPenaltyCoins = null,
    bool ClearHomeworkPenaltyCoins = false,
    int? DailyTasksMin = null, int? DailyTasksMax = null, int? DailyNewWordsMin = null, int? DailyNewWordsMax = null,
    int? PenaltyCoinsPerMissedTask = null, bool ClearPlannerLimits = false, PlannerMode? PlannerMode = null);

/// <summary>A supervision relationship: which supervisor supervises the student since when.</summary>
public record SupervisorLinkResponse(int SupervisorId, string SupervisorName, SupervisorRelation Relation, DateTime CreatedAt);

/// <summary>Input for adding another supervisor (e.g. mother/grandmother).</summary>
public record AddSupervisorDto(int SupervisorId, SupervisorRelation Relation = SupervisorRelation.Other);

/// <summary>
/// A points ledger entry of the child. <c>Currency</c> (B-425) is derived from <c>Kind</c> on the server - the ledger has no
/// currency column by design - and travels along so a history never shows a gem amount as coins: a client copy of the
/// kind-to-currency rule would drift with the next new kind.
/// </summary>
public record PointsEntryResponse(int Id, int ChildId, int Amount, PointKind Kind, string Reason, DateTime CreatedAt, Currency Currency);

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
