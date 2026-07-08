using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Controllers;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Student;

/// <summary>
/// Kind-zentrische Sicht auf den Vokabel-Lernfortschritt: „welche Vokabeln sitzen bei diesem Kind, welche nicht?"
/// Der Fortschritt ist Eigentum des Kindes (Ownership über <see cref="ChildOwnershipFilter"/>: Vater = eigenes Kind,
/// Sohn = er selbst). Liest den plan-übergreifenden Stand je Item (<see cref="ItemProgress"/>) und – über die
/// denormalisierte <c>vocabularyId</c> – das Wort-Rollup über alle Übungen hinweg (Grundlage für gezielte
/// Wiederholungs-Übungen aus schlecht gelernten Wörtern). Historie je Item aus <see cref="ItemReviewEvent"/>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.V1 + "/children/{childId:int}/vocabulary-progress")]
[Tags("Learn – Vocabulary Progress")]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class ChildVocabularyProgressController(PuglingDbContext db) : ControllerBase
{
    /// <summary>Ab welcher Beherrschung (Prozent) ein Item/Wort als „schwach" gilt (Filter <c>onlyWeak</c>); geteilte Schwelle.</summary>
    private const int WeakBelowPercent = ItemProgress.WeakBelowPercent;

    /// <summary>Lernstand eines Kindes zu einem Item (Front/Rückseite aus dem Store, kanonisch Wort → Übersetzung).</summary>
    public record ItemProgressResponse(int ItemId, int ExerciseId, int VocabularyId, string Front, string Back,
        int Box, int MaxBox, int MasteryPercent, int SeenCount, int CorrectCount,
        DateOnly? IntroducedAt, DateTime? LastAnswerAt, bool? LastCorrect,
        [property: System.Text.Json.Serialization.JsonPropertyName("vocabulary")] string Vocabulary);

    /// <summary>Aggregierter Wort-Beherrschungsstand über alle Übungen, die dieses Store-Wort nutzen.</summary>
    public record WordMasteryResponse(int VocabularyId, string Word, string Translation, int ItemCount,
        int AvgMasteryPercent, int MinBox, int SeenCount, int CorrectCount, int CorrectPercent,
        [property: System.Text.Json.Serialization.JsonPropertyName("vocabulary")] string Vocabulary);

    /// <summary>Ein protokolliertes Antwort-Ereignis der Item-Historie.</summary>
    public record HistoryResponse(DateTime At, string Source, int StageValue, string? GivenAnswer,
        bool WasCorrect, int? PlanPositionId);

    // EF-Projektion ohne den abgeleiteten Link (im Speicher ergänzt).
    private record Row(int ItemId, int ExerciseId, int VocabularyId, string Front, string Back,
        int Box, int MasteryPercent, int SeenCount, int CorrectCount,
        DateOnly? IntroducedAt, DateTime? LastAnswerAt, bool? LastCorrect);

    /// <summary>
    /// Der Item-Lernstand des Kindes, schwächste zuerst. Filter: <paramref name="exerciseId"/> (nur eine Übung),
    /// <paramref name="maxBox"/> (Box ≤ N), <paramref name="onlyWeak"/> (Beherrschung &lt; 50 %). Gesamtzahl im Header <c>X-Total-Count</c>.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ItemProgressResponse>>> List(int childId,
        [FromQuery] int? exerciseId, [FromQuery] int? maxBox, [FromQuery] bool onlyWeak = false,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake)
    {
        var q = db.ItemProgress.AsNoTracking().Where(p => p.ChildId == childId);
        if (exerciseId is { } ex) q = q.Where(p => p.ExerciseId == ex);
        if (maxBox is { } mb) q = q.Where(p => p.Box <= mb);
        if (onlyWeak) q = q.Where(p => p.MasteryPercent < WeakBelowPercent);

        var projected =
            from p in q
            join v in db.Vocabulary.AsNoTracking() on p.VocabularyId equals v.Id into vj
            from v in vj.DefaultIfEmpty()
            orderby p.MasteryPercent, p.SeenCount descending, p.ItemId
            select new Row(p.ItemId, p.ExerciseId, p.VocabularyId,
                v == null ? "" : v.Word, v == null ? "" : v.Translation,
                p.Box, p.MasteryPercent, p.SeenCount, p.CorrectCount, p.IntroducedAt, p.LastAnswerAt, p.LastCorrect);

        var page = await projected.ToPagedListAsync(Response, skip, take);
        return page.Select(MapRow).ToList();
    }

    /// <summary>
    /// Wort-Rollup: aggregiert den Stand je Store-Vokabel über alle Übungen des Kindes (schwächste zuerst).
    /// <paramref name="onlyWeak"/> beschränkt auf Wörter mit Ø-Beherrschung &lt; 50 % – die Kandidaten für gezielte Wiederholung.
    /// </summary>
    [HttpGet("by-word")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<WordMasteryResponse>>> ByWord(int childId,
        [FromQuery] bool onlyWeak = false,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake)
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
            .ToPagedListAsync(Response, skip, take);

        var ids = page.Select(g => g.VocabularyId).ToList();
        var vocabById = await db.Vocabulary.AsNoTracking().Where(v => ids.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, v => new { v.Word, v.Translation });
        return page.Select(g =>
        {
            var v = vocabById.GetValueOrDefault(g.VocabularyId);
            return new WordMasteryResponse(g.VocabularyId, v?.Word ?? "", v?.Translation ?? "", g.ItemCount,
                g.AvgMastery, g.MinBox, g.Seen, g.Correct, g.Seen == 0 ? 0 : (int)Math.Round(100.0 * g.Correct / g.Seen),
                VocabLink.Path + g.VocabularyId);
        }).ToList();
    }

    /// <summary>Der Item-Lernstand des Kindes zu einem einzelnen Item (404, wenn dazu noch kein Fortschritt existiert).</summary>
    [HttpGet("{itemId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemProgressResponse>> Get(int childId, int itemId)
    {
        var row = await (
            from p in db.ItemProgress.AsNoTracking().Where(p => p.ChildId == childId && p.ItemId == itemId)
            join v in db.Vocabulary.AsNoTracking() on p.VocabularyId equals v.Id into vj
            from v in vj.DefaultIfEmpty()
            select new Row(p.ItemId, p.ExerciseId, p.VocabularyId,
                v == null ? "" : v.Word, v == null ? "" : v.Translation,
                p.Box, p.MasteryPercent, p.SeenCount, p.CorrectCount, p.IntroducedAt, p.LastAnswerAt, p.LastCorrect))
            .FirstOrDefaultAsync();
        return row is null ? NotFound() : MapRow(row);
    }

    /// <summary>Die Antwort-Historie des Kindes zu einem Item, neueste zuerst. Gesamtzahl im Header <c>X-Total-Count</c>.</summary>
    [HttpGet("{itemId:int}/history")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<HistoryResponse>>> History(int childId, int itemId,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake)
    {
        var events = await db.ItemReviewEvents.AsNoTracking()
            .Where(e => e.ChildId == childId && e.ItemId == itemId)
            .OrderByDescending(e => e.At).ThenByDescending(e => e.Id)
            .Select(e => new HistoryResponse(e.At, e.Source.ToString(), e.StageValue, e.GivenAnswer, e.WasCorrect, e.PlanPositionId))
            .ToPagedListAsync(Response, skip, take);
        return events;
    }

    private static ItemProgressResponse MapRow(Row r) =>
        new(r.ItemId, r.ExerciseId, r.VocabularyId, r.Front, r.Back, r.Box, ItemProgress.MaxBox, r.MasteryPercent,
            r.SeenCount, r.CorrectCount, r.IntroducedAt, r.LastAnswerAt, r.LastCorrect, VocabLink.Path + r.VocabularyId);
}
