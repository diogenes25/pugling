using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;
using Pugling.Api.Errors;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// The supervisor's evaluation of a child (B-733): what was learnt per day and subject, the profile over 7 and 28 days and
/// the problem words with the kind of error. Supervisor-only - a creator-only account gets 403, a foreign supervisor 404, and
/// no student route returns these types (the child neither sees its own error statistics nor a brother's).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/children/{childId:int}/evaluations")]
[Tags("Supervisor – Evaluations")]
[Produces("application/json")]
[Authorize(Roles = Roles.Supervisor)]
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class ChildEvaluationsController(DailyEvaluationService evaluations, LearningDay learningDay) : ControllerBase
{
    /// <summary>
    /// The days of the child per subject, newest first, from <paramref name="from"/> to <paramref name="to"/> (default: the
    /// last 28 days up to today). Closed days come from the stored evaluation and never change; today's rows are computed live
    /// (<c>live: true</c>). A day without a row is a gap - no practice and no duty owed (Sunday, pause), not a zero day.
    /// Closed days further back than 28 days are not evaluated and stay gaps.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<DailyEvaluationResponse>>> List(int childId, [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null, [FromQuery] int? subjectId = null, [FromQuery] int skip = 0,
        [FromQuery] int take = PagingExtensions.DefaultTake, CancellationToken ct = default)
    {
        var (start, end, problem) = Range(from, to);
        if (problem is not null) return problem;
        var rows = await evaluations.ListAsync(childId, start, end, subjectId, ct);
        return rows.ToPagedList(Response, skip, take);
    }

    /// <summary>
    /// The child's profile per subject over the last 7 and 28 closed days: mastery, tempo (consolidated words per learning
    /// day), the most frequent error kinds and resilience (share of duty days met). Computed from the stored days only.
    /// </summary>
    [HttpGet("profile")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EvaluationProfileResponse>> Profile(int childId, CancellationToken ct = default) =>
        await evaluations.ProfileAsync(childId, ct);

    /// <summary>
    /// The words the child got wrong from <paramref name="from"/> to <paramref name="to"/> (default: the last 28 days), most
    /// wrong answers first, with the kinds of the wrong answers, the last wrong answer and the word's current box.
    /// </summary>
    [HttpGet("problem-words")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ProblemWordResponse>>> ProblemWords(int childId,
        [FromQuery] DateOnly? from = null, [FromQuery] DateOnly? to = null, [FromQuery] int? subjectId = null,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake, CancellationToken ct = default)
    {
        var (start, end, problem) = Range(from, to);
        if (problem is not null) return problem;
        var words = await evaluations.ProblemWordsAsync(childId, start, end, subjectId, ct);
        return words.ToPagedList(Response, skip, take);
    }

    private (DateOnly From, DateOnly To, ActionResult? Problem) Range(DateOnly? from, DateOnly? to)
    {
        var end = to ?? learningDay.Today;
        var start = from ?? end.AddDays(-(DailyEvaluationService.WindowDays - 1));
        return start > end
            ? (start, end, this.ProblemWithCode(ApiErrors.ValidationError, "'from' must not be after 'to'."))
            : (start, end, null);
    }
}
