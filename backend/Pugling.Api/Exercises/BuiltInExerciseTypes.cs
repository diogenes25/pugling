using System.Globalization;
using Pugling.Api.Models;

namespace Pugling.Api.Exercises;

// The remaining built-in exercise types. Short, because they inherit their facet defaults from
// ExerciseTypeBase and only carry key/manifest/ItemsOf (+ check/stages where needed). Vocabulary sits
// apart because of its size (VocabularyExerciseType). Shared check primitives live in AnswerChecking (below).

/// <summary>
/// Reading comprehension: text + comprehension questions. The questions ARE graded against their solution -
/// <see cref="ExerciseCheckMode.None"/> only says the type has no final-test surface, not that nothing is
/// checked. (That sentence used to read "pure content exercise, no automatic check" and sent a whole
/// investigation down the wrong path.)
/// </summary>
public sealed class ReadingExerciseType : ExerciseTypeBase
{
    /// <inheritdoc/>
    public override string Key => ExerciseTypeKeys.Reading;
    /// <inheritdoc/>
    public override ExerciseTypeManifest Manifest { get; } = new(
        ExerciseTypeKeys.Reading, "Leseverständnis", "reading", 1, "reading",
        ExerciseCheckMode.None, null, null, [], ItemNoun: "Fragen", ItemNounOne: "Frage");
    /// <inheritdoc/>
    public override IReadOnlyList<ContentItem> ItemsOf(string configJson)
    {
        var c = Deserialize<ReadingConfig>(configJson);
        return AnswerChecking.FromQuestions(c.Questions, passage: c.Text);
    }

    /// <inheritdoc cref="AnswerChecking.ChoicesOf"/>
    public override IReadOnlyList<string>? Choices(string configJson, IReadOnlyList<ContentItem> items, ContentItem item, int stage) =>
        AnswerChecking.ChoicesOf(Deserialize<ReadingConfig>(configJson).Questions, item);
}

/// <summary>Listening comprehension: audio source + comprehension questions.</summary>
public sealed class ListeningExerciseType : ExerciseTypeBase
{
    /// <inheritdoc/>
    public override string Key => ExerciseTypeKeys.Listening;
    /// <inheritdoc/>
    public override ExerciseTypeManifest Manifest { get; } = new(
        ExerciseTypeKeys.Listening, "Hörverständnis", "listening", 1, "listening",
        ExerciseCheckMode.None, null, null, ["audio", "transcript"], ItemNoun: "Fragen", ItemNounOne: "Frage");
    /// <inheritdoc/>
    public override IReadOnlyList<ContentItem> ItemsOf(string configJson)
    {
        var c = Deserialize<ListeningConfig>(configJson);
        // No passage: the transcript is the creator's and would answer the question outright.
        return AnswerChecking.FromQuestions(c.Questions, audioUrl: c.AudioUrl);
    }

    /// <summary>
    /// The recording, on every stage. Unlike the vocabulary listening stage this is not a question form the
    /// supervisor picks - it is what the exercise IS, and a listening comprehension without its audio is an
    /// unanswerable question.
    /// </summary>
    public override (int? LetterBoxLength, string? AudioUrl, string? ImageUrl, string? AnswerPattern) StageFacets(ContentItem item, int stage) =>
        (null, item.AudioUrl, null, null);

    /// <inheritdoc cref="AnswerChecking.ChoicesOf"/>
    public override IReadOnlyList<string>? Choices(string configJson, IReadOnlyList<ContentItem> items, ContentItem item, int stage) =>
        AnswerChecking.ChoicesOf(Deserialize<ListeningConfig>(configJson).Questions, item);
}

/// <summary>Essay: free text, no item-by-item comparison – hence no checkable content.</summary>
public sealed class EssayExerciseType : ExerciseTypeBase
{
    /// <inheritdoc/>
    public override string Key => ExerciseTypeKeys.Essay;
    /// <inheritdoc/>
    public override ExerciseTypeManifest Manifest { get; } = new(
        ExerciseTypeKeys.Essay, "Aufsatz", "essay", 1, "essays",
        ExerciseCheckMode.None, null, null, ["rubric", "wordCount"]);
    /// <inheritdoc/>
    public override IReadOnlyList<ContentItem> ItemsOf(string configJson) => [];

    /// <summary>
    /// The writing task plus its limits and rubric, exactly the fields the author entered (B-15) – shown,
    /// never graded. Limits and rubric share one readable hint line; a structured rubric would need its own
    /// fields in the contract.
    /// </summary>
    public override ContentItem? StaticView(string configJson)
    {
        var c = Deserialize<EssayConfig>(configJson);
        var parts = new List<string>();
        var length = (c.MinWords, c.MaxWords) switch
        {
            ({ } min, { } max) => $"{min}–{max} Wörter",
            ({ } min, null) => $"mindestens {min} Wörter",
            (null, { } max) => $"höchstens {max} Wörter",
            _ => null,
        };
        if (length is not null) parts.Add($"Länge: {length}");
        if (c.Rubric is { Count: > 0 } rubric)
            parts.Add("Bewertung: " + string.Join(", ", rubric.Select(r => $"{r.Criterion} ({r.MaxScore} P.)")));
        return new ContentItem(0, c.Prompt, "", [], Hint: parts.Count > 0 ? string.Join(" · ", parts) : null);
    }
}

