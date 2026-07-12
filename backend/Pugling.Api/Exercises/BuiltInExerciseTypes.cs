using System.Globalization;
using Pugling.Api.Models;

namespace Pugling.Api.Exercises;

// Die übrigen eingebauten Übungstypen. Kurz, weil sie ihre Facetten-Defaults aus ExerciseTypeBase erben und
// nur Key/Manifest/ItemsOf (+ ggf. Check/Stufen) tragen. Vokabeln stehen wegen ihres Umfangs separat
// (VocabularyExerciseType). Geteilte Prüf-Primitive liegen in AnswerChecking (unten).

/// <summary>Leseverständnis: Text + Verständnisfragen (reine Inhaltsübung, kein automatischer Check).</summary>
public sealed class ReadingExerciseType : ExerciseTypeBase
{
    public override string Key => ExerciseTypeKeys.Reading;
    public override ExerciseTypeManifest Manifest { get; } = new(
        ExerciseTypeKeys.Reading, "Leseverständnis", "reading", 1, "reading",
        ExerciseCheckMode.None, null, null, []);
    public override IReadOnlyList<ContentItem> ItemsOf(string configJson) =>
        AnswerChecking.FromQuestions(Deserialize<ReadingConfig>(configJson).Questions);
}

/// <summary>Hörverständnis: Audioquelle + Verständnisfragen.</summary>
public sealed class ListeningExerciseType : ExerciseTypeBase
{
    public override string Key => ExerciseTypeKeys.Listening;
    public override ExerciseTypeManifest Manifest { get; } = new(
        ExerciseTypeKeys.Listening, "Hörverständnis", "listening", 1, "listening",
        ExerciseCheckMode.None, null, null, ["audio", "transcript"]);
    public override IReadOnlyList<ContentItem> ItemsOf(string configJson) =>
        AnswerChecking.FromQuestions(Deserialize<ListeningConfig>(configJson).Questions);
}

/// <summary>Aufsatz: freier Text, kein Item-für-Item-Abgleich – daher keine prüfbaren Inhalte.</summary>
public sealed class EssayExerciseType : ExerciseTypeBase
{
    public override string Key => ExerciseTypeKeys.Essay;
    public override ExerciseTypeManifest Manifest { get; } = new(
        ExerciseTypeKeys.Essay, "Aufsatz", "essay", 1, "essays",
        ExerciseCheckMode.None, null, null, ["rubric", "wordCount"]);
    public override IReadOnlyList<ContentItem> ItemsOf(string configJson) => [];
}

/// <summary>Grammatik: Umformungs-/Regelaufgaben.</summary>
public sealed class GrammarExerciseType : ExerciseTypeBase
{
    public override string Key => ExerciseTypeKeys.Grammar;
    public override ExerciseTypeManifest Manifest { get; } = new(
        ExerciseTypeKeys.Grammar, "Grammatik", "prompts", 1, "grammar",
        ExerciseCheckMode.None, null, null, ["ruleHints"]);
    public override IReadOnlyList<ContentItem> ItemsOf(string configJson)
    {
        var c = Deserialize<GrammarConfig>(configJson);
        return [.. c.Tasks.Select((t, i) => new ContentItem(i, t.Prompt, t.Answer, [t.Answer], t.RuleHint))];
    }
}

/// <summary>Übersetzung: Sätze mit erwarteter Übersetzung (+ Alternativen).</summary>
public sealed class TranslationExerciseType : ExerciseTypeBase
{
    public override string Key => ExerciseTypeKeys.Translation;
    public override ExerciseTypeManifest Manifest { get; } = new(
        ExerciseTypeKeys.Translation, "Übersetzung", "prompts", 1, "translation",
        ExerciseCheckMode.None, null, null, ["alternatives"]);
    public override IReadOnlyList<ContentItem> ItemsOf(string configJson)
    {
        var c = Deserialize<TranslationConfig>(configJson);
        return [.. c.Items.Select((t, i) => new ContentItem(i, t.Source, t.Target, Accepted(t.Target, t.Alternatives)))];
    }
}

/// <summary>Birkenbihl: Wort-für-Wort-Dekodierung; reine Inhaltsübung ohne aktives Abfragen.</summary>
public sealed class BirkenbihlExerciseType : ExerciseTypeBase
{
    public override string Key => ExerciseTypeKeys.Birkenbihl;
    public override ExerciseTypeManifest Manifest { get; } = new(
        ExerciseTypeKeys.Birkenbihl, "Birkenbihl", "birkenbihl", 1, "birkenbihl",
        ExerciseCheckMode.None, null, null, ["wordByWord", "autoDecode", "vocabLinked"]);
    public override IReadOnlyList<ContentItem> ItemsOf(string configJson)
    {
        // Prompt = Satz der Lernsprache, „Antwort" = natürliche Übersetzung (für Anzeige/Fortschritt, nicht zum Tippen).
        var c = Deserialize<BirkenbihlConfig>(configJson);
        return [.. c.Sentences.Select((s, i) => new ContentItem(i, s.LearningSentence, s.NaturalTranslation, [s.NaturalTranslation]))];
    }
}

