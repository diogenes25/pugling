namespace Pugling.Contracts.Shared;

// Ebenen-übergreifender Vertrag der Antwort-Auswertung: dieselbe Form beim zustandslosen
// Katalog-Check (Creator) wie beim server-autoritativen Abschlusstest (Student).

/// <summary>Eine vom Kind abgegebene Antwort, positionsbezogen (Index in der jeweiligen Aufgabenliste).</summary>
public record GivenAnswer(int Index, string? Value);

/// <summary>Auswertung einer einzelnen Position.</summary>
public record ItemCheck(int Index, string Prompt, string? Given, string Expected, bool Correct);

/// <summary>Gesamtergebnis einer Auswertung: Trefferzahl, Prozent und Einzelergebnisse.</summary>
public record CheckResult(int Total, int Correct, int ScorePercent, IReadOnlyList<ItemCheck> Items);

/// <summary>Ein erzeugter Rechenausdruck samt Lösung.</summary>
public record GeneratedProblem(string Prompt, decimal Answer);
