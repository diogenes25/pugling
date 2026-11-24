namespace Pugling.Contracts.Shared;

// Contract of a child's homework (B-760): the same view for the supervisor and the child - there is no solution to hide,
// and the child should see what a hand-in earns and what missing the due day costs.

/// <summary>A homework of a child with its hand-ins.</summary>
/// <param name="Id">The homework.</param>
/// <param name="ChildId">The child it belongs to.</param>
/// <param name="SubjectId">The subject, or <c>null</c> when none was given or it was deleted since.</param>
/// <param name="SubjectName">Its name, or <c>null</c>.</param>
/// <param name="Text">What is to be done.</param>
/// <param name="DueOn">The due day: a hand-in by the end of it averts the penalty.</param>
/// <param name="CreatedAt">When the supervisor set it.</param>
/// <param name="Status">Where it stands.</param>
/// <param name="ReworkNote">The supervisor's note when he last gave it back, or <c>null</c>; it stays after a new hand-in.</param>
/// <param name="ReviewedAt">When the supervisor last accepted it or gave it back, or <c>null</c>.</param>
/// <param name="RewardCoins">Coins an acceptance books - the amount booked once accepted, else the child's current amount.</param>
/// <param name="PenaltyCoins">Coins missing the due day costs - the amount booked once charged, else the child's current amount.</param>
/// <param name="PenaltyCharged">Whether the penalty was booked; a later hand-in does not take it back.</param>
/// <param name="Submissions">The hand-ins, oldest first.</param>
public record HomeworkResponse(int Id, int ChildId, int? SubjectId, string? SubjectName, string Text, DateOnly DueOn,
    DateTime CreatedAt, HomeworkStatus Status, string? ReworkNote, DateTime? ReviewedAt, int RewardCoins, int PenaltyCoins,
    bool PenaltyCharged, IReadOnlyList<HomeworkSubmissionResponse> Submissions);

/// <summary>One hand-in of a homework.</summary>
/// <param name="Id">The hand-in.</param>
/// <param name="SubmittedAt">Server time of the hand-in; the supervisor compares it with the homework.</param>
/// <param name="InTime">Whether it came by the end of the due day, so that it averts the penalty.</param>
public record HomeworkSubmissionResponse(int Id, DateTime SubmittedAt, bool InTime);
