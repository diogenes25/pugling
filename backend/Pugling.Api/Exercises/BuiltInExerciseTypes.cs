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
        ExerciseCheckMode.None, null, null, []);
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
        ExerciseCheckMode.None, null, null, ["audio", "transcript"]);
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
    public override (int? LetterBoxLength, string? AudioUrl, string? ImageUrl) StageFacets(ContentItem item, int stage) =>
        (null, item.AudioUrl, null);

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
}

/// <summary>Grammar: transformation/rule tasks.</summary>
public sealed class GrammarExerciseType : ExerciseTypeBase
{
    /// <inheritdoc/>
    public override string Key => ExerciseTypeKeys.Grammar;
    /// <inheritdoc/>
    public override ExerciseTypeManifest Manifest { get; } = new(
        ExerciseTypeKeys.Grammar, "Grammatik", "prompts", 1, "grammar",
        ExerciseCheckMode.None, null, null, ["ruleHints"]);
    /// <inheritdoc/>
    public override IReadOnlyList<ContentItem> ItemsOf(string configJson)
    {
        var c = Deserialize<GrammarConfig>(configJson);
        return [.. c.Tasks.Select((t, i) =>
            new ContentItem(i, t.Prompt, t.Answer, [t.Answer], t.RuleHint,
                Passage: AnswerChecking.Blank(c.Instruction)))];
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
        ExerciseCheckMode.None, null, null, ["alternatives"]);
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
        ExerciseCheckMode.None, null, null, ["wordByWord", "autoDecode", "vocabLinked"]);
    /// <inheritdoc/>
    public override IReadOnlyList<ContentItem> ItemsOf(string configJson)
    {
        // Prompt = sentence in the source language, "answer" = natural translation (for display/progress, not for typing).
        var c = Deserialize<BirkenbihlConfig>(configJson);
        return [.. c.Sentences.Select((s, i) => new ContentItem(i, s.LearningSentence, s.NaturalTranslation, [s.NaturalTranslation]))];
    }
}

/// <summary>Cloze: one item per gap; store-backed (the solution can come from the vocabulary store).</summary>
public sealed class ClozeExerciseType : ExerciseTypeBase
{
    /// <inheritdoc/>
    public override string Key => ExerciseTypeKeys.Cloze;
    /// <inheritdoc/>
    public override ExerciseTypeManifest Manifest { get; } = new(
        ExerciseTypeKeys.Cloze, "Lückentext", "cloze", 1, "cloze",
        ExerciseCheckMode.StudyPlanTest, "tests", LearningMethod.Cloze,
        ["wordBank", "translation", "letterHints", "vocabStore"]);

    /// <inheritdoc/>
    public override IReadOnlyList<ContentItem> ItemsOf(string configJson)
    {
        var c = Deserialize<ClozeConfig>(configJson);
        return [.. c.Gaps.Select((g, i) => new ContentItem(i, c.Text, g.Answer, Accepted(g.Answer, g.Alternatives), Hint: null, GapIndex: g.Index))];
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
    /// </summary>
    public override IReadOnlyList<string>? Choices(string configJson, IReadOnlyList<ContentItem> items, ContentItem item, int stage)
    {
        if ((ClozeStage)stage != ClozeStage.TranslationWordBank) return null;
        var bank = Deserialize<ClozeConfig>(configJson).WordBank;
        return bank is { Count: > 0 } ? [.. bank.OrderBy(w => w, StringComparer.OrdinalIgnoreCase)] : null;
    }

    /// <inheritdoc/>
    public override IReadOnlyList<StageOption> StageOptions { get; } =
    [
        new((int)ClozeStage.TranslationWordBank, "Wortbank"),
        new((int)ClozeStage.TranslationFreeText, "Übersetzung + Freitext"),
        new((int)ClozeStage.FreeText, "Freitext"),
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
        ["distractors", "reverse"]);

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
        ExerciseCheckMode.CatalogCheck, null, null, ["tolerance"]);

    /// <inheritdoc/>
    public override IReadOnlyList<ContentItem> ItemsOf(string configJson)
    {
        var c = Deserialize<ArithmeticConfig>(configJson);
        return [.. c.Problems.Select((p, i) =>
        {
            var answer = p.Answer.ToString(CultureInfo.InvariantCulture);
            return new ContentItem(i, p.Prompt, answer, [answer]);
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
        ExerciseCheckMode.CatalogCheck, null, null, ["orderedOptional", "alternatives"]);

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
    /// The answer choices the creator typed for the question this atom stands for – unchanged and in their
    /// order; <c>null</c> when the list is empty. Curated content, not a generated pool: it is deliberately
    /// not reordered, because the author's order can carry meaning (a timeline, a "none of these" at the end)
    /// and there is no cross-item correlation to break – every question owns its own list.
    /// <para>
    /// Decided per item, not per exercise: one run may mix questions with options and questions without.
    /// Blank entries drop out (same stance as <see cref="Blank"/>) – an empty option would arrive as an
    /// unlabelled button.
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
        var usable = choices.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
        return usable.Count > 0 ? usable : null;
    }

    public static Dictionary<int, string?> ByIndex(IReadOnlyList<GivenAnswer> answers)
    {
        var map = new Dictionary<int, string?>();
        foreach (var a in answers) map[a.Index] = a.Value; // a later mention wins
        return map;
    }

    public static string? Value(Dictionary<int, string?> given, int index) =>
        given.TryGetValue(index, out var v) ? v : null;

    public static bool NumericMatch(string? given, decimal expected, decimal tolerance) =>
        decimal.TryParse(given, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
        && Math.Abs(value - expected) <= tolerance;

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
