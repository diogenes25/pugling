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
[ApiVersion("1.0")]
[Route(ApiRoutes.V1 + "/study-plans/{planId:int}/practice-sessions")]
[Tags("Study – Practice")]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(PlanOwnershipFilter))]
public class PracticeSessionsController(PuglingDbContext db, StudyProgressService progress,
    ScheduleService schedule, ScoringService scoring, GamificationService gamification, AnswerGrader grader,
    ILogger<PracticeSessionsController> logger)
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

    /// <summary>Antwort auf eine einzelne Lücke eines Lückentexts (positionsbezogen).</summary>
    public record GapAnswer(int GapIndex, string? GivenAnswer);

    /// <summary>
    /// Die vom Kind abgegebene Antwort auf eine Übungskarte. ContentId = Vokabel- bzw. Lückentext-Id.
    /// Die Stufe bestimmt der Server aus dem Fahrplan (nicht wählbar, Anti-Schummel – wie bei den Tests);
    /// das Frontend liefert die zur Stufe passende Antwort: getippte Vokabel-/Zuordnungs-Stufen
    /// <paramref name="GivenAnswer"/>, Lückentexte <paramref name="Gaps"/>, reine Anzeige-/Selbst-
    /// einschätzungs-Stufen <paramref name="WasKnown"/>. Der Server bewertet – nie das Frontend.
    /// </summary>
    public record ReviewDto(int ContentId, string? GivenAnswer,
        IReadOnlyList<GapAnswer>? Gaps, bool? WasKnown);

    /// <summary>
    /// Ergebnis einer Leitner-Wiederholung: ob die Antwort richtig war (serverseitig bewertet), die nun
    /// aufgedeckte Lösung, vergebene Punkte, neue Box, nächste Fälligkeit sowie die serverseitig gezählte
    /// Combo (Treffer in Folge) und etwaige Boni (Combo, Schnelle Antwort) – fürs Feedback im Frontend.
    /// </summary>
    public record ReviewOutcome(bool WasCorrect, string? Expected, int Awarded, int Box,
        DateOnly? DueOn, int Combo, int ComboBonus, int SpeedBonus);

    /// <summary>
    /// Nimmt die Antwort des Kindes zu einer Übungskarte entgegen, bewertet sie serverseitig gegen die
    /// hinterlegte Lösung und protokolliert die Wiederholung. Bei Leitner-Plänen wandert die Karte
    /// zusätzlich die Box hoch/runter und richtige Antworten bringen Punkte (+ Combo-Bonus).
    /// Reine Selbsteinschätzungs-Stufen werden bei <see cref="StudyPlan.RequireTypedTest"/> nur
    /// protokolliert (keine Punkte, keine Box-Bewegung) – gegen Selbstbetrug.
    /// </summary>
    [HttpPost("{sessionId:int}/review")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewOutcome>> Review(int planId, int sessionId, ReviewDto dto)
    {
        var session = await GetSession(planId, sessionId);
        if (session is null) return NotFound();

        var item = await db.StudyPlanItems
            .Include(i => i.Vocabulary)
            .Include(i => i.ClozeText)
            .FirstOrDefaultAsync(i => i.StudyPlanId == planId
                && (i.VocabularyId == dto.ContentId || i.ClozeTextId == dto.ContentId));
        if (item is null) return Problem(statusCode: 404, detail: "Inhalt gehört nicht zum Lehrplan.");

        var plan = (await GetPlan(planId))!;
        // Stufe serverseitig aus dem Fahrplan erzwingen (nicht vom Client wählbar, wie bei den Tests):
        // sonst könnte der Sohn eine getippte Stufe auf Selbsteinschätzung herunterstufen.
        var stage = StudyProgressService.StageForDay(plan, session.Day);
        // Serverseitige Bewertung: das Frontend liefert nur die Antwort, nie ein "richtig"-Flag.
        var (wasCorrect, expected, isTypedStage) = Grade(plan.Method, stage, dto, item);

        // Erstkontakt über die Übungssession zählt als Einführung. Sonst bliebe IntroducedAt bei rein
        // übungsbasiertem Lernen null → die NewWords-Metrik und die Stundenplan-Auswahl (nächste Charge)
        // stünden still, weil bislang nur der Test-/Generate-Fluss einführt.
        if (item.IntroducedAt is null)
        {
            item.IntroducedAt = session.Day;
            item.DueOn ??= session.Day;
        }

        // Gewertet wird nur eine fällige Karte, und höchstens einmal pro Tag (Anti-Farming): sonst könnte
        // der Sohn dieselbe Karte – zumal die Lösung im Feedback steht – wiederholt einreichen und
        // Punkte/Combo endlos abgreifen. Eine falsch beantwortete Karte bleibt zwar zum Weiterüben fällig,
        // bringt aber am selben Tag keine weiteren Punkte mehr.
        var due = item.DueOn is null || item.DueOn <= session.Day;
        var alreadyScoredToday = item.LastReviewedAt is { } lastReviewed
            && DateOnly.FromDateTime(lastReviewed) == session.Day;
        // Selbsteinschätzung zählt nur, wenn der Plan sie zulässt. Sonst wertlos: weder Punkte/Box noch
        // Combo noch Statistik – daher gar nicht erst als "richtig" protokollieren (Anti-Selbstbetrug).
        var scored = (isTypedStage || !plan.RequireTypedTest) && due && !alreadyScoredToday;

        // Combo VOR dem Hinzufügen des neuen Reviews zählen: EF-Relationship-Fixup würde das noch
        // ungespeicherte Review sonst in session.Reviews einreihen und die Combo um 1 verfälschen.
        var prevStreak = 0;
        foreach (var r in session.Reviews.OrderByDescending(r => r.At).ThenByDescending(r => r.Id))
        {
            if (r.WasCorrect) prevStreak++; else break;
        }
        // Antwortzeit serverseitig messen (Zeit seit der letzten Antwort derselben Sitzung) – VOR dem
        // Hinzufügen des neuen Reviews, sonst zählte EF-Fixup dieses als jüngstes und die Zeit wäre 0.
        var lastAt = session.Reviews.Count > 0 ? session.Reviews.Max(r => r.At) : (DateTime?)null;
        double? elapsedSeconds = lastAt is { } la ? (DateTime.UtcNow - la).TotalSeconds : null;

        db.ReviewEvents.Add(new ReviewEvent
        {
            PracticeSessionId = sessionId,
            ContentId = dto.ContentId,
            StageValue = stage,
            WasCorrect = wasCorrect && scored,
        });

        // Nicht gewertet: kein Leitner-Plan, oder nicht zählende Selbsteinschätzung (RequireTypedTest).
        // Nur protokollieren, keine Punkte/Box-Bewegung.
        if (!plan.UseLeitner || !scored)
        {
            await db.SaveChangesAsync();
            return NoContent();
        }

        // Combo serverseitig (cheat-sicher): Treffer in Folge inkl. dieses Reviews.
        var combo = wasCorrect ? prevStreak + 1 : 0;

        // Zustand VOR dem Box-Aufstieg festhalten (neuer Inhalt zählt am meisten), dann terminieren.
        var (preBox, preReviewCount) = (item.Box, item.ReviewCount);
        schedule.ApplyReview(plan, item, wasCorrect, session.Day, DateTime.UtcNow);

        // Punkte zentral im ScoringService: Basis (Box/Neuheit × Zeitfenster) + Ereignis-Boni (Combo, Speed).
        var score = await scoring.ScoreReviewAsync(plan, preReviewCount, preBox, item.Box, wasCorrect, combo,
            DateTime.Now, elapsedSeconds);
        foreach (var c in score.Contributions)
            db.ChildPoints.Add(new ChildPointsEntry
            {
                ChildId = plan.ChildId,
                Kind = c.Kind,
                Amount = c.Amount,
                Reason = c.Reason,
            });
        await db.SaveChangesAsync();

        // Audit-Trail für die punkte-relevanteste Aktion (Anti-Cheat/Streitfall nachvollziehbar):
        // wer, welcher Plan, richtig?, welche Boni – nur wenn tatsächlich Punkte flossen.
        if (score.Total > 0)
            logger.LogInformation(
                "Wiederholung gewertet: Kind {ChildId} Plan {PlanId} Inhalt {ContentId} → +{Total} Punkte " +
                "(Basis {Base}, Combo ×{Combo} +{ComboBonus}, Speed +{SpeedBonus})",
                plan.ChildId, planId, dto.ContentId, score.Total, score.BasePoints, combo,
                score.ComboBonus, score.SpeedBonus);

        // Missionen/Auszeichnungen nach jeder gewerteten Wiederholung auswerten – Belohnungen fließen
        // beim Spielen, nicht erst beim Ansehen. Idempotent (je Zeitraum/Auszeichnung höchstens einmal).
        await gamification.EvaluateAndAwardAsync(plan.ChildId, session.Day);

        return new ReviewOutcome(wasCorrect, expected, score.BasePoints, item.Box, item.DueOn, combo,
            score.ComboBonus, score.SpeedBonus);
    }

    /// <summary>
    /// Bewertet eine abgegebene Antwort serverseitig gegen die Lösung des Inhalts und meldet, ob die
    /// Stufe überhaupt getippt/objektiv war (sonst reine Selbsteinschätzung). <c>expected</c> ist die
    /// nun aufdeckbare Lösung fürs Feedback.
    /// </summary>
    private (bool wasCorrect, string? expected, bool isTypedStage) Grade(
        LearningMethod method, int stage, ReviewDto dto, StudyPlanItem item)
    {
        if (item.ClozeTextId is not null)
        {
            var text = item.ClozeText!;
            var expected = string.Join(", ", text.Gaps.OrderBy(g => g.Index).Select(g => g.Answer));
            // Nur Freitext-Stufen prüfen wir gegen die Lösung; Wortpool-Stufen sind Auswahl → Selbsteinschätzung.
            if (!StudyProgressService.IsTyped((ClozeStage)stage))
                return (dto.WasKnown ?? false, expected, false);

            var given = new Dictionary<int, string?>();
            foreach (var g in dto.Gaps ?? []) given[g.GapIndex] = g.GivenAnswer;
            var allCorrect = text.Gaps.Count > 0
                && text.Gaps.All(g => grader.MatchesGap(g, given.GetValueOrDefault(g.Index)));
            return (allCorrect, expected, true);
        }

        var v = item.Vocabulary!;
        if (method == LearningMethod.Matching)
        {
            // Richtung aus der Stufe: Reverse fragt das Wort ab, sonst die Übersetzung. Zuordnung ist objektiv.
            var reverse = (MatchStage)stage is MatchStage.Reverse or MatchStage.ReverseDistractors;
            var expected = reverse ? v.Word : v.Translation;
            return (grader.Matches(dto.GivenAnswer, expected), expected, true);
        }

        // Vokabel: getippte Stufen serverseitig prüfen, Anzeige-/Selbsteinschätzung dem WasKnown-Flag überlassen.
        if (!StudyProgressService.IsTyped((TestStage)stage))
            return (dto.WasKnown ?? false, v.Translation, false);
        return (grader.Matches(dto.GivenAnswer, v.Translation), v.Translation, true);
    }

    /// <summary>
    /// Eine Übungskarte für den Sohn – bewusst OHNE Lösung (außer bei Anzeige-/Selbsteinschätzungs-Stufen,
    /// die die Lösung per Design aufdecken). So bleibt die Bewertung serverseitig (siehe <see cref="Review"/>).
    /// </summary>
    public record PracticeCard(int ContentId, int Stage, LearningMethod Method, string Prompt,
        int? AnswerLength, string? AudioUrl, string? Translation,
        IReadOnlyList<int>? GapIndexes, IReadOnlyList<string>? WordBank);

    /// <summary>Liefert die für heute fälligen Übungskarten (lösungsfrei) laut Stundenplan/Leitner-Fälligkeit.</summary>
    [HttpGet("{sessionId:int}/cards")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PracticeCard>>> Cards(int planId, int sessionId)
    {
        var session = await GetSession(planId, sessionId);
        if (session is null) return NotFound();

        var plan = await db.StudyPlans
            .AsNoTracking()
            .Include(p => p.Items.OrderBy(i => i.Order)).ThenInclude(i => i.Vocabulary)
            .Include(p => p.Items).ThenInclude(i => i.ClozeText)
            .FirstOrDefaultAsync(p => p.Id == planId);
        if (plan is null) return NotFound();

        var stage = StudyProgressService.StageForDay(plan, session.Day);
        var selection = await schedule.SelectAsync(plan, session.Day);
        return selection.Items.Select(i => ToCard(i, plan.Method, stage)).ToList();
    }

    /// <summary>Projiziert einen Plan-Inhalt auf eine Übungskarte und hält die Lösung getippter Stufen zurück.</summary>
    private static PracticeCard ToCard(StudyPlanItem i, LearningMethod method, int stage)
    {
        if (i.ClozeTextId is not null)
        {
            var t = i.ClozeText!;
            var cs = (ClozeStage)stage;
            var showTranslation = cs is ClozeStage.TranslationWordBank or ClozeStage.TranslationFreeText;
            var showWordBank = cs is ClozeStage.WordBank or ClozeStage.TranslationWordBank;
            return new PracticeCard(t.Id, stage, method, t.Text, null, null,
                showTranslation ? t.Translation : null,
                t.Gaps.OrderBy(g => g.Index).Select(g => g.Index).ToList(),
                showWordBank ? t.WordBank : null);
        }

        var v = i.Vocabulary!;
        if (method == LearningMethod.Matching)
        {
            var reverse = (MatchStage)stage is MatchStage.Reverse or MatchStage.ReverseDistractors;
            return new PracticeCard(v.Id, stage, method, reverse ? v.Translation : v.Word,
                null, null, null, null, null);
        }

        var ts = (TestStage)stage;
        // Anzeige-/Selbsteinschätzungs-Stufen decken die Lösung bewusst auf (Sohn urteilt selbst);
        // getippte Stufen halten sie zurück – der Server bewertet in /review.
        var reveal = ts is TestStage.ShowBoth or TestStage.SelfAssess;
        return new PracticeCard(v.Id, stage, method, v.Word,
            ts == TestStage.LetterBoxes ? v.Translation.Length : null,
            ts == TestStage.Audio ? v.PronunciationAudioUrl : null,
            reveal ? v.Translation : null,
            null, null);
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
        var dayProgress = await progress.EvaluateAndAwardAsync(plan, session.Day);
        // Zeitbasierte Missionen (z.B. MinutesPracticed) am Sitzungsende auswerten – nicht im häufigen
        // Heartbeat und nicht erst beim Ansehen der Missionsseite (die ist reine Lesesicht).
        await gamification.EvaluateAndAwardAsync(plan.ChildId, session.Day);
        return dayProgress;
    }
}
