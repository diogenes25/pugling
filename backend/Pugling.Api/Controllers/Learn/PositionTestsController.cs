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
/// Abschlusstest einer einzelnen Lehrplan-Position (neues Modell): prüft die Inhalte EINER Übung.
/// Inhalt kommt aus der Übungs-Config (<see cref="ExerciseContentProvider"/>), bewertet wird typ-neutral
/// gegen die Item-Lösung. Bestehen misst sich an <see cref="PlanPosition.GoalThreshold"/> (Standard 80 %).
/// Die Punkte fürs Bestehen (per-Position-Ziel) folgen in der Ziel-/Punkte-Engine (Etappe 4); hier zählt
/// der Versuch bereits für metrik-basierte Missionen (z. B. „Tests bestanden").
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.V1 + "/study-plans/{planId:int}/positions/{positionId:int}/tests")]
[Tags("Study – Position Tests")]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(PlanOwnershipFilter))]
public class PositionTestsController(PuglingDbContext db, PositionPlayService play,
    PositionProgressService progress, GamificationService gamification, AnswerGrader grader) : ControllerBase
{
    /// <summary>Standard-Bestehensgrenze, wenn die Position keine eigene Schwelle setzt.</summary>
    private const int DefaultPassPercent = 80;

    public record TestItem(int ItemIndex, string Prompt, int Stage, string? Reveal, int? AnswerLength, string? Hint,
        IReadOnlyList<string>? Choices, string? AudioUrl);
    public record AttemptResponse(int AttemptId, int PlanId, int PositionId, DateOnly Day, int Stage,
        int TotalItems, IReadOnlyList<TestItem> Items);

    private Task<StudyPlan?> GetPlan(int planId) => db.StudyPlans.FirstOrDefaultAsync(p => p.Id == planId);

    private Task<PlanPosition?> GetPosition(int planId, int positionId) =>
        db.PlanPositions.Include(p => p.Exercise)
            .FirstOrDefaultAsync(p => p.Id == positionId && p.StudyPlanId == planId);

    private Task<TestAttempt?> LoadAttempt(int planId, int positionId, int attemptId) =>
        db.TestAttempts.Include(t => t.Results)
            .FirstOrDefaultAsync(t => t.Id == attemptId && t.StudyPlanId == planId && t.PlanPositionId == positionId);

    private static TestItem ToItem(ContentItem item, ExerciseType type, int stage, bool typed, IReadOnlyList<string>? choices)
    {
        var isLetterBoxes = type == ExerciseType.Vocabulary && (TestStage)stage == TestStage.LetterBoxes;
        // Hör-Stufe: die Vokabel wird vorgelesen – die Audioquelle mitgeben, damit der Client sie abspielt
        // (und den Wort-Text ausblendet). Andere Stufen erhalten keine Audioquelle.
        var isAudio = type == ExerciseType.Vocabulary && (TestStage)stage == TestStage.Audio;
        return new TestItem(item.Index, item.Prompt, stage,
            typed ? null : item.Answer, // Anzeige-/Selbsteinschätzung deckt die Lösung auf, getippt nicht.
            isLetterBoxes ? item.Answer.Length : null,
            typed ? item.Hint : null,
            choices, // nur bei Multiple-Choice gesetzt (Lösung + Ablenker)
            isAudio ? item.AudioUrl : null);
    }

    public record StartDto(int? Stage, DateOnly? Day);

    /// <summary>Startet einen Testversuch für die Position und liefert die zu lösenden Aufgaben (ohne Lösung).</summary>
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
        if (dto.Day is { } dd && dd != today && !User.IsFather()) return Forbid();
        // Anti-Schummel: der Sohn darf nur seinen aktiven, laufenden Plan testen (siehe Übungs-Start).
        if (User.IsChild() && !PositionPlayService.PlanPlayableForChild(plan, today))
            return this.ProblemWithCode(ApiErrors.PlanInactive, "This study plan is not currently active. Ask your parent.");
        var day = dto.Day ?? today;
        // Stufe: nur der Vater darf sie frei wählen; für den Sohn gilt die Fahrplan-/Positions-Stufe des Tages.
        var stage = User.IsFather() && dto.Stage is not null ? dto.Stage.Value : PositionPlayService.StageForDay(pos, plan, day);
        var typed = PositionPlayService.IsTypedStage(pos.Exercise.Type, stage);

        // Der Test ist Standortbestimmung: bereits eingeführte Inhalte prüfen, sonst den gesamten Pool
        // (sperrt nicht, wenn per Üben noch nichts „fällig" ist).
        var introduced = await db.PositionItemProgress
            .Where(p => p.PlanPositionId == positionId && p.IntroducedAt != null && p.ItemIndex < poolSize)
            .Select(p => p.ItemIndex).OrderBy(i => i).ToListAsync();
        var pool = introduced.Count > 0 ? introduced : Enumerable.Range(0, poolSize).ToList();

        var attempt = new TestAttempt
        {
            StudyPlanId = planId,
            PlanPositionId = positionId,
            Day = day,
            StageValue = stage,
            Graded = typed,
            TotalItems = pool.Count,
            Results = pool.Select(i => new TestItemResult { ContentId = pos.ExerciseId, ItemIndex = i, StageValue = stage }).ToList(),
        };
        db.TestAttempts.Add(attempt);
        await db.SaveChangesAsync();

        var presented = pool.Select(i =>
            ToItem(items[i], pos.Exercise.Type, stage, typed,
                PositionPlayService.ChoicesFor(items, items[i], pos.Exercise.Type, stage))).ToList();
        return CreatedAtAction(nameof(Get), new { planId, positionId, attemptId = attempt.Id },
            new AttemptResponse(attempt.Id, planId, positionId, day, stage, attempt.TotalItems, presented));
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
    public record SubmitDto(List<AnswerDto> Answers);
    public record ItemOutcome(int ItemIndex, string Prompt, string Expected, string? GivenAnswer, bool WasCorrect);
    public record SubmitResponse(int AttemptId, int Stage, int TotalItems, int CorrectItems,
        int ScorePercent, bool Passed, int PassPercent, IReadOnlyList<ItemOutcome> Items);

    /// <summary>Bewertet die Antworten serverseitig gegen die Item-Lösungen und schließt den Versuch ab.</summary>
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
        if (User.IsChild() && !PositionPlayService.PlanPlayableForChild(plan, DateOnly.FromDateTime(DateTime.UtcNow)))
            return this.ProblemWithCode(ApiErrors.PlanInactive, "This study plan is not currently active. Ask your parent.");
        var pos = await GetPosition(planId, positionId);
        if (pos?.Exercise is null) return NotFound();

        var items = await play.ItemsOfAsync(pos);
        var typed = PositionPlayService.IsTypedStage(pos.Exercise.Type, attempt.StageValue);
        var answers = dto.Answers.ToDictionary(a => a.ItemIndex);

        var outcomes = new List<ItemOutcome>();
        foreach (var result in attempt.Results)
        {
            var index = result.ItemIndex ?? 0;
            var item = items[index];
            answers.TryGetValue(index, out var answer);
            var correct = typed
                ? item.AcceptedAnswers.Any(a => grader.Matches(answer?.GivenAnswer, a))
                : answer?.WasKnown ?? false;

            result.GivenAnswer = answer?.GivenAnswer;
            result.WasCorrect = correct;
            outcomes.Add(new ItemOutcome(index, item.Prompt, item.Answer, answer?.GivenAnswer, correct));
        }

        var passPercent = pos.GoalThreshold is > 0 ? pos.GoalThreshold.Value : DefaultPassPercent;
        attempt.CorrectItems = attempt.Results.Count(r => r.WasCorrect);
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
