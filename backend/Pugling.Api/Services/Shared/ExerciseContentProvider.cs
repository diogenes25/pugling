using System.Globalization;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// Ein einzelnes übbares/prüfbares Element einer Übung, verfahrensneutral aus der Übungs-Config projiziert.
/// <paramref name="Index"/> ist der stabile Positionsbezug (→ <see cref="PositionItemProgress.ItemIndex"/>).
/// <paramref name="AcceptedAnswers"/> enthält die erwartete Lösung plus zulässige Alternativen (roh, der
/// Textvergleich normalisiert später über <see cref="AnswerGrader"/>). <paramref name="GapIndex"/> ist nur
/// bei Lückentexten gesetzt (die {{n}}-Nummer der Lücke). <paramref name="ItemId"/> und
/// <paramref name="VocabularyId"/> tragen (nur bei Vokabelübungen) die stabile Item- bzw. Store-Identität –
/// die Grundlage für den je Kind/Item protokollierten Lernfortschritt; bei allen anderen Typen <c>null</c>.
/// </summary>
public record ContentItem(
    int Index,
    string Prompt,
    string Answer,
    IReadOnlyList<string> AcceptedAnswers,
    string? Hint = null,
    int? GapIndex = null,
    string? AudioUrl = null,
    int? ItemId = null,
    int? VocabularyId = null);