/// <summary>Grammar: transformation/rule tasks.</summary>
public sealed class GrammarExerciseType : ExerciseTypeBase
{
    /// <inheritdoc/>
    public override string Key => ExerciseTypeKeys.Grammar;
    /// <inheritdoc/>
    public override ExerciseTypeManifest Manifest { get; } = new(
        // "Regelaufgaben", not "Grammatik" (B-163): same reason as the vocabulary type - the label names the
        // form (a prompt with an answer and an optional rule hint), not the subject matter, because the
        // category axis owns subject matter and "Grammatik" is a seeded category.
        ExerciseTypeKeys.Grammar, "Regelaufgaben", "prompts", 1, "grammar",
        ExerciseCheckMode.None, null, null, ["ruleHints"], ItemNoun: "Aufgaben", ItemNounOne: "Aufgabe");
    /// <inheritdoc/>
    public override IReadOnlyList<ContentItem> ItemsOf(string configJson)
    {
        var c = Deserialize<GrammarConfig>(configJson);
        return [.. c.Tasks.Select((t, i) =>
            new ContentItem(i, t.Prompt, t.Answer, [t.Answer], t.RuleHint,
                Passage: AnswerChecking.Blank(c.Instruction),
                // The rule hint is meant "after the answer" (GrammarTask); it stays the tip before it as well (B-529).
                Explanation: AnswerChecking.Blank(t.RuleHint)))];
    }
}

/// <summary>Translation: sentences with an expected translation (+ alternatives).</summary>
public sealed class TranslationExerciseType : ExerciseTypeBase
{
    /// <inheritdoc/>
    public override string Key => ExerciseTypeKeys.Translation;
    /// <inheritdoc/>
    public override ExerciseTypeManifest Manifest { get; } = new(
        ExerciseTypeKeys.Translation, "Übersetzung", "prompts", 1, "translation",
        ExerciseCheckMode.None, null, null, ["alternatives"], ItemNoun: "Sätze", ItemNounOne: "Satz");
    /// <inheritdoc/>
    public override IReadOnlyList<ContentItem> ItemsOf(string configJson)
    {
        var c = Deserialize<TranslationConfig>(configJson);
        return [.. c.Items.Select((t, i) => new ContentItem(i, t.Source, t.Target, Accepted(t.Target, t.Alternatives)))];
    }
}

/// <summary>Birkenbihl: word-for-word decoding; pure content exercise with no active querying.</summary>
public sealed class BirkenbihlExerciseType : ExerciseTypeBase
{
    /// <inheritdoc/>
    public override string Key => ExerciseTypeKeys.Birkenbihl;
    /// <inheritdoc/>
    public override ExerciseTypeManifest Manifest { get; } = new(
        ExerciseTypeKeys.Birkenbihl, "Birkenbihl", "birkenbihl", 1, "birkenbihl",
        ExerciseCheckMode.None, null, null, ["wordByWord", "autoDecode", "vocabLinked"], ItemNoun: "Sätze", ItemNounOne: "Satz");
    /// <inheritdoc/>
    public override IReadOnlyList<ContentItem> ItemsOf(string configJson)
    {
        // Prompt = sentence in the source language, "answer" = natural translation (for display/progress, not for typing).
        // The decoding travels along: it IS the method (word above word), and without it the card is an ordinary
        // translation card - the author maintained a word list nobody got to see.
        var c = Deserialize<BirkenbihlConfig>(configJson);
        return [.. c.Sentences.Select((s, i) => new ContentItem(i, s.LearningSentence, s.NaturalTranslation,
            [s.NaturalTranslation], Decoding: s.Decoding))];
    }

    /// <summary>
    /// Never typed: the method learns by reading/listening to the decoding and deliberately forgoes active
    /// querying (hence no <c>/check</c> for this type, see <see cref="BirkenbihlConfig"/>). The inherited
    /// default <c>true</c> put a text field next to the decoding and demanded the sentence be typed - the one
    /// thing this method does not ask for.
    /// <para>
    /// The decoding hangs on this: <c>CardFacets</c> passes it through unconditionally, which is only safe
    /// because this type - the only one that fills it - is never typed. Whoever makes a stage of this type typed
    /// has to decide there whether the decoding may still travel with it.
    /// </para>
    /// </summary>
    public override bool IsTypedStage(int stage) => false;

    /// <summary>
    /// No stage of this type is ever typed (B-93) - a position's <c>RequireTypedTest</c> would otherwise be
    /// accepted but never apply, because <see cref="PositionPracticeController"/> only scores a typed answer
    /// or an untyped one that is NOT gated by <c>RequireTypedTest</c>.
    /// </summary>
    public override bool SupportsRequireTypedTest => false;
}

