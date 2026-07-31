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
    ItemProgressService itemProgress) : ControllerBase
{
    /// <summary>Default pass threshold when the position sets no threshold of its own.</summary>
    private const int DefaultPassPercent = 80;

    /// <summary>
    /// How many test attempts the child gets per goal period. A second chance is pedagogically right, the
    /// fifth is grade farming: without a cap the child can restart until the result is good and silently drop
    /// every bad run (an abandoned attempt writes nothing – see <see cref="Answer"/>).
    /// <para>
    /// Deliberately a constant and not a field on the position: a per-position cap would be a schema change,
    /// and the migration chain is folded rather than extended. If the cap ever needs to be configurable, this
    /// is the one place to replace.
    /// </para>
    /// </summary>
    private const int MaxAttemptsPerPeriod = 2;


    // Kein Vorgabewert für `ct`: er ließe die Aufrufstelle korrekt aussehen, während der Abbruch des
    // Clients verpufft – ein weggelassenes optionales Argument sieht weder CA2016 noch der Wächter.
    private Task<StudyPlan?> GetPlan(int planId, CancellationToken ct) =>
        db.StudyPlans.FirstOrDefaultAsync(p => p.Id == planId, ct);

    // Der Plan kommt mit, weil die Bebilderung das Kind braucht (die Auswahl hängt an seinem Profil).
    private Task<PlanPosition?> GetPosition(int planId, int positionId, CancellationToken ct) =>
        db.PlanPositions.Include(p => p.Exercise).Include(p => p.StudyPlan)
            .FirstOrDefaultAsync(p => p.Id == positionId && p.StudyPlanId == planId, ct);

    private Task<TestAttempt?> LoadAttempt(int planId, int positionId, int attemptId, CancellationToken ct) =>
        db.TestAttempts.Include(t => t.Results)
            .FirstOrDefaultAsync(t => t.Id == attemptId && t.StudyPlanId == planId && t.PlanPositionId == positionId, ct);

    private static TestItem ToItem(IReadOnlyList<ContentItem> items, ContentItem item, IExerciseType type, int stage, bool typed)
    {
        // Geteilte Anti-Cheat-Projektion (Reveal/Länge/Hint/Choices/Audio/Bild je Stufe) – dieselbe Regel wie die Übungskarte.
        var f = PositionPlayService.CardFacets(items, item, type, stage, typed);
        return new TestItem(item.Index, item.Prompt, stage, f.Reveal, f.AnswerLength, f.Hint, f.Choices, f.AudioUrl,
            f.ImageUrl, f.ImageAlt);
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

        var items = await play.ItemsOfAsync(pos, pos.StudyPlan?.ChildId, ct);
        var poolSize = play.PoolSize(pos, items.Count);
        if (poolSize == 0) return this.ProblemWithCode(ApiErrors.NoCheckableContent, "The exercise contains no checkable content.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (dto.Day is { } dd && dd != today && !User.IsSupervisor()) return Forbid();
        // Anti-Schummel: der Sohn darf nur seinen aktiven, laufenden Plan testen (siehe Übungs-Start).
        if (User.IsStudent() && !PositionPlayService.PlanPlayableForChild(plan, today))
            return this.ProblemWithCode(ApiErrors.PlanInactive, "This study plan is not currently active. Ask your parent.");
        var day = dto.Day ?? today;

        // Der Sohn läuft nicht aus einem laufenden Versuch heraus und bekommt je Periode nur begrenzt viele.
        // Der Vater bleibt ausgenommen: er nutzt den Endpunkt für Vorschau/Nachtrag mit eigener Stufe.
        if (User.IsStudent())
        {
            // Dieselben Periodengrenzen wie die Ziel-Abrechnung – eine zweite Rechnung würde driften.
            var (from, to) = PositionProgressService.PeriodRange(pos.Cadence, day);

            // Fortsetzen statt neu anlegen: Cursor und Reihenfolge liegen persistiert vor. Ohne das würde ein
            // versehentlicher Reload einen der knappen Versuche verbrennen – der Deckel wäre dann eine Strafe
            // für Pech statt für Farming.
            if (await db.TestAttempts
                    .Where(t => t.PlanPositionId == positionId && t.Day >= from && t.Day <= to && t.CompletedAt == null)
                    .OrderBy(t => t.Id)
                    .FirstOrDefaultAsync(ct) is { } open)
                return new AttemptResponse(open.Id, planId, positionId, open.Day, open.StageValue, open.TotalItems);

            // Gezählt wird der START, nicht die Abgabe: sonst bliebe Weglaufen gratis und der Deckel wirkungslos.
            var used = await db.TestAttempts
                .CountAsync(t => t.PlanPositionId == positionId && t.Day >= from && t.Day <= to, ct);
            if (used >= MaxAttemptsPerPeriod)
                return this.ProblemWithCode(ApiErrors.TestAttemptsExhausted,
                    "No test attempts left for this period. Ask your parent.");
        }

        // Stufe: nur der Vater darf sie frei wählen; für den Sohn gilt die Fahrplan-/Positions-Stufe des Tages.
        if (play.TypeOf(pos.Exercise) is not { } type)
            return this.ProblemWithCode(ApiErrors.UnknownExerciseType, "The exercise has an unknown type.");
        var stage = User.IsSupervisor() && dto.Stage is not null ? dto.Stage.Value : PositionPlayService.StageForDay(pos, plan, day, type);
        var typed = type.IsTypedStage(stage);

        // Der Test ist Standortbestimmung: bereits eingeführte Inhalte prüfen, sonst den gesamten Pool
        // (sperrt nicht, wenn per Üben noch nichts „fällig" ist).
        var progress = await db.PositionItemProgress
            .Where(p => p.PlanPositionId == positionId && p.ItemIndex < poolSize)
            .ToDictionaryAsync(p => p.ItemIndex, ct);
        var introduced = progress.Values.Where(p => p.IntroducedAt != null).Select(p => p.ItemIndex).ToList();
        var pool = introduced.Count > 0 ? introduced : Enumerable.Range(0, poolSize).ToList();
        // Prüfungsreihenfolge gemäß Strategie der Position EINFRIEREN (strikt server-getrieben, kein Zurück).
        var order = PositionPlayService.OrderIndices(pool.Select(i => (i, progress.GetValueOrDefault(i))), pos.OrderStrategy);

        var attempt = new TestAttempt
        {
            StudyPlanId = planId,
            PlanPositionId = positionId,
            Day = day,
            StageValue = stage,
            Graded = typed,
            TotalItems = pool.Count,
            Order = [.. order],
            // Die Übung steht am Versuch (über die Position), nicht an jeder Ergebniszeile: ContentId war
            // eine FK-lose Kopie von PlanPosition.ExerciseId, die niemand gelesen hat.
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

        var items = await play.ItemsOfAsync(pos, pos.StudyPlan?.ChildId, ct);
        if (play.TypeOf(pos.Exercise) is not { } type)
            return this.ProblemWithCode(ApiErrors.UnknownExerciseType, "The exercise has an unknown type.");
        var typed = type.IsTypedStage(attempt.StageValue);
        var cursor = PositionPlayService.SkipRemoved(attempt.Order, attempt.Cursor, items.Count);
        if (cursor != attempt.Cursor) { attempt.Cursor = cursor; await db.SaveChangesAsync(ct); }
        if (cursor >= attempt.Order.Count) return new TestNextResponse(null, true, cursor, attempt.TotalItems);

        var item = ToItem(items, items[attempt.Order[cursor]], type, attempt.StageValue, typed);
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

        var items = await play.ItemsOfAsync(pos, pos.StudyPlan?.ChildId, ct);
        if (play.TypeOf(pos.Exercise) is not { } type)
            return this.ProblemWithCode(ApiErrors.UnknownExerciseType, "The exercise has an unknown type.");
        var typed = type.IsTypedStage(attempt.StageValue);
        var cursor = PositionPlayService.SkipRemoved(attempt.Order, attempt.Cursor, items.Count);
        if (cursor < attempt.Order.Count)
        {
            var index = attempt.Order[cursor];
            var item = items[index];
            var result = attempt.Results.FirstOrDefault(r => r.ItemIndex == index);
            var correct = typed
                ? item.AcceptedAnswers.Any(a => grader.Matches(dto.GivenAnswer, a))
                : dto.WasKnown ?? false;
            if (result is not null)
            {
                result.GivenAnswer = dto.GivenAnswer;
                result.WasCorrect = correct;
            }
            // Bewusst KEINE plan-übergreifende Aufzeichnung hier: der Item-Fortschritt/die Historie werden
            // erst beim Abschluss (Submit) EINMAL geschrieben, damit abgebrochene/wiederholte Versuche den
            // Lernstand nicht verfälschen (sonst zählte jede Zwischenantwort dauerhaft, auch ohne Abgabe).
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
        return new AttemptDetail(a.Id, a.StudyPlanId, positionId, a.Day, a.StageValue, a.StartedAt, a.CompletedAt,
            a.TotalItems, a.CorrectItems, a.ScorePercent, a.Passed,
            a.Results.OrderBy(r => r.ItemIndex).Select(r => new ItemResultDto(r.ItemIndex ?? 0, r.GivenAnswer, r.WasCorrect, r.HintsUsed)).ToList());
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
        // Anti-Schummel: einen inzwischen deaktivierten oder abgelaufenen Plan darf der Sohn auch nicht über
        // einen offenen Testversuch abschließen und bepunkten (der Vater bleibt ausgenommen).
        if (User.IsStudent() && !PositionPlayService.PlanPlayableForChild(plan, DateOnly.FromDateTime(DateTime.UtcNow)))
            return this.ProblemWithCode(ApiErrors.PlanInactive, "This study plan is not currently active. Ask your parent.");
        var pos = await GetPosition(planId, positionId, ct);
        if (pos?.Exercise is null) return NotFound();

        var items = await play.ItemsOfAsync(pos, pos.StudyPlan?.ChildId, ct);
        if (play.TypeOf(pos.Exercise) is not { } type)
            return this.ProblemWithCode(ApiErrors.UnknownExerciseType, "The exercise has an unknown type.");
        var typed = type.IsTypedStage(attempt.StageValue);

        // Bulk-Abgabe (Legacy/Fallback): übergebene Antworten nur BEWERTEN (Aufzeichnung folgt einmalig unten).
        // Im Klausur-Fluss ist die Liste leer – die Ergebnisse stehen bereits aus den schrittweisen /answer fest.
        if (dto.Answers is { Count: > 0 } bulk)
        {
            var answers = bulk.ToDictionary(a => a.ItemIndex);
            foreach (var result in attempt.Results)
            {
                var index = result.ItemIndex ?? 0;
                // Das Item kann seit Test-Start entfernt/umsortiert worden sein (Item-CRUD); nicht mehr existierende
                // Indizes überspringen, statt out-of-range zu laufen oder das falsche Wort zu bewerten.
                if (index < 0 || index >= items.Count) continue;
                if (!answers.TryGetValue(index, out var answer)) continue;
                var item = items[index];
                result.GivenAnswer = answer.GivenAnswer;
                result.WasCorrect = typed
                    ? item.AcceptedAnswers.Any(a => grader.Matches(answer.GivenAnswer, a))
                    : answer.WasKnown ?? false;
            }
        }

        // Nur noch existierende (nicht mid-Test gelöschte) Items zählen – für Ergebnis-Karten, Aufzeichnung UND Quote.
        var scorable = attempt.Results
            .Where(r => (r.ItemIndex ?? 0) >= 0 && (r.ItemIndex ?? 0) < items.Count)
            .OrderBy(r => r.ItemIndex)
            .ToList();

        // Ergebnis-Karten aufbauen und den plan-übergreifenden Item-Fortschritt/Historie genau EINMAL je
        // abgeschlossenem Versuch schreiben (nicht je Zwischenantwort) – Idempotenz gegen Abbruch/Wiederholung.
        var outcomes = new List<ItemOutcome>(scorable.Count);
        foreach (var r in scorable)
        {
            var index = r.ItemIndex ?? 0;
            var item = items[index];
            outcomes.Add(new ItemOutcome(index, item.Prompt, item.Answer, r.GivenAnswer, r.WasCorrect));
            await itemProgress.RecordAsync(plan.ChildId, pos.ExerciseId, item, r.WasCorrect, attempt.StageValue,
                typed ? r.GivenAnswer : null, ItemReviewSource.Test, positionId, attempt.Day, countsForMastery: true, ct: ct);
        }

        var passPercent = pos.GoalThreshold is > 0 ? pos.GoalThreshold.Value : DefaultPassPercent;
        // Quote über die tatsächlich gestellten (noch existierenden) Fragen, nicht über die eingefrorene Startzahl:
        // ein mid-Test gelöschtes Item soll die erreichbare Punktzahl nicht heimlich senken.
        attempt.TotalItems = scorable.Count;
        attempt.CorrectItems = scorable.Count(r => r.WasCorrect);
        attempt.ScorePercent = attempt.TotalItems == 0 ? 0
            : (int)Math.Round(100.0 * attempt.CorrectItems / attempt.TotalItems);
        attempt.Passed = attempt.ScorePercent >= passPercent;
        attempt.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        // Ziel-Punkte der Position (idempotent) VOR der Gamification buchen, damit münz-basierte
        // Missionen die frische Gutschrift bereits sehen.
        await progress.EvaluateAndAwardAsync(plan, attempt.Day, ct);
        // Metrik-basierte Missionen/Auszeichnungen (z. B. „Tests bestanden") auch am Test-Abschluss auswerten.
        await gamification.EvaluateAndAwardAsync(plan.ChildId, attempt.Day, ct);

        return new SubmitResponse(attempt.Id, attempt.StageValue, attempt.TotalItems, attempt.CorrectItems,
            attempt.ScorePercent, attempt.Passed, passPercent, outcomes);
    }
}
