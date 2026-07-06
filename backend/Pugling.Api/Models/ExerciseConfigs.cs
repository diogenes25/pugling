using System.Text.Json.Serialization;

namespace Pugling.Api.Models;

// Config-Schemas je Übungstyp.
// Werden als JSON in Exercise.ConfigJson gespeichert, im API aber typisiert
// als Teil von ExercisePayload<TConfig> / ExerciseResponse<TConfig> übertragen.
// Jeder Typ = eigener Pfad + eigenes Swagger-Schema.

/// <summary>Frage mit optionalen Antwortmöglichkeiten (leer = Freitext).</summary>
public record Question(string Prompt, List<string>? Choices, string Answer);

/// <summary>
/// Vokabelübung. Verweist über <see cref="Refs"/> auf Einträge des Vokabel-Stores (per <see cref="VocabRef.VocabularyId"/>),
/// damit dieselbe Vokabel über mehrere Übungen hinweg verknüpft und zentral pflegbar ist. Inline-<see cref="Items"/>
/// ohne eigene <see cref="VocabItem.VocabularyId"/> werden beim Speichern automatisch im Store angelegt und verlinkt –
/// so liegt garantiert jede genutzte Vokabel im Store. <see cref="SourceLang"/>/<see cref="TargetLang"/> sind dafür
/// nötig (der Store-Key wird aus Sprache + Wort + Übersetzung gebildet).
/// </summary>
public class VocabularyConfig
{
    /// <summary>Abfragerichtung: front-to-back | back-to-front | both.</summary>
    public string Direction { get; set; } = "front-to-back";
    /// <summary>Sprachcode der Ausgangssprache (z. B. „en"); nötig, um Inline-<see cref="Items"/> im Store anzulegen.</summary>
    public string SourceLang { get; set; } = "";
    /// <summary>Sprachcode der Zielsprache (z. B. „de"); nötig, um Inline-<see cref="Items"/> im Store anzulegen.</summary>
    public string TargetLang { get; set; } = "";
    /// <summary>Referenzen auf Vokabel-Store-Einträge (per ID; die Antwort ergänzt den Link).</summary>
    public List<VocabRef>? Refs { get; set; }
    /// <summary>Inline-Vokabeln; ohne <see cref="VocabItem.VocabularyId"/> beim Speichern automatisch im Store angelegt.</summary>
    public List<VocabItem> Items { get; set; } = new();
}

/// <summary>
/// Verweis auf einen Vokabel-Store-Eintrag. Persistiert wird die <paramref name="VocabularyId"/> (und – als Lesehilfe –
/// optional der <paramref name="Key"/>). <paramref name="Self"/> ist ein rein abgeleiteter HATEOAS-Link
/// (<c>/api/v1/learn/vocabulary/{id}</c>), der nur in Antworten gefüllt und nie gespeichert wird.
/// </summary>
[JsonConverter(typeof(VocabRefJsonConverter))]
public record VocabRef(int VocabularyId, string? Key = null, string? Self = null);

/// <summary>
/// Inline-Vokabel. <paramref name="VocabularyId"/> verweist auf den zugehörigen Store-Eintrag (beim Speichern
/// automatisch angelegt); <paramref name="Self"/> ist der abgeleitete, nur lesend gefüllte HATEOAS-Link.
/// </summary>
public record VocabItem(string Front, string Back, string? Hint = null,
    int? VocabularyId = null, [property: JsonPropertyName("_self")] string? Self = null);

/// <summary>Leseverständnis: Text + Verständnisfragen.</summary>
public class ReadingConfig
{
    public string Text { get; set; } = "";
    public List<Question> Questions { get; set; } = new();
}

/// <summary>Lückentext: Text mit Platzhaltern {{1}}, {{2}} … + Lösungen.</summary>
public class ClozeConfig
{
    public string Text { get; set; } = "";
    public List<Gap> Gaps { get; set; } = new();
    /// <summary>Optionaler Wortpool zur Auswahl.</summary>
    public List<string>? WordBank { get; set; }
}
/// <summary>
/// Eine Lücke. Ist <paramref name="VocabKey"/> gesetzt, kommt die Lösung aus dem Vokabel-Store
/// (Wort des Eintrags), zentral pflegbar; das inline <paramref name="Answer"/> bleibt Fallback für
/// Lücken ohne Store-Bezug. So lässt sich ein Lückentext aus dem gepflegten Wortschatz bauen.
/// </summary>
public record Gap(int Index, string Answer, List<string>? Alternatives = null, string? VocabKey = null);

