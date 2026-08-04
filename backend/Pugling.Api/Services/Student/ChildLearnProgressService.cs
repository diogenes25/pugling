using Microsoft.EntityFrameworkCore;
using Pugling.Api.Controllers;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Student;

/// <summary>
/// Child-centric drill-down view of vocabulary learning progress along the catalog hierarchy
/// (subject → series unit → exercise → item; subject is reached transitively through the exercise's
/// series unit, see B-106 T-01). Complements the flat <see cref="Controllers.Student.ChildVocabularyProgressController"/> view
/// with aggregated roll-ups per level. Displays the <b>relevant set</b>: everything assigned to the child via a
/// <see cref="StudyPlan"/> (even with 0% progress, so coverage is visible)
/// <b>plus</b> everything for which progress already exists (<see cref="ItemProgress"/>) – so progress once
/// earned doesn't disappear when the exercise is later dropped or its plan deactivated. The
/// <c>Active</c> flag distinguishes "currently assigned via an active plan" from "only historical / inactive".
/// Only vocabulary exercises are item-tracked (stable <see cref="ExerciseItem.Id"/>), so the view is deliberately
/// vocabulary-scoped. The progress is not recalculated but read from <see cref="ItemProgress"/>
/// (updated by the <see cref="ItemProgressService"/> at the grading points).
/// </summary>
public class ChildLearnProgressService(PuglingDbContext db, ExerciseTypeRegistry registry)
{
    /// <summary>The mastery threshold (percent) below which an item counts as "weak" – shared with the flat view.</summary>
    private const int WeakBelowPercent = ItemProgress.WeakBelowPercent;

    // MasteryRollup/Subject-/SeriesUnit-/ExerciseProgressResponse/ItemProgressResponse live in the contract
    // project (Pugling.Contracts.Student); the flat and the hierarchical view share the item DTO.

    // One vocabulary exercise relevant to the view (assigned and/or with progress) plus its catalog coordinates.
    // Active = referenced by at least one ACTIVE plan of the child.
    internal record RelevantExercise(int ExerciseId, string Title, int ExerciseOrder,
        int SeriesUnitId, string SeriesUnitLabel, int SeriesUnitOrder, int SubjectId, bool Active);

    // Raw aggregate of an item set: summable, so exercise → series unit → subject rolls up without another DB query.
    private record Agg(int TotalItems, int Introduced, int Mastered, int Weak,
        int Seen, int Correct, int MasterySum, DateTime? LastActivity);

    // Per-exercise aggregated progress row from ItemProgress (Introduced = rows, i.e. answered at least once).
    internal record ProgRow(int Introduced, int Mastered, int Weak, int Seen, int Correct, int MasterySum, DateTime? LastActivity);

    // EF projection of the item leaf without the derived store link (added in memory).
    private record ItemRow(int ItemId, int ExerciseId, int VocabularyId, string Front, string Back,
        int Box, int MasteryPercent, int SeenCount, int CorrectCount,
        DateOnly? IntroducedAt, DateTime? LastAnswerAt, bool? LastCorrect);

