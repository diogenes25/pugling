using System.Text;

namespace Pugling.Agent.Creator.Briefing;

/// <summary>
/// Everything that makes up tailoring an exercise to <b>one specific child</b>: who they are, what they
/// like (and what not) and where they stand. If this part is missing, a general catalog exercise results
/// - the domain separation stays the same in both cases: the material is fixed, the
/// <see cref="Interests">interests</see> only determine how it is <i>dressed up</i>.
/// </summary>
public sealed record ChildFacts(
    int ChildId,
    string Name,
    int? Age,
    int? Grade,
    SchoolTypes SchoolType,
    Gender Gender,
    IReadOnlyList<string> Interests,
    IReadOnlyList<ChildInterestResponse> WeightedInterests,
    IReadOnlyList<ChildInterestResponse> Dislikes,
    string? ProfileNotes,
    IReadOnlyList<TextbookResponse> Textbooks,
    IReadOnlyList<WordMasteryResponse> WeakWords)
{
    /// <summary>The textbook the material is aligned to (the first one matching the subject, otherwise the first one at all).</summary>
    public TextbookResponse? PrimaryTextbook(int subjectId, string subjectName) =>
        Textbooks.FirstOrDefault(b => b.SubjectId == subjectId
                                      || string.Equals(b.SubjectName, subjectName, StringComparison.OrdinalIgnoreCase))
        ?? Textbooks.FirstOrDefault();

    /// <summary>The child section of the request.</summary>
    public string ToPromptText()
    {
        var text = new StringBuilder();
        text.AppendLine("## Das Kind");
        text.AppendLine($"- Name: {Name}");
        if (Age is { } age) text.AppendLine($"- Alter: {age} Jahre");
        if (Grade is { } grade) text.AppendLine($"- Klassenstufe: {grade}");
        if (SchoolType != SchoolTypes.None) text.AppendLine($"- Schulart: {SchoolType}");
        if (Gender != Gender.None) text.AppendLine($"- Geschlecht: {Gender}");
        // Weighted tags first (they carry the ranking), free text as a complement - merged, so that the
        // prompt does not hold two competing interest lines.
        var likes = WeightedInterests.Select(i => i.Label)
            .Concat(Interests)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        text.AppendLine(likes.Count > 0
            ? $"- Interessen (wichtigste zuerst): {string.Join(", ", likes)}"
            : "- Interessen: keine hinterlegt (dann neutrale, altersgerechte Alltagssituationen wählen)");
        // The dislikes are not a nicety: a technically correct task about spiders is useless if the child
        // cannot stand spiders. Hence a hard ban list, not a preference.
        if (Dislikes.Count > 0)
            text.AppendLine($"- Vermeide unbedingt (Abneigungen): {string.Join(", ", Dislikes.Select(i => i.Label))}");
        if (!string.IsNullOrWhiteSpace(ProfileNotes)) text.AppendLine($"- Hinweise: {ProfileNotes}");

        return text.ToString();
    }
}
