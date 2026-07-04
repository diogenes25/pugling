using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Models;
using Pugling.Api.Services;

namespace Pugling.Api.Controllers.Learn;

/// <summary>
/// Zuordnungs-Abschlusstest: links (Prompt) den richtigen Eintrag aus einem Auswahl-Pool zuordnen.
/// Nutzt den Vokabel-Store als Inhalt; Zuordnung ist objektiv (immer gewertet).
/// </summary>
[ApiController]
[Route("api/study-plans/{planId:int}/matching-tests")]
[Tags("Study – Matching Tests")]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(PlanOwnershipFilter))]
public class MatchingTestsController(PuglingDbContext db, ScheduleService schedule, TestAttemptService attempts)
    : ControllerBase
{
    static bool IsReverse(MatchStage s) => s is MatchStage.Reverse or MatchStage.ReverseDistractors;
    static bool HasDistractors(MatchStage s) => s is MatchStage.Distractors or MatchStage.ReverseDistractors;
    static string Left(Vocabulary v, MatchStage s) => IsReverse(s) ? v.Translation : v.Word;
    static string Right(Vocabulary v, MatchStage s) => IsReverse(s) ? v.Word : v.Translation;

    public record MatchLeft(int VocabularyId, string Prompt);
    public record AttemptResponse(int AttemptId, int PlanId, DateOnly Day, MatchStage Stage,
        int TotalItems, IReadOnlyList<MatchLeft> Items, IReadOnlyList<string> Options);

    public record StartDto(MatchStage? Stage, DateOnly? Day);

    /// <summary>Startet einen Zuordnungs-Test: liefert die Prompts und einen gemischten Auswahl-Pool.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttemptResponse>> Start(int planId, StartDto dto)
    {
        var plan = await db.StudyPlans.Include(p => p.Items.OrderBy(i => i.Order)).ThenInclude(i => i.Vocabulary)
            .FirstOrDefaultAsync(p => p.Id == planId);
        if (plan is null) return NotFound("Lehrplan nicht gefunden.");
        if (plan.Method != LearningMethod.Matching) return BadRequest("Dieser Lehrplan ist kein Zuordnungs-Plan.");
        if (plan.Items.Count == 0) return BadRequest("Lehrplan enthält keine Vokabeln.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (dto.Day is { } dd && dd != today && !User.IsFather()) return Forbid();
        var day = dto.Day ?? today;
        // Nur der Vater darf die Stufe frei wählen; für den Sohn gilt zwingend die Fahrplan-Stufe des Tages.
        var stage = (User.IsFather() && dto.Stage is not null)
            ? dto.Stage.Value : (MatchStage)StudyProgressService.StageForDay(plan, day);

        // Stundenplan-gesteuerte Auswahl (neuer Stoff vs. Wiederholung).
        var selection = await schedule.SelectAsync(plan, day);
        var vocab = selection.Items.Select(i => i.Vocabulary!).ToList();
        if (vocab.Count == 0) return BadRequest("Für heute stehen keine Vokabeln an.");
        if (selection.Mode == LessonDayMode.New) await schedule.MarkIntroducedAsync(selection.Items, day);

        var correct = vocab.Select(v => Right(v, stage)).ToList();
        var options = new List<string>(correct);
        if (HasDistractors(stage))
        {
            var planVocabIds = vocab.Select(v => v.Id).ToList();
            var candidates = await db.Vocabulary.Where(v => !planVocabIds.Contains(v.Id)).Take(50).ToListAsync();
            var distractors = candidates.Select(v => Right(v, stage))
                .Where(r => !correct.Contains(r)).Distinct()
                .OrderBy(_ => Random.Shared.Next()).Take(4);
            options.AddRange(distractors);
        }
        options = options.Distinct().OrderBy(_ => Random.Shared.Next()).ToList();

        var attempt = new TestAttempt
        {
            StudyPlanId = planId,
            Day = day,
            StageValue = (int)stage,
            Graded = true, // Zuordnung ist objektiv, keine Selbsteinschätzung
            TotalItems = vocab.Count,
            Results = vocab.Select(v => new TestItemResult { ContentId = v.Id, StageValue = (int)stage }).ToList(),
        };
        db.TestAttempts.Add(attempt);
        await db.SaveChangesAsync();

        var items = vocab.Select(v => new MatchLeft(v.Id, Left(v, stage))).ToList();
        return CreatedAtAction(nameof(Get), new { planId, attemptId = attempt.Id },
            new AttemptResponse(attempt.Id, planId, day, stage, vocab.Count, items, options));
    }

    public record ItemResultDto(int VocabularyId, MatchStage Stage, string? GivenAnswer, bool WasCorrect);
    public record AttemptDetail(int Id, int PlanId, DateOnly Day, MatchStage Stage, DateTime StartedAt,
        DateTime? CompletedAt, int TotalItems, int CorrectItems, int ScorePercent, bool Passed,
        IReadOnlyList<ItemResultDto> Results);

    /// <summary>Ein Zuordnungs-Testversuch samt Einzelergebnissen.</summary>
    [HttpGet("{attemptId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttemptDetail>> Get(int planId, int attemptId)
    {
        var a = await attempts.LoadAttemptAsync(planId, attemptId);
        if (a is null) return NotFound();
        return new AttemptDetail(a.Id, a.StudyPlanId, a.Day, (MatchStage)a.StageValue, a.StartedAt, a.CompletedAt,
            a.TotalItems, a.CorrectItems, a.ScorePercent, a.Passed,
            a.Results.Select(r => new ItemResultDto(r.ContentId, (MatchStage)r.StageValue, r.GivenAnswer, r.WasCorrect)).ToList());
    }

    public record MatchAnswerDto(int VocabularyId, string? ChosenAnswer);
    public record SubmitDto(List<MatchAnswerDto> Answers);
    public record MatchOutcome(int VocabularyId, string Prompt, string ExpectedAnswer, string? ChosenAnswer, bool WasCorrect);
    public record SubmitResponse(int AttemptId, MatchStage Stage, int TotalItems, int CorrectItems,
        int ScorePercent, bool Passed, StudyProgressService.DayProgress DayProgress, IReadOnlyList<MatchOutcome> Items);

    /// <summary>Bewertet die Zuordnungen, speichert das Ergebnis und vergibt fällige Punkte.</summary>
    [HttpPost("{attemptId:int}/submit")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubmitResponse>> Submit(int planId, int attemptId, SubmitDto dto)
    {
        var attempt = await attempts.LoadAttemptAsync(planId, attemptId);
        if (attempt is null) return NotFound();
        if (attempt.CompletedAt is not null) return BadRequest("Test wurde bereits eingereicht.");

        var plan = (await attempts.GetPlanAsync(planId))!;
        var stage = (MatchStage)attempt.StageValue;
        var vocabIds = attempt.Results.Select(r => r.ContentId).ToList();
        var vocab = await db.Vocabulary.Where(v => vocabIds.Contains(v.Id)).ToDictionaryAsync(v => v.Id, v => v);
        var answers = dto.Answers.ToDictionary(a => a.VocabularyId);

        var outcomes = new List<MatchOutcome>();
        foreach (var result in attempt.Results)
        {
            var v = vocab[result.ContentId];
            var expected = Right(v, stage);
            answers.TryGetValue(result.ContentId, out var answer);
            var correct = StudyProgressService.Normalize(answer?.ChosenAnswer) == StudyProgressService.Normalize(expected);

            result.GivenAnswer = answer?.ChosenAnswer;
            result.WasCorrect = correct;
            outcomes.Add(new MatchOutcome(v.Id, Left(v, stage), expected, answer?.ChosenAnswer, correct));
        }

        var dayProgress = await attempts.ScoreAndAwardAsync(attempt, plan);
        return new SubmitResponse(attempt.Id, stage, attempt.TotalItems, attempt.CorrectItems,
            attempt.ScorePercent, attempt.Passed, dayProgress, outcomes);
    }
}