    // All vocabulary exercises relevant to the child: (assigned through any plan) ∪ (has progress), unique
    // per exercise, with the active flag (referenced by an active plan) and the catalog coordinates.
    // Deliberately several simple DB queries merged in memory (Distinct/filter over the projection tuple is
    // not translatable by the SQLite provider); the relevant set per child is small.
    private async Task<List<RelevantExercise>> LoadRelevantAsync(int childId, CancellationToken ct)
    {
        // From the plans: exercise ids + whether an ACTIVE plan references them.
        var planRows = await (
            from pp in db.PlanPositions.AsNoTracking()
            where pp.StudyPlan!.ChildId == childId
            select new { pp.ExerciseId, pp.StudyPlan!.Active })
            .ToListAsync(ct);

        // From progress: exercises with a learning state (they survive being unassigned - ItemProgress cannot outlive a deleted exercise).
        var progressIds = await db.ItemProgress.AsNoTracking()
            .Where(p => p.ChildId == childId)
            .Select(p => p.ExerciseId).Distinct().ToListAsync(ct);

        var activeIds = planRows.Where(r => r.Active).Select(r => r.ExerciseId).ToHashSet();
        var allIds = planRows.Select(r => r.ExerciseId).Concat(progressIds).Distinct().ToList();
        if (allIds.Count == 0) return [];

        // Catalog coordinates (item-tracked types only, today vocabulary).
        var itemProgressKeys = registry.KeysSupportingItemProgress;
        // Subject is reached transitively through the unit's series (SeriesUnit has no SubjectId of its own,
        // B-106 T-01) - exercises whose series carries no subject are excluded, same as everywhere else.
        var coords = await (
            from ex in db.Exercises.AsNoTracking()
            where allIds.Contains(ex.Id) && itemProgressKeys.Contains(ex.Type)
            join u in db.SeriesUnits.AsNoTracking() on ex.SeriesUnitId equals u.Id
            join s in db.TextbookSeries.AsNoTracking() on u.SeriesId equals s.Id
            where s.SubjectId != null
            select new { ex.Id, ex.Title, ExOrder = ex.OrderIndex, UnitId = u.Id, UnitLabel = u.Label, UnitOrder = u.OrderIndex, SubjectId = s.SubjectId!.Value })
            .ToListAsync(ct);

        return coords
            .Select(c => new RelevantExercise(c.Id, c.Title, c.ExOrder, c.UnitId, c.UnitLabel, c.UnitOrder, c.SubjectId, activeIds.Contains(c.Id)))
            .ToList();
    }

