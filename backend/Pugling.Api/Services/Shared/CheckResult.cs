namespace Pugling.Api.Services.Shared;

/// <summary>Eine vom Kind abgegebene Antwort, positionsbezogen (Index in der jeweiligen Aufgabenliste).</summary>
public record GivenAnswer(int Index, string? Value);

/// <summary>Auswertung einer einzelnen Position.</summary>
public record ItemCheck(int Index, string Prompt, string? Given, string Expected, bool Correct);

/// <summary>Gesamtergebnis einer Auswertung: Trefferzahl, Prozent und Einzelergebnisse.</summary>
public record CheckResult(int Total, int Correct, int ScorePercent, IReadOnlyList<ItemCheck> Items);
