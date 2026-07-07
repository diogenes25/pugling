using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services;

/// <summary>
/// Wertet Missionen (zeitgebundene Ziele) und Auszeichnungen (permanente Meilensteine) eines Kindes
/// aus und schreibt fällige Belohnungen idempotent gut – je Mission/Zeitraum bzw. je Auszeichnung genau
/// einmal (analog zum früheren plan-weiten Fortschritts-Service). Liefert außerdem den aktuellen Status fürs Frontend.
/// </summary>
public class GamificationService(PuglingDbContext db, MetricsService metrics, ILogger<GamificationService> logger)
{
    public record MissionStatus(int Id, string Title, ProgressMetric Metric, MissionPeriod Period,
        int Target, int Current, bool Completed, int RewardPoints);

    public record AchievementStatus(int Id, string Title, string? Icon, ProgressMetric Metric,
        int Threshold, int Current, bool Earned, DateTime? EarnedAt, int RewardPoints);

    /// <summary>Wertet alle aktiven Missionen und Auszeichnungen aus und vergibt fällige Belohnungen.</summary>
    public async Task EvaluateAndAwardAsync(int childId, DateOnly today)
    {
        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == childId);
        if (child is null) return;

        foreach (var m in await db.Missions.Where(m => m.ChildId == childId && m.Active).ToListAsync())
        {
            var (from, to, key) = PeriodWindow(m.Period, today);
            var current = await metrics.ValueAsync(childId, m.Metric, from, to, today);
            if (current < m.Target || m.RewardPoints <= 0) continue;
            if (await db.MissionAwards.AnyAsync(a => a.MissionId == m.Id && a.PeriodKey == key)) continue;

            db.MissionAwards.Add(new MissionAward { MissionId = m.Id, PeriodKey = key, Points = m.RewardPoints });
            db.ChildPoints.Add(new ChildPointsEntry
            {
                ChildId = childId,
                Kind = PointKind.Mission,
                Amount = m.RewardPoints,
                Reason = $"Mission erfüllt: {m.Title}",
            });
            if (await SaveIgnoringDuplicateAsync(() => db.MissionAwards.AnyAsync(a => a.MissionId == m.Id && a.PeriodKey == key)))
                logger.LogInformation("Belohnung gebucht: Kind {ChildId} +{Points} (Mission) – \"{Title}\" ({PeriodKey})",
                    childId, m.RewardPoints, m.Title, key);
        }

