using System.Text;

namespace Pugling.Agent.Creator.Briefing;

/// <summary>
/// The <b>teacher</b> on whose behalf the design is made: subject, school branch, grade range and - if
/// set - the concrete textbook series along with the unit currently in progress. This is the half of the
/// briefing that carries <i>without a child</i>: a general catalog exercise arises from this alone.
/// <see cref="Persona"/> and <see cref="Didactics"/> deliberately go into the <b>system</b> prompt (they
/// describe who is designing), everything else into the request (it describes what is being designed).
/// </summary>
public sealed record ProfileFacts(
    int ProfileId,
    string Name,
    string? SubjectName,
    SchoolTypes SchoolTypes,
    int? GradeMin,
    int? GradeMax,
    string SourceLang,
    string TargetLang,
    string? Persona,
    string? Didactics,
    int? SeriesId,
    string? SeriesName,
    string? SeriesPublisher,
    string? SeriesNotes,
    SeriesUnitResponse? Unit)
{
    /// <summary>The textbook series in one line ("Access (Cornelsen)"), if a series is set.</summary>
    public string? SeriesLabel => SeriesName is { Length: > 0 } name
        ? SeriesPublisher is { Length: > 0 } publisher ? $"{name} ({publisher})" : name
        : null;

    /// <summary>
    /// Source attribution for the exercise metadata ("Access, grade 8, Unit 3 - Growing up"). It is the
    /// thread by which the supervisor later rediscovers the generated exercise in the catalog.
    /// </summary>
    public string? Source
    {
        get
        {
            var parts = new List<string>();
            if (SeriesName is { Length: > 0 } name) parts.Add(name);
            if (Unit?.Grade is { } grade) parts.Add($"Klasse {grade}");
            if (Unit?.Label is { Length: > 0 } label) parts.Add(label);
            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }
    }

    /// <summary>The textbook-series part of the request. Empty if the profile is series-independent.</summary>
    public string ToPromptText()
    {
        var text = new StringBuilder();
        if (SeriesLabel is { } series) text.AppendLine($"- Lehrwerk: {series}");
        if (!string.IsNullOrWhiteSpace(SeriesNotes)) text.AppendLine($"- Zum Werk: {SeriesNotes}");

        if (Unit is null) return text.ToString();

        text.AppendLine($"- Unit: {Unit.Label}{(Unit.Grade is { } g ? $" (Band für Klasse {g})" : "")}");
        // Der Stoff der Unit ist der Grund, warum es diese Tabelle gibt: ohne ihn erfindet das Modell
        // Inhalte, die im Unterricht des Kindes nicht vorkommen.
        if (!string.IsNullOrWhiteSpace(Unit.Topics)) text.AppendLine($"- Themen der Unit: {Unit.Topics}");
        if (!string.IsNullOrWhiteSpace(Unit.Grammar)) text.AppendLine($"- Grammatik der Unit: {Unit.Grammar}");
        if (!string.IsNullOrWhiteSpace(Unit.VocabularyNotes))
            text.AppendLine($"- Wortschatz der Unit: {Unit.VocabularyNotes}");

        return text.ToString();
    }
}
