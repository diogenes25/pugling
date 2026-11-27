using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// Management of the logged-in father's children, including point balance. The father is derived from
/// the JWT (<c>fid</c>); child-related endpoints are secured by the <see cref="ChildOwnershipFilter"/>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/children")]
[Tags("Supervisor – Children")]
[Produces("application/json")]
[Authorize(Roles = Roles.Supervisor)]
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class ChildrenController(PuglingDbContext db, WalletService wallet, AccountService accounts, TimeProvider time,
    LearningDay learningDay) : ControllerBase
{
    /// <summary>Upper bound of a child's bonus cap: a typo of an extra zero must not open the family shop wide.</summary>
    private const int MaxBonusDailyCoinCap = 1000;
    private const string BonusCapMessage = "The daily bonus cap must be between 0 and 1000.";

    private static bool BonusCapAcceptable(int? cap) => cap is null or (>= 0 and <= MaxBonusDailyCoinCap);

    /// <summary>Upper bound of a child's tower-game minutes: four hours a day is no supplement to practice any more.</summary>
    private const int MaxTowerGameMinutes = 240;
    private const string TowerMinutesMessage = "The tower-game minutes per day must be between 0 and 240.";

    private static bool TowerMinutesAcceptable(int? minutes) => minutes is null or (>= 0 and <= MaxTowerGameMinutes);

    /// <summary>Upper bound of a child's homework amounts (B-760): the same typo guard as the bonus cap.</summary>
    private const int MaxHomeworkCoins = 1000;
    private const string HomeworkCoinsMessage = "The homework coins and the homework penalty must be between 0 and 1000.";

    private static bool HomeworkCoinsAcceptable(int? coins) => coins is null or (>= 0 and <= MaxHomeworkCoins);

    // B-731: typo guards for the planner limits - a day of 500 tasks is no plan.
    private const int MaxDailyTasks = 50;
    private const int MaxDailyNewWords = 200;
    private const int MaxPenaltyPerTask = 1000;

    /// <summary>
    /// The planner limits (B-731) as the child holds them after the change: each floor 1 to its maximum and not above its
    /// ceiling, each ceiling at most its maximum, the penalty 0 to 1000. Checked on the merged values, so a PATCH that only
    /// lowers a ceiling below the stored floor is refused too. <c>null</c> when they hold, else the message.
    /// </summary>
    private static string? PlannerLimitsProblem(int? tasksMin, int? tasksMax, int? wordsMin, int? wordsMax, int? penalty)
    {
        if (tasksMin is < 1 or > MaxDailyTasks || tasksMax is < 1 or > MaxDailyTasks)
            return $"The daily tasks must be between 1 and {MaxDailyTasks}; the floor is never 0.";
        if (wordsMin is < 1 or > MaxDailyNewWords || wordsMax is < 1 or > MaxDailyNewWords)
            return $"The daily new words must be between 1 and {MaxDailyNewWords}; the floor is never 0.";
        if (tasksMin > tasksMax) return "The floor of daily tasks is above their ceiling.";
        if (wordsMin > wordsMax) return "The floor of daily new words is above their ceiling.";
        if (penalty is < 0 or > MaxPenaltyPerTask) return $"The penalty per missed task must be between 0 and {MaxPenaltyPerTask}.";
        return null;
    }

    private const string SchoolTypeMessage = "A child attends exactly one school type; send a single value or None.";

    /// <summary>
    /// B-152: <see cref="SchoolTypes"/> is a flags enum because exercises, series and profiles use it as a filter,
    /// where a combination makes sense. A child attends ONE school, so a combination is a modelling accident that
    /// makes profile matches ambiguous. Only a SENT value is checked - stored combinations stay until someone
    /// touches the field. <c>Enum.IsDefined</c> catches undefined bits that the string-enum converter lets through
    /// as plain numbers.
    /// </summary>
    private static bool SchoolTypeAcceptable(SchoolTypes? value) =>
        value is null or SchoolTypes.None
        || (System.Numerics.BitOperations.IsPow2((int)value.Value) && Enum.IsDefined(value.Value));

    Task<ChildResponse?> ProjectOne(int childId, CancellationToken ct) =>
        db.Children
            .Where(c => c.Id == childId)
            .Select(c => new ChildResponse(c.Id, c.Name, c.BirthYear, c.Grade, c.SchoolType,
                c.Gender, c.Interests, c.ProfileNotes, c.AllowedContentRating,
                c.CreatedAt,
                c.PointsEntries.Where(p => PointKindCurrency.CoinKinds.Contains(p.Kind)).Sum(p => (int?)p.Amount) ?? 0,
                c.PointsEntries.Where(p => PointKindCurrency.GemKinds.Contains(p.Kind)).Sum(p => (int?)p.Amount) ?? 0,
                c.BonusDailyCoinCap, c.TowerGameMinutesPerDay, c.HomeworkCoins, c.HomeworkPenaltyCoins,
                c.DailyTasksMin, c.DailyTasksMax, c.DailyNewWordsMin, c.DailyNewWordsMax, c.PenaltyCoinsPerMissedTask,
                c.PlannerMode, c.PlannerSince))
            .FirstOrDefaultAsync(ct);

    /// <summary>List of students supervised by the logged-in supervisor.</summary>
    /// <param name="skip">Number of entries to skip (paging).</param>
    /// <param name="take">Optional page size (0..500). Without it the whole list is returned; the total count is always in the <c>X-Total-Count</c> header.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ChildResponse>>> List(
        [FromQuery] int skip = 0, [FromQuery] int? take = null, CancellationToken ct = default)
    {
        var supervisorId = User.AdultId();
        return await db.Children
            .Where(c => c.SupervisorLinks.Any(l => l.SupervisorId == supervisorId))
            // Names are not unique (two children called "Max"): the Id tie-break keeps a window deterministic.
            .OrderBy(c => c.Name).ThenBy(c => c.Id)
            .Select(c => new ChildResponse(c.Id, c.Name, c.BirthYear, c.Grade, c.SchoolType,
                c.Gender, c.Interests, c.ProfileNotes, c.AllowedContentRating,
                c.CreatedAt,
                c.PointsEntries.Where(p => PointKindCurrency.CoinKinds.Contains(p.Kind)).Sum(p => (int?)p.Amount) ?? 0,
                c.PointsEntries.Where(p => PointKindCurrency.GemKinds.Contains(p.Kind)).Sum(p => (int?)p.Amount) ?? 0,
                c.BonusDailyCoinCap, c.TowerGameMinutesPerDay, c.HomeworkCoins, c.HomeworkPenaltyCoins,
                c.DailyTasksMin, c.DailyTasksMax, c.DailyNewWordsMin, c.DailyNewWordsMax, c.PenaltyCoinsPerMissedTask,
                c.PlannerMode, c.PlannerSince))
            .ToOptionallyPagedListAsync(Response, skip, take, ct);
    }

    /// <summary>A single child.</summary>
    [HttpGet("{childId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChildResponse>> Get(int childId, CancellationToken ct = default)
    {
        var child = await ProjectOne(childId, ct);
        return child is null ? NotFound() : child;
    }

    /// <summary>Creates a child under the logged-in father.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChildResponse>> Create(CreateChildDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return this.ProblemWithCode(ApiErrors.ValidationError, "Name is required.");
        if (dto.Pin is not null && !PinHasher.IsAcceptable(dto.Pin)) return this.ProblemWithCode(ApiErrors.ValidationError, PinHasher.TooShortMessage);
        if (!BonusCapAcceptable(dto.BonusDailyCoinCap)) return this.ProblemWithCode(ApiErrors.ValidationError, BonusCapMessage);
        if (!TowerMinutesAcceptable(dto.TowerGameMinutesPerDay)) return this.ProblemWithCode(ApiErrors.ValidationError, TowerMinutesMessage);
        if (!HomeworkCoinsAcceptable(dto.HomeworkCoins) || !HomeworkCoinsAcceptable(dto.HomeworkPenaltyCoins))
            return this.ProblemWithCode(ApiErrors.ValidationError, HomeworkCoinsMessage);
        if (!SchoolTypeAcceptable(dto.SchoolType)) return this.ProblemWithCode(ApiErrors.ValidationError, SchoolTypeMessage);

        var child = new Child
        {
            Name = dto.Name.Trim(),
            BirthYear = dto.BirthYear,
            Grade = dto.Grade,
            SchoolType = dto.SchoolType ?? SchoolTypes.None,
            Gender = dto.Gender ?? Gender.None,
            Interests = dto.Interests ?? [],
            ProfileNotes = dto.ProfileNotes,
            // With nothing given, the strictest level - releasing images must be a deliberate supervisor choice.
            AllowedContentRating = dto.AllowedContentRating ?? ContentRating.Everyone,
            BonusDailyCoinCap = dto.BonusDailyCoinCap,
            TowerGameMinutesPerDay = dto.TowerGameMinutesPerDay,
            HomeworkCoins = dto.HomeworkCoins,
            HomeworkPenaltyCoins = dto.HomeworkPenaltyCoins,
        };
        db.Children.Add(child);
        // Establish supervision by the creating supervisor (a student can get more later) - in the SAME commit
        // as the child: if the request broke between two SaveChanges (client gone, connection dead), a child
        // without a link would remain, and that is reachable for nobody (List filters through the links, every
        // single access runs through ChildOwnershipFilter → 404). EF fills the StudentId from the navigation,
        // hence no second pass for the id.
        child.SupervisorLinks.Add(new SupervisorLink { SupervisorId = User.AdultId()!.Value });
        await db.SaveChangesAsync(ct);
        // Create the login account (student) right away so the new child can log in.
        await accounts.EnsureForChildAsync(child, dto.Pin, ct);

        var response = new ChildResponse(child.Id, child.Name, child.BirthYear, child.Grade,
            child.SchoolType, child.Gender, child.Interests, child.ProfileNotes,
            child.AllowedContentRating, child.CreatedAt, 0, 0, child.BonusDailyCoinCap,
            child.TowerGameMinutesPerDay, child.HomeworkCoins, child.HomeworkPenaltyCoins,
            child.DailyTasksMin, child.DailyTasksMax, child.DailyNewWordsMin, child.DailyNewWordsMax, child.PenaltyCoinsPerMissedTask,
            child.PlannerMode, child.PlannerSince);
        return CreatedAtAction(nameof(Get), new { childId = child.Id }, response);
    }

    /// <summary>Changes a child (partial).</summary>
    [HttpPatch("{childId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChildResponse>> Update(int childId, UpdateChildDto dto, CancellationToken ct = default)
    {
        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == childId, ct);
        if (child is null) return NotFound();
        if (dto.Pin is not null && !PinHasher.IsAcceptable(dto.Pin)) return this.ProblemWithCode(ApiErrors.ValidationError, PinHasher.TooShortMessage);
        if (!BonusCapAcceptable(dto.BonusDailyCoinCap)) return this.ProblemWithCode(ApiErrors.ValidationError, BonusCapMessage);
        if (!TowerMinutesAcceptable(dto.TowerGameMinutesPerDay)) return this.ProblemWithCode(ApiErrors.ValidationError, TowerMinutesMessage);
        if (!HomeworkCoinsAcceptable(dto.HomeworkCoins) || !HomeworkCoinsAcceptable(dto.HomeworkPenaltyCoins))
            return this.ProblemWithCode(ApiErrors.ValidationError, HomeworkCoinsMessage);
        if (!SchoolTypeAcceptable(dto.SchoolType)) return this.ProblemWithCode(ApiErrors.ValidationError, SchoolTypeMessage);
        // The string-enum converter also takes numbers; an undefined mode would switch the planner on as an unknown kind.
        if (dto.PlannerMode is { } sentMode && !Enum.IsDefined(sentMode))
            return this.ProblemWithCode(ApiErrors.ValidationError, "Unknown planner mode; send Off or Shadow.");
        // B-1036: the enforced mode arrives with the cutoff-day reset (B-737) - before it, a duty from tasks would run beside
        // learning states and plans the reset is meant to clear, and no task penalty is booked yet (B-1037).
        if (dto.PlannerMode == PlannerMode.Enforced)
            return this.ProblemWithCode(ApiErrors.ValidationError, "The enforced planner mode is not available yet; send Off or Shadow.");
        // Value first, clear switch second - as for every other field.
        int? Limit(int? sent, int? stored) => dto.ClearPlannerLimits ? null : sent ?? stored;
        var tasksMin = Limit(dto.DailyTasksMin, child.DailyTasksMin);
        var tasksMax = Limit(dto.DailyTasksMax, child.DailyTasksMax);
        var wordsMin = Limit(dto.DailyNewWordsMin, child.DailyNewWordsMin);
        var wordsMax = Limit(dto.DailyNewWordsMax, child.DailyNewWordsMax);
        var penalty = Limit(dto.PenaltyCoinsPerMissedTask, child.PenaltyCoinsPerMissedTask);
        if (PlannerLimitsProblem(tasksMin, tasksMax, wordsMin, wordsMax, penalty) is { } limitsProblem)
            return this.ProblemWithCode(ApiErrors.ValidationError, limitsProblem);
        // B-734: the planner composes within the limits, so it runs only while all four are set - also after this change.
        var mode = dto.PlannerMode ?? child.PlannerMode;
        if (mode != PlannerMode.Off && (tasksMin is null || tasksMax is null || wordsMin is null || wordsMax is null))
            return this.ProblemWithCode(ApiErrors.ValidationError, child.PlannerMode == PlannerMode.Off
                ? "The day planner needs the floor and ceiling of daily tasks and new words; set them before switching it on."
                : "The day planner is on and plans within these limits; switch it off before removing them.");

        if (dto.Name is not null) child.Name = dto.Name.Trim();
        // Value first, clear switch second - "clear" wins if a client sends both.
        if (dto.BirthYear.HasValue) child.BirthYear = dto.BirthYear;
        if (dto.ClearBirthYear) child.BirthYear = null;
        if (dto.Grade.HasValue) child.Grade = dto.Grade;
        if (dto.ClearGrade) child.Grade = null;
        if (dto.SchoolType.HasValue) child.SchoolType = dto.SchoolType.Value;
        if (dto.Gender.HasValue) child.Gender = dto.Gender.Value;
        // Assign a new list (no in-place mutation - the JSON column pitfall).
        if (dto.Interests is not null) child.Interests = [.. dto.Interests];
        if (dto.ProfileNotes is not null) child.ProfileNotes = dto.ProfileNotes;
        if (dto.AllowedContentRating.HasValue) child.AllowedContentRating = dto.AllowedContentRating.Value;
        if (dto.BonusDailyCoinCap.HasValue) child.BonusDailyCoinCap = dto.BonusDailyCoinCap;
        if (dto.ClearBonusDailyCoinCap) child.BonusDailyCoinCap = null;
        if (dto.TowerGameMinutesPerDay.HasValue) child.TowerGameMinutesPerDay = dto.TowerGameMinutesPerDay;
        if (dto.ClearTowerGameMinutesPerDay) child.TowerGameMinutesPerDay = null;
        if (dto.HomeworkCoins.HasValue) child.HomeworkCoins = dto.HomeworkCoins;
        if (dto.ClearHomeworkCoins) child.HomeworkCoins = null;
        if (dto.HomeworkPenaltyCoins.HasValue) child.HomeworkPenaltyCoins = dto.HomeworkPenaltyCoins;
        if (dto.ClearHomeworkPenaltyCoins) child.HomeworkPenaltyCoins = null;
        child.DailyTasksMin = tasksMin;
        child.DailyTasksMax = tasksMax;
        child.DailyNewWordsMin = wordsMin;
        child.DailyNewWordsMax = wordsMax;
        child.PenaltyCoinsPerMissedTask = penalty;
        // The day it was switched on: days before it are no missing plans (B-734, decision 5).
        if (mode != child.PlannerMode) child.PlannerSince = mode == PlannerMode.Off ? null : learningDay.Today;
        child.PlannerMode = mode;
        // Mirror the name onto the login account - in the SAME commit, or a renamed child is still greeted with the
        // old name after the next login (the display name comes from the account). See AccountService.MirrorAsync.
        // The PIN lives on the account only (B-230).
        var account = await accounts.MirrorAsync(child, ct);
        if (dto.Pin is not null) AccountService.SetPin(account, dto.Pin);
        await db.SaveChangesAsync(ct);

        return (await ProjectOne(childId, ct))!;
    }

    /// <summary>
    /// Deletes a child together with its study plans, progress, point ledger entries and its login account
    /// (catalog content stays).
    /// </summary>
    [HttpDelete("{childId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int childId, CancellationToken ct = default)
    {
        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == childId, ct);
        if (child is null) return NotFound();

        // The login account has no foreign key to the child: the cascade only clears its Student profile and
        // would leave an empty shell whose PIN hash `auth/login` keeps accepting. Same rule as in
        // AdultsController.Delete - an account whose profiles all point at this child goes with it.
        var orphanedAccounts = await db.Accounts
            .Where(a => a.Profiles.Any(p => p.ChildId == childId))
            .Where(a => a.Profiles.All(p => p.ChildId == childId))
            .ToListAsync(ct);
        db.Accounts.RemoveRange(orphanedAccounts);

        db.Children.Remove(child);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ---- Co-supervisors (several supervisors per student) ----

    /// <summary>All supervisors of this student (the acting supervisor must be one themself).</summary>
    [HttpGet("{childId:int}/supervisors")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<SupervisorLinkResponse>>> Supervisors(int childId, CancellationToken ct = default) =>
        await db.SupervisorLinks.AsNoTracking()
            .Where(l => l.StudentId == childId)
            .OrderBy(l => l.CreatedAt)
            .Select(l => new SupervisorLinkResponse(l.SupervisorId, l.Supervisor!.Name, l.Relation, l.CreatedAt))
            .ToListAsync(ct);

    /// <summary>
    /// Adds another supervisor to the student (e.g. mother/grandmother). The acting supervisor
    /// must already supervise the student (<see cref="ChildOwnershipFilter"/>); the new supervisor must exist.
    /// Idempotent: an existing supervision link is not duplicated - then answers <c>200</c> with the stored
    /// link (its own relation, not the caller's), <c>201</c> only on a real insert.
    /// </summary>
    [HttpPost("{childId:int}/supervisors")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupervisorLinkResponse>> AddSupervisor(int childId, AddSupervisorDto dto, CancellationToken ct = default)
    {
        var supervisor = await db.Adults.FirstOrDefaultAsync(f => f.Id == dto.SupervisorId, ct);
        if (supervisor is null) return this.ProblemWithCode(ApiErrors.InvalidReference, "Supervisor not found.");

        var existing = await db.SupervisorLinks.AsNoTracking()
            .FirstOrDefaultAsync(l => l.StudentId == childId && l.SupervisorId == dto.SupervisorId, ct);
        if (existing is not null)
            return Ok(new SupervisorLinkResponse(supervisor.Id, supervisor.Name, existing.Relation, existing.CreatedAt));

        var link = new SupervisorLink { StudentId = childId, SupervisorId = dto.SupervisorId, Relation = dto.Relation };
        db.SupervisorLinks.Add(link);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Supervisors), new { childId },
            new SupervisorLinkResponse(supervisor.Id, supervisor.Name, link.Relation, link.CreatedAt));
    }

    /// <summary>Removes a supervision link. The last supervisor cannot be removed (the student would be orphaned).</summary>
    [HttpDelete("{childId:int}/supervisors/{supervisorId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveSupervisor(int childId, int supervisorId, CancellationToken ct = default)
    {
        var link = await db.SupervisorLinks.FirstOrDefaultAsync(l => l.StudentId == childId && l.SupervisorId == supervisorId, ct);
        if (link is null) return NotFound();
        if (await db.SupervisorLinks.CountAsync(l => l.StudentId == childId, ct) <= 1)
            return this.ProblemWithCode(ApiErrors.ValidationError, "Cannot remove the last supervisor of a student.");
        db.SupervisorLinks.Remove(link);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ---- The child's points ----

    /// <summary>Wallet balance of the child (coins + gems) with the latest ledger entries (newest first).</summary>
    /// <param name="childId">Child whose wallet balance is being read.</param>
    /// <param name="skip">Number of ledger entries to skip (paging).</param>
    /// <param name="take">Maximum number of ledger entries (1..500). Total count in the <c>X-Total-Count</c> header.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("{childId:int}/points")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChildPointsResponse>> GetPoints(
        int childId, [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake,
        CancellationToken ct = default)
    {
        // Balance per currency over ALL entries (summed in the DB) - the list itself is paged (default 100).
        // Otherwise the displayed balance would differ from the page as soon as a child has more entries than
        // one page holds (base/combo/speed + missions/awards create many small rows per session).
        var (coins, gems) = await wallet.BalancesAsync(childId, ct);

        var entries = await db.ChildPointsEntries
            .AsNoTracking()
            .Where(p => p.ChildId == childId)
            .OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
            .Select(p => new PointsEntryResponse(p.Id, p.ChildId, p.Amount, p.Kind, p.Reason, p.CreatedAt, PointKindCurrency.Of(p.Kind)))
            .ToPagedListAsync(Response, skip, take, ct);

        return new ChildPointsResponse(childId, coins, gems, entries);
    }

    /// <summary>
    /// Books a manual point credit or debit (gifting/deducting outside of the shop and
    /// goal penalty). The currency determines the <see cref="PointKind"/>: coins → <see cref="PointKind.Manual"/>,
    /// gems → <see cref="PointKind.ManualGems"/>.
    /// </summary>
    [HttpPost("{childId:int}/points")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PointsEntryResponse>> AddPoints(int childId, PointsEntryDto dto, CancellationToken ct = default)
    {
        // Currency → point kind: gems through the manual twin, otherwise the classic manual coin entry.
        var kind = dto.Currency == Currency.Gems ? PointKind.ManualGems : PointKind.Manual;
        var entry = new ChildPointsEntry { ChildId = childId, Kind = kind, Amount = dto.Amount, Reason = dto.Reason ?? "", CreatedAt = time.GetUtcNow().UtcDateTime };
        db.ChildPointsEntries.Add(entry);
        await db.SaveChangesAsync(ct);

        var response = new PointsEntryResponse(entry.Id, childId, entry.Amount, entry.Kind, entry.Reason, entry.CreatedAt, PointKindCurrency.Of(entry.Kind));
        return CreatedAtAction(nameof(GetPoints), new { childId }, response);
    }
}
