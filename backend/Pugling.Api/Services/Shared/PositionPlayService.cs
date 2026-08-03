using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// Position-based learning engine (new model): selects the content due today for a
/// <see cref="PlanPosition"/>, schedules its Leitner box progress (<see cref="PositionItemProgress"/>)
/// and resolves the test stage. The content comes from the exercise config via the
/// <see cref="ExerciseContentProvider"/>, grading stays with the <see cref="AnswerGrader"/>.
/// The counterpart to the former plan-wide schedule/progress services, but per exercise
/// instead of per plan – goals and points now hang off the position.
/// </summary>
public class PositionPlayService(PuglingDbContext db, ExerciseContentResolver content, ExerciseTypeRegistry registry)
{
    /// <summary>
    /// The <see cref="IExerciseType"/> behind an exercise (for stage/facet rules), or <c>null</c> if its
    /// type key is unknown. Null-safe like the content resolution (<see cref="ExerciseContentResolver"/>),
    /// so a data integrity error reaches the caller as a clean <see cref="ApiErrors.UnknownExerciseType"/>
    /// instead of an unhandled exception.
    /// </summary>
    public IExerciseType? TypeOf(Exercise exercise) => registry.ByKey(exercise.Type);

    /// <summary>Default Leitner intervals in days (index = box; index 0 unused).</summary>
    private static readonly int[] DefaultBoxIntervalDays = [0, 1, 2, 4, 7, 14];

    /// <summary>Leitner intervals of the position (own, otherwise default).</summary>
    public IReadOnlyList<int> BoxIntervals(PlanPosition pos) =>
        pos.BoxIntervalDays is { Count: > 1 } custom ? custom : DefaultBoxIntervalDays;

    /// <summary>
    /// The content items of this position's exercise (store-resolved for referenced vocabulary items).
    /// <paramref name="childId"/> unlocks image selection – only the practice card passes it, because only
    /// it shows an image. Everything else skips the selection, and not merely to save a query: selecting
    /// <b>freezes</b> the child's motif for good (see <c>MediaSelector</c>), so a path that renders no image
    /// would silently decide what the child sees later.
    /// </summary>
    public async Task<IReadOnlyList<ContentItem>> ItemsOfAsync(PlanPosition pos, int? childId = null,
        CancellationToken ct = default) =>
        pos.Exercise is { } ex ? await content.ItemsOfAsync(ex, childId, ct) : [];

    /// <summary>
    /// May the child play this plan today (practice/test)? Only an active plan within its
    /// runtime is playable – this way the child can't pick an easy or expired plan for convenient
    /// point farming (anti-cheating). The supervisor is exempt from this (preview/backfill).
    /// </summary>
    public static bool PlanPlayableForChild(StudyPlan plan, DateOnly today) =>
        plan.Active && plan.StartDate <= today && today <= plan.EndDate;

    /// <summary>
    /// Test stage of the position applicable for a given day: study plan (if set) → position override →
    /// exercise default → method default. Enforced server-side (not selectable by the client).
    /// </summary>
    public static int StageForDay(PlanPosition pos, StudyPlan plan, DateOnly day, IExerciseType type)
    {
        var dayNumber = day.DayNumber - plan.StartDate.DayNumber + 1;
        var step = pos.StageSchedule?
            .Where(s => s.DayNumber <= dayNumber)
            .OrderByDescending(s => s.DayNumber)
            .FirstOrDefault();
        return step?.Stage ?? pos.Stage ?? pos.Exercise?.DefaultStage ?? type.DefaultStage;
    }

    /// <summary>Number of content items used by the position (override, exercise default, otherwise all available).</summary>
    public int PoolSize(PlanPosition pos, int available) =>
        (pos.ItemCount ?? pos.Exercise?.DefaultItemCount) is > 0 and var count ? Math.Min(count, available) : available;

    /// <summary>
    /// Selects the item indices due today: limited to the pool (<see cref="PlanPosition.ItemCount"/>),
    /// filtered by <see cref="ItemScope"/> (new/old/all) and – for Leitner – only the due ones
    /// (never seen counts as due). <paramref name="strategy"/> determines the order (default =
    /// weakest first = previous behavior). The progress is loaded for this, but NOT newly created
    /// (that only happens when grading in <see cref="ApplyReview"/>).
    /// </summary>
    public async Task<IReadOnlyList<int>> DueItemIndicesAsync(PlanPosition pos, DateOnly day,
        PracticeOrder strategy = PracticeOrder.WeakestFirst, bool dueOnly = true, CancellationToken ct = default)
    {
        var poolSize = PoolSize(pos, (await ItemsOfAsync(pos, ct: ct)).Count);
        if (poolSize == 0) return [];

        var progress = await db.PositionItemProgress
            .Where(p => p.PlanPositionId == pos.Id && p.ItemIndex < poolSize)
            .ToDictionaryAsync(p => p.ItemIndex, ct);

        var due = Enumerable.Range(0, poolSize)
            .Select(i => (Index: i, Prog: progress.GetValueOrDefault(i)))
            .Where(x => ScopeMatch(pos.Scope, x.Prog) && (!dueOnly || !pos.UseLeitner || IsDue(x.Prog, day)));

        return OrderIndices(due, strategy);
    }

