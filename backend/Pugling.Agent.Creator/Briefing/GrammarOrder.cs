using System.Text.RegularExpressions;

namespace Pugling.Agent.Creator.Briefing;

/// <summary>
/// Order of a unit's grammar topics for the agent (B-471). The catalog lists them by label, which put "past progressive"
/// before "question tags" and let a plan with two cloze parts skip the unit's new grammar altogether.
/// </summary>
public static class GrammarOrder
{
    // The unit notes mark new material with "NEU <topic>" (lehrwerk-aufbereiten, FORMAT.md). Only the marker counts:
    // revised topics are mentioned in the same notes, and a mention alone says nothing about what is new. Between the
    // marker and the label a colon, a dash or an opening quote may stand ("NEU – question tags", "NEU „question tags"),
    // as people write it - before B-482 those markers silently did nothing.
    private static readonly Regex NewMarker = new(@"\bNEU\b[:\s\-–—„“""'‚‘«»‹›]*", RegexOptions.CultureInvariant);

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
            // The separators the marker swallowed may belong to the label itself ("NEU -ing form"): the label is tried at
            // every position from right behind "NEU" to the end of the match, the first position that holds one wins (B-482).
            string? label = null;
            for (var at = marker.Index + "NEU".Length; label is null && at <= marker.Index + marker.Length; at++)
                label = LongestLabelAt(labels, grammarNotes, at);
            if (label is not null && !marked.Contains(label)) marked.Add(label);
        }

        // Markers are matched left to right, so `marked` is already in the order the notes name the topics (each label once:
        // twins cannot be told apart by their label, so a second marker moves no second twin). Each marked label
        // takes ONE entry out of the rest: two topics with the same label (only (subject, slug) is unique) both stay -
        // filtering by value dropped the second one from the briefing (B-482).
        var pending = marked.ToList();
        var rest = labels.Where(l =>
        {
            var i = pending.FindIndex(m => m == l);
            if (i < 0) return true;
            pending.RemoveAt(i);
            return false;
        }).ToList();
        return [.. marked, .. rest];
    }

    /// <summary>
    /// The longest label that starts at <paramref name="at"/> and ends on a word boundary: "Present perfect" must not claim
    /// "NEU present perfect progressive", which is a topic of its own.
    /// </summary>
    private static string? LongestLabelAt(IReadOnlyList<string> labels, string notes, int at) =>
        labels.Where(l => at + l.Length <= notes.Length
                          && string.Compare(notes, at, l, 0, l.Length, StringComparison.OrdinalIgnoreCase) == 0
                          && (at + l.Length == notes.Length || !char.IsLetterOrDigit(notes[at + l.Length])))
            .MaxBy(l => l.Length);
}
