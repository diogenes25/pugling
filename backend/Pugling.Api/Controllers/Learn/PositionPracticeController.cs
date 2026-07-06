using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;
using Pugling.Api.Services;

namespace Pugling.Api.Controllers.Learn;

/// <summary>
/// Üben einer einzelnen Lehrplan-Position (neues Modell): der Sohn spielt die Inhalte EINER Übung,
/// der Fortschritt läuft pro Inhalts-Atom über <see cref="PositionItemProgress"/>. Inhalt kommt aus der
/// Übungs-Config (<see cref="ExerciseContentProvider"/>), bewertet wird typ-neutral gegen die Item-Lösung.
/// Ersetzt die frühere plan-weite Übungssitzung.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.V1 + "/study-plans/{planId:int}/positions/{positionId:int}/practice-sessions")]
[Tags("Study – Position Practice")]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(PlanOwnershipFilter))]
public class PositionPracticeController(PuglingDbContext db, PositionPlayService play, ScoringService scoring,
    PositionProgressService progress, GamificationService gamification, AnswerGrader grader,
    ILogger<PositionPracticeController> logger)
    : ControllerBase
{
    /// <summary>Obergrenze der pro Heartbeat anrechenbaren Sekunden (Anti-Zeit-Cheat).</summary>
    private const int MaxHeartbeatSeconds = 120;

    public record SessionResponse(int Id, int PlanId, int PositionId, DateOnly Day,
        DateTime StartedAt, DateTime? EndedAt, int ActiveSeconds, int ReviewCount);

    private static SessionResponse Map(PracticeSession s) =>
        new(s.Id, s.StudyPlanId, s.PlanPositionId ?? 0, s.Day, s.StartedAt, s.EndedAt, s.ActiveSeconds, s.Reviews.Count);

    private Task<StudyPlan?> GetPlan(int planId) => db.StudyPlans.FirstOrDefaultAsync(p => p.Id == planId);

    private Task<PlanPosition?> GetPosition(int planId, int positionId) =>
        db.PlanPositions.Include(p => p.Exercise)
            .FirstOrDefaultAsync(p => p.Id == positionId && p.StudyPlanId == planId);

    private Task<PracticeSession?> GetSession(int planId, int positionId, int sessionId) =>
        db.PracticeSessions.Include(s => s.Reviews)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.StudyPlanId == planId && s.PlanPositionId == positionId);

    public record StartDto(DateOnly? Day);

    /// <summary>Startet eine Übungssitzung für die Position. Day nur zum Nachtragen (Vater); sonst heute.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessionResponse>> Start(int planId, int positionId, StartDto dto)
    {
        if (await GetPosition(planId, positionId) is null) return NotFound();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (dto.Day is { } d && d != today && !User.IsFather())
            return Forbid(); // Nachtragen anderer Tage nur für den Vater (Anti-Schummel).
        // Anti-Schummel: der Sohn darf nur seinen aktiven, laufenden Plan spielen – kein Cherry-Picking
        // leichter oder abgelaufener Pläne für bequeme Punkte. Der Vater darf jederzeit (Vorschau/Nachtrag).
        if (User.IsChild() && await GetPlan(planId) is { } plan && !PositionPlayService.PlanPlayableForChild(plan, today))
            return this.ProblemWithCode(ApiErrors.PlanInactive, "This study plan is not currently active. Ask your parent.");

        var session = new PracticeSession { StudyPlanId = planId, PlanPositionId = positionId, Day = dto.Day ?? today };
        db.PracticeSessions.Add(session);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { planId, positionId, sessionId = session.Id }, Map(session));
    }

    /// <summary>Eine Übungssitzung der Position.</summary>
    [HttpGet("{sessionId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessionResponse>> Get(int planId, int positionId, int sessionId) =>
        await GetSession(planId, positionId, sessionId) is { } s ? Map(s) : NotFound();

    public record HeartbeatDto(int Seconds, bool Active);

    /// <summary>Fügt (aktive) Übungssekunden hinzu (Anti-Zeit-Cheat: pro Heartbeat gedeckelt).</summary>
    [HttpPost("{sessionId:int}/heartbeat")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessionResponse>> Heartbeat(int planId, int positionId, int sessionId, HeartbeatDto dto)
    {
        var session = await GetSession(planId, positionId, sessionId);
        if (session is null) return NotFound();
        if (dto.Active && dto.Seconds > 0) session.ActiveSeconds += Math.Clamp(dto.Seconds, 0, MaxHeartbeatSeconds);
        await db.SaveChangesAsync();
        return Map(session);
    }

    /// <summary>
    /// Eine Übungskarte – bewusst OHNE Lösung, außer bei Anzeige-/Selbsteinschätzungs-Stufen, die die
    /// Lösung per Design aufdecken (der Server bewertet in <see cref="Review"/>, nie das Frontend).
    /// </summary>
    public record PracticeCard(int ItemIndex, int Stage, string Type, string Prompt,
        string? Hint, int? AnswerLength, string? Reveal, IReadOnlyList<string>? Choices, string? AudioUrl);

    /// <summary>Liefert die heute fälligen Übungskarten der Position (Scope/ItemCount/Leitner-Fälligkeit).</summary>
    [HttpGet("{sessionId:int}/cards")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PracticeCard>>> Cards(int planId, int positionId, int sessionId)
    {
        var session = await GetSession(planId, positionId, sessionId);
        if (session is null) return NotFound();
        var plan = (await GetPlan(planId))!;
        // Anti-Schummel: auch mit einer noch offenen Session darf der Sohn einen inzwischen deaktivierten
        // oder abgelaufenen Plan nicht weiter beüben (der Vater bleibt für Vorschau/Nachtrag ausgenommen).
        if (User.IsChild() && !PositionPlayService.PlanPlayableForChild(plan, DateOnly.FromDateTime(DateTime.UtcNow)))
            return this.ProblemWithCode(ApiErrors.PlanInactive, "This study plan is not currently active. Ask your parent.");
        var pos = await GetPosition(planId, positionId);
        if (pos?.Exercise is null) return NotFound();

        var items = await play.ItemsOfAsync(pos);
        var stage = PositionPlayService.StageForDay(pos, plan, session.Day);
        var typed = PositionPlayService.IsTypedStage(pos.Exercise.Type, stage);
        var dueIndices = await play.DueItemIndicesAsync(pos, session.Day);

        var cards = dueIndices.Select(i =>
        {
            var item = items[i];
            // Getippte Stufen halten die Lösung zurück; Anzeige-/Selbsteinschätzung deckt sie bewusst auf.
            var isLetterBoxes = pos.Exercise.Type == ExerciseType.Vocabulary && (TestStage)stage == TestStage.LetterBoxes;
            // Hör-Stufe: Audioquelle mitgeben, damit der Client die Vokabel vorliest und den Wort-Text ausblendet.
            var isAudio = pos.Exercise.Type == ExerciseType.Vocabulary && (TestStage)stage == TestStage.Audio;
            return new PracticeCard(i, stage, pos.Exercise.Type.ToString(), item.Prompt,
                typed ? item.Hint : null,
                isLetterBoxes ? item.Answer.Length : null,
                typed ? null : item.Answer,
                PositionPlayService.ChoicesFor(items, item, pos.Exercise.Type, stage),
                isAudio ? item.AudioUrl : null);
        }).ToList();
        return cards;
    }

    /// <summary>
    /// Die Antwort des Kindes auf eine Übungskarte. <paramref name="ItemIndex"/> adressiert das Inhalts-Atom
    /// in der Übung. Getippte Stufen liefern <paramref name="GivenAnswer"/>, Anzeige-/Selbsteinschätzungs-
    /// Stufen <paramref name="WasKnown"/>. Die Stufe erzwingt der Server; er bewertet – nie das Frontend.
    /// </summary>
    public record ReviewDto(int ItemIndex, string? GivenAnswer, bool? WasKnown);

    /// <summary>Ergebnis einer Leitner-Wiederholung (serverseitig bewertet) inkl. Boni fürs Feedback.</summary>
    public record ReviewOutcome(bool WasCorrect, string Expected, int Awarded, int Box,
        DateOnly? DueOn, int Combo, int ComboBonus, int SpeedBonus);

    /// <summary>
    /// Nimmt die Antwort zu einer Übungskarte entgegen, bewertet sie serverseitig gegen die Item-Lösung
    /// und protokolliert die Wiederholung. Bei Leitner-Positionen wandert das Atom die Box hoch/runter,
    /// richtige Antworten bringen Punkte (+ Combo-/Speed-Bonus). Anti-Farming: gewertet wird nur eine
    /// fällige Karte und höchstens einmal pro Tag; nicht-getippte Selbsteinschätzung zählt bei
    /// <see cref="PlanPosition.RequireTypedTest"/> nicht.
    /// </summary>
    [HttpPost("{sessionId:int}/review")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewOutcome>> Review(int planId, int positionId, int sessionId, ReviewDto dto)
    {
        var session = await GetSession(planId, positionId, sessionId);
        if (session is null) return NotFound();
        var plan = (await GetPlan(planId))!;
        // Anti-Schummel: auch mit einer noch offenen Session darf der Sohn einen inzwischen deaktivierten
        // oder abgelaufenen Plan nicht weiter beüben (der Vater bleibt für Vorschau/Nachtrag ausgenommen).
        if (User.IsChild() && !PositionPlayService.PlanPlayableForChild(plan, DateOnly.FromDateTime(DateTime.UtcNow)))
            return this.ProblemWithCode(ApiErrors.PlanInactive, "This study plan is not currently active. Ask your parent.");
        var pos = await GetPosition(planId, positionId);
        if (pos?.Exercise is null) return NotFound();

        var items = await play.ItemsOfAsync(pos);
        if (dto.ItemIndex < 0 || dto.ItemIndex >= play.PoolSize(pos, items.Count))
            return this.ProblemWithCode(ApiErrors.NotFound, "The content does not belong to this position.");
        var item = items[dto.ItemIndex];

        // Stufe serverseitig erzwingen (nicht vom Client wählbar) und typ-neutral bewerten.
        var stage = PositionPlayService.StageForDay(pos, plan, session.Day);
        var typed = PositionPlayService.IsTypedStage(pos.Exercise.Type, stage);
        var wasCorrect = typed
            ? item.AcceptedAnswers.Any(a => grader.Matches(dto.GivenAnswer, a))
            : dto.WasKnown ?? false;

        var prog = await play.ProgressForAsync(positionId, dto.ItemIndex);
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
        var lastAt = session.Reviews.Count > 0 ? session.Reviews.Max(r => r.At) : (DateTime?)null;
        double? elapsedSeconds = lastAt is { } la ? (DateTime.UtcNow - la).TotalSeconds : null;

        db.ReviewEvents.Add(new ReviewEvent
        {
            PracticeSessionId = sessionId,
            ContentId = pos.ExerciseId,
            ItemIndex = dto.ItemIndex,
            StageValue = stage,
            WasCorrect = wasCorrect && scored,
        });

        if (!pos.UseLeitner || !scored)
        {
            await db.SaveChangesAsync();
            return NoContent();
        }

        var combo = wasCorrect ? prevStreak + 1 : 0;
        var (preBox, preReviewCount) = (prog.Box, prog.ReviewCount);
        play.ApplyReview(pos, prog, wasCorrect, session.Day, DateTime.UtcNow);

        var cfg = new ScoringService.ScoreConfig($"{plan.Title} · {pos.Exercise.Title}", pos.NewContentPoints,
            pos.ComboThreshold, pos.ComboBonusPoints, pos.SpeedThresholdSeconds, pos.SpeedBonusPoints);
        var score = await scoring.ScoreReviewAsync(cfg, preReviewCount, preBox, prog.Box, wasCorrect, combo,
            DateTime.Now, elapsedSeconds);
        foreach (var c in score.Contributions)
            db.ChildPoints.Add(new ChildPointsEntry { ChildId = plan.ChildId, Kind = c.Kind, Amount = c.Amount, Reason = c.Reason });
        await db.SaveChangesAsync();

        if (score.Total > 0)
            logger.LogInformation(
                "Positions-Wiederholung gewertet: Kind {ChildId} Plan {PlanId} Position {PositionId} Item {ItemIndex} " +
                "→ +{Total} Punkte (Basis {Base}, Combo ×{Combo} +{ComboBonus}, Speed +{SpeedBonus})",
                plan.ChildId, planId, positionId, dto.ItemIndex, score.Total, score.BasePoints, combo,
                score.ComboBonus, score.SpeedBonus);

        await gamification.EvaluateAndAwardAsync(plan.ChildId, session.Day);

        return new ReviewOutcome(wasCorrect, item.Answer, score.BasePoints, prog.Box, prog.DueOn, combo,
            score.ComboBonus, score.SpeedBonus);
    }

    /// <summary>Beendet die Sitzung und wertet zeitbasierte Missionen aus.</summary>
    [HttpPost("{sessionId:int}/end")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessionResponse>> End(int planId, int positionId, int sessionId)
    {
        var session = await GetSession(planId, positionId, sessionId);
        if (session is null) return NotFound();
        session.EndedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var plan = (await GetPlan(planId))!;
        // Ziel-Punkte der Position (idempotent): erfasst v. a. reine Inhalts-/Leseübungen, deren Ziel mit
        // dem Beenden der Sitzung erfüllt ist. VOR der Gamification, damit Missionen die Gutschrift sehen.
        await progress.EvaluateAndAwardAsync(plan, session.Day);
        await gamification.EvaluateAndAwardAsync(plan.ChildId, session.Day);
        return Map(session);
    }
}
