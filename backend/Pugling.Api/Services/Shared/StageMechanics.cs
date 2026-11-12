using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// Procedure-neutral stage/answer mechanics shared by the plan-position learning engine and the answer
/// comparison: which stage is "typed" (objectively checkable) and how an answer is normalized for
/// comparison. Deliberately stateless statics – the single source of truth for these small rules.
/// </summary>
public static class StageMechanics
{
    /// <summary>
    /// Vocabulary stages that are objectively checkable (server-side against the solution) – as
    /// opposed to pure self-assessment. Multiple choice counts as one of these: the selection is
    /// checked against the correct option.
    /// </summary>
    public static bool IsTyped(TestStage stage) =>
        stage is TestStage.LetterBoxes or TestStage.FreeText or TestStage.Audio or TestStage.MultipleChoice
            or TestStage.LetterScramble;

    /// <summary>
    /// Objectively checkable cloze stages – every one of them, including the word bank. Picking from a pool
    /// is an answer like any other (same reasoning as multiple choice above): the server compares it against
    /// the gap's solution. Counting the word-bank stage as self-assessment would hand the child the solution
    /// as <c>Reveal</c> and turn the pool into decoration next to it.
    /// </summary>
    public static bool IsTyped(ClozeStage stage) =>
        stage is ClozeStage.TranslationWordBank or ClozeStage.TranslationFreeText or ClozeStage.FreeText;

