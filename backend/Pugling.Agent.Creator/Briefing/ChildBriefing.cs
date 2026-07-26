using System.Text;

namespace Pugling.Agent.Creator.Briefing;

/// <summary>
/// Alles, was den Zuschnitt einer Übung auf <b>ein bestimmtes Kind</b> ausmacht, an einer Stelle:
/// wer es ist, was es lernen muss und wo es steht. Die Trennung ist fachlich entscheidend – der
/// <see cref="RequiredWords">Lernstoff</see> ist gesetzt (Lehrbuch/Lehrplan), die
/// <see cref="Interests">Interessen</see> bestimmen nur die <i>Einkleidung</i>.
/// </summary>
public sealed record ChildBriefing(
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
    int SubjectId,
    string SubjectName,
    int ChapterId,
    string ChapterName,
    string? Topic,
    IReadOnlyList<string> ExistingExerciseTitles,
    IReadOnlyList<string> RequiredWords,
    IReadOnlyList<WordMasteryResponse> WeakWords)
{
    /// <summary>Das Lehrbuch, an dem sich der Stoff ausrichtet (das erste zum Fach passende, sonst das erste überhaupt).</summary>
    public TextbookResponse? PrimaryTextbook =>
        Textbooks.FirstOrDefault(b => b.SubjectId == SubjectId || string.Equals(b.SubjectName, SubjectName, StringComparison.OrdinalIgnoreCase))
        ?? Textbooks.FirstOrDefault();

    /// <summary>Quellenangabe für die Übungs-Metadaten (Lehrbuch + aktuelles Kapitel), sonst das Thema.</summary>
    public string? Source => PrimaryTextbook is { } book
        ? string.Join(", ", new[] { book.Title, book.CurrentChapter }.Where(s => !string.IsNullOrWhiteSpace(s)))
        : Topic;

    /// <summary>
    /// Das Briefing als kompakter deutscher Fließtext für den Prompt. Bewusst knapp: lokale Modelle
    /// verlieren bei langen Kontexten die Anweisungen aus dem Blick.
    /// </summary>
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

        text.AppendLine();
        text.AppendLine("## Der Lernstoff (fest vorgegeben)");
        text.AppendLine($"- Fach: {SubjectName}");
        text.AppendLine($"- Kapitel: {ChapterName}");
        if (!string.IsNullOrWhiteSpace(Topic)) text.AppendLine($"- Thema: {Topic}");
        if (PrimaryTextbook is { } book)
            text.AppendLine($"- Lehrbuch: {book.Title}{(book.CurrentChapter is { Length: > 0 } c ? $" (aktuell: {c})" : "")}");

        if (RequiredWords.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("## Pflicht-Wortschatz (unveränderlich, vollständig verwenden)");
            foreach (var word in RequiredWords) text.AppendLine($"- {word}");
        }

        if (WeakWords.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("## Schwach beherrschte Wörter (dürfen häufiger vorkommen)");
            foreach (var weak in WeakWords)
                text.AppendLine($"- {weak.Word} = {weak.Translation} ({weak.CorrectPercent} % richtig)");
        }

        if (ExistingExerciseTitles.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("## Bereits vorhandene Übungen in diesem Kapitel (Titel nicht wiederholen)");
            foreach (var title in ExistingExerciseTitles) text.AppendLine($"- {title}");
        }

        return text.ToString();
    }
}
