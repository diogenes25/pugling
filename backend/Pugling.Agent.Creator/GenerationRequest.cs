namespace Pugling.Agent.Creator;

/// <summary>
/// The request to the agent: <b>on whose behalf</b> (creator profile), <b>for whom</b> (child - or
/// nobody, which produces a general catalog exercise), <b>where</b> in the catalog (series/series unit,
/// since B-106), <b>what</b> (exercise type, topic, scope) and under which safety rules (dry run, self-test).
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
/// <param name="SubjectId">Subject of the exercise: the subject of <paramref name="SeriesId"/> - the briefing rejects any other
/// (B-488); a profile's subject only for a series without one.</param>
/// <param name="SeriesId">Target textbook series in the catalog.</param>
/// <param name="SeriesUnitId">Target series unit the exercise is created under.</param>
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
/// <param name="RewardPoints">
/// The author's <b>recommendation</b> for the target reward, not a property of the exercise (B-45): a new
/// study-plan position inherits it when the supervisor states nothing. Outside <c>(0, 200]</c> the server
/// discards it and the position falls back to 20, so a figure above the bound is not "more coins" but
/// none of the author's doing at all.
/// </param>
/// <param name="Interests">
/// Interest slugs the exercise should be <b>dressed</b> in (B-46), resolved against the shared taxonomy.
/// This is the childless counterpart to <paramref name="ChildId"/>: with a child the dressing comes from
/// the child's own positively weighted interests, so naming both is a usage error rather than a merge.
/// Empty = no dressing recorded.
/// </param>
/// <param name="DryRun">Only plan and print - write nothing.</param>
/// <param name="Strict">Delete the exercise again if the self-test does not reach 100 %.</param>
/// <param name="Part">
/// Which exercise of a series this is (0 = the first). A study plan with two vocabulary exercises on one unit asks
/// for part 0 and part 1, and each gets its own slice of the unit's word pairs instead of the same ten twice.
/// </param>
/// <param name="SchoolTest">
/// A cloze in the school's vocabulary-test format (B-524): single sentences with one gap each, every gap a word of
/// the unit's list linked to its store entry (so the cue box can show its meaning), the sentence form where it
/// differs from the stored word, and German distractors for the cue box instead of an English word bank.
/// </param>
public sealed record GenerationRequest(
    int? ChildId,
    int? ProfileId,
    int? UnitId,
    bool General,
    int SubjectId,
    int SeriesId,
    int SeriesUnitId,
    string TypeKey,
    string? Topic,
    int ItemCount,
    IReadOnlyList<string> Words,
    bool UseWeakWords,
    string? SourceLang,
    string? TargetLang,
    int RewardPoints,
    IReadOnlyList<string> Interests,
    bool DryRun,
    bool Strict,
    int Part = 0,
    bool SchoolTest = false);
