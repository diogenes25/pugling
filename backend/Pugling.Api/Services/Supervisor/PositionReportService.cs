using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Supervisor;

/// <summary>
/// Builds the learning report of a study plan position: for each content atom (e.g. vocabulary item) the
/// Leitner box state (box → mastery), introduction/due date and the test hit rate. Answers the supervisor's
/// question "which vocabulary is mastered, which isn't" – reconstructed in the position model from
/// <see cref="PositionItemProgress"/> and <see cref="TestItemResult"/> (replaces the plan-wide report of the old model).
/// </summary>
public class PositionReportService(PuglingDbContext db, PositionPlayService play)
{
    // ItemReport/Report live in the contract project (Pugling.Contracts.Supervisor).

    /// <summary>Mastery in percent from the Leitner box (box 1 = 0% … MaxBox = 100%).</summary>
    private static int MasteryOf(int box, int maxBox) =>
        maxBox <= 1 ? 100 : (int)Math.Round(100.0 * (Math.Clamp(box, 1, maxBox) - 1) / (maxBox - 1));

    /// <summary>Report of the position, or <c>null</c> if it (with exercise) does not exist in the plan.</summary>
    public async Task<Report?> BuildAsync(int planId, int positionId, CancellationToken ct = default)
    {
        var pos = await db.PlanPositions.AsNoTracking().Include(p => p.Exercise)
            .FirstOrDefaultAsync(p => p.Id == positionId && p.StudyPlanId == planId, ct);
        if (pos?.Exercise is null) return null;

        // Contents of the exercise (type-agnostic) - order = the stable ItemIndex.
        var items = await play.ItemsOfAsync(pos, ct: ct);

        // Leitner/introduction state per item (one plan = one child), filtered in the DB.
        var progress = await db.PositionItemProgress.AsNoTracking()
            .Where(p => p.PlanPositionId == positionId)
            .ToDictionaryAsync(p => p.ItemIndex, ct);

        // Test hit rate per item from completed attempts of this position.
        var testResults = await db.TestAttempts.AsNoTracking()
            .Where(a => a.PlanPositionId == positionId && a.CompletedAt != null)
            .SelectMany(a => a.Results)
            .Where(r => r.ItemIndex != null)
            .Select(r => new { Index = r.ItemIndex!.Value, r.WasCorrect })
            .ToListAsync(ct);
        var testsByItem = testResults.GroupBy(r => r.Index)
            .ToDictionary(g => g.Key, g => (Seen: g.Count(), Correct: g.Count(x => x.WasCorrect)));

        var maxBox = pos.MaxBox;
        // B-637: the stages in the order the child steps down them; a stage off the ladder (none today) sorts last.
        var ladder = pos.Remediation ? play.TypeOf(pos.Exercise)?.EaseLadder ?? [] : [];
        // Only tasks of the pool are remediated; an item beyond a position's item count would show as open forever.
        var pool = play.PoolSize(pos, items.Count);
        var rows = items.Select(item =>
        {
            progress.TryGetValue(item.Index, out var ip);
            testsByItem.TryGetValue(item.Index, out var tests);
            var introduced = ip?.IntroducedAt != null;
            var box = ip?.Box ?? 1;
            return new ItemReport(item.Index, item.Prompt, item.Answer, introduced,
                box, introduced ? MasteryOf(box, maxBox) : 0, ip?.ReviewCount ?? 0,
                ip?.DueOn, ip?.LastReviewedAt, tests.Seen, tests.Correct, item.Written is not null, item.RecognitionOnly,
                pos.Remediation && item.Index < pool ? RemediationOf(item, ip, ladder) : null);
        }).ToList();

        return new Report(pos.Id, pos.ExerciseId, pos.Exercise.Title, pos.Exercise.Type.ToString(),
            maxBox, rows.Count, rows.Count(r => r.Introduced), rows.Count(r => r.Box >= maxBox), rows,
            pos.Remediation, pos.CompletedOn);
    }

    // The cue is the gap's German prompt, which the content item carries as its hint (ClozeGaps.WithPrompt).
    private static RemediationTaskReport RemediationOf(ContentItem item, PositionItemProgress? ip, IReadOnlyList<int> ladder) =>
        new(item.Hint, ip?.RemediationDoneAt,
            [.. (ip?.SolvedByStage ?? [])
                .OrderBy(kv => ladder.Contains(kv.Key) ? ladder.TakeWhile(s => s != kv.Key).Count() : int.MaxValue).ThenBy(kv => kv.Key)
                .Select(kv => new StageSolveCount(kv.Key, kv.Value))]);
}
