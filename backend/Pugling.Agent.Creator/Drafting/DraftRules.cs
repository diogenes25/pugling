using Pugling.Agent.Creator.Briefing;

namespace Pugling.Agent.Creator.Drafting;

/// <summary>
/// Collects a draft's rule violations. Deliberately a list instead of "first error wins": the model
/// should see <b>all</b> shortcomings at once in the repair round.
/// </summary>
public sealed class Violations
{
    private readonly List<string> _messages = [];

    /// <summary>If the condition does not hold, the message is recorded.</summary>
    public void Require(bool condition, string message)
    {
        if (!condition) _messages.Add(message);
    }

    /// <summary>Records a message unconditionally.</summary>
    public void Add(string message) => _messages.Add(message);

    /// <summary>The collected violations (empty = draft is usable).</summary>
    public IReadOnlyList<string> Messages => _messages;
}

/// <summary>
/// The deterministic checks that apply to several exercise types. They run <b>before</b> any write
/// access - the API would reject much of this too, but only after creation and with technical messages;
/// here a repair hint in domain language arises instead.
/// <para>
/// All checks are <b>null-tolerant</b>, and that is not a convenience: the draft records declare their
/// fields non-nullable, but a model may omit any field - then the JSON deserializer simply inserts
/// <c>null</c>. If the validator broke on that with a <see cref="NullReferenceException"/>, the agent
/// would die with a stack trace exactly where it is supposed to trigger a repair round. A missing field
/// is a <i>rule violation</i>, not a crash.
/// </para>
/// </summary>
public static class DraftRules
{
    /// <summary>Lower/upper bound for the task count - guards against one-item and endless exercises.</summary>
    public const int MinItems = 3;
    /// <inheritdoc cref="MinItems"/>
    public const int MaxItems = 30;

    /// <summary>Title set, plausibly long, and not already used in the chapter.</summary>
    public static void Title(Violations violations, string? title, CreatorBriefing briefing)
    {
        violations.Require(!string.IsNullOrWhiteSpace(title), "Der Titel fehlt.");
        violations.Require(title is null || title.Length <= 120, "Der Titel ist länger als 120 Zeichen.");
        violations.Require(title is null || !briefing.ExistingExerciseTitles.Contains(title.Trim(), StringComparer.OrdinalIgnoreCase),
            $"Der Titel '{title}' existiert im Kapitel bereits – wähle einen anderen.");
    }

    /// <summary>The task count is within bounds and matches the requested value.</summary>
    public static void Count(Violations violations, int actual, GenerationRequest request)
    {
        violations.Require(actual >= MinItems, $"Zu wenige Aufgaben: {actual} (mindestens {MinItems}).");
        violations.Require(actual <= MaxItems, $"Zu viele Aufgaben: {actual} (höchstens {MaxItems}).");
        violations.Require(actual == request.ItemCount,
            $"Es wurden {actual} Aufgaben geliefert, gefordert waren {request.ItemCount}.");
    }

    /// <summary>No empty required fields.</summary>
    public static void NotBlank(Violations violations, string? value, string what)
    {
        if (string.IsNullOrWhiteSpace(value)) violations.Add($"{what} ist leer.");
    }

    /// <summary>No keys occurring twice (words, sentences, task prompts).</summary>
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
    /// The hard rule of the core principle: if a required vocabulary is prescribed, it must appear
    /// <b>in full</b>. <paramref name="exact"/> compares whole entries (vocabulary/cloze answers),
    /// otherwise occurrence within the text suffices (sentences, tasks).
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
    /// Task and answer must not be identical (otherwise there is nothing to learn). If either one is
    /// missing, <see cref="NotBlank"/> already reports it - this stays silent then, instead of reporting
    /// "empty equals empty" as a second violation.
    /// </summary>
    public static void PromptDiffersFromAnswer(Violations violations, string? prompt, string? answer, int index)
    {
        if (string.IsNullOrWhiteSpace(prompt) || string.IsNullOrWhiteSpace(answer)) return;

        if (string.Equals(prompt.Trim(), answer.Trim(), StringComparison.OrdinalIgnoreCase))
            violations.Add($"Aufgabe {index + 1}: Aufgabenstellung und Lösung sind identisch ('{answer}').");
    }
}
