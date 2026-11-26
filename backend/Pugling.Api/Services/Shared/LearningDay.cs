using Microsoft.Extensions.Options;

namespace Pugling.Api.Services.Shared;

/// <summary>Configuration of the learning day (B-730): the zone whose calendar day is "today".</summary>
public sealed class LearningDayOptions
{
    /// <summary>The configuration section.</summary>
    public const string SectionName = "LearningDay";

    /// <summary>
    /// IANA or Windows id of the zone the family lives in. The learning day is the calendar day there and ends at local
    /// midnight - "by tonight" must not run until 1 or 2 a.m. because the server counts days in UTC.
    /// </summary>
    public string TimeZone { get; set; } = "Europe/Berlin";

    /// <summary>
    /// Sunday owes no duty (B-731, decision 1): no penalty, and the streak neither counts nor breaks on it. Off only in a
    /// test suite whose cases form "today" from the wall clock, so they do not depend on the weekday they run on.
    /// </summary>
    public bool SundayFree { get; set; } = true;
}

/// <summary>
/// The one place where "today" and "the day of a stored instant" are formed (B-730). Every day number the product shows or
/// books - streak, duty, penalty, timeline, chest, weekly coins - must come from here, or two of them disagree around
/// midnight. A guard in <c>ConventionGuardTests</c> rejects a day formed anywhere else.
/// </summary>
public sealed class LearningDay
{
    private readonly TimeProvider _time;

    /// <summary>Resolves the configured zone once; an unknown id fails at startup, not at the first request.</summary>
    public LearningDay(TimeProvider time, IOptions<LearningDayOptions> options)
    {
        _time = time;
        Zone = Resolve(options.Value.TimeZone);
    }

    /// <summary>The zone whose calendar the learning day follows.</summary>
    public TimeZoneInfo Zone { get; }

    /// <summary>Today's learning day.</summary>
    public DateOnly Today => DayOf(_time.GetUtcNow().UtcDateTime);

    /// <summary>The wall-clock time in the family's zone - for rules that read the hour (time-slot multipliers).</summary>
    public DateTime LocalNow => TimeZoneInfo.ConvertTimeFromUtc(_time.GetUtcNow().UtcDateTime, Zone);

    /// <summary>
    /// The learning day a stored UTC instant falls on. Database columns come back without a kind; they are UTC by
    /// convention, so the kind is set rather than trusted.
    /// </summary>
    public DateOnly DayOf(DateTime utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone));

    /// <summary>
    /// The UTC instant at which <paramref name="day"/> begins - the lower bound for querying stored instants of that day.
    /// Local midnight exists on every day of Europe/Berlin (the clock changes at 2 or 3 a.m.); a zone that changes at
    /// midnight is not supported - converting a skipped midnight would throw.
    /// </summary>
    public DateTime StartUtc(DateOnly day) =>
        DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(TimeOnly.MinValue), Zone), DateTimeKind.Utc);

    /// <summary>
    /// The instant that shows the same wall-clock time <paramref name="days"/> calendar days earlier (B-842). A week with a
    /// clock change is 167 or 169 hours long, so subtracting hours would shift the time of day. A wall clock the spring
    /// change skipped moves on by an hour (the size of Europe/Berlin's change); a doubled one in autumn reads as standard
    /// time.
    /// </summary>
    public DateTime SameTimeDaysEarlier(DateTime utc, int days)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone).AddDays(-days);
        if (Zone.IsInvalidTime(local))
            local = local.AddHours(1);
        return DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeToUtc(local, Zone), DateTimeKind.Utc);
    }

    /// <summary>Finds a zone by id, failing with a message that names the setting.</summary>
    public static TimeZoneInfo Resolve(string id)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new InvalidOperationException(
                $"LearningDay:TimeZone '{id}' ist auf diesem System unbekannt (fehlen ICU/tzdata?).", e);
        }
    }
}
