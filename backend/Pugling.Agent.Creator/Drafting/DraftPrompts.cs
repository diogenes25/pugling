using System.Text;
using Pugling.Agent.Creator.Briefing;

namespace Pugling.Agent.Creator.Drafting;

/// <summary>
/// Die Prompt-Bausteine. Der System-Prompt trägt die Regeln, die für <b>jeden</b> Übungstyp gelten –
/// allen voran die fachliche Kernregel: Interessen kleiden den Stoff ein, sie ersetzen ihn nie.
/// Alles, was sich deterministisch prüfen lässt, steht zusätzlich im <see cref="DraftRules">Validator</see>;
/// der Prompt ist die Bitte, der Validator die Zusicherung.
/// </summary>
public static class DraftPrompts
{
    /// <summary>
    /// Der System-Prompt eines Auftrags: die Persona und die Didaktik des Profils <b>vor</b> den festen
    /// Regeln. Die Reihenfolge ist die Aussage – ein Profil darf die Rolle prägen, aber keine Regel
    /// aufweichen; deshalb steht der unveränderliche Block zuletzt und behält das letzte Wort.
    /// </summary>
    public static string SystemFor(CreatorBriefing briefing)
    {
        if (briefing.Profile is not { } profile) return System;

        var text = new StringBuilder();
        if (profile.Persona is { Length: > 0 } persona) text.AppendLine(persona).AppendLine();
        if (profile.Didactics is { Length: > 0 } didactics)
        {
            text.AppendLine("Didaktische Vorgaben deines Profils:");
            text.AppendLine(didactics);
            text.AppendLine();
        }
        text.Append(System);
        return text.ToString();
    }

    /// <summary>Rollen- und Regelbeschreibung; gilt für alle Typen.</summary>
    public const string System = """
        Du bist der Creator der Lern-App Pugling und entwirfst Schulübungen für ein einzelnes Kind.

        Regeln:
        1. Antworte ausschließlich mit JSON nach dem vorgegebenen Schema. Kein Fließtext, keine
           Erklärungen, keine Markdown-Codezäune.
        2. Der Lernstoff ist vorgegeben und unveränderlich. Die Interessen des Kindes ändern NIE,
           welche Wörter oder Inhalte geübt werden – sie bestimmen nur die Einkleidung: Sätze,
           Situationen, Beispiele, Namen. Ist ein Pflicht-Wortschatz genannt, kommt jedes Wort daraus vor.
        3. Wortschatz und Satzbau passen zu Alter und Klassenstufe des Kindes.
        4. Aufgabenstellungen, Titel und Hinweise auf Deutsch.
        5. Alle Inhalte sind kindgerecht, gewaltfrei, respektvoll und sachlich korrekt.
        6. Keine Dubletten, keine leeren Felder. Jede Aufgabe hat genau eine eindeutige Lösung.
        """;

    /// <summary>Baut den Auftragsteil: Briefing plus typ-spezifische Anweisung.</summary>
    public static string User(CreatorBriefing briefing, GenerationRequest request, string taskInstruction)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine(briefing.ToPromptText());
        prompt.AppendLine("## Auftrag");
        prompt.AppendLine(taskInstruction);
        prompt.AppendLine();
        prompt.AppendLine($"Anzahl Aufgaben: genau {request.ItemCount}.");
        prompt.AppendLine($"Lernsprache: {briefing.SourceLang}, Muttersprache: {briefing.TargetLang}.");
        if (briefing.Interests.Count > 0)
            prompt.AppendLine($"Kleide die Aufgaben in die Interessen ein: {string.Join(", ", briefing.Interests)} – " +
                              "aber ändere den Lernstoff dadurch nicht.");
        return prompt.ToString();
    }

    /// <summary>
    /// Die Reparatur-Runde: der verworfene Entwurf mit den konkreten Verstößen zurück ans Modell.
    /// Konkrete Verstöße wirken deutlich besser als ein pauschales „versuch es nochmal".
    /// </summary>
    public static string Repair(IReadOnlyList<string> violations) =>
        $"""
        Dein letzter Entwurf wurde abgelehnt. Verstöße:
        {string.Join(Environment.NewLine, violations.Select(v => "- " + v))}

        Erzeuge einen vollständig neuen Entwurf, der alle Punkte behebt. Wieder nur JSON nach Schema.
        """;
}
