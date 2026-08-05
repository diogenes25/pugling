namespace Pugling.Contracts.Student;

// Contract of the practice loop (Leitner) of one study plan position. Server-authoritative: the server
// picks the card, grades the answer and drives the cursor - the frontend only renders.

/// <summary>A running or finished practice session on a position.</summary>
public record SessionResponse(int Id, int PlanId, int PositionId, DateOnly Day,
    DateTime StartedAt, DateTime? EndedAt, int ActiveSeconds, int ReviewCount,
    PlayMode Mode, int Cursor, int Total);

/// <summary>
/// Start payload of a practice session. <paramref name="Mode"/> selects the playback mode (default
/// <see cref="PlayMode.Lern"/> = server-driven with cursor; <see cref="PlayMode.Info"/> = free practice without
/// feedback). <paramref name="Day"/> only for backfilling (supervisor).
/// </summary>
public record StartPracticeDto(DateOnly? Day, PlayMode Mode = PlayMode.Lern);

/// <summary>Reports (active) practice seconds; the server caps them per heartbeat (anti time-cheat).</summary>
public record HeartbeatDto(int Seconds, bool Active);

/// <summary>
/// A practice card – deliberately WITHOUT the solution, except at display/self-assessment stages that
/// reveal the solution by design (the server grades, never the frontend).
/// <c>ImageUrl</c>/<c>ImageAlt</c> carry the image <b>selected for this child</b>; they are only set at
/// stages where a motif cannot give away the solution, and are absent otherwise.
/// <para>
/// <c>GapIndex</c> names the placeholder <c>{{n}}</c> of <c>Prompt</c> that is being asked. It is set only
/// by types whose atoms share one text (the cloze); without it two gaps of the same text would arrive as
/// byte-identical cards and the child could not tell which one to fill.
/// </para>
/// <para>
/// <c>Passage</c> is what the question is <b>about</b> and belongs to the whole exercise – the reading text,
/// the instruction covering all grammar tasks. Deliberately its own field rather than folded into
/// <c>Prompt</c>: the prompt is the question and is reused as such in every evaluation line.
/// </para>
/// <para>
/// <c>Prompt</c> is nullable because at the vocabulary listening stage the recording <b>is</b> the question –
/// showing the word next to it would turn "listen, then type" into a reading task. That is an anti-cheat
/// decision, so the server makes it; the frontend renders whatever arrives.
/// </para>
/// <para>
/// <c>AnyOrder</c> says that <b>any</b> answer not yet named counts, because the exercise is a set rather
/// than a sequence (an unordered list). The child has to be told: the <c>Ordered</c> setting lives in the
/// exercise config and never reaches it, so an ordered and an unordered list would arrive as identical cards
/// while the grading works the opposite way.
/// </para>
/// <para>
/// <c>RevealAlternatives</c> carries the <b>equally valid</b> answers beside <c>Reveal</c> and is set exactly
/// where <c>Reveal</c> is. Its own field rather than a merged comma list, so the interface can separate "the
/// answer" from "also correct": whoever thought of an alternative must not grade themselves wrong.
/// </para>
/// <para>
/// <c>Decoding</c> is the word-for-word decoding of the sentence (Birkenbihl) – the method itself, so it belongs
/// on the <b>front</b> of the card next to <c>Prompt</c>, not behind the reveal. <c>null</c> for every other type.
/// </para>
/// <para>
/// <c>DisplayOnly</c> (B-96) marks a free display stage ("getting acquainted"): both sides are shown at
/// once, there is no self-assessment and no Leitner movement. Distinct from typed-ness – the frontend must
/// not infer it from <c>Reveal</c> being set, because self-assessment stages set it too.
/// </para>
/// <para>
/// <c>AnswerPattern</c> (B-66) is the letter-box mask: an underscore per letter/digit to type, every other
/// character (space, hyphen, …) kept literally – already fixed by the solution, so the child never types
/// it. <c>null</c> outside the letter-box stage, same as <c>AnswerLength</c>.
/// </para>
/// </summary>
public record PracticeCard(int ItemIndex, int Stage, string Type, string? Prompt,
    string? Hint, int? AnswerLength, string? Reveal, IReadOnlyList<string>? Choices, string? AudioUrl,
    string? ImageUrl = null, string? ImageAlt = null, int? GapIndex = null, string? Passage = null,
    bool AnyOrder = false, IReadOnlyList<string>? RevealAlternatives = null,
    IReadOnlyList<WordPair>? Decoding = null, bool DisplayOnly = false, string? AnswerPattern = null);

/// <summary>The next card in learn mode (or <c>Done</c>), server-driven via the session cursor.</summary>
public record NextResponse(PracticeCard? Card, bool Done, int Cursor, int Total);

/// <summary>
/// The child's answer to a practice card. <paramref name="ItemIndex"/> addresses the content atom
/// in the exercise. Typed stages supply <paramref name="GivenAnswer"/>, display/self-assessment
/// stages supply <paramref name="WasKnown"/>. The server enforces the stage; it grades – never the frontend.
/// </summary>
public record ReviewDto(int ItemIndex, string? GivenAnswer, bool? WasKnown);

/// <summary>
/// Result of a Leitner review (graded server-side) incl. bonuses for feedback. <see cref="Next"/>
/// carries the next card directly in learn mode (no separate roundtrip needed); <see cref="Done"/> signals
/// the end of the run. For cards that are not graded (not due / already graded today / not Leitner-based),
/// the points fields are 0, but grading and cursor still advance.
/// <para>
/// <see cref="Expected"/> is nullable for the set-graded types: an answer that matches no open entry hits no
/// atom at all, and naming one particular entry as "the" solution would be arbitrary while a dozen are still
/// open – it would also give away an entry the child is about to be asked for. <see cref="Box"/> is 0 and
/// <see cref="DueOn"/> null in that case for the same reason: nothing was reviewed, so nothing moved.
/// </para>
/// </summary>
public record ReviewOutcome(bool WasCorrect, string? Expected, int Awarded, int Box,
    DateOnly? DueOn, int Combo, int ComboBonus, int SpeedBonus, PracticeCard? Next, bool Done);
