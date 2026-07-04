using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Models;
using Pugling.Api.Services;

namespace Pugling.Api.Controllers.Learn;

/// <summary>Mehrstufiger Abschlusstest (Lernkarten) für einen Vokabel-Lehrplan.</summary>
[ApiController]
[Route("api/study-plans/{planId:int}/tests")]
[Tags("Study – Vocabulary Tests")]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(PlanOwnershipFilter))]
public class TestsController(PuglingDbContext db, ScheduleService schedule, TestAttemptService attempts)
    : ControllerBase
{
    /// <summary>Was dem Kind je Vokabel angezeigt wird – ohne die Lösung zu verraten (außer Stufe 1).</summary>
    public record TestItem(int VocabularyId, string Prompt, TestStage Stage,
        string? Translation, int? AnswerLength, string? AudioUrl);
    public record AttemptResponse(int AttemptId, int PlanId, DateOnly Day, TestStage Stage,
        int TotalItems, IReadOnlyList<TestItem> Items);

    static TestItem ToItem(Vocabulary v, TestStage stage) => new(
        v.Id, v.Word, stage,
        Translation: stage == TestStage.ShowBoth ? v.Translation : null,
        AnswerLength: stage == TestStage.LetterBoxes ? v.Translation.Length : null,
        AudioUrl: stage == TestStage.Audio ? v.PronunciationAudioUrl : null);

    public record StartDto(TestStage? Stage, DateOnly? Day);

    /// <summary>Startet einen Testversuch und liefert die zu lösenden Karten (ohne Lösung).</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttemptResponse>> Start(int planId, StartDto dto)
    {
        var plan = await db.StudyPlans.Include(p => p.Items.OrderBy(i => i.Order)).ThenInclude(i => i.Vocabulary)
            .FirstOrDefaultAsync(p => p.Id == planId);
        if (plan is null) return NotFound("Lehrplan nicht gefunden.");
        if (plan.Method != LearningMethod.Vocabulary) return BadRequest("Dieser Lehrplan ist kein Vokabel-Plan.");
        if (plan.Items.Count == 0) return BadRequest("Lehrplan enthält keine Vokabeln.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (dto.Day is { } dd && dd != today && !User.IsFather()) return Forbid();
        var day = dto.Day ?? today;
        // Nur der Vater darf die Stufe frei wählen; für den Sohn gilt zwingend die Fahrplan-Stufe des Tages.
        var stage = (User.IsFather() && dto.Stage is not null)
            ? dto.Stage.Value : (TestStage)StudyProgressService.StageForDay(plan, day);

        // Stundenplan-gesteuerte Auswahl: neuer Stoff am Unterrichtstag, sonst Wiederholung.
        var selection = await schedule.SelectAsync(plan, day);
        var pool = selection.Items;
        if (pool.Count == 0)
        {
            // Der Tagestest ist eine Standortbestimmung, kein Leitner-Trigger: sind (z.B. nachdem die
            // Übung alle Karten hochgestuft hat) keine Karten mehr fällig, wird der bereits eingeführte
            // Stoff geprüft – sonst alle Plan-Inhalte. So sperrt Üben nicht den Test.
            var introduced = plan.Items.Where(i => i.IntroducedAt != null).OrderBy(i => i.Order).ToList();
            pool = introduced.Count > 0 ? introduced : plan.Items.OrderBy(i => i.Order).ToList();
        }

        var attempt = new TestAttempt
        {
            StudyPlanId = planId,
            Day = day,
            StageValue = (int)stage,
            Graded = StudyProgressService.IsTyped(stage),
            TotalItems = pool.Count,
            Results = pool.Select(i => new TestItemResult { ContentId = i.VocabularyId!.Value, StageValue = (int)stage }).ToList(),
        };
        db.TestAttempts.Add(attempt);
        await db.SaveChangesAsync();

        // Neuer Stoff gilt nach dem Unterrichtstags-Test als eingeführt.
        if (selection.Mode == LessonDayMode.New) await schedule.MarkIntroducedAsync(pool, day);

        var items = pool.Select(i => ToItem(i.Vocabulary!, stage)).ToList();
        return CreatedAtAction(nameof(Get), new { planId, attemptId = attempt.Id },
            new AttemptResponse(attempt.Id, planId, attempt.Day, stage, attempt.TotalItems, items));
    }

    public record ItemResultDto(int VocabularyId, TestStage Stage, string? GivenAnswer, bool WasCorrect, int HintsUsed);
    public record AttemptDetail(int Id, int PlanId, DateOnly Day, TestStage Stage, DateTime StartedAt,
        DateTime? CompletedAt, int TotalItems, int CorrectItems, int ScorePercent, bool Passed,
        IReadOnlyList<ItemResultDto> Results);

    /// <summary>Ein Testversuch samt Einzelergebnissen.</summary>
    [HttpGet("{attemptId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttemptDetail>> Get(int planId, int attemptId)
    {
        var a = await attempts.LoadAttemptAsync(planId, attemptId);
        if (a is null) return NotFound();
        return new AttemptDetail(a.Id, a.StudyPlanId, a.Day, (TestStage)a.StageValue, a.StartedAt, a.CompletedAt,
            a.TotalItems, a.CorrectItems, a.ScorePercent, a.Passed,
            a.Results.Select(r => new ItemResultDto(r.ContentId, (TestStage)r.StageValue, r.GivenAnswer, r.WasCorrect, r.HintsUsed)).ToList());
    }

    public record HintDto(int VocabularyId);
    public record HintResponse(int VocabularyId, int Index, char Letter, int HintsUsed);

    /// <summary>Stufe 3/4/5: deckt einen zufälligen Buchstaben der Lösung auf (zählt als genutzter Tipp).</summary>
    [HttpPost("{attemptId:int}/hint")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HintResponse>> Hint(int planId, int attemptId, HintDto dto)
    {
        var attempt = await attempts.LoadAttemptAsync(planId, attemptId);
        if (attempt is null) return NotFound();
        if (attempt.CompletedAt is not null) return BadRequest("Test ist bereits abgeschlossen.");
        if ((TestStage)attempt.StageValue is TestStage.ShowBoth or TestStage.SelfAssess)
            return BadRequest("Für diese Teststufe gibt es keine Buchstaben-Tipps.");

        var result = attempt.Results.FirstOrDefault(r => r.ContentId == dto.VocabularyId);
        if (result is null) return NotFound("Vokabel gehört nicht zum Test.");

        var translation = await db.Vocabulary.Where(v => v.Id == dto.VocabularyId)
            .Select(v => v.Translation).FirstAsync();
        var index = Random.Shared.Next(translation.Length);
        result.HintsUsed++;
        await db.SaveChangesAsync();
        return new HintResponse(dto.VocabularyId, index, translation[index], result.HintsUsed);
    }

    public record AnswerDto(int VocabularyId, string? GivenAnswer, bool? WasKnown);
    public record SubmitDto(List<AnswerDto> Answers);
    public record SubmitResponse(int AttemptId, TestStage Stage, int TotalItems, int CorrectItems,
        int ScorePercent, bool Passed, StudyProgressService.DayProgress DayProgress,
        IReadOnlyList<ItemOutcome> Items);
    public record ItemOutcome(int VocabularyId, string Word, string ExpectedTranslation,
        string? GivenAnswer, bool WasCorrect, int HintsUsed);

    /// <summary>Bewertet die Antworten, speichert das Ergebnis und vergibt fällige Punkte.</summary>
    [HttpPost("{attemptId:int}/submit")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubmitResponse>> Submit(int planId, int attemptId, SubmitDto dto)
    {
        var attempt = await attempts.LoadAttemptAsync(planId, attemptId);
        if (attempt is null) return NotFound();
        if (attempt.CompletedAt is not null) return BadRequest("Test wurde bereits eingereicht.");

        var plan = (await attempts.GetPlanAsync(planId))!;
        var stage = (TestStage)attempt.StageValue;
        var vocabIds = attempt.Results.Select(r => r.ContentId).ToList();
        var vocab = await db.Vocabulary.Where(v => vocabIds.Contains(v.Id)).ToDictionaryAsync(v => v.Id, v => v);
        var answers = dto.Answers.ToDictionary(a => a.VocabularyId);

        var outcomes = new List<ItemOutcome>();
        foreach (var result in attempt.Results)
        {
            answers.TryGetValue(result.ContentId, out var answer);
            var v = vocab[result.ContentId];

            bool correct = stage switch
            {
                TestStage.ShowBoth => true, // reine Anzeige-Stufe
                TestStage.SelfAssess => answer?.WasKnown ?? false,
                _ => answer is not null &&
                     StudyProgressService.Normalize(answer.GivenAnswer) == StudyProgressService.Normalize(v.Translation),
            };

            result.GivenAnswer = answer?.GivenAnswer;
            result.WasCorrect = correct;
            outcomes.Add(new ItemOutcome(v.Id, v.Word, v.Translation, answer?.GivenAnswer, correct, result.HintsUsed));
        }

        var dayProgress = await attempts.ScoreAndAwardAsync(attempt, plan);
        return new SubmitResponse(attempt.Id, stage, attempt.TotalItems, attempt.CorrectItems,
            attempt.ScorePercent, attempt.Passed, dayProgress, outcomes);
    }
}
