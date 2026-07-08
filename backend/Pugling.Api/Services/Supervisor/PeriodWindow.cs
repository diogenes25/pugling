using System.Globalization;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Supervisor;

/// <summary>
/// Berechnet das aktuelle Zeitfenster einer <see cref="OfferPeriod"/> – das halboffene Intervall
/// <c>[From, To)</c>, in dem Käufe auf dasselbe Kontingent zählen. <see cref="OfferPeriod.OneOff"/>
/// liefert ein offenes Fenster (<c>From = null</c>), sodass alle Käufe über die gesamte Laufzeit zählen.
/// Verfahrensneutral (auf <see cref="DateTime"/>), damit der Kauf-Zeitstempel direkt geprüft werden kann.
/// </summary>
public static class PeriodWindow
{
    /// <summary>Halboffenes Fenster der aktuellen Periode; <c>From == null</c> = unbegrenzt zurück (Einmal-Angebot).</summary>
    public readonly record struct Window(DateTime? From, DateTime? To)
    {
        /// <summary>Ob ein Zeitpunkt in die aktuelle Periode fällt (untere Grenze inklusive, obere exklusive).</summary>
        public bool Contains(DateTime at) => (From is null || at >= From) && (To is null || at < To);
    }

    /// <summary>Aktuelles Kontingent-Fenster für <paramref name="period"/> bezogen auf <paramref name="nowUtc"/>.</summary>
    public static Window Of(OfferPeriod period, DateTime nowUtc)
    {
        var today = DateOnly.FromDateTime(nowUtc);
        return period switch
        {
            OfferPeriod.Daily => new Window(Start(today), Start(today.AddDays(1))),
            OfferPeriod.Weekly => WeeklyWindow(today),
            OfferPeriod.Monthly => MonthlyWindow(today),
            _ => new Window(null, null), // OneOff: gesamte Laufzeit
        };
    }

    private static Window WeeklyWindow(DateOnly today)
    {
        // Montag der ISO-Woche (DayOfWeek: So=0 → 6 Tage zurück, Mo=1 → 0).
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        return new Window(Start(monday), Start(monday.AddDays(7)));
    }

    private static Window MonthlyWindow(DateOnly today)
    {
        var first = new DateOnly(today.Year, today.Month, 1);
        return new Window(Start(first), Start(first.AddMonths(1)));
    }

    private static DateTime Start(DateOnly day) => day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    /// <summary>ISO-Wochen-Schlüssel wie "2026-W27" – für die idempotente Gamification-Vergabe wiederverwendet.</summary>
    public static string IsoWeekKey(DateOnly today)
    {
        var dt = today.ToDateTime(TimeOnly.MinValue);
        return $"{ISOWeek.GetYear(dt)}-W{ISOWeek.GetWeekOfYear(dt):D2}";
    }
}
