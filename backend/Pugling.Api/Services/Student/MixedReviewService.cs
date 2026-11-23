using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Student;

/// <summary>
/// The mixed review (B-764): one short round of the cards due across all of the child's playable Leitner positions, so
/// nothing due is left lying in a plan the child does not open today.
/// <para>
/// It is voluntary like the bonus training and opens behind the same lock - the path comes first. It draws only from
/// vocabulary positions without an open duty of the running period: reviewing the due cards of such a position would
/// empty its next learn round, and an empty round counts as met ("nothing was due") - the duty would be fulfilled
/// without being done. It draws only cards the child has met already (introduced and, where needed, imprinted): a never
/// seen card would be introduced past the daily dose and the learn card. And it creates no practice session, so it
/// meets no goal, keeps no streak and earns no chest.
/// </para>
/// </summary>
public class MixedReviewService(PuglingDbContext db, PositionProgressService progress, PositionPlayService play,
    ExerciseContentResolver content, ExerciseTypeRegistry registry, ExtraTrainingService extras,
    IOptions<ExtraTrainingOptions> options, TimeProvider time, LearningDay learningDay)
{
    private readonly ExtraTrainingOptions opts = options.Value;

    /// <summary>Outcome of a start or submit: either the value or the error to answer with.</summary>
    public record Outcome<T>(T? Value, ApiError? Error = null, string? Detail = null) where T : class
    {
        /// <summary>A failed outcome answered with <paramref name="error"/>.</summary>
        public static Outcome<T> Fail(ApiError error, string detail) => new(null, error, detail);
    }

    private const string LockedDetail =
        "The mixed review opens once the goals it waits for are done: today's daily goals, or the weekly goals when no plan has a daily one.";

    /// <summary>Today's mixed review: the bonus lock, how many cards are due for it, an open round and the shared coins.</summary>
    public async Task<MixedReviewOverview> OverviewAsync(int childId, CancellationToken ct)
    {
        var today = learningDay.Today;
        var day = await progress.ComputeChildDayAsync(childId, today, ct);
        var gate = ExtraTrainingService.GateOf(day);
        var due = gate.Plans.Count == 0 ? 0 : (await SelectAsync(day, today, ct)).Count;
        var openId = await db.MixedReviewRuns.AsNoTracking()
            .Where(r => r.ChildId == childId && r.CompletedAt == null && r.Day == today)
            .Select(r => (int?)r.Id).FirstOrDefaultAsync(ct);
        var childCap = await db.Children.AsNoTracking().Where(c => c.Id == childId).Select(c => c.BonusDailyCoinCap).FirstAsync(ct);
        return new MixedReviewOverview(gate.Unlocked, gate.LockReason, gate.Outstanding, due, opts.MixedReviewRoundSize,
            openId, await extras.CoinsTodayAsync(childId, today, ct), extras.CapOf(childCap));
    }

    /// <summary>
    /// Starts a round, or resumes today's open one with the same cards. A round left open from an earlier day is
    /// discarded first: its cards were due then, not now.
    /// </summary>
    public async Task<Outcome<MixedReviewRunResponse>> StartAsync(int childId, CancellationToken ct)
    {
        var today = learningDay.Today;
        var day = await progress.ComputeChildDayAsync(childId, today, ct);
        if (!ExtraTrainingService.GateOf(day).Unlocked)
            return Outcome<MixedReviewRunResponse>.Fail(ApiErrors.ExtraTrainingLocked, LockedDetail);

        var run = await OpenRunAsync(childId, ct);
        if (run is not null && run.Day != today)
        {
            db.MixedReviewRuns.Remove(run);
            await db.SaveChangesAsync(ct);
            run = null;
        }

        if (run is null)
        {
            var picked = await SelectAsync(day, today, ct);
            if (picked.Count == 0)
                return Outcome<MixedReviewRunResponse>.Fail(ApiErrors.MixedReviewEmpty,
                    "No card is due in a position the mixed review may draw from right now.");

            run = new MixedReviewRun { ChildId = childId, Day = today, StartedAt = time.GetUtcNow().UtcDateTime };
            run.Cards.AddRange(picked.Select((c, order) => new MixedReviewCard
            {
                PlanPositionId = c.Position.Id,
                ExerciseId = c.Exercise.Id,
                ItemIndex = c.ItemIndex,
                Order = order,
                Stage = c.Stage,
                ContentHash = ExtraTrainingService.ContentFingerprint(c.Exercise, c.Items),
            }));
            db.MixedReviewRuns.Add(run);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // A parallel start (double tap) won the filtered unique index: resume its round instead of failing.
                db.ChangeTracker.Clear();
                run = await OpenRunAsync(childId, ct);
                if (run is null) throw;
            }
        }

        var left = await extras.CoinsLeftTodayAsync(childId, today, ct);
        return new Outcome<MixedReviewRunResponse>(new MixedReviewRunResponse(run.Id, await CardViewsAsync(run, ct),
            left == int.MaxValue ? null : left));
    }

    private Task<MixedReviewRun?> OpenRunAsync(int childId, CancellationToken ct) =>
        db.MixedReviewRuns.Include(r => r.Cards)
            .FirstOrDefaultAsync(r => r.ChildId == childId && r.CompletedAt == null, ct);

    /// <summary>A card a round may ask: the position, its plan, the exercise it plays today with its items, and the stage.</summary>
    internal sealed record Candidate(PlanPosition Position, StudyPlan Plan, Exercise Exercise, IExerciseType Type,
        IReadOnlyList<ContentItem> Items, int ItemIndex, int Stage, DateOnly DueOn, int Box);

    /// <summary>
    /// The cards a round draws on <paramref name="today"/>, most overdue first, then the lowest box - at most the round
    /// size, and a word that two positions share only once (the other position's card stays due for the next round).
    /// </summary>
    internal async Task<List<Candidate>> SelectAsync(ChildDayRollup day, DateOnly today, CancellationToken ct)
    {
        // The day view the lock reads also says which duty is open: a goal of the running period (the week for a weekly
        // goal) not met yet.
        var openDuty = day.Positions.Where(s => s.Cadence != GoalCadence.None && !s.GoalMet).Select(s => s.PositionId).ToHashSet();
        var plans = day.Plans.Select(p => p.Plan).Where(p => PositionPlayService.PlanPlayableForChild(p, today))
            .ToDictionary(p => p.Id);
        if (plans.Count == 0) return [];

        var planIds = plans.Keys.ToList();
        var positions = await db.PlanPositions.AsNoTracking()
            .Include(p => p.Exercise).ThenInclude(e => e!.SeriesUnit).ThenInclude(u => u!.Series)
            .Include(p => p.Exercise).ThenInclude(e => e!.Prerequisites!).ThenInclude(e => e.SeriesUnit).ThenInclude(u => u!.Series)
            .Where(p => planIds.Contains(p.StudyPlanId) && p.UseLeitner)
            .OrderBy(p => p.StudyPlanId).ThenBy(p => p.Order).ThenBy(p => p.Id)
            .ToListAsync(ct);
        positions.RemoveAll(p => openDuty.Contains(p.Id));
        if (positions.Count == 0) return [];

        var positionIds = positions.Select(p => p.Id).ToList();
        // Only cards met already (decision 4): a row with an introduction date. "Never seen counts as due" is the play
        // path's rule for the learn round, which introduces within the dose - the mixed review introduces nothing.
        var rows = (await db.PositionItemProgress.AsNoTracking()
                .Where(p => positionIds.Contains(p.PlanPositionId) && p.IntroducedAt != null
                    && (p.DueOn == null || p.DueOn <= today))
                .ToListAsync(ct))
            .ToLookup(p => p.PlanPositionId);

        var candidates = new List<Candidate>();
        foreach (var pos in positions)
        {
            // Vocabulary only for now (B-218 decision 7); a set-graded list accepts any open entry and has no card to ask alone.
            if (PositionPlayService.PlayedExercise(pos, today) is not { Type: ExerciseTypeKeys.Vocabulary } exercise
                || registry.ByKey(exercise.Type) is not { } type || type.GradesAsSet(exercise.ConfigJson))
                continue;
            var plan = plans[pos.StudyPlanId];
            // The position's stage of the day, never an easier one (B-765 decision 2); a learn-card day is asked at its
            // target like in the test, and a free display day has nothing to review.
            var stage = PositionPlayService.TestTargetStage(type, pos,
                PositionPlayService.StageForDay(pos, plan, today, type), today);
            if (type.IsDisplayOnlyStage(stage)) continue;

            var items = await content.ItemsOfAsync(exercise, ct: ct);
            var poolSize = play.PoolSize(pos, today, items.Count);
            foreach (var row in rows[pos.Id].Where(r => r.ExerciseId == exercise.Id && r.ItemIndex < poolSize))
            {
                var item = items[row.ItemIndex];
                // A word met on its learn card but not imprinted comes back as a learn card in its position (B-643).
                if (!PositionPlayService.Known(type, new CardProgress(row.Box, row.IntroducedAt!.Value, row.ImprintedAt), item))
                    continue;
                candidates.Add(new Candidate(pos, plan, exercise, type, items, row.ItemIndex, stage, row.DueOn ?? today, row.Box));
            }
        }

        return [.. candidates
            .OrderBy(c => c.DueOn).ThenBy(c => c.Box)
            .DistinctBy(c => (c.Exercise.Id, c.ItemIndex))
            .Take(opts.MixedReviewRoundSize)];
    }

    // The cards of a round as the child sees them, in round order. A card whose item is gone since the start is left out;
    // the submit does not grade it either.
    private async Task<List<MixedReviewCardView>> CardViewsAsync(MixedReviewRun run, CancellationToken ct)
    {
        var positionIds = run.Cards.Select(c => c.PlanPositionId).Distinct().ToList();
        var positions = await db.PlanPositions.AsNoTracking().Include(p => p.StudyPlan).ThenInclude(s => s!.Subject)
            .Where(p => positionIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        var exerciseIds = run.Cards.Select(c => c.ExerciseId).Distinct().ToList();
        var exercises = await db.Exercises.AsNoTracking().Include(e => e.SeriesUnit).ThenInclude(u => u!.Series)
            .Where(e => exerciseIds.Contains(e.Id)).ToDictionaryAsync(e => e.Id, ct);
        var itemsOf = new Dictionary<int, IReadOnlyList<ContentItem>>();
        foreach (var exercise in exercises.Values) itemsOf[exercise.Id] = await content.ItemsOfAsync(exercise, ct: ct);

        var views = new List<MixedReviewCardView>(run.Cards.Count);
        foreach (var card in run.Cards.OrderBy(c => c.Order))
        {
            if (!positions.TryGetValue(card.PlanPositionId, out var pos) || !exercises.TryGetValue(card.ExerciseId, out var exercise)
                || registry.ByKey(exercise.Type) is not { } type
                || itemsOf[exercise.Id].FirstOrDefault(i => i.Index == card.ItemIndex) is not { } raw)
                continue;
            views.Add(new MixedReviewCardView(card.Id, pos.Id, pos.StudyPlan?.Title ?? "", pos.StudyPlan?.Subject?.Name,
                exercise.Title, card.Stage, type.IsTypedStage(type.CardCeiling(raw, card.Stage)),
                BuildCard(run.Id, card, exercise, type, itemsOf[exercise.Id], raw)));
        }
        return views;
    }

    // One card at its stage, capped by the card's own ceiling (B-597) - the same projection the bonus run and the practice
    // card use, so the grading reads the form the child saw. The options are shuffled per card.
    private ExtraCard BuildCard(int runId, MixedReviewCard card, Exercise exercise, IExerciseType type,
        IReadOnlyList<ContentItem> items, ContentItem raw)
    {
        var stage = type.CardCeiling(raw, card.Stage);
        var typed = type.IsTypedStage(stage);
        var staged = PositionPlayService.AtStage(type, items, stage);
        var item = type.CardForStage(raw, stage);
        var f = PositionPlayService.CardFacets(exercise.ConfigJson, staged, item, type, stage, typed, runId);
        var rng = new Random(card.Id);
        var choices = f.Choices is { } c ? c.OrderBy(_ => rng.Next()).ToList() : null;
        // Only a checked hit pays (B-765 decision 3): a self-assessed card announces no coins.
        var rate = typed ? extras.CardRate(exercise, type, items, raw, card.Stage) : 0;
        return new ExtraCard(item.Index, f.Prompt, f.GapIndex, f.Hint, f.AnswerLength, f.Reveal, f.RevealAlternatives,
            choices, f.AudioUrl, f.Passage, f.AnyOrder, f.Decoding, f.AnswerPattern, f.LetterTiles, f.CueBox, rate,
            f.DictationSpokenAnswer, f.DictationLang, PositionPlayService.KeyboardLangOf(exercise));
    }
}
