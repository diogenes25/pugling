namespace Pugling.Contracts.Student;

// Vertrag des Abschlusstests einer Lehrplan-Position. Strikt server-getrieben (Klausur-Modus):
// eine Frage nach der anderen über den Attempt-Cursor, kein Zurück, Feedback erst beim Abschluss.

/// <summary>Eine Prüfungsfrage – ohne Lösung, außer bei Stufen, die sie per Design aufdecken.</summary>
public record TestItem(int ItemIndex, string Prompt, int Stage, string? Reveal, int? AnswerLength, string? Hint,
    IReadOnlyList<string>? Choices, string? AudioUrl);

/// <summary>
/// Antwort des Test-Starts. Der Klausur-Modus ist strikt server-getrieben: es kommen <b>keine</b> Aufgaben
/// im Bulk, nur die Metadaten. Die Fragen holt der Client einzeln über den <c>next</c>-Endpunkt (kein Zurück).
/// </summary>
public record AttemptResponse(int AttemptId, int PlanId, int PositionId, DateOnly Day, int Stage, int TotalItems);

/// <summary>Start-Payload des Abschlusstests. <c>Day</c> nur zum Nachtragen (Vater); sonst heute.</summary>
public record StartTestDto(int? Stage, DateOnly? Day);

/// <summary>Die nächste Prüfungsfrage (oder <c>Done</c>), server-geführt über den Attempt-Cursor – ohne Lösung.</summary>
public record TestNextResponse(TestItem? Item, bool Done, int Cursor, int Total);

/// <summary>Bestätigung einer abgegebenen Prüfungsantwort – bewusst OHNE Korrektheit (Feedback erst beim Abschluss).</summary>
public record AnswerAck(bool Done, int Cursor, int Total);

/// <summary>Ein Einzelergebnis im abgeschlossenen Versuch.</summary>
public record ItemResultDto(int ItemIndex, string? GivenAnswer, bool WasCorrect, int HintsUsed);

/// <summary>Ein Testversuch mit allen Einzelergebnissen.</summary>
public record AttemptDetail(int Id, int PlanId, int PositionId, DateOnly Day, int Stage, DateTime StartedAt,
    DateTime? CompletedAt, int TotalItems, int CorrectItems, int ScorePercent, bool Passed,
    IReadOnlyList<ItemResultDto> Results);

/// <summary>Eine abgegebene Prüfungsantwort: getippt (<paramref name="GivenAnswer"/>) oder Selbsteinschätzung (<paramref name="WasKnown"/>).</summary>
public record AnswerDto(int ItemIndex, string? GivenAnswer, bool? WasKnown);

/// <summary>Sammel-Abgabe des Tests (Alternative zur Einzel-Abgabe über den <c>answer</c>-Endpunkt).</summary>
public record SubmitDto(List<AnswerDto>? Answers);

/// <summary>Auswertung einer einzelnen Prüfungsfrage – hier wird die Lösung offengelegt.</summary>
public record ItemOutcome(int ItemIndex, string Prompt, string Expected, string? GivenAnswer, bool WasCorrect);

/// <summary>Gesamtergebnis des Abschlusstests inkl. Bestehensgrenze.</summary>
public record SubmitResponse(int AttemptId, int Stage, int TotalItems, int CorrectItems,
    int ScorePercent, bool Passed, int PassPercent, IReadOnlyList<ItemOutcome> Items);
