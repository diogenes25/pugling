namespace Pugling.Api.Models;

/// <summary>
/// What a child learnt on one closed day in one subject (B-733): counts from the answers, the introduced words, the practice
/// time and the day's daily duties. Written once when the day is evaluated and never recomputed, so a later rule change or a
/// corrected answer does not rewrite the history the supervisor has already read. A day the child neither practised nor owed a
/// duty has no row - the supervisor's curve shows a gap there, not a zero.
/// </summary>
public class DailyEvaluation
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    /// <summary>The learning day (German time, <c>LearningDay</c>).</summary>
    public DateOnly Day { get; set; }
    /// <summary>The subject: the exercise's series subject, else the plan's; <c>null</c> where neither has one.</summary>
    public int? SubjectId { get; set; }
    public Subject? Subject { get; set; }
    /// <summary>Words introduced to the child at a position this day (the position's introduction stamp, as in the day view).</summary>
    public int NewWords { get; set; }
    /// <summary>Right answers that moved a word into the top box.</summary>
    public int ConsolidatedWords { get; set; }
    /// <summary>Wrong answers that dropped a word from above box 1.</summary>
    public int FellBackWords { get; set; }
    public int CorrectAnswers { get; set; }
    public int WrongAnswers { get; set; }
    /// <summary>Sum of the practice sessions' active seconds.</summary>
    public int PracticedSeconds { get; set; }
    /// <summary>Wrong typed answers per error kind (enum name → count); empty before the kinds were stored (B-732).</summary>
    public Dictionary<string, int> ErrorKinds { get; set; } = new();
    /// <summary>Daily duties owed this day and met.</summary>
    public int DutiesMet { get; set; }
    /// <summary>Daily duties owed this day and left open.</summary>
    public int DutiesMissed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Marks a closed day of a child as evaluated (B-733), also when it produced no <see cref="DailyEvaluation"/> row (a Sunday,
/// a pause, a day before the first plan). Without the mark such a day would be recomputed on every read; with it a closed
/// day is computed exactly once.
/// </summary>
public class EvaluatedDay
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    public DateOnly Day { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
