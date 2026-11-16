namespace Pugling.Contracts.Student;

// Contract of the practice loop (Leitner) of one study plan position. Server-authoritative: the server
// picks the card, grades the answer and drives the cursor - the frontend only renders.

/// <summary>
/// A running or finished practice session on a position. <paramref name="Testable"/> mirrors
/// <c>PositionStatus.Testable</c> (B-114) for this session's day: false on a free display stage, so a
/// client offering "continue to the test" after the round (or when nothing is due) can gate on it instead
/// of sending the child into the exam's <c>stage_not_testable</c> dead end.
///
/// <paramref name="TestAttemptsLeft"/> (B-522) is the number of exam attempts still startable on the session's
/// day - the same count that locks the exam start with <c>test_attempts_exhausted</c>; an open attempt counts as
/// startable (it is resumed). <c>Testable</c> says whether the day's stage has a test at all, this says whether
/// the child may still take it today. A token without the student role is exempt from the cap and always gets the
/// daily maximum. It is <c>null</c> only in the heartbeat's answer: the heartbeat runs every few seconds and its
/// answer is not a screen state, so it skips the count (start, read and end always carry it).
/// </summary>
public record SessionResponse(int Id, int PlanId, int PositionId, DateOnly Day,
    DateTime StartedAt, DateTime? EndedAt, int ActiveSeconds, int ReviewCount,
    PlayMode Mode, int Cursor, int Total, bool Testable, int? TestAttemptsLeft);

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
///
/// <c>GapIndex</c> names the placeholder <c>{{n}}</c> of <c>Prompt</c> that is being asked. It is set only
/// by types whose atoms share one text (the cloze); without it two gaps of the same text would arrive as
/// byte-identical cards and the child could not tell which one to fill.
///
/// <c>Passage</c> is what the question is <b>about</b> and belongs to the whole exercise – the reading text,
/// the instruction covering all grammar tasks. Deliberately its own field rather than folded into
/// <c>Prompt</c>: the prompt is the question and is reused as such in every evaluation line.
///
/// <c>Prompt</c> is nullable because at the vocabulary listening stage the recording <b>is</b> the question –
/// showing the word next to it would turn "listen, then type" into a reading task. That is an anti-cheat
/// decision, so the server makes it; the frontend renders whatever arrives.
///
/// <c>AnyOrder</c> says that <b>any</b> answer not yet named counts, because the exercise is a set rather
/// than a sequence (an unordered list). The child has to be told: the <c>Ordered</c> setting lives in the
/// exercise config and never reaches it, so an ordered and an unordered list would arrive as identical cards
/// while the grading works the opposite way.
///
/// <c>RevealAlternatives</c> carries the <b>equally valid</b> answers beside <c>Reveal</c> and is set exactly
/// where <c>Reveal</c> is. Its own field rather than a merged comma list, so the interface can separate "the
/// answer" from "also correct": whoever thought of an alternative must not grade themselves wrong.
///
/// <c>Decoding</c> is the word-for-word decoding of the sentence (Birkenbihl) – the method itself, so it belongs
/// on the <b>front</b> of the card next to <c>Prompt</c>, not behind the reveal. <c>null</c> for every other type.
///
/// <c>DisplayOnly</c> (B-96) marks a free display stage ("getting acquainted"): both sides are shown at
/// once, there is no self-assessment and no Leitner movement. Distinct from typed-ness – the frontend must
/// not infer it from <c>Reveal</c> being set, because self-assessment stages set it too.
///
/// <c>AnswerPattern</c> (B-66) is the letter-box mask: an underscore per letter/digit to type, every other
/// character (space, hyphen, …) kept literally – already fixed by the solution, so the child never types
/// it. <c>null</c> outside the letter-box stage, same as <c>AnswerLength</c>.
///
/// <c>LetterTiles</c> (B-05) are the scrambled letters of the answer on the scramble stage, in the order to show
/// them; the child sends the word it builds as <c>GivenAnswer</c>. Shuffled once per session/attempt (the same card
/// shows the same order when fetched again). <c>null</c> on every other stage and for an answer that is not one
/// short word - that card comes as letter boxes instead.
///
/// <c>Introduction</c> (B-419) marks a card the child has never seen: it is shown at the type's display stage
/// (so <c>DisplayOnly</c> is set too) whatever the day's stage, and asked for in the next round. The second way to
/// <c>DisplayOnly</c> next to a display stage of the day, told apart here so the round's end can say "new words
/// learned" only where there were new words - a display stage of the day introduces nothing.
///
/// <c>SpeechLang</c> (B-252) is the language the <c>Prompt</c> may be read aloud in by the device - only for a vocabulary
/// card in reading direction whose word has no recording, never on the listening stage. Only the prompt is ever spoken,
/// so it gives away nothing the card does not already show; <c>null</c> = no read-aloud button.
///
/// <c>DictationSpokenAnswer</c>/<c>DictationLang</c> (B-597) are set only on the dictation stage of a vocabulary card
/// without a recording: the learnt word the device reads aloud, and the language to read it in. <b>This is the answer,
/// handed out before the answer</b> - a deliberate exception to the anti-cheat rule (B-597 decision 9), because without
/// it no word of the stock could be dictated. The client never shows it as text; without a voice for the language it
/// plays the card as free text from the German <c>Prompt</c>.
///
/// <c>LearnCard</c> (B-598) marks the learn card: <c>Prompt</c> is the German word, <c>Reveal</c> the learnt word the child
/// uncovers, copies and then recalls; the answer carries both results (<c>WasKnown</c> of the uncovering,
/// <c>GivenAnswer</c> of the recall, <c>null</c> = given up). <c>RevealSpeechLang</c>/<c>RevealAudioUrl</c> read that
/// learnt word aloud - on the learn card only, where it is shown anyway, and only while it is shown.
/// </summary>
public record PracticeCard(int ItemIndex, int Stage, string Type, string? Prompt,
    string? Hint, int? AnswerLength, string? Reveal, IReadOnlyList<string>? Choices, string? AudioUrl,
    string? ImageUrl = null, string? ImageAlt = null, int? GapIndex = null, string? Passage = null,
    bool AnyOrder = false, IReadOnlyList<string>? RevealAlternatives = null,
    IReadOnlyList<WordPair>? Decoding = null, bool DisplayOnly = false, string? AnswerPattern = null,
    IReadOnlyList<string>? LetterTiles = null, bool Introduction = false, IReadOnlyList<string>? CueBox = null,
    string? SpeechLang = null, string? DictationSpokenAnswer = null, string? DictationLang = null,
    bool LearnCard = false, string? RevealSpeechLang = null, string? RevealAudioUrl = null);

