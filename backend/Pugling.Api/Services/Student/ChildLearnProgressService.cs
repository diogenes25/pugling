using Microsoft.EntityFrameworkCore;
using Pugling.Api.Controllers;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Student;

/// <summary>
/// Kind-zentrische Drill-down-Sicht auf den Vokabel-Lernstand entlang der Katalog-Hierarchie
/// (Fach → Kapitel → Übung → Item). Ergänzt die flache <see cref="Controllers.Student.ChildVocabularyProgressController"/>-Sicht
/// um aggregierte Roll-ups je Ebene. Angezeigt wird die <b>relevante Menge</b>: alles, was dem Kind über einen
/// <see cref="StudyPlan"/> zugewiesen ist (auch mit 0 % Fortschritt, damit die Abdeckung sichtbar wird)
/// <b>plus</b> alles, wozu bereits Lernstand existiert (<see cref="ItemProgress"/>) – so verschwindet ein einmal
/// erarbeiteter Fortschritt nicht, wenn die Übung später abgehängt oder ihr Plan deaktiviert wird. Das Flag
/// <c>Active</c> unterscheidet „aktuell über einen aktiven Plan zugewiesen" von „nur noch historisch / inaktiv".
/// Nur Vokabelübungen sind item-getrackt (stabile <see cref="ExerciseItem.Id"/>), daher ist die Sicht bewusst
/// vokabel-skopiert. Der Fortschritt wird nicht neu berechnet, sondern aus <see cref="ItemProgress"/> gelesen
/// (fortgeschrieben vom <see cref="ItemProgressService"/> an den Bewertungspunkten).
/// </summary>
public class ChildLearnProgressService(PuglingDbContext db)
{
    /// <summary>Ab welcher Beherrschung (Prozent) ein Item als „schwach" gilt – geteilt mit der flachen Sicht.</summary>
    private const int WeakBelowPercent = ItemProgress.WeakBelowPercent;

    /// <summary>Aggregierter Lernstand über eine Menge Vokabel-Items (auf jeder Ebene identisch aufgebaut).</summary>
    public record MasteryRollup(
        int TotalItems, int IntroducedItems, int MasteredItems, int WeakItems,
        int AvgMasteryPercent, int SeenCount, int CorrectCount, int CorrectPercent, DateTime? LastActivityAt);

    /// <summary>Fortschritt eines Fachs. <paramref name="Active"/> = enthält ≥1 aktuell (über aktiven Plan) zugewiesene Übung.</summary>
    public record SubjectProgressResponse(int SubjectId, string Name, int ChapterCount, int ExerciseCount, bool Active, MasteryRollup Progress);

    /// <summary>Fortschritt eines Kapitels. <paramref name="Active"/> = enthält ≥1 aktuell zugewiesene Übung.</summary>
    public record ChapterProgressResponse(int ChapterId, string Name, int OrderIndex, int ExerciseCount, bool Active, MasteryRollup Progress);

    /// <summary>Fortschritt einer einzelnen Vokabelübung. <paramref name="Active"/> = aktuell über einen aktiven Plan zugewiesen.</summary>
    public record ExerciseProgressResponse(int ExerciseId, string Title, int OrderIndex, bool Active, MasteryRollup Progress);

    /// <summary>Item-Lernstand des Kindes (Front/Rückseite live aus dem Store); Form wie in der flachen Sicht.</summary>
    public record ItemProgressResponse(int ItemId, int ExerciseId, int VocabularyId, string Front, string Back,
        int Box, int MaxBox, int MasteryPercent, int SeenCount, int CorrectCount,
        DateOnly? IntroducedAt, DateTime? LastAnswerAt, bool? LastCorrect,
        [property: System.Text.Json.Serialization.JsonPropertyName("vocabulary")] string Vocabulary);

    // Eine für die Sicht relevante Vokabelübung (zugewiesen und/oder mit Fortschritt) samt Katalog-Koordinaten.
    // Active = von mindestens einem AKTIVEN Plan des Kindes referenziert.
    internal record RelevantExercise(int ExerciseId, string Title, int ExerciseOrder,
        int ChapterId, string ChapterName, int ChapterOrder, int SubjectId, bool Active);

    // Roh-Aggregat einer Item-Menge: summierbar, damit sich Übung → Kapitel → Fach ohne erneute DB-Abfrage rollt.
    private record Agg(int TotalItems, int Introduced, int Mastered, int Weak,
        int Seen, int Correct, int MasterySum, DateTime? LastActivity);

