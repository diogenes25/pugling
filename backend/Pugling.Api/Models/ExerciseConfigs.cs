namespace Pugling.Api.Models;

// Config-Schemas je Übungstyp.
// Werden als JSON in Exercise.ConfigJson gespeichert, im API aber typisiert
// als Teil von ExercisePayload<TConfig> / ExerciseResponse<TConfig> übertragen.
// Jeder Typ = eigener Pfad + eigenes Swagger-Schema.

/// <summary>Frage mit optionalen Antwortmöglichkeiten (leer = Freitext).</summary>
public record Question(string Prompt, List<string>? Choices, string Answer);

/// <summary>Vokabelübung.</summary>
public class VocabularyConfig
{
    /// <summary>Abfragerichtung: front-to-back | back-to-front | both.</summary>
    public string Direction { get; set; } = "front-to-back";
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
