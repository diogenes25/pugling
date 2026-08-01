using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Student;

/// <summary>
/// Child-centric view of vocabulary learning progress: "which words has this child got down, and which not?"
/// The progress is owned by the child (ownership via <see cref="ChildOwnershipFilter"/>: father = own child,
/// child = themself). Reads the plan-wide status per item (<see cref="ItemProgress"/>) and – via the
/// denormalized <c>vocabularyId</c> – the word rollup across all exercises (basis for targeted
/// review exercises from poorly learned words). History per item from <see cref="ItemReviewEvent"/>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Student + "/children/{childId:int}/vocabulary-progress")]
[Tags("Student – Vocabulary Progress")]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class ChildVocabularyProgressController(PuglingDbContext db) : ControllerBase
{
    /// <summary>The mastery (percent) below which an item/word counts as "weak" (filter <c>onlyWeak</c>); a shared threshold.</summary>
    private const int WeakBelowPercent = ItemProgress.WeakBelowPercent;

    // EF projection without the derived link (added in memory).
    private record Row(int ItemId, int ExerciseId, int VocabularyId, string Front, string Back,
        int Box, int MasteryPercent, int SeenCount, int CorrectCount,
        DateOnly? IntroducedAt, DateTime? LastAnswerAt, bool? LastCorrect);

    /// <summary>
    /// The item learning progress of the child, weakest first. Filter: <paramref name="exerciseId"/> (a single exercise only),
    /// <paramref name="maxBox"/> (box ≤ N), <paramref name="onlyWeak"/> (mastery &lt; 50 %). Total count in the <c>X-Total-Count</c> header.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ItemProgressResponse>>> List(int childId,
        [FromQuery] int? exerciseId, [FromQuery] int? maxBox, [FromQuery] bool onlyWeak = false,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake, CancellationToken ct = default)
    {
        var q = db.ItemProgress.AsNoTracking().Where(p => p.ChildId == childId);
        if (exerciseId is { } ex) q = q.Where(p => p.ExerciseId == ex);
        if (maxBox is { } mb) q = q.Where(p => p.Box <= mb);
        if (onlyWeak) q = q.Where(p => p.MasteryPercent < WeakBelowPercent);

        var projected =
            from p in q
            join v in db.Vocabularies.AsNoTracking() on p.VocabularyId equals v.Id into vj
            from v in vj.DefaultIfEmpty()
            orderby p.MasteryPercent, p.SeenCount descending, p.ItemId
            select new Row(p.ItemId, p.ExerciseId, p.VocabularyId,
                v == null ? "" : v.Word, v == null ? "" : v.Translation,
                p.Box, p.MasteryPercent, p.SeenCount, p.CorrectCount, p.IntroducedAt, p.LastAnswerAt, p.LastCorrect);

        var page = await projected.ToPagedListAsync(Response, skip, take, ct);
        return page.Select(MapRow).ToList();
    }

    /// <summary>
    /// Word rollup: aggregates the status per store vocabulary across all exercises of the child (weakest first).
    /// <paramref name="onlyWeak"/> restricts to words with average mastery &lt; 50 % – the candidates for targeted review.
    /// </summary>
    [HttpGet("by-word")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<WordMasteryResponse>>> ByWord(int childId,
        [FromQuery] bool onlyWeak = false,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake, CancellationToken ct = default)
    {
        var groups = db.ItemProgress.AsNoTracking()
            .Where(p => p.ChildId == childId)
            .GroupBy(p => p.VocabularyId)
            .Select(g => new
            {
                VocabularyId = g.Key,
                ItemCount = g.Count(),
                AvgMastery = (int)g.Average(x => x.MasteryPercent),
                MinBox = g.Min(x => x.Box),
                Seen = g.Sum(x => x.SeenCount),
                Correct = g.Sum(x => x.CorrectCount),
            });
        if (onlyWeak) groups = groups.Where(x => x.AvgMastery < WeakBelowPercent);

        var page = await groups
            .OrderBy(x => x.AvgMastery).ThenByDescending(x => x.Seen).ThenBy(x => x.VocabularyId)
            .ToPagedListAsync(Response, skip, take, ct);

        var ids = page.Select(g => g.VocabularyId).ToList();
        var vocabById = await db.Vocabularies.AsNoTracking().Where(v => ids.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, v => new { v.Word, v.Translation }, ct);
        return page.Select(g =>
        {
            var v = vocabById.GetValueOrDefault(g.VocabularyId);
            return new WordMasteryResponse(g.VocabularyId, v?.Word ?? "", v?.Translation ?? "", g.ItemCount,
                g.AvgMastery, g.MinBox, g.Seen, g.Correct, g.Seen == 0 ? 0 : (int)Math.Round(100.0 * g.Correct / g.Seen),
                VocabLink.Path + g.VocabularyId);
        }).ToList();
    }

    /// <summary>The item learning progress of the child for a single item (404 if no progress exists for it yet).</summary>
    [HttpGet("{itemId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemProgressResponse>> Get(int childId, int itemId, CancellationToken ct = default)
    {
        var row = await (
            from p in db.ItemProgress.AsNoTracking().Where(p => p.ChildId == childId && p.ItemId == itemId)
            join v in db.Vocabularies.AsNoTracking() on p.VocabularyId equals v.Id into vj
            from v in vj.DefaultIfEmpty()
            select new Row(p.ItemId, p.ExerciseId, p.VocabularyId,
                v == null ? "" : v.Word, v == null ? "" : v.Translation,
                p.Box, p.MasteryPercent, p.SeenCount, p.CorrectCount, p.IntroducedAt, p.LastAnswerAt, p.LastCorrect))
            .FirstOrDefaultAsync(ct);
        return row is null ? NotFound() : MapRow(row);
    }

    /// <summary>The answer history of the child for an item, newest first. Total count in the <c>X-Total-Count</c> header.</summary>
    [HttpGet("{itemId:int}/history")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<HistoryResponse>>> History(int childId, int itemId,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake, CancellationToken ct = default)
    {
        var events = await db.ItemReviewEvents.AsNoTracking()
            .Where(e => e.ChildId == childId && e.ItemId == itemId)
            .OrderByDescending(e => e.At).ThenByDescending(e => e.Id)
            .Select(e => new HistoryResponse(e.At, e.Source.ToString(), e.StageValue, e.GivenAnswer, e.WasCorrect, e.PlanPositionId))
            .ToPagedListAsync(Response, skip, take, ct);
        return events;
    }

    private static ItemProgressResponse MapRow(Row r) =>
        new(r.ItemId, r.ExerciseId, r.VocabularyId, r.Front, r.Back, r.Box, ItemProgress.MaxBox, r.MasteryPercent,
            r.SeenCount, r.CorrectCount, r.IntroducedAt, r.LastAnswerAt, r.LastCorrect, VocabLink.Path + r.VocabularyId);
}