    // Pro-Übung aggregierte Fortschrittszeile aus ItemProgress (Introduced = Zeilen, also mind. einmal beantwortet).
    internal record ProgRow(int Introduced, int Mastered, int Weak, int Seen, int Correct, int MasterySum, DateTime? LastActivity);

    // EF-Projektion des Item-Blatts ohne den abgeleiteten Store-Link (im Speicher ergänzt).
    private record ItemRow(int ItemId, int ExerciseId, int VocabularyId, string Front, string Back,
        int Box, int MasteryPercent, int SeenCount, int CorrectCount,
        DateOnly? IntroducedAt, DateTime? LastAnswerAt, bool? LastCorrect);

    // Alle für das Kind relevanten Vokabelübungen: (über irgendeinen Plan zugewiesen) ∪ (hat Fortschritt),
    // je Übung eindeutig, mit Active-Flag (von einem aktiven Plan referenziert) und Katalog-Koordinaten.
    // Bewusst in mehreren einfachen DB-Abfragen + im Speicher gemergt (Distinct/Filter über das Projektions-Tupel
    // ist beim SQLite-Provider nicht übersetzbar); die relevante Menge je Kind ist klein.
    private async Task<List<RelevantExercise>> LoadRelevantAsync(int childId, CancellationToken ct)
    {
        // Aus Plänen: Übungs-Ids + ob ein AKTIVER Plan sie referenziert.
        var planRows = await (
            from pp in db.PlanPositions.AsNoTracking()
            where pp.StudyPlan!.ChildId == childId
            select new { pp.ExerciseId, pp.StudyPlan!.Active })
            .ToListAsync(ct);

        // Aus Fortschritt: Übungen mit Lernstand (überleben das Abhängen – ItemProgress kann keine gelöschte Übung überdauern).
        var progressIds = await db.ItemProgress.AsNoTracking()
            .Where(p => p.ChildId == childId)
            .Select(p => p.ExerciseId).Distinct().ToListAsync(ct);

        var activeIds = planRows.Where(r => r.Active).Select(r => r.ExerciseId).ToHashSet();
        var allIds = planRows.Select(r => r.ExerciseId).Concat(progressIds).Distinct().ToList();
        if (allIds.Count == 0) return [];

        // Katalog-Koordinaten (nur Vokabelübungen).
        var coords = await (
            from ex in db.Exercises.AsNoTracking()
            where allIds.Contains(ex.Id) && ex.Type == ExerciseType.Vocabulary
            join ch in db.Chapters.AsNoTracking() on ex.ChapterId equals ch.Id
            select new { ex.Id, ex.Title, ExOrder = ex.OrderIndex, ChId = ch.Id, ChName = ch.Name, ChOrder = ch.OrderIndex, ch.SubjectId })
            .ToListAsync(ct);

        return coords
            .Select(c => new RelevantExercise(c.Id, c.Title, c.ExOrder, c.ChId, c.ChName, c.ChOrder, c.SubjectId, activeIds.Contains(c.Id)))
            .ToList();
    }

