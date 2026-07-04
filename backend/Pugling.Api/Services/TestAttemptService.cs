using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services;

/// <summary>
/// Gemeinsamer Lebenszyklus der mehrstufigen Abschlusstests (Vokabel/Lückentext/Zuordnung):
/// Plan und Versuch laden sowie einen bewerteten Versuch abschließen und Punkte vergeben.
/// Die verfahrensspezifische Aufgaben-Auswahl und Antwort-Bewertung bleibt im jeweiligen Controller.
/// </summary>
public class TestAttemptService(PuglingDbContext db, StudyProgressService progress, GamificationService gamification)
{
    /// <summary>Lädt den Lehrplan (ohne Items).</summary>
    public Task<StudyPlan?> GetPlanAsync(int planId) =>
        db.StudyPlans.FirstOrDefaultAsync(p => p.Id == planId);

    /// <summary>Lädt einen Testversuch samt Einzelergebnissen – nur im angegebenen Plan.</summary>
    public Task<TestAttempt?> LoadAttemptAsync(int planId, int attemptId) =>
        db.TestAttempts.Include(t => t.Results)
            .FirstOrDefaultAsync(t => t.Id == attemptId && t.StudyPlanId == planId);

    /// <summary>
    /// Schließt einen Versuch ab: zählt die Treffer, berechnet Prozentwert und Bestehen,
    /// speichert und vergibt die fälligen Tagespunkte. Die Einzel-Ergebnisse
    /// (<see cref="TestItemResult.WasCorrect"/>) müssen vom Controller vorher gesetzt sein.
    /// </summary>
    public async Task<StudyProgressService.DayProgress> ScoreAndAwardAsync(TestAttempt attempt, StudyPlan plan)
    {
        attempt.CorrectItems = attempt.Results.Count(r => r.WasCorrect);
        attempt.ScorePercent = attempt.TotalItems == 0 ? 0
            : (int)Math.Round(100.0 * attempt.CorrectItems / attempt.TotalItems);
        attempt.Passed = attempt.ScorePercent >= plan.DailyTestPassPercent;
        attempt.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        var dayProgress = await progress.EvaluateAndAwardAsync(plan, attempt.Day);
        // Missionen/Auszeichnungen auch beim Test-Abschluss auswerten (z.B. Metrik TestsPassed): sonst
        // würden test-getriebene Belohnungen erst bei einer späteren Leitner-Wiederholung gutgeschrieben.
        await gamification.EvaluateAndAwardAsync(plan.ChildId, attempt.Day);
        return dayProgress;
    }
}
