namespace Pugling.Agent.Creator;

/// <summary>
/// The request to the agent: <b>on whose behalf</b> (creator profile), <b>for whom</b> (child - or
/// nobody, which produces a general catalog exercise), <b>where</b> in the catalog (subject/chapter),
/// <b>what</b> (exercise type, topic, scope) and under which safety rules (dry run, self-test).
/// </summary>
/// <param name="ChildId">
/// The child this is tailored to (profile, interests, learning progress) - or <c>null</c> for a
/// <b>general</b> exercise. Without a child, the agent needs no supervision rights, only the creator role.
/// </param>
/// <param name="ProfileId">
/// The creator profile ("subject teacher"). If missing, the agent looks for the best fit when
/// <paramref name="ChildId"/> is set (series match first); without either, the request is incomplete.
/// </param>
/// <param name="UnitId">
/// The unit of the textbook series whose material applies. If missing, the child's current unit applies;
/// if that is also missing, <paramref name="Topic"/> alone carries the material.
/// </param>
/// <param name="General">
/// Design <b>without</b> individualization even with <paramref name="ChildId"/> set: the child's series
/// and unit determine the material, but its interests are left out. For exercises meant for the shared
/// catalog but aligned to a specific child's standing.
/// </param>
/// <param name="SubjectId">Target subject in the catalog.</param>
/// <param name="ChapterId">Target chapter in the catalog.</param>
/// <param name="TypeKey">Exercise-type key from the manifest (e.g. <c>Vocabulary</c>, <c>Cloze</c>).</param>
/// <param name="Topic">Free-text topic or textbook unit ("Unit 3: Animals").</param>
/// <param name="ItemCount">Desired number of tasks.</param>
/// <param name="Words">
/// Prescribed vocabulary. If set, it is <b>immutable</b> - the model may only dress it up, not swap it
/// out (see <see cref="Drafting.DraftPrompts"/>).
/// </param>
/// <param name="UseWeakWords">Use the child's weakly mastered words as vocabulary.</param>
/// <param name="SourceLang">Language code of the language being learned; <c>null</c> = adopt the profile's.</param>
/// <param name="TargetLang">Language code of the native language; <c>null</c> = adopt the profile's.</param>
/// <param name="RewardPoints">Points the exercise is worth.</param>
/// <param name="DryRun">Only plan and print - write nothing.</param>
/// <param name="Strict">Delete the exercise again if the self-test does not reach 100 %.</param>
public sealed record GenerationRequest(
    int? ChildId,
    int? ProfileId,
    int? UnitId,
    bool General,
    int SubjectId,
    int ChapterId,
    string TypeKey,
    string? Topic,
    int ItemCount,
    IReadOnlyList<string> Words,
    bool UseWeakWords,
    string? SourceLang,
    string? TargetLang,
    int RewardPoints,
    bool DryRun,
    bool Strict);
