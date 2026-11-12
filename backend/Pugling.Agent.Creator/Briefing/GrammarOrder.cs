using System.Text.RegularExpressions;

namespace Pugling.Agent.Creator.Briefing;

/// <summary>
/// Order of a unit's grammar topics for the agent (B-471). The catalog lists them by label, which put "past progressive"
/// before "question tags" and let a plan with two cloze parts skip the unit's new grammar altogether.
/// </summary>
public static class GrammarOrder
{
    // The unit notes mark new material with "NEU <topic>" (lehrwerk-aufbereiten, FORMAT.md). Only the marker counts:
    // revised topics are mentioned in the same notes, and a mention alone says nothing about what is new.
    private static readonly Regex NewMarker = new(@"\bNEU\b[:\s]*", RegexOptions.CultureInvariant);

    /// <summary>
    /// The topics the notes mark as new first, in the order the notes name them, then the rest in their given order.
    /// Without notes, or without a marked topic, the order stays as it is.
    /// </summary>
    public static IReadOnlyList<string> NewMaterialFirst(IReadOnlyList<string> labels, string? grammarNotes)
    {
        if (string.IsNullOrWhiteSpace(grammarNotes)) return labels;

        var marked = new List<string>();
        foreach (Match marker in NewMarker.Matches(grammarNotes))
        {
            var after = marker.Index + marker.Length;
            // The longest label that ends on a word boundary: "Present perfect" must not claim "NEU present perfect
            // progressive", which is a topic of its own.
            var label = labels
                .Where(l => after + l.Length <= grammarNotes.Length
                            && string.Compare(grammarNotes, after, l, 0, l.Length, StringComparison.OrdinalIgnoreCase) == 0
                            && (after + l.Length == grammarNotes.Length || !char.IsLetterOrDigit(grammarNotes[after + l.Length])))
                .MaxBy(l => l.Length);
            if (label is not null && !marked.Contains(label)) marked.Add(label);
        }

        // Markers are matched left to right, so `marked` is already in the order the notes name the topics.
        return [.. marked, .. labels.Where(l => !marked.Contains(l))];
    }
}
