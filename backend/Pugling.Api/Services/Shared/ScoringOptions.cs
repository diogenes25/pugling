namespace Pugling.Api.Services.Shared;

/// <summary>
/// Scoring settings from the configuration (section <c>Scoring</c>).
/// <para>
/// Until E12 the time slots were a <b>table</b> (<c>TimeSlotRule</c>) - without an API, without a write path
/// besides the seed, without an index and without an overlap check. The test factory even had to <i>delete</i>
/// its rows to get deterministic scores. A table whose rows the suite has to clear away in order to get
/// sensible results is configuration.
/// </para>
/// </summary>
public class ScoringOptions
{
    /// <summary>Configuration section.</summary>
    public const string SectionName = "Scoring";

    /// <summary>
    /// Whether the time slots apply at all. <c>false</c> means: factor 1.0 at every time of day.
    /// <para>
    /// The switch exists for the test suite, and for a hard reason: with slots, the score of the same correct
    /// answer hangs on the <b>time of the run</b> (mornings ×1.5, evenings ×0.8). For the documentation checked
    /// in by <c>DocsCaptureTests</c> that is diff noise. The same construction as
    /// <c>RateLimiting:LoginEnabled</c>, which exists for the same reason.
    /// </para>
    /// </summary>
    public bool TimeSlotsEnabled { get; set; } = true;

    /// <summary>Time slots with a points multiplier; overlap is allowed (see <see cref="ScoringTimeSlot"/>).</summary>
    public List<ScoringTimeSlot> TimeSlots { get; set; } = [];
}

/// <summary>
/// A time slot with a points multiplier: learning in the morning yields more than late in the evening.
/// <para>
/// Overlapping slots are <b>allowed</b> - the choice is fixed nonetheless: the slot starting latest (i.e. the
/// narrowest) wins, on a tie the one ending earlier. Without that ordering the same correct answer would
/// yield a different number of points depending on the order.
/// </para>
/// </summary>
public class ScoringTimeSlot
{
    /// <summary>Descriptive name, purely for the readability of the configuration ("Vormittag").</summary>
    public string Name { get; set; } = "";
    /// <summary>Start (inclusive).</summary>
    public TimeOnly Start { get; set; }
    /// <summary>End (exclusive).</summary>
    public TimeOnly End { get; set; }
    /// <summary>Factor applied to the base points.</summary>
    public double Multiplier { get; set; } = 1.0;
}
