using System.Text;

namespace Pugling.Agent.Creator.Briefing;

/// <summary>
/// Das vollständige Briefing eines Auftrags – aus <b>zwei</b> Quellen: dem Profil (der Lehrer: Fach,
/// Schulzweig, Lehrwerk, Didaktik) und optional dem Kind (Interessen, Lernstand). Genau diese Trennung
/// macht beide Betriebsarten möglich: mit <see cref="Child"/> entsteht eine <i>individuelle</i> Übung,
/// ohne es eine <i>allgemeine</i> für den geteilten Katalog – und dafür braucht der Agent dann kein
/// Konto mit Betreuungsrecht.
/// <para>
/// Die Durchgriffs-Eigenschaften (<see cref="Grade"/>, <see cref="SchoolType"/>, <see cref="Source"/> …)
/// bevorzugen die Kind-Fakten und fallen aufs Profil zurück. Dadurch bleiben die Übungstyp-Strategien
/// unverändert einfach: sie fragen das Briefing, nicht die Herkunft der Angabe.
/// </para>
/// </summary>
public sealed record CreatorBriefing(
    ProfileFacts? Profile,
    ChildFacts? Child,
    int SubjectId,
    string SubjectName,
    int ChapterId,
    string ChapterName,
    string? Topic,
    string SourceLang,
    string TargetLang,
    IReadOnlyList<string> ExistingExerciseTitles,
    IReadOnlyList<string> RequiredWords)
{
    /// <summary>Wird auf ein Kind zugeschnitten (statt für den allgemeinen Katalog)?</summary>
    public bool Individual => Child is not null;

    /// <summary>Für wen entworfen wird – Kindname bzw. Profilname (nur für Ausgaben).</summary>
    public string Audience => Child?.Name ?? Profile?.Name ?? "Allgemeiner Katalog";

    /// <summary>Interessen des Kindes; im allgemeinen Modus bewusst leer (nichts zum Einkleiden).</summary>
    public IReadOnlyList<string> Interests => Child?.Interests ?? [];

    /// <summary>Schwach beherrschte Wörter des Kindes; im allgemeinen Modus leer.</summary>
    public IReadOnlyList<WordMasteryResponse> WeakWords => Child?.WeakWords ?? [];

    /// <summary>Klassenstufe des Kindes, sonst die Untergrenze des Profils.</summary>
    public int? Grade => Child?.Grade ?? Profile?.GradeMin;

    /// <summary>Untere Eignungsgrenze der Übungs-Metadaten: beim Kind exakt seine Stufe, sonst der Profil-Bereich.</summary>
    public int? GradeMin => Child?.Grade ?? Profile?.GradeMin;

    /// <summary>Obere Eignungsgrenze: beim Kind exakt seine Stufe, sonst der Profil-Bereich.</summary>
    public int? GradeMax => Child?.Grade ?? Profile?.GradeMax;

    /// <summary>Schularten der Übungs-Metadaten (Kind schlägt Profil). Singular benannt wie bei <see cref="ChildFacts"/>.</summary>
    public SchoolTypes SchoolType =>
        Child is { SchoolType: not SchoolTypes.None } c ? c.SchoolType
        : Profile?.SchoolTypes ?? SchoolTypes.None;

    /// <summary>
    /// Quellenangabe der Übung: das Lehrwerk des Profils, sonst das Lehrbuch des Kindes, sonst das Thema.
    /// Das Profil steht vorn, weil es die katalogisierte – also wiederfindbare – Form ist.
    /// </summary>
    public string? Source =>
        Profile?.Source
        ?? (Child?.PrimaryTextbook(SubjectId, SubjectName) is { } book
            ? string.Join(", ", new[] { book.Title, book.CurrentChapter }.Where(s => !string.IsNullOrWhiteSpace(s)))
            : Topic);

    /// <summary>
    /// Das Briefing als kompakter deutscher Fließtext für den Prompt. Bewusst knapp: lokale Modelle
    /// verlieren bei langen Kontexten die Anweisungen aus dem Blick.
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
        text.AppendLine($"- Kapitel: {ChapterName}");
        if (!string.IsNullOrWhiteSpace(Topic)) text.AppendLine($"- Thema: {Topic}");
        if (Profile?.ToPromptText() is { Length: > 0 } material) text.Append(material);
        // Ohne katalogisiertes Lehrwerk trägt das Freitext-Buch des Kindes den Stoff.
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
            // Der allgemeine Modus braucht diese Ansage: sonst füllt das Modell die Leerstelle mit
            // erfundenen Vorlieben und die Übung wäre für den geteilten Katalog unbrauchbar.
            text.AppendLine("## Kein bestimmtes Kind");
            text.AppendLine("- Diese Übung geht in den gemeinsamen Katalog: wähle neutrale, altersgerechte");
            text.AppendLine("  Alltagssituationen, keine persönliche Ansprache und keine erfundenen Vorlieben.");
        }

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

    private static string Range(int? min, int? max) => (min, max) switch
    {
        ({ } a, { } b) when a == b => a.ToString(),
        ({ } a, { } b) => $"{a}–{b}",
        ({ } a, null) => $"ab {a}",
        (null, { } b) => $"bis {b}",
        _ => "alle",
    };
}
