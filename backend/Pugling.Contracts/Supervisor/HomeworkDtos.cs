namespace Pugling.Contracts.Supervisor;

// Contract of setting and judging a child's homework (B-760). The amounts are not part of it: they are the child's two
// settings (ChildResponse), so the supervisor never does sums when accepting.

/// <summary>A new homework for a child.</summary>
/// <param name="SubjectId">The subject, optional.</param>
/// <param name="Text">What is to be done, required.</param>
/// <param name="DueOn">The due day. A homework set after its due day costs no penalty.</param>
public record CreateHomeworkDto(int? SubjectId, string Text, DateOnly DueOn);

/// <summary>Gives a hand-in back for rework; the due day stays.</summary>
/// <param name="Note">What is missing, required (one line).</param>
public record ReworkHomeworkDto(string Note);
