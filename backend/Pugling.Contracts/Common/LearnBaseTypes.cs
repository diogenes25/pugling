namespace Pugling.Contracts;

/// <summary>
/// School types for which an exercise is suitable. A <c>[Flags]</c> enum so that an exercise
/// can be assigned to multiple school types (e.g. Realschule | Gymnasium).
/// <see cref="None"/> means "for all school types" (no filter exclusion).
/// </summary>
[Flags]
public enum SchoolTypes
{
    /// <summary>No restriction – the exercise fits any school type.</summary>
    None = 0,
    /// <summary>Primary school (Grundschule).</summary>
    Grundschule = 1,
    /// <summary>Lower secondary school (Hauptschule).</summary>
    Hauptschule = 2,
    /// <summary>Intermediate secondary school (Realschule).</summary>
    Realschule = 4,
    /// <summary>Academic secondary school (Gymnasium).</summary>
    Gymnasium = 8,
    /// <summary>Comprehensive school (Gesamtschule).</summary>
    Gesamtschule = 16,
    /// <summary>Vocational school (Berufsschule).</summary>
    Berufsschule = 32,
}

/// <summary>
/// Bonus system suggested by the exercise author (global on the exercise). Serves only as a template:
/// when a study plan is created from the exercise, these values are copied ONCE into its bonus fields.
/// Later changes to the exercise therefore do NOT retroactively affect existing child plans –
/// the running bonus system remains child-specific and adjustable per study plan (motivation control
/// per child/exercise). Fields mirror the bonus knobs of the <c>StudyPlan</c>.
/// </summary>
public record SuggestedBonus(
    int ComboThreshold,
    int ComboBonusPoints,
    int SpeedThresholdSeconds,
    int SpeedBonusPoints,
    int NewContentPoints);

/// <summary>
/// RWX permission that an owner grants to an individual creator on an exercise. Hierarchy
/// <see cref="Owner"/> ⊃ <see cref="Write"/> ⊃ <see cref="Execute"/>: an owner may additionally delete,
/// toggle <c>Exercise.ExecutePublic</c>, and grant/revoke permissions themselves. Read is deliberately
/// not part of the model – the catalog remains readable for everyone (shared library).
/// </summary>
public enum GrantPermission
{
    /// <summary>Full access: change, delete, toggle sharing, grant and revoke permissions.</summary>
    Owner,
    /// <summary>May change the exercise's content, but not delete it or grant permissions.</summary>
    Write,
    /// <summary>May assign the exercise to a supervised child and play it out, but not change it.</summary>
    Execute,
}
