using Pugling.Api.Models;

namespace Pugling.Api.Services;

/// <summary>
/// Bewertet die vom Kind abgegebenen Antworten gegen die hinterlegte Lösung – die eine, gemeinsame
/// Vergleichsregel für die Abschlusstests (Vokabel/Zuordnung/Lückentext) und die Leitner-Übungsschleife
/// (<c>/review</c>). Zustandslos und ohne DB-Zugriff; Textvergleiche laufen über
/// <see cref="StudyProgressService.Normalize"/> (Groß-/Kleinschreibung und Mehrfach-Leerzeichen sind egal).
/// </summary>
public class AnswerGrader
{
    /// <summary>Textantwort (Vokabel/Zuordnung) gegen die erwartete Lösung. Leere Eingabe gilt nie als korrekt.</summary>
    public bool Matches(string? given, string expected)
    {
        var g = StudyProgressService.Normalize(given);
        return g.Length > 0 && g == StudyProgressService.Normalize(expected);
    }

    /// <summary>Eine Lücke: nicht leer und trifft die Lösung oder eine hinterlegte Alternative.</summary>
    public bool MatchesGap(Gap gap, string? given)
    {
        var g = StudyProgressService.Normalize(given);
        return g.Length > 0 && (g == StudyProgressService.Normalize(gap.Answer)
            || (gap.Alternatives?.Any(a => StudyProgressService.Normalize(a) == g) ?? false));
    }
}