    /// <summary>
    /// Advances a cursor across the frozen order (<paramref name="order"/>) past item indices
    /// that have been removed since the start (out-of-range relative to <paramref name="itemCount"/>). Shared by
    /// the practice and the test cursor, so the skip rule lives in exactly one place.
    /// </summary>
    public static int SkipRemoved(IReadOnlyList<int> order, int cursor, int itemCount)
    {
        while (cursor < order.Count && order[cursor] >= itemCount) cursor++;
        return cursor;
    }

    /// <summary>
    /// The raw exercise config – what a type needs for facets it cannot derive from the atoms (the cloze
    /// word bank). Takes the <see cref="Exercise"/> itself, not the position: a <c>pos.Exercise?.…</c> would
    /// swallow a forgotten <c>Include</c> and cost the word bank in silence, the same failure mode the
    /// imagery rules forbid for <c>childId</c>. This way the compiler asks the question.
    /// </summary>
    public static string ConfigOf(Exercise exercise) => exercise.ConfigJson;

    /// <summary>
    /// The entry a given answer credits in a <b>set-graded</b> exercise: the first candidate whose accepted
    /// answers match, or <c>null</c> if none does (a wrong answer, or one already credited – the caller keeps
    /// those out of <paramref name="candidates"/>). Candidates are walked in ascending index order, so two
    /// entries accepting the same text always credit the same one; a set must still grade deterministically.
    /// <para>
    /// Lives here rather than in a controller because both play paths need it, and the plain per-index
    /// comparison it replaces already stands written out four times.
    /// </para>
    /// </summary>
    public static int? MatchOpenEntry(IReadOnlyList<ContentItem> items, IEnumerable<int> candidates,
        string? given, AnswerGrader grader)
    {
        foreach (var index in candidates.Where(i => i >= 0 && i < items.Count).Order())
        {
            if (items[index].AcceptedAnswers.Any(a => grader.Matches(given, a))) return index;
        }
        return null;
    }

    /// <summary>
    /// The representation of a content atom permitted per stage as a card/test item (anti-cheat in one place):
    /// typed stages withhold the solution (<c>Reveal</c>), display/self-assessment reveals it;
    /// letter boxes give the length, the listening stage the audio source, multiple choice the options.
    /// Shared by practice card (<c>PracticeCard</c>) and test item (<c>TestItem</c>) – the latter drops the
    /// image facets, it renders no image.
    /// </summary>
    public static (string? Hint, int? AnswerLength, string? Reveal, IReadOnlyList<string>? Choices,
        string? AudioUrl, string? ImageUrl, string? ImageAlt, int? GapIndex, string? Prompt, string? Passage,
        bool AnyOrder)
        CardFacets(string configJson, IReadOnlyList<ContentItem> items, ContentItem item, IExerciseType type,
            int stage, bool typed)
    {
        var (letterBoxLength, audioUrl, imageUrl) = type.StageFacets(item, stage);
        return (
            typed ? item.Hint : null,
            letterBoxLength,
            typed ? null : item.Answer,
            type.Choices(configJson, items, item, stage),
            audioUrl,
            imageUrl,
            // The alt text follows the image: no image, no alt text - otherwise the description ("a unicorn is
            // running") would leak on a typed stage exactly what the image would have given away.
            imageUrl is null ? null : item.ImageAlt,
            // Which placeholder of a shared text is being asked. Unlike the facets above this is no anti-cheat
            // decision - it is the address of the atom, and withholding it makes the card unanswerable.
            item.GapIndex,
            // The prompt is withheld exactly where the recording replaces it (the type decides, see
            // IExerciseType.AudioReplacesPrompt) - and only when a recording actually arrived, so a missing
            // audio source never leaves the card blank on both counts. Whitespace counts as missing: the
            // audio URL is a free-text field, and "" would satisfy a null check while playing nothing.
            type.AudioReplacesPrompt(stage) && !string.IsNullOrWhiteSpace(audioUrl) ? null : item.Prompt,
            // What the question is about, unabridged: reading text, grammar instruction. No anti-cheat rule
            // applies - it is the material, not the solution.
            item.Passage,
            // Whether any not-yet-named answer counts. Like the gap index this is an address, not a secret:
            // the rule lives in the config the child never sees, so withholding it makes the card a guess.
            // The `typed` proviso is the one both graders apply - it must not promise a rule on a stage that
            // is self-assessed and therefore still graded card by card.
            typed && type.GradesAsSet(configJson));
    }

