using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services;

/// <summary>
/// Bewertet die Tages-Anforderungen eines Lehrplans (Übungszeit + bestandener Test)
/// und schreibt Punkte idempotent gut (jede Belohnung höchstens einmal pro Tag).
/// </summary>
public class StudyProgressService(PuglingDbContext db)
{
    public record DayProgress(
        DateOnly Day, int MinutesPracticed, bool MinutesMet,
        int TestAttempts, int? BestScorePercent, bool TestPassed,
        bool DayComplete, int PointsAwarded, IReadOnlyList<string> Outstanding);

    /// <summary>Berechnet den Fortschritt eines einzelnen Tages (ohne Punktevergabe).</summary>
    public async Task<DayProgress> ComputeDayAsync(StudyPlan plan, DateOnly day)
    {
        var seconds = await db.PracticeSessions
            .Where(s => s.StudyPlanId == plan.Id && s.Day == day)
            .SumAsync(s => (int?)s.ActiveSeconds) ?? 0;
        var minutes = seconds / 60;
        var minutesMet = minutes >= plan.DailyMinutesRequired;

        var attempts = await db.TestAttempts
            .Where(t => t.StudyPlanId == plan.Id && t.Day == day && t.CompletedAt != null)
            .ToListAsync();
        int? best = attempts.Count == 0 ? null : attempts.Max(t => t.ScorePercent);
        // Bei RequireTypedTest zählt nur ein bestandener Test auf gewerteter (getippter) Stufe.
        var testPassed = attempts.Any(t => t.Passed && (!plan.RequireTypedTest || t.Graded));

        var pointsAwarded = await db.StudyDayRewards
            .Where(r => r.StudyPlanId == plan.Id && r.Day == day)
            .SumAsync(r => (int?)r.Points) ?? 0;

        var testReq = !plan.DailyTestRequired || testPassed;
        var dayComplete = minutesMet && testReq;

        var outstanding = new List<string>();
        if (!minutesMet)
            outstanding.Add($"Noch {plan.DailyMinutesRequired - minutes} min üben (bisher {minutes}/{plan.DailyMinutesRequired}).");
        if (plan.DailyTestRequired && !testPassed)
        {
            var typedButUngraded = plan.RequireTypedTest && attempts.Any(t => t.Passed && !t.Graded);
            outstanding.Add(typedButUngraded
                ? $"Test muss getippt werden (Stufe ≥ Buchstabenfelder), Selbsteinschätzung zählt nicht."
                : best is null
                    ? $"Abschlusstest noch offen (mind. {plan.DailyTestPassPercent}%)."
                    : $"Test noch nicht bestanden (bisher beste {best}%, nötig {plan.DailyTestPassPercent}%).");
        }

        return new DayProgress(day, minutes, minutesMet, attempts.Count, best, testPassed,
            dayComplete, pointsAwarded, outstanding);
    }

    /// <summary>
    /// Wertet den Tag nach einer Aktivität aus und schreibt fällige Punkte gut.
    /// Gibt den aktuellen Fortschritt zurück.
    /// </summary>
    public async Task<DayProgress> EvaluateAndAwardAsync(StudyPlan plan, DateOnly day)
    {
        var before = await ComputeDayAsync(plan, day);

        if (before.MinutesMet)
            await AwardAsync(plan, day, RewardKind.MinutesMet, plan.PointsMinutesMet,
                $"Tagesziel {plan.DailyMinutesRequired} min geübt");

        if (before.TestPassed)
            await AwardAsync(plan, day, RewardKind.TestPassed, plan.PointsTestPassed,
                $"Abschlusstest bestanden ({before.BestScorePercent}%)");

        if (before.DayComplete)
            await AwardAsync(plan, day, RewardKind.DayCompleteBonus, plan.PointsDayCompleteBonus,
                "Tag vollständig geschafft (Zeit + Test)");

        return await ComputeDayAsync(plan, day);
    }

    /// <summary>Vergibt eine Belohnung genau einmal je (Plan, Tag, Art) und bucht die Punkte dem Kind gut.</summary>
    private async Task AwardAsync(StudyPlan plan, DateOnly day, RewardKind kind, int points, string reason)
    {
        if (points <= 0) return;
        if (await db.StudyDayRewards.AnyAsync(r => r.StudyPlanId == plan.Id && r.Day == day && r.Kind == kind))
            return;

        db.StudyDayRewards.Add(new StudyDayReward { StudyPlanId = plan.Id, Day = day, Kind = kind, Points = points });
        db.ChildPoints.Add(new ChildPointsEntry
        {
            ChildId = plan.ChildId,
            Kind = kind switch
            {
                RewardKind.MinutesMet => PointKind.Minutes,
                RewardKind.TestPassed => PointKind.Test,
                _ => PointKind.DayComplete,
            },
            Amount = points,
            Reason = $"[{plan.Title}] {day:yyyy-MM-dd}: {reason}",
        });
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Paralleler Doppel-Request: der Unique-Index (Plan, Tag, Art) hat bereits gegriffen.
            // Belohnung gilt als vergeben – keine doppelten Punkte, kein 500.
            foreach (var entry in db.ChangeTracker.Entries().Where(e => e.State == EntityState.Added))
                entry.State = EntityState.Detached;
        }
    }

    /// <summary>Getippte Vokabel-Stufen, die echtes Wissen abfragen (nicht bloße Selbsteinschätzung).</summary>
    public static bool IsTyped(TestStage stage) =>
        stage is TestStage.LetterBoxes or TestStage.FreeText or TestStage.Audio;

    /// <summary>Freitext-Stufen des Lückentexts (echtes Schreiben statt Auswahl).</summary>
    public static bool IsTyped(ClozeStage stage) =>
        stage is ClozeStage.TranslationFreeText or ClozeStage.FreeText;

    /// <summary>Ermittelt die für einen Tag geplante Stufe aus dem Fahrplan (sonst DefaultStage).</summary>
    public static int StageForDay(StudyPlan plan, DateOnly day)
    {
        var dayNumber = day.DayNumber - plan.StartDate.DayNumber + 1;
        var step = plan.StageSchedule?
            .Where(s => s.DayNumber <= dayNumber)
            .OrderByDescending(s => s.DayNumber)
            .FirstOrDefault();
        return step?.Stage ?? plan.DefaultStage;
    }

    /// <summary>Normalisiert eine Antwort für den Vergleich (trim, klein, Mehrfach-Leerzeichen).</summary>
    public static string Normalize(string? s) =>
        string.Join(' ', (s ?? "").Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