/// <summary>Aufsatz: Schreibauftrag + Rahmenbedingungen.</summary>
public class EssayConfig
{
    public string Prompt { get; set; } = "";
    public int? MinWords { get; set; }
    public int? MaxWords { get; set; }
    /// <summary>Optionale Bewertungskriterien.</summary>
    public List<RubricCriterion>? Rubric { get; set; }
}
public record RubricCriterion(string Criterion, int MaxScore);

/// <summary>Hörverständnis: Audioquelle + Verständnisfragen.</summary>
public class ListeningConfig
{
    /// <summary>URL / Referenz auf die Audiodatei.</summary>
    public string AudioUrl { get; set; } = "";
    public string? Transcript { get; set; }
    public List<Question> Questions { get; set; } = new();
}

/// <summary>Grammatik: Umformungs- / Regelaufgaben.</summary>
public class GrammarConfig
{
    public string? Instruction { get; set; }
    public List<GrammarTask> Tasks { get; set; } = new();
}
public record GrammarTask(string Prompt, string Answer, string? RuleHint = null);

/// <summary>Zuordnung: Paare links ↔ rechts.</summary>
public class MatchingConfig
{
    public string? Instruction { get; set; }
    public List<MatchPair> Pairs { get; set; } = new();
}
public record MatchPair(string Left, string Right);

/// <summary>Übersetzung: Sätze mit erwarteter Übersetzung.</summary>
public class TranslationConfig
{
    public string SourceLang { get; set; } = "";
    public string TargetLang { get; set; } = "";
    public List<TranslationItem> Items { get; set; } = new();
}
/// <summary>
/// Ein Übersetzungspaar. <paramref name="VocabularyId"/> verweist auf den zugehörigen Store-Eintrag
/// (beim Speichern automatisch angelegt); <paramref name="Self"/> ist der abgeleitete, nur lesend gefüllte Link.
/// </summary>
public record TranslationItem(string Source, string Target, List<string>? Alternatives = null,
    int? VocabularyId = null, [property: JsonPropertyName("_self")] string? Self = null);

/// <summary>Feste Rechenaufgaben: manuell gepflegte Liste aus Ausdruck und erwarteter Lösung.</summary>
public class ArithmeticConfig
{
    public List<ArithmeticProblem> Problems { get; set; } = new();
}
/// <summary>
/// Eine feste Rechenaufgabe. <see cref="Tolerance"/> erlaubt Rundungsspielraum
/// (0 = exakte Lösung erwartet), z. B. bei nicht glatt aufgehenden Divisionen.
/// </summary>
public record ArithmeticProblem(string Prompt, decimal Answer, decimal Tolerance = 0m);

/// <summary>Rechenart einer erzeugten Aufgabe.</summary>
public enum ArithmeticOperation { Addition, Subtraction, Multiplication, Division }

/// <summary>
/// Regeln für zufällig erzeugte Rechenaufgaben. Gespeichert werden nur die Regeln;
/// die konkreten Aufgaben erzeugt der Server pro Abruf (siehe ArithmeticDrillController.Generate).
/// </summary>
public class ArithmeticDrillConfig
{
    /// <summary>Erlaubte Rechenarten; pro Aufgabe wird zufällig eine davon gewählt.</summary>
    public List<ArithmeticOperation> Operations { get; set; } = new() { ArithmeticOperation.Addition };
    /// <summary>Kleinster Operand (inklusive).</summary>
    public int MinOperand { get; set; } = 1;
    /// <summary>Größter Operand (inklusive).</summary>
    public int MaxOperand { get; set; } = 10;
    /// <summary>Anzahl der pro Durchlauf erzeugten Aufgaben.</summary>
    public int ProblemCount { get; set; } = 10;
    /// <summary>Ob Subtraktionen ein negatives Ergebnis liefern dürfen.</summary>
    public bool AllowNegativeResults { get; set; } = false;
    /// <summary>Ob Divisionen ohne Rest aufgehen müssen (ganzzahliges Ergebnis).</summary>
    public bool DivisionMustBeWhole { get; set; } = true;
    /// <summary>Optionaler fester Seed für reproduzierbare Durchläufe (leer = echter Zufall).</summary>
    public int? Seed { get; set; }
}