    // Loads the total item count (including unpracticed ones) and aggregated progress per exercise for the given ids.
    private async Task<(Dictionary<int, int> Total, Dictionary<int, ProgRow> Prog)> LoadAggAsync(
        int childId, IReadOnlyList<int> exerciseIds, CancellationToken ct)
    {
        var total = await db.ExerciseItems.AsNoTracking()
            .Where(i => exerciseIds.Contains(i.ExerciseId))
            .GroupBy(i => i.ExerciseId)
            .Select(g => new { ExerciseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ExerciseId, x => x.Count, ct);

        // Pull the aggregates as an anonymous type (projecting straight into a record constructor is not
        // translatable), then map to ProgRow in memory.
        var progRows = await db.ItemProgress.AsNoTracking()
            .Where(p => p.ChildId == childId && exerciseIds.Contains(p.ExerciseId))
            .GroupBy(p => p.ExerciseId)
            .Select(g => new
            {
                ExerciseId = g.Key,
                Introduced = g.Count(),
                Mastered = g.Count(x => x.Box >= ItemProgress.MaxBox),
                Weak = g.Count(x => x.MasteryPercent < WeakBelowPercent),
                Seen = g.Sum(x => x.SeenCount),
                Correct = g.Sum(x => x.CorrectCount),
                MasterySum = g.Sum(x => x.MasteryPercent),
                LastActivity = g.Max(x => x.LastAnswerAt),
            })
            .ToListAsync(ct);
        var prog = progRows.ToDictionary(x => x.ExerciseId,
            x => new ProgRow(x.Introduced, x.Mastered, x.Weak, x.Seen, x.Correct, x.MasterySum, x.LastActivity));

        return (total, prog);
    }

    private static Agg AggFor(int exerciseId, IReadOnlyDictionary<int, int> total, IReadOnlyDictionary<int, ProgRow> prog)
    {
        var items = total.GetValueOrDefault(exerciseId);
        return prog.TryGetValue(exerciseId, out var p)
            ? new Agg(items, p.Introduced, p.Mastered, p.Weak, p.Seen, p.Correct, p.MasterySum, p.LastActivity)
            : new Agg(items, 0, 0, 0, 0, 0, 0, null);
    }

    private static Agg Combine(IEnumerable<Agg> parts)
    {
        var acc = new Agg(0, 0, 0, 0, 0, 0, 0, null);
        foreach (var a in parts)
            acc = new Agg(acc.TotalItems + a.TotalItems, acc.Introduced + a.Introduced, acc.Mastered + a.Mastered,
                acc.Weak + a.Weak, acc.Seen + a.Seen, acc.Correct + a.Correct, acc.MasterySum + a.MasterySum,
                a.LastActivity is { } d && (acc.LastActivity is null || d > acc.LastActivity) ? d : acc.LastActivity);
        return acc;
    }

    /// <summary>Empty roll-up (scope without relevant exercises / without progress).</summary>
    public static readonly MasteryRollup EmptyRollup = new(0, 0, 0, 0, 0, 0, 0, 0, null);

    // Average mastery over the INTRODUCED items (not over all), hit rate over the answers seen.
    private static MasteryRollup ToRollup(Agg a) =>
        new(a.TotalItems, a.Introduced, a.Mastered, a.Weak,
            a.Introduced == 0 ? 0 : (int)Math.Round((double)a.MasterySum / a.Introduced),
            a.Seen, a.Correct, a.Seen == 0 ? 0 : (int)Math.Round(100.0 * a.Correct / a.Seen),
            a.LastActivity);

    // Coverage 0..1 for the sort key "coverage" (introduced / total).
    private static double Coverage(MasteryRollup r) => r.TotalItems == 0 ? 0 : (double)r.IntroducedItems / r.TotalItems;

    private static IOrderedEnumerable<T> Order<T, TKey>(IEnumerable<T> src, Func<T, TKey> key, bool desc) =>
        desc ? src.OrderByDescending(key) : src.OrderBy(key);

    private static bool Matches(string text, string term) => text.Contains(term, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// All relevant subjects of the child with aggregated vocabulary progress. Optionally filtered by
    /// <paramref name="search"/> (subject name), <paramref name="active"/> (only (in)active) and sorted
    /// (<c>name</c> [default], <c>mastery</c>, <c>coverage</c>, <c>weak</c>, <c>activity</c>).
    /// </summary>
    public async Task<List<SubjectProgressResponse>> SubjectsAsync(int childId, string? search,
        (string? Key, bool Desc) sort, bool? active, CancellationToken ct = default)
    {
        var relevant = await LoadRelevantAsync(childId, ct);
        if (relevant.Count == 0) return [];

        var (total, prog) = await LoadAggAsync(childId, relevant.Select(r => r.ExerciseId).ToList(), ct);
        var subjectIds = relevant.Select(r => r.SubjectId).Distinct().ToList();
        var names = await db.Subjects.AsNoTracking().Where(s => subjectIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

        var rows = relevant.GroupBy(r => r.SubjectId)
            .Select(g => new SubjectProgressResponse(g.Key, names.GetValueOrDefault(g.Key, ""),
                g.Select(r => r.SeriesUnitId).Distinct().Count(), g.Count(), g.Any(r => r.Active),
                ToRollup(Combine(g.Select(r => AggFor(r.ExerciseId, total, prog))))))
            .AsEnumerable();

        if (active is { } act) rows = rows.Where(r => r.Active == act);
        if (!string.IsNullOrWhiteSpace(search)) rows = rows.Where(r => Matches(r.Name, search.Trim()));

        return SortSubjects(rows, sort).ToList();
    }

    private static IEnumerable<SubjectProgressResponse> SortSubjects(IEnumerable<SubjectProgressResponse> rows, (string? Key, bool Desc) s) =>
        s.Key?.ToLowerInvariant() switch
        {
            "name" => Order(rows, r => r.Name, s.Desc).ThenBy(r => r.SubjectId),
            "mastery" => Order(rows, r => r.Progress.AvgMasteryPercent, s.Desc).ThenBy(r => r.SubjectId),
            "coverage" => Order(rows, r => Coverage(r.Progress), s.Desc).ThenBy(r => r.SubjectId),
            "weak" => Order(rows, r => r.Progress.WeakItems, s.Desc).ThenBy(r => r.SubjectId),
            "activity" => Order(rows, r => r.Progress.LastActivityAt, s.Desc).ThenBy(r => r.SubjectId),
            _ => rows.OrderBy(r => r.Name).ThenBy(r => r.SubjectId),
        };

    /// <summary>A single relevant subject; <c>null</c> if nothing is assigned to the child in it and no progress exists.</summary>
    public async Task<SubjectProgressResponse?> SubjectAsync(int childId, int subjectId, CancellationToken ct = default)
    {
        var relevant = (await LoadRelevantAsync(childId, ct)).Where(r => r.SubjectId == subjectId).ToList();
        if (relevant.Count == 0) return null;

        var (total, prog) = await LoadAggAsync(childId, relevant.Select(r => r.ExerciseId).ToList(), ct);
        var name = await db.Subjects.AsNoTracking().Where(s => s.Id == subjectId).Select(s => s.Name).FirstOrDefaultAsync(ct) ?? "";
        return new SubjectProgressResponse(subjectId, name,
            relevant.Select(r => r.SeriesUnitId).Distinct().Count(), relevant.Count, relevant.Any(r => r.Active),
            ToRollup(Combine(relevant.Select(r => AggFor(r.ExerciseId, total, prog)))));
    }

    /// <summary>
    /// Chapters of a subject with progress; <c>null</c> if the subject is not relevant. Filter/sorting as
    /// for subjects (sort keys additionally <c>order</c> [default, chapter order]).
    /// </summary>
    public async Task<List<SeriesUnitProgressResponse>?> SeriesUnitsAsync(int childId, int subjectId, string? search,
        (string? Key, bool Desc) sort, bool? active, CancellationToken ct = default)
    {
        var relevant = (await LoadRelevantAsync(childId, ct)).Where(r => r.SubjectId == subjectId).ToList();
        if (relevant.Count == 0) return null;

        var (total, prog) = await LoadAggAsync(childId, relevant.Select(r => r.ExerciseId).ToList(), ct);
        var rows = relevant.GroupBy(r => new { r.SeriesUnitId, r.SeriesUnitLabel, r.SeriesUnitOrder })
            .Select(g => new SeriesUnitProgressResponse(g.Key.SeriesUnitId, g.Key.SeriesUnitLabel, g.Key.SeriesUnitOrder, g.Count(), g.Any(r => r.Active),
                ToRollup(Combine(g.Select(r => AggFor(r.ExerciseId, total, prog))))))
            .AsEnumerable();

        if (active is { } act) rows = rows.Where(r => r.Active == act);
        if (!string.IsNullOrWhiteSpace(search)) rows = rows.Where(r => Matches(r.Name, search.Trim()));

        return SortSeriesUnits(rows, sort).ToList();
    }

    private static IEnumerable<SeriesUnitProgressResponse> SortSeriesUnits(IEnumerable<SeriesUnitProgressResponse> rows, (string? Key, bool Desc) s) =>
        s.Key?.ToLowerInvariant() switch
        {
            "name" => Order(rows, r => r.Name, s.Desc).ThenBy(r => r.SeriesUnitId),
            "mastery" => Order(rows, r => r.Progress.AvgMasteryPercent, s.Desc).ThenBy(r => r.SeriesUnitId),
            "coverage" => Order(rows, r => Coverage(r.Progress), s.Desc).ThenBy(r => r.SeriesUnitId),
            "weak" => Order(rows, r => r.Progress.WeakItems, s.Desc).ThenBy(r => r.SeriesUnitId),
            "activity" => Order(rows, r => r.Progress.LastActivityAt, s.Desc).ThenBy(r => r.SeriesUnitId),
            _ => rows.OrderBy(r => r.OrderIndex).ThenBy(r => r.SeriesUnitId),
        };

    /// <summary>
    /// Relevant vocabulary exercises of a chapter with progress per exercise; <c>null</c> if the chapter is not relevant.
    /// Filter/sorting as for chapters (sort keys additionally <c>title</c>, <c>active</c>; default <c>order</c>).
    /// </summary>
    public async Task<List<ExerciseProgressResponse>?> ExercisesAsync(int childId, int subjectId, int seriesUnitId, string? search,
        (string? Key, bool Desc) sort, bool? active, CancellationToken ct = default)
    {
        var relevant = (await LoadRelevantAsync(childId, ct))
            .Where(r => r.SubjectId == subjectId && r.SeriesUnitId == seriesUnitId).ToList();
        if (relevant.Count == 0) return null;

        var (total, prog) = await LoadAggAsync(childId, relevant.Select(r => r.ExerciseId).ToList(), ct);
        var rows = relevant
            .Select(r => new ExerciseProgressResponse(r.ExerciseId, r.Title, r.ExerciseOrder, r.Active,
                ToRollup(AggFor(r.ExerciseId, total, prog))))
            .AsEnumerable();

        if (active is { } act) rows = rows.Where(r => r.Active == act);
        if (!string.IsNullOrWhiteSpace(search)) rows = rows.Where(r => Matches(r.Title, search.Trim()));

        return SortExercises(rows, sort).ToList();
    }

    private static IEnumerable<ExerciseProgressResponse> SortExercises(IEnumerable<ExerciseProgressResponse> rows, (string? Key, bool Desc) s) =>
        s.Key?.ToLowerInvariant() switch
        {
            "title" => Order(rows, r => r.Title, s.Desc).ThenBy(r => r.ExerciseId),
            "mastery" => Order(rows, r => r.Progress.AvgMasteryPercent, s.Desc).ThenBy(r => r.ExerciseId),
            "coverage" => Order(rows, r => Coverage(r.Progress), s.Desc).ThenBy(r => r.ExerciseId),
            "weak" => Order(rows, r => r.Progress.WeakItems, s.Desc).ThenBy(r => r.ExerciseId),
            "activity" => Order(rows, r => r.Progress.LastActivityAt, s.Desc).ThenBy(r => r.ExerciseId),
            "active" => Order(rows, r => r.Active, s.Desc).ThenBy(r => r.ExerciseId),
            _ => rows.OrderBy(r => r.OrderIndex).ThenBy(r => r.ExerciseId),
        };

    /// <summary>Checks whether this vocabulary exercise is relevant for the child under exactly this subject/chapter (leaf guard).</summary>
    public async Task<bool> IsRelevantExerciseAsync(int childId, int subjectId, int seriesUnitId, int exerciseId, CancellationToken ct = default) =>
        (await LoadRelevantAsync(childId, ct))
            .Any(r => r.SubjectId == subjectId && r.SeriesUnitId == seriesUnitId && r.ExerciseId == exerciseId);

    /// <summary>
    /// Item learning progress of the child for an exercise. Default: weakest first. Optional <paramref name="search"/>
    /// (word/translation) and sorting (<c>word</c>, <c>mastery</c>, <c>box</c>, <c>seen</c>, <c>activity</c>).
    /// Total count in the <c>X-Total-Count</c> header.
    /// </summary>
    public async Task<List<ItemProgressResponse>> ItemsAsync(int childId, int exerciseId, string? search,
        (string? Key, bool Desc) sort, HttpResponse response, int skip, int take, CancellationToken ct = default)
    {
        var joined =
            from p in db.ItemProgress.AsNoTracking().Where(p => p.ChildId == childId && p.ExerciseId == exerciseId)
            join v in db.Vocabularies.AsNoTracking() on p.VocabularyId equals v.Id into vj
            from v in vj.DefaultIfEmpty()
            select new { P = p, Word = v == null ? "" : v.Word, Translation = v == null ? "" : v.Translation };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            joined = joined.Where(x => x.Word.Contains(term) || x.Translation.Contains(term));
        }

        var ordered = (sort.Key?.ToLowerInvariant(), sort.Desc) switch
        {
            ("word", false) => joined.OrderBy(x => x.Word).ThenBy(x => x.P.ItemId),
            ("word", true) => joined.OrderByDescending(x => x.Word).ThenBy(x => x.P.ItemId),
            ("mastery", false) => joined.OrderBy(x => x.P.MasteryPercent).ThenBy(x => x.P.ItemId),
            ("mastery", true) => joined.OrderByDescending(x => x.P.MasteryPercent).ThenBy(x => x.P.ItemId),
            ("box", false) => joined.OrderBy(x => x.P.Box).ThenBy(x => x.P.ItemId),
            ("box", true) => joined.OrderByDescending(x => x.P.Box).ThenBy(x => x.P.ItemId),
            ("seen", false) => joined.OrderBy(x => x.P.SeenCount).ThenBy(x => x.P.ItemId),
            ("seen", true) => joined.OrderByDescending(x => x.P.SeenCount).ThenBy(x => x.P.ItemId),
            ("activity", false) => joined.OrderBy(x => x.P.LastAnswerAt).ThenBy(x => x.P.ItemId),
            ("activity", true) => joined.OrderByDescending(x => x.P.LastAnswerAt).ThenBy(x => x.P.ItemId),
            // Default: weakest first (as in the flat view).
            _ => joined.OrderBy(x => x.P.MasteryPercent).ThenByDescending(x => x.P.SeenCount).ThenBy(x => x.P.ItemId),
        };

        var rows = ordered.Select(x => new ItemRow(x.P.ItemId, x.P.ExerciseId, x.P.VocabularyId, x.Word, x.Translation,
            x.P.Box, x.P.MasteryPercent, x.P.SeenCount, x.P.CorrectCount, x.P.IntroducedAt, x.P.LastAnswerAt, x.P.LastCorrect));

        var page = await rows.ToPagedListAsync(response, skip, take, ct);
        return page.Select(r => new ItemProgressResponse(r.ItemId, r.ExerciseId, r.VocabularyId, r.Front, r.Back,
            r.Box, ItemProgress.MaxBox, r.MasteryPercent, r.SeenCount, r.CorrectCount,
            r.IntroducedAt, r.LastAnswerAt, r.LastCorrect, VocabLink.Path + r.VocabularyId)).ToList();
    }

    /// <summary>
    /// Loads the child's relevant learning progress <b>once</b> and returns an evaluator that computes the
    /// <see cref="MasteryRollup"/> for arbitrary catalog scopes in memory – without further DB queries
    /// (the foundation for evaluating learn goals across many goals).
    /// </summary>
    public async Task<ScopeEvaluator> LoadScopeEvaluatorAsync(int childId, CancellationToken ct = default)
    {
        var relevant = await LoadRelevantAsync(childId, ct);
        var (total, prog) = relevant.Count == 0
            ? (new Dictionary<int, int>(), new Dictionary<int, ProgRow>())
            : await LoadAggAsync(childId, relevant.Select(r => r.ExerciseId).ToList(), ct);
        return new ScopeEvaluator(relevant, total, prog);
    }

    /// <summary>Computes roll-ups for catalog scopes from a once-loaded learning-progress snapshot.</summary>
    public sealed class ScopeEvaluator
    {
        // Private types in the constructor → deliberately a private ctor; only the enclosing class creates the evaluator.
        private readonly IReadOnlyList<RelevantExercise> _relevant;
        private readonly IReadOnlyDictionary<int, int> _total;
        private readonly IReadOnlyDictionary<int, ProgRow> _prog;

        internal ScopeEvaluator(IReadOnlyList<RelevantExercise> relevant,
            IReadOnlyDictionary<int, int> total, IReadOnlyDictionary<int, ProgRow> prog)
        {
            _relevant = relevant;
            _total = total;
            _prog = prog;
        }

        /// <summary>Roll-up for a scope (subject, optionally chapter/exercise). Empty roll-up if nothing matches.</summary>
        public MasteryRollup For(int subjectId, int? seriesUnitId, int? exerciseId)
        {
            var parts = _relevant
                .Where(r => r.SubjectId == subjectId
                    && (seriesUnitId is null || r.SeriesUnitId == seriesUnitId)
                    && (exerciseId is null || r.ExerciseId == exerciseId))
                .Select(r => AggFor(r.ExerciseId, _total, _prog))
                .ToList();
            return parts.Count == 0 ? EmptyRollup : ToRollup(Combine(parts));
        }
    }
}
