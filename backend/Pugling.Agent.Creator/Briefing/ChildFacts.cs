using System.Text;

namespace Pugling.Agent.Creator.Briefing;

/// <summary>
/// Alles, was den Zuschnitt einer Übung auf <b>ein bestimmtes Kind</b> ausmacht: wer es ist, was es mag
/// (und was nicht) und wo es steht. Fehlt dieser Teil, entsteht eine allgemeine Katalog-Übung – die
/// fachliche Trennung bleibt in beiden Fällen dieselbe: der Lernstoff ist gesetzt, die
/// <see cref="Interests">Interessen</see> bestimmen nur die <i>Einkleidung</i>.
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
    /// <summary>Das Lehrbuch, an dem sich der Stoff ausrichtet (das erste zum Fach passende, sonst das erste überhaupt).</summary>
    public TextbookResponse? PrimaryTextbook(int subjectId, string subjectName) =>
        Textbooks.FirstOrDefault(b => b.SubjectId == subjectId
                                      || string.Equals(b.SubjectName, subjectName, StringComparison.OrdinalIgnoreCase))
        ?? Textbooks.FirstOrDefault();

    /// <summary>Der Kind-Abschnitt des Auftrags.</summary>
    public string ToPromptText()
    {
        var text = new StringBuilder();
        text.AppendLine("## Das Kind");
        text.AppendLine($"- Name: {Name}");
        if (Age is { } age) text.AppendLine($"- Alter: {age} Jahre");
        if (Grade is { } grade) text.AppendLine($"- Klassenstufe: {grade}");
        if (SchoolType != SchoolTypes.None) text.AppendLine($"- Schulart: {SchoolType}");
        if (Gender != Gender.None) text.AppendLine($"- Geschlecht: {Gender}");
        // Gewichtete Tags zuerst (sie tragen die Rangfolge), Freitext als Ergänzung – zusammengeführt,
        // damit im Prompt keine zwei konkurrierenden Interessens-Zeilen stehen.
        var likes = WeightedInterests.Select(i => i.Label)
            .Concat(Interests)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        text.AppendLine(likes.Count > 0
            ? $"- Interessen (wichtigste zuerst): {string.Join(", ", likes)}"
            : "- Interessen: keine hinterlegt (dann neutrale, altersgerechte Alltagssituationen wählen)");
        // Die Abneigungen sind keine Feinheit: eine fachlich korrekte Aufgabe über Spinnen ist unbrauchbar,
        // wenn das Kind Spinnen nicht erträgt. Deshalb als harte Verbotsliste, nicht als Vorliebe.
        if (Dislikes.Count > 0)
            text.AppendLine($"- Vermeide unbedingt (Abneigungen): {string.Join(", ", Dislikes.Select(i => i.Label))}");
        if (!string.IsNullOrWhiteSpace(ProfileNotes)) text.AppendLine($"- Hinweise: {ProfileNotes}");

        return text.ToString();
    }
}
