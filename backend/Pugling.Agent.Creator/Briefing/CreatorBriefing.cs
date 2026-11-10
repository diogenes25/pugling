using System.Text;

namespace Pugling.Agent.Creator.Briefing;

/// <summary>
/// The complete briefing of a request - from <b>two</b> sources: the profile (the teacher: subject,
/// school branch, textbook, didactics) and, optionally, the child (interests, learning progress). Exactly
/// this separation makes both modes of operation possible: with <see cref="Child"/> an <i>individual</i>
/// exercise results, without it a <i>general</i> one for the shared catalog - and for that the agent then
/// needs no account with supervision rights.
/// <para>
/// The pass-through properties (<see cref="Grade"/>, <see cref="SchoolType"/>, <see cref="Source"/> …)
/// prefer the child facts and fall back to the profile. This keeps the exercise-type strategies simple
/// as before: they query the briefing, not where the value came from.
/// </para>
/// </summary>
public sealed record CreatorBriefing(
    ProfileFacts? Profile,
    ChildFacts? Child,
    int SubjectId,
    string SubjectName,
    int SeriesUnitId,
    string SeriesUnitLabel,
    string? Topic,
    string SourceLang,
    string TargetLang,
    IReadOnlyList<string> ExistingExerciseTitles,
    IReadOnlyList<string> RequiredWords,
    IReadOnlyList<int> InterestTagIds,
    IReadOnlyList<string> CatalogInterests,
    IReadOnlyList<int> GrammarTopicIds,
    IReadOnlyList<string> GrammarTopicLabels,
    IReadOnlyList<VocabPair> WordPairs)
{
    // WordPairs is this exercise's slice of the unit's word list (UnitVocabulary): what a vocabulary exercise consists
    // of word for word, and what a translation exercise should build on. It stands in the prompt with its
    // translations, which the unit's free-text notes do not carry.

    /// <summary>Is this tailored to a child (instead of for the general catalog)?</summary>
    public bool Individual => Child is not null;

    /// <summary>Who this is designed for - child name or profile name (for output only).</summary>
    public string Audience => Child?.Name ?? Profile?.Name ?? "Allgemeiner Katalog";

    /// <summary>
    /// What the tasks are dressed in: the child's interests, or in general mode the labels of the tags named with
    /// <c>--interests</c> (<see cref="CatalogInterests"/>, B-264) - empty if neither was given.
    /// </summary>
    public IReadOnlyList<string> Interests => Child?.Interests ?? CatalogInterests;

    // <see cref="GrammarTopicIds"/> are the target unit's grammar topics (B-155, decision 7): the agent takes them
    // over as the form would pre-fill them, only without a human who deselects - in a mixed unit it tags broader
    // than necessary, which is the accepted price for a catalog that does not go blind where it grows fastest.
    // The model itself has no say in it; the deterministic pipeline gets no extra model decision.

    // Why <see cref="InterestTagIds"/> exists next to <see cref="Interests"/>, which reads like the same
    // thing: that one is for the PROMPT (a child's free text included, because the nuance the taxonomy lacks
    // is exactly what dresses a task well; without a child the tag labels), this one is for the CATALOG (ids
    // only, because only a stable key lets a second child's profile be compared against it - B-46, decision 1).

    /// <summary>The child's weakly mastered words; empty in general mode.</summary>
    public IReadOnlyList<WordMasteryResponse> WeakWords => Child?.WeakWords ?? [];

    /// <summary>The child's grade, otherwise the profile's lower bound.</summary>
    public int? Grade => Child?.Grade ?? Profile?.GradeMin;

    /// <summary>Lower suitability bound of the exercise metadata: with a child, exactly its grade, otherwise the profile range.</summary>
    public int? GradeMin => Child?.Grade ?? Profile?.GradeMin;

    /// <summary>Upper suitability bound: with a child, exactly its grade, otherwise the profile range.</summary>
    public int? GradeMax => Child?.Grade ?? Profile?.GradeMax;

    /// <summary>School types of the exercise metadata (child overrides profile). Named in singular as in <see cref="ChildFacts"/>.</summary>
    public SchoolTypes SchoolType =>
        Child is { SchoolType: not SchoolTypes.None } c ? c.SchoolType
        : Profile?.SchoolTypes ?? SchoolTypes.None;

    /// <summary>
    /// Source attribution of the exercise: the profile's textbook series, otherwise the child's textbook,
    /// otherwise the topic. The profile comes first because it is the catalogued - i.e. rediscoverable - form.
    /// </summary>
    public string? Source =>
        Profile?.Source
        ?? (Child?.PrimaryTextbook(SubjectId, SubjectName) is { } book
            ? string.Join(", ", new[] { book.Title, book.CurrentChapter }.Where(s => !string.IsNullOrWhiteSpace(s)))
            : Topic);

    /// <summary>
    /// The briefing as compact German running text for the prompt. Deliberately brief: local models
    /// lose track of the instructions with long contexts.
    /// </summary>
    public string ToPromptText()
    {
        var text = new StringBuilder();

        if (Profile is { } profile)
        {
            text.AppendLine("## Dein Auftrag als Fachlehrer");
            text.AppendLine($"- Profil: {profile.Name}");
            if (!string.IsNullOrWhiteSpace(profile.SubjectName)) text.AppendLine($"- Fach: {profile.SubjectName}");
            if (profile.SchoolTypes != SchoolTypes.None) text.AppendLine($"- Schulart: {profile.SchoolTypes}");
            if (profile.GradeMin is not null || profile.GradeMax is not null)
                text.AppendLine($"- Klassenstufen: {Range(profile.GradeMin, profile.GradeMax)}");
            text.AppendLine();
        }

        text.AppendLine("## Der Lernstoff (fest vorgegeben)");
        text.AppendLine($"- Fach: {SubjectName}");
        text.AppendLine($"- Unit: {SeriesUnitLabel}");
        if (!string.IsNullOrWhiteSpace(Topic)) text.AppendLine($"- Thema: {Topic}");
        if (GrammarTopicLabels.Count > 0) text.AppendLine($"- Grammatik-Themen der Unit: {string.Join(", ", GrammarTopicLabels)}");
        // With the word pairs at hand, the notes' long word list is only a second, translation-less copy of them -
        // and local models lose track of the instructions with every line the context grows.
        if (Profile?.ToPromptText(includeVocabularyNotes: WordPairs.Count == 0) is { Length: > 0 } material) text.Append(material);
        // Without a cataloged textbook, the child's free-text book carries the subject matter.
        else if (Child?.PrimaryTextbook(SubjectId, SubjectName) is { } book)
            text.AppendLine($"- Lehrbuch: {book.Title}"
                            + (book.CurrentChapter is { Length: > 0 } c ? $" (aktuell: {c})" : ""));

        text.AppendLine();
        if (Child is { } child)
        {
            text.Append(child.ToPromptText());
        }
        else
        {
            // The general mode needs this statement: otherwise the model fills the gap with invented
            // preferences and the exercise would be useless for the shared catalog.
            text.AppendLine("## Kein bestimmtes Kind");
            // With --interests the dressing is given, so "neutral" would contradict it (B-264). The labels stand here and
            // not only in the request part: the `briefing` verb prints this text alone (B-322).
            if (CatalogInterests.Count > 0)
            {
                text.AppendLine("- Diese Übung geht in den gemeinsamen Katalog: altersgerechte Situationen aus den vorgegebenen");
                text.AppendLine($"  Interessen ({string.Join(", ", CatalogInterests)}), keine persönliche Ansprache und keine "
                                + "weiteren erfundenen Vorlieben.");
            }
            else
            {
                text.AppendLine("- Diese Übung geht in den gemeinsamen Katalog: wähle neutrale, altersgerechte");
                text.AppendLine("  Alltagssituationen, keine persönliche Ansprache und keine erfundenen Vorlieben.");
            }
        }

        if (RequiredWords.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("## Pflicht-Wortschatz (unveränderlich, vollständig verwenden)");
            foreach (var word in RequiredWords) text.AppendLine($"- {word}");
        }

        if (WordPairs.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("## Wortschatz dieser Übung (aus der Wortliste der Unit, Übersetzungen stehen fest)");
            foreach (var pair in WordPairs) text.AppendLine($"- {pair.Word} = {pair.Translation}");
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
            text.AppendLine("## Bereits vorhandene Übungen in dieser Unit (Titel nicht wiederholen)");
            foreach (var title in ExistingExerciseTitles) text.AppendLine($"- {title}");
        }

        return text.ToString();
    }

    private static string Range(int? min, int? max) => (min, max) switch
    {
        ({ } a, { } b) when a == b => a.ToString(),
        ({ } a, { } b) => $"{a}–{b}",
        ({ } a, null) => $"ab {a}",
        (null, { } b) => $"bis {b}",
        _ => "alle",
    };
}
