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
public static partial class DraftRules
{
    /// <summary>Lower/upper bound for the task count - guards against one-item and endless exercises.</summary>
    public const int MinItems = 3;
    /// <inheritdoc cref="MinItems"/>
    public const int MaxItems = 30;

    /// <summary>Title set, plausibly long, and not already used in the series unit.</summary>
    public static void Title(Violations violations, string? title, CreatorBriefing briefing)
    {
        violations.Require(!string.IsNullOrWhiteSpace(title), "Der Titel fehlt.");
        violations.Require(title is null || title.Length <= 120, "Der Titel ist länger als 120 Zeichen.");
        violations.Require(title is null || !briefing.ExistingExerciseTitles.Contains(title.Trim(), StringComparer.OrdinalIgnoreCase),
            $"Der Titel '{title}' existiert in der Unit bereits – wähle einen anderen.");
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

    /// <summary>
    /// No keys occurring twice (words, sentences, task prompts), ignoring case. <paramref name="fold"/> widens "the
    /// same" to what the server treats as the same (the vocabulary store key, B-336); the message then names every
    /// spelling of the group, because "the café = the cafe" is what the model has to see to repair it.
    /// </summary>
    public static void NoDuplicates(Violations violations, IEnumerable<string?>? keys, string what,
        Func<string, string>? fold = null)
    {
        var duplicates = (keys ?? [])
            .Select(k => k?.Trim() ?? "")
            .Where(k => k.Length > 0)
            .GroupBy(k => fold is null ? k : fold(k), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => string.Join(" = ", g.Distinct(StringComparer.Ordinal)))
            .ToList();

        if (duplicates.Count > 0)
            violations.Add($"{what} kommen doppelt vor: {string.Join(", ", duplicates)}.");
    }

    /// <summary>
    /// The hard rule of the core principle: if a required vocabulary is prescribed, it must appear
    /// <b>in full</b>. <paramref name="exact"/> compares whole entries (vocabulary/cloze answers),
    /// otherwise occurrence within the text suffices (sentences, tasks).
    /// <paramref name="slotFree"/> is set where the produced texts are answers checked by
    /// <see cref="AnswerWithoutPlaceholder"/> (B-626): there the core of a required word covers it too.
    /// </summary>
    public static void CoversRequiredWords(Violations violations, CreatorBriefing briefing,
        IEnumerable<string?>? produced, bool exact, bool slotFree)
    {
        if (briefing.RequiredWords.Count == 0) return;

        var haystack = (produced ?? []).Select(p => p?.Trim() ?? "").Where(p => p.Length > 0).ToList();
        // B-626: a slot-free answer cannot hold "(to) point sth. out (to sb.)", so its core "point out" counts - demanding
        // the whole entry would contradict AnswerWithoutPlaceholder in every repair round. Elsewhere (a vocabulary front
        // must be the stored entry itself, or the weak word's store link is lost) only the entry counts.
        var missing = briefing.RequiredWords
            .Where(word => !haystack.Any(candidate => Covers(candidate, word, exact)
                || slotFree && SlotFreeCore(word) is { Length: > 0 } core && Covers(candidate, core, exact)))
            .ToList();

        if (missing.Count > 0)
            violations.Add($"Diese Wörter aus dem Pflicht-Wortschatz fehlen: {string.Join(", ", missing)}. " +
                           "Der Wortschatz ist unveränderlich – ersetze ihn nicht durch andere Wörter.");
    }

    // What is left of a word-list entry once its bracketed parts and slots go: "(to) point sth. out (to sb.)" -> "point out".
    private static string SlotFreeCore(string word) => WordListPlaceholders.Core(RoundBrackets().Replace(word, " "));

    private static bool Covers(string candidate, string word, bool exact) => exact
        ? string.Equals(candidate, word, StringComparison.OrdinalIgnoreCase)
        : candidate.Contains(word, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The answer must not already stand in the task (B-458): "While they ___ (play) football …" beside "They were
    /// playing football" lets the child copy instead of form. Round brackets are left out of the comparison - there
    /// stands the base form the task asks to change, and in "They ___ (play)" it may well equal the answer.
    /// </summary>
    public static void AnswerNotInPrompt(Violations violations, string? prompt, string? answer, int index)
    {
        if (string.IsNullOrWhiteSpace(prompt) || string.IsNullOrWhiteSpace(answer)) return;
        // Identical is PromptDiffersFromAnswer's message; a second one for the same fault only adds noise.
        if (string.Equals(prompt.Trim(), answer.Trim(), StringComparison.OrdinalIgnoreCase)) return;

        var readable = RoundBrackets().Replace(prompt, " ");
        if (ClozeMarkup.ContainsWord(readable, answer))
            violations.Add($"Aufgabe {index + 1}: die Lösung '{answer.Trim()}' steht schon in der Aufgabenstellung – " +
                           "das Kind könnte sie abschreiben.");
    }

    /// <summary>
    /// An answer asks only the core of a word-list entry, never its slots (B-626): "point sth. out" in a gap makes the
    /// child type "sth.", which the word list prints as a stand-in for the object the sentence names. The list is the
    /// server's (<see cref="WordListPlaceholders"/>), so the agent rejects exactly what the grader treats as accessory.
    /// </summary>
    public static void AnswerWithoutPlaceholder(Violations violations, string? answer, string where)
    {
        if (string.IsNullOrWhiteSpace(answer) || !WordListPlaceholders.ContainsPlaceholder(answer)) return;
        violations.Add($"{where}: die Lösung '{answer.Trim()}' enthält einen Platzhalter der Wortliste (sth., sb., qn, qc, " +
                       "jdn., jdm., etw.) – setze das Wort ein, für das er steht, oder lass ihn weg.");
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

    [System.Text.RegularExpressions.GeneratedRegex(@"\([^()]*\)")]
    private static partial System.Text.RegularExpressions.Regex RoundBrackets();
}
