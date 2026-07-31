using System.Text;
using Pugling.Agent.Creator.Briefing;

namespace Pugling.Agent.Creator.Drafting;

/// <summary>
/// The prompt building blocks. The system prompt carries the rules that apply to <b>every</b> exercise
/// type - foremost the core domain rule: interests dress up the material, they never replace it.
/// Everything that can be checked deterministically also lives in the <see cref="DraftRules">validator</see>;
/// the prompt is the request, the validator is the guarantee.
/// </summary>
public static class DraftPrompts
{
    /// <summary>
    /// The system prompt of a request: the profile's persona and didactics <b>before</b> the fixed rules.
    /// The order makes the statement - a profile may shape the role but not soften any rule; that is why
    /// the immutable block comes last and keeps the final word.
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

    /// <summary>Role and rule description; applies to all types.</summary>
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

    /// <summary>Builds the request part: briefing plus type-specific instruction.</summary>
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
    /// The repair round: the rejected draft with the concrete violations goes back to the model.
    /// Concrete violations work considerably better than a blanket "try again".
    /// </summary>
    public static string Repair(IReadOnlyList<string> violations) =>
        $"""
        Dein letzter Entwurf wurde abgelehnt. Verstöße:
        {string.Join(Environment.NewLine, violations.Select(v => "- " + v))}

        Erzeuge einen vollständig neuen Entwurf, der alle Punkte behebt. Wieder nur JSON nach Schema.
        """;
}
