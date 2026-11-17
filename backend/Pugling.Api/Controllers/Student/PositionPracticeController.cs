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
    TimeProvider time, DailyBoxService dailyBox)
    : ControllerBase
{
    /// <summary>Upper bound of the seconds creditable per heartbeat (anti time cheat).</summary>
    private const int MaxHeartbeatSeconds = 120;

    // Not static (B-114 follow-up): Testable needs `progress`, and PlanPosition/StudyPlan are only
    // reliably present when the caller loaded them - see `GetSession`'s Includes and `Start`'s assignment.
    // `withAttempts` is false only for the heartbeat (B-522 review): it fires every few seconds, and the count costs a
    // query pair the child's screen never reads from that answer.
    private async Task<SessionResponse> MapAsync(PracticeSession s, bool withAttempts, CancellationToken ct) =>
        new(s.Id, s.StudyPlanId, s.PlanPositionId ?? 0, s.Day, s.StartedAt, s.EndedAt, s.ActiveSeconds,
            s.Reviews.Count, s.Mode, s.Cursor, s.Order.Count,
            s.PlanPosition is { } pos && pos.StudyPlan is { } plan && progress.IsTestable(pos, plan, s.Day),
            withAttempts ? await TestAttemptsLeftAsync(s, ct) : null,
            withAttempts ? await RemediationOfAsync(s, ct) : null);

    // B-635: the round count and the done tasks of a remediation position - its learn rounds up to this one, and the
    // tasks of the pool typed freely right. Skipped by the heartbeat like the attempts count.
    private async Task<RemediationState?> RemediationOfAsync(PracticeSession s, CancellationToken ct)
    {
        if (s.PlanPosition is not { Remediation: true } pos || s.Mode != PlayMode.Lern) return null;
        var round = await db.PracticeSessions.CountAsync(
            x => x.PlanPositionId == pos.Id && x.Mode == PlayMode.Lern && x.Id <= s.Id, ct);
        var total = play.PoolSize(pos, (await play.ItemsOfAsync(pos, ct: ct)).Count);
        var done = await db.PositionItemProgress.CountAsync(
            p => p.PlanPositionId == pos.Id && p.RemediationDoneAt != null && p.ItemIndex < total, ct);
        return new RemediationState(round, done, total);
    }

    // The same exemption as the exam start (PositionTestsController.Start): the cap binds every token with the student
    // role, so a token carrying both roles sees the count the start will enforce, not the supervisor's maximum.
    private async Task<int> TestAttemptsLeftAsync(PracticeSession s, CancellationToken ct) =>
        s.PlanPositionId is { } positionId
            ? await progress.TestAttemptsLeftAsync(positionId, s.Day, forSupervisor: !User.IsStudent(), ct)
            : 0;

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
            .Include(s => s.PlanPosition!).ThenInclude(p => p.Exercise)
            .Include(s => s.PlanPosition!).ThenInclude(p => p.StudyPlan)
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
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        if (dto.Day is { } d && d != today && !User.IsSupervisor())
            return Forbid(); // catching up on other days is for the supervisor only (anti-cheat).
        // Anti-cheat: the child may only play its active, running plan - no cherry-picking easy or expired
        // plans for comfortable points. The supervisor may play any time (preview/catch-up).
        if (User.IsStudent() && await GetPlan(planId, ct) is { } plan && !PositionPlayService.PlanPlayableForChild(plan, today))
            return this.ProblemWithCode(ApiErrors.PlanInactive, "This study plan is not currently active. Ask your parent.");

        var day = dto.Day ?? today;
        // B-635: an open remediation round of the day is continued, not replaced. A new round would bring a task the child
        // just made easier back as free text - quitting after seeing the solution and starting over would finish it.
        if (pos.Remediation && dto.Mode == PlayMode.Lern && await OpenRemediationRoundAsync(planId, positionId, day, ct) is { } open)
            return Ok(await MapAsync(open, withAttempts: true, ct));
        // Assigned directly (not reloaded via GetSession) so MapAsync() can compute Testable right away - pos
        // already carries Exercise + StudyPlan from GetPosition above.
        var session = new PracticeSession
        {
            StudyPlanId = planId,
            PlanPositionId = positionId,
            Day = day,
            Mode = dto.Mode,
            PlanPosition = pos,
            StartedAt = time.GetUtcNow().UtcDateTime,
        };
        // Freeze the order ONCE (per the position's strategy) so that it does not shift during the run and
        // cursor (learn) and batch (info/offline) use the same stable sequence.
        // Info = free practice: the whole scope-filtered pool (no Leitner due date), so that already learned
        // words can be repeated too. Learn = only the cards due today.
        // A remediation round (B-635) is the list of tasks still open, in order; free practice stays as it is.
        if (pos.Remediation && dto.Mode == PlayMode.Lern)
            session.Order = [.. await play.RemediationRoundAsync(pos, ct)];
        else
        {
            var due = await play.DueItemIndicesAsync(pos, day, pos.OrderStrategy, dueOnly: dto.Mode != PlayMode.Info, ct);
            session.Order = [.. await IntroductionsFirstAsync(pos, day, dto.Mode, due, ct)];
        }
        db.PracticeSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { planId, positionId, sessionId = session.Id }, await MapAsync(session, withAttempts: true, ct));
    }

    // B-635: the latest learn round of this remediation position on the day that still has cards to play.
    private async Task<PracticeSession?> OpenRemediationRoundAsync(int planId, int positionId, DateOnly day,
        CancellationToken ct)
    {
        var candidate = await db.PracticeSessions.AsNoTracking()
            .Where(s => s.PlanPositionId == positionId && s.StudyPlanId == planId && s.Mode == PlayMode.Lern && s.Day == day)
            .OrderByDescending(s => s.Id).FirstOrDefaultAsync(ct);
        return candidate is not null && candidate.Cursor < candidate.Order.Count
            ? await GetSession(planId, positionId, candidate.Id, ct)
            : null;
    }

    /// <summary>
    /// The frozen order with the introductions (B-419) in front, then the self-assessed rungs of the ladder (B-476),
    /// otherwise as the strategy sorted it (stable). Show first, then ask - and neither kind of review is ever a hit,
    /// so one between two asked cards would break the child's combo in a round that mixes them.
    /// </summary>
    private async Task<IReadOnlyList<int>> IntroductionsFirstAsync(PlanPosition pos, DateOnly day, PlayMode mode,
        IReadOnlyList<int> order, CancellationToken ct)
    {
        if (pos.Exercise is null || pos.StudyPlan is null || play.TypeOf(pos.Exercise) is not { } type) return order;
        var dayStage = PositionPlayService.StageForDay(pos, pos.StudyPlan, day, type);
        var introduced = await play.IntroducedCardsAsync(pos.Id, ct);
        var items = await play.ItemsOfAsync(pos, ct: ct);
        return [.. order.OrderBy(i =>
        {
            var card = CardOf(introduced, i);
            if (PositionPlayService.IsIntroduction(type, dayStage, mode, card is not null)) return 0;
            // The order comes from the same item list; an index beyond it is skipped by the cursor anyway.
            if (i >= items.Count) return 2;
            var stage = PositionPlayService.CardStage(type, pos, dayStage, mode, card, day, items[i]);
            return IsSelfClaimRung(type, dayStage, stage) ? 1 : 2;
        })];
    }

    private static CardProgress? CardOf(Dictionary<int, CardProgress> introduced, int index) =>
        introduced.TryGetValue(index, out var card) ? card : null;

    // A card the ladder lowered to self-assessment (B-476): the child only claims to know it, nothing is checked.
    // A learn card (B-598) is neither: it is shown with its solution and never scores.
    private static bool IsSelfClaimRung(IExerciseType type, int dayStage, int stage) =>
        stage != dayStage && !type.IsTypedStage(stage) && !type.IsDisplayOnlyStage(stage) && !type.IsLearnCardStage(stage);

    /// <summary>
    /// B-598: is the card at <paramref name="position"/> of the frozen order a learn card coming back? A learn card given
    /// up or recalled wrong is appended once (see Review), so a second occurrence of an index is exactly that repeat.
    /// </summary>
    private static bool IsRepeat(PracticeSession session, int position) =>
        position >= 0 && position < session.Order.Count && session.Order.IndexOf(session.Order[position]) < position;

    /// <summary>
    /// The stage the card at <paramref name="position"/> of the round is played at: its own (B-419, B-476), or the learn
    /// card again where it is the repeat of one (B-598, decision 7) - the word was not learnt yet, so it is learnt again.
    /// </summary>
    private static int StageAt(IExerciseType type, PlanPosition pos, int dayStage, PracticeSession session,
        Dictionary<int, CardProgress> introduced, IReadOnlyList<ContentItem> items, int position)
    {
        var index = session.Order[position];
        // B-635: a card the child made easier keeps that stage for this place in the round; a remediation task starts at
        // the free-text rung of the ladder whatever the day says. Read from the session, never from the client.
        if (session.EasedStages?.TryGetValue(position, out var eased) == true) return eased;
        if (IsRemediationRound(pos, session, type)) return type.EaseLadder[0];
        if (IsRepeat(session, position) && type.IntroductionStage is { } learn && type.IsLearnCardStage(learn))
            return type.CardCeiling(items[index], learn);
        return PositionPlayService.CardStage(type, pos, dayStage, session.Mode, CardOf(introduced, index), session.Day,
            items[index]);
    }

    // B-635: a learn round on a remediation position of a type that can be made easier.
    private static bool IsRemediationRound(PlanPosition pos, PracticeSession session, IExerciseType type) =>
        pos.Remediation && session.Mode == PlayMode.Lern && type.EaseLadder.Count > 0;

    /// <summary>A practice session of the position.</summary>
    [HttpGet("{sessionId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessionResponse>> Get(int planId, int positionId, int sessionId, CancellationToken ct = default) =>
        await GetSession(planId, positionId, sessionId, ct) is { } s ? await MapAsync(s, withAttempts: true, ct) : NotFound();

    /// <summary>
    /// "The next card is visible now" (B-530): the server stamps the moment with its own clock, so the fast-answer
    /// bonus measures from the visible card rather than from the previous answer - reading the feedback sheet is not
    /// answer time. The client says only <i>when</i>, never <i>how long</i>. One stamp per card: once a stamp is
    /// later than the latest answer, further calls change nothing, and before the first answer of a session there is
    /// nothing to stamp (the first card earns no speed bonus). The 1-second floor still applies to the measurement.
    /// The supervisor does not stamp: a preview pays nothing, and his call must not shape the child's measurement.
    /// The stamp is bound to the card the client names: a report for any card but the session's current one changes
    /// nothing (still 204 - the child has nothing to fix).
    /// </summary>
    [HttpPost("{sessionId:int}/shown")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Shown(int planId, int positionId, int sessionId, CardShownDto dto, CancellationToken ct = default)
    {
        var session = await GetSession(planId, positionId, sessionId, ct);
        if (session is null) return NotFound();
        if (!User.IsStudent() || session.EndedAt is not null || session.Reviews.Count == 0) return NoContent();
        // The client fires this without waiting. Overtaken by the answer to the same card, it would land after that
        // answer and become the base of the NEXT card, measuring its answer time too short.
        if (session.Cursor >= session.Order.Count || session.Order[session.Cursor] != dto.ItemIndex) return NoContent();

        var lastAnswer = session.Reviews.Max(r => r.At);
        if (session.CardShownAt is { } stamped && stamped > lastAnswer) return NoContent();
        session.CardShownAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>
    /// Adds (active) practice seconds (anti time cheat: capped per heartbeat). The answer leaves
    /// <c>TestAttemptsLeft</c> null: the heartbeat fires every few seconds and skips that count.
    /// </summary>
    [HttpPost("{sessionId:int}/heartbeat")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessionResponse>> Heartbeat(int planId, int positionId, int sessionId, HeartbeatDto dto, CancellationToken ct = default)
    {
        var session = await GetSession(planId, positionId, sessionId, ct);
        if (session is null) return NotFound();
        if (dto.Active && dto.Seconds > 0) session.ActiveSeconds += Math.Clamp(dto.Seconds, 0, MaxHeartbeatSeconds);
        await db.SaveChangesAsync(ct);
        return await MapAsync(session, withAttempts: false, ct);
    }

    /// <summary>
    /// Builds a practice card from a content atom. Typed stages withhold the solution
    /// (the server grades, never the frontend); display/self-assessment and the listening stage reveal
    /// it by design, or supply the audio source.
    /// </summary>
    private static PracticeCard BuildCard(PlanPosition pos, IExerciseType type, int dayStage, PracticeSession session,
        Dictionary<int, CardProgress> introduced, IReadOnlyList<ContentItem> items, int position)
    {
        var exercise = pos.Exercise!;
        var index = session.Order[position];
        var card = CardOf(introduced, index);
        var introduction = PositionPlayService.IsIntroduction(type, dayStage, session.Mode, card is not null);
        var stage = StageAt(type, pos, dayStage, session, introduced, items, position);
        var learnCard = type.IsLearnCardStage(stage);
        // B-597: the card in the form its stage asks it - the same form Review grades.
        var staged = PositionPlayService.AtStage(type, items, stage);
        var item = staged[index];
        var f = PositionPlayService.CardFacets(PositionPlayService.ConfigOf(exercise), staged, item, type, stage,
            type.IsTypedStage(stage), session.Id);
        var canEase = IsRemediationRound(pos, session, type)
            && PositionPlayService.EasierStage(type, PositionPlayService.ConfigOf(exercise), staged, item, stage) is not null;
        return new PracticeCard(index, stage, type.Key, f.Prompt,
            f.Hint, f.AnswerLength, f.Reveal, f.Choices, f.AudioUrl, f.ImageUrl, f.ImageAlt, f.GapIndex, f.Passage,
            f.AnyOrder, f.RevealAlternatives, f.Decoding, type.IsDisplayOnlyStage(stage), f.AnswerPattern, f.LetterTiles,
            introduction, f.CueBox,
            // B-252: never on the listening stage - there hearing the word IS the task, and its text stands in only when
            // no recording came.
            type.AudioReplacesPrompt(stage) ? null : item.SpeechLang,
            f.DictationSpokenAnswer, f.DictationLang,
            // B-598: the learn card reads its learnt word aloud while it shows it (decision 10) - the word is the reveal.
            learnCard, learnCard ? item.DictationLang : null, learnCard ? item.DictationAudioUrl : null,
            canEase, session.EasedStages?.ContainsKey(position) == true);
    }

    /// <summary>
    /// The entries already named in a set-graded exercise within the round's period – the progress rows
    /// stamped on that day. There is no session state for this and none is needed: in set mode a review only
    /// ever touches a row on a hit (see <see cref="Review"/>), so a stamp from that day means "this entry was
    /// named".
    ///
    /// Two windows, not one span: the session's day <b>and</b> the current one. Normally they are the same;
    /// they part when a round runs past UTC midnight or when the supervisor backdates one
    /// (<c>StartPracticeDto.Day</c>) – the stamp then carries the wall clock while the round belongs to
    /// another day. Covering both is the safe direction, because a missed stamp does not merely lose a
    /// detail: every remaining card would accept the same answer again. A single span across both days would
    /// be worse than two windows – a round backdated by a week would swallow every entry practised in
    /// between and reject answers that are right.
    ///
    /// Compared as UTC ranges rather than by casting the column to a date: the cast does not translate, and
    /// EF rejects the query instead of grading it – correct, but only at runtime.
    ///
    /// Consequence, deliberate: a <b>second round on the same day</b> has every entry named already, so each
    /// answer is a wrong mention – on the day, nothing counts twice, and a repeat is a wrong mention no matter
    /// which round it arrives in. Reporting it as "not correct" rather than as its own third state is the price;
    /// there are only right and wrong, and a milder practice rule than the exam's would teach a rule that does
    /// not hold in the exam. It stays invisible on a Leitner position (a credited entry is no longer due and
    /// never enters the next round's order) and reachable only through the API otherwise, because the child's
    /// app offers no practice button for a testable position without Leitner.
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
        if (User.IsStudent() && !PositionPlayService.PlanPlayableForChild(plan, DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime)))
            return this.ProblemWithCode(ApiErrors.PlanInactive, "This study plan is not currently active. Ask your parent.");
        var pos = await GetPosition(planId, positionId, ct);
        if (pos?.Exercise is null) return NotFound();

        var items = await play.ItemsOfAsync(pos, pos.StudyPlan?.ChildId, ct);
        if (play.TypeOf(pos.Exercise) is not { } type)
            return this.ProblemWithCode(ApiErrors.UnknownExerciseType, "The exercise has an unknown type.");
        var stage = PositionPlayService.StageForDay(pos, plan, session.Day, type);
        var introduced = await play.IntroducedCardsAsync(positionId, ct);

        // The frozen order; skip items removed since the start (item CRUD). By position: a learn card coming back (B-598)
        // stands twice in the order and is played as the repeat the second time.
        return Enumerable.Range(0, session.Order.Count)
            .Where(p => session.Order[p] >= 0 && session.Order[p] < items.Count)
            .Select(p => BuildCard(pos, type, stage, session, introduced, items, p)).ToList();
    }

    /// <summary>
    /// "Different image" for a card. Deliberately addressed here and not at the child: which carrier
    /// holds the pick – the exercise-local override or the store vocabulary – only follows from the
    /// specificity cascade that only the server knows. The client only has the card in front of it.
    ///
    /// The rejected image never comes up again for this carrier. If there is no alternative, everything
    /// stays unchanged (<c>409 media_no_alternative</c>) instead of leaving the card without an image.
    ///
    /// The endpoint <b>hands out an image</b> and therefore carries the same guards as the playback
    /// itself: only the playable plan, only cards of this session, and only where the card actually shows an
    /// image. Without the last guard it would be the hole in the anti-cheat rule – on a typed
    /// stage it would deliver image <i>and</i> alt text and thereby the meaning of exactly the word that is
    /// supposed to be typed (<see cref="PositionPlayService.CardFacets"/> deliberately withholds both there).
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
            && !PositionPlayService.PlanPlayableForChild(pos.StudyPlan, DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime)))
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
        // An introduction (B-419) is shown at the display stage and therefore does show its image.
        var dayStage = PositionPlayService.StageForDay(pos, pos.StudyPlan, session.Day, type);
        var introduced = await play.IntroducedCardsAsync(positionId, ct);
        // The card the round shows at the cursor - a learn card coming back (B-598) shows its image like the first time.
        var stage = session.Cursor < session.Order.Count && session.Order[session.Cursor] == itemIndex
            ? StageAt(type, pos, dayStage, session, introduced, items, session.Cursor)
            : PositionPlayService.CardStage(type, pos, dayStage, session.Mode, CardOf(introduced, itemIndex), session.Day, item);
        if (PositionPlayService.CardFacets(PositionPlayService.ConfigOf(pos.Exercise), items,
                type.CardForStage(item, stage), type, stage, type.IsTypedStage(stage), seed: 0).ImageUrl is null)
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
        if (User.IsStudent() && !PositionPlayService.PlanPlayableForChild(plan, DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime)))
            return this.ProblemWithCode(ApiErrors.PlanInactive, "This study plan is not currently active. Ask your parent.");
        var pos = await GetPosition(planId, positionId, ct);
        if (pos?.Exercise is null) return NotFound();

        var items = await play.ItemsOfAsync(pos, pos.StudyPlan?.ChildId, ct);
        if (play.TypeOf(pos.Exercise) is not { } type)
            return this.ProblemWithCode(ApiErrors.UnknownExerciseType, "The exercise has an unknown type.");
        var stage = PositionPlayService.StageForDay(pos, plan, session.Day, type);

        var cursor = PositionPlayService.SkipRemoved(session.Order, session.Cursor, items.Count);
        if (cursor != session.Cursor) { session.Cursor = cursor; await db.SaveChangesAsync(ct); }
        if (cursor >= session.Order.Count) return new NextResponse(null, true, cursor, session.Order.Count);

        var introduced = await play.IntroducedCardsAsync(positionId, ct);
        var card = BuildCard(pos, type, stage, session, introduced, items, cursor);
        return new NextResponse(card, false, cursor, session.Order.Count);
    }

    /// <summary>
    /// Makes the round's current card easier (B-635): one rung down its type's ease ladder - free text, letter boxes,
    /// scrambled letters, choice - skipping a rung the card cannot take, and never back up. Free of charge (decision 4);
    /// its only cost is that the task stays open. The stage is kept on the session for this place of the round, so the
    /// answer is graded as what was shown whatever the client sends. Returns the card at its new stage.
    /// </summary>
    /// <remarks>
    /// Only the current card: an index elsewhere in the round would let a client ease a card it has not reached and then
    /// keep the free form for the one it is on. <c>409 cannot_ease</c> when the card is not current, is at its easiest
    /// form already, or the position is no remediation.
    /// </remarks>
    [HttpPost("{sessionId:int}/easier")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PracticeCard>> Easier(int planId, int positionId, int sessionId, EaseCardDto dto,
        CancellationToken ct = default)
    {
        var session = await GetSession(planId, positionId, sessionId, ct);
        if (session is null) return NotFound();
        var plan = (await GetPlan(planId, ct))!;
        if (User.IsStudent() && !PositionPlayService.PlanPlayableForChild(plan, DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime)))
            return this.ProblemWithCode(ApiErrors.PlanInactive, "This study plan is not currently active. Ask your parent.");
        var pos = await GetPosition(planId, positionId, ct);
        if (pos?.Exercise is null) return NotFound();
        if (play.TypeOf(pos.Exercise) is not { } type)
            return this.ProblemWithCode(ApiErrors.UnknownExerciseType, "The exercise has an unknown type.");
        if (!IsRemediationRound(pos, session, type))
            return this.ProblemWithCode(ApiErrors.CannotEase, "Only remediation tasks can be made easier.");
        var position = session.Cursor;
        if (position >= session.Order.Count || session.Order[position] != dto.ItemIndex)
            return this.ProblemWithCode(ApiErrors.CannotEase, "Only the current card of the round can be made easier.");

        var items = await play.ItemsOfAsync(pos, pos.StudyPlan?.ChildId, ct);
        if (dto.ItemIndex < 0 || dto.ItemIndex >= items.Count) return NotFound();
        var dayStage = PositionPlayService.StageForDay(pos, plan, session.Day, type);
        var introduced = await play.IntroducedCardsAsync(positionId, ct);
        var stage = StageAt(type, pos, dayStage, session, introduced, items, position);
        var staged = PositionPlayService.AtStage(type, items, stage);
        if (PositionPlayService.EasierStage(type, PositionPlayService.ConfigOf(pos.Exercise), staged, staged[dto.ItemIndex],
                stage) is not { } easier)
            return this.ProblemWithCode(ApiErrors.CannotEase, "This card is at its easiest form already.");

        session.EasedStages = new Dictionary<int, int>(session.EasedStages ?? []) { [position] = easier };
        await db.SaveChangesAsync(ct);
        return BuildCard(pos, type, dayStage, session, introduced, items, position);
    }

    /// <summary>
    /// Accepts the answer to a practice card, grades it server-side against the item solution
    /// and logs the review. For Leitner positions the atom moves the box up/down,
    /// correct answers earn points (+ combo/speed bonus). Anti-farming: only one
    /// due card is scored and at most once per day; non-typed self-assessment does not count with
    /// <see cref="PlanPosition.RequireTypedTest"/>. A card the vocabulary ladder lowered to self-assessment
    /// (B-476) never pays, but "known" moves its box once a day - also under RequireTypedTest - so it can reach
    /// the checked rungs. The server cursor then advances one card
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
        if (User.IsStudent() && !PositionPlayService.PlanPlayableForChild(plan, DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime)))
            return this.ProblemWithCode(ApiErrors.PlanInactive, "This study plan is not currently active. Ask your parent.");
        var pos = await GetPosition(planId, positionId, ct);
        if (pos?.Exercise is null) return NotFound();

        // Info mode: free practice without any learning feedback - record nothing, score nothing.
        if (session.Mode == PlayMode.Info) return NoContent();

        var items = await play.ItemsOfAsync(pos, pos.StudyPlan?.ChildId, ct);
        if (dto.ItemIndex < 0 || dto.ItemIndex >= play.PoolSize(pos, items.Count))
            return this.ProblemWithCode(ApiErrors.NotFound, "The content does not belong to this position.");
        // Under a daily dose (B-04) only the cards of this round can be reviewed: any other index would introduce a
        // content the day has no room for, and the dose would be a suggestion.
        if (play.DailyDose(pos) > 0 && !session.Order.Contains(dto.ItemIndex))
            return this.ProblemWithCode(ApiErrors.NotFound, "The content is not part of this practice round.");

        // Enforce the stage server-side (not selectable by the client) and grade type-agnostically.
        if (play.TypeOf(pos.Exercise) is not { } type)
            return this.ProblemWithCode(ApiErrors.UnknownExerciseType, "The exercise has an unknown type.");
        var dayStage = PositionPlayService.StageForDay(pos, plan, session.Day, type);
        // The card's own stage, not the day's (B-419, B-476): a card is graded as what it was shown as - an
        // introduction earns nothing, a choice rung is checked as a choice - so the client cannot pick its form.
        var introduced = await play.IntroducedCardsAsync(positionId, ct);
        // The card at the cursor when the answer is for it; otherwise (an offline batch) its first place in the round.
        var position = session.Cursor < session.Order.Count && session.Order[session.Cursor] == dto.ItemIndex
            ? session.Cursor : session.Order.IndexOf(dto.ItemIndex);
        // B-635: a remediation round is played card by card - an answer for a card the round has not reached (the batch
        // path) would let a task be typed while another one's solution is on the screen.
        if (IsRemediationRound(pos, session, type) && position != session.Cursor)
            return this.ProblemWithCode(ApiErrors.NotFound, "The content is not the current card of this remediation round.");
        var stage = position >= 0
            ? StageAt(type, pos, dayStage, session, introduced, items, position)
            : PositionPlayService.CardStage(type, pos, dayStage, session.Mode, CardOf(introduced, dto.ItemIndex),
                session.Day, items[dto.ItemIndex]);
        // B-598: a learn card answered after its word was introduced meanwhile - by its own first answer (sent twice) or by
        // another round in a second tab - is still the learn card this round showed; graded as its rung, a recall made
        // seconds after seeing the solution would count. The learn card's history row written after this round started tells
        // it - strictly after: a round started in the same instant as a previous round's answer (a frozen clock) is a new round.
        if (!type.IsLearnCardStage(stage) && type.IntroductionStage is { } learn && type.IsLearnCardStage(learn)
            && items[dto.ItemIndex].ItemId is { } learntItem
            && await db.ItemReviewEvents.AnyAsync(e => e.ChildId == plan.ChildId && e.PlanPositionId == pos.Id
                && e.ItemId == learntItem && e.StageValue == learn && e.At > session.StartedAt, ct))
            stage = type.CardCeiling(items[dto.ItemIndex], learn);
        // B-597: graded in the form the card was shown in - on a writing stage the learnt word, never the German one.
        var staged = PositionPlayService.AtStage(type, items, stage);
        var item = staged[dto.ItemIndex];
        if (type.IsLearnCardStage(stage))
            return await LearnCardReviewAsync(plan, pos, session, type, dayStage, introduced, items, staged, item, dto,
                atCursor: position == session.Cursor, ct);
        var typed = type.IsTypedStage(stage);
        var displayOnly = type.IsDisplayOnlyStage(stage);
        var selfClaimRung = IsSelfClaimRung(type, dayStage, stage);
        // B-635: a card the child made easier is answered with help - it shows right or wrong and the solution, and it
        // neither pays nor finishes the task.
        var eased = position >= 0 && session.EasedStages?.ContainsKey(position) == true;

        // A set-graded exercise (an unordered list) is not answered card by card: any entry not yet named today
        // counts, and the ANSWER decides which entry it credits - not the card it arrived on. A miss credits
        // nothing, because there is no atom to attribute it to: naming one of a dozen open entries as "the"
        // solution would be arbitrary, and demoting it would punish an entry the child never claimed.
        var setMode = typed && type.GradesAsSet(PositionPlayService.ConfigOf(pos.Exercise));
        int? creditedIndex = dto.ItemIndex;
        if (setMode)
        {
            var named = await NamedInRoundAsync(positionId, session.Day, ct);
            creditedIndex = PositionPlayService.MatchOpenEntry(staged,
                session.Order.Where(i => !named.Contains(i)), dto.GivenAnswer, grader);
        }

        var wasCorrect = setMode ? creditedIndex is not null
            : typed ? grader.Matches(dto.GivenAnswer, item)
            : dto.WasKnown ?? false;
        // B-422: a wrong typed answer that is the right word misspelled is named, not forgiven - the review stays wrong,
        // as in the test (B-524). Set mode has no asked atom to measure against; the siblings keep a neighbouring word
        // from passing as a slip. Only on a miss: the distance runs over every atom of the exercise.
        var misspelled = typed && !setMode && !wasCorrect && grader.IsMisspelled(dto.GivenAnswer, item, staged);

        // Outside set mode the credited atom is always the card's own, so `prog` is never null there.
        var prog = creditedIndex is { } credited ? await play.ProgressForAsync(positionId, credited, ct) : null;
        // B-433: a review with no answer at all on an atom introduced today and never checked is no verdict. The card
        // the child was served was an introduction that asked for nothing; the atom was introduced elsewhere since
        // (a second tab), so the server now sees an asked card. Judging the empty "next" as wrong would be unfair and
        // invisible to the child. Narrow on purpose: a word introduced today sits in box 1 anyway, so nothing is lost by
        // not demoting it. Read before the first-contact stamp below, which would make every new atom look like this.
        var noVerdict = !setMode && !displayOnly && dto.WasKnown is null && string.IsNullOrEmpty(dto.GivenAnswer)
            && PositionPlayService.IntroductionStage(type) is not null
            && prog is { ReviewCount: 0 } && prog.IntroducedAt == session.Day;
        // First contact counts as the introduction - otherwise IntroducedAt/DueOn would stand still for purely
        // practice-based learning (due dates, the "new/old" scope). Only the child's own encounter introduces the
        // position's atom (B-433): what the supervisor sees in a preview or catch-up round the child has not seen -
        // the same wall as in the test (PositionTestsController, daily dose and IntroduceAnsweredAsync). The wall
        // covers PositionItemProgress only: a graded supervisor review still writes the child's plan-wide
        // ItemProgress/history - by decision the catch-up the supervisor records for the child (B-542; trying an
        // exercise without side effects is the preview) - and a Leitner review without an introduction stage moves
        // box and DueOn while IntroducedAt stays null, so the daily dose keeps counting the atom as new.
        if (User.IsStudent() && prog is not null && prog.IntroducedAt is null)
        {
            prog.IntroducedAt = session.Day;
            prog.DueOn ??= session.Day;
        }

        var due = prog is null || prog.DueOn is null || prog.DueOn <= session.Day;
        var alreadyScoredToday = prog?.LastReviewedAt is { } last && DateOnly.FromDateTime(last) == session.Day;
        // A free display stage (B-96) counts as practiced (session/heartbeat/missions still see it) but never
        // as a graded hit: no points, no Leitner movement - "Kennenlernen" must not be the cheapest way to coins.
        // A self-assessed rung of the ladder (B-476) is a claim, not a check: it pays nothing either. It still moves
        // the box (below), or the card would never reach the rungs where it is checked.
        var scored = prog is not null && (typed || !pos.RequireTypedTest) && due && !alreadyScoredToday && !displayOnly
            && !selfClaimRung && !noVerdict && !eased;
        var climbs = selfClaimRung && pos.UseLeitner && prog is not null && due && !alreadyScoredToday && !noVerdict;

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
        // B-530: from the moment this card became visible, if the client reported it after the previous answer;
        // without such a stamp the old rule (since the previous answer) holds - never more generous.
        var measuredFrom = lastAt is { } la && session.CardShownAt is { } shownAt && shownAt > la ? shownAt : lastAt;
        double? elapsedSeconds = measuredFrom is { } from ? (now - from).TotalSeconds : null;

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
        // A display-only stage (B-96) never carries a verdict at all - `wasCorrect` here would just be the
        // ungated `dto.WasKnown` claim, and the history must not show a "correctly answered" Kennenlernen card
        // when there was no judgment to make.
        // A review without a verdict (B-433) leaves no history entry: the introduction it answers was recorded already.
        if (creditedIndex is { } recorded && !noVerdict)
        {
            await itemProgress.RecordAsync(plan.ChildId, pos.ExerciseId, staged[recorded], wasCorrect && !displayOnly,
                stage, typed ? dto.GivenAnswer : null, ItemReviewSource.Practice, positionId, session.Day, countsForMastery: scored, ct: ct);
        }

        // Points/box only on Leitner positions and only for graded cards (anti-farming). Otherwise 0.
        int awarded = 0, comboBonus = 0, speedBonus = 0, combo = 0;
        var leitnerScored = pos.UseLeitner && scored;
        // `scored` already implies a credited atom; the null check is the compiler's proof, not a second rule.
        if (leitnerScored && prog is not null)
        {
            combo = wasCorrect ? prevStreak + 1 : 0;
            var preBox = prog.Box;
            // B-538: a position without a duty pays no coins at all - otherwise it were a second, uncapped way to coins
            // next to the bonus training, open before the duty. The box and the learning state move as before.
            var paysCoins = pos.Cadence != GoalCadence.None;
            // B-535: the first graded hit pays the new-content points, once - a wrong first try leaves them for later. A
            // hit that pays nothing leaves them too: a later duty on the same position still pays them.
            var isNew = paysCoins && wasCorrect && prog.NewBonusPaidAt is null;
            if (isNew) prog.NewBonusPaidAt = now;
            // The shared clock, not the wall one: ApplyReview stamps LastReviewedAt, which in set mode is the
            // "already named" marker read back above. A second reading would put the stamp outside the window
            // a frozen test clock defines, and then every remaining card would accept the same answer again.
            play.ApplyReview(pos, prog, wasCorrect, verified: typed, session.Day, now);

            var cfg = new ScoringService.ScoreConfig($"{plan.Title} · {pos.Exercise.Title}", pos.NewContentPoints,
                pos.ComboThreshold, pos.ComboBonusPoints, pos.SpeedThresholdSeconds, pos.SpeedBonusPoints,
                pos.TimeSlots);
            var score = scoring.ScoreReview(cfg, isNew, preBox, prog.Box, wasCorrect, combo,
                time.GetLocalNow().DateTime, elapsedSeconds);
            if (paysCoins)
            {
                foreach (var c in score.Contributions)
                    db.ChildPointsEntries.Add(new ChildPointsEntry { ChildId = plan.ChildId, Kind = c.Kind, Amount = c.Amount, Reason = c.Reason, CreatedAt = now });
                awarded = score.BasePoints;
                comboBonus = score.ComboBonus;
                speedBonus = score.SpeedBonus;
            }

            if (paysCoins && score.Total > 0)
                // The CREDITED entry, not the card it arrived on: in set mode those differ, and the card index is
                // the one value the booking has nothing to do with.
                logger.LogInformation(
                    "Positions-Wiederholung gewertet: Kind {ChildId} Plan {PlanId} Position {PositionId} Item {ItemIndex} " +
                    "→ +{Total} Punkte (Basis {Base}, Combo ×{Combo} +{ComboBonus}, Speed +{SpeedBonus})",
                    plan.ChildId, planId, positionId, prog.ItemIndex, score.Total, score.BasePoints, combo,
                    score.ComboBonus, score.SpeedBonus);
        }
        else if (climbs && prog is not null)
        {
            // The claim moves the box like a review, but it is not one for the score: ReviewCount counts graded reviews
            // only. The new-content points hang on NewBonusPaidAt (B-535), which a claim never sets.
            var reviewCount = prog.ReviewCount;
            play.ApplyReview(pos, prog, wasCorrect, verified: false, session.Day, now);
            prog.ReviewCount = reviewCount;
        }

        // B-635: a remediation task counts its solutions per stage, and it is done only by a free answer that was right -
        // the stage comes from the session (`eased`), never from what the client sent. Only the child's own answer counts,
        // as only its own encounter introduces (B-433).
        var taskDone = false;
        if (IsRemediationRound(pos, session, type) && User.IsStudent() && prog is not null && wasCorrect)
        {
            prog.SolvedByStage = new Dictionary<int, int>(prog.SolvedByStage ?? [])
            {
                [stage] = (prog.SolvedByStage?.GetValueOrDefault(stage) ?? 0) + 1,
            };
            if (!eased && prog.RemediationDoneAt is null)
            {
                prog.RemediationDoneAt = session.Day;
                taskDone = true;
            }
        }

        // Server cursor: onto the card just answered (a valid one), one further, then skip removed ones.
        var cursor = PositionPlayService.SkipRemoved(session.Order, session.Cursor, items.Count);
        if (cursor < session.Order.Count) cursor++;
        session.Cursor = PositionPlayService.SkipRemoved(session.Order, cursor, items.Count);

        await db.SaveChangesAsync(ct);

        if (leitnerScored)
            await gamification.EvaluateAndAwardAsync(plan.ChildId, session.Day, ct);

        // The atom just reviewed is introduced now; the set was read before, and the next card may be that atom when a
        // client answered a card other than the cursor's (offline batch).
        if (prog?.IntroducedAt is { } introducedAt) introduced[prog.ItemIndex] = new CardProgress(prog.Box, introducedAt);
        var done = session.Cursor >= session.Order.Count;
        var next = done ? null : BuildCard(pos, type, dayStage, session, introduced, items, session.Cursor);
        // Without a credited atom there is no solution to name and no box that moved (set mode, wrong answer):
        // the feedback stays empty instead of pointing at the card's own entry, which would both be arbitrary
        // and give away an entry that is still to be asked for.
        return new ReviewOutcome(wasCorrect, creditedIndex is { } shown ? staged[shown].Answer : null,
            awarded, prog?.Box ?? 0, prog?.DueOn, combo, comboBonus, speedBonus, next, done, misspelled,
            creditedIndex is { } explained ? staged[explained].Explanation : null, TaskDone: taskDone);
    }

    /// <summary>
    /// The answer to a learn card (B-598): both results of the card in one call - <see cref="ReviewDto.WasKnown"/> from
    /// uncovering it, <see cref="ReviewDto.GivenAnswer"/> from recalling it (<c>null</c> = given up). Neither scores nor
    /// moves a box (decision 2: the solution lay on the device); the word counts as introduced, and both results go into
    /// the learning history as two rows that count for no mastery - a recall given up keeps no answer, a wrong one keeps
    /// what was written (decisions 6, 8). Given up or wrong, the card comes back once at the end of the round (decision 7):
    /// a second occurrence in the order is that repeat, and a repeat is never appended again.
    /// </summary>
    /// <remarks>
    /// Only the card at the cursor is appended: an answer for an index outside the round, or a learn card answered again
    /// after the round moved on, would otherwise grow the round with a card it never served.
    /// </remarks>
    private async Task<ActionResult<ReviewOutcome>> LearnCardReviewAsync(StudyPlan plan, PlanPosition pos,
        PracticeSession session, IExerciseType type, int dayStage, Dictionary<int, CardProgress> introduced,
        IReadOnlyList<ContentItem> items, IReadOnlyList<ContentItem> staged, ContentItem item, ReviewDto dto,
        bool atCursor, CancellationToken ct)
    {
        var index = dto.ItemIndex;
        var recalled = !string.IsNullOrWhiteSpace(dto.GivenAnswer) && grader.Matches(dto.GivenAnswer, item);
        var prog = await play.ProgressForAsync(pos.Id, index, ct);
        // Only the child's own encounter introduces (B-433), as in every other review.
        if (User.IsStudent() && prog.IntroducedAt is null)
        {
            prog.IntroducedAt = session.Day;
            prog.DueOn ??= session.Day;
        }

        var now = time.GetUtcNow().UtcDateTime;
        // A review without a hit: the round counts as played, and the combo does not grow on a card that pays nothing.
        db.ReviewEvents.Add(new ReviewEvent { PracticeSessionId = session.Id, WasCorrect = false, At = now });
        // The card always sends whether the word was known; an answer carrying neither result is no verdict (B-433) - it
        // introduces the word and leaves neither a history row nor a repeat.
        var verdict = dto.WasKnown is not null || !string.IsNullOrWhiteSpace(dto.GivenAnswer);
        if (verdict)
        {
            await itemProgress.RecordAsync(plan.ChildId, pos.ExerciseId, item, dto.WasKnown ?? false,
                (int)TestStage.SelfAssess, null, ItemReviewSource.Practice, pos.Id, session.Day, countsForMastery: false,
                ct: ct);
            await itemProgress.RecordAsync(plan.ChildId, pos.ExerciseId, item, recalled, (int)TestStage.LearnCard,
                string.IsNullOrWhiteSpace(dto.GivenAnswer) ? null : dto.GivenAnswer, ItemReviewSource.Practice, pos.Id,
                session.Day, countsForMastery: false, ct: ct);
        }

        var repeats = verdict && !recalled && atCursor && session.Order.Count(i => i == index) < 2;
        if (repeats) session.Order = [.. session.Order, index];

        var cursor = PositionPlayService.SkipRemoved(session.Order, session.Cursor, items.Count);
        if (cursor < session.Order.Count) cursor++;
        session.Cursor = PositionPlayService.SkipRemoved(session.Order, cursor, items.Count);
        await db.SaveChangesAsync(ct);

        if (prog.IntroducedAt is { } introducedAt) introduced[index] = new CardProgress(prog.Box, introducedAt);
        var done = session.Cursor >= session.Order.Count;
        var next = done ? null : BuildCard(pos, type, dayStage, session, introduced, items, session.Cursor);
        var misspelled = !recalled && !string.IsNullOrWhiteSpace(dto.GivenAnswer)
            && grader.IsMisspelled(dto.GivenAnswer, item, staged);
        return new ReviewOutcome(recalled, item.Answer, 0, prog.Box, prog.DueOn, 0, 0, 0, next, done, misspelled,
            item.Explanation, repeats);
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
        var payable = !User.IsStudent() || PositionPlayService.PlanPlayableForChild(plan, DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime));

        session.EndedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);
        if (!payable) return await MapAsync(session, withAttempts: true, ct);

        // The position's goal points (idempotent): this mainly covers pure content/reading exercises whose goal
        // is met by a round played far enough. BEFORE the gamification, so that missions see the credit.
        await progress.EvaluateAndAwardAsync(plan, session.Day, ct);
        await gamification.EvaluateAndAwardAsync(plan.ChildId, session.Day, ct);
        // The daily reward box: same occasion as the goal reward above, once the daily goals of all the child's active
        // plans are met (B-535, B-606) - not just those of the plan played here.
        await dailyBox.EvaluateAndAwardAsync(plan.ChildId, session.Day, ct);
        return await MapAsync(session, withAttempts: true, ct);
    }
}
