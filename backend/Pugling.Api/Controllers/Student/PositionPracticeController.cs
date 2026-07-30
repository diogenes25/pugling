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

    // Kein Vorgabewert für `ct`: er ließe die Aufrufstelle korrekt aussehen, während der Abbruch des
    // Clients verpufft – ein weggelassenes optionales Argument sieht weder CA2016 noch der Wächter.
    private Task<StudyPlan?> GetPlan(int planId, CancellationToken ct) =>
        db.StudyPlans.FirstOrDefaultAsync(p => p.Id == planId, ct);

    // Der Plan kommt mit, weil die Bebilderung das Kind braucht (die Auswahl hängt an seinem Profil).
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
            return Forbid(); // Nachtragen anderer Tage nur für den Vater (Anti-Schummel).
        // Anti-Schummel: der Sohn darf nur seinen aktiven, laufenden Plan spielen – kein Cherry-Picking
        // leichter oder abgelaufener Pläne für bequeme Punkte. Der Vater darf jederzeit (Vorschau/Nachtrag).
        if (User.IsStudent() && await GetPlan(planId, ct) is { } plan && !PositionPlayService.PlanPlayableForChild(plan, today))
            return this.ProblemWithCode(ApiErrors.PlanInactive, "This study plan is not currently active. Ask your parent.");

        var day = dto.Day ?? today;
        var session = new PracticeSession { StudyPlanId = planId, PlanPositionId = positionId, Day = day, Mode = dto.Mode };
        // Reihenfolge EINMAL einfrieren (gemäß Strategie der Position), damit sie sich im Lauf nicht
        // verschiebt und Cursor (Lern) bzw. Batch (Info/Offline) dieselbe stabile Sequenz nutzen.
        // Info = freies Üben: der ganze scope-gefilterte Pool (ohne Leitner-Fälligkeit), damit auch bereits
        // gelernte Vokabeln wiederholbar sind. Lern = nur die heute fälligen Karten.
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
    private static PracticeCard BuildCard(IExerciseType type, int stage, bool typed,
        IReadOnlyList<ContentItem> items, int index)
    {
        var item = items[index];
        var f = PositionPlayService.CardFacets(items, item, type, stage, typed);
        return new PracticeCard(index, stage, type.Key, item.Prompt,
            f.Hint, f.AnswerLength, f.Reveal, f.Choices, f.AudioUrl, f.ImageUrl, f.ImageAlt);
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
        // Anti-Schummel: auch mit einer noch offenen Session darf der Sohn einen inzwischen deaktivierten
        // oder abgelaufenen Plan nicht weiter beüben (der Vater bleibt für Vorschau/Nachtrag ausgenommen).
        if (User.IsStudent() && !PositionPlayService.PlanPlayableForChild(plan, DateOnly.FromDateTime(DateTime.UtcNow)))
            return this.ProblemWithCode(ApiErrors.PlanInactive, "This study plan is not currently active. Ask your parent.");
        var pos = await GetPosition(planId, positionId, ct);
        if (pos?.Exercise is null) return NotFound();

        var items = await play.ItemsOfAsync(pos, pos.StudyPlan?.ChildId, ct);
        if (play.TypeOf(pos.Exercise) is not { } type)
            return this.ProblemWithCode(ApiErrors.UnknownExerciseType, "The exercise has an unknown type.");
        var stage = PositionPlayService.StageForDay(pos, plan, session.Day, type);
        var typed = type.IsTypedStage(stage);

        // Eingefrorene Reihenfolge; seit dem Start entfernte Items (Item-CRUD) überspringen.
        return session.Order.Where(i => i >= 0 && i < items.Count)
            .Select(i => BuildCard(type, stage, typed, items, i)).ToList();
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

        // Nur Karten dieser Sitzung: die beim Start eingefrorene Reihenfolge ist genau die Menge, die
        // ausgeliefert wird (und liegt im Pool der Position). Ein freier Index könnte sonst die Motive der
        // ganzen Übung durchzählen – auch die der Karten, die diese Sitzung nie zeigt.
        if (!session.Order.Contains(itemIndex)) return NotFound();

        var items = await play.ItemsOfAsync(pos, pos.StudyPlan.ChildId, ct);
        if (items.FirstOrDefault(i => i.Index == itemIndex) is not
            { ItemId: { } itemId, VocabularyId: { } vocabId } item)
            return NotFound();
        if (play.TypeOf(pos.Exercise) is not { } type)
            return this.ProblemWithCode(ApiErrors.UnknownExerciseType, "The exercise has an unknown type.");

        // Umgewählt wird nur das Bild, das die Karte auch zeigt – die Stufe kommt dabei wie überall aus
        // dem Fahrplan, nie vom Client.
        var stage = PositionPlayService.StageForDay(pos, pos.StudyPlan, session.Day, type);
        if (PositionPlayService.CardFacets(items, item, type, stage, type.IsTypedStage(stage)).ImageUrl is null)
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

        var card = BuildCard(type, stage, typed, items, session.Order[cursor]);
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
        // Anti-Schummel: auch mit einer noch offenen Session darf der Sohn einen inzwischen deaktivierten
        // oder abgelaufenen Plan nicht weiter beüben (der Vater bleibt für Vorschau/Nachtrag ausgenommen).
        if (User.IsStudent() && !PositionPlayService.PlanPlayableForChild(plan, DateOnly.FromDateTime(DateTime.UtcNow)))
            return this.ProblemWithCode(ApiErrors.PlanInactive, "This study plan is not currently active. Ask your parent.");
        var pos = await GetPosition(planId, positionId, ct);
        if (pos?.Exercise is null) return NotFound();

        // Info-Modus: freies Üben ohne jegliches Lernfeedback – nichts protokollieren, nichts bepunkten.
        if (session.Mode == PlayMode.Info) return NoContent();

        var items = await play.ItemsOfAsync(pos, pos.StudyPlan?.ChildId, ct);
        if (dto.ItemIndex < 0 || dto.ItemIndex >= play.PoolSize(pos, items.Count))
            return this.ProblemWithCode(ApiErrors.NotFound, "The content does not belong to this position.");
        var item = items[dto.ItemIndex];

        // Stufe serverseitig erzwingen (nicht vom Client wählbar) und typ-neutral bewerten.
        if (play.TypeOf(pos.Exercise) is not { } type)
            return this.ProblemWithCode(ApiErrors.UnknownExerciseType, "The exercise has an unknown type.");
        var stage = PositionPlayService.StageForDay(pos, plan, session.Day, type);
        var typed = type.IsTypedStage(stage);
        var wasCorrect = typed
            ? item.AcceptedAnswers.Any(a => grader.Matches(dto.GivenAnswer, a))
            : dto.WasKnown ?? false;

        var prog = await play.ProgressForAsync(positionId, dto.ItemIndex, ct);
        // Erstkontakt zählt als Einführung – sonst stünde IntroducedAt/DueOn bei rein übungsbasiertem
        // Lernen still (Fälligkeit, Scope „neu/alt").
        if (prog.IntroducedAt is null)
        {
            prog.IntroducedAt = session.Day;
            prog.DueOn ??= session.Day;
        }

        var due = prog.DueOn is null || prog.DueOn <= session.Day;
        var alreadyScoredToday = prog.LastReviewedAt is { } last && DateOnly.FromDateTime(last) == session.Day;
        var scored = (typed || !pos.RequireTypedTest) && due && !alreadyScoredToday;

        // Combo/Antwortzeit VOR dem Hinzufügen des neuen Reviews (EF-Fixup würde es sonst mitzählen).
        var prevStreak = 0;
        foreach (var r in session.Reviews.OrderByDescending(r => r.At).ThenByDescending(r => r.Id))
        {
            if (r.WasCorrect) prevStreak++; else break;
        }
        // Antwortzeit und Zeitstempel kommen aus DERSELBEN Uhr (<see cref="TimeProvider"/>): der Abstand
        // zwischen zwei Antworten ist die Grundlage des Schnelle-Antwort-Bonus samt seiner
        // Anti-Farming-Untergrenze von einer Sekunde. Eine Regel im Sekunden-Bereich lässt sich mit der
        // Wanduhr nicht prüfen – ein Test müsste zwei Requests binnen einer Sekunde durchbringen und wird
        // unter Last zum Flake, der wie ein Punkte-Regress aussieht (docs/testplan.md, Etappe 3). Mit der
        // gemeinsamen, ersetzbaren Uhr ist die gemessene Zeit eine Eingabe statt einer Hoffnung.
        var lastAt = session.Reviews.Count > 0 ? session.Reviews.Max(r => r.At) : (DateTime?)null;
        var now = time.GetUtcNow().UtcDateTime;
        double? elapsedSeconds = lastAt is { } la ? (now - la).TotalSeconds : null;

        // Nur Korrektheit und Zeitpunkt: daraus entstehen Combo-Serie, Antwortzeit und die Metrik
        // CorrectReviews. Welches Atom es war, protokolliert der ItemReviewEvent unten über die stabile
        // ItemId – hier wäre es eine index-adressierte Zweitwahrheit ohne Leser.
        db.ReviewEvents.Add(new ReviewEvent
        {
            PracticeSessionId = sessionId,
            WasCorrect = wasCorrect && scored,
            At = now,
        });

        // Plan-übergreifenden Item-Fortschritt + Antwort-Historie mitschreiben (nur Vokabel-Items mit stabiler ItemId).
        // Bewusst mit der tatsächlichen Korrektheit (nicht anti-farming-gedämpft) – dies ist eine Auswertungsebene,
        // nicht die Punktequelle; persistiert wird über das SaveChanges unten.
        await itemProgress.RecordAsync(plan.ChildId, pos.ExerciseId, item, wasCorrect, stage,
            typed ? dto.GivenAnswer : null, ItemReviewSource.Practice, positionId, session.Day, countsForMastery: scored, ct: ct);

        // Punkte/Box nur bei Leitner-Positionen und nur für gewertete Karten (Anti-Farming). Sonst 0.
        int awarded = 0, comboBonus = 0, speedBonus = 0, combo = 0;
        var leitnerScored = pos.UseLeitner && scored;
        if (leitnerScored)
        {
            combo = wasCorrect ? prevStreak + 1 : 0;
            var (preBox, preReviewCount) = (prog.Box, prog.ReviewCount);
            play.ApplyReview(pos, prog, wasCorrect, session.Day, DateTime.UtcNow);

            var cfg = new ScoringService.ScoreConfig($"{plan.Title} · {pos.Exercise.Title}", pos.NewContentPoints,
                pos.ComboThreshold, pos.ComboBonusPoints, pos.SpeedThresholdSeconds, pos.SpeedBonusPoints);
            var score = await scoring.ScoreReviewAsync(cfg, preReviewCount, preBox, prog.Box, wasCorrect, combo,
                DateTime.Now, elapsedSeconds, ct);
            foreach (var c in score.Contributions)
                db.ChildPoints.Add(new ChildPointsEntry { ChildId = plan.ChildId, Kind = c.Kind, Amount = c.Amount, Reason = c.Reason });
            awarded = score.BasePoints;
            comboBonus = score.ComboBonus;
            speedBonus = score.SpeedBonus;

            if (score.Total > 0)
                logger.LogInformation(
                    "Positions-Wiederholung gewertet: Kind {ChildId} Plan {PlanId} Position {PositionId} Item {ItemIndex} " +
                    "→ +{Total} Punkte (Basis {Base}, Combo ×{Combo} +{ComboBonus}, Speed +{SpeedBonus})",
                    plan.ChildId, planId, positionId, dto.ItemIndex, score.Total, score.BasePoints, combo,
                    score.ComboBonus, score.SpeedBonus);
        }

        // Server-Cursor: auf die gerade beantwortete (gültige) Karte, eins weiter, dann entfernte überspringen.
        var cursor = PositionPlayService.SkipRemoved(session.Order, session.Cursor, items.Count);
        if (cursor < session.Order.Count) cursor++;
        session.Cursor = PositionPlayService.SkipRemoved(session.Order, cursor, items.Count);

        await db.SaveChangesAsync(ct);

        if (leitnerScored)
            await gamification.EvaluateAndAwardAsync(plan.ChildId, session.Day, ct);

        var done = session.Cursor >= session.Order.Count;
        var next = done ? null : BuildCard(type, stage, typed, items, session.Order[session.Cursor]);
        return new ReviewOutcome(wasCorrect, item.Answer, awarded, prog.Box, prog.DueOn, combo,
            comboBonus, speedBonus, next, done);
    }

    /// <summary>Ends the session and evaluates time-based missions.</summary>
    [HttpPost("{sessionId:int}/end")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessionResponse>> End(int planId, int positionId, int sessionId, CancellationToken ct = default)
    {
        var session = await GetSession(planId, positionId, sessionId, ct);
        if (session is null) return NotFound();
        session.EndedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var plan = (await GetPlan(planId, ct))!;
        // Ziel-Punkte der Position (idempotent): erfasst v. a. reine Inhalts-/Leseübungen, deren Ziel mit
        // dem Beenden der Sitzung erfüllt ist. VOR der Gamification, damit Missionen die Gutschrift sehen.
        await progress.EvaluateAndAwardAsync(plan, session.Day, ct);
        await gamification.EvaluateAndAwardAsync(plan.ChildId, session.Day, ct);
        return Map(session);
    }
}