    /// <summary>
    /// Normalizes an answer for comparison (trim, lowercase, collapse repeated whitespace into one space, one spelling
    /// of the apostrophe). Any whitespace separates words - a no-break space copied from a PDF is a space, or an option
    /// and the answer would look alike and compare apart (B-497). A tablet keyboard types the typographic apostrophe on
    /// its own ("l’école"), and which key the device picked is not what the child is asked for (B-440). A closing
    /// <c>. ? ! …</c> is dropped as well: a right translation without its full stop is a right translation (B-457).
    /// </summary>
    public static string Normalize(string? s) =>
        string.Join(' ', WithoutClosingMarks(FoldApostrophes(s ?? "").Trim()).ToLowerInvariant()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static readonly char[] ClosingMarks = ['.', '?', '!', '…'];

    // An answer that is nothing but marks keeps them: stripped it would be empty, and empty is never correct.
    private static string WithoutClosingMarks(string s)
    {
        var stripped = s.TrimEnd();
        while (stripped.Length > 0 && Array.IndexOf(ClosingMarks, stripped[^1]) >= 0)
            stripped = stripped[..^1].TrimEnd();
        return stripped.Length > 0 ? stripped : s;
    }

    // Right and left single quotation mark, modifier apostrophe, acute and grave accent as stand-alone characters.
    private static string FoldApostrophes(string s) =>
        s.Replace('’', '\'').Replace('‘', '\'').Replace('ʼ', '\'').Replace('´', '\'').Replace('`', '\'');

    // B-421: the articles a typed answer may leave out, per language of the expected answer. Product knowledge, kept
    // small on purpose: only the three languages the catalog teaches with articles; Latin has none, an unknown
    // language forgives nothing. French elision ("l'école") is a prefix, not a word of its own.
    private static readonly Dictionary<string, (string[] Words, string[] Prefixes)> Articles = new(StringComparer.Ordinal)
    {
        ["de"] = (["der", "die", "das", "ein", "eine"], []),
        ["en"] = (["the", "a", "an"], []),
        ["fr"] = (["le", "la", "les", "un", "une", "des"], ["l'"]),
    };

    /// <summary>
    /// The normalized answer without its leading article in <paramref name="language"/> (an ISO code, a region suffix
    /// like <c>de-DE</c> is ignored), or <c>null</c> when it has none - or nothing would be left after it: an answer that
    /// is only an article has nothing to leave the article off.
    /// </summary>
    public static string? WithoutLeadingArticle(string normalized, string language)
    {
        if (!Articles.TryGetValue(language.Split('-')[0].ToLowerInvariant(), out var table)) return null;
        var space = normalized.IndexOf(' ');
        if (space > 0 && table.Words.Contains(normalized[..space]))
            return normalized[(space + 1)..];
        foreach (var prefix in table.Prefixes)
        {
            // Trimmed: "l' école" with a space after the elision is the same answer as "l'école".
            if (normalized.Length > prefix.Length && normalized.StartsWith(prefix, StringComparison.Ordinal))
                return normalized[prefix.Length..].TrimStart();
        }
        return null;
    }

    /// <summary>
    /// The letter-box mask of an answer (B-66): an underscore for every letter/digit to type, every other
    /// character (space, hyphen, apostrophe, comma, …) kept literally and not entered by the child - it is
    /// already fixed by the solution, and typing it is fumbling, not learning. "not a letter or digit" instead
    /// of a positive list of separators on purpose: closed and language-independent, so a forgotten character
    /// in some future language's solution can never slip through unmasked.
    /// </summary>
    public static string LetterBoxPattern(string answer) =>
        new([.. answer.Select(c => char.IsLetterOrDigit(c) ? '_' : c)]);

    /// <summary>The longest answer still played as letter tiles (B-05); a longer one becomes letter boxes.</summary>
    public const int MaxScrambleLength = 12;

    /// <summary>
    /// Whether an answer can be played as scrambled letter tiles (B-05): one word of 2 to
    /// <see cref="MaxScrambleLength"/> letters/digits. A space, hyphen or apostrophe would be a tile the child
    /// has to place although the solution fixes it - the same reason letter boxes keep those literal (B-66).
    /// </summary>
    public static bool IsScramblable(string answer) =>
        answer.Length is >= 2 and <= MaxScrambleLength && answer.All(char.IsLetterOrDigit);

    /// <summary>
    /// The letters of <paramref name="answer"/> in a shuffled order, deterministic for one <paramref name="seed"/>:
    /// the card shows the same arrangement every time it is fetched within a session or attempt (the seed comes
    /// from its id), and a new session shuffles anew. A shuffle that happens to give the solution back is rotated
    /// by one, so the tiles never already spell the answer (unless every letter is the same).
    /// <para>
    /// The tiles are lower case: grading ignores case (<see cref="Normalize"/>), and the one capital tile of a German
    /// noun would give away which letter comes first. The "already the solution" check compares lower case too.
    /// </para>
    /// </summary>
    public static IReadOnlyList<string> ScrambleLetters(string answer, int seed)
    {
        var word = answer.ToLowerInvariant();
        var letters = word.Select(c => c.ToString()).ToArray();
        // System.Random with a seed is the legacy generator: the same seed gives the same sequence on a given
        // runtime, which is exactly the "frozen per session" the card needs. A reshuffle after a runtime upgrade
        // would cost one open session its arrangement, nothing more.
        var random = new Random(seed);
        for (var i = letters.Length - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (letters[i], letters[j]) = (letters[j], letters[i]);
        }
        if (string.Concat(letters) == word) letters = [.. letters.Skip(1), letters[0]];
        return letters;
    }

    /// <summary>
    /// Multiple-choice pool built from the exercise itself: the atom's own solution plus up to
    /// <paramref name="maxDistractors"/> wrong options taken from the other atoms, rotated deterministically
    /// by index so the solution is not always in front. No randomness on purpose – the same card must offer
    /// the same pool when it is fetched twice.
    /// <para>
    /// Deduplication runs over the whole <see cref="ContentItem.GradedAnswers"/> set on both sides, not
    /// just over the primary answer: an answer declared equally valid must never show up as the <i>wrong</i>
    /// option of the same question. That holds even when only one of the two rows declares the equivalence –
    /// so a candidate is dropped as soon as <b>any</b> of its accepted answers has been seen.
    /// </para>
    /// <para>
    /// Shared by the vocabulary multiple-choice stage and the matching distractor stage: both ask "this
    /// solution among the neighbours' solutions", and the pool must not drift apart between them.
    /// </para>
    /// <para>
    /// <b>No pool without a distractor.</b> If no candidate survives – a single-atom exercise, or every
    /// neighbour holding the same answer – the result would be one option, and that option <i>is</i> the
    /// solution: the child taps the only button and scores. Returning <c>null</c> instead falls back to free
    /// text, which is the harder form and the honest one.
    /// </para>
    /// </summary>
    public static IReadOnlyList<string>? DistractorPool(IReadOnlyList<ContentItem> items, ContentItem item,
        int maxDistractors)
    {
        if (string.IsNullOrWhiteSpace(item.Answer)) return null;

        var seen = new HashSet<string>(StringComparer.Ordinal);
        // B-462: with separable meanings a neighbour's "sobald" is a right answer to "sobald, sowie", not a distractor.
        foreach (var accepted in item.GradedAnswers().Append(item.Answer))
        {
            var normalized = Normalize(accepted);
            seen.Add(normalized);
            // B-421: where the article may be left out, the bare word is a right answer too - a neighbour's "Stadt"
            // next to "die Stadt" would be a second correct button, not a distractor.
            if (item.ArticleOptionalIn is { } language && WithoutLeadingArticle(normalized, language) is { } bare)
                seen.Add(bare);
        }

        var distractors = new List<string>();
        foreach (var other in items)
        {
            if (other.Index == item.Index || string.IsNullOrWhiteSpace(other.Answer)) continue;
            // A card asked the other way round answers in the other language - and with "both" the word's own second
            // card answers with the very word being asked (B-472).
            if (other.Reversed != item.Reversed) continue;
            var candidate = other.GradedAnswers().Append(other.Answer).Select(Normalize).ToList();
            if (candidate.Any(seen.Contains)) continue;
            foreach (var normalized in candidate) seen.Add(normalized);
            distractors.Add(other.Answer);
            if (distractors.Count >= maxDistractors) break;
        }

        if (distractors.Count == 0) return null;

        var choices = new List<string>(distractors.Count + 1) { item.Answer };
        choices.AddRange(distractors);
        // Not Index alone: with "both" forward cards have even indices and backward cards odd ones, so Index % 4 would put
        // the right answer only on buttons 0/2 resp. 1/3 and give the direction away (B-472). Index + Index / 2 spreads
        // either parity over every button, and consecutive indices still cover all of them.
        var shift = (item.Index + item.Index / 2) % choices.Count;
        return [.. choices.Skip(shift), .. choices.Take(shift)];
    }
}
