using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Student;

/// <summary>
/// Baut den Lern-Report einer Lehrplan-Position: je Inhalts-Atom (z. B. Vokabel) den Karteikasten-Stand
/// (Box → Beherrschung), Einführung/Fälligkeit und die Test-Trefferquote. Beantwortet dem Vater die Frage
/// „welche Vokabel sitzt, welche nicht" – im Positions-Modell aus <see cref="PositionItemProgress"/> und
/// <see cref="TestItemResult"/> rekonstruiert (ersetzt den plan-weiten Report des Alt-Modells).
/// </summary>
public class PositionReportService(PuglingDbContext db, PositionPlayService play)
{
    /// <summary>Report-Zeile eines einzelnen Inhalts.</summary>
    public record ItemReport(int ItemIndex, string Prompt, string Answer, bool Introduced,
        int Box, int MasteryPercent, int ReviewCount, DateOnly? DueOn, DateTime? LastReviewedAt,
        int TestsSeen, int TestsCorrect);

    /// <summary>Report einer Position samt Kopf-Kennzahlen (eingeführt/beherrscht).</summary>
    public record Report(int PositionId, int ExerciseId, string ExerciseTitle, string ExerciseType,
        int MaxBox, int TotalItems, int IntroducedItems, int MasteredItems, IReadOnlyList<ItemReport> Items);

    /// <summary>Beherrschung in Prozent aus der Leitner-Box (Box 1 = 0 % … MaxBox = 100 %).</summary>
    private static int MasteryOf(int box, int maxBox) =>
        maxBox <= 1 ? 100 : (int)Math.Round(100.0 * (Math.Clamp(box, 1, maxBox) - 1) / (maxBox - 1));

    /// <summary>Report der Position oder <c>null</c>, wenn sie (mit Übung) im Plan nicht existiert.</summary>
    public async Task<Report?> BuildAsync(int planId, int positionId, CancellationToken ct = default)
    {
        var pos = await db.PlanPositions.AsNoTracking().Include(p => p.Exercise)
            .FirstOrDefaultAsync(p => p.Id == positionId && p.StudyPlanId == planId, ct);
        if (pos?.Exercise is null) return null;

        // Inhalte der Übung (verfahrensneutral) – Reihenfolge = stabiler ItemIndex.
        var items = await play.ItemsOfAsync(pos);

        // Leitner-/Einführungsstand je Item (ein Plan = ein Kind), in der DB gefiltert.
        var progress = await db.PositionItemProgress.AsNoTracking()
            .Where(p => p.PlanPositionId == positionId)
            .ToDictionaryAsync(p => p.ItemIndex, ct);

        // Test-Trefferquote je Item aus abgeschlossenen Versuchen dieser Position.
        var testResults = await db.TestAttempts.AsNoTracking()
            .Where(a => a.PlanPositionId == positionId && a.CompletedAt != null)
            .SelectMany(a => a.Results)
            .Where(r => r.ItemIndex != null)
            .Select(r => new { Index = r.ItemIndex!.Value, r.WasCorrect })
            .ToListAsync(ct);
        var testsByItem = testResults.GroupBy(r => r.Index)
            .ToDictionary(g => g.Key, g => (Seen: g.Count(), Correct: g.Count(x => x.WasCorrect)));

        var maxBox = pos.MaxBox;
        var rows = items.Select(item =>
        {
            progress.TryGetValue(item.Index, out var ip);
            testsByItem.TryGetValue(item.Index, out var tests);
            var introduced = ip?.IntroducedAt != null;
            var box = ip?.Box ?? 1;
            return new ItemReport(item.Index, item.Prompt, item.Answer, introduced,
                box, introduced ? MasteryOf(box, maxBox) : 0, ip?.ReviewCount ?? 0,
                ip?.DueOn, ip?.LastReviewedAt, tests.Seen, tests.Correct);
        }).ToList();

        return new Report(pos.Id, pos.ExerciseId, pos.Exercise.Title, pos.Exercise.Type.ToString(),
            maxBox, rows.Count, rows.Count(r => r.Introduced), rows.Count(r => r.Box >= maxBox), rows);
    }
}
