using System.Text;

namespace Pugling.Agent.Creator.Briefing;

/// <summary>
/// The <b>material</b>: the textbook series and unit the exercise is created in, which is also where its subject
/// matter comes from (B-484). It used to hang off the profile, so an exercise without a profile lost the unit's notes
/// and fell back to whatever book of the child came first; it is now part of every briefing.
/// </summary>
public sealed record MaterialFacts(
    string SeriesName,
    string? SeriesPublisher,
    string? SeriesNotes,
    SeriesUnitResponse Unit)
{
    /// <summary>The textbook series in one line ("Access (Cornelsen)").</summary>
    public string SeriesLabel => SeriesPublisher is { Length: > 0 } publisher ? $"{SeriesName} ({publisher})" : SeriesName;

    /// <summary>
    /// Source attribution for the exercise metadata ("Access, Klasse 8, Unit 3 - Growing up"). It is the thread by which
    /// the supervisor later rediscovers the generated exercise in the catalog.
    /// </summary>
    public string Source
    {
        get
        {
            var parts = new List<string> { SeriesName };
            if (Unit.Grade is { } grade) parts.Add($"Klasse {grade}");
            if (Unit.Label is { Length: > 0 } label) parts.Add(label);
            return string.Join(", ", parts);
        }
    }

    /// <summary>
    /// The material part of the request. <paramref name="includeVocabularyNotes"/> leaves out the unit's vocabulary
    /// notes when the briefing carries the unit's word pairs itself.
    /// </summary>
    public string ToPromptText(bool includeVocabularyNotes = true)
    {
        var text = new StringBuilder();
        text.AppendLine($"- Lehrwerk: {SeriesLabel}");
        if (!string.IsNullOrWhiteSpace(SeriesNotes)) text.AppendLine($"- Zum Werk: {SeriesNotes}");

        text.AppendLine($"- Unit: {Unit.Label}{(Unit.Grade is { } g ? $" (Band für Klasse {g})" : "")}");
        // The unit's subject matter is the reason the catalog has units: without it the model invents content that
        // never appears in the child's lessons.
        if (Unit.Topics.Count > 0) text.AppendLine($"- Themen der Unit: {string.Join(", ", Unit.Topics)}");
        if (!string.IsNullOrWhiteSpace(Unit.Grammar)) text.AppendLine($"- Grammatik der Unit: {Unit.Grammar}");
        if (includeVocabularyNotes && !string.IsNullOrWhiteSpace(Unit.VocabularyNotes))
            text.AppendLine($"- Wortschatz der Unit: {Unit.VocabularyNotes}");

        return text.ToString();
    }
}
