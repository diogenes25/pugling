using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Models;
using Pugling.Api.Services;

namespace Pugling.Api.Controllers.Learn;

/// <summary>Übungssitzungen des Kindes: erfassen echte Übungszeit und was geübt wurde.</summary>
[ApiController]
[Route("api/study-plans/{planId:int}/practice-sessions")]
[Tags("Study – Practice")]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(PlanOwnershipFilter))]
public class PracticeSessionsController(PuglingDbContext db, StudyProgressService progress,
    ScheduleService schedule, PointsService points)
    : ControllerBase
{
    /// <summary>Obergrenze der pro Heartbeat anrechenbaren Sekunden (großzügiges Intervall).</summary>
    private const int MaxHeartbeatSeconds = 120;

    public record SessionResponse(int Id, int PlanId, DateOnly Day, DateTime StartedAt, DateTime? EndedAt,
        int ActiveSeconds, int ReviewCount);

    static SessionResponse Map(PracticeSession s) =>
        new(s.Id, s.StudyPlanId, s.Day, s.StartedAt, s.EndedAt, s.ActiveSeconds, s.Reviews.Count);

    Task<StudyPlan?> GetPlan(int planId) => db.StudyPlans.FirstOrDefaultAsync(p => p.Id == planId);
    Task<PracticeSession?> GetSession(int planId, int sessionId) =>
        db.PracticeSessions.Include(s => s.Reviews).FirstOrDefaultAsync(s => s.Id == sessionId && s.StudyPlanId == planId);

    public record StartDto(DateOnly? Day);

    /// <summary>Startet eine Übungssitzung. Day nur zum Nachtragen/Testen; sonst heute.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessionResponse>> Start(int planId, StartDto dto)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (dto.Day is { } d && d != today && !User.IsFather())
            return Forbid(); // Nachtragen anderer Tage nur für den Vater (Anti-Schummel).
        var session = new PracticeSession { StudyPlanId = planId, Day = dto.Day ?? today };
        db.PracticeSessions.Add(session);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { planId, sessionId = session.Id }, Map(session));
    }

    /// <summary>Eine Übungssitzung.</summary>
    [HttpGet("{sessionId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessionResponse>> Get(int planId, int sessionId) =>
        await GetSession(planId, sessionId) is { } s ? Map(s) : NotFound();

    public record HeartbeatDto(int Seconds, bool Active);

    /// <summary>Fügt (aktive) Übungssekunden hinzu und gibt den aktuellen Tagesfortschritt zurück.</summary>
    [HttpPost("{sessionId:int}/heartbeat")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudyProgressService.DayProgress>> Heartbeat(int planId, int sessionId, HeartbeatDto dto)
    {
        var session = await GetSession(planId, sessionId);
        if (session is null) return NotFound();
        // Pro Heartbeat höchstens ein plausibles Intervall gutschreiben (Anti-Zeit-Cheat).
        if (dto.Active && dto.Seconds > 0) session.ActiveSeconds += Math.Clamp(dto.Seconds, 0, MaxHeartbeatSeconds);
        await db.SaveChangesAsync();

        var plan = (await GetPlan(planId))!;
        return await progress.EvaluateAndAwardAsync(plan, session.Day);
    }

    /// <summary>Alle N Treffer in Folge gibt es einen eskalierenden Combo-Bonus (Motivations-Feature).</summary>
    private const int ComboMilestone = 5;

    /// <summary>ContentId = Vokabel-Id bzw. Lückentext-Id; Stage = jeweilige Stufe (verfahrensabhängig).</summary>
    public record ReviewDto(int ContentId, int Stage, bool WasCorrect);
    /// <summary>
    /// Ergebnis einer Leitner-Wiederholung: vergebene Punkte, neue Box, nächste Fälligkeit sowie die
    /// serverseitig gezählte Combo (Treffer in Folge) und ein etwaiger Combo-Bonus.
    /// </summary>
    public record ReviewOutcome(int Awarded, int Box, DateOnly? DueOn, int Combo, int ComboBonus);

    /// <summary>
    /// Protokolliert eine Wiederholung (was wurde geübt, auf welcher Stufe, richtig?). Bei Leitner-Plänen
    /// wandert die Karte zusätzlich die Box hoch/runter und richtige Antworten bringen dem Kind Punkte.
    /// </summary>
    [HttpPost("{sessionId:int}/review")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewOutcome>> Review(int planId, int sessionId, ReviewDto dto)
    {
        var session = await GetSession(planId, sessionId);
        if (session is null) return NotFound();

        var item = await db.StudyPlanItems.FirstOrDefaultAsync(i => i.StudyPlanId == planId
            && (i.VocabularyId == dto.ContentId || i.ClozeTextId == dto.ContentId));
        if (item is null) return NotFound("Inhalt gehört nicht zum Lehrplan.");

        db.ReviewEvents.Add(new ReviewEvent
        {
            PracticeSessionId = sessionId,
            ContentId = dto.ContentId,
            StageValue = dto.Stage,
            WasCorrect = dto.WasCorrect,
        });

        var plan = (await GetPlan(planId))!;
        if (!plan.UseLeitner)
        {
            await db.SaveChangesAsync();
            return NoContent();
        }

        // Combo serverseitig zählen (cheat-sicher): Treffer in Folge am Ende der bisherigen Sitzungs-Reviews.
        var prevStreak = 0;
        foreach (var r in session.Reviews.OrderByDescending(r => r.At).ThenByDescending(r => r.Id))
        {
            if (r.WasCorrect) prevStreak++; else break;
        }
        var combo = dto.WasCorrect ? prevStreak + 1 : 0;
        // Alle ComboMilestone Treffer in Folge: eskalierender Bonus (5, 10, 15 …).
        var comboBonus = combo > 0 && combo % ComboMilestone == 0 ? ComboMilestone * (combo / ComboMilestone) : 0;

        // Punkte aus dem Zustand VOR dem Box-Aufstieg berechnen (neuer Inhalt zählt am meisten),
        // dann die Karte terminieren.
        int awarded = dto.WasCorrect ? await points.PointsForReviewAsync(item.ReviewCount, item.Box, DateTime.Now) : 0;
        schedule.ApplyReview(plan, item, dto.WasCorrect, session.Day, DateTime.UtcNow);
        if (awarded > 0)
            db.ChildPoints.Add(new ChildPointsEntry
            {
                ChildId = plan.ChildId,
                Amount = awarded,
                Reason = $"[{plan.Title}] Leitner-Wiederholung richtig → Box {item.Box}",
            });
        if (comboBonus > 0)
            db.ChildPoints.Add(new ChildPointsEntry
            {
                ChildId = plan.ChildId,
                Amount = comboBonus,
                Reason = $"[{plan.Title}] Combo ×{combo} – Bonus!",
            });
        await db.SaveChangesAsync();

        return new ReviewOutcome(awarded, item.Box, item.DueOn, combo, comboBonus);
    }

    /// <summary>Beendet die Sitzung, wertet den Tag aus und gibt den Fortschritt zurück.</summary>
    [HttpPost("{sessionId:int}/end")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudyProgressService.DayProgress>> End(int planId, int sessionId)
    {
        var session = await GetSession(planId, sessionId);
        if (session is null) return NotFound();
        session.EndedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var plan = (await GetPlan(planId))!;
        return await progress.EvaluateAndAwardAsync(plan, session.Day);
    }
}