        foreach (var a in await db.Achievements.Where(a => a.ChildId == childId && a.Active).ToListAsync())
        {
            var current = await metrics.ValueAsync(childId, a.Metric, null, null, today);
            if (current < a.Threshold) continue;
            if (await db.AchievementAwards.AnyAsync(x => x.AchievementId == a.Id)) continue;

            db.AchievementAwards.Add(new AchievementAward { AchievementId = a.Id, Points = a.RewardPoints });
            if (a.RewardPoints > 0)
                db.ChildPoints.Add(new ChildPointsEntry
                {
                    ChildId = childId,
                    Kind = PointKind.Achievement,
                    Amount = a.RewardPoints,
                    Reason = $"Auszeichnung erreicht: {a.Title}",
                });
            if (await SaveIgnoringDuplicateAsync(() => db.AchievementAwards.AnyAsync(x => x.AchievementId == a.Id)))
                logger.LogInformation("Belohnung gebucht: Kind {ChildId} +{Points} (Auszeichnung) – \"{Title}\"",
                    childId, a.RewardPoints, a.Title);
        }
    }

    /// <summary>
    /// Aktueller Missions-Status für die Anzeige beim Kind/Vater – reine Lesesicht, ohne Punktevergabe.
    /// Belohnungen fließen an den Schreib-Nahtstellen (Wiederholung, Test-Abschluss, Sitzungsende), nicht
    /// beim Ansehen: ein GET darf keine Punkte buchen (sichere HTTP-Methode, kein Prefetch-/Retry-Effekt).
    /// </summary>
    public async Task<(IReadOnlyList<MissionStatus> Items, int Total)> MissionStatusesAsync(
        int childId, DateOnly today, int skip, int take)
    {
        var missions = await db.Missions.AsNoTracking()
            .Where(m => m.ChildId == childId && m.Active)
            .OrderBy(m => m.Id)
            .ToListAsync();

        // Nur für die zurückgegebene Seite die (teure) Metrik berechnen – nicht für alle Missionen.
        var items = new List<MissionStatus>();
        foreach (var m in missions.Skip(skip).Take(take))
            items.Add(await MapMissionAsync(childId, m, today));
        return (items, missions.Count);
    }

    private async Task<MissionStatus> MapMissionAsync(int childId, Mission m, DateOnly today)
    {
        var (from, to, key) = PeriodWindow(m.Period, today);
        var current = await metrics.ValueAsync(childId, m.Metric, from, to, today);
        var completed = await db.MissionAwards.AnyAsync(a => a.MissionId == m.Id && a.PeriodKey == key)
            || current >= m.Target;
        return new MissionStatus(m.Id, m.Title, m.Metric, m.Period, m.Target,
            Math.Min(current, m.Target), completed, m.RewardPoints);
    }

    /// <summary>Aktueller Auszeichnungs-Status (reine Lesesicht, ohne Punktevergabe), erreichte zuerst.</summary>
    public async Task<(IReadOnlyList<AchievementStatus> Items, int Total)> AchievementStatusesAsync(
        int childId, DateOnly today, int skip, int take)
    {
        var achievements = await db.Achievements.AsNoTracking().Where(a => a.ChildId == childId && a.Active).ToListAsync();
        // Award-Lookup ist billig und wird sowohl für die Sortierung (erreichte zuerst) als auch den
        // Earned-Status gebraucht – die teure Metrik berechnen wir erst für die Seite.
        var awards = await db.AchievementAwards
            .Where(x => achievements.Select(a => a.Id).Contains(x.AchievementId))
            .ToDictionaryAsync(x => x.AchievementId, x => x.EarnedAt);

        var page = achievements
            .OrderByDescending(a => awards.ContainsKey(a.Id)).ThenBy(a => a.Threshold)
            .Skip(skip).Take(take);
        var items = new List<AchievementStatus>();
        foreach (var a in page)
            items.Add(await MapAchievementAsync(childId, a, awards.TryGetValue(a.Id, out var at) ? at : null, today));
        return (items, achievements.Count);
    }

    private async Task<AchievementStatus> MapAchievementAsync(int childId, Achievement a, DateTime? earnedAt, DateOnly today)
    {
        var current = await metrics.ValueAsync(childId, a.Metric, null, null, today);
        return new AchievementStatus(a.Id, a.Title, a.Icon, a.Metric, a.Threshold,
            current, earnedAt is not null, earnedAt, a.RewardPoints);
    }

    /// <summary>Status einer einzelnen Mission des Kindes (Einzelansicht); <c>null</c>, wenn nicht vorhanden/aktiv/eigen.</summary>
    public async Task<MissionStatus?> MissionStatusAsync(int childId, int missionId, DateOnly today)
    {
        var m = await db.Missions.AsNoTracking().FirstOrDefaultAsync(m => m.Id == missionId && m.ChildId == childId && m.Active);
        return m is null ? null : await MapMissionAsync(childId, m, today);
    }

    /// <summary>Status einer einzelnen Auszeichnung des Kindes (Einzelansicht); <c>null</c>, wenn nicht vorhanden/aktiv/eigen.</summary>
    public async Task<AchievementStatus?> AchievementStatusAsync(int childId, int achievementId, DateOnly today)
    {
        var a = await db.Achievements.AsNoTracking().FirstOrDefaultAsync(a => a.Id == achievementId && a.ChildId == childId && a.Active);
        if (a is null) return null;

        var at = await db.AchievementAwards.Where(x => x.AchievementId == a.Id).Select(x => (DateTime?)x.EarnedAt).FirstOrDefaultAsync();
        return await MapAchievementAsync(childId, a, at, today);
    }

    /// <summary>Tages-/Wochen-/Einmal-Fenster + Schlüssel für die idempotente Vergabe.</summary>
    private static (DateOnly? from, DateOnly? to, string key) PeriodWindow(MissionPeriod period, DateOnly today) =>
        period switch
        {
            MissionPeriod.Daily => (today, today, today.ToString("yyyy-MM-dd")),
            MissionPeriod.Weekly => WeeklyWindow(today),
            _ => (null, null, "once"),
        };

    private static (DateOnly? from, DateOnly? to, string key) WeeklyWindow(DateOnly today)
    {
        // Montag der ISO-Woche (DayOfWeek: So=0 → 6 Tage zurück, Mo=1 → 0).
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var dt = today.ToDateTime(TimeOnly.MinValue);
        var key = $"{ISOWeek.GetYear(dt)}-W{ISOWeek.GetWeekOfYear(dt):D2}";
        return (monday, monday.AddDays(6), key);
    }

    /// <summary>
    /// Speichert; ein paralleler Doppel-Request greift den Unique-Index ab, ohne doppelte Punkte/500.
    /// Liefert <c>true</c>, wenn tatsächlich gebucht wurde, <c>false</c> beim abgefangenen Duplikat –
    /// damit der Aufrufer nur echte Buchungen ins Audit-Log schreibt. <paramref name="alreadyAwardedAsync"/>
    /// prüft, ob die Belohnung inzwischen (durch den Konkurrenz-Request) existiert.
    /// </summary>
    private async Task<bool> SaveIgnoringDuplicateAsync(Func<Task<bool>> alreadyAwardedAsync)
    {
        try
        {
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex)
        {
            // Nur den erwarteten Doppel-Request abfangen: taucht die Belohnung jetzt bereits auf, war es
            // der Unique-Index-Race → gutartig. Sonst ein echter DB-Fehler (FK, NOT NULL, …) → durchreichen,
            // damit legitime Punkte nicht stillschweigend verloren gehen.
            if (!await alreadyAwardedAsync()) throw;
            logger.LogWarning(ex, "Doppelte Gamification-Belohnung abgefangen (Unique-Index)");
            foreach (var entry in db.ChangeTracker.Entries().Where(e => e.State == EntityState.Added))
                entry.State = EntityState.Detached;
            return false;
        }
    }
}
