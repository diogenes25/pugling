using System.Text.Json;

namespace Pugling.Contracts.Creator;

// Contract of the type-agnostic catalog view: search, load-for-edit, usage report.
// The type-specific configuration travels along as raw JSON (JsonElement).

/// <summary>
/// Lean hit row of the exercise search (child-neutral catalog). <c>AuthorAdultId</c>/<c>AuthorName</c> carry the
/// attribution of the shared library (<c>null</c> = seeded system exercise); <c>IsOwn</c> = the requesting supervisor
/// may change/delete the exercise.
///
/// The four <c>Default…</c>/<c>RewardPoints</c> values are the creator's <b>recommendations</b>, not settings: the
/// supervisor's form pre-fills from them and stays free to override (B-45). They belong on the lean row rather than
/// only on <see cref="ExerciseDetail"/> because the form pre-fills straight from the search hit - fetching the
/// detail just to read one number would be a round trip per keystroke in the list.
///
/// The <c>SuggestedGoalPoints</c> figure is what a position would actually inherit from <c>RewardPoints</c> -
/// the raw value only when it is plausible, otherwise the model default (B-192). Both travel, and that is the
/// point: the form shows the effective one, while the raw one stays readable for anyone editing the exercise.
/// Deriving the effective figure client-side would copy a server rule, and the copy is what drifts.
///
/// <c>AverageRating</c>/<c>RatingCount</c> are the other creators' verdict (B-12): <c>null</c>/<c>0</c> while
/// nobody has rated. They sit on the lean row because the whole point is to tell proven material apart while
/// browsing - the same reason <c>sort=rating</c> exists.
/// </summary>
public record ExerciseSummary(int Id, int SeriesId, int SeriesUnitId, int? SubjectId, string Type, string Title,
    int? GradeMin, int? GradeMax, SchoolTypes SchoolTypes, string? Source, int? CategoryId, string? CategoryName,
    int? AuthorAdultId, string? AuthorName, bool IsOwn, bool IsOwner, bool ExecutePublic, string? Description,
    bool DefaultUseLeitner, bool DefaultRequireTypedTest, int? DefaultItemCount, int RewardPoints,
    int SuggestedGoalPoints, double? AverageRating, int RatingCount);

/// <summary>
/// Complete, type-spanning view of an exercise including raw config and all metadata –
/// the basis for editing (load config into the type-specific editor; saving goes through
/// the per-type PUT <c>.../textbook-series/{seriesId}/units/{seriesUnitId}/&lt;type&gt;/{id}</c>,
/// where <see cref="SeriesId"/> supplies the route segment the flat cross-catalog view has no other way to know).
///
/// <c>InterestTagIds</c> is the dressing (B-46) - metadata, and this view promises all of it. It stays off
/// <see cref="ExerciseSummary"/> for the reason that row gives about itself: the lean row carries what the
/// supervisor's form pre-fills from, and a dressing pre-fills nothing. The fit filter B-18 will build
/// belongs in the <i>query</i> anyway, not in a list the client scans.
///
/// <c>AverageRating</c>/<c>RatingCount</c>: the same aggregate as on <see cref="ExerciseSummary"/> (B-12).
/// <c>GrammarTopicIds</c> (B-155): the topics the exercise practices, ascending like the dressing - the editor
/// reads them back here and the PUT has to carry them, or the full replacement drops them.
/// </summary>
public record ExerciseDetail(int Id, int SeriesId, int SeriesUnitId, string SeriesUnitLabel, int? SubjectId, string SubjectName,
    string Type, string Title, int OrderIndex, int RewardPoints, int? GradeMin, int? GradeMax,
    SchoolTypes SchoolTypes, string? Source, int? CategoryId, string? CategoryName,
    SuggestedBonus? SuggestedBonus, int? DefaultStage, int? DefaultItemCount,
    int? AuthorAdultId, string? AuthorName, bool IsOwn, bool IsOwner, bool ExecutePublic, int GrantCount,
    JsonElement Config, string? Description,
    bool DefaultUseLeitner, bool DefaultRequireTypedTest, IReadOnlyList<int> InterestTagIds,
    double? AverageRating, int RatingCount, IReadOnlyList<int> GrammarTopicIds);

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
