using Pugling.Agent.Creator.Briefing;

namespace Pugling.Agent.Creator.Drafting;

/// <summary>
/// Sammelt Regelverstöße eines Entwurfs. Bewusst als Liste statt „erster Fehler gewinnt": das Modell
/// soll in der Reparatur-Runde <b>alle</b> Mängel auf einmal sehen.
/// </summary>
public sealed class Violations
{
    private readonly List<string> _messages = [];

    /// <summary>Hält die Bedingung nicht, wird die Meldung vermerkt.</summary>
    public void Require(bool condition, string message)
    {
        if (!condition) _messages.Add(message);
    }

    /// <summary>Vermerkt eine Meldung unbedingt.</summary>
    public void Add(string message) => _messages.Add(message);

    /// <summary>Die gesammelten Verstöße (leer = Entwurf ist verwendbar).</summary>
    public IReadOnlyList<string> Messages => _messages;
}

/// <summary>
/// Die deterministischen Prüfungen, die für mehrere Übungstypen gelten. Sie laufen <b>vor</b> jedem
/// Schreibzugriff – die API würde vieles zwar auch ablehnen, aber erst nach dem Anlegen und mit
/// technischen Meldungen; hier entsteht stattdessen ein Reparatur-Hinweis in Fachsprache.
/// <para>
/// Alle Prüfungen sind <b>null-tolerant</b>, und das ist keine Bequemlichkeit: die Entwurfs-Records
/// deklarieren ihre Felder nicht-nullbar, aber ein Modell darf jedes Feld weglassen – dann setzt der
/// JSON-Deserialisierer schlicht <c>null</c> ein. Würde der Validator daran mit einer
/// <see cref="NullReferenceException"/> zerbrechen, stürbe der Agent mit Stacktrace genau dort, wo er
/// eine Reparatur-Runde anstoßen soll. Ein fehlendes Feld ist ein <i>Regelverstoß</i>, kein Absturz.
/// </para>
/// </summary>
public static class DraftRules
{
    /// <summary>Untergrenze/Obergrenze für die Aufgabenzahl – schützt vor Ein-Wort- und Endlos-Übungen.</summary>
    public const int MinItems = 3;
    /// <inheritdoc cref="MinItems"/>
    public const int MaxItems = 30;

    /// <summary>Titel gesetzt, plausibel lang und nicht schon im Kapitel vergeben.</summary>
    public static void Title(Violations violations, string? title, CreatorBriefing briefing)
    {
        violations.Require(!string.IsNullOrWhiteSpace(title), "Der Titel fehlt.");
        violations.Require(title is null || title.Length <= 120, "Der Titel ist länger als 120 Zeichen.");
        violations.Require(title is null || !briefing.ExistingExerciseTitles.Contains(title.Trim(), StringComparer.OrdinalIgnoreCase),
            $"Der Titel '{title}' existiert im Kapitel bereits – wähle einen anderen.");
    }

    /// <summary>Die Aufgabenzahl liegt in den Grenzen und trifft die Vorgabe.</summary>
    public static void Count(Violations violations, int actual, GenerationRequest request)
    {
        violations.Require(actual >= MinItems, $"Zu wenige Aufgaben: {actual} (mindestens {MinItems}).");
        violations.Require(actual <= MaxItems, $"Zu viele Aufgaben: {actual} (höchstens {MaxItems}).");
        violations.Require(actual == request.ItemCount,
            $"Es wurden {actual} Aufgaben geliefert, gefordert waren {request.ItemCount}.");
    }

    /// <summary>Keine leeren Pflichtfelder.</summary>
    public static void NotBlank(Violations violations, string? value, string what)
    {
        if (string.IsNullOrWhiteSpace(value)) violations.Add($"{what} ist leer.");
    }

    /// <summary>Keine zweimal vorkommenden Schlüssel (Wörter, Sätze, Aufgabenstellungen).</summary>
    public static void NoDuplicates(Violations violations, IEnumerable<string?>? keys, string what)
    {
        var duplicates = (keys ?? [])
            .Select(k => k?.Trim() ?? "")
            .Where(k => k.Length > 0)
            .GroupBy(k => k, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count > 0)
            violations.Add($"{what} kommen doppelt vor: {string.Join(", ", duplicates)}.");
    }

    /// <summary>
    /// Die harte Regel des Grundprinzips: ist ein Pflicht-Wortschatz vorgegeben, muss er
    /// <b>vollständig</b> auftauchen. <paramref name="exact"/> vergleicht ganze Einträge
    /// (Vokabel-/Lückenlösungen), sonst genügt Vorkommen im Text (Sätze, Aufgaben).
    /// </summary>
    public static void CoversRequiredWords(Violations violations, CreatorBriefing briefing,
        IEnumerable<string?>? produced, bool exact)
    {
        if (briefing.RequiredWords.Count == 0) return;

        var haystack = (produced ?? []).Select(p => p?.Trim() ?? "").Where(p => p.Length > 0).ToList();
        var missing = briefing.RequiredWords
            .Where(word => !haystack.Any(candidate => exact
                ? string.Equals(candidate, word, StringComparison.OrdinalIgnoreCase)
                : candidate.Contains(word, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (missing.Count > 0)
            violations.Add($"Diese Wörter aus dem Pflicht-Wortschatz fehlen: {string.Join(", ", missing)}. " +
                           "Der Wortschatz ist unveränderlich – ersetze ihn nicht durch andere Wörter.");
    }

    /// <summary>
    /// Aufgabe und Lösung dürfen nicht identisch sein (sonst ist nichts zu lernen). Fehlt eines von
    /// beiden, meldet das bereits <see cref="NotBlank"/> – hier bleibt es dann still, statt „leer ist
    /// gleich leer" als zweiten Verstoß zu melden.
    /// </summary>
    public static void PromptDiffersFromAnswer(Violations violations, string? prompt, string? answer, int index)
    {
        if (string.IsNullOrWhiteSpace(prompt) || string.IsNullOrWhiteSpace(answer)) return;

        if (string.Equals(prompt.Trim(), answer.Trim(), StringComparison.OrdinalIgnoreCase))
            violations.Add($"Aufgabe {index + 1}: Aufgabenstellung und Lösung sind identisch ('{answer}').");
    }
}
