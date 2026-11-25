namespace Pugling.Api.Models;

// Timetable control: the study plan follows the child's school timetable. On a subject's lesson day NEW
// material is learned, on the other days it is reviewed (the day right before counts as preparation).

/// <summary>One timetable entry: on this weekday the child has this subject.</summary>
public class TimetableEntry
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    public int SubjectId { get; set; }
    public Subject? Subject { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    /// <summary>Optional time of day as free text (e.g. "Nachmittag").</summary>
    public string? TimeOfDay { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// A pause of the child (B-731), from <see cref="From"/> to <see cref="Until"/>, both days included: no duty is owed - no
/// penalty, and the streak neither counts nor breaks. Several per child, so the history stays readable.
/// </summary>
public class ChildPause
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    /// <summary>First paused day.</summary>
    public DateOnly From { get; set; }
    /// <summary>Last paused day.</summary>
    public DateOnly Until { get; set; }
    /// <summary>Optional reason ("krank").</summary>
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
