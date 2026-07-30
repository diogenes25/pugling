namespace Pugling.Contracts;

/// <summary>
/// Learning method – only the self-description in the exercise type manifest (<see cref="ExerciseTypeManifest"/>)
/// still needs this mapping. No more plan-wide method.
/// </summary>
public enum LearningMethod
{
    /// <summary>Vocabulary learning (card with front/back, Leitner boxes).</summary>
    Vocabulary = 0,
    /// <summary>Cloze.</summary>
    Cloze = 1,
    /// <summary>Matching pairs.</summary>
    Matching = 2,
}

/// <summary>A step in the stage schedule: from day <c>DayNumber</c> (1-based) onward, stage <c>Stage</c> applies.</summary>
public record StageStep(int DayNumber, int Stage);

/// <summary>
/// Play mode of an exercise session. <see cref="Info"/> = free practice: content all in a row, the frontend
/// drives the iteration, and <b>no</b> learning feedback flows (no grading/points/Leitner, doesn't count toward
/// the goal). <see cref="Lern"/> = server-driven: the server holds the cursor + frozen order and grades.
/// </summary>
public enum PlayMode
{
    /// <summary>Free practice without learning feedback – the frontend drives the iteration.</summary>
    Info = 0,
    /// <summary>Server-driven learning with cursor, grading, points, and Leitner scheduling.</summary>
    Lern = 1,
}
