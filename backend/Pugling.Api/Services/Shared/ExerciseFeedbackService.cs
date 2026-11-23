using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// A child's feedback on a card (B-628): writing it with the merge of a repeated open reason, and the three views on it -
/// the supervisor's full list, the creator's counted reasons and the child's done notices. Writes only
/// <see cref="ExerciseFeedback"/> rows: no learning state, points, duty or session is touched (B-623, decision 6).
/// </summary>
public class ExerciseFeedbackService(PuglingDbContext db, PositionPlayService play, TimeProvider time)
{
    /// <summary>Upper bound of one report's free text.</summary>
    public const int MaxTextLength = 500;

    /// <summary>Upper bound of the joined texts of a merged entry - the length of the column; the oldest text goes first.</summary>
    private const int MaxJoinedTextLength = 2000;

    /// <summary>
    /// Writes the report: one entry per reason (one without a reason for a text-only report). An open entry of the same
    /// child, exercise, card and reason is merged into - count up, text appended (decision 9). <paramref name="card"/> is the
    /// card reported on, already checked to belong to the position's exercise, or <c>null</c> for the whole exercise.
    /// <para>
    /// The card is anchored by its index like the learning state itself (<see cref="PositionItemProgress.ItemIndex"/>):
    /// a played exercise's list may not shrink or reorder through the item endpoints (B-328), so the index keeps meaning
    /// the same card. No unique index guards the merge - nullable card and reason would need filtered indexes, and two
    /// reports in the same instant only make two rows whose counts the creator's sum still adds up.
    /// </para>
    /// </summary>
    /// <returns>The entries written or merged into, in the order of the reasons.</returns>
    public async Task<IReadOnlyList<int>> ReportAsync(int childId, PlanPosition pos, DateOnly day, ContentItem? card,
        IReadOnlyList<FeedbackReason> reasons, string? text, CancellationToken ct)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var itemIndex = card?.Index;
        // B-848: the card belongs to the exercise the position plays, the prerequisite on a build path.
        var exerciseId = PositionPlayService.PlayedExerciseId(pos, day);
        var entries = new List<ExerciseFeedback>();
        foreach (var reason in reasons.Count > 0 ? reasons.Select(r => (FeedbackReason?)r) : [null])
        {
            var entry = await db.ExerciseFeedbacks.FirstOrDefaultAsync(f => f.ChildId == childId
                && f.ExerciseId == exerciseId && f.ItemIndex == itemIndex && f.Reason == reason
                && f.Status == FeedbackStatus.Open, ct);
            if (entry is null)
            {
                entry = new ExerciseFeedback
                {
                    ChildId = childId,
                    ExerciseId = exerciseId,
                    ItemIndex = itemIndex,
                    ExerciseItemId = card?.ItemId,
                    VocabularyId = card?.VocabularyId,
                    Reason = reason,
                    Count = 0,
                    CreatedAt = now,
                };
                db.ExerciseFeedbacks.Add(entry);
            }
            entry.Count++;
            entry.PlanPositionId = pos.Id;
            entry.LastReportedAt = now;
            entry.Text = Join(entry.Text, text);
            entries.Add(entry);
        }
        await db.SaveChangesAsync(ct);
        return [.. entries.Select(e => e.Id)];
    }

    // A position over a bare exercise: it has no build path, so the day it is resolved for reads nothing.
    private static PlanPosition BareExercise(Exercise exercise) => new() { Exercise = exercise };

    // The texts of a merged entry, newest last; over the column's length the oldest characters go.
    private static string? Join(string? before, string? added)
    {
        if (string.IsNullOrEmpty(added)) return before;
        var joined = string.IsNullOrEmpty(before) ? added : before + "\n" + added;
        return joined.Length <= MaxJoinedTextLength ? joined : joined[^MaxJoinedTextLength..];
    }

    /// <summary>
    /// The feedback of the given children for the supervisor, open first, newest report first, each with the card as it
    /// is now and the child's most recent stored answer to it.
    /// </summary>
    public async Task<IReadOnlyList<FeedbackResponse>> ListAsync(IReadOnlyCollection<int> childIds, FeedbackStatus? status,
        int? take, CancellationToken ct)
    {
        var query = db.ExerciseFeedbacks.AsNoTracking()
            .Include(f => f.Child).Include(f => f.Exercise).Include(f => f.PlanPosition)
            .Where(f => childIds.Contains(f.ChildId) && (status == null || f.Status == status))
            // Open first as a comparison, not by the column: the status is stored as text, and "Done" sorts before
            // "Open" - with `take` the open reports would be the ones cut off.
            .OrderBy(f => f.Status == FeedbackStatus.Open ? 0 : 1)
            .ThenByDescending(f => f.LastReportedAt).ThenByDescending(f => f.Id);
        var rows = await (take is { } n ? query.Take(n) : query).ToListAsync(ct);

        var cards = new Dictionary<int, IReadOnlyList<ContentItem>>();
        var result = new List<FeedbackResponse>(rows.Count);
        foreach (var f in rows)
        {
            ContentItem? card = null;
            if (f.ItemIndex is { } index && f.Exercise is { } exercise)
            {
                if (!cards.TryGetValue(exercise.Id, out var items))
                    cards[exercise.Id] = items = await play.ItemsOfAsync(BareExercise(exercise), default, ct: ct);
                card = items.ElementAtOrDefault(index);
            }
            result.Add(new FeedbackResponse(f.Id, f.ChildId, f.Child?.Name ?? "", f.PlanPosition?.StudyPlanId,
                f.PlanPositionId, f.ExerciseId, f.Exercise?.Title ?? "", f.ItemIndex, card?.Prompt, card?.Answer,
                await ChildAnswerAsync(f, ct), f.Reason, f.Reason is { } r ? FeedbackReasons.ReachOf(r) : null, f.Count,
                f.Text, f.Status, f.CreatedAt, f.LastReportedAt, f.DoneAt));
        }
        return result;
    }

    /// <summary>
    /// The child's most recent stored answer to the card: from its tests on the position and, for a vocabulary item, from
    /// its answer history. A practice answer to a non-vocabulary card is not stored anywhere, so it stays <c>null</c>.
    /// </summary>
    private async Task<string?> ChildAnswerAsync(ExerciseFeedback f, CancellationToken ct)
    {
        if (f.ItemIndex is not { } index) return null;
        // B-849: only attempts on the exercise reported on - on a build path an index of another level names another
        // card. A prerequisite is examined on its own level; the position's own exercise on the target level, or without
        // a level where the position had no build path then.
        int? prerequisiteLevel = f.PlanPosition is { } p && p.ExerciseId != f.ExerciseId ? f.Exercise?.BuildLevel : null;
        var tested = f.PlanPositionId is { } positionId
            // TestItemResult has no DbSet of its own; it is reached through its attempt. A scalar subquery, not a
            // SelectMany: the latter needs SQL APPLY, which SQLite does not have.
            ? await db.TestAttempts.AsNoTracking()
                .Where(PositionPlayService.AttemptsOnLevel(prerequisiteLevel))
                .Where(t => t.PlanPositionId == positionId && !t.BySupervisor && t.CompletedAt != null
                    && t.Results.Any(r => r.ItemIndex == index && r.GivenAnswer != null))
                .OrderByDescending(t => t.CompletedAt).ThenByDescending(t => t.Id)
                .Select(t => new
                {
                    GivenAnswer = t.Results.Where(r => r.ItemIndex == index && r.GivenAnswer != null)
                        .OrderByDescending(r => r.Id).Select(r => r.GivenAnswer).FirstOrDefault(),
                    At = t.CompletedAt!.Value,
                })
                .FirstOrDefaultAsync(ct)
            : null;
        var practiced = f.ExerciseItemId is { } itemId
            ? await db.ItemReviewEvents.AsNoTracking()
                .Where(e => e.ChildId == f.ChildId && e.ItemId == itemId && e.GivenAnswer != null)
                .OrderByDescending(e => e.At).ThenByDescending(e => e.Id)
                .Select(e => new { e.GivenAnswer, e.At })
                .FirstOrDefaultAsync(ct)
            : null;
        if (tested is null) return practiced?.GivenAnswer;
        if (practiced is null) return tested.GivenAnswer;
        return practiced.At > tested.At ? practiced.GivenAnswer : tested.GivenAnswer;
    }

    /// <summary>Marks the entry done (for every supervisor) or open again; a new done raises a fresh notice for the child.</summary>
    public async Task SetStatusAsync(ExerciseFeedback entry, FeedbackStatus status, int? adultId, CancellationToken ct)
    {
        if (entry.Status != status)
        {
            entry.Status = status;
            var done = status == FeedbackStatus.Done;
            entry.DoneAt = done ? time.GetUtcNow().UtcDateTime : null;
            entry.DoneByAdultId = done ? adultId : null;
            entry.SeenByChildAt = null;
        }
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// The counted reasons of an exercise for its creator: no child, no text (decision 7). Text-only entries carry no
    /// reason and are left out.
    /// </summary>
    public async Task<ExerciseFeedbackSummary> SummaryAsync(int exerciseId, CancellationToken ct)
    {
        var rows = await db.ExerciseFeedbacks.AsNoTracking()
            .Where(f => f.ExerciseId == exerciseId && f.Reason != null)
            .Select(f => new { f.ItemIndex, Reason = f.Reason!.Value, f.Count })
            .ToListAsync(ct);

        static IReadOnlyList<FeedbackReasonCount> Count(IEnumerable<(FeedbackReason Reason, int Count)> source) =>
        [
            .. source.GroupBy(x => x.Reason)
                .Select(g => new FeedbackReasonCount(g.Key, FeedbackReasons.ReachOf(g.Key), g.Sum(x => x.Count)))
                .OrderByDescending(c => c.Count).ThenBy(c => c.Reason),
        ];

        return new ExerciseFeedbackSummary(exerciseId, rows.Sum(r => r.Count),
            Count(rows.Select(r => (r.Reason, r.Count))),
            [
                .. rows.GroupBy(r => r.ItemIndex).OrderBy(g => g.Key ?? -1)
                    .Select(g => new FeedbackCardCount(g.Key, Count(g.Select(r => (r.Reason, r.Count))))),
            ]);
    }

    /// <summary>The child's done, not yet acknowledged feedback (decision 8), newest first, with the card's prompt.</summary>
    public async Task<IReadOnlyList<FeedbackNotice>> NoticesAsync(int childId, CancellationToken ct)
    {
        var rows = await db.ExerciseFeedbacks.AsNoTracking().Include(f => f.Exercise)
            .Where(f => f.ChildId == childId && f.Status == FeedbackStatus.Done && f.SeenByChildAt == null)
            .OrderByDescending(f => f.DoneAt).ThenByDescending(f => f.Id)
            .ToListAsync(ct);
        var cards = new Dictionary<int, IReadOnlyList<ContentItem>>();
        var notices = new List<FeedbackNotice>(rows.Count);
        foreach (var f in rows)
        {
            string? cue = null;
            if (f.ItemIndex is { } index && f.Exercise is { } exercise)
            {
                if (!cards.TryGetValue(exercise.Id, out var items))
                    cards[exercise.Id] = items = await play.ItemsOfAsync(BareExercise(exercise), default, ct: ct);
                cue = items.ElementAtOrDefault(index)?.Prompt;
            }
            notices.Add(new FeedbackNotice(f.Id, f.ExerciseId, f.Exercise?.Title ?? "", f.ItemIndex, cue, f.Reason,
                f.DoneAt!.Value));
        }
        return notices;
    }
}
