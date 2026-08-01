namespace Pugling.Api.Models;

// ObjectiveKind/KeyResultMetric live in the contract project (Pugling.Contracts).

/// <summary>
/// A <b>big goal</b> set by the supervisor for a child (the OKR core, child-friendly): a dated, motivating
/// bracket over several measurable <see cref="KeyResult"/>s (the "milestones"). Just as a
/// <see cref="StudyPlan"/> is a container over <see cref="PlanPosition"/>s, an objective is a container over
/// key results. Progress is computed <b>live</b> from the aggregated learning state (no materialized state);
/// rewards are paid idempotently through lazy settlement (see <c>ObjectiveRewardService</c>): a small bite per
/// reached milestone (<see cref="RewardPerKeyResult"/>) and the big chunk on full completion
/// (<see cref="RewardOnComplete"/>). No grading, no penalty.
/// </summary>
public class Objective
{
    public int Id { get; set; }

    public int ChildId { get; set; }
    public Child? Child { get; set; }

    /// <summary>Concrete, child-friendly title (e.g. "master English Unit 3").</summary>
    public string Title { get; set; } = "";
    /// <summary>The "why" in one sentence – shown to the child as motivation.</summary>
    public string? Motivation { get; set; }
    /// <summary>Mandatory (coins) or stretch goal (gems).</summary>
    public ObjectiveKind Kind { get; set; }

    /// <summary>Optional start; class test grades only count from this day on (null = no lower bound).</summary>
    public DateOnly? Start { get; set; }
    /// <summary>Optional due date; after it an unreached goal counts as "overdue".</summary>
    public DateOnly? DueDate { get; set; }
    /// <summary>Whether the goal is actively pursued (and rewarded). Inactive goals are no longer settled.</summary>
    public bool Active { get; set; } = true;

    /// <summary>Reward for reaching ALL key results (coins or gems per <see cref="Kind"/>). 0 = none.</summary>
    public int RewardOnComplete { get; set; }
    /// <summary>Reward per individually reached milestone (a short feedback loop). 0 = none.</summary>
    public int RewardPerKeyResult { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<KeyResult> KeyResults { get; set; } = [];
}

/// <summary>
/// A measurable <b>milestone</b> of an <see cref="Objective"/> on a catalog scope (subject, optionally
/// chapter/exercise). The mastery metrics are evaluated live through the learning state's
/// <c>ScopeEvaluator</c>; <see cref="KeyResultMetric.ClassTestGrade"/> reads the subject's
/// <see cref="Klassenarbeit.Grade"/> as entered by the supervisor (the scope is then the subject only).
/// </summary>
public class KeyResult
{
    public int Id { get; set; }

    public int ObjectiveId { get; set; }
    public Objective? Objective { get; set; }

    // --- Catalog scope (hierarchy: Exercise ⊂ Chapter ⊂ Subject) ---
    /// <summary>Subject of the milestone (mandatory).</summary>
    public int SubjectId { get; set; }
    /// <summary>Optional: chapter; <c>null</c> = the whole subject. Only allowed for mastery metrics.</summary>
    public int? ChapterId { get; set; }
    /// <summary>Optional: a concrete vocabulary exercise; requires <see cref="ChapterId"/>. Mastery metrics only.</summary>
    public int? ExerciseId { get; set; }

    // --- Goal ---
    /// <summary>The measured figure.</summary>
    public KeyResultMetric Metric { get; set; }
    /// <summary>Target value: percent (0..100), or a count (MaxWeakItems), or grade×10 (ClassTestGrade, 10..60).</summary>
    public int TargetValue { get; set; }
    /// <summary>Optional freely chosen title (otherwise derivable from scope/metric).</summary>
    public string? Title { get; set; }
}

/// <summary>
/// Records a <b>one-off</b> reward entry of an <see cref="Objective"/> – the objective counterpart to
/// <see cref="PositionGoalReward"/>. Two <b>filtered</b> unique indexes guarantee that every milestone and the
/// full completion are paid out at most once per objective – even if the lazy settlement runs several times.
/// Unlike the periodic position goals, the reward here is <b>one-off</b> (no period): a later regression of the
/// learning state does not take an already earned milestone back (no penalty on objectives).
/// <para>
/// The occasion sits in <see cref="PaidKeyResultId"/>: set = this milestone, <c>null</c> = the full completion.
/// </para>
/// </summary>
public class ObjectiveReward
{
    public int Id { get; set; }
    public int ObjectiveId { get; set; }
    public Objective? Objective { get; set; }
    /// <summary>
    /// The milestone that was paid for; <c>null</c> stands for the full completion of the objective.
    /// <para>
    /// Deliberately <b>not</b> a foreign key to <see cref="KeyResult"/>, for three reasons that all point in
    /// the same direction: <c>SetNull</c> would silently turn a milestone entry into the <i>completion</i>
    /// entry when the milestone is deleted (a discriminator must not flip because of a deletion);
    /// <c>Cascade</c> would create a second cascade path coming from the objective (objective → key result →
    /// reward next to objective → reward), i.e. exactly the SQLite diamond this model otherwise avoids; and
    /// the entry is meant to <b>outlive</b> the milestone anyway – paid is paid. That makes the column an
    /// audit snapshot like <c>ShopPurchase.SupervisorId</c>.
    /// </para>
    /// </summary>
    public int? PaidKeyResultId { get; set; }
    /// <summary>Credited amount (a positive value; coins or gems per <see cref="ObjectiveKind"/>).</summary>
    public int Points { get; set; }
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
}
