namespace Pugling.Contracts.Student;

// Contract of the final test of a study plan position. Strictly server-driven (class-test mode):
// one question after another through the attempt cursor, no going back, feedback only on completion.

/// <summary>
/// An exam question – without the solution, except at stages that reveal it by design. Deliberately
/// <b>without</b> an image: the exam shows none, and a field that is always null is the kind of silent lie
/// the API otherwise rejects with <c>unknown_field</c>.
/// <para>
/// <c>GapIndex</c> is no such lie: it names the asked placeholder <c>{{n}}</c> of <c>Prompt</c> and is null
/// only for types whose atoms already stand on their own. The exam needs it just as much as the practice
/// card – it is where a child gets stuck for good, because there is no going back.
/// </para>
/// <para>
/// <c>AnyOrder</c> likewise: it says that any entry not yet named counts (a set instead of a sequence). The
/// exam is where that matters most – there is no second try, so a card whose rule the child has to guess is
/// lost for good.
/// </para>
/// <para>
/// <c>Type</c> is the exercise type key, as the practice card has carried all along: the renderer needs it to
/// tell an <b>ordered</b> list (where "entry 8" is the address of the card) from a vocabulary card, and both
/// arrive with the same fields otherwise.
/// </para>
/// <para>
/// <c>RevealAlternatives</c> lists the equally valid answers beside <c>Reveal</c> and is set exactly where
/// <c>Reveal</c> is – see <see cref="PracticeCard"/>.
/// </para>
/// </summary>
public record TestItem(int ItemIndex, string? Prompt, int Stage, string? Reveal, int? AnswerLength, string? Hint,
    IReadOnlyList<string>? Choices, string? AudioUrl, int? GapIndex = null, string? Passage = null,
    bool AnyOrder = false, string? Type = null, IReadOnlyList<string>? RevealAlternatives = null);

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

/// <summary>
/// Evaluation of a single exam question – here the solution is disclosed. Carries <c>GapIndex</c> for the
/// same reason the question does: without it the review screen lists one identical line per gap, which is
/// the very confusion the exam just resolved.
/// </summary>
public record ItemOutcome(int ItemIndex, string Prompt, string Expected, string? GivenAnswer, bool WasCorrect,
    int? GapIndex = null);

/// <summary>
/// Overall result of the class test incl. pass threshold.
/// <para>
/// <see cref="WrongMentions"/> only fills up for the set-graded types (an unordered list): an answer that
/// matched no open entry belongs to no <see cref="ItemOutcome"/>, because the outcomes are keyed by entry
/// there. Without this list the review screen would show what the child <i>forgot</i> but silently drop what
/// it actually typed.
/// </para>
/// </summary>
public record SubmitResponse(int AttemptId, int Stage, int TotalItems, int CorrectItems,
    int ScorePercent, bool Passed, int PassPercent, IReadOnlyList<ItemOutcome> Items,
    IReadOnlyList<string>? WrongMentions = null);
