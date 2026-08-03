namespace Pugling.Contracts.Creator;

// Contract of the preview mode ("try it out"): the supervisor/teacher plays a catalog exercise through
// without side effects. Deliberately its own records next to the child's play view - preview always reveals the answer.

/// <summary>
/// A problem presented in test mode. <c>Reveal</c> carries the revealed solution for self-assessment
/// (<c>null</c> for typed stages); <c>AnswerLength</c> is only set for vocabulary letter boxes.
/// <c>Passage</c> is the exercise-wide material the question is about (reading text, grammar instruction) –
/// the same field the child's card carries, because a preview that shows less than the child gets is a
/// reassurance rather than a check.
/// </summary>
public record PreviewItem(int ItemIndex, string Prompt, int? GapIndex, string? Hint, int? AnswerLength, string? Reveal,
    IReadOnlyList<string>? Choices, string? AudioUrl, string? Passage = null);

/// <summary>
/// The playable state of an exercise in test mode: type, chosen stage, whether typed, the problems and
/// – for trying out – the query forms toggleable for this exercise type (<see cref="Stages"/>).
/// </summary>
public record PreviewData(string Type, int Stage, bool Typed, IReadOnlyList<StageOption> Stages, IReadOnlyList<PreviewItem> Items);

/// <summary>An answer from the supervisor: typed (<paramref name="GivenAnswer"/>) or self-assessment (<paramref name="WasKnown"/>).</summary>
public record PreviewAnswer(int ItemIndex, string? GivenAnswer, bool? WasKnown);

/// <summary>Individual evaluation including the expected solution (in test mode the solution is always disclosed).</summary>
public record PreviewItemOutcome(int ItemIndex, string Prompt, string Expected, string? GivenAnswer, bool WasCorrect);

/// <summary>Overall result of a test-mode run.</summary>
public record PreviewResult(int Total, int Correct, int ScorePercent, IReadOnlyList<PreviewItemOutcome> Items);

/// <summary>
/// Body of the test-mode check: the submitted answers and – if toggled – the query form.
/// Deliberately not named <c>CheckDto</c>: that name is used by the stateless catalog check of the exercise type.
/// </summary>
public record PreviewCheckDto(List<PreviewAnswer> Answers, int? Stage = null);