/// <summary>Lückentext: ein Item je Lücke; Store-gestützt (Lösung kann aus dem Vokabelspeicher kommen).</summary>
public sealed class ClozeExerciseType : ExerciseTypeBase
{
    public override string Key => ExerciseTypeKeys.Cloze;
    public override ExerciseTypeManifest Manifest { get; } = new(
        ExerciseTypeKeys.Cloze, "Lückentext", "cloze", 1, "cloze",
        ExerciseCheckMode.StudyPlanTest, "tests", LearningMethod.Cloze,
        ["wordBank", "translation", "letterHints", "vocabStore"]);

    public override IReadOnlyList<ContentItem> ItemsOf(string configJson)
    {
        var c = Deserialize<ClozeConfig>(configJson);
        return [.. c.Gaps.Select((g, i) => new ContentItem(i, c.Text, g.Answer, Accepted(g.Answer, g.Alternatives), Hint: null, GapIndex: g.Index))];
    }

    public override int DefaultStage => (int)ClozeStage.TranslationWordBank;
    public override int PreviewStage => (int)ClozeStage.TranslationFreeText;
    public override bool IsTypedStage(int stage) => StageMechanics.IsTyped((ClozeStage)stage);
    public override StoreResolution StoreResolution => StoreResolution.VocabRefs;

    public override IReadOnlyList<StageOption> StageOptions { get; } =
    [
        new((int)ClozeStage.TranslationWordBank, "Wortbank"),
        new((int)ClozeStage.TranslationFreeText, "Übersetzung + Freitext"),
        new((int)ClozeStage.FreeText, "Freitext"),
    ];
}

/// <summary>Zuordnung: Paare links ↔ rechts. StudyPlan-Test und zusätzlich ein Katalog-Direktcheck.</summary>
public sealed class MatchingExerciseType : ExerciseTypeBase
{
    public override string Key => ExerciseTypeKeys.Matching;
    public override ExerciseTypeManifest Manifest { get; } = new(
        ExerciseTypeKeys.Matching, "Zuordnung", "matching", 1, "matching",
        ExerciseCheckMode.StudyPlanTest, "tests", LearningMethod.Matching,
        ["distractors", "reverse"]);

    public override int DefaultStage => (int)MatchStage.Direct;
    public override int PreviewStage => (int)MatchStage.Direct;

    public override IReadOnlyList<ContentItem> ItemsOf(string configJson)
    {
        var c = Deserialize<MatchingConfig>(configJson);
        return [.. c.Pairs.Select((p, i) => new ContentItem(i, p.Left, p.Right, [p.Right]))];
    }

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

/// <summary>Feste Rechenaufgaben: numerischer Vergleich je Aufgabe im Rahmen der Toleranz.</summary>
public sealed class ArithmeticExerciseType : ExerciseTypeBase
{
    public override string Key => ExerciseTypeKeys.Arithmetic;
    public override ExerciseTypeManifest Manifest { get; } = new(
        ExerciseTypeKeys.Arithmetic, "Rechenaufgaben", "arithmetic", 1, "arithmetic",
        ExerciseCheckMode.CatalogCheck, null, null, ["tolerance"]);

