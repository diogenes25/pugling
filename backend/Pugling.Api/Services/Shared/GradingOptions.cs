namespace Pugling.Api.Services.Shared;

/// <summary>
/// The grading key from the configuration (section <c>Grading</c>, B-740): the lowest share in percent that still
/// earns grades 1 to 5, best grade first; anything below the last bound is a 6. One table for the whole app, so the
/// same performance never earns two different grades (the subject level of B-741 and a mock exam of B-249 share it).
/// <para>
/// Deliberately without a default in code: the table is a product setting and lives in <c>appsettings.json</c>
/// only, like <see cref="ScoringOptions.TimeSlots"/>. A list property with defaults would also be appended to by the
/// configuration binder instead of replaced.
/// </para>
/// </summary>
public class GradingOptions
{
    /// <summary>Configuration section.</summary>
    public const string SectionName = "Grading";

    /// <summary>
    /// Lower bounds in percent for grades 1, 2, 3, 4 and 5 (exactly five, strictly descending, within 0 to 100).
    /// A share equal to a bound earns that bound's grade.
    /// </summary>
    public List<decimal> Thresholds { get; set; } = [];
}
