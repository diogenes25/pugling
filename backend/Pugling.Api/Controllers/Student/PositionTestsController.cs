using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;
using Pugling.Api.Services;

namespace Pugling.Api.Controllers.Student;

/// <summary>
/// Abschlusstest einer einzelnen Lehrplan-Position (neues Modell): prüft die Inhalte EINER Übung.
/// Inhalt kommt aus der Übungs-Config (<see cref="ExerciseContentProvider"/>), bewertet wird typ-neutral
/// gegen die Item-Lösung. Bestehen misst sich an <see cref="PlanPosition.GoalThreshold"/> (Standard 80 %).
/// Die Punkte fürs Bestehen (per-Position-Ziel) folgen in der Ziel-/Punkte-Engine (Etappe 4); hier zählt
/// der Versuch bereits für metrik-basierte Missionen (z. B. „Tests bestanden").
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
    /// <summary>Standard-Bestehensgrenze, wenn die Position keine eigene Schwelle setzt.</summary>
    private const int DefaultPassPercent = 80;

    public record TestItem(int ItemIndex, string Prompt, int Stage, string? Reveal, int? AnswerLength, string? Hint,
        IReadOnlyList<string>? Choices, string? AudioUrl);
    /// <summary>
    /// Antwort des Test-Starts. Der Klausur-Modus ist strikt server-getrieben: es kommen <b>keine</b> Aufgaben
    /// im Bulk, nur die Metadaten. Die Fragen holt der Client einzeln über <see cref="Next"/> (kein Zurück).
    /// </summary>
    public record AttemptResponse(int AttemptId, int PlanId, int PositionId, DateOnly Day, int Stage, int TotalItems);

    private Task<StudyPlan?> GetPlan(int planId) => db.StudyPlans.FirstOrDefaultAsync(p => p.Id == planId);

    private Task<PlanPosition?> GetPosition(int planId, int positionId) =>
        db.PlanPositions.Include(p => p.Exercise)
            .FirstOrDefaultAsync(p => p.Id == positionId && p.StudyPlanId == planId);

    private Task<TestAttempt?> LoadAttempt(int planId, int positionId, int attemptId) =>
        db.TestAttempts.Include(t => t.Results)
            .FirstOrDefaultAsync(t => t.Id == attemptId && t.StudyPlanId == planId && t.PlanPositionId == positionId);

    private static TestItem ToItem(IReadOnlyList<ContentItem> items, ContentItem item, ExerciseType type, int stage, bool typed)
    {
        // Geteilte Anti-Cheat-Projektion (Reveal/Länge/Hint/Choices/Audio je Stufe) – dieselbe Regel wie die Übungskarte.
        var f = PositionPlayService.CardFacets(items, item, type, stage, typed);
        return new TestItem(item.Index, item.Prompt, stage, f.Reveal, f.AnswerLength, f.Hint, f.Choices, f.AudioUrl);
    }

    public record StartDto(int? Stage, DateOnly? Day);

    /// <summary>
    /// Startet einen Testversuch für die Position. Der Klausur-Modus ist strikt server-getrieben: der Start
    /// friert die Prüfungsreihenfolge ein und liefert nur die Metadaten – die Fragen holt der Client einzeln
    /// über <see cref="Next"/> und beantwortet sie über <see cref="Answer"/> (kein Zurück, Feedback erst bei
    /// <see cref="Submit"/>).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttemptResponse>> Start(int planId, int positionId, StartDto dto)
    {
        var plan = await GetPlan(planId);
        if (plan is null) return NotFound();
        var pos = await GetPosition(planId, positionId);
        if (pos?.Exercise is null) return NotFound();

        var items = await play.ItemsOfAsync(pos);
        var poolSize = play.PoolSize(pos, items.Count);
        if (poolSize == 0) return this.ProblemWithCode(ApiErrors.NoCheckableContent, "The exercise contains no checkable content.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (dto.Day is { } dd && dd != today && !User.IsSupervisor()) return Forbid();
        // Anti-Schummel: der Sohn darf nur seinen aktiven, laufenden Plan testen (siehe Übungs-Start).
        if (User.IsStudent() && !PositionPlayService.PlanPlayableForChild(plan, today))
            return this.ProblemWithCode(ApiErrors.PlanInactive, "This study plan is not currently active. Ask your parent.");
        var day = dto.Day ?? today;
        // Stufe: nur der Vater darf sie frei wählen; für den Sohn gilt die Fahrplan-/Positions-Stufe des Tages.
        var stage = User.IsSupervisor() && dto.Stage is not null ? dto.Stage.Value : PositionPlayService.StageForDay(pos, plan, day);
        var typed = PositionPlayService.IsTypedStage(pos.Exercise.Type, stage);

        // Der Test ist Standortbestimmung: bereits eingeführte Inhalte prüfen, sonst den gesamten Pool
        // (sperrt nicht, wenn per Üben noch nichts „fällig" ist).
        var progress = await db.PositionItemProgress
            .Where(p => p.PlanPositionId == positionId && p.ItemIndex < poolSize)
            .ToDictionaryAsync(p => p.ItemIndex);
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
            Results = order.Select(i => new TestItemResult { ContentId = pos.ExerciseId, ItemIndex = i, StageValue = stage }).ToList(),
        };
        db.TestAttempts.Add(attempt);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { planId, positionId, attemptId = attempt.Id },
            new AttemptResponse(attempt.Id, planId, positionId, day, stage, attempt.TotalItems));
    }

    /// <summary>Die nächste Prüfungsfrage (oder <c>Done</c>), server-geführt über den Attempt-Cursor – ohne Lösung.</summary>
    public record TestNextResponse(TestItem? Item, bool Done, int Cursor, int Total);

    /// <summary>
    /// Liefert die aktuelle Prüfungsfrage an der Cursor-Position (One-at-a-time, kein Zurück). Seit dem Start
    /// entfernte Items werden übersprungen. Am Ende der Reihenfolge kommt <see cref="TestNextResponse.Done"/>.
    /// </summary>
    [HttpGet("{attemptId:int}/next")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TestNextResponse>> Next(int planId, int positionId, int attemptId)
    {
        var attempt = await LoadAttempt(planId, positionId, attemptId);
        if (attempt is null) return NotFound();
        if (attempt.CompletedAt is not null)
            return new TestNextResponse(null, true, attempt.Cursor, attempt.TotalItems);
        var plan = (await GetPlan(planId))!;
        if (User.IsStudent() && !PositionPlayService.PlanPlayableForChild(plan, DateOnly.FromDateTime(DateTime.UtcNow)))
            return this.ProblemWithCode(ApiErrors.PlanInactive, "This study plan is not currently active. Ask your parent.");
        var pos = await GetPosition(planId, positionId);
        if (pos?.Exercise is null) return NotFound();

        var items = await play.ItemsOfAsync(pos);
        var typed = PositionPlayService.IsTypedStage(pos.Exercise.Type, attempt.StageValue);
        var cursor = PositionPlayService.SkipRemoved(attempt.Order, attempt.Cursor, items.Count);
        if (cursor != attempt.Cursor) { attempt.Cursor = cursor; await db.SaveChangesAsync(); }
        if (cursor >= attempt.Order.Count) return new TestNextResponse(null, true, cursor, attempt.TotalItems);

        var item = ToItem(items, items[attempt.Order[cursor]], pos.Exercise.Type, attempt.StageValue, typed);
        return new TestNextResponse(item, false, cursor, attempt.TotalItems);
    }

    /// <summary>Bestätigung einer abgegebenen Prüfungsantwort – bewusst OHNE Korrektheit (Feedback erst beim Abschluss).</summary>
    public record AnswerAck(bool Done, int Cursor, int Total);

    /// <summary>
    /// Nimmt die Antwort zur aktuellen Prüfungsfrage entgegen, bewertet sie serverseitig (und protokolliert
    /// den plan-übergreifenden Item-Fortschritt), gibt die Korrektheit aber NICHT zurück (echte Klausur:
    /// Feedback erst bei <see cref="Submit"/>). Adressiert wird stets die Cursor-Frage – der Client kann die
    /// Reihenfolge nicht umgehen. Danach rückt der Cursor eine Frage weiter.
    /// </summary>
    [HttpPost("{attemptId:int}/answer")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnswerAck>> Answer(int planId, int positionId, int attemptId, AnswerDto dto)
    {
        var attempt = await LoadAttempt(planId, positionId, attemptId);
        if (attempt is null) return NotFound();
        if (attempt.CompletedAt is not null) return this.ProblemWithCode(ApiErrors.TestAlreadySubmitted, "The test has already been submitted.");
        var plan = (await GetPlan(planId))!;
        if (User.IsStudent() && !PositionPlayService.PlanPlayableForChild(plan, DateOnly.FromDateTime(DateTime.UtcNow)))
            return this.ProblemWithCode(ApiErrors.PlanInactive, "This study plan is not currently active. Ask your parent.");
        var pos = await GetPosition(planId, positionId);
        if (pos?.Exercise is null) return NotFound();

        var items = await play.ItemsOfAsync(pos);
        var typed = PositionPlayService.IsTypedStage(pos.Exercise.Type, attempt.StageValue);
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
        await db.SaveChangesAsync();
        return new AnswerAck(attempt.Cursor >= attempt.Order.Count, attempt.Cursor, attempt.TotalItems);
    }

    public record ItemResultDto(int ItemIndex, string? GivenAnswer, bool WasCorrect, int HintsUsed);
    public record AttemptDetail(int Id, int PlanId, int PositionId, DateOnly Day, int Stage, DateTime StartedAt,
        DateTime? CompletedAt, int TotalItems, int CorrectItems, int ScorePercent, bool Passed,
        IReadOnlyList<ItemResultDto> Results);

    /// <summary>Ein Testversuch samt Einzelergebnissen.</summary>
    [HttpGet("{attemptId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttemptDetail>> Get(int planId, int positionId, int attemptId)
    {
        var a = await LoadAttempt(planId, positionId, attemptId);
        if (a is null) return NotFound();
        return new AttemptDetail(a.Id, a.StudyPlanId, positionId, a.Day, a.StageValue, a.StartedAt, a.CompletedAt,
            a.TotalItems, a.CorrectItems, a.ScorePercent, a.Passed,
            a.Results.OrderBy(r => r.ItemIndex).Select(r => new ItemResultDto(r.ItemIndex ?? 0, r.GivenAnswer, r.WasCorrect, r.HintsUsed)).ToList());
    }

    public record AnswerDto(int ItemIndex, string? GivenAnswer, bool? WasKnown);
    public record SubmitDto(List<AnswerDto>? Answers);
    public record ItemOutcome(int ItemIndex, string Prompt, string Expected, string? GivenAnswer, bool WasCorrect);
    public record SubmitResponse(int AttemptId, int Stage, int TotalItems, int CorrectItems,
        int ScorePercent, bool Passed, int PassPercent, IReadOnlyList<ItemOutcome> Items);

    /// <summary>
    /// Schließt den Versuch ab und liefert das Ergebnis (inkl. Lösungen). Im Klausur-Modus wurden die Antworten
    /// bereits schrittweise über <see cref="Answer"/> bewertet; hier wird nur aggregiert. Wird ausnahmsweise ein
    /// <paramref name="dto"/> mit Antworten übergeben (Bulk-Abgabe), werden diese noch serverseitig bewertet.
    /// </summary>
    [HttpPost("{attemptId:int}/submit")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubmitResponse>> Submit(int planId, int positionId, int attemptId, SubmitDto dto)
    {
        var attempt = await LoadAttempt(planId, positionId, attemptId);
        if (attempt is null) return NotFound();
        if (attempt.CompletedAt is not null) return this.ProblemWithCode(ApiErrors.TestAlreadySubmitted, "The test has already been submitted.");
        var plan = (await GetPlan(planId))!;
        // Anti-Schummel: einen inzwischen deaktivierten oder abgelaufenen Plan darf der Sohn auch nicht über
        // einen offenen Testversuch abschließen und bepunkten (der Vater bleibt ausgenommen).
        if (User.IsStudent() && !PositionPlayService.PlanPlayableForChild(plan, DateOnly.FromDateTime(DateTime.UtcNow)))
            return this.ProblemWithCode(ApiErrors.PlanInactive, "This study plan is not currently active. Ask your parent.");
        var pos = await GetPosition(planId, positionId);
        if (pos?.Exercise is null) return NotFound();

        var items = await play.ItemsOfAsync(pos);
        var typed = PositionPlayService.IsTypedStage(pos.Exercise.Type, attempt.StageValue);

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
                typed ? r.GivenAnswer : null, ItemReviewSource.Test, positionId, attempt.Day, countsForMastery: true);
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
        await db.SaveChangesAsync();

        // Ziel-Punkte der Position (idempotent) VOR der Gamification buchen, damit münz-basierte
        // Missionen die frische Gutschrift bereits sehen.
        await progress.EvaluateAndAwardAsync(plan, attempt.Day);
        // Metrik-basierte Missionen/Auszeichnungen (z. B. „Tests bestanden") auch am Test-Abschluss auswerten.
        await gamification.EvaluateAndAwardAsync(plan.ChildId, attempt.Day);

        return new SubmitResponse(attempt.Id, attempt.StageValue, attempt.TotalItems, attempt.CorrectItems,
            attempt.ScorePercent, attempt.Passed, passPercent, outcomes);
    }
}
