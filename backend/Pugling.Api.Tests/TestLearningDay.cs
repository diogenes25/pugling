using Microsoft.Extensions.Options;

namespace Pugling.Api.Tests;

/// <summary>
/// Learning-day clocks for tests that build services by hand (B-730). The suite counts on the UTC calendar, like the
/// test factory does; <see cref="Berlin"/> is for the tests of the zone rule itself.
/// </summary>
public static class TestLearningDay
{
    /// <summary>The UTC calendar on the system clock.</summary>
    public static readonly LearningDay Utc = For("UTC");

    /// <summary>The production zone on the system clock.</summary>
    public static readonly LearningDay Berlin = For("Europe/Berlin");

    /// <summary>A learning day in <paramref name="zone"/>, read from <paramref name="time"/> (system clock if omitted).</summary>
    public static LearningDay For(string zone, TimeProvider? time = null) =>
        new(time ?? TimeProvider.System, Options.Create(new LearningDayOptions { TimeZone = zone }));
}
