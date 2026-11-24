namespace Pugling.Contracts;

/// <summary>
/// Where a homework stands (B-760). Whether it was handed in by its due day is a separate question: the penalty asks only
/// that, and no state here averts or causes it.
/// </summary>
public enum HomeworkStatus
{
    /// <summary>Nothing handed in yet.</summary>
    Open = 0,
    /// <summary>Handed in, waiting for the supervisor.</summary>
    Submitted = 1,
    /// <summary>The supervisor accepted it and the coins are booked; final.</summary>
    Accepted = 2,
    /// <summary>The supervisor gave it back with a note; the child hands in again. The due day does not move.</summary>
    Rework = 3,
}