/// <summary>Cloze: one item per gap; store-backed (the solution can come from the vocabulary store).</summary>
public sealed class ClozeExerciseType : ExerciseTypeBase
{
    /// <inheritdoc/>
    public override string Key => ExerciseTypeKeys.Cloze;
    /// <summary>
    /// The word bank is one pool for the whole text, sorted on purpose - see <see cref="Choices"/>. The choice per gap
    /// (B-599) is the options of one question and is shuffled like any other.
    /// </summary>
    public override bool ChoicesShareOnePool(int stage) => (ClozeStage)stage == ClozeStage.TranslationWordBank;

    /// <summary>
    /// B-635 (decision 3 of B-632): free text with the translation, then letter boxes, scrambled letters and the choice
    /// per gap - the order of the help, smallest first.
    /// </summary>
    public override IReadOnlyList<int> EaseLadder { get; } =
        [(int)ClozeStage.TranslationFreeText, (int)ClozeStage.LetterBoxes, (int)ClozeStage.LetterScramble, (int)ClozeStage.Choice];

    /// <inheritdoc/>
    public override bool SupportsRemediation => true;

    /// <summary>B-599: a choice per gap with fewer than two distractors is played as letter boxes, card by card.</summary>
    public override int? ChoiceFallbackStage(int stage) =>
        (ClozeStage)stage == ClozeStage.Choice ? (int)ClozeStage.LetterBoxes : null;

    /// <summary>
    /// B-599: letter boxes on their own stage, and on the scramble and choice stages for a card that falls back to them
    /// (the choice's fallback arrives here as the letter-box stage, see <see cref="ChoiceFallbackStage"/>). The boxes
    /// mask the solution's letters and keep its separators ("didn't he": apostrophe and space fixed).
    /// </summary>
    public override (int? LetterBoxLength, string? AudioUrl, string? ImageUrl, string? AnswerPattern) StageFacets(ContentItem item, int stage) =>
        AsLetterBoxes(item, stage) ? (item.Answer.Length, null, null, StageMechanics.LetterBoxPattern(item.Answer)) : (null, null, null, null);

    private static bool AsLetterBoxes(ContentItem item, int stage) =>
        (ClozeStage)stage == ClozeStage.LetterBoxes
        || ((ClozeStage)stage == ClozeStage.LetterScramble && !StageMechanics.IsScramblable(item.Answer));

    /// <summary>B-599: the solution's letters on the scramble stage, when its words have 2 to 12 letters each (B-634).</summary>
    public override string? LetterTileSource(ContentItem item, int stage) =>
        (ClozeStage)stage == ClozeStage.LetterScramble && StageMechanics.IsScramblable(item.Answer) ? item.Answer : null;
    /// <inheritdoc/>
    public override ExerciseTypeManifest Manifest { get; } = new(
        ExerciseTypeKeys.Cloze, "Lückentext", "cloze", 1, "cloze",
        ExerciseCheckMode.StudyPlanTest, "tests", LearningMethod.Cloze,
        // "remediation" (B-635): a position of this type may run as remediation (SupportsRemediation) - the father's form reads it.
        ["wordBank", "translation", "letterHints", "vocabStore", "cueBox", "remediation"], ItemNoun: "Lücken", ItemNounOne: "Lücke");

    /// <inheritdoc/>
    public override IReadOnlyList<ContentItem> ItemsOf(string configJson)
    {
        var c = Deserialize<ClozeConfig>(configJson);
        return [.. c.Gaps.Select((g, i) =>
        {
            var solution = ClozeGaps.SolutionOf(g, g.Answer);
            return ClozeGaps.WithPrompt(g,
                new ContentItem(i, c.Text, solution, Accepted(solution, g.Alternatives), Hint: null, GapIndex: g.Index));
        })];
    }

    /// <inheritdoc/>
    public override int DefaultStage => (int)ClozeStage.TranslationWordBank;
    /// <inheritdoc/>
    public override int PreviewStage => (int)ClozeStage.TranslationFreeText;
    /// <inheritdoc/>
    public override bool IsTypedStage(int stage) => StageMechanics.IsTyped((ClozeStage)stage);
    /// <inheritdoc/>
    public override StoreResolution StoreResolution => StoreResolution.VocabRefs;

