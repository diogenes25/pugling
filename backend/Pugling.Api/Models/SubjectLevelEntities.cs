namespace Pugling.Api.Models;

/// <summary>
/// The grade level of a child in one vocabulary subject (B-741): "on the level of a grade-2 pupil". Derived only from
/// server-graded answers (the share of mastered words in the material of the child's active plans, converted by
/// <see cref="Services.Shared.GradeKey"/>) and stored, because the lowering limit and the best level need history a
/// read-time computation would lose. One row per (child, subject); a subject without vocabulary in an active plan
/// has no measure and gets no row. A level change books nothing to the ledger (no coin value).
/// </summary>
public class ChildSubjectLevel
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    public int SubjectId { get; set; }
    public Subject? Subject { get; set; }

    /// <summary>The current level, 1 (best) to 6. Rises without limit, sinks at most one step per seven days.</summary>
    public int Level { get; set; }

    /// <summary>The best level ever reached; never gets worse, even when <see cref="Level"/> sinks.</summary>
    public int BestLevel { get; set; }

    /// <summary>The learning day of the last lowering through the learning state; <c>null</c> = never lowered.</summary>
    public DateOnly? LastLoweredOn { get; set; }

    /// <summary>The learning day of the last evaluation; a second evaluation on the same day changes nothing.</summary>
    public DateOnly EvaluatedOn { get; set; }
}
