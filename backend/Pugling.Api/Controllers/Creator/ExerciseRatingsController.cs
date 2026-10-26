using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// Ratings of shared exercises (B-12): any creator may give somebody else's exercise 1-5 stars and a short
/// comment, so that proven material can be told apart while browsing the catalog. Readable for every creator,
/// like the catalog itself - Read is deliberately not a permission. The owners of an exercise cannot rate it:
/// their stars would only inflate the signal meant to come from the people using it.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/exercises/{exerciseId:int}")]
[Tags("Creator – Exercise Ratings")]
[Produces("application/json")]
[Authorize(Roles = Roles.Creator)]
public class ExerciseRatingsController(PuglingDbContext db, ExercisePermissionService perms) : ControllerBase
{
    /// <summary>Upper bound of a comment - the same length the free-text column carries.</summary>
    private const int MaxCommentLength = 2000;

    /// <summary>All ratings of the exercise, newest change first - visible to every creator, not only the owner.</summary>
    [HttpGet("ratings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ExerciseRatingResponse>>> List(int exerciseId, CancellationToken ct = default)
    {
        if (!await db.Exercises.AnyAsync(e => e.Id == exerciseId, ct))
            return this.ProblemWithCode(ApiErrors.NotFound, "Exercise not found.");
        var fid = User.AdultId();
        return await db.ExerciseRatings.AsNoTracking()
            .Where(r => r.ExerciseId == exerciseId)
            .OrderByDescending(r => r.UpdatedAt).ThenBy(r => r.Id)
            .Select(r => new ExerciseRatingResponse(r.CreatorId, r.Creator!.Name, r.CreatorId == fid,
                r.Stars, r.Comment, r.CreatedAt, r.UpdatedAt))
            .ToListAsync(ct);
    }

    /// <summary>
    /// Rates the exercise or changes the caller's existing rating (PUT semantics: there is never a second row
    /// per creator). Refused with <c>403 cannot_rate_own_exercise</c> for anyone who administers the exercise.
    /// </summary>
    [HttpPut("rating")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseRatingResponse>> Rate(int exerciseId, RateExerciseDto dto, CancellationToken ct = default)
    {
        if (await EnsureMayRateAsync(exerciseId, ct) is { } refused) return refused;
        if (dto.Stars is < 1 or > 5)
            return this.ProblemWithCode(ApiErrors.ValidationError, "Stars must be between 1 and 5.");
        var comment = string.IsNullOrWhiteSpace(dto.Comment) ? null : dto.Comment.Trim();
        if (comment is { Length: > MaxCommentLength })
            return this.ProblemWithCode(ApiErrors.ValidationError, $"Comment must not exceed {MaxCommentLength} characters.");

        var fid = User.AdultId()!.Value;
        ExerciseRating rating;
        try
        {
            rating = await UpsertAsync(exerciseId, fid, dto.Stars, comment, ct);
        }
        catch (DbUpdateException)
        {
            // Two first ratings of the same creator at once (double click, retry): the unique index let only
            // one insert through. The other request is an overwrite of that row, not a 500.
            db.ChangeTracker.Clear();
            rating = await UpsertAsync(exerciseId, fid, dto.Stars, comment, ct);
        }

        var name = await db.Adults.Where(a => a.Id == fid).Select(a => a.Name).FirstAsync(ct);
        return new ExerciseRatingResponse(fid, name, true, rating.Stars, rating.Comment, rating.CreatedAt, rating.UpdatedAt);
    }

    /// <summary>Removes the caller's own rating; <c>404</c> if there is none.</summary>
    [HttpDelete("rating")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(int exerciseId, CancellationToken ct = default)
    {
        if (!await db.Exercises.AnyAsync(e => e.Id == exerciseId, ct))
            return this.ProblemWithCode(ApiErrors.NotFound, "Exercise not found.");
        var fid = User.AdultId();
        var rating = await db.ExerciseRatings.FirstOrDefaultAsync(r => r.ExerciseId == exerciseId && r.CreatorId == fid, ct);
        if (rating is null) return this.ProblemWithCode(ApiErrors.NotFound, "You have not rated this exercise.");

        db.ExerciseRatings.Remove(rating);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<ExerciseRating> UpsertAsync(int exerciseId, int creatorId, int stars, string? comment, CancellationToken ct)
    {
        var rating = await db.ExerciseRatings.FirstOrDefaultAsync(r => r.ExerciseId == exerciseId && r.CreatorId == creatorId, ct);
        var now = DateTime.UtcNow;
        if (rating is null)
        {
            rating = new ExerciseRating { ExerciseId = exerciseId, CreatorId = creatorId, CreatedAt = now };
            db.ExerciseRatings.Add(rating);
        }
        rating.Stars = stars;
        rating.Comment = comment;
        rating.UpdatedAt = now;
        await db.SaveChangesAsync(ct);
        return rating;
    }

    // 404 before 403, as with the grants. A rating hangs off the rater's adult id; a token without one has
    // nobody to attribute the stars to and is refused rather than stored anonymously.
    private async Task<ObjectResult?> EnsureMayRateAsync(int exerciseId, CancellationToken ct)
    {
        if (!await db.Exercises.AnyAsync(e => e.Id == exerciseId, ct))
            return this.ProblemWithCode(ApiErrors.NotFound, "Exercise not found.");
        if (await perms.CanAdministerAsync(User, exerciseId, ct))
            return this.ProblemWithCode(ApiErrors.CannotRateOwnExercise, "Owners cannot rate their own exercise.");
        return User.AdultId() is null
            ? this.ProblemWithCode(ApiErrors.Forbidden, "Only a creator with an adult profile can rate exercises.")
            : null;
    }
}
