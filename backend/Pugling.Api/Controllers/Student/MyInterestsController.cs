using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Student;

/// <summary>
/// The child describes itself (B-50): its own weighted interests, set by the child, without a supervisor
/// in between. This is the one place where a child may steer something, and it is safe precisely because
/// interests only <b>dress</b> the material – they never change what is practised, how often, or at which
/// threshold, so there is nothing here to game.
///
/// Two guarantees separate this from the supervisor's editor. First, the child picks <b>from</b> the shared
/// taxonomy and can never extend it: an invented tag would match no image at all, and the taxonomy is the
/// very axis that makes image selection computable. Second, a changed profile does not touch an already
/// frozen <see cref="ChildMediaPick"/> – the selector only reads interests when it draws a <i>new</i>
/// choice, so image constancy (which is the memory effect) survives a child that likes to fiddle.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Student + "/me/interests")]
[Tags("Student – Interests")]
[Produces("application/json")]
[Authorize(Roles = Roles.Student)]
public class MyInterestsController(PuglingDbContext db) : ControllerBase
{
    /// <summary>Maximum length of one free-text wish – a wish is a keyword, not an essay.</summary>
    public const int MaxWishLength = 60;

    /// <summary>
    /// Maximum number of free-text entries the child may leave behind. Without a cap the list grows
    /// unbounded: it is a JSON column with no schema limit, and nothing else prunes it.
    /// </summary>
    public const int MaxWishes = 20;

    /// <summary>
    /// The child's own profile: weighted taxonomy entries plus its free-text wishes, strongest preference
    /// first and dislikes last (the same order the supervisor sees).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MyInterestProfileResponse>> Get(CancellationToken ct = default)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();
        return await ProfileAsync(cid.Value, ct);
    }

    /// <summary>
    /// Replaces the child's interests completely (empty list = remove all) – the guided process edits the
    /// set as a whole, and a replacement is the only way an entry can be removed again.
    ///
    /// Unlike the supervisor's endpoint this resolves <b>only</b> against existing tags: an unknown
    /// <c>tagId</c> is rejected with <c>invalid_reference</c> instead of quietly creating taxonomy.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MyInterestProfileResponse>> Replace(SetMyInterestsDto dto, CancellationToken ct = default)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();

        var inputs = dto.Interests ?? [];
        foreach (var input in inputs)
            if (input.Weight is < ChildInterest.MinWeight or > ChildInterest.MaxWeight)
                return this.ProblemWithCode(ApiErrors.ValidationError,
                    $"Weight must be between {ChildInterest.MinWeight} and {ChildInterest.MaxWeight}.");

        // Check all references first: a single unknown tag must leave the existing set untouched.
        var wanted = inputs.Select(i => i.TagId).Distinct().ToList();
        var known = await db.InterestTags.AsNoTracking().Where(t => wanted.Contains(t.Id))
            .Select(t => t.Id).ToListAsync(ct);
        if (known.Count != wanted.Count)
            return this.ProblemWithCode(ApiErrors.InvalidReference,
                "Every interest must reference an existing interest tag by tagId.");

        // A token can outlive its child (deleted meanwhile). Without this the insert below hits the foreign
        // key and answers 500, where AddWish two methods down correctly answers 403.
        if (!await db.Children.AnyAsync(c => c.Id == cid, ct)) return Forbid();

        db.ChildInterests.RemoveRange(await db.ChildInterests.Where(i => i.ChildId == cid).ToListAsync(ct));
        // Two entries for the same tag would violate the unique index - the last one wins, as in an assignment.
        foreach (var input in inputs.GroupBy(i => i.TagId).Select(g => g.Last()))
            db.ChildInterests.Add(new ChildInterest { ChildId = cid.Value, InterestTagId = input.TagId, Weight = input.Weight });

        await db.SaveChangesAsync(ct);
        return await ProfileAsync(cid.Value, ct);
    }

    /// <summary>
    /// Adds a free-text wish outside the taxonomy ("Sonstiges: …"). It lands in the child's free-form
    /// interest list, which the supervisor already sees and edits – no hidden channel; an adult can later
    /// turn it into a real tag through the existing backfill funnel.
    /// </summary>
    [HttpPost("wishes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MyInterestProfileResponse>> AddWish(AddInterestWishDto dto, CancellationToken ct = default)
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();

        var text = (dto.Text ?? "").Trim();
        if (text.Length == 0)
            return this.ProblemWithCode(ApiErrors.ValidationError, "The wish must not be empty.");
        if (text.Length > MaxWishLength)
            return this.ProblemWithCode(ApiErrors.ValidationError,
                $"A wish may be at most {MaxWishLength} characters long.");

        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == cid, ct);
        if (child is null) return Forbid();

        // Already there (in any spelling) is a no-op, not an error - a child that taps twice has not failed.
        if (!child.Interests.Any(i => string.Equals(i, text, StringComparison.OrdinalIgnoreCase)))
        {
            if (child.Interests.Count >= MaxWishes)
                // Deliberately not "remove one first": the child's contract offers no way to delete a wish -
                // only a supervisor can shrink Child.Interests. A message must not ask for a door that
                // isn't there.
                return this.ProblemWithCode(ApiErrors.InterestWishLimitReached,
                    $"There are already {MaxWishes} free-text interests – ask a grown-up to remove one.");

            child.Interests = [.. child.Interests, text]; // reassign: JSON column, no in-place mutation
            // Child.ConcurrencyStamp is an EF concurrency token, and this is a read-modify-write of a
            // whole JSON list: without bumping it, two parallel wishes both match the unchanged token and
            // the second silently overwrites the first - a lost update reported as 200.
            child.ConcurrencyStamp = Guid.NewGuid();

            if (!await TrySaveAsync(ct))
                return this.ProblemWithCode(ApiErrors.ConcurrencyConflict,
                    "The wish conflicted with a concurrent change — please try again.");
        }

        return await ProfileAsync(cid.Value, ct);
    }

    /// <summary>Saves and catches a concurrency collision (token): false = collided, nothing committed.</summary>
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

    /// <summary>
    /// Reads the child's own profile. <c>NeedsIntro</c> answers "has this child ever described itself?"
    /// from an <b>empty</b> profile instead of from a new timestamp column – that keeps the story free of
    /// a schema change, at the price that a child who deliberately picks nothing is asked again (the app
    /// remembers the visit locally).
    /// </summary>
    private async Task<MyInterestProfileResponse> ProfileAsync(int childId, CancellationToken ct)
    {
        var interests = await db.ChildInterests.AsNoTracking()
            .Where(i => i.ChildId == childId)
            .OrderByDescending(i => i.Weight).ThenBy(i => i.InterestTag!.Slug)
            .Select(i => new MyInterestResponse(i.InterestTagId, i.InterestTag!.Slug, i.InterestTag.Label,
                i.InterestTag.Facet, i.Weight))
            .ToListAsync(ct);

        var wishes = await db.Children.AsNoTracking().Where(c => c.Id == childId)
            .Select(c => c.Interests).FirstOrDefaultAsync(ct) ?? [];

        return new MyInterestProfileResponse(interests, wishes, interests.Count == 0 && wishes.Count == 0);
    }
}
