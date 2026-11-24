using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Supervisor;

/// <summary>
/// The supervisor's judgement of a child's feedback (B-767) and, for "justified", the correction it books (B-768): the
/// attempt's correction trail, then - as the world would have been with the right grading, no more - the missed duty's
/// penalty taken back and a missing goal reward paid. Learning state (box, mastery), missions, awards and an opened chest
/// are not re-evaluated (B-627 decisions 5 and 7); the streak follows by itself, since it reads the duty.
/// </summary>
public class FeedbackCorrectionService(PuglingDbContext db, ExerciseFeedbackService feedback,
    PositionProgressService progress, TimeProvider time)
{
    /// <summary>
    /// Applies the judgement and, for "justified", books the correction - one transaction, with the child's wallet
    /// stamp bumped on every justified path, also one that only adds coins (the wallet invariant, B-627 decision 6; a
    /// parallel purchase retries once). The caller has
    /// checked the change (<see cref="ExerciseFeedbackService.CheckChange"/>). A justified feedback can be confirmed
    /// again: the trail is recomputed from all justified answers and every booking has its own idempotency mark.
    /// </summary>
    /// <exception cref="DbUpdateConcurrencyException">A parallel booking on the child's wallet won; retry.</exception>
    public async Task JudgeAsync(ExerciseFeedback entry, FeedbackStatus status, FeedbackOutcome? outcome, int? adultId,
        CancellationToken ct)
    {
        feedback.SetStatus(entry, status, outcome, adultId);
        if (entry.Outcome != FeedbackOutcome.Justified || entry.TestItemResultId is not { } resultId)
        {
            await db.SaveChangesAsync(ct);
            return;
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // The child first: its stamp serializes this booking against a purchase or a second confirmation running at once.
        var child = await db.Children.FirstAsync(c => c.Id == entry.ChildId, ct);
        var attempt = await db.TestAttempts.Include(t => t.Results)
            .Include(t => t.PlanPosition).ThenInclude(p => p!.Exercise).ThenInclude(e => e!.Prerequisites)
            .Include(t => t.StudyPlan)
            .FirstAsync(t => t.Results.Any(r => r.Id == resultId), ct);
        Correct(attempt, await JustifiedWrongAnswersAsync(entry, attempt, ct));
        child.ConcurrencyStamp = Guid.NewGuid();
        await db.SaveChangesAsync(ct);

        // The period is judged anew from the saved trail - IsGoalMetAsync reads the database.
        if (attempt.CorrectedItems is > 0 && attempt.PlanPosition is { } pos && attempt.StudyPlan is { } plan
            && await progress.IsGoalMetAsync(pos, plan, attempt.Day, ct))
        {
            var now = time.GetUtcNow().UtcDateTime;
            if (await progress.OpenPenaltyAroundAsync(pos.Id, attempt.Day, ct) is { } penalty)
            {
                penalty.RefundedAt = now;
                penalty.RefundedByFeedbackId = entry.Id;
                // A positive GoalPenalty is a correction, not earned coins (WalletService.EarnedCoinKinds).
                db.ChildPointsEntries.Add(new ChildPointsEntry
                {
                    ChildId = entry.ChildId,
                    Kind = PointKind.GoalPenalty,
                    Amount = penalty.Points,
                    Reason = $"[{plan.Title} · {pos.Exercise?.Title}] Malus zurückgenommen (berechtigte Meldung #{entry.Id})",
                    CreatedAt = now,
                });
            }
            await progress.AddGoalRewardIfDueAsync(pos, plan, attempt.Day, now, ct);
            // The stamp was bumped above, in this transaction: it holds SQLite's write lock throughout, so no
            // purchase slips in between - one bump serializes every booking of this confirmation.
            await db.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);
    }

    /// <summary>
    /// The attempt's wrongly graded answers confirmed as justified: by this feedback and by every other justified one -
    /// counted per answer, so two reasons reported on the same answer correct it once.
    /// </summary>
    private async Task<int> JustifiedWrongAnswersAsync(ExerciseFeedback entry, TestAttempt attempt, CancellationToken ct)
    {
        var wrong = attempt.Results.Where(r => !r.WasCorrect && r.ItemIndex != null).Select(r => r.Id).ToList();
        var justified = await db.ExerciseFeedbacks.AsNoTracking()
            .Where(f => f.Id != entry.Id && f.Outcome == FeedbackOutcome.Justified && f.TestItemResultId != null
                && wrong.Contains(f.TestItemResultId.Value))
            .Select(f => f.TestItemResultId!.Value)
            .ToListAsync(ct);
        return justified.Append(entry.TestItemResultId!.Value).Where(wrong.Contains).Distinct().Count();
    }

    /// <summary>
    /// Writes the correction trail (decisions 1 and 7): the corrected count and "passed" with the threshold of now. A
    /// correction never takes a pass away - a threshold raised since must not fail a test the child passed. Without a
    /// corrected answer (the reported one was graded right) the trail stays empty.
    /// </summary>
    private static void Correct(TestAttempt attempt, int corrected)
    {
        if (corrected == 0) return;
        var correct = Math.Min(attempt.CorrectItems + corrected, attempt.TotalItems);
        var score = attempt.TotalItems == 0 ? 0 : (int)Math.Round(100.0 * correct / attempt.TotalItems);
        var pos = attempt.PlanPosition;
        attempt.CorrectedItems = corrected;
        attempt.PassedAfterCorrection = attempt.Passed
            || (pos is not null && score >= PositionProgressService.TestPassPercent(pos));
    }
}
