namespace Pugling.Api.Models;

// Config-Schemas je Übungstyp.
// Werden als JSON in Exercise.ConfigJson gespeichert, im API aber typisiert
// als Teil von ExercisePayload<TConfig> / ExerciseResponse<TConfig> übertragen.
// Jeder Typ = eigener Pfad + eigenes Swagger-Schema.

/// <summary>Frage mit optionalen Antwortmöglichkeiten (leer = Freitext).</summary>
public record Question(string Prompt, List<string>? Choices, string Answer);

/// <summary>
/// Vokabelübung. Bevorzugt verweist sie über <see cref="Refs"/> (Store-Keys) auf den Vokabel-Komplextyp,
/// damit dieselbe Vokabel über mehrere Übungen hinweg verknüpft und zentral pflegbar ist. <see cref="Items"/>
/// (inline) bleibt für Abwärtskompatibilität (Alt-/Seed-Übungen); der Resolver liest beide Formen.
/// </summary>
public class VocabularyConfig
{
    /// <summary>Abfragerichtung: front-to-back | back-to-front | both.</summary>
    public string Direction { get; set; } = "front-to-back";
    /// <summary>Referenzen auf Vokabel-Store-Einträge per Key (bevorzugt).</summary>
    public List<string>? Refs { get; set; }
    /// <summary>Inline-Vokabeln (Legacy/ohne Store-Bezug).</summary>
    public List<VocabItem> Items { get; set; } = new();
}
public record VocabItem(string Front, string Back, string? Hint = null);

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
public record Gap(int Index, string Answer, List<string>? Alternatives = null);

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
public record TranslationItem(string Source, string Target, List<string>? Alternatives = null);

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
    /// <summary>Sprache, die gelernt wird – die Sätze stehen in ihr (z. B. „Englisch").</summary>
    public string LearningLang { get; set; } = "";
    /// <summary>Muttersprache der Wort-für-Wort-Dekodierung und der Übersetzung (z. B. „Deutsch").</summary>
    public string NativeLang { get; set; } = "";
    /// <summary>Die Sätze des Textes in Lesereihenfolge.</summary>
    public List<BirkenbihlSentence> Sentences { get; set; } = new();
}

/// <summary>
/// Ein Satz der Birkenbihl-Übung: der Originalsatz in der Lernsprache, seine positionsgenaue
/// Wort-für-Wort-Dekodierung (<paramref name="Decoding"/>) und eine natürliche, grammatikalisch
/// korrekte Übersetzung (<paramref name="NaturalTranslation"/>).
/// </summary>
public record BirkenbihlSentence(string Text, List<WordPair> Decoding, string NaturalTranslation);

/// <summary>Ein Wort-Tuple der Dekodierung: <paramref name="Word"/> der Lernsprache → wörtliche Übersetzung <paramref name="Literal"/>.</summary>
public record WordPair(string Word, string Literal);