    /// <summary>
    /// The word bank of the exercise, whole and unshortened, on the stage named after it. Deliberately not
    /// the vocabulary pattern of "solution plus three distractors": the pool is curated by the author, and
    /// trimming it would silently discard that work. Nor does it shrink as gaps get filled – tracking
    /// consumption would need session state <b>and</b> give the last gap away for free.
    /// <para>
    /// Sorted, though. Authors maintain the bank gap by gap, so its natural order puts the first gap's
    /// solutions at the front – the <i>position</i> would then give away the mapping the stage is meant to
    /// ask about. Alphabetical rather than rotated: it is deterministic, it is the same on every card of the
    /// exercise (a pool that reshuffles per card is unreadable), and unlike a rotation it also breaks the
    /// correlation for the very first gap.
    /// </para>
    /// <para>
    /// Complete, too (B-487): a gap none of whose tiles the grader accepts gets its solution added. The bank is
    /// written by hand on several paths (editor, loader, API) and a store-backed gap takes its solution from the
    /// store, which can change after the bank was written - so the guarantee sits here, where every bank reaches
    /// the child. Added for every gap of the exercise, not only the asked one: a pool that differs per card names
    /// the asked gap's solution.
    /// </para>
    /// <para>
    /// Cleaned first (B-491, <see cref="AnswerChecking.Tiles"/>): a bank of blank entries is no bank and the stage
    /// stays free text - counted as a bank, the completion would turn it into a pool of nothing but solutions.
    /// </para>
    /// </summary>
    public override IReadOnlyList<string>? Choices(string configJson, IReadOnlyList<ContentItem> items, ContentItem item, int stage)
    {
        if ((ClozeStage)stage == ClozeStage.Choice) return GapChoices(Deserialize<ClozeConfig>(configJson), items, item);
        if ((ClozeStage)stage != ClozeStage.TranslationWordBank) return null;
        var config = Deserialize<ClozeConfig>(configJson);
        var pool = AnswerChecking.Tiles(config.WordBank ?? []);
        if (pool.Count == 0) return null;
        // B-501: an added solution takes the authors' form - "Went." beside "go", "gone" would be the only tile with a
        // capital and a full stop and so name itself. Grading folds case and closing marks, so the tile still solves.
        // Lowered only where the capital comes from the gap opening a sentence; "London" or a German noun keep theirs.
        var lowerBank = pool.Where(tile => char.IsLetter(tile[0])).All(tile => char.IsLower(tile[0]));
        var bareBank = pool.All(tile => StageMechanics.WithoutClosingMarks(tile) == tile);
        foreach (var gap in items)
        {
            // Checked against the pool as it grows: two gaps sharing a missing solution get one tile.
            if (pool.Any(tile => Grader.Matches(tile, gap))) continue;
            // A missing store entry leaves an empty placeholder; a blank tile would solve nothing.
            // Trimmed like the author's tiles (B-497): a leading space would sort the added solution first and name it.
            if (gap.GradedAnswers().FirstOrDefault(a => !string.IsNullOrWhiteSpace(a)) is { } solution)
                pool.Add(InBankForm(solution.Trim(), lowerBank && OpensSentence(config.Text, gap.GapIndex), bareBank));
        }
        return [.. pool.OrderBy(w => w, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>Most options of a choice per gap besides the solution (B-599, decision 3).</summary>
    public const int MaxGapDistractors = 4;

    /// <summary>
    /// The options of the choice per gap (B-599): the solution plus up to <see cref="MaxGapDistractors"/> distractors -
    /// the gap's own first (decision 2: they take precedence), topped up with the other gaps' solutions in text order
    /// only where fewer than two of them are left, so a gap with one own distractor still offers a choice. A distractor
    /// the grader would accept for this gap is no distractor and falls out, as does a second spelling of one already
    /// offered; under two left the card has no choice (<c>null</c>) and falls back to letter boxes.
    /// <para>
    /// Every option is shown in one form, so none names itself (risk 2): closing marks dropped (grading ignores them),
    /// apostrophes and spaces folded to one spelling, a capital that only marks a sentence start lowered - but, as for
    /// the word bank, only where every other option starts lower-case; "I'm" opening a sentence beside "I'll" keeps its
    /// capital, or the solution would be the only one with one. Where the asked gap opens a sentence all are capitalised.
    /// The card shuffles the list per session.
    /// </para>
    /// </summary>
    private static IReadOnlyList<string>? GapChoices(ClozeConfig config, IReadOnlyList<ContentItem> items, ContentItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Answer)) return null;
        var opens = OpensSentence(config.Text, item.GapIndex);
        // (text, its capital only marks a sentence start) - the solution first, then the candidates in priority order.
        var own = AnswerChecking.Tiles(config.Gaps.ElementAtOrDefault(item.Index)?.Distractors ?? [])
            .Select(d => (Text: Clean(d), Positional: false));
        var derived = items.Where(o => o.Index != item.Index && !string.IsNullOrWhiteSpace(o.Answer))
            .Select(o => (Text: Clean(o.Answer), Positional: OpensSentence(config.Text, o.GapIndex)));

        var solution = (Text: Clean(item.Answer), Positional: opens);
        var seen = new HashSet<string>(StringComparer.Ordinal) { StageMechanics.Normalize(solution.Text) };
        var picked = new List<(string Text, bool Positional)>();
        void Take(IEnumerable<(string Text, bool Positional)> candidates, int upTo)
        {
            foreach (var candidate in candidates)
            {
                if (picked.Count >= upTo) return;
                if (candidate.Text.Length == 0 || Grader.Matches(candidate.Text, item)) continue;
                if (!seen.Add(StageMechanics.Normalize(candidate.Text))) continue;
                picked.Add(candidate);
            }
        }
        Take(own, MaxGapDistractors);
        if (picked.Count < 2) Take(derived, picked.Count == 0 ? MaxGapDistractors : 2);
        if (picked.Count < 2) return null;

        var options = new[] { solution }.Concat(picked).ToList();
        // The bank's rule (B-501): lower a positional capital only where every other option starts lower-case.
        var lowerRest = options.Where(o => !o.Positional && o.Text.Length > 0 && char.IsLetter(o.Text[0]))
            .All(o => char.IsLower(o.Text[0]));
        return [.. options.Select(o => o.Positional && lowerRest ? Lowered(o.Text) : o.Text)
            .Select(o => opens ? Capitalized(o) : o)];
    }

    // One option in one spelling: trimmed, without closing marks or a pasted BOM, one apostrophe, single spaces.
    private static string Clean(string option) =>
        string.Join(' ', StageMechanics.FoldApostrophes(StageMechanics.WithoutClosingMarks(
            option.Replace(((char)0xFEFF).ToString(), "").Trim())).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string Lowered(string option) =>
        option.Length > 0 && char.IsUpper(option[0]) ? char.ToLowerInvariant(option[0]) + option[1..] : option;

    private static string Capitalized(string option) =>
        option.Length > 0 && char.IsLower(option[0]) ? char.ToUpperInvariant(option[0]) + option[1..] : option;

    private static string InBankForm(string solution, bool lower, bool bareBank)
    {
        // A byte-order mark after the stop would hide it from WithoutClosingMarks; grading drops it anyway.
        solution = solution.Replace(((char)0xFEFF).ToString(), "");
        var form = bareBank ? StageMechanics.WithoutClosingMarks(solution) : solution;
        return lower && form.Length > 0 && char.IsUpper(form[0]) ? char.ToLowerInvariant(form[0]) + form[1..] : form;
    }

    // The gap's capital is positional when nothing but blanks stand between it and the text start, a line break or a
    // closing mark ("1. {{1}}" in a numbered list counts too).
    private static bool OpensSentence(string text, int? gapIndex)
    {
        if (gapIndex is not { } index) return false;
        var at = text.IndexOf("{{" + index + "}}", StringComparison.Ordinal);
        if (at < 0) return false;
        var before = text[..at].TrimEnd(' ', '\t');
        return before.Length == 0 || before[^1] is '.' or '?' or '!' or '…' or '\n';
    }

    // Stateless; the same comparison that grades the child's pick decides whether a tile solves a gap.
    private static readonly AnswerGrader Grader = new();

    /// <summary>
    /// The cue box (B-524): the meaning of every gap (the store translation) plus the author's distractors, trimmed,
    /// one entry per spelling and sorted - the same on every card of the exercise, like the word bank above, so neither
    /// the position nor a per-card difference gives the mapping away. A gap without a store meaning adds nothing (a
    /// missing store key only leaves a placeholder hint, which is no meaning). An empty box is still a box: the stage
    /// withholds the per-gap hint either way.
    /// </summary>
    public override IReadOnlyList<string>? CueBox(string configJson, IReadOnlyList<ContentItem> items, int stage)
    {
        if ((ClozeStage)stage != ClozeStage.CueBoxFreeText) return null;
        var config = Deserialize<ClozeConfig>(configJson);
        var meanings = items.Where(i => !string.IsNullOrWhiteSpace(i.Answer)).Select(i => i.Hint);
        return [.. meanings.Concat(config.CueDistractors ?? [])
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Select(m => m!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(m => m, StringComparer.OrdinalIgnoreCase)];
    }

    /// <inheritdoc/>
    public override IReadOnlyList<StageOption> StageOptions { get; } =
    [
        new((int)ClozeStage.TranslationWordBank, "Wortbank"),
        new((int)ClozeStage.TranslationFreeText, "Übersetzung + Freitext"),
        new((int)ClozeStage.FreeText, "Freitext"),
        new((int)ClozeStage.CueBoxFreeText, "Wortkasten + Freitext"),
        new((int)ClozeStage.LetterBoxes, "Übersetzung + Buchstabenkästchen"),
        new((int)ClozeStage.LetterScramble, "Übersetzung + Buchstaben ordnen"),
        new((int)ClozeStage.Choice, "Übersetzung + Auswahl je Lücke"),
    ];
}

/// <summary>Matching: pairs left ↔ right. Study-plan test and additionally a direct catalog check.</summary>
public sealed class MatchingExerciseType : ExerciseTypeBase
{
    /// <inheritdoc/>
    public override string Key => ExerciseTypeKeys.Matching;
    /// <inheritdoc/>
    public override ExerciseTypeManifest Manifest { get; } = new(
        ExerciseTypeKeys.Matching, "Zuordnung", "matching", 1, "matching",
        ExerciseCheckMode.StudyPlanTest, "tests", LearningMethod.Matching,
        ["distractors", "reverse"], ItemNoun: "Paare", ItemNounOne: "Paar");

    /// <inheritdoc/>
    public override int DefaultStage => (int)MatchStage.Direct;
    /// <inheritdoc/>
    public override int PreviewStage => (int)MatchStage.Direct;

    /// <inheritdoc/>
    public override IReadOnlyList<ContentItem> ItemsOf(string configJson)
    {
        var c = Deserialize<MatchingConfig>(configJson);
        return [.. c.Pairs.Select((p, i) => new ContentItem(i, p.Left, p.Right, [p.Right]))];
    }

    /// <summary>
    /// Distractors on the stage named after them: the pair's own counterpart plus up to three counterparts of
    /// the <i>other</i> pairs (<see cref="StageMechanics.DistractorPool"/> – the same pool the vocabulary
    /// multiple-choice stage offers). <see cref="MatchStage.Direct"/> stays free text.
    /// <para>
    /// No curated distractor list in the config: the plausible wrong options of a matching exercise are
    /// exactly the other entries of its right-hand column, so asking the author for them again would be busy
    /// work that can also contradict the pairs.
    /// </para>
    /// </summary>
    public override IReadOnlyList<string>? Choices(string configJson, IReadOnlyList<ContentItem> items, ContentItem item, int stage) =>
        (MatchStage)stage == MatchStage.Distractors ? StageMechanics.DistractorPool(items, item, maxDistractors: 3) : null;

    /// <inheritdoc/>
    public override CheckResult Check(string configJson, IReadOnlyList<GivenAnswer> answers, int? seed)
    {
        var c = Deserialize<MatchingConfig>(configJson);
        var given = AnswerChecking.ByIndex(answers);
        var items = c.Pairs.Select((pair, i) =>
        {
            var value = AnswerChecking.Value(given, i);
            return new ItemCheck(i, pair.Left, value, pair.Right, AnswerChecking.TextMatch(value, pair.Right));
        });
        return AnswerChecking.Aggregate(items);
    }
}

/// <summary>Fixed arithmetic problems: numeric comparison per problem within the tolerance.</summary>
public sealed class ArithmeticExerciseType : ExerciseTypeBase
{
    /// <inheritdoc/>
    public override string Key => ExerciseTypeKeys.Arithmetic;
    /// <inheritdoc/>
    public override ExerciseTypeManifest Manifest { get; } = new(
        ExerciseTypeKeys.Arithmetic, "Rechenaufgaben", "arithmetic", 1, "arithmetic",
        ExerciseCheckMode.CatalogCheck, null, null, ["tolerance"], ItemNoun: "Aufgaben", ItemNounOne: "Aufgabe");

    /// <inheritdoc/>
    public override IReadOnlyList<ContentItem> ItemsOf(string configJson)
    {
        var c = Deserialize<ArithmeticConfig>(configJson);
        return [.. c.Problems.Select((p, i) =>
        {
            var answer = p.Answer.ToString(CultureInfo.InvariantCulture);
            return new ContentItem(i, p.Prompt, answer, [answer], Numeric: new NumericTarget(p.Answer, p.Tolerance));
        })];
    }

    /// <inheritdoc/>
    public override CheckResult Check(string configJson, IReadOnlyList<GivenAnswer> answers, int? seed)
    {
        var c = Deserialize<ArithmeticConfig>(configJson);
        var given = AnswerChecking.ByIndex(answers);
        var items = c.Problems.Select((p, i) =>
        {
            var value = AnswerChecking.Value(given, i);
            return new ItemCheck(i, p.Prompt, value, p.Answer.ToString(CultureInfo.InvariantCulture),
                AnswerChecking.NumericMatch(value, p.Answer, p.Tolerance));
        });
        return AnswerChecking.Aggregate(items);
    }
}

/// <summary>
/// Random arithmetic problems: rules are stored, the problem set is generated per request from a fixed seed
/// (<see cref="IGeneratingExerciseType"/>) and re-checked server-side from the same seed.
/// </summary>
public sealed class ArithmeticDrillExerciseType(ArithmeticProblemGenerator generator)
    : ExerciseTypeBase, IGeneratingExerciseType
{
    /// <inheritdoc/>
    public override string Key => ExerciseTypeKeys.ArithmeticDrill;
    /// <inheritdoc/>
    public override ExerciseTypeManifest Manifest { get; } = new(
        ExerciseTypeKeys.ArithmeticDrill, "Rechen-Drill", "arithmetic", 1, "arithmetic-drill",
        ExerciseCheckMode.CatalogGenerateCheck, null, null, ["generated", "seed"]);

    // Tasks are generated per request - no fixed, individually addressable contents.
    /// <inheritdoc/>
    public override IReadOnlyList<ContentItem> ItemsOf(string configJson) => [];

    /// <summary>Checks the config bounds; error message or <c>null</c> if everything is fine.</summary>
    public static string? Validate(ArithmeticDrillConfig c) =>
        c.Operations.Count == 0 ? "At least one operation type is required."
        : c.MaxOperand < c.MinOperand ? "MaxOperand must be ≥ MinOperand."
        : c.ProblemCount is < 1 or > 100 ? "ProblemCount must be between 1 and 100."
        : null;

    /// <inheritdoc/>
    public (int Seed, IReadOnlyList<GeneratedProblem> Problems) Generate(string configJson, int? seed)
    {
        var config = Deserialize<ArithmeticDrillConfig>(configJson);
        // Pin the seed (even for "real" randomness) so the set stays gradable later.
        int effectiveSeed = seed ?? config.Seed ?? Random.Shared.Next();
        return (effectiveSeed, generator.Generate(config, new Random(effectiveSeed)));
    }

    /// <summary>Regenerates the set from the same seed and grades it. <c>null</c> if no seed is present (→ 400 for the caller).</summary>
    public override CheckResult? Check(string configJson, IReadOnlyList<GivenAnswer> answers, int? seed)
    {
        var config = Deserialize<ArithmeticDrillConfig>(configJson);
        if ((seed ?? config.Seed) is not { } s) return null;
        return AnswerChecking.CheckGenerated(generator.Generate(config, new Random(s)), answers);
    }
}

/// <summary>List to memorize (e.g. the federal states): as a set, or position-exact with <c>Ordered</c>.</summary>
public sealed class ListExerciseType : ExerciseTypeBase
{
    /// <inheritdoc/>
    public override string Key => ExerciseTypeKeys.List;
    /// <inheritdoc/>
    public override ExerciseTypeManifest Manifest { get; } = new(
        ExerciseTypeKeys.List, "Liste", "list", 1, "list",
        ExerciseCheckMode.CatalogCheck, null, null, ["orderedOptional", "alternatives"], ItemNoun: "Einträge", ItemNounOne: "Eintrag");

    // Without `Ordered` the entries are a set: naming them in any sequence is right, so the play path must not
    // demand entry N on card N. The catalog check has always graded it that way (see Check below); this hook is
    // what carries the same rule into practice and exam.
    /// <inheritdoc/>
    public override bool GradesAsSet(string configJson) => !Deserialize<ListConfig>(configJson).Ordered;

    /// <inheritdoc/>
    public override IReadOnlyList<ContentItem> ItemsOf(string configJson)
    {
        var c = Deserialize<ListConfig>(configJson);
        return [.. c.Items.Select((e, i) => new ContentItem(i, c.Instruction ?? "", e.Value, Accepted(e.Value, e.Alternatives)))];
    }

    /// <inheritdoc/>
    public override CheckResult Check(string configJson, IReadOnlyList<GivenAnswer> answers, int? seed)
    {
        var c = Deserialize<ListConfig>(configJson);
        if (c.Ordered)
        {
            var given = AnswerChecking.ByIndex(answers);
            var items = c.Items.Select((entry, i) =>
            {
                var value = AnswerChecking.Value(given, i);
                return new ItemCheck(i, "", value, entry.Value, AnswerChecking.EntryMatches(entry, value));
            });
            return AnswerChecking.Aggregate(items);
        }

        // Unordered: for every expected entry it counts whether it (or an alternative) was named anywhere;
        // each mention is credited only once.
        var remaining = answers.Select(a => StageMechanics.Normalize(a.Value)).ToList();
        var results = c.Items.Select((entry, i) =>
        {
            var hit = remaining.FindIndex(a => AnswerChecking.EntryMatches(entry, a));
            string? matched = hit >= 0 ? answers[hit].Value : null;
            if (hit >= 0) remaining[hit] = " "; // consumed - prevents crediting the same mention twice
            return new ItemCheck(i, "", matched, entry.Value, hit >= 0);
        });
        return AnswerChecking.Aggregate(results);
    }
}

/// <summary>
/// The one rule for what a gap's solution is (B-524): the sentence form when the author set one, otherwise the store
/// word or the inline answer. One place, because the provider, the store resolver and the grader all read gaps, and
/// a path that skipped the form would grade the base form right again.
/// </summary>
internal static class ClozeGaps
{
    /// <summary>The gap's solution: <see cref="Gap.Form"/> if set, else <paramref name="fallback"/> (store word or inline answer).</summary>
    public static string SolutionOf(Gap gap, string fallback) =>
        string.IsNullOrWhiteSpace(gap.Form) ? fallback : gap.Form.Trim();

    /// <summary>
    /// B-633: the author's own prompt of a gap is its hint (it beats the store's translation) and the material above the
    /// text - it is the task as the test put it ("Richtung Osten"), so the card shows it openly instead of behind the
    /// tip button. Blank is no prompt: the item stays as it was built.
    /// </summary>
    public static ContentItem WithPrompt(Gap gap, ContentItem item) =>
        string.IsNullOrWhiteSpace(gap.Prompt) ? item : item with { Hint = gap.Prompt.Trim(), Passage = gap.Prompt.Trim() };
}

/// <summary>
/// Shared, stateless check primitives for the catalog checks (formerly <c>ExerciseAnswerChecker</c>). Text
/// comparisons run through <see cref="StageMechanics.Normalize"/>, so they behave like in the vocabulary test.
/// </summary>
internal static class AnswerChecking
{
    // `passage`/`audioUrl` are what the questions are ABOUT and belong to the exercise, so they ride along on
    // every atom. Reading passes a text, listening a recording - never both, and listening never its
    // transcript (that one is the creator's, handing it to the child would answer the question).
    // Both configs default their string to "", so blank is normalized to null here: a third state ("present
    // but empty") would reach the card as a field that is set and useless.
    public static IReadOnlyList<ContentItem> FromQuestions(IReadOnlyList<Question> questions,
        string? passage = null, string? audioUrl = null) =>
        [.. questions.Select((q, i) => new ContentItem(i, q.Prompt, q.Answer, [q.Answer],
            AudioUrl: Blank(audioUrl), Passage: Blank(passage)))];

    /// <summary>Blank-as-absent: an empty or whitespace-only config string is no content.</summary>
    public static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    /// <summary>
    /// The author's tiles as the child may see them: blank entries drop out (an empty one would arrive as an unlabelled
    /// button), kept ones lose the spaces around them, and of entries the grader cannot tell apart (<see cref="StageMechanics.Normalize"/>) only the first
    /// written stays - "went" and "Went." would be two right tiles. The one input cleanup for every curated list of
    /// options (B-491), so the word bank and the comprehension choices cannot differ in it.
    /// </summary>
    public static List<string> Tiles(IEnumerable<string?> entries) =>
        [.. entries.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e!.Trim()).DistinctBy(StageMechanics.Normalize)];

    /// <summary>
    /// The answer choices the creator typed for the question this atom stands for, cleaned by <see cref="Tiles"/>;
    /// <c>null</c> when nothing usable is left. Returned in the author's order - the card shuffles them per session
    /// (<c>PositionPlayService.CardFacets</c>), because authors tend to list the solution first and a fixed order
    /// teaches the position instead of the value.
    /// <para>
    /// Decided per item, not per exercise: one run may mix questions with options and questions without.
    /// </para>
    /// <para>
    /// Always solvable (B-490): if no option is one the grader accepts, the answer is added behind the author's
    /// options. Checked here, where every list reaches the child, because the lists are written on several paths
    /// (editor, loader, API) and only the editor warns.
    /// </para>
    /// <para>
    /// The stage plays no part here, unlike in every sibling type: the comprehension types have no stage
    /// selection (<c>StageOptions</c> is empty) and are typed on every stage, so there is nothing to gate on.
    /// </para>
    /// </summary>
    public static IReadOnlyList<string>? ChoicesOf(IReadOnlyList<Question> questions, ContentItem item)
    {
        if (item.Index < 0 || item.Index >= questions.Count) return null;
        var choices = questions[item.Index].Choices;
        if (choices is null) return null;
        var usable = Tiles(choices);
        if (usable.Count == 0) return null;
        if (!usable.Any(choice => Grader.Matches(choice, item))
            && item.GradedAnswers().FirstOrDefault(a => !string.IsNullOrWhiteSpace(a)) is { } answer)
            usable.Add(answer.Trim());
        return usable;
    }

    // Stateless; the same comparison that grades the child's pick decides whether an option answers the question.
    private static readonly AnswerGrader Grader = new();

    public static Dictionary<int, string?> ByIndex(IReadOnlyList<GivenAnswer> answers)
    {
        var map = new Dictionary<int, string?>();
        foreach (var a in answers) map[a.Index] = a.Value; // a later mention wins
        return map;
    }

    public static string? Value(Dictionary<int, string?> given, int index) =>
        given.TryGetValue(index, out var v) ? v : null;

    public static bool NumericMatch(string? given, decimal expected, decimal tolerance) =>
        NumericAnswer.Matches(given, expected, tolerance);

    public static bool TextMatch(string? given, string expected) =>
        StageMechanics.Normalize(given) == StageMechanics.Normalize(expected);

    /// <summary>Does the (raw or normalized) answer match the entry or one of its alternatives?</summary>
    public static bool EntryMatches(ListEntry entry, string? given)
    {
        var value = StageMechanics.Normalize(given);
        if (value.Length == 0) return false;
        if (value == StageMechanics.Normalize(entry.Value)) return true;
        return entry.Alternatives?.Any(alt => value == StageMechanics.Normalize(alt)) ?? false;
    }

    /// <summary>Random arithmetic problems: integer results exact, rounded ones with small tolerance.</summary>
    public static CheckResult CheckGenerated(IReadOnlyList<GeneratedProblem> problems, IReadOnlyList<GivenAnswer> answers)
    {
        var given = ByIndex(answers);
        var items = problems.Select((p, i) =>
        {
            var value = Value(given, i);
            var tolerance = p.Answer == Math.Truncate(p.Answer) ? 0m : 0.005m;
            return new ItemCheck(i, p.Prompt, value, p.Answer.ToString(CultureInfo.InvariantCulture),
                NumericMatch(value, p.Answer, tolerance));
        });
        return Aggregate(items);
    }

    public static CheckResult Aggregate(IEnumerable<ItemCheck> items)
    {
        var list = items.ToList();
        var correct = list.Count(i => i.Correct);
        var percent = list.Count == 0 ? 0 : (int)Math.Round(100.0 * correct / list.Count);
        return new CheckResult(list.Count, correct, percent, list);
    }
}
