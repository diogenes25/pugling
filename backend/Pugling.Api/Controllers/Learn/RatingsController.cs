using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Learn;

/// <summary>Der Sohn bewertet die Lerninhalte eines Plans (passt der Stoff zum aktuellen Schulthema?).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.V1 + "/study-plans/{planId:int}/ratings")]
[Tags("Study – Ratings")]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(PlanOwnershipFilter))]
public class RatingsController(PuglingDbContext db) : ControllerBase
{
    public record RatingResponse(int Id, int ContentId, ExerciseFeedback Feedback, string? Comment, DateTime CreatedAt);

    static RatingResponse Map(ContentRating r) => new(r.Id, r.ContentId, r.Feedback, r.Comment, r.CreatedAt);

    /// <summary>Bewertungen dieses Plans (für Sohn und Vater).</summary>
    [HttpGet]
    public async Task<IEnumerable<RatingResponse>> List(int planId) =>
        (await db.ContentRatings.Where(r => r.StudyPlanId == planId)
            .OrderByDescending(r => r.CreatedAt).Take(200).ToListAsync())
        .Select(Map);

    public record CreateRatingDto(int ContentId, ExerciseFeedback Feedback, string? Comment);

    /// <summary>Bewertet einen Lerninhalt des Plans (Sehr Gut / Gut / Neutral / Schlecht / Fehler).</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RatingResponse>> Create(int planId, CreateRatingDto dto)
    {
        if (!await db.StudyPlanItems.AnyAsync(i => i.StudyPlanId == planId
                && (i.VocabularyId == dto.ContentId || i.ClozeTextId == dto.ContentId)))
            return Problem(statusCode: 400, detail: "Inhalt gehört nicht zum Lehrplan.");

        var childId = User.ChildId() ?? 0;
        var rating = new ContentRating
        {
            StudyPlanId = planId,
            ChildId = childId,
            ContentId = dto.ContentId,
            Feedback = dto.Feedback,
            Comment = dto.Comment,
        };
        db.ContentRatings.Add(rating);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(List), new { planId }, Map(rating));
    }
}
