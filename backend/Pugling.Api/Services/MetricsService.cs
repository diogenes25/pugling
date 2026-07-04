using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services;

/// <summary>
/// Berechnet die Fortschritts-Metriken eines Kindes serverseitig aus den bestehenden Tabellen
/// (Übungssitzungen, Reviews, Tests, Tagesbelohnungen). Eine gemeinsame Quelle für Missionen
/// (zeitfenster-bezogen) und Auszeichnungen (lebenslang bzw. aktuelle Serie). Nur Lese-Queries.
/// </summary>
public class MetricsService(PuglingDbContext db)
{
    /// <summary>
    /// Wert einer Metrik für ein Kind im halboffenen Tagesfenster [<paramref name="from"/>, <paramref name="to"/>]
    /// (beide inklusive; null = unbegrenzt). <paramref name="today"/> dient der Serien-Berechnung.
    /// </summary>
    public async Task<int> ValueAsync(int childId, ProgressMetric metric, DateOnly? from, DateOnly? to, DateOnly today)
    {
        var lo = from ?? DateOnly.MinValue;
        var hi = to ?? DateOnly.MaxValue;

        return metric switch
        {
            ProgressMetric.NewWords => await db.StudyPlanItems
                .Where(i => i.StudyPlan!.ChildId == childId && i.IntroducedAt != null
                    && i.IntroducedAt >= lo && i.IntroducedAt <= hi)
                .CountAsync(),

            ProgressMetric.CorrectReviews => await db.ReviewEvents
                .Where(r => r.WasCorrect && r.PracticeSession!.StudyPlan!.ChildId == childId
                    && r.PracticeSession!.Day >= lo && r.PracticeSession!.Day <= hi)
                .CountAsync(),

            ProgressMetric.TestsPassed => await db.TestAttempts
                .Where(t => t.Passed && t.CompletedAt != null && t.StudyPlan!.ChildId == childId
                    && t.Day >= lo && t.Day <= hi)
                .CountAsync(),

            ProgressMetric.MinutesPracticed => (await db.PracticeSessions
                .Where(s => s.StudyPlan!.ChildId == childId && s.Day >= lo && s.Day <= hi)
                .SumAsync(s => (int?)s.ActiveSeconds) ?? 0) / 60,

            ProgressMetric.DaysComplete => await CompleteDaysQuery(childId)
                .Where(d => d >= lo && d <= hi)
                .Distinct()
                .CountAsync(),

            ProgressMetric.StreakDays => await CurrentStreakAsync(childId, today),

            _ => 0,
        };
    }

    /// <summary>Tage, an denen mindestens ein Lehrplan des Kindes vollständig war (DayComplete-Belohnung).</summary>
    private IQueryable<DateOnly> CompleteDaysQuery(int childId) =>
        from reward in db.StudyDayRewards
        join plan in db.StudyPlans on reward.StudyPlanId equals plan.Id
        where plan.ChildId == childId && reward.Kind == RewardKind.DayCompleteBonus
        select reward.Day;

    /// <summary>Länge der aktuellen Serie vollständiger Tage bis (einschließlich) heute oder gestern.</summary>
    private async Task<int> CurrentStreakAsync(int childId, DateOnly today)
    {
        var days = await CompleteDaysQuery(childId).Distinct().ToListAsync();
        if (days.Count == 0) return 0;
        var set = days.ToHashSet();

        // Die Serie darf heute noch offen sein: zählt ab heute, wenn heute schon vollständig, sonst ab gestern.
        var cursor = set.Contains(today) ? today : today.AddDays(-1);
        var streak = 0;
        while (set.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }
        return streak;
    }
}