    public override IReadOnlyList<ContentItem> ItemsOf(string configJson)
    {
        var c = Deserialize<ArithmeticConfig>(configJson);
        return [.. c.Problems.Select((p, i) =>
        {
            var answer = p.Answer.ToString(CultureInfo.InvariantCulture);
            return new ContentItem(i, p.Prompt, answer, [answer]);
        })];
    }

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
/// Zufalls-Rechenaufgaben: Regeln sind gespeichert, der Satz wird pro Abruf aus einem festen Seed erzeugt
/// (<see cref="IGeneratingExerciseType"/>) und aus demselben Seed serverseitig erneut geprüft.
/// </summary>
public sealed class ArithmeticDrillExerciseType(ArithmeticProblemGenerator generator)
    : ExerciseTypeBase, IGeneratingExerciseType
{
    public override string Key => ExerciseTypeKeys.ArithmeticDrill;
    public override ExerciseTypeManifest Manifest { get; } = new(
        ExerciseTypeKeys.ArithmeticDrill, "Rechen-Drill", "arithmetic", 1, "arithmetic-drill",
        ExerciseCheckMode.CatalogGenerateCheck, null, null, ["generated", "seed"]);

    // Aufgaben werden pro Abruf erzeugt – keine festen, einzeln abfragbaren Inhalte.
    public override IReadOnlyList<ContentItem> ItemsOf(string configJson) => [];

    /// <summary>Prüft die Config-Grenzen; Fehlermeldung oder <c>null</c>, wenn alles passt.</summary>
    public static string? Validate(ArithmeticDrillConfig c) =>
        c.Operations.Count == 0 ? "At least one operation type is required."
        : c.MaxOperand < c.MinOperand ? "MaxOperand must be ≥ MinOperand."
        : c.ProblemCount is < 1 or > 100 ? "ProblemCount must be between 1 and 100."
        : null;

    public (int Seed, IReadOnlyList<GeneratedProblem> Problems) Generate(string configJson, int? seed)
    {
        var config = Deserialize<ArithmeticDrillConfig>(configJson);
        // Seed fixieren (auch bei „echtem" Zufall), damit der Satz später auswertbar bleibt.
        int effectiveSeed = seed ?? config.Seed ?? Random.Shared.Next();
        return (effectiveSeed, generator.Generate(config, new Random(effectiveSeed)));
    }

    /// <summary>Erzeugt den Satz aus demselben Seed erneut und bewertet ihn. <c>null</c>, wenn kein Seed vorliegt (→ 400 beim Aufrufer).</summary>
    public override CheckResult? Check(string configJson, IReadOnlyList<GivenAnswer> answers, int? seed)
    {
        var config = Deserialize<ArithmeticDrillConfig>(configJson);
        if ((seed ?? config.Seed) is not { } s) return null;
        return AnswerChecking.CheckGenerated(generator.Generate(config, new Random(s)), answers);
    }
}

/// <summary>Auswendig zu lernende Liste (z. B. die Bundesländer): als Menge, oder positionsgenau bei <c>Ordered</c>.</summary>
public sealed class ListExerciseType : ExerciseTypeBase
{
    public override string Key => ExerciseTypeKeys.List;
    public override ExerciseTypeManifest Manifest { get; } = new(
        ExerciseTypeKeys.List, "Liste", "list", 1, "list",
        ExerciseCheckMode.CatalogCheck, null, null, ["orderedOptional", "alternatives"]);

    public override IReadOnlyList<ContentItem> ItemsOf(string configJson)
    {
        var c = Deserialize<ListConfig>(configJson);
        return [.. c.Items.Select((e, i) => new ContentItem(i, c.Instruction ?? "", e.Value, Accepted(e.Value, e.Alternatives)))];
    }

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

        // Ungeordnet: für jeden erwarteten Eintrag zählt, ob er (oder eine Alternative) irgendwo genannt wurde;
        // jede Nennung wird nur einmal angerechnet.
        var remaining = answers.Select(a => StageMechanics.Normalize(a.Value)).ToList();
        var results = c.Items.Select((entry, i) =>
        {
            var hit = remaining.FindIndex(a => AnswerChecking.EntryMatches(entry, a));
            string? matched = hit >= 0 ? answers[hit].Value : null;
            if (hit >= 0) remaining[hit] = " "; // verbraucht – verhindert Doppelanrechnung derselben Nennung
            return new ItemCheck(i, "", matched, entry.Value, hit >= 0);
        });
        return AnswerChecking.Aggregate(results);
    }
}

/// <summary>
/// Geteilte, zustandslose Prüf-Primitive der Katalog-Checks (früher <c>ExerciseAnswerChecker</c>). Textvergleiche
/// laufen über <see cref="StageMechanics.Normalize"/>, damit sie sich wie im Vokabeltest verhalten.
/// </summary>
internal static class AnswerChecking
{
    public static IReadOnlyList<ContentItem> FromQuestions(IReadOnlyList<Question> questions) =>
        [.. questions.Select((q, i) => new ContentItem(i, q.Prompt, q.Answer, [q.Answer]))];

    public static Dictionary<int, string?> ByIndex(IReadOnlyList<GivenAnswer> answers)
    {
        var map = new Dictionary<int, string?>();
        foreach (var a in answers) map[a.Index] = a.Value; // spätere Nennung gewinnt
        return map;
    }

    public static string? Value(Dictionary<int, string?> given, int index) =>
        given.TryGetValue(index, out var v) ? v : null;

    public static bool NumericMatch(string? given, decimal expected, decimal tolerance) =>
        decimal.TryParse(given, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
        && Math.Abs(value - expected) <= tolerance;

    public static bool TextMatch(string? given, string expected) =>
        StageMechanics.Normalize(given) == StageMechanics.Normalize(expected);

    /// <summary>Trifft die (rohe oder normalisierte) Antwort den Eintrag oder eine seiner Alternativen?</summary>
    public static bool EntryMatches(ListEntry entry, string? given)
    {
        var value = StageMechanics.Normalize(given);
        if (value.Length == 0) return false;
        if (value == StageMechanics.Normalize(entry.Value)) return true;
        return entry.Alternatives?.Any(alt => value == StageMechanics.Normalize(alt)) ?? false;
    }

    /// <summary>Zufalls-Rechenaufgaben: ganzzahlige Ergebnisse exakt, gerundete mit kleiner Toleranz.</summary>
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
