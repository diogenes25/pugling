namespace Pugling.Contracts.Creator;

// Vertrag des Testmodus („Ausprobieren"): Der Vater/Lehrer spielt eine Katalog-Übung nebenwirkungsfrei
// durch. Bewusst eigene Records neben der Kind-Spielsicht – der Testmodus deckt die Lösung immer auf.

/// <summary>
/// Eine im Testmodus vorgelegte Aufgabe. <c>Reveal</c> trägt bei Selbsteinschätzung die aufgedeckte Lösung
/// (bei getippten Stufen <c>null</c>); <c>AnswerLength</c> ist nur bei Vokabel-Buchstabenkästchen gesetzt.
/// </summary>
public record PreviewItem(int ItemIndex, string Prompt, int? GapIndex, string? Hint, int? AnswerLength, string? Reveal,
    IReadOnlyList<string>? Choices, string? AudioUrl);

/// <summary>
/// Der spielbare Zustand einer Übung im Testmodus: Typ, gewählte Stufe, ob getippt wird, die Aufgaben und
/// – zum Durchprobieren – die für diesen Übungstyp umschaltbaren Abfrageformen (<see cref="Stages"/>).
/// </summary>
public record PreviewData(string Type, int Stage, bool Typed, IReadOnlyList<StageOption> Stages, IReadOnlyList<PreviewItem> Items);

/// <summary>Eine Antwort des Vaters: getippt (<paramref name="GivenAnswer"/>) oder Selbsteinschätzung (<paramref name="WasKnown"/>).</summary>
public record PreviewAnswer(int ItemIndex, string? GivenAnswer, bool? WasKnown);

/// <summary>Einzelauswertung inklusive erwarteter Lösung (im Testmodus wird die Lösung immer offengelegt).</summary>
public record PreviewItemOutcome(int ItemIndex, string Prompt, string Expected, string? GivenAnswer, bool WasCorrect);

/// <summary>Gesamtergebnis eines Testmodus-Durchlaufs.</summary>
public record PreviewResult(int Total, int Correct, int ScorePercent, IReadOnlyList<PreviewItemOutcome> Items);

/// <summary>
/// Body des Testmodus-Checks: die abgegebenen Antworten und – falls umgeschaltet – die Abfrageform.
/// Heißt bewusst nicht <c>CheckDto</c>: den Namen trägt der zustandslose Katalog-Check des Übungstyps.
/// </summary>
public record PreviewCheckDto(List<PreviewAnswer> Answers, int? Stage = null);
