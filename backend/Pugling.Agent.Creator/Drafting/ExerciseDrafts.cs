namespace Pugling.Agent.Creator.Drafting;

// Die Entwurfs-Formen, die das Sprachmodell füllt. Bewusst NICHT die Vertrags-Configs selbst:
// die tragen technische Felder (Ids, Store-Verweise, HATEOAS-Links, Zähler), die kein Modell
// erfinden darf. Die Entwürfe sind flach und minimal – je kleiner das JSON-Schema, desto
// zuverlässiger liefern lokale Modelle gültige Antworten. Die Übersetzung Entwurf → Config
// macht die jeweilige Strategie, nicht das Modell.

/// <summary>Entwurf einer Vokabelübung: Titel plus die Wortpaare.</summary>
/// <param name="Title">Sprechender deutscher Titel der Übung.</param>
/// <param name="Items">Die Wortpaare in Lernreihenfolge.</param>
public sealed record VocabularyDraft(string Title, List<VocabularyDraftItem> Items);

/// <summary>Ein Wortpaar.</summary>
/// <param name="Front">Wort in der Lernsprache.</param>
/// <param name="Back">Übersetzung in der Muttersprache.</param>
/// <param name="Hint">Optionaler kurzer Merkhinweis (Eselsbrücke, Kontext).</param>
public sealed record VocabularyDraftItem(string Front, string Back, string? Hint);

/// <summary>Entwurf eines Lückentexts.</summary>
/// <param name="Title">Sprechender deutscher Titel der Übung.</param>
/// <param name="Text">Fließtext mit Platzhaltern <c>{{1}}</c>, <c>{{2}}</c> … an den Lückenstellen.</param>
/// <param name="Gaps">Zu jedem Platzhalter genau eine Lücke mit derselben Nummer.</param>
/// <param name="WordBank">Auswahlwörter (alle Lösungen plus einige plausible Ablenker).</param>
public sealed record ClozeDraft(string Title, string Text, List<ClozeGapDraft> Gaps, List<string>? WordBank);

/// <summary>Eine Lücke des Lückentexts.</summary>
/// <param name="Index">Nummer des Platzhalters im Text (<c>{{1}}</c> → 1).</param>
/// <param name="Answer">Die einzig richtige Lösung.</param>
/// <param name="Alternatives">Zusätzlich akzeptierte Schreibweisen.</param>
public sealed record ClozeGapDraft(int Index, string Answer, List<string>? Alternatives);

/// <summary>Entwurf einer Übersetzungsübung.</summary>
/// <param name="Title">Sprechender deutscher Titel der Übung.</param>
/// <param name="Items">Die Satzpaare.</param>
public sealed record TranslationDraft(string Title, List<TranslationDraftItem> Items);

/// <summary>Ein Satzpaar.</summary>
/// <param name="Source">Satz in der Ausgangssprache.</param>
/// <param name="Target">Erwartete Übersetzung.</param>
/// <param name="Alternatives">Weitere zulässige Übersetzungen.</param>
public sealed record TranslationDraftItem(string Source, string Target, List<string>? Alternatives);

/// <summary>Entwurf einer Grammatikübung.</summary>
/// <param name="Title">Sprechender deutscher Titel der Übung.</param>
/// <param name="Instruction">Deutsche Arbeitsanweisung für das Kind.</param>
/// <param name="Tasks">Die Einzelaufgaben.</param>
public sealed record GrammarDraft(string Title, string Instruction, List<GrammarDraftTask> Tasks);

/// <summary>Eine Grammatikaufgabe.</summary>
/// <param name="Prompt">Aufgabenstellung/Ausgangssatz.</param>
/// <param name="Answer">Die erwartete Lösung.</param>
/// <param name="RuleHint">Kurzer Hinweis auf die zugrunde liegende Regel.</param>
public sealed record GrammarDraftTask(string Prompt, string Answer, string? RuleHint);
