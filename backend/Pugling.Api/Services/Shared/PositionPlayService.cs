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

    /// <summary>
    /// The daily dose in force: the position's <see cref="PlanPosition.NewItemsPerDay"/>, but 0 while its exercise grades
    /// as a set (an unordered list). Such an exercise accepts any entry not yet named, and a dose would hide most of them -
    /// a right answer outside the day's portion would count as wrong. Assigning rejects the combination; this covers the
    /// list a creator switches to any order after it was assigned (B-478).
    /// </summary>
    public int DailyDose(PlanPosition pos) =>
        pos.NewItemsPerDay > 0 && pos.Exercise is { } exercise && TypeOf(exercise)?.GradesAsSet(ConfigOf(exercise)) == true
            ? 0
            : pos.NewItemsPerDay;

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

    /// <summary>
    /// The stage a never-seen card is shown at first (B-419): the type's free display stage, or <c>null</c> for a
    /// type that has none - there the card is played at the day's stage as before, because there is no way to
    /// show it without grading it.
    /// </summary>
    public static int? IntroductionStage(IExerciseType type) =>
        type.StageOptions.Where(o => type.IsDisplayOnlyStage(o.Value)).Select(o => (int?)o.Value).FirstOrDefault();

    /// <summary>
    /// Is this card an introduction (B-419)? The child must see an atom once with its solution before it is asked
    /// for it, so first contact is a property of the card, not of the day: an atom without an introduction date is
    /// shown at <see cref="IntroductionStage"/> in a learn round, whatever the schedule says. The info mode is left
    /// out because it records nothing - every card would stay "new" there forever. A day that is already a display
    /// stage needs no second rule.
    /// </summary>
    public static bool IsIntroduction(IExerciseType type, int dayStage, PlayMode mode, bool introduced) =>
        mode != PlayMode.Info && !introduced && !type.IsDisplayOnlyStage(dayStage)
            && IntroductionStage(type) is not null;

    /// <summary>
    /// Box and introduction date of every introduced item of the position, by item index - the one lookup the card
    /// projection needs: presence tells an introduction from a regular card (<see cref="IsIntroduction"/>), box and date
    /// pick the card's rung (<see cref="CardStage"/>).
    /// </summary>
    public async Task<Dictionary<int, CardProgress>> IntroducedCardsAsync(int positionId, CancellationToken ct) =>
        await db.PositionItemProgress.AsNoTracking()
            .Where(p => p.PlanPositionId == positionId && p.IntroducedAt != null)
            .ToDictionaryAsync(p => p.ItemIndex, p => new CardProgress(p.Box, p.IntroducedAt!.Value), ct);

    /// <summary>
    /// The stage one card is played at. A never-seen card is shown (B-419); an introduced one climbs the type's ladder
    /// with its box up to the day's stage (B-476). The ladder needs a box that moves, so it holds only in a learn round
    /// on a Leitner position - without Leitner the card would stay on the lowest rung for good, and the info mode
    /// records nothing. A card in the position's top box is asked at the target whatever the ladder says: with a
    /// <see cref="PlanPosition.MaxBox"/> below the ladder's top rung it could never climb further, and would be
    /// stuck on a rung below the target for good. The ladder also ends by the calendar (<see cref="LadderDays"/>), in
    /// practice as in the test: a word the test asks in writing must have been asked in writing here as well.
    /// </summary>
    public static int CardStage(IExerciseType type, PlanPosition pos, int dayStage, PlayMode mode, CardProgress? card,
        DateOnly day)
    {
        if (IsIntroduction(type, dayStage, mode, card is not null)) return IntroductionStage(type)!.Value;
        if (mode != PlayMode.Lern || !pos.UseLeitner || card is not { } c || c.Box >= pos.MaxBox) return dayStage;
        if (c.IntroducedAt.AddDays(LadderDays) <= day) return dayStage;
        return type.CardStage(dayStage, c.Box);
    }

    /// <summary>
    /// Days after its introduction from which a word is asked at the target in practice and test alike, whatever its
    /// box (B-476, decision 7). The father's choice, and the honest path's length: with the default intervals a word
    /// practised every day is first asked in writing on day 7 (shown and turned over on day 1, chosen on day 3, typed
    /// on day 7), so the deadline only meets a word the child keeps low - by not practising or by failing on purpose.
    /// It counts from the actual introduction, not from a day the word was due: the daily dose never catches up a day
    /// not played, so a due date would let a week of missed days end the ladder of every later word before the child
    /// ever met it. Each word so gets at most one week of ladder; missed days are the malus' to punish.
    /// </summary>
    public const int LadderDays = 7;

    /// <summary>
    /// The stage one item is examined at in the test (B-476): the ladder of practice, but never below a checked rung -
    /// a self-assessment is a claim, and the duty is "passed under check". A rung the child would only judge itself is
    /// raised box by box to the first checked one (for vocabulary: the choice). An item not introduced yet counts as
    /// box 1 introduced today - the child's test introduces it (<see cref="IntroduceAsync"/>). When the target itself is
    /// unchecked there is nothing to raise to, and without Leitner no box moves - both keep the target for every item.
    /// <para>
    /// The box is the child's to move - not practising keeps a word in box 1, and so does answering wrong on purpose.
    /// So the ladder ends by the calendar too: <see cref="LadderDays"/> days after <paramref name="introducedOn"/> a
    /// word is examined at the target whatever its box, and no behaviour of the child stretches the easier form
    /// beyond that.
    /// </para>
    /// </summary>
    public static int TestItemStage(IExerciseType type, PlanPosition pos, int target, int? box, DateOnly introducedOn,
        DateOnly day)
    {
        if (!pos.UseLeitner || !type.IsTypedStage(target)) return target;
        if (introducedOn.AddDays(LadderDays) <= day) return target;
        for (var b = Math.Max(1, box ?? 1); b < pos.MaxBox; b++)
        {
            var stage = type.CardStage(target, b);
            if (type.IsTypedStage(stage)) return stage;
        }
        return target;
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

        // The dose only shapes a round that is played for the record; free practice (info) introduces nothing.
        if (dueOnly && DailyDose(pos) is > 0 and var dose)
            due = WithinDailyDose(due, dose, progress.Values.Count(p => p.IntroducedAt == day));

        return OrderIndices(due, strategy);
    }

    /// <summary>
    /// Applies the daily dose (B-04): of the never-shown contents only as many pass as the day still has room for,
    /// lowest index first - the order of the book. What was introduced earlier today counts against the dose, so a
    /// second round on the same day brings no further new ones. Known contents pass untouched.
    /// </summary>
    private static List<(int Index, PositionItemProgress? Prog)> WithinDailyDose(
        IEnumerable<(int Index, PositionItemProgress? Prog)> due, int newItemsPerDay, int introducedToday)
    {
        var room = Math.Max(0, newItemsPerDay - introducedToday);
        var result = new List<(int Index, PositionItemProgress? Prog)>();
        foreach (var x in due.OrderBy(x => x.Index))
        {
            if (x.Prog?.IntroducedAt is not null) result.Add(x);
            else if (room-- > 0) result.Add(x);
        }
        return result;
    }

    /// <summary>
    /// The contents a test checks on <paramref name="day"/> when the position carries a daily dose (B-04): what the
    /// child met today - introduced, reviewed or due - plus the new contents the day's dose still has room for. A
    /// test over everything introduced would grow with every day of the plan until the duty cannot be met in one
    /// sitting.
    /// <para>
    /// The open rest of the dose is the anti-cheat half: a child that skips practice is asked the day's new contents
    /// cold, and because the caller introduces them (<see cref="IntroduceAsync"/>), they stay due until they are
    /// practised. Skipping practice makes the test longer every day, never shorter.
    /// </para>
    /// <para>
    /// On a day with nothing met and nothing new left, the weakest known contents are checked, twice the dose at
    /// most - the duty stays meetable after the pool is through.
    /// </para>
    /// </summary>
    public static IReadOnlyList<int> DailyTestPortion(int newItemsPerDay, int poolSize,
        IEnumerable<PositionItemProgress> progress, DateOnly day)
    {
        var introduced = progress.Where(p => p.IntroducedAt is not null && p.ItemIndex < poolSize).ToList();
        var known = introduced.Select(p => p.ItemIndex).ToHashSet();
        var room = Math.Max(0, newItemsPerDay - introduced.Count(p => p.IntroducedAt == day));

        var portion = introduced
            .Where(p => p.IntroducedAt == day || IsDue(p, day)
                || (p.LastReviewedAt is { } at && DateOnly.FromDateTime(at) == day))
            .Select(p => p.ItemIndex)
            .Concat(Enumerable.Range(0, poolSize).Where(i => !known.Contains(i)).Take(room))
            .ToList();
        if (portion.Count > 0) return portion;

        return [.. introduced.OrderBy(p => p.Box).ThenBy(p => p.LastReviewedAt).ThenBy(p => p.ItemIndex)
            .Take(newItemsPerDay * 2).Select(p => p.ItemIndex)];
    }

    /// <summary>
    /// Marks the given contents as introduced on <paramref name="day"/> and due at once - for a child's test that asks
    /// never-shown contents (under a dose the portion at the start, <see cref="DailyTestPortion"/>; otherwise each word
    /// as the child answers it). The caller saves.
    /// </summary>
    public async Task IntroduceAsync(int positionId, IEnumerable<int> itemIndices, DateOnly day, CancellationToken ct)
    {
        foreach (var index in itemIndices)
        {
            var prog = await ProgressForAsync(positionId, index, ct);
            if (prog.IntroducedAt is not null) continue;
            prog.IntroducedAt = day;
            prog.DueOn ??= day;
        }
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
            if (grader.Matches(given, items[index])) return index;
        }
        return null;
    }

    /// <summary>
    /// The representation of a content atom permitted per stage as a card/test item (anti-cheat in one place):
    /// typed stages withhold the solution (<c>Reveal</c>), display/self-assessment reveals it - together with
    /// every equally valid alternative (<c>RevealAlternatives</c>), so the child does not mark itself wrong;
    /// letter boxes give the length, the listening stage the audio source, multiple choice the options.
    /// Shared by practice card (<c>PracticeCard</c>) and test item (<c>TestItem</c>) – the latter drops the
    /// image facets, it renders no image.
    /// </summary>
    public static (string? Hint, int? AnswerLength, string? Reveal, IReadOnlyList<string>? Choices,
        string? AudioUrl, string? ImageUrl, string? ImageAlt, int? GapIndex, string? Prompt, string? Passage,
        bool AnyOrder, IReadOnlyList<string>? RevealAlternatives, IReadOnlyList<WordPair>? Decoding,
        string? AnswerPattern, IReadOnlyList<string>? LetterTiles)
        CardFacets(string configJson, IReadOnlyList<ContentItem> items, ContentItem item, IExerciseType type,
            int stage, bool typed, int seed)
    {
        // The scramble stage (B-05): the type names the word, the shuffle needs the session's or attempt's id as
        // seed - frozen per session, new per session - and the item index, so two cards of one round differ.
        var tileSource = typed ? type.LetterTileSource(item, stage) : null;
        var (letterBoxLength, audioUrl, imageUrl, answerPattern) = type.StageFacets(item, stage);
        return (
            typed ? item.Hint : null,
            letterBoxLength,
            typed ? null : item.Answer,
            // Options only where the solution is withheld. Today every type that offers a pool does so on a
            // typed stage anyway, so this changes nothing - but it makes the rule structural instead of a
            // coincidence of three unrelated types. Without it, one added display stage would put the answer
            // next to the options and no test would notice.
            typed ? type.Choices(configJson, items, item, stage) : null,
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
            typed && type.GradesAsSet(configJson),
            // The equally valid answers next to the revealed one - same anti-cheat boundary as `Reveal`, because
            // an alternative is no bigger secret than the primary answer on a stage that shows it anyway.
            // Without them the child grades ITSELF as wrong for a correct answer ("sehr groß" against a revealed
            // "riesig"), which is the very damage B-65 fixed for the typed stages.
            // Index 0 is the primary answer by contract of the `Accepted(...)` helper that builds the list.
            typed || item.AcceptedAnswers.Count <= 1 ? null : item.AcceptedAnswers.Skip(1).ToList(),
            // The word-for-word decoding, unconditional like the passage: it is the method the exercise is named
            // after, not the solution to withhold. Without it a Birkenbihl card is an ordinary translation card.
            item.Decoding,
            // The letter-box mask (B-66) - null outside that stage, same condition as `letterBoxLength`.
            answerPattern,
            tileSource is null ? null : StageMechanics.ScrambleLetters(tileSource, unchecked((seed * 397) ^ item.Index)));
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

/// <summary>The learning state one introduced card's stage depends on (B-476): its Leitner box and when it was introduced.</summary>
public readonly record struct CardProgress(int Box, DateOnly IntroducedAt);
