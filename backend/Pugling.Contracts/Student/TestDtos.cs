namespace Pugling.Contracts.Student;

// Vertrag des Abschlusstests einer Lehrplan-Position. Strikt server-getrieben (Klausur-Modus):
// eine Frage nach der anderen über den Attempt-Cursor, kein Zurück, Feedback erst beim Abschluss.

/// <summary>An exam question – without the solution, except at stages that reveal it by design.</summary>
public record TestItem(int ItemIndex, string Prompt, int Stage, string? Reveal, int? AnswerLength, string? Hint,
    IReadOnlyList<string>? Choices, string? AudioUrl, string? ImageUrl = null, string? ImageAlt = null);

/// <summary>
/// Response of the test start. Class-test mode is strictly server-driven: <b>no</b> questions are sent
/// in bulk, only the metadata. The client fetches the questions one at a time via the <c>next</c> endpoint (no going back).
/// </summary>
public record AttemptResponse(int AttemptId, int PlanId, int PositionId, DateOnly Day, int Stage, int TotalItems);

/// <summary>Start payload of the class test. <c>Day</c> only for backfilling (supervisor); otherwise today.</summary>
public record StartTestDto(int? Stage, DateOnly? Day);

/// <summary>The next exam question (or <c>Done</c>), server-driven via the attempt cursor – without the solution.</summary>
public record TestNextResponse(TestItem? Item, bool Done, int Cursor, int Total);

/// <summary>Confirmation of a submitted exam answer – deliberately WITHOUT correctness (feedback only at completion).</summary>
public record AnswerAck(bool Done, int Cursor, int Total);

/// <summary>A single result within the completed attempt.</summary>
public record ItemResultDto(int ItemIndex, string? GivenAnswer, bool WasCorrect, int HintsUsed);

/// <summary>A test attempt with all individual results.</summary>
public record AttemptDetail(int Id, int PlanId, int PositionId, DateOnly Day, int Stage, DateTime StartedAt,
    DateTime? CompletedAt, int TotalItems, int CorrectItems, int ScorePercent, bool Passed,
    IReadOnlyList<ItemResultDto> Results);

/// <summary>A submitted exam answer: typed (<paramref name="GivenAnswer"/>) or self-assessed (<paramref name="WasKnown"/>).</summary>
public record AnswerDto(int ItemIndex, string? GivenAnswer, bool? WasKnown);

/// <summary>Bulk submission of the test (alternative to individual submission via the <c>answer</c> endpoint).</summary>
public record SubmitDto(List<AnswerDto>? Answers);

/// <summary>Evaluation of a single exam question – here the solution is disclosed.</summary>
public record ItemOutcome(int ItemIndex, string Prompt, string Expected, string? GivenAnswer, bool WasCorrect);

/// <summary>Overall result of the class test incl. pass threshold.</summary>
public record SubmitResponse(int AttemptId, int Stage, int TotalItems, int CorrectItems,
    int ScorePercent, bool Passed, int PassPercent, IReadOnlyList<ItemOutcome> Items);