/// <summary>The next card in learn mode (or <c>Done</c>), server-driven via the session cursor.</summary>
public record NextResponse(PracticeCard? Card, bool Done, int Cursor, int Total);

/// <summary>
/// The child's answer to a practice card. <paramref name="ItemIndex"/> addresses the content atom
/// in the exercise. Typed stages supply <paramref name="GivenAnswer"/>, display/self-assessment
/// stages supply <paramref name="WasKnown"/>. The server enforces the stage; it grades – never the frontend.
/// </summary>
public record ReviewDto(int ItemIndex, string? GivenAnswer, bool? WasKnown);

/// <summary>
/// "This card is visible now" (B-530). <paramref name="ItemIndex"/> names the card the client shows, the same atom
/// address as in <see cref="ReviewDto"/>: the server stamps only when it is the session's current card, so a report
/// that arrives after that card's own answer cannot become the base of the card after it.
/// </summary>
public record CardShownDto(int ItemIndex);

/// <summary>
/// Result of a Leitner review (graded server-side) incl. bonuses for feedback. <see cref="Next"/>
/// carries the next card directly in learn mode (no separate roundtrip needed); <see cref="Done"/> signals
/// the end of the run. For cards that are not graded (not due / already graded today / not Leitner-based),
/// the points fields are 0, but grading and cursor still advance.
///
/// <see cref="Expected"/> is nullable for the set-graded types: an answer that matches no open entry hits no
/// atom at all, and naming one particular entry as "the" solution would be arbitrary while a dozen are still
/// open – it would also give away an entry the child is about to be asked for. <see cref="Box"/> is 0 and
/// <see cref="DueOn"/> null in that case for the same reason: nothing was reviewed, so nothing moved.
/// <para>
/// <see cref="Misspelled"/> marks a wrong typed answer that is the right word with a small spelling slip (B-422, the
/// measure of B-524). Display only, as in the test: the review stays wrong - box, points and combo as for any miss -
/// so practice prepares for the strict test; the mark only tells the child it knew the word.
/// </para>
/// <para>
/// <see cref="Explanation"/> (B-529) says why the answer is right or wrong - the rule hint of a rule task or the explanation of a vocabulary item (B-583);
/// <c>null</c> where no atom was hit; <c>null</c> for content that has no explanation.
/// </para>
/// <para>
/// <see cref="RepeatsAtEnd"/> (B-598): a learn card given up or recalled wrong comes back once at the end of this round.
/// </para>
/// </summary>
public record ReviewOutcome(bool WasCorrect, string? Expected, int Awarded, int Box,
    DateOnly? DueOn, int Combo, int ComboBonus, int SpeedBonus, PracticeCard? Next, bool Done, bool Misspelled = false,
    string? Explanation = null, bool RepeatsAtEnd = false);
