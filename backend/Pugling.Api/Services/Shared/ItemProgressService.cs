using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// Advances the plan-spanning learning progress per (child, item) and logs every answer. Called at the
/// server-authoritative grading points (practice/test) – exclusively for vocabulary items that carry a
/// stable <see cref="ContentItem.ItemId"/>. Updates <see cref="ItemProgress"/> (box/mastery, counters,
/// last answer) and appends an <see cref="ItemReviewEvent"/> to the history. Deliberately without its
/// own <c>SaveChanges</c>: the calling controllers save bundled together with their remaining writes.
/// </summary>
public class ItemProgressService(PuglingDbContext db, TimeProvider time)
{
    /// <summary>Mastery in percent derived from the Leitner box (box 1 = 0% … MaxBox = 100%; as in the position report).</summary>
    private static int MasteryOf(int box) =>
        (int)Math.Round(100.0 * (Math.Clamp(box, 1, ItemProgress.MaxBox) - 1) / (ItemProgress.MaxBox - 1));

    /// <summary>
    /// Logs a graded answer for an item. If the content carries no stable item/store identity
    /// (non-vocabulary types), nothing happens. The answer history (<see cref="ItemReviewEvent"/>) is
    /// always written; the aggregated learning progress (<see cref="ItemProgress"/>: box/mastery/counters)
    /// is only advanced if the answer <paramref name="countsForMastery"/> is true – otherwise the box
    /// could be farmed up by repeating the same card within one session (anti-farming, as with the
    /// position engine). No <c>SaveChanges</c> – see class doc.
    /// <paramref name="accentSlip"/> marks a miss that practice counted half (B-624); the learning progress still records
    /// it as the miss <paramref name="wasCorrect"/> says (decision 7), the flag only tells the report why.
    /// </summary>
    public async Task RecordAsync(int childId, int exerciseId, ContentItem item, bool wasCorrect, int stageValue,
        string? givenAnswer, ItemReviewSource source, int? planPositionId, DateOnly today, bool countsForMastery,
        bool accentSlip, CancellationToken ct = default)
    {
        if (item.ItemId is not { } itemId || item.VocabularyId is not { } vocabId) return;

        var now = time.GetUtcNow().UtcDateTime;

        // The history records every answer - including ungraded repetitions (that is genuine history).
        db.ItemReviewEvents.Add(new ItemReviewEvent
        {
            ChildId = childId,
            ItemId = itemId,
            ExerciseId = exerciseId,
            VocabularyId = vocabId,
            PlanPositionId = planPositionId,
            Source = source,
            StageValue = stageValue,
            GivenAnswer = givenAnswer,
            WasCorrect = wasCorrect,
            AccentSlip = accentSlip,
            At = now,
        });

        if (!countsForMastery) return;

        // The tracker first: a test submission records all its answers before saving, and with "both" two cards share
        // one item (B-472). The query alone does not see a row added earlier in the same run - a second row for the
        // same item would then break the unique index.
        var prog = db.ItemProgress.Local.FirstOrDefault(p => p.ChildId == childId && p.ItemId == itemId)
            ?? await db.ItemProgress.FirstOrDefaultAsync(p => p.ChildId == childId && p.ItemId == itemId, ct);
        if (prog is null)
        {
            prog = new ItemProgress { ChildId = childId, ItemId = itemId, IntroducedAt = today };
            db.ItemProgress.Add(prog);
        }
        // Keep the denormalized references current (the item may have been reassigned to another vocabulary entry).
        prog.ExerciseId = exerciseId;
        prog.VocabularyId = vocabId;
        prog.IntroducedAt ??= today;
        prog.SeenCount++;
        if (wasCorrect) prog.CorrectCount++;
        // Leitner step: correct → one box up (capped), wrong → back to box 1 (as in the position engine).
        prog.Box = wasCorrect ? Math.Min(ItemProgress.MaxBox, Math.Max(1, prog.Box) + 1) : 1;
        prog.MasteryPercent = MasteryOf(prog.Box);
        prog.LastAnswerAt = now;
        prog.LastCorrect = wasCorrect;
    }
}