    // Lädt Item-Gesamtzahl (inkl. ungeübter) und aggregierten Fortschritt je Übung für die gegebenen Übungs-Ids.
    private async Task<(Dictionary<int, int> Total, Dictionary<int, ProgRow> Prog)> LoadAggAsync(
        int childId, IReadOnlyList<int> exerciseIds, CancellationToken ct)
    {
        var total = await db.ExerciseItems.AsNoTracking()
            .Where(i => exerciseIds.Contains(i.ExerciseId))
            .GroupBy(i => i.ExerciseId)
            .Select(g => new { ExerciseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ExerciseId, x => x.Count, ct);

        // Aggregate als anonymen Typ ziehen (die direkte Projektion in einen Record-Konstruktor ist nicht übersetzbar),
        // erst im Speicher in den ProgRow abbilden.
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

    /// <summary>Leerer Roll-up (Scope ohne relevante Übungen / ohne Fortschritt).</summary>
    public static readonly MasteryRollup EmptyRollup = new(0, 0, 0, 0, 0, 0, 0, 0, null);

    // Ø-Beherrschung über die EINGEFÜHRTEN Items (nicht über alle), Trefferquote über gesehene Antworten.
    private static MasteryRollup ToRollup(Agg a) =>
        new(a.TotalItems, a.Introduced, a.Mastered, a.Weak,
            a.Introduced == 0 ? 0 : (int)Math.Round((double)a.MasterySum / a.Introduced),
            a.Seen, a.Correct, a.Seen == 0 ? 0 : (int)Math.Round(100.0 * a.Correct / a.Seen),
            a.LastActivity);

    // Abdeckung 0..1 für den Sortier-Key „coverage" (eingeführt / gesamt).
    private static double Coverage(MasteryRollup r) => r.TotalItems == 0 ? 0 : (double)r.IntroducedItems / r.TotalItems;

    private static IOrderedEnumerable<T> Order<T, TKey>(IEnumerable<T> src, Func<T, TKey> key, bool desc) =>
        desc ? src.OrderByDescending(key) : src.OrderBy(key);

    private static bool Matches(string text, string term) => text.Contains(term, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Alle relevanten Fächer des Kindes mit aggregiertem Vokabel-Fortschritt. Optional gefiltert nach
    /// <paramref name="search"/> (Fachname), <paramref name="active"/> (nur (in)aktive) und sortiert
    /// (<c>name</c> [Standard], <c>mastery</c>, <c>coverage</c>, <c>weak</c>, <c>activity</c>).
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
                g.Select(r => r.ChapterId).Distinct().Count(), g.Count(), g.Any(r => r.Active),
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

    /// <summary>Ein einzelnes relevantes Fach; <c>null</c>, wenn dem Kind darin nichts zugewiesen ist und kein Fortschritt existiert.</summary>
    public async Task<SubjectProgressResponse?> SubjectAsync(int childId, int subjectId, CancellationToken ct = default)
    {
        var relevant = (await LoadRelevantAsync(childId, ct)).Where(r => r.SubjectId == subjectId).ToList();
        if (relevant.Count == 0) return null;

        var (total, prog) = await LoadAggAsync(childId, relevant.Select(r => r.ExerciseId).ToList(), ct);
        var name = await db.Subjects.AsNoTracking().Where(s => s.Id == subjectId).Select(s => s.Name).FirstOrDefaultAsync(ct) ?? "";
        return new SubjectProgressResponse(subjectId, name,
            relevant.Select(r => r.ChapterId).Distinct().Count(), relevant.Count, relevant.Any(r => r.Active),
            ToRollup(Combine(relevant.Select(r => AggFor(r.ExerciseId, total, prog)))));
    }

    /// <summary>
    /// Kapitel eines Fachs mit Fortschritt; <c>null</c>, wenn das Fach nicht relevant ist. Filter/Sortierung wie
    /// bei den Fächern (Sort-Keys zusätzlich <c>order</c> [Standard, Kapitelreihenfolge]).
    /// </summary>
    public async Task<List<ChapterProgressResponse>?> ChaptersAsync(int childId, int subjectId, string? search,
        (string? Key, bool Desc) sort, bool? active, CancellationToken ct = default)
    {
        var relevant = (await LoadRelevantAsync(childId, ct)).Where(r => r.SubjectId == subjectId).ToList();
        if (relevant.Count == 0) return null;

        var (total, prog) = await LoadAggAsync(childId, relevant.Select(r => r.ExerciseId).ToList(), ct);
        var rows = relevant.GroupBy(r => new { r.ChapterId, r.ChapterName, r.ChapterOrder })
            .Select(g => new ChapterProgressResponse(g.Key.ChapterId, g.Key.ChapterName, g.Key.ChapterOrder, g.Count(), g.Any(r => r.Active),
                ToRollup(Combine(g.Select(r => AggFor(r.ExerciseId, total, prog))))))
            .AsEnumerable();

        if (active is { } act) rows = rows.Where(r => r.Active == act);
        if (!string.IsNullOrWhiteSpace(search)) rows = rows.Where(r => Matches(r.Name, search.Trim()));

        return SortChapters(rows, sort).ToList();
    }

    private static IEnumerable<ChapterProgressResponse> SortChapters(IEnumerable<ChapterProgressResponse> rows, (string? Key, bool Desc) s) =>
        s.Key?.ToLowerInvariant() switch
        {
            "name" => Order(rows, r => r.Name, s.Desc).ThenBy(r => r.ChapterId),
            "mastery" => Order(rows, r => r.Progress.AvgMasteryPercent, s.Desc).ThenBy(r => r.ChapterId),
            "coverage" => Order(rows, r => Coverage(r.Progress), s.Desc).ThenBy(r => r.ChapterId),
            "weak" => Order(rows, r => r.Progress.WeakItems, s.Desc).ThenBy(r => r.ChapterId),
            "activity" => Order(rows, r => r.Progress.LastActivityAt, s.Desc).ThenBy(r => r.ChapterId),
            _ => rows.OrderBy(r => r.OrderIndex).ThenBy(r => r.ChapterId),
        };

    /// <summary>
    /// Relevante Vokabelübungen eines Kapitels mit Fortschritt je Übung; <c>null</c>, wenn das Kapitel nicht relevant ist.
    /// Filter/Sortierung wie bei Kapiteln (Sort-Keys zusätzlich <c>title</c>, <c>active</c>; Standard <c>order</c>).
    /// </summary>
    public async Task<List<ExerciseProgressResponse>?> ExercisesAsync(int childId, int subjectId, int chapterId, string? search,
        (string? Key, bool Desc) sort, bool? active, CancellationToken ct = default)
    {
        var relevant = (await LoadRelevantAsync(childId, ct))
            .Where(r => r.SubjectId == subjectId && r.ChapterId == chapterId).ToList();
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

    /// <summary>Prüft, ob diese Vokabelübung unter genau diesem Fach/Kapitel für das Kind relevant ist (Blatt-Guard).</summary>
    public async Task<bool> IsRelevantExerciseAsync(int childId, int subjectId, int chapterId, int exerciseId, CancellationToken ct = default) =>
        (await LoadRelevantAsync(childId, ct))
            .Any(r => r.SubjectId == subjectId && r.ChapterId == chapterId && r.ExerciseId == exerciseId);

    /// <summary>
    /// Item-Lernstand des Kindes für eine Übung. Standard: schwächste zuerst. Optional <paramref name="search"/>
    /// (Wort/Übersetzung) und Sortierung (<c>word</c>, <c>mastery</c>, <c>box</c>, <c>seen</c>, <c>activity</c>).
    /// Gesamtzahl im Header <c>X-Total-Count</c>.
    /// </summary>
    public async Task<List<ItemProgressResponse>> ItemsAsync(int childId, int exerciseId, string? search,
        (string? Key, bool Desc) sort, HttpResponse response, int skip, int take, CancellationToken ct = default)
    {
        var joined =
            from p in db.ItemProgress.AsNoTracking().Where(p => p.ChildId == childId && p.ExerciseId == exerciseId)
            join v in db.Vocabulary.AsNoTracking() on p.VocabularyId equals v.Id into vj
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
            // Standard: schwächste zuerst (wie in der flachen Sicht).
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
    /// Lädt den relevanten Lernstand des Kindes <b>einmal</b> und liefert einen Evaluator, der daraus den
    /// <see cref="MasteryRollup"/> für beliebige Katalog-Scopes im Speicher berechnet – ohne erneute DB-Abfragen
    /// (Grundlage der Lernziel-Auswertung über viele Ziele hinweg).
    /// </summary>
    public async Task<ScopeEvaluator> LoadScopeEvaluatorAsync(int childId, CancellationToken ct = default)
    {
        var relevant = await LoadRelevantAsync(childId, ct);
        var (total, prog) = relevant.Count == 0
            ? (new Dictionary<int, int>(), new Dictionary<int, ProgRow>())
            : await LoadAggAsync(childId, relevant.Select(r => r.ExerciseId).ToList(), ct);
        return new ScopeEvaluator(relevant, total, prog);
    }

    /// <summary>Berechnet Roll-ups für Katalog-Scopes aus einem einmal geladenen Lernstand-Snapshot.</summary>
    public sealed class ScopeEvaluator
    {
        // Private-Typen im Konstruktor → bewusst privater Ctor; nur die umschließende Klasse erzeugt den Evaluator.
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

        /// <summary>Roll-up für einen Scope (Fach, optional Kapitel/Übung). Leerer Roll-up, wenn nichts passt.</summary>
        public MasteryRollup For(int subjectId, int? chapterId, int? exerciseId)
        {
            var parts = _relevant
                .Where(r => r.SubjectId == subjectId
                    && (chapterId is null || r.ChapterId == chapterId)
                    && (exerciseId is null || r.ExerciseId == exerciseId))
                .Select(r => AggFor(r.ExerciseId, _total, _prog))
                .ToList();
            return parts.Count == 0 ? EmptyRollup : ToRollup(Combine(parts));
        }
    }
}
