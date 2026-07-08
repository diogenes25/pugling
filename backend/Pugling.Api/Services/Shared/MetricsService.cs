using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// Berechnet die Fortschritts-Metriken eines Kindes serverseitig aus den bestehenden Tabellen
/// (Übungssitzungen, Reviews, Tests, Tagesbelohnungen). Eine gemeinsame Quelle für Missionen
/// (zeitfenster-bezogen) und Auszeichnungen (lebenslang bzw. aktuelle Serie). Nur Lese-Queries.
/// </summary>
public class MetricsService(PuglingDbContext db, PositionProgressService progress)
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
            ProgressMetric.NewWords => await db.PositionItemProgress
                .Where(p => p.PlanPosition!.StudyPlan!.ChildId == childId && p.IntroducedAt != null
                    && p.IntroducedAt >= lo && p.IntroducedAt <= hi)
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

            ProgressMetric.DaysComplete => (await CompleteDaysAsync(childId, today))
                .Count(d => d >= lo && d <= hi),

            ProgressMetric.StreakDays => CurrentStreak(await CompleteDaysAsync(childId, today), today),

            _ => 0,
        };
    }

    /// <summary>
    /// Tage bis (einschließlich) <paramref name="until"/>, an denen die Tagespflicht eines Plans des Kindes
    /// vollständig erledigt war – <b>dieselbe</b> Regel (<see cref="PositionProgressService.DayOverview.DutyDone"/>),
    /// die auch die Tagesmission/Overview-Serie beim Sohn nutzt. Bewusst über den Fortschritts-Service statt über
    /// eine reine Belohnungs-Query: „mindestens ein Ziel gebucht" ≠ „Tag vollständig", und Missionen/Auszeichnungen
    /// dürfen nicht bei nur teil-erledigten Tagen feuern.
    /// </summary>
    private async Task<IReadOnlyCollection<DateOnly>> CompleteDaysAsync(int childId, DateOnly until)
    {
        var plans = await db.StudyPlans.AsNoTracking().Where(p => p.ChildId == childId).ToListAsync();
        var complete = new HashSet<DateOnly>();
        foreach (var plan in plans)
            foreach (var day in await progress.ProgressAsync(plan, until))
                if (day.DutyDone) complete.Add(day.Day);
        return complete;
    }

    /// <summary>Länge der aktuellen Serie vollständiger Tage bis (einschließlich) heute oder gestern.</summary>
    private static int CurrentStreak(IReadOnlyCollection<DateOnly> completeDays, DateOnly today)
    {
        if (completeDays.Count == 0) return 0;
        var set = completeDays as HashSet<DateOnly> ?? completeDays.ToHashSet();

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