/// <summary>
/// Liest die einzelnen Inhalte einer Katalog-<see cref="Exercise"/> aus ihrer
/// <see cref="Exercise.ConfigJson"/> und projiziert sie verfahrensneutral in <see cref="ContentItem"/>s –
/// die eine Stelle, an der der neue Lehrplan-Motor an den Übungs-Inhalt kommt (ersetzt das direkte Laden
/// aus den Vokabel-/Lückentext-Stores). Zustandslos, ohne DB-Zugriff und daher leicht testbar. Die
/// Extraktion bleibt bewusst kanonisch (Vorderseite → Rückseite); Richtungs-/Stufen-Varianten und die
/// Bewertung selbst liegen weiterhin beim Motor bzw. <see cref="AnswerGrader"/>.
/// </summary>
public class ExerciseContentProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Die Inhalte einer Übung als verfahrensneutrale Item-Liste.</summary>
    public IReadOnlyList<ContentItem> ItemsOf(Exercise exercise) =>
        ItemsOf(exercise.Type, exercise.ConfigJson);

    /// <summary>
    /// Wie <see cref="ItemsOf(Exercise)"/>, aber direkt aus Typ + Roh-JSON. Typen ohne feste, einzeln
    /// abfragbare Inhalte liefern bewusst eine leere Liste: <see cref="ExerciseType.Essay"/> (freier Text,
    /// kein Item-für-Item-Abgleich) und <see cref="ExerciseType.ArithmeticDrill"/> (Aufgaben werden pro
    /// Abruf erzeugt, siehe <see cref="ArithmeticProblemGenerator"/>).
    /// </summary>
    public IReadOnlyList<ContentItem> ItemsOf(ExerciseType type, string configJson) => type switch
    {
        ExerciseType.Vocabulary => FromVocabulary(Deserialize<VocabularyConfig>(configJson)),
        ExerciseType.Cloze => FromCloze(Deserialize<ClozeConfig>(configJson)),
        ExerciseType.Matching => FromMatching(Deserialize<MatchingConfig>(configJson)),
        ExerciseType.List => FromList(Deserialize<ListConfig>(configJson)),
        ExerciseType.Arithmetic => FromArithmetic(Deserialize<ArithmeticConfig>(configJson)),
        ExerciseType.Grammar => FromGrammar(Deserialize<GrammarConfig>(configJson)),
        ExerciseType.Translation => FromTranslation(Deserialize<TranslationConfig>(configJson)),
        ExerciseType.Reading => FromQuestions(Deserialize<ReadingConfig>(configJson).Questions),
        ExerciseType.Listening => FromQuestions(Deserialize<ListeningConfig>(configJson).Questions),
        ExerciseType.Birkenbihl => FromBirkenbihl(Deserialize<BirkenbihlConfig>(configJson)),
        _ => [],
    };

    private static T Deserialize<T>(string configJson) where T : new() =>
        (string.IsNullOrWhiteSpace(configJson) ? default : JsonSerializer.Deserialize<T>(configJson, JsonOptions)) ?? new();

    private static IReadOnlyList<string> Accepted(string answer, IEnumerable<string>? alternatives = null) =>
        alternatives is null ? [answer] : [answer, .. alternatives];

    /// <summary>
    /// Wendet die Abfragerichtung einer Vokabel auf ein kanonisch (Wort → Übersetzung) gebautes Item an:
    /// <c>back-to-front</c> vertauscht Prompt/Antwort, <c>both</c> vertauscht deterministisch bei ungeradem
    /// Index (stabil pro Item, ohne Zufall). Der Index bleibt gleich – der Leitner-/Test-Fortschritt kippt nicht.
    /// </summary>
    public static ContentItem WithDirection(ContentItem item, string? direction) => direction switch
    {
        "back-to-front" => Swap(item),
        "both" => item.Index % 2 == 0 ? item : Swap(item),
        _ => item,
    };

    // Prompt/Antwort tauschen; die Alternativen des Rückwärts-Falls entfallen (galten für die alte Antwort),
    // der Artikel-Hinweis ebenso (er gehörte zum nun abgefragten Wort). Die Aussprache-Audioquelle entfällt
    // ebenfalls: sie liest das Wort vor, das nach dem Tausch die Lösung ist – sonst würde die Hör-Stufe die
    // Antwort vorsprechen (Anti-Schummel). Rückwärts-Items werden in der Hör-Stufe damit textlich gezeigt.
    private static ContentItem Swap(ContentItem it) =>
        it with { Prompt = it.Answer, Answer = it.Prompt, AcceptedAnswers = [it.Prompt], Hint = null, AudioUrl = null };

    // Vokabeln: kanonisch Vorderseite → Rückseite; die Abfragerichtung dreht das Item (siehe WithDirection).
    private static List<ContentItem> FromVocabulary(VocabularyConfig c) =>
        c.Items.Select((v, i) => WithDirection(new ContentItem(i, v.Front ?? "", v.Back ?? "", [v.Back ?? ""], v.Hint), c.Direction)).ToList();

    // Lückentext: ein Item je Lücke; Prompt ist der Trägertext, GapIndex die {{n}}-Nummer.
    private static List<ContentItem> FromCloze(ClozeConfig c) =>
        c.Gaps.Select((g, i) => new ContentItem(i, c.Text, g.Answer, Accepted(g.Answer, g.Alternatives), Hint: null, GapIndex: g.Index)).ToList();

    private static List<ContentItem> FromMatching(MatchingConfig c) =>
        c.Pairs.Select((p, i) => new ContentItem(i, p.Left, p.Right, [p.Right])).ToList();

    private static List<ContentItem> FromList(ListConfig c) =>
        c.Items.Select((e, i) => new ContentItem(i, c.Instruction ?? "", e.Value, Accepted(e.Value, e.Alternatives))).ToList();

    private static List<ContentItem> FromArithmetic(ArithmeticConfig c) =>
        c.Problems.Select((p, i) =>
        {
            var answer = p.Answer.ToString(CultureInfo.InvariantCulture);
            return new ContentItem(i, p.Prompt, answer, [answer]);
        }).ToList();

    private static List<ContentItem> FromGrammar(GrammarConfig c) =>
        c.Tasks.Select((t, i) => new ContentItem(i, t.Prompt, t.Answer, [t.Answer], t.RuleHint)).ToList();

    private static List<ContentItem> FromTranslation(TranslationConfig c) =>
        c.Items.Select((t, i) => new ContentItem(i, t.Source, t.Target, Accepted(t.Target, t.Alternatives))).ToList();

    private static List<ContentItem> FromQuestions(IReadOnlyList<Question> questions) =>
        questions.Select((q, i) => new ContentItem(i, q.Prompt, q.Answer, [q.Answer])).ToList();

    // Birkenbihl: reine Inhaltsübung ohne aktives Abfragen – Prompt = Satz der Lernsprache,
    // „Antwort" = die natürliche Übersetzung (nützlich für Anzeige/Fortschritt, nicht zum Tippen-Prüfen).
    private static List<ContentItem> FromBirkenbihl(BirkenbihlConfig c) =>
        c.Sentences.Select((s, i) => new ContentItem(i, s.LearningSentence, s.NaturalTranslation, [s.NaturalTranslation])).ToList();
}
