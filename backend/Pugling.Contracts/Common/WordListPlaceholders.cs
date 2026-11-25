namespace Pugling.Contracts;

/// <summary>
/// The word list's slots for a person or thing - "sth.", "sb.", "qn", "qc", "jdn.", "jdm.", "etw." (B-626). An entry
/// such as "(to) point sth. out (to sb.)" asks only its core, so the server lets a slot be left out of an answer and keeps
/// it fixed on the letter boxes, and the creator agent never makes one part of an answer. In the contract because both
/// sides need the same list - a second copy would drift (as with <see cref="VocabKey"/>).
/// </summary>
public static class WordListPlaceholders
{
    // Not the abbreviation list of the grader (B-462): "so" is an abbreviation there but a real word here.
    private static readonly HashSet<string> Slots = new(StringComparer.OrdinalIgnoreCase)
    {
        "sth", "sb", "qn", "qc", "jdn", "jdm", "etw",
    };

    /// <summary>
    /// Whether a whitespace-delimited token is only slots: "sth.", "qc", "qn/qc", "jdn./jdm." - with or without the dot,
    /// a trailing comma or semicolon ignored. A word containing a slot ("something") is no slot.
    /// </summary>
    public static bool IsPlaceholder(string token)
    {
        var parts = token.TrimEnd(',', ';').Split('/');
        return parts.All(p => Slots.Contains(p.TrimEnd('.')));
    }

    /// <summary>
    /// Whether any whitespace-delimited word of <paramref name="text"/> is a slot, also one glued to a bracket or quote
    /// ("(to sb.)"). Unlike <see cref="IsPlaceholder"/>, which reads a word-list entry whose brackets are handled apart.
    /// </summary>
    public static bool ContainsPlaceholder(string text) =>
        Words(text).Any(t => IsPlaceholder(t.Trim('(', ')', '"', '\'')));

    /// <summary>
    /// <paramref name="text"/> without its slot words, single-spaced: "marcher sur qc" → "marcher sur". Brackets are left
    /// as they are - which bracketed parts may be dropped is the grader's business.
    /// </summary>
    public static string Core(string text) =>
        string.Join(' ', Words(text).Where(t => !IsPlaceholder(t)));

    private static string[] Words(string text) => text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
}
