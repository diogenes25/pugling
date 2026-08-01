using System.Globalization;
using Pugling.Api.Models;

namespace Pugling.Api.Exercises;

// The remaining built-in exercise types. Short, because they inherit their facet defaults from
// ExerciseTypeBase and only carry key/manifest/ItemsOf (+ check/stages where needed). Vocabulary sits
// apart because of its size (VocabularyExerciseType). Shared check primitives live in AnswerChecking (below).

/// <summary>Reading comprehension: text + comprehension questions (pure content exercise, no automatic check).</summary>
public sealed class ReadingExerciseType : ExerciseTypeBase
{
    /// <inheritdoc/>
    public override string Key => ExerciseTypeKeys.Reading;
    /// <inheritdoc/>
    public override ExerciseTypeManifest Manifest { get; } = new(
        ExerciseTypeKeys.Reading, "Leseverständnis", "reading", 1, "reading",
        ExerciseCheckMode.None, null, null, []);
    /// <inheritdoc/>
    public override IReadOnlyList<ContentItem> ItemsOf(string configJson) =>
        AnswerChecking.FromQuestions(Deserialize<ReadingConfig>(configJson).Questions);
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
    public override IReadOnlyList<ContentItem> ItemsOf(string configJson) =>
        AnswerChecking.FromQuestions(Deserialize<ListeningConfig>(configJson).Questions);
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
        return [.. c.Tasks.Select((t, i) => new ContentItem(i, t.Prompt, t.Answer, [t.Answer], t.RuleHint))];
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
    public static IReadOnlyList<ContentItem> FromQuestions(IReadOnlyList<Question> questions) =>
        [.. questions.Select((q, i) => new ContentItem(i, q.Prompt, q.Answer, [q.Answer]))];

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