    /// <summary>
    /// Orders a set (index, progress) according to the chosen strategy and returns the indices.
    /// Used when freezing the session/exam order; the randomness (Random/NewestWeighted)
    /// therefore falls only <b>once</b> at the start, not on every call.
    /// </summary>
    public static IReadOnlyList<int> OrderIndices(
        IEnumerable<(int Index, PositionItemProgress? Prog)> items, PracticeOrder strategy)
    {
        var list = items.ToList();
        return strategy switch
        {
            PracticeOrder.Serial => list.OrderBy(x => x.Index).Select(x => x.Index).ToList(),
            PracticeOrder.Random => list.OrderBy(_ => Random.Shared.Next()).Select(x => x.Index).ToList(),
            PracticeOrder.NewestWeighted => WeightedNewest(list),
            _ => list.OrderBy(x => x.Prog?.Box ?? 1).ThenBy(x => x.Index).Select(x => x.Index).ToList(),
        };
    }

    /// <summary>
    /// Weighted draw without replacement: most recently introduced (or never introduced) content items receive
    /// significantly higher weight (rank weight 1, 1/2, 1/3 …), so they are placed near the front with high
    /// probability – the "newest first, but not rigid" rule.
    /// </summary>
    private static List<int> WeightedNewest(List<(int Index, PositionItemProgress? Prog)> items)
    {
        // Rank by introduction date descending (null = brand new = highest rank), then the index as a tiebreaker.
        var ranked = items
            .OrderByDescending(x => x.Prog?.IntroducedAt ?? DateOnly.MaxValue)
            .ThenBy(x => x.Index)
            .ToList();
        var pool = ranked.Select((x, rank) => (x.Index, Weight: 1.0 / (rank + 1))).ToList();

        var result = new List<int>(pool.Count);
        while (pool.Count > 0)
        {
            var total = pool.Sum(p => p.Weight);
            var roll = Random.Shared.NextDouble() * total;
            var i = 0;
            for (; i < pool.Count - 1; i++)
            {
                roll -= pool[i].Weight;
                if (roll <= 0) break;
            }
            result.Add(pool[i].Index);
            pool.RemoveAt(i);
        }
        return result;
    }

    private static bool ScopeMatch(ItemScope scope, PositionItemProgress? prog) => scope switch
    {
        ItemScope.New => prog?.IntroducedAt is null,
        ItemScope.Old => prog?.IntroducedAt is not null,
        _ => true,
    };

    // Due when never seen (no progress) or the due date has been reached.
    private static bool IsDue(PositionItemProgress? prog, DateOnly day) =>
        prog is null || prog.DueOn is null || prog.DueOn <= day;

    /// <summary>Retrieves the progress record of a position's content atom, or creates it (tracked) if missing.</summary>
    public async Task<PositionItemProgress> ProgressForAsync(int positionId, int itemIndex,
        CancellationToken ct = default)
    {
        var prog = await db.PositionItemProgress
            .FirstOrDefaultAsync(p => p.PlanPositionId == positionId && p.ItemIndex == itemIndex, ct);
        if (prog is null)
        {
            prog = new PositionItemProgress { PlanPositionId = positionId, ItemIndex = itemIndex };
            db.PositionItemProgress.Add(prog);
        }
        return prog;
    }

    /// <summary>
    /// Records a Leitner review on the progress: correct → one box higher (longer
    /// interval), incorrect → back to box 1 and due again immediately. The caller saves.
    /// </summary>
    public void ApplyReview(PlanPosition pos, PositionItemProgress prog, bool correct, DateOnly today, DateTime nowUtc)
    {
        var intervals = BoxIntervals(pos);
        prog.ReviewCount++;
        prog.LastReviewedAt = nowUtc;

        if (correct)
        {
            prog.Box = Math.Min(pos.MaxBox, Math.Max(1, prog.Box) + 1);
            prog.DueOn = today.AddDays(intervals[Math.Min(prog.Box, intervals.Count - 1)]);
        }
        else
        {
            prog.Box = 1;
            prog.DueOn = today; // same day: practice again right away
        }
    }
}
