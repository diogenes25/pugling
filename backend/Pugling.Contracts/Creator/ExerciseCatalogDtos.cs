using System.Text.Json;

namespace Pugling.Contracts.Creator;

// Contract of the type-agnostic catalog view: search, load-for-edit, usage report.
// The type-specific configuration travels along as raw JSON (JsonElement).

/// <summary>
/// Lean hit row of the exercise search (child-neutral catalog). <c>AuthorAdultId</c>/<c>AuthorName</c> carry the
/// attribution of the shared library (<c>null</c> = seeded system exercise); <c>IsOwn</c> = the requesting supervisor
/// may change/delete the exercise.
/// </summary>
public record ExerciseSummary(int Id, int ChapterId, int SubjectId, string Type, string Title,
    int? GradeMin, int? GradeMax, SchoolTypes SchoolTypes, string? Source, int? CategoryId, string? CategoryName,
    int? AuthorAdultId, string? AuthorName, bool IsOwn, bool IsOwner, bool ExecutePublic, string? Description,
    bool DefaultUseLeitner, bool DefaultRequireTypedTest);

/// <summary>
/// Complete, type-spanning view of an exercise including raw config and all metadata –
/// the basis for editing (load config into the type-specific editor; saving goes through
/// the per-type PUT <c>.../chapters/{}/&lt;type&gt;/{id}</c>).
/// </summary>
public record ExerciseDetail(int Id, int ChapterId, string ChapterName, int SubjectId, string SubjectName,
    string Type, string Title, int OrderIndex, int RewardPoints, int? GradeMin, int? GradeMax,
    SchoolTypes SchoolTypes, string? Source, int? CategoryId, string? CategoryName,
    SuggestedBonus? SuggestedBonus, int? DefaultStage, int? DefaultItemCount,
    int? AuthorAdultId, string? AuthorName, bool IsOwn, bool IsOwner, bool ExecutePublic, int GrantCount,
    JsonElement Config, string? Description,
    bool DefaultUseLeitner, bool DefaultRequireTypedTest);

/// <summary>A study plan in which an exercise is used as a position.</summary>
public record PlanUsage(int PlanId, string PlanTitle, int ChildId, string ChildName);

/// <summary>A class test to which an exercise is assigned (directly or via a tag).</summary>
public record ClassTestUsage(int Id, string Title, int ChildId, string ChildName);

/// <summary>
/// Where an exercise is used. <see cref="Plans"/> and <see cref="ClassTests"/> only name resources of the
/// caller's <b>own</b> children – other supervisors' children must not show up here.
/// </summary>
/// <param name="Plans">Study plans of own children in which the exercise is used as a position.</param>
/// <param name="ClassTests">Class tests of own children (assigned directly or via a tag).</param>
/// <param name="OtherLearnersCount">
/// How many <b>different children</b> use the exercise whom the caller <b>does not supervise</b> – just the
/// number, without names. Two reasons:
/// <list type="bullet">
/// <item>Without it the lists report "nowhere" while deletion fails with <c>409 exercise_in_use</c> –
/// a contradiction the author cannot resolve (remark 14).</item>
/// <item>For a <b>creator without own children</b> (a teacher, or an AI creator app), both
/// lists are permanently empty. This number is then not the footnote but the only answer to their
/// actual question: is my material being used?</item>
/// </list>
/// <b>Children</b> are counted, not usage sites: three positions in the same child's plans
/// are one user. The basis is the FK-relevant usages that also block deletion – a class test only
/// collected via a tag does not prevent it and is therefore not counted.
/// </param>
public record UsageResponse(
    IReadOnlyList<PlanUsage> Plans, IReadOnlyList<ClassTestUsage> ClassTests, int OtherLearnersCount);
