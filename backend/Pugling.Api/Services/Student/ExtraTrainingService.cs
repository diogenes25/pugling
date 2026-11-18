using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Student;

/// <summary>
/// The bonus training: extra exercises on the child's current topic that pay coins beyond the day's duty.
/// <para>
/// Three rules keep it from becoming a coin farm. It opens only once today's daily goals are met (B-535; all goals in a
/// plan without daily ones) - the path comes first.
/// Every right card pays - a claimed self-assessment a token 1, choice 1, typed 3 (B-657) - capped per day. And every
/// further completed run of the same exercise on the same day halves its worth - 100, 50, 25, then 0 % (B-538); the next
/// day it pays in full again, because daily repetition is the core of learning words. A vocabulary exercise also asks
/// harder with every completed run - self-assessment, then choice, then typed for good; any other exercise plays its
/// checked stage on every run.
/// </para>
/// <para>
/// The topic is the set of textbook units the child's playable plans draw from, plus the current unit of its
/// textbooks. A non-vocabulary exercise that is a position of one of the child's plans is left out: the child knows
/// it from the path, and replaying it would pay for memory, not for learning.
/// </para>
/// </summary>
public class ExtraTrainingService(PuglingDbContext db, PositionProgressService progress, ExerciseContentResolver content,
    ExercisePreviewService preview, ExerciseTypeRegistry registry, IOptions<ExtraTrainingOptions> options,
    TimeProvider time)
{
    private readonly ExtraTrainingOptions opts = options.Value;

    /// <summary>A choice needs distractors: with fewer atoms the options would give the answer away.</summary>
    private const int MinItemsForChoice = 4;

    /// <summary>Lock reason: the child has no playable plan, so there is neither a duty nor a topic.</summary>
    public const string LockNoPlan = "no_plan";
    /// <summary>Lock reason: today's daily goals (or, when no active plan has daily goals, all goals) are not met yet.</summary>
    public const string LockDutyOpen = "duty_open";

    /// <summary>Outcome of a start or submit: either the value or the error to answer with.</summary>
    public record Outcome<T>(T? Value, ApiError? Error = null, string? Detail = null) where T : class
    {
        /// <summary>A failed outcome answered with <paramref name="error"/>.</summary>
        public static Outcome<T> Fail(ApiError error, string detail) => new(null, error, detail);
    }

    private DateOnly Today => DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);

    /// <summary>The child's bonus training for today: whether it is open, the coins so far and the exercise pool.</summary>
    public async Task<ExtraTrainingOverview> OverviewAsync(int childId, CancellationToken ct)
    {
        var today = Today;
        var gate = await GateAsync(childId, today, ct);
        var coinsToday = await CoinsTodayAsync(childId, today, ct);
        var cap = CapOf(await db.Children.AsNoTracking().Where(c => c.Id == childId).Select(c => c.BonusDailyCoinCap).FirstAsync(ct));
        var pool = gate.Plans.Count == 0 ? [] : await PoolAsync(childId, gate.Plans, ct);
        return new ExtraTrainingOverview(gate.Unlocked, gate.LockReason, gate.Outstanding, coinsToday, cap,
            [.. pool.Select(p => p.Row)], gate.RewardCadence);
    }

    /// <summary>
    /// Starts a run of <paramref name="exerciseId"/>, or resumes the open one. The form is frozen on the run, so a
    /// resumed run asks the same questions in the same form.
    /// </summary>
    public async Task<Outcome<ExtraRunResponse>> StartAsync(int childId, int exerciseId, CancellationToken ct)
    {
        var today = Today;
        var gate = await GateAsync(childId, today, ct);
        if (!gate.Unlocked)
            return Outcome<ExtraRunResponse>.Fail(ApiErrors.ExtraTrainingLocked,
                "The bonus training opens once the goals it waits for are done: today's daily goals, or the weekly goals when no plan has a daily one.");

        var pool = await PoolAsync(childId, gate.Plans, ct);
        if (pool.FirstOrDefault(p => p.Row.ExerciseId == exerciseId) is not { } entry)
            return Outcome<ExtraRunResponse>.Fail(ApiErrors.NotFound, "The exercise is not part of the bonus training.");

        // Final for today beats provisional: a fixed-form exercise worth nothing more today says so before the cap says
        // "not now" - a run at 0 % would show its solutions for nothing. Vocabulary may go on for no coins, as it may at a
        // full cap (B-543, decision 5).
        if (entry.Row.Done && IsFixedForm(entry.Exercise))
            return Outcome<ExtraRunResponse>.Fail(ApiErrors.ExtraExerciseDone,
                "This bonus exercise is worth nothing more today; its worth resets tomorrow.");
        // A fixed-form exercise shows its solutions after the run, so at a full cap it would hand them out for nothing -
        // and pay in full for the same answers tomorrow. The check also covers resuming an open run: played to the end
        // today, it would only hit the same wall at submit.
        if (IsFixedForm(entry.Exercise) && await CoinsLeftTodayAsync(childId, today, ct) == 0)
            return Outcome<ExtraRunResponse>.Fail(ApiErrors.ExtraCapReached, CapReachedDetail);

        var run = entry.Row.OpenRunId is { } openId
            ? await db.ExtraTrainingRuns.FirstAsync(r => r.Id == openId, ct)
            : null;
        var fingerprint = ContentFingerprint(entry.Exercise, entry.Items);
        // B-545: an open run whose exercise changed since asks questions the key no longer matches - start it afresh.
        if (run is { ContentHash: { } started } && started != fingerprint)
        {
            db.ExtraTrainingRuns.Remove(run);
            await db.SaveChangesAsync(ct);
            run = null;
        }
        if (run is null)
        {
            run = new ExtraTrainingRun
            {
                ChildId = childId,
                ExerciseId = exerciseId,
                Day = today,
                Pass = entry.Row.CompletedRuns,
                Form = entry.Row.NextForm,
                Stage = entry.Stage,
                StartedAt = time.GetUtcNow().UtcDateTime,
                ItemCount = entry.Items.Count,
                ContentHash = fingerprint,
            };
            db.ExtraTrainingRuns.Add(run);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // A parallel start (double tap) won the filtered unique index: resume its run instead of failing.
                // Any other cause (the exercise deleted meanwhile) has no open run to resume and stays an error.
                db.ChangeTracker.Clear();
                run = await db.ExtraTrainingRuns.FirstOrDefaultAsync(r => r.ChildId == childId && r.ExerciseId == exerciseId
                    && r.CompletedAt == null, ct);
                if (run is null) throw;
            }
        }

        return new Outcome<ExtraRunResponse>(BuildRun(run, entry.Exercise, entry.Type, entry.Items, entry.Row.NextRunPercent));
    }

    /// <summary>
    /// Grades a run and books its coins: per checked hit, never above what is left of today's cap. A run is graded
    /// once, and only while today's daily goals are met - a run started on an open day and kept back must not pay out on a
    /// day they were skipped. The child's concurrency stamp serializes the submit against every other one.
    /// </summary>
    public async Task<Outcome<ExtraRunResult>> SubmitAsync(int childId, int runId, IReadOnlyList<ExtraAnswer> answers,
        CancellationToken ct)
    {
        // The child first: its stamp is the serialization point, and it only protects what is read AFTER it. Loaded
        // last, a second submit of the same run (double tap, two tabs) would read the run as still open, see the
        // stamp the first one had just written, and book the coins a second time.
        var child = await db.Children.FirstAsync(c => c.Id == childId, ct);

        var run = await db.ExtraTrainingRuns.Include(r => r.Exercise)
            .FirstOrDefaultAsync(r => r.Id == runId && r.ChildId == childId, ct);
        if (run?.Exercise is null) return Outcome<ExtraRunResult>.Fail(ApiErrors.NotFound, "Bonus run not found.");
        if (run.CompletedAt is not null)
            return Outcome<ExtraRunResult>.Fail(ApiErrors.ExtraRunCompleted, "This bonus run has already been submitted.");

        // B-545: graded only against the content its questions came from. A changed exercise discards the run unpaid;
        // deleting it costs no halving step - the child caused nothing and saw no solution yet.
        if (run.ContentHash is { } started
            && started != ContentFingerprint(run.Exercise, await content.ItemsOfAsync(run.Exercise, ct: ct)))
        {
            db.ExtraTrainingRuns.Remove(run);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                // A parallel submit of the same run (double tap) deleted it first - the run is gone either way.
                db.ChangeTracker.Clear();
            }
            return Outcome<ExtraRunResult>.Fail(ApiErrors.ExtraRunStale,
                "The exercise was changed since you started; start it again.");
        }

        var today = Today;
        if (!(await GateAsync(childId, today, ct)).Unlocked)
            return Outcome<ExtraRunResult>.Fail(ApiErrors.ExtraTrainingLocked,
                "The bonus training opens once the goals it waits for are done: today's daily goals, or the weekly goals when no plan has a daily one.");

        var coinsBefore = await CoinsTodayAsync(childId, today, ct);
        var dailyCap = CapOf(child.BonusDailyCoinCap);
        var left = dailyCap is { } cap ? Math.Max(0, cap - coinsBefore) : int.MaxValue;
        // A fixed-form run handed in at a full cap stays open and unpaid until coins are left. This happens before grading:
        // the answer carries the solutions, and a child who saw them today could hand in the same run later for full coins.
        if (IsFixedForm(run.Exercise) && left == 0)
            return Outcome<ExtraRunResult>.Fail(ApiErrors.ExtraCapReached, CapReachedDetail);

        var graded = await preview.CheckAsync(run.Exercise,
            [.. answers.Select(a => new PreviewAnswer(a.ItemIndex, a.GivenAnswer, a.WasKnown))], run.Stage, ct);
        if (graded is null)
            return Outcome<ExtraRunResult>.Fail(ApiErrors.NotFound, "The exercise has no content to grade any more.");

        // The halving counts the runs of this exercise completed today BEFORE this one, at the submit and not at the start:
        // a run is booked on the day it is handed in, so a run started yesterday pays by today's count.
        var completedToday = await db.ExtraTrainingRuns.CountAsync(r => r.ChildId == childId && r.ExerciseId == run.ExerciseId
            && r.Day == today && r.CompletedAt != null, ct);
        var perHit = CoinsPerHit(run.Form);
        var percent = RunPercent(completedToday);
        // Each hit pays by the form its card was asked in, not by the run's (B-597): a typed run on a "both" list asks
        // every forward card as a choice, and a tap must not earn what writing earns.
        var cardRates = await CardRatesAsync(run.Exercise, run.Stage, ct);
        var hitCoins = graded.Items.Where(i => i.WasCorrect).Sum(i => cardRates.GetValueOrDefault(i.ItemIndex, perHit));
        var earned = hitCoins * percent / 100;
        var coins = Math.Min(earned, left);

        var now = time.GetUtcNow().UtcDateTime;
        // The coins count against the day they are booked on, not the day the run was started.
        run.Day = today;
        run.CompletedAt = now;
        run.CorrectCount = graded.Correct;
        run.CoinsAwarded = coins;
        if (coins > 0)
            db.ChildPointsEntries.Add(new ChildPointsEntry
            {
                ChildId = childId,
                Kind = PointKind.ExtraTrainingCoins,
                Amount = coins,
                Reason = $"Bonus-Training: {run.Exercise.Title}",
                CreatedAt = now,
            });

        child.ConcurrencyStamp = Guid.NewGuid();
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            return Outcome<ExtraRunResult>.Fail(ApiErrors.ConcurrencyConflict, "Another bonus run was saved at the same time. Please try again.");
        }

        return new Outcome<ExtraRunResult>(new ExtraRunResult(run.Id, run.Form, graded.Total, graded.Correct, coins,
            coins < earned, coinsBefore + coins, dailyCap,
            [.. graded.Items.Select(i => new ExtraItemOutcome(i.ItemIndex, i.Prompt, i.Expected, i.GivenAnswer, i.WasCorrect, i.GapIndex))],
            perHit, percent, earned, hitCoins));
    }

    /// <summary>
    /// SHA-256 over what grading reads (B-545): the config and, per item, index, prompt, answer, accepted answers, gap
    /// and item id - in a fixed order, so the same content always gives the same value. Image, audio and hint stay out:
    /// a new picture changes no solution and must not cost a run.
    /// </summary>
    private static string ContentFingerprint(Exercise exercise, IReadOnlyList<ContentItem> items)
    {
        var text = new StringBuilder(exercise.ConfigJson);
        foreach (var i in items)
        {
            text.Append('\u001e').Append(i.Index).Append('\u001f').Append(i.Prompt).Append('\u001f').Append(i.Answer)
                .Append('\u001f').Append(string.Join('\u001d', i.AcceptedAnswers)).Append('\u001f').Append(i.GapIndex)
                .Append('\u001f').Append(i.ItemId);
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())));
    }

    private record Gate(bool Unlocked, string? LockReason, IReadOnlyList<string> Outstanding, IReadOnlyList<StudyPlan> Plans,
        GoalCadence RewardCadence);

    // Open once the reward goals of the child's day are met (the daily goals of all plans, B-535/B-611) - read from
    // the child's one day view (B-606), the same that earns the daily box. A plan without any duty has nothing to come first, so it neither
    // locks nor needs the box rule's "at least one goal".
    private async Task<Gate> GateAsync(int childId, DateOnly today, CancellationToken ct)
    {
        var day = await progress.ComputeChildDayAsync(childId, today, ct);
        var plans = day.Plans.Select(p => p.Plan).ToList();
        if (plans.Count == 0) return new Gate(false, LockNoPlan, [], plans, GoalCadence.None);

        return day.RewardOutstanding.Count == 0
            ? new Gate(true, null, [], plans, day.RewardCadence)
            : new Gate(false, LockDutyOpen, day.RewardOutstanding, plans, day.RewardCadence);
    }

    // The supervisor's cap for this child wins over the server default.
    private int? CapOf(int? childCap) => childCap ?? opts.DailyCoinCap;

    // Names the lock only: with a cap of 0 there is no "tomorrow" to promise.
    private const string CapReachedDetail = "Today's bonus coin cap is reached; this exercise would show its solutions for no coins.";

    // Everything but vocabulary plays the same checked stage on every run; vocabulary climbs a form per run instead.
    private static bool IsFixedForm(Exercise exercise) => exercise.Type != ExerciseTypeKeys.Vocabulary;

    /// <summary>
    /// What the next completed run of an exercise is worth today, in percent of its hits (B-538): 100, 50, 25, then 0 -
    /// halved by every run of the same exercise completed earlier the same day. The coins are rounded down.
    /// </summary>
    public static int RunPercent(int completedToday) => completedToday switch
    {
        0 => 100,
        1 => 50,
        2 => 25,
        _ => 0,
    };

    // What is left of today's cap; without any cap there is always something left.
    private async Task<int> CoinsLeftTodayAsync(int childId, DateOnly today, CancellationToken ct)
    {
        var childCap = await db.Children.AsNoTracking().Where(c => c.Id == childId).Select(c => c.BonusDailyCoinCap).FirstAsync(ct);
        return CapOf(childCap) is { } cap ? Math.Max(0, cap - await CoinsTodayAsync(childId, today, ct)) : int.MaxValue;
    }

    private async Task<int> CoinsTodayAsync(int childId, DateOnly day, CancellationToken ct) =>
        await db.ExtraTrainingRuns.Where(r => r.ChildId == childId && r.Day == day).SumAsync(r => (int?)r.CoinsAwarded, ct) ?? 0;

    private record PoolEntry(ExtraExercise Row, Exercise Exercise, IExerciseType Type, int Stage, IReadOnlyList<ContentItem> Items);

    // The exercises of the current topic the child can play for a bonus, each with the form of its next run.
    private async Task<List<PoolEntry>> PoolAsync(int childId, IReadOnlyList<StudyPlan> plans, CancellationToken ct)
    {
        var planIds = plans.Select(p => p.Id).ToList();
        var positionExercises = await db.PlanPositions.AsNoTracking()
            .Where(p => planIds.Contains(p.StudyPlanId))
            .Select(p => new { p.ExerciseId, p.Exercise!.SeriesUnitId })
            .ToListAsync(ct);
        var units = positionExercises.Select(p => p.SeriesUnitId).ToHashSet();
        units.UnionWith(await db.Textbooks.AsNoTracking()
            .Where(t => t.ChildId == childId && t.CurrentUnitId != null)
            .Select(t => t.CurrentUnitId!.Value)
            .ToListAsync(ct));
        if (units.Count == 0) return [];

        // Every plan of the child, not only the playable ones: an exercise known from an earlier path is known too.
        var knownFromPath = (await db.PlanPositions.AsNoTracking()
            .Where(p => p.StudyPlan!.ChildId == childId)
            .Select(p => p.ExerciseId)
            .ToListAsync(ct)).ToHashSet();
        var assigned = positionExercises.Select(p => p.ExerciseId).ToHashSet();

        var exercises = await db.Exercises.AsNoTracking().Include(e => e.SeriesUnit)
            .Where(e => units.Contains(e.SeriesUnitId) && (e.ExecutePublic || assigned.Contains(e.Id)))
            .OrderBy(e => e.SeriesUnitId).ThenBy(e => e.OrderIndex).ThenBy(e => e.Id)
            .ToListAsync(ct);

        var today = Today;
        var runs = await db.ExtraTrainingRuns.AsNoTracking()
            .Where(r => r.ChildId == childId)
            .Select(r => new { r.ExerciseId, r.Id, r.CompletedAt, r.Day })
            .ToListAsync(ct);
        var completed = runs.Where(r => r.CompletedAt != null).GroupBy(r => r.ExerciseId).ToDictionary(g => g.Key, g => g.Count());
        var completedToday = runs.Where(r => r.CompletedAt != null && r.Day == today).GroupBy(r => r.ExerciseId)
            .ToDictionary(g => g.Key, g => g.Count());
        var open = runs.Where(r => r.CompletedAt == null).ToDictionary(r => r.ExerciseId, r => r.Id);

        var pool = new List<PoolEntry>();
        foreach (var exercise in exercises)
        {
            if (registry.ByKey(exercise.Type) is not { } type) continue;
            var climbs = !IsFixedForm(exercise);
            if (!climbs && knownFromPath.Contains(exercise.Id)) continue;

            var items = await content.ItemsOfAsync(exercise, ct: ct);
            if (items.Count == 0) continue;

            var done = completed.GetValueOrDefault(exercise.Id);
            if (FormFor(exercise, type, climbs, items, done) is not var (form, stage)) continue;
            // Every exercise may be played again (B-538); "done" now means "worth nothing more today".
            var percent = RunPercent(completedToday.GetValueOrDefault(exercise.Id));

            pool.Add(new PoolEntry(
                new ExtraExercise(exercise.Id, exercise.Title, exercise.Type, exercise.SeriesUnit?.Label ?? "", items.Count,
                    climbs, done, percent == 0, form, CoinsPerHit(form), open.TryGetValue(exercise.Id, out var runId) ? runId : null,
                    percent),
                exercise, type, stage, items));
        }
        return pool;
    }

    // The form of the next run, or null when the exercise has no checked form to be paid for. Vocabulary climbs with
    // its completed runs; everything else plays its author's stage, or the type's most telling one, on every run - and
    // only where that stage is checked, because a self-assessed fixed form would be the cheapest coins there are.
    private (ExtraForm Form, int Stage)? FormFor(Exercise exercise, IExerciseType type, bool climbs,
        IReadOnlyList<ContentItem> items, int completedRuns)
    {
        if (climbs)
        {
            if (completedRuns < opts.SelfAssessRuns) return (ExtraForm.SelfAssess, (int)TestStage.SelfAssess);
            if (completedRuns < opts.SelfAssessRuns + opts.ChoiceRuns && items.Count >= MinItemsForChoice)
                return (ExtraForm.Choice, (int)TestStage.MultipleChoice);
            return (ExtraForm.Typed, (int)TestStage.FreeText);
        }

        int? stage = exercise.DefaultStage is { } own && type.IsTypedStage(own) ? own
            : type.IsTypedStage(type.PreviewStage) ? type.PreviewStage : null;
        if (stage is not { } checkedStage) return null;
        // A checked stage whose cards offer options or letter tiles is a pick, not a recall, and pays as one (B-599: the
        // scramble counts as a choice). Named for the run only where every card picks: a card falling back to letter
        // boxes pays the typed rate on its own (CardRatesAsync), and a run advertised as a pick would undersell it.
        return (items.All(i => type.Choices(exercise.ConfigJson, items, i, checkedStage) is not null
                || type.LetterTileSource(i, checkedStage) is not null) ? ExtraForm.Choice : ExtraForm.Typed,
            checkedStage);
    }

    // The rate of each card of a run by item index, from the stage it is asked at: its ceiling (B-597) caps the run's stage,
    // and a card offering options or letter tiles is a pick, everything else typed - the same facets the card showed.
    private async Task<Dictionary<int, int>> CardRatesAsync(Exercise exercise, int runStage, CancellationToken ct)
    {
        if (registry.ByKey(exercise.Type) is not { } type) return [];
        var items = await content.ItemsOfAsync(exercise, ct: ct);
        return items.ToDictionary(raw => raw.Index, raw =>
        {
            var stage = type.CardCeiling(raw, runStage);
            if (!type.IsTypedStage(stage)) return CoinsPerHit(ExtraForm.SelfAssess);
            var staged = PositionPlayService.AtStage(type, items, stage);
            var item = type.CardForStage(raw, stage);
            var picks = type.Choices(exercise.ConfigJson, staged, item, stage) is not null
                || type.LetterTileSource(item, stage) is not null;
            return CoinsPerHit(picks ? ExtraForm.Choice : ExtraForm.Typed);
        });
    }

    private int CoinsPerHit(ExtraForm form) => form switch
    {
        ExtraForm.Choice => opts.CoinsPerChoiceHit,
        ExtraForm.Typed => opts.CoinsPerTypedHit,
        _ => opts.CoinsPerSelfAssessHit,
    };

    // The cards in an order of their own per run, and the options shuffled per run as well: the shared projection
    // places the right option by the atom's index, which a child replaying the same words would learn as a position.
    private ExtraRunResponse BuildRun(ExtraTrainingRun run, Exercise exercise, IExerciseType type,
        IReadOnlyList<ContentItem> items, int runPercent)
    {
        var typed = type.IsTypedStage(run.Stage);
        var rng = new Random(run.Id);
        var ordered = exercise.Type == ExerciseTypeKeys.Vocabulary ? items.OrderBy(_ => rng.Next()).ToList() : [.. items];
        var cards = ordered.Select(raw =>
        {
            // B-597: each card at its own ceiling and in the form that stage asks it - the preview check grades the same way.
            var stage = type.CardCeiling(raw, run.Stage);
            var staged = PositionPlayService.AtStage(type, items, stage);
            var item = type.CardForStage(raw, stage);
            var f = PositionPlayService.CardFacets(exercise.ConfigJson, staged, item, type, stage, type.IsTypedStage(stage), run.Id);
            var choices = f.Choices is { } c ? c.OrderBy(_ => rng.Next()).ToList() : null;
            return new ExtraCard(item.Index, f.Prompt, f.GapIndex, f.Hint, f.AnswerLength, f.Reveal, f.RevealAlternatives,
                choices, f.AudioUrl, f.Passage, f.AnyOrder, f.Decoding, f.AnswerPattern, f.LetterTiles, f.CueBox,
                f.DictationSpokenAnswer, f.DictationLang);
        }).ToList();
        return new ExtraRunResponse(run.Id, exercise.Id, exercise.Title, exercise.Type, run.Form, run.Stage, typed,
            CoinsPerHit(run.Form), cards, runPercent);
    }
}
