using System.Text;

namespace Pugling.Agent.Creator.Briefing;

/// <summary>
/// Der <b>Lehrer</b>, in dessen Auftrag entworfen wird: Fach, Schulzweig, Klassenstufen und – wenn
/// hinterlegt – das konkrete Lehrwerk samt der Unit, die gerade dran ist. Das ist die Hälfte des
/// Briefings, die <i>ohne Kind</i> trägt: eine allgemeine Katalog-Übung entsteht allein hieraus.
/// <see cref="Persona"/> und <see cref="Didactics"/> gehen bewusst in den <b>System</b>-Prompt (sie
/// beschreiben, wer entwirft), alles andere in den Auftrag (er beschreibt, was entworfen wird).
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
    /// <summary>Das Lehrwerk in einer Zeile („Access (Cornelsen)"), sofern eine Reihe hinterlegt ist.</summary>
    public string? SeriesLabel => SeriesName is { Length: > 0 } name
        ? SeriesPublisher is { Length: > 0 } publisher ? $"{name} ({publisher})" : name
        : null;

    /// <summary>
    /// Quellenangabe für die Übungs-Metadaten („Access, Klasse 8, Unit 3 – Growing up"). Sie ist der
    /// Faden, an dem der Supervisor die generierte Übung später im Katalog wiederfindet.
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

    /// <summary>Der Lehrwerk-Teil des Auftrags. Leer, wenn das Profil werkunabhängig ist.</summary>
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