/// <summary>
/// Auswendig zu lernende Liste (z. B. „die 16 Bundesländer"). Bei der Auswertung zählt
/// standardmäßig nur, ob die Einträge genannt wurden – die Reihenfolge nur, wenn <see cref="Ordered"/> gesetzt ist.
/// </summary>
public class ListConfig
{
    /// <summary>Optionaler Auftrag/Frage (z. B. „Nenne alle 16 Bundesländer").</summary>
    public string? Instruction { get; set; }
    /// <summary>Ob die Reihenfolge für die Bewertung zählt.</summary>
    public bool Ordered { get; set; }
    public List<ListEntry> Items { get; set; } = new();
}
/// <summary>Ein Listeneintrag; <paramref name="Alternatives"/> erlaubt zulässige Synonyme/Schreibweisen.</summary>
public record ListEntry(string Value, List<string>? Alternatives = null);

/// <summary>
/// Birkenbihl-Methode: ein Text in der zu lernenden Sprache wird grammatik-unabhängig
/// Wort für Wort in die Muttersprache dekodiert. Zu jedem Satz gehört zusätzlich eine
/// natürliche, grammatikalisch korrekte Übersetzung, damit der Sinn klar wird. Gelernt wird
/// durch Lesen/Hören der Dekodierung – die Methode verzichtet bewusst auf aktives Abfragen
/// (deshalb hat der Typ kein <c>/check</c>; die Punkte gibt es fürs Durcharbeiten).
/// </summary>
public class BirkenbihlConfig
{
    /// <summary>Sprachcode der Lernsprache – die Sätze stehen in ihr (z. B. „en"). Muss zum Vokabelspeicher passen.</summary>
    public string LearningLang { get; set; } = "";
    /// <summary>Sprachcode der Muttersprache (Glossen + Übersetzung, z. B. „de"). Muss zum Vokabelspeicher passen.</summary>
    public string NativeLang { get; set; } = "";
    /// <summary>Nächste zu vergebende <see cref="BirkenbihlSentence.SentenceId"/> (monoton, kein Recycling gelöschter IDs).</summary>
    public int NextSentenceId { get; set; }
    /// <summary>
    /// Nächste zu vergebende <see cref="WordPair.WordId"/>. Bewusst <b>übungsweit</b> eindeutig (nicht pro Satz),
    /// damit der Austausch-Endpunkt <c>.../words/{wordId}</c> ohne Satz-Segment ein Wort eindeutig trifft.
    /// </summary>
    public int NextWordId { get; set; }
    /// <summary>Die Sätze des Textes in Lesereihenfolge.</summary>
    public List<BirkenbihlSentence> Sentences { get; set; } = new();
}

/// <summary>
/// Ein Satz der Birkenbihl-Übung: der Originalsatz in der Lernsprache (<paramref name="LearningSentence"/>),
/// seine positionsgenaue Wort-für-Wort-Dekodierung (<paramref name="Decoding"/>) und eine natürliche,
/// grammatikalisch korrekte Übersetzung (<paramref name="NaturalTranslation"/>).
/// </summary>
public record BirkenbihlSentence(int SentenceId, string LearningSentence, string NaturalTranslation, List<WordPair> Decoding);

/// <summary>
/// Ein Wort-Tuple der Dekodierung: <paramref name="LearningWord"/> der Lernsprache → wörtliche muttersprachliche
/// Glosse <paramref name="Gloss"/>. <paramref name="Gloss"/>/<paramref name="VocabularyId"/> sind <c>null</c>, wenn
/// das Wort (noch) nicht im Vokabelspeicher liegt und keine manuelle Glosse gesetzt wurde. <paramref name="WordId"/>
/// ist übungsweit eindeutig (siehe <see cref="BirkenbihlConfig.NextWordId"/>).
/// </summary>
public record WordPair(int WordId, string LearningWord, string? Gloss, int? VocabularyId,
    [property: JsonPropertyName("_self")] string? Self = null);
