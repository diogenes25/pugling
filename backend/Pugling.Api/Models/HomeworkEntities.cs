namespace Pugling.Api.Models;

/// <summary>
/// A school homework of a child (B-760): set by a supervisor with a due day, handed in by the child, accepted or given back
/// by the supervisor. It is no position: it is not repeatable, has no answer key and counts toward no streak, daily box,
/// mission or bonus-training lock. It takes over the duty's penalty <i>pattern</i> (ledger entry, unique settlement key,
/// wallet bump), not its table - <see cref="PositionGoalPenalty"/> needs a position.
/// </summary>
public class Homework
{
    public int Id { get; set; }

    /// <summary>The child; the homework disappears with it.</summary>
    public int ChildId { get; set; }
    public Child? Child { get; set; }

    /// <summary>The subject, optional; a deleted subject leaves the homework without one (SetNull).</summary>
    public int? SubjectId { get; set; }
    public Subject? Subject { get; set; }

    /// <summary>What is to be done.</summary>
    public string Text { get; set; } = "";

    /// <summary>The due day on the learning-day calendar: a hand-in by the end of it averts the penalty.</summary>
    public DateOnly DueOn { get; set; }

    /// <summary>When the supervisor set it. A homework set after its due day costs no penalty: the child never saw it in time.</summary>
    public DateTime CreatedAt { get; set; }

    public HomeworkStatus Status { get; set; } = HomeworkStatus.Open;

    /// <summary>The supervisor's note when he last gave it back; it stays after a new hand-in.</summary>
    public string? ReworkNote { get; set; }

    /// <summary>When the supervisor last accepted it or gave it back.</summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>The coins booked on acceptance (snapshot of the child's amount then), or <c>null</c> while not accepted.</summary>
    public int? AwardedCoins { get; set; }

    public List<HomeworkSubmission> Submissions { get; set; } = [];
}

/// <summary>
/// One hand-in of a homework (B-760). A record with the server time only; the photo file is added by B-761. Server time is
/// what counts - the supervisor's look at the hand-in is the anti-cheat, not forensics (B-687 decision 4).
/// </summary>
public class HomeworkSubmission
{
    public int Id { get; set; }

    public int HomeworkId { get; set; }
    public Homework? Homework { get; set; }

    public DateTime SubmittedAt { get; set; }
}

/// <summary>
/// The booked penalty of a homework handed in nothing by its due day (B-760). The unique index on
/// <see cref="HomeworkId"/> is the idempotency guarantee: two parallel logins cannot charge the same homework twice.
/// </summary>
public class HomeworkPenalty
{
    public int Id { get; set; }

    public int HomeworkId { get; set; }
    public Homework? Homework { get; set; }

    /// <summary>Coins deducted (a positive value; the ledger entry is negative).</summary>
    public int Points { get; set; }

    public DateTime AppliedAt { get; set; }
}
