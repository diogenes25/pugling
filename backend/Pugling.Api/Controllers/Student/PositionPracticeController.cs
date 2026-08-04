using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Student;

/// <summary>
/// Practicing a single study plan position (new model): the child plays the content of ONE exercise,
/// progress runs per content atom via <see cref="PositionItemProgress"/>. Content comes from the
/// exercise config (<see cref="ExerciseContentProvider"/>), grading is type-neutral against the item solution.
/// Replaces the former plan-wide practice session.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Student + "/study-plans/{planId:int}/positions/{positionId:int}/practice-sessions")]
[Tags("Student – Position Practice")]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(PlanOwnershipFilter))]
public class PositionPracticeController(PuglingDbContext db, PositionPlayService play, ScoringService scoring,
    PositionProgressService progress, GamificationService gamification, AnswerGrader grader,
    ItemProgressService itemProgress, MediaSelector selector, ILogger<PositionPracticeController> logger,
    TimeProvider time)
    : ControllerBase
{
    /// <summary>Upper bound of the seconds creditable per heartbeat (anti time cheat).</summary>
    private const int MaxHeartbeatSeconds = 120;

    private static SessionResponse Map(PracticeSession s) =>
        new(s.Id, s.StudyPlanId, s.PlanPositionId ?? 0, s.Day, s.StartedAt, s.EndedAt, s.ActiveSeconds,
            s.Reviews.Count, s.Mode, s.Cursor, s.Order.Count);

    // No default for `ct`: it would make the call site look correct while the client's cancellation fizzles
    // out - neither CA2016 nor the guard sees an omitted optional argument.
    private Task<StudyPlan?> GetPlan(int planId, CancellationToken ct) =>
        db.StudyPlans.FirstOrDefaultAsync(p => p.Id == planId, ct);

    // The plan comes along because the imagery needs the child (the selection hangs on its profile).
    private Task<PlanPosition?> GetPosition(int planId, int positionId, CancellationToken ct) =>
        db.PlanPositions.Include(p => p.Exercise).Include(p => p.StudyPlan)
            .FirstOrDefaultAsync(p => p.Id == positionId && p.StudyPlanId == planId, ct);

    private Task<PracticeSession?> GetSession(int planId, int positionId, int sessionId, CancellationToken ct) =>
        db.PracticeSessions.Include(s => s.Reviews)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.StudyPlanId == planId && s.PlanPositionId == positionId, ct);

    /// <summary>Starts a practice session for the position. Day only for backdating (father); otherwise today.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessionResponse>> Start(int planId, int positionId, StartPracticeDto dto, CancellationToken ct = default)
    {
        var pos = await GetPosition(planId, positionId, ct);
        if (pos is null) return NotFound();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (dto.Day is { } d && d != today && !User.IsSupervisor())
            return Forbid(); // catching up on other days is for the supervisor only (anti-cheat).
        // Anti-cheat: the child may only play its active, running plan - no cherry-picking easy or expired
        // plans for comfortable points. The supervisor may play any time (preview/catch-up).
        if (User.IsStudent() && await GetPlan(planId, ct) is { } plan && !PositionPlayService.PlanPlayableForChild(plan, today))
            return this.ProblemWithCode(ApiErrors.PlanInactive, "This study plan is not currently active. Ask your parent.");

        var day = dto.Day ?? today;
        var session = new PracticeSession { StudyPlanId = planId, PlanPositionId = positionId, Day = day, Mode = dto.Mode };
        // Freeze the order ONCE (per the position's strategy) so that it does not shift during the run and
        // cursor (learn) and batch (info/offline) use the same stable sequence.
        // Info = free practice: the whole scope-filtered pool (no Leitner due date), so that already learned
        // words can be repeated too. Learn = only the cards due today.
        session.Order = [.. await play.DueItemIndicesAsync(pos, day, pos.OrderStrategy, dueOnly: dto.Mode != PlayMode.Info, ct)];
        db.PracticeSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { planId, positionId, sessionId = session.Id }, Map(session));
    }

    /// <summary>A practice session of the position.</summary>
    [HttpGet("{sessionId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessionResponse>> Get(int planId, int positionId, int sessionId, CancellationToken ct = default) =>
        await GetSession(planId, positionId, sessionId, ct) is { } s ? Map(s) : NotFound();

    /// <summary>Adds (active) practice seconds (anti time cheat: capped per heartbeat).</summary>
    [HttpPost("{sessionId:int}/heartbeat")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessionResponse>> Heartbeat(int planId, int positionId, int sessionId, HeartbeatDto dto, CancellationToken ct = default)
    {
        var session = await GetSession(planId, positionId, sessionId, ct);
        if (session is null) return NotFound();
        if (dto.Active && dto.Seconds > 0) session.ActiveSeconds += Math.Clamp(dto.Seconds, 0, MaxHeartbeatSeconds);
        await db.SaveChangesAsync(ct);
        return Map(session);
    }

    /// <summary>
    /// Builds a practice card from a content atom. Typed stages withhold the solution
    /// (the server grades, never the frontend); display/self-assessment and the listening stage reveal
    /// it by design, or supply the audio source.
    /// </summary>
    private static PracticeCard BuildCard(Exercise exercise, IExerciseType type, int stage, bool typed,
        IReadOnlyList<ContentItem> items, int index)
    {
        var item = items[index];
        var f = PositionPlayService.CardFacets(PositionPlayService.ConfigOf(exercise), items, item, type, stage, typed);
        return new PracticeCard(index, stage, type.Key, f.Prompt,
            f.Hint, f.AnswerLength, f.Reveal, f.Choices, f.AudioUrl, f.ImageUrl, f.ImageAlt, f.GapIndex, f.Passage,
            f.AnyOrder, f.RevealAlternatives, f.Decoding);
    }

    /// <summary>
    /// The entries already named in a set-graded exercise within the round's period – the progress rows
    /// stamped on that day. There is no session state for this and none is needed: in set mode a review only
    /// ever touches a row on a hit (see <see cref="Review"/>), so a stamp from that day means "this entry was
    /// named".
    /// <para>
    /// Two windows, not one span: the session's day <b>and</b> the current one. Normally they are the same;
    /// they part when a round runs past UTC midnight or when the supervisor backdates one
    /// (<c>StartPracticeDto.Day</c>) – the stamp then carries the wall clock while the round belongs to
    /// another day. Covering both is the safe direction, because a missed stamp does not merely lose a
    /// detail: every remaining card would accept the same answer again. A single span across both days would
    /// be worse than two windows – a round backdated by a week would swallow every entry practised in
    /// between and reject answers that are right.
    /// </para>
    /// <para>
    /// Compared as UTC ranges rather than by casting the column to a date: the cast does not translate, and
    /// EF rejects the query instead of grading it – correct, but only at runtime.
    /// </para>
    /// <para>
    /// Consequence, deliberate: a <b>second round on the same day</b> has every entry named already, so each
    /// answer is a wrong mention – on the day, nothing counts twice, and a repeat is a wrong mention no matter
    /// which round it arrives in. Reporting it as "not correct" rather than as its own third state is the price;
    /// there are only right and wrong, and a milder practice rule than the exam's would teach a rule that does
    /// not hold in the exam. It stays invisible on a Leitner position (a credited entry is no longer due and
    /// never enters the next round's order) and reachable only through the API otherwise, because the child's
    /// app offers no practice button for a testable position without Leitner.
    /// </para>
    /// </summary>
    private async Task<HashSet<int>> NamedInRoundAsync(int positionId, DateOnly day, CancellationToken ct)
    {
        var dayFrom = day.ToDateTime(TimeOnly.MinValue);
        var dayTo = dayFrom.AddDays(1);
        var todayFrom = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime).ToDateTime(TimeOnly.MinValue);
        var todayTo = todayFrom.AddDays(1);
        return [.. await db.PositionItemProgress.AsNoTracking()
            .Where(p => p.PlanPositionId == positionId
                && ((p.LastReviewedAt >= dayFrom && p.LastReviewedAt < dayTo)
                    || (p.LastReviewedAt >= todayFrom && p.LastReviewedAt < todayTo)))
            .Select(p => p.ItemIndex)
            .ToListAsync(ct)];
    }

    /// <summary>
    /// Returns all cards of the session in the order frozen at start – for the info mode
    /// (free practice, frontend iterates) and as an offline fallback of the learn mode (answers are buffered
    /// and sent later via <see cref="Review"/> on reconnect). The learn mode otherwise uses <see cref="Next"/>.
    /// </summary>
    [HttpGet("{sessionId:int}/cards")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PracticeCard>>> Cards(int planId, int positionId, int sessionId, CancellationToken ct = default)
    {
        var session = await GetSession(planId, positionId, sessionId, ct);
        if (session is null) return NotFound();
        var plan = (await GetPlan(planId, ct))!;
        // Anti-cheat: even with a session still open, the child must not keep practicing a plan that has since
        // been deactivated or expired (the supervisor stays exempt for preview/catch-up).
        if (User.IsStudent() && !PositionPlayService.PlanPlayableForChild(plan, DateOnly.FromDateTime(DateTime.UtcNow)))
            return this.ProblemWithCode(ApiErrors.PlanInactive, "This study plan is not currently active. Ask your parent.");
        var pos = await GetPosition(planId, positionId, ct);
        if (pos?.Exercise is null) return NotFound();

        var items = await play.ItemsOfAsync(pos, pos.StudyPlan?.ChildId, ct);
        if (play.TypeOf(pos.Exercise) is not { } type)
            return this.ProblemWithCode(ApiErrors.UnknownExerciseType, "The exercise has an unknown type.");
        var stage = PositionPlayService.StageForDay(pos, plan, session.Day, type);
        var typed = type.IsTypedStage(stage);

        // The frozen order; skip items removed since the start (item CRUD).
        return session.Order.Where(i => i >= 0 && i < items.Count)
            .Select(i => BuildCard(pos.Exercise, type, stage, typed, items, i)).ToList();
    }

    /// <summary>
    /// "Different image" for a card. Deliberately addressed here and not at the child: which carrier
    /// holds the pick – the exercise-local override or the store vocabulary – only follows from the
    /// specificity cascade that only the server knows. The client only has the card in front of it.
    /// <para>
    /// The rejected image never comes up again for this carrier. If there is no alternative, everything
    /// stays unchanged (<c>409 media_no_alternative</c>) instead of leaving the card without an image.
    /// </para>
    /// <para>
    /// The endpoint <b>hands out an image</b> and therefore carries the same guards as the playback
    /// itself: only the playable plan, only cards of this session, and only where the card actually shows an
    /// image. Without the last guard it would be the hole in the anti-cheat rule – on a typed
    /// stage it would deliver image <i>and</i> alt text and thereby the meaning of exactly the word that is
    /// supposed to be typed (<see cref="PositionPlayService.CardFacets"/> deliberately withholds both there).
    /// </para>
    /// </summary>
    [HttpPost("{sessionId:int}/cards/{itemIndex:int}/image/reshuffle")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SelectedMediaResponse>> ReshuffleImage(int planId, int positionId,
        int sessionId, int itemIndex, CancellationToken ct)
    {
        var session = await GetSession(planId, positionId, sessionId, ct);
        if (session is null) return NotFound();
        var pos = await GetPosition(planId, positionId, ct);
        if (pos?.Exercise is null || pos.StudyPlan is null) return NotFound();
        if (User.IsStudent()
            && !PositionPlayService.PlanPlayableForChild(pos.StudyPlan, DateOnly.FromDateTime(DateTime.UtcNow)))
            return this.ProblemWithCode(ApiErrors.PlanInactive, "This study plan is not currently active. Ask your parent.");

        // Cards of this session only: the order frozen at the start is exactly the set that is served (and it
        // lies within the position's pool). A free index could otherwise enumerate the motifs of the whole
        // exercise - including those of cards this session never shows.
        if (!session.Order.Contains(itemIndex)) return NotFound();

        var items = await play.ItemsOfAsync(pos, pos.StudyPlan.ChildId, ct);
        if (items.FirstOrDefault(i => i.Index == itemIndex) is not
            { ItemId: { } itemId, VocabularyId: { } vocabId } item)
            return NotFound();
        if (play.TypeOf(pos.Exercise) is not { } type)
            return this.ProblemWithCode(ApiErrors.UnknownExerciseType, "The exercise has an unknown type.");

        // Only the image the card actually shows can be re-chosen - and the stage comes from the schedule as
        // everywhere else, never from the client.
        var stage = PositionPlayService.StageForDay(pos, pos.StudyPlan, session.Day, type);
        if (PositionPlayService.CardFacets(PositionPlayService.ConfigOf(pos.Exercise), items, item, type, stage, type.IsTypedStage(stage)).ImageUrl is null)
            return this.ProblemWithCode(ApiErrors.MediaNotOnCard, "This card does not show an image.");

        var picked = await selector.ReshuffleForItemAsync(pos.StudyPlan.ChildId, itemId, vocabId, ct: ct);
        if (picked is null)
            return this.ProblemWithCode(ApiErrors.MediaNoAlternative, "There is no other image available for this card.");

        return new SelectedMediaResponse(picked.MediaAssetId, picked.Url, picked.Alt);
    }

    /// <summary>
    /// Returns the current card at the session's cursor position (learn mode, one-at-a-time). Items removed
    /// since the start are skipped. If the cursor is at the end, <see cref="NextResponse.Done"/> is returned.
    /// </summary>
    [HttpGet("{sessionId:int}/next")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NextResponse>> Next(int planId, int positionId, int sessionId, CancellationToken ct = default)
    {
        var session = await GetSession(planId, positionId, sessionId, ct);
        if (session is null) return NotFound();
        var plan = (await GetPlan(planId, ct))!;
        if (User.IsStudent() && !PositionPlayService.PlanPlayableForChild(plan, DateOnly.FromDateTime(DateTime.UtcNow)))
            return this.ProblemWithCode(ApiErrors.PlanInactive, "This study plan is not currently active. Ask your parent.");
        var pos = await GetPosition(planId, positionId, ct);
        if (pos?.Exercise is null) return NotFound();

        var items = await play.ItemsOfAsync(pos, pos.StudyPlan?.ChildId, ct);
        if (play.TypeOf(pos.Exercise) is not { } type)
            return this.ProblemWithCode(ApiErrors.UnknownExerciseType, "The exercise has an unknown type.");
        var stage = PositionPlayService.StageForDay(pos, plan, session.Day, type);
        var typed = type.IsTypedStage(stage);

        var cursor = PositionPlayService.SkipRemoved(session.Order, session.Cursor, items.Count);
        if (cursor != session.Cursor) { session.Cursor = cursor; await db.SaveChangesAsync(ct); }
        if (cursor >= session.Order.Count) return new NextResponse(null, true, cursor, session.Order.Count);

        var card = BuildCard(pos.Exercise, type, stage, typed, items, session.Order[cursor]);
        return new NextResponse(card, false, cursor, session.Order.Count);
    }

    /// <summary>
    /// Accepts the answer to a practice card, grades it server-side against the item solution
    /// and logs the review. For Leitner positions the atom moves the box up/down,
    /// correct answers earn points (+ combo/speed bonus). Anti-farming: only one
    /// due card is scored and at most once per day; non-typed self-assessment does not count with
    /// <see cref="PlanPosition.RequireTypedTest"/>. The server cursor then advances one card
    /// and returns the next one right away. In <see cref="PlayMode.Info"/> no feedback flows (204).
    /// </summary>
    [HttpPost("{sessionId:int}/review")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewOutcome>> Review(int planId, int positionId, int sessionId, ReviewDto dto, CancellationToken ct = default)
    {
        var session = await GetSession(planId, positionId, sessionId, ct);
        if (session is null) return NotFound();
        var plan = (await GetPlan(planId, ct))!;
        // Anti-cheat: even with a session still open, the child must not keep practicing a plan that has since
        // been deactivated or expired (the supervisor stays exempt for preview/catch-up).
        if (User.IsStudent() && !PositionPlayService.PlanPlayableForChild(plan, DateOnly.FromDateTime(DateTime.UtcNow)))
            return this.ProblemWithCode(ApiErrors.PlanInactive, "This study plan is not currently active. Ask your parent.");
        var pos = await GetPosition(planId, positionId, ct);
        if (pos?.Exercise is null) return NotFound();

        // Info mode: free practice without any learning feedback - record nothing, score nothing.
        if (session.Mode == PlayMode.Info) return NoContent();

        var items = await play.ItemsOfAsync(pos, pos.StudyPlan?.ChildId, ct);
        if (dto.ItemIndex < 0 || dto.ItemIndex >= play.PoolSize(pos, items.Count))
            return this.ProblemWithCode(ApiErrors.NotFound, "The content does not belong to this position.");
        var item = items[dto.ItemIndex];

        // Enforce the stage server-side (not selectable by the client) and grade type-agnostically.
        if (play.TypeOf(pos.Exercise) is not { } type)
            return this.ProblemWithCode(ApiErrors.UnknownExerciseType, "The exercise has an unknown type.");
        var stage = PositionPlayService.StageForDay(pos, plan, session.Day, type);
        var typed = type.IsTypedStage(stage);

        // A set-graded exercise (an unordered list) is not answered card by card: any entry not yet named today
        // counts, and the ANSWER decides which entry it credits - not the card it arrived on. A miss credits
        // nothing, because there is no atom to attribute it to: naming one of a dozen open entries as "the"
        // solution would be arbitrary, and demoting it would punish an entry the child never claimed.
        var setMode = typed && type.GradesAsSet(PositionPlayService.ConfigOf(pos.Exercise));
        int? creditedIndex = dto.ItemIndex;
        if (setMode)
        {
            var named = await NamedInRoundAsync(positionId, session.Day, ct);
            creditedIndex = PositionPlayService.MatchOpenEntry(items,
                session.Order.Where(i => !named.Contains(i)), dto.GivenAnswer, grader);
        }

        var wasCorrect = setMode ? creditedIndex is not null
            : typed ? item.AcceptedAnswers.Any(a => grader.Matches(dto.GivenAnswer, a))
            : dto.WasKnown ?? false;

        // Outside set mode the credited atom is always the card's own, so `prog` is never null there.
        var prog = creditedIndex is { } credited ? await play.ProgressForAsync(positionId, credited, ct) : null;
        // First contact counts as the introduction - otherwise IntroducedAt/DueOn would stand still for purely
        // practice-based learning (due dates, the "new/old" scope).
        if (prog is not null && prog.IntroducedAt is null)
        {
            prog.IntroducedAt = session.Day;
            prog.DueOn ??= session.Day;
        }

        var due = prog is null || prog.DueOn is null || prog.DueOn <= session.Day;
        var alreadyScoredToday = prog?.LastReviewedAt is { } last && DateOnly.FromDateTime(last) == session.Day;
        var scored = prog is not null && (typed || !pos.RequireTypedTest) && due && !alreadyScoredToday;

        // Combo/answer time BEFORE adding the new review (EF fixup would otherwise count it in).
        var prevStreak = 0;
        foreach (var r in session.Reviews.OrderByDescending(r => r.At).ThenByDescending(r => r.Id))
        {
            if (r.WasCorrect) prevStreak++; else break;
        }
        // Answer time and timestamp come from the SAME clock (TimeProvider): the gap between two answers is the
        // basis of the fast-answer bonus including its anti-farming lower bound of one second. A rule in the
        // second range cannot be tested against the wall clock - a test would have to push two requests through
        // within one second and becomes a flake under load that looks like a points regression
        // (docs/testplan.md, stage 3). With the shared, replaceable clock the measured time is an input instead
        // of a hope.
        var lastAt = session.Reviews.Count > 0 ? session.Reviews.Max(r => r.At) : (DateTime?)null;
        var now = time.GetUtcNow().UtcDateTime;
        double? elapsedSeconds = lastAt is { } la ? (now - la).TotalSeconds : null;

        // In set mode the progress row IS the "already named" marker (see NamedInRoundAsync), so a hit stamps
        // it even on a position without Leitner (where ApplyReview below never runs) - otherwise the same
        // answer would be accepted again on the very next card. Stamped from the same clock as the review
        // below, not a second reading of it.
        if (setMode && wasCorrect && prog is not null) prog.LastReviewedAt = now;

        // Correctness and timestamp only: from them come the combo streak, the answer time and the metric
        // CorrectReviews. Which atom it was is recorded by the ItemReviewEvent below through the stable ItemId -
        // here it would be an index-addressed second truth without a reader.
        db.ReviewEvents.Add(new ReviewEvent
        {
            PracticeSessionId = sessionId,
            WasCorrect = wasCorrect && scored,
            At = now,
        });

        // Also write the cross-plan item progress + answer history (vocabulary items with a stable ItemId only).
        // Deliberately with the actual correctness (not anti-farming damped) - this is a reporting layer, not
        // the points source; it is persisted through the SaveChanges below.
        // In set mode the credited entry is the subject of the record - and a miss is recorded for NOBODY:
        // booking it against the card's own entry would claim the child got that particular one wrong, which is
        // exactly the false attribution this story removes.
        if (creditedIndex is { } recorded)
        {
            await itemProgress.RecordAsync(plan.ChildId, pos.ExerciseId, items[recorded], wasCorrect, stage,
                typed ? dto.GivenAnswer : null, ItemReviewSource.Practice, positionId, session.Day, countsForMastery: scored, ct: ct);
        }

        // Points/box only on Leitner positions and only for graded cards (anti-farming). Otherwise 0.
        int awarded = 0, comboBonus = 0, speedBonus = 0, combo = 0;
        var leitnerScored = pos.UseLeitner && scored;
        // `scored` already implies a credited atom; the null check is the compiler's proof, not a second rule.
        if (leitnerScored && prog is not null)
        {
            combo = wasCorrect ? prevStreak + 1 : 0;
            var (preBox, preReviewCount) = (prog.Box, prog.ReviewCount);
            // The shared clock, not the wall one: ApplyReview stamps LastReviewedAt, which in set mode is the
            // "already named" marker read back above. A second reading would put the stamp outside the window
            // a frozen test clock defines, and then every remaining card would accept the same answer again.
            play.ApplyReview(pos, prog, wasCorrect, session.Day, now);

            var cfg = new ScoringService.ScoreConfig($"{plan.Title} · {pos.Exercise.Title}", pos.NewContentPoints,
                pos.ComboThreshold, pos.ComboBonusPoints, pos.SpeedThresholdSeconds, pos.SpeedBonusPoints,
                pos.TimeSlots);
            var score = scoring.ScoreReview(cfg, preReviewCount, preBox, prog.Box, wasCorrect, combo,
                DateTime.Now, elapsedSeconds);
            foreach (var c in score.Contributions)
                db.ChildPointsEntries.Add(new ChildPointsEntry { ChildId = plan.ChildId, Kind = c.Kind, Amount = c.Amount, Reason = c.Reason });
            awarded = score.BasePoints;
            comboBonus = score.ComboBonus;
            speedBonus = score.SpeedBonus;

            if (score.Total > 0)
                // The CREDITED entry, not the card it arrived on: in set mode those differ, and the card index is
                // the one value the booking has nothing to do with.
                logger.LogInformation(
                    "Positions-Wiederholung gewertet: Kind {ChildId} Plan {PlanId} Position {PositionId} Item {ItemIndex} " +
                    "→ +{Total} Punkte (Basis {Base}, Combo ×{Combo} +{ComboBonus}, Speed +{SpeedBonus})",
                    plan.ChildId, planId, positionId, prog.ItemIndex, score.Total, score.BasePoints, combo,
                    score.ComboBonus, score.SpeedBonus);
        }

        // Server cursor: onto the card just answered (a valid one), one further, then skip removed ones.
        var cursor = PositionPlayService.SkipRemoved(session.Order, session.Cursor, items.Count);
        if (cursor < session.Order.Count) cursor++;
        session.Cursor = PositionPlayService.SkipRemoved(session.Order, cursor, items.Count);

        await db.SaveChangesAsync(ct);

        if (leitnerScored)
            await gamification.EvaluateAndAwardAsync(plan.ChildId, session.Day, ct);

        var done = session.Cursor >= session.Order.Count;
        var next = done ? null : BuildCard(pos.Exercise, type, stage, typed, items, session.Order[session.Cursor]);
        // Without a credited atom there is no solution to name and no box that moved (set mode, wrong answer):
        // the feedback stays empty instead of pointing at the card's own entry, which would both be arbitrary
        // and give away an entry that is still to be asked for.
        return new ReviewOutcome(wasCorrect, creditedIndex is { } shown ? items[shown].Answer : null,
            awarded, prog?.Box ?? 0, prog?.DueOn, combo, comboBonus, speedBonus, next, done);
    }

    /// <summary>
    /// Ends the session and evaluates time-based missions. A round that was in flight when the plan expired
    /// or was switched off can still be closed; only the goal points are withheld then.
    /// </summary>
    [HttpPost("{sessionId:int}/end")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessionResponse>> End(int planId, int positionId, int sessionId, CancellationToken ct = default)
    {
        var session = await GetSession(planId, positionId, sessionId, ct);
        if (session is null) return NotFound();
        var plan = (await GetPlan(planId, ct))!;
        // Anti-cheat: the booking below does NOT check `Active` itself, so a plan deactivated or expired
        // mid-round could otherwise still be driven to the points of the running period.
        // Closing the round is not the danger and stays allowed on purpose: rejecting the write would leave the
        // session open forever (the frontend closes it from an effect cleanup and swallows the error), and an
        // open session is worth no less to the child than a closed one - the goal rule reads cursor and order,
        // not EndedAt.
        var payable = !User.IsStudent() || PositionPlayService.PlanPlayableForChild(plan, DateOnly.FromDateTime(DateTime.UtcNow));

        session.EndedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        if (!payable) return Map(session);

        // The position's goal points (idempotent): this mainly covers pure content/reading exercises whose goal
        // is met by a round played far enough. BEFORE the gamification, so that missions see the credit.
        await progress.EvaluateAndAwardAsync(plan, session.Day, ct);
        await gamification.EvaluateAndAwardAsync(plan.ChildId, session.Day, ct);
        return Map(session);
    }
}
