using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Student;

/// <summary>
/// Final test of a single study plan position (new model): tests the content of ONE exercise.
/// Content comes from the exercise config (<see cref="ExerciseContentProvider"/>), grading is type-neutral
/// against the item solution. Passing is measured against <see cref="PlanPosition.GoalThreshold"/> (default 80 %).
/// The points for passing (per-position goal) follow in the goal/points engine (stage 4); here the
/// attempt already counts for metric-based missions (e.g. "tests passed").
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Student + "/study-plans/{planId:int}/positions/{positionId:int}/tests")]
[Tags("Student – Position Tests")]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(PlanOwnershipFilter))]
public class PositionTestsController(PuglingDbContext db, PositionPlayService play,
    PositionProgressService progress, GamificationService gamification, AnswerGrader grader,
    ItemProgressService itemProgress, DailyBoxService dailyBox) : ControllerBase
{
    /// <summary>Default pass threshold when the position sets no threshold of its own.</summary>
    private const int DefaultPassPercent = 80;

    /// <summary>
    /// How many test attempts the child gets per <b>day</b>. A second chance is pedagogically right, the
    /// fifth is grade farming: without a cap the child can restart until the result is good and silently drop
    /// every bad run (an abandoned attempt writes nothing – see <see cref="Answer"/>).
    /// <para>
    /// Per day and deliberately <em>not</em> per goal period: what the cap defends against is the immediate
    /// restart, and a fresh day is fresh learning rather than farming. Tied to the period, a weekly position
    /// would grant two attempts for the entire week – two Monday failures would lock the child out of its own
    /// duty until Sunday and then fine it (<see cref="PlanPosition.PenaltyCoins"/>) for missing it.
    /// </para>
    /// <para>
    /// It also holds for positions without a duty (<see cref="GoalCadence.None"/>): a test always writes item
    /// progress and feeds the metric missions, so unlimited restarts are farming there too.
    /// </para>
    /// <para>
    /// Deliberately a constant and not a field on the position: a per-position cap would be a schema change,
    /// and the migration chain is folded rather than extended. If the cap ever needs to be configurable, this
    /// is the one place to replace.
    /// </para>
    /// </summary>
    private const int MaxAttemptsPerDay = 2;


    // No default for `ct`: it would make the call site look correct while the client's cancellation fizzles
    // out - neither CA2016 nor the guard sees an omitted optional argument.
    private Task<StudyPlan?> GetPlan(int planId, CancellationToken ct) =>
        db.StudyPlans.FirstOrDefaultAsync(p => p.Id == planId, ct);

    private Task<PlanPosition?> GetPosition(int planId, int positionId, CancellationToken ct) =>
        db.PlanPositions.Include(p => p.Exercise)
            .FirstOrDefaultAsync(p => p.Id == positionId && p.StudyPlanId == planId, ct);

    private Task<TestAttempt?> LoadAttempt(int planId, int positionId, int attemptId, CancellationToken ct) =>
        db.TestAttempts.Include(t => t.Results)
            .FirstOrDefaultAsync(t => t.Id == attemptId && t.StudyPlanId == planId && t.PlanPositionId == positionId, ct);

    private static TestItem ToItem(Exercise exercise, IReadOnlyList<ContentItem> items, ContentItem item,
        IExerciseType type, int stage, bool typed)
    {
        // Shared anti-cheat projection (reveal/length/hint/choices/audio per stage) - the same rule as the
        // practice card. The image is deliberately left out: the exam renders none, and asking for one would
        // freeze the child's motif choice as a side effect of taking a test (see MediaSelector).
        var f = PositionPlayService.CardFacets(PositionPlayService.ConfigOf(exercise), items, item, type, stage, typed);
        return new TestItem(item.Index, f.Prompt, stage, f.Reveal, f.AnswerLength, f.Hint, f.Choices, f.AudioUrl,
            f.GapIndex, f.Passage, f.AnyOrder, type.Key, f.RevealAlternatives, f.AnswerPattern, f.Decoding);
    }

    /// <summary>
    /// The entries already credited within this attempt – the rows that carry an atom and were answered
    /// correctly. This is the set-mode counterpart to the practice path's "named today": inside one exam the
    /// attempt itself is the period, and its results already record every answer, so no extra state is needed.
    /// </summary>
    private static HashSet<int> CreditedEntries(TestAttempt attempt) =>
        [.. attempt.Results.Where(r => r.WasCorrect && r.ItemIndex is not null).Select(r => r.ItemIndex!.Value)];

    /// <summary>
    /// Books one answer of a set-graded exercise: it credits the first entry still open (and
    /// <paramref name="credited"/> grows by it), or – matching nothing – becomes a <b>wrong mention</b>, a
    /// result row without an atom (<see cref="TestItemResult.ItemIndex"/> is nullable for exactly this). A
    /// wrong mention must never be attributed to the card that carried it: that row still stands for an entry
    /// the child may yet name.
    /// <para>
    /// Shared by the step-wise <see cref="Answer"/> and the bulk <see cref="Submit"/>, because a second copy of
    /// this rule is how the two paths would drift apart.
    /// </para>
    /// </summary>
    private static void CreditSetAnswer(TestAttempt attempt, IReadOnlyList<ContentItem> items,
        HashSet<int> credited, string? given, AnswerGrader grader)
    {
        if (PositionPlayService.MatchOpenEntry(items, attempt.Order.Where(i => !credited.Contains(i)), given, grader)
                is { } hit
            && attempt.Results.FirstOrDefault(r => r.ItemIndex == hit) is { } hitRow)
        {
            hitRow.GivenAnswer = given;
            hitRow.WasCorrect = true;
            credited.Add(hit);
            return;
        }
        // A blank answer is a skipped card, not a mention: it names nothing, the result screen would drop it
        // again, and a child clicking through a 16-entry list would leave sixteen empty rows behind.
        if (string.IsNullOrWhiteSpace(given)) return;
        attempt.Results.Add(new TestItemResult { ItemIndex = null, StageValue = attempt.StageValue, GivenAnswer = given });
    }

    /// <summary>
    /// Starts a test attempt for the position. The class-test mode is strictly server-driven: the start
    /// freezes the question order and returns only the metadata – the client fetches questions one at a time
    /// via <see cref="Next"/> and answers them via <see cref="Answer"/> (no going back, feedback only on
    /// <see cref="Submit"/>).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttemptResponse>> Start(int planId, int positionId, StartTestDto dto, CancellationToken ct = default)
    {
        var plan = await GetPlan(planId, ct);
        if (plan is null) return NotFound();
        var pos = await GetPosition(planId, positionId, ct);
        if (pos?.Exercise is null) return NotFound();

        var items = await play.ItemsOfAsync(pos, ct: ct);
        var poolSize = play.PoolSize(pos, items.Count);
        if (poolSize == 0) return this.ProblemWithCode(ApiErrors.NoCheckableContent, "The exercise contains no checkable content.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (dto.Day is { } dd && dd != today && !User.IsSupervisor()) return Forbid();
        // Anti-cheat: the child may only test its active, running plan (see the practice start).
        if (User.IsStudent() && !PositionPlayService.PlanPlayableForChild(plan, today))
            return this.ProblemWithCode(ApiErrors.PlanInactive, "This study plan is not currently active. Ask your parent.");
        var day = dto.Day ?? today;

        // The child does not walk out of a running attempt and gets only a limited number per day.
        // The supervisor stays exempt: they use the endpoint for preview/catch-up with a stage of their own.
        // Both queries therefore ignore supervisor attempts (see TestAttempt.BySupervisor) - they belong to a
        // different actor under different rules and must not leak into the child's day.
        if (User.IsStudent())
        {
            // Resume instead of creating anew: cursor and order are persisted. Without that, an accidental
            // reload would burn one of the scarce attempts - the cap would then punish bad luck instead of farming.
            if (await db.TestAttempts
                    .Where(t => t.PlanPositionId == positionId && t.Day == day && t.CompletedAt == null && !t.BySupervisor)
                    .OrderBy(t => t.Id)
                    .FirstOrDefaultAsync(ct) is { } open)
                return new AttemptResponse(open.Id, planId, positionId, open.Day, open.StageValue, open.TotalItems);

            // The START is counted, not the submission: otherwise walking away would be free and the cap toothless.
            var used = await db.TestAttempts
                .CountAsync(t => t.PlanPositionId == positionId && t.Day == day && !t.BySupervisor, ct);
            if (used >= MaxAttemptsPerDay)
                return this.ProblemWithCode(ApiErrors.TestAttemptsExhausted,
                    "No test attempts left for today. Ask your parent.");
        }

        // Stage: only the supervisor may choose it freely; for the child the day's schedule/position stage applies.
        if (play.TypeOf(pos.Exercise) is not { } type)
            return this.ProblemWithCode(ApiErrors.UnknownExerciseType, "The exercise has an unknown type.");
        var stage = User.IsSupervisor() && dto.Stage is not null ? dto.Stage.Value : PositionPlayService.StageForDay(pos, plan, day, type);
        // A free display stage (B-96) has no question to grade - a test without a question is not a test,
        // regardless of who requests it (student schedule or supervisor preview).
        if (type.IsDisplayOnlyStage(stage))
            return this.ProblemWithCode(ApiErrors.StageNotTestable, "This stage is a free display stage and cannot be tested.");
        var typed = type.IsTypedStage(stage);

        // The test is a position check: test the contents already introduced, otherwise the whole pool
        // (it does not block when practice has nothing "due" yet).
        var progress = await db.PositionItemProgress
            .Where(p => p.PlanPositionId == positionId && p.ItemIndex < poolSize)
            .ToDictionaryAsync(p => p.ItemIndex, ct);
        var introduced = progress.Values.Where(p => p.IntroducedAt != null).Select(p => p.ItemIndex).ToList();
        var pool = introduced.Count > 0 ? introduced : Enumerable.Range(0, poolSize).ToList();
        // FREEZE the examination order per the position's strategy (strictly server-driven, no going back).
        var order = PositionPlayService.OrderIndices(pool.Select(i => (i, progress.GetValueOrDefault(i))), pos.OrderStrategy);

        var attempt = new TestAttempt
        {
            StudyPlanId = planId,
            PlanPositionId = positionId,
            Day = day,
            StageValue = stage,
            Graded = typed,
            // Stamp the actor: everything below the child's rules (resume, attempt cap) keys off it.
            BySupervisor = !User.IsStudent(),
            TotalItems = pool.Count,
            Order = [.. order],
            // The exercise sits on the attempt (through the position), not on every result row: ContentId was
            // an FK-less copy of PlanPosition.ExerciseId that nobody read.
            Results = order.Select(i => new TestItemResult { ItemIndex = i, StageValue = stage }).ToList(),
        };
        db.TestAttempts.Add(attempt);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { planId, positionId, attemptId = attempt.Id },
            new AttemptResponse(attempt.Id, planId, positionId, day, stage, attempt.TotalItems));
    }


    /// <summary>
    /// Returns the current test question at the cursor position (one-at-a-time, no going back). Items removed
    /// since the start are skipped. At the end of the order, <see cref="TestNextResponse.Done"/> is returned.
    /// </summary>
    [HttpGet("{attemptId:int}/next")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TestNextResponse>> Next(int planId, int positionId, int attemptId, CancellationToken ct = default)
    {
        var attempt = await LoadAttempt(planId, positionId, attemptId, ct);
        if (attempt is null) return NotFound();
        if (attempt.CompletedAt is not null)
            return new TestNextResponse(null, true, attempt.Cursor, attempt.TotalItems);
        var plan = (await GetPlan(planId, ct))!;
        if (User.IsStudent() && !PositionPlayService.PlanPlayableForChild(plan, DateOnly.FromDateTime(DateTime.UtcNow)))
            return this.ProblemWithCode(ApiErrors.PlanInactive, "This study plan is not currently active. Ask your parent.");
        var pos = await GetPosition(planId, positionId, ct);
        if (pos?.Exercise is null) return NotFound();

        var items = await play.ItemsOfAsync(pos, ct: ct);
        if (play.TypeOf(pos.Exercise) is not { } type)
            return this.ProblemWithCode(ApiErrors.UnknownExerciseType, "The exercise has an unknown type.");
        var typed = type.IsTypedStage(attempt.StageValue);
        var cursor = PositionPlayService.SkipRemoved(attempt.Order, attempt.Cursor, items.Count);
        if (cursor != attempt.Cursor) { attempt.Cursor = cursor; await db.SaveChangesAsync(ct); }
        if (cursor >= attempt.Order.Count) return new TestNextResponse(null, true, cursor, attempt.TotalItems);

        var item = ToItem(pos.Exercise, items, items[attempt.Order[cursor]], type, attempt.StageValue, typed);
        return new TestNextResponse(item, false, cursor, attempt.TotalItems);
    }


    /// <summary>
    /// Accepts the answer to the current test question, grades it server-side (and logs
    /// the plan-wide item progress), but does NOT return correctness (real class test:
    /// feedback only on <see cref="Submit"/>). Always addresses the cursor question – the client cannot
    /// bypass the order. The cursor then advances one question.
    /// </summary>
    [HttpPost("{attemptId:int}/answer")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnswerAck>> Answer(int planId, int positionId, int attemptId, AnswerDto dto, CancellationToken ct = default)
    {
        var attempt = await LoadAttempt(planId, positionId, attemptId, ct);
        if (attempt is null) return NotFound();
        if (attempt.CompletedAt is not null) return this.ProblemWithCode(ApiErrors.TestAlreadySubmitted, "The test has already been submitted.");
        var plan = (await GetPlan(planId, ct))!;
        if (User.IsStudent() && !PositionPlayService.PlanPlayableForChild(plan, DateOnly.FromDateTime(DateTime.UtcNow)))
            return this.ProblemWithCode(ApiErrors.PlanInactive, "This study plan is not currently active. Ask your parent.");
        var pos = await GetPosition(planId, positionId, ct);
        if (pos?.Exercise is null) return NotFound();

        var items = await play.ItemsOfAsync(pos, ct: ct);
        if (play.TypeOf(pos.Exercise) is not { } type)
            return this.ProblemWithCode(ApiErrors.UnknownExerciseType, "The exercise has an unknown type.");
        var typed = type.IsTypedStage(attempt.StageValue);
        var cursor = PositionPlayService.SkipRemoved(attempt.Order, attempt.Cursor, items.Count);
        if (cursor < attempt.Order.Count)
        {
            var index = attempt.Order[cursor];
            var item = items[index];
            // Set-graded exercise (an unordered list): the answer decides which entry it credits, not the card
            // it arrived on - any entry not yet credited in this attempt counts. A repeat therefore matches
            // nothing and is a wrong mention, exactly as the catalog check consumes a mention (B-77/E1, E2, E4).
            if (typed && type.GradesAsSet(PositionPlayService.ConfigOf(pos.Exercise)))
            {
                CreditSetAnswer(attempt, items, CreditedEntries(attempt), dto.GivenAnswer, grader);
            }
            else
            {
                var result = attempt.Results.FirstOrDefault(r => r.ItemIndex == index);
                var correct = typed
                    ? item.AcceptedAnswers.Any(a => grader.Matches(dto.GivenAnswer, a))
                    : dto.WasKnown ?? false;
                if (result is not null)
                {
                    result.GivenAnswer = dto.GivenAnswer;
                    result.WasCorrect = correct;
                }
            }
            // Deliberately NO cross-plan recording here: item progress and history are written ONCE on
            // completion (submit), so that aborted/repeated attempts do not distort the learning state
            // (otherwise every intermediate answer would count permanently, even without a submission).
            cursor++;
        }
        attempt.Cursor = PositionPlayService.SkipRemoved(attempt.Order, cursor, items.Count);
        await db.SaveChangesAsync(ct);
        return new AnswerAck(attempt.Cursor >= attempt.Order.Count, attempt.Cursor, attempt.TotalItems);
    }


    /// <summary>A test attempt together with individual results.</summary>
    [HttpGet("{attemptId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttemptDetail>> Get(int planId, int positionId, int attemptId, CancellationToken ct = default)
    {
        var a = await LoadAttempt(planId, positionId, attemptId, ct);
        if (a is null) return NotFound();
        // Only rows that carry an atom: a set-mode wrong mention has none, and `?? 0` would report it as an
        // answer to the first entry. The mentions themselves belong to the child's result screen
        // (SubmitResponse), not to this per-item view.
        return new AttemptDetail(a.Id, a.StudyPlanId, positionId, a.Day, a.StageValue, a.StartedAt, a.CompletedAt,
            a.TotalItems, a.CorrectItems, a.ScorePercent, a.Passed,
            a.Results.Where(r => r.ItemIndex is not null).OrderBy(r => r.ItemIndex)
                .Select(r => new ItemResultDto(r.ItemIndex!.Value, r.GivenAnswer, r.WasCorrect, r.HintsUsed)).ToList());
    }


    /// <summary>
    /// Concludes the attempt and returns the result (incl. solutions). In class-test mode the answers were
    /// already graded step by step via <see cref="Answer"/>; here only aggregation happens. If exceptionally a
    /// <paramref name="dto"/> with answers is passed (bulk submission), these are still graded server-side.
    /// </summary>
    [HttpPost("{attemptId:int}/submit")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubmitResponse>> Submit(int planId, int positionId, int attemptId, SubmitDto dto, CancellationToken ct = default)
    {
        var attempt = await LoadAttempt(planId, positionId, attemptId, ct);
        if (attempt is null) return NotFound();
        if (attempt.CompletedAt is not null) return this.ProblemWithCode(ApiErrors.TestAlreadySubmitted, "The test has already been submitted.");
        var plan = (await GetPlan(planId, ct))!;
        // Anti-cheat: the child must not complete and score a plan that has since been deactivated or expired
        // through an open test attempt either (the supervisor stays exempt).
        if (User.IsStudent() && !PositionPlayService.PlanPlayableForChild(plan, DateOnly.FromDateTime(DateTime.UtcNow)))
            return this.ProblemWithCode(ApiErrors.PlanInactive, "This study plan is not currently active. Ask your parent.");
        var pos = await GetPosition(planId, positionId, ct);
        if (pos?.Exercise is null) return NotFound();

        var items = await play.ItemsOfAsync(pos, ct: ct);
        if (play.TypeOf(pos.Exercise) is not { } type)
            return this.ProblemWithCode(ApiErrors.UnknownExerciseType, "The exercise has an unknown type.");
        var typed = type.IsTypedStage(attempt.StageValue);

        var setMode = typed && type.GradesAsSet(PositionPlayService.ConfigOf(pos.Exercise));

        // Bulk submission (legacy/fallback): only GRADE the answers passed in (recording follows once below).
        // In the class-test flow the list is empty - the results already stand from the step-wise /answer calls.
        if (dto.Answers is { Count: > 0 } bulk)
        {
            if (setMode)
            {
                // A set in one go: the answers are walked in the order they were given, each crediting the first
                // entry still open. Same rule as the step-wise path - the index they carry is meaningless here.
                // Capped at the number of cards: every answer beyond that could only ever be a wrong mention,
                // and each one writes a row. The step-wise path is bounded by the cursor; this one is bounded
                // by nothing but the request body, so a client could turn one submission into a hundred
                // thousand rows.
                var credited = CreditedEntries(attempt);
                foreach (var answer in bulk.Take(attempt.Order.Count))
                    CreditSetAnswer(attempt, items, credited, answer.GivenAnswer, grader);
            }
            else
            {
                // Indexed assignment, not ToDictionary: a duplicate itemIndex in the body is a client mistake
                // worth a 400 at most, and ToDictionary answered it with an unhandled 500. The last mention
                // wins, as everywhere else answers are folded by index.
                var answers = new Dictionary<int, AnswerDto>();
                foreach (var a in bulk) answers[a.ItemIndex] = a;
                foreach (var result in attempt.Results)
                {
                    // The item may have been removed/reordered since the test started (item CRUD); skip indexes that
                    // no longer exist instead of running out of range or grading the wrong word. A row without an
                    // index is skipped too: it can only be a wrong mention from a set-mode answer (the exercise's
                    // `Ordered` flag can be flipped mid-attempt), and it belongs to no entry.
                    if (result.ItemIndex is not { } index || index < 0 || index >= items.Count) continue;
                    if (!answers.TryGetValue(index, out var answer)) continue;
                    var item = items[index];
                    result.GivenAnswer = answer.GivenAnswer;
                    result.WasCorrect = typed
                        ? item.AcceptedAnswers.Any(a => grader.Matches(answer.GivenAnswer, a))
                        : answer.WasKnown ?? false;
                }
            }
        }

        // Only rows that carry an item which still exists (not deleted mid-test) count - for result cards,
        // recording AND the score. A row WITHOUT an item is a wrong mention (set mode): it must not become
        // "entry 0" here, or it would score against the first entry and depress the percentage twice.
        var scorable = attempt.Results
            .Where(r => r.ItemIndex is { } i && i >= 0 && i < items.Count)
            .OrderBy(r => r.ItemIndex)
            .ToList();
        var wrongMentions = attempt.Results
            .Where(r => r.ItemIndex is null && !string.IsNullOrWhiteSpace(r.GivenAnswer))
            .Select(r => r.GivenAnswer!)
            .ToList();

        // Build the result cards and write the cross-plan item progress/history exactly ONCE per completed
        // attempt (not per intermediate answer) - idempotency against abort/repeat.
        var outcomes = new List<ItemOutcome>(scorable.Count);
        foreach (var r in scorable)
        {
            // Non-null by the filter above - the coalescing that used to stand here would have hidden a wrong
            // mention as an answer to entry 0.
            var index = r.ItemIndex!.Value;
            var item = items[index];
            outcomes.Add(new ItemOutcome(index, item.Prompt, item.Answer, r.GivenAnswer, r.WasCorrect, item.GapIndex));
            await itemProgress.RecordAsync(plan.ChildId, pos.ExerciseId, item, r.WasCorrect, attempt.StageValue,
                typed ? r.GivenAnswer : null, ItemReviewSource.Test, positionId, attempt.Day, countsForMastery: true, ct: ct);
        }

        var passPercent = pos.GoalThreshold is > 0 ? pos.GoalThreshold.Value : DefaultPassPercent;
        // The score is computed over the questions actually asked (still existing), not over the frozen start
        // count: an item deleted mid-test must not secretly lower the reachable score.
        attempt.TotalItems = scorable.Count;
        attempt.CorrectItems = scorable.Count(r => r.WasCorrect);
        attempt.ScorePercent = attempt.TotalItems == 0 ? 0
            : (int)Math.Round(100.0 * attempt.CorrectItems / attempt.TotalItems);
        attempt.Passed = attempt.ScorePercent >= passPercent;
        attempt.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        // Book the position's goal points (idempotent) BEFORE the gamification, so that coin-based missions
        // already see the fresh credit.
        var dayOverview = await progress.EvaluateAndAwardAsync(plan, attempt.Day, ct);
        // Also evaluate metric-based missions/awards (e.g. "tests passed") on test completion.
        await gamification.EvaluateAndAwardAsync(plan.ChildId, attempt.Day, ct);
        // The daily reward box: same occasion as the goal reward above, once the day's duty is fully met.
        await dailyBox.EvaluateAndAwardAsync(plan, attempt.Day, dayOverview, ct);

        // The wrong mentions ride along only where they exist: in set mode the outcomes above name what the
        // child FORGOT, and without this list what it actually typed would silently disappear.
        return new SubmitResponse(attempt.Id, attempt.StageValue, attempt.TotalItems, attempt.CorrectItems,
            attempt.ScorePercent, attempt.Passed, passPercent, outcomes,
            wrongMentions.Count > 0 ? wrongMentions : null);
    }
}
