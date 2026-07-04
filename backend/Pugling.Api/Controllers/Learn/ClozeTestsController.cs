using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Models;
using Pugling.Api.Services;

namespace Pugling.Api.Controllers.Learn;

/// <summary>Mehrstufiger Lückentext-Abschlusstest für einen Lückentext-Lehrplan.</summary>
[ApiController]
[Route("api/study-plans/{planId:int}/cloze-tests")]
[Tags("Study – Cloze Tests")]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(PlanOwnershipFilter))]
public class ClozeTestsController(PuglingDbContext db, ScheduleService schedule, TestAttemptService attempts)
    : ControllerBase
{
    static bool ShowTranslation(ClozeStage s) => s is ClozeStage.TranslationWordBank or ClozeStage.TranslationFreeText;
    static bool ShowWordBank(ClozeStage s) => s is ClozeStage.WordBank or ClozeStage.TranslationWordBank;

    static bool GapCorrect(Gap gap, string? given)
    {
        var g = StudyProgressService.Normalize(given);
        return g.Length > 0 && (g == StudyProgressService.Normalize(gap.Answer)
            || (gap.Alternatives?.Any(a => StudyProgressService.Normalize(a) == g) ?? false));
    }

    /// <summary>Was dem Kind je Lückentext angezeigt wird – ohne Lösungen zu verraten.</summary>
    public record ClozeTestText(int ClozeTextId, string Title, string Text, string? Translation,
        IReadOnlyList<int> GapIndexes, IReadOnlyList<string>? WordBank);
    public record AttemptResponse(int AttemptId, int PlanId, DateOnly Day, ClozeStage Stage,
        int TotalGaps, IReadOnlyList<ClozeTestText> Texts);

    public record StartDto(ClozeStage? Stage, DateOnly? Day);

    /// <summary>Startet einen Lückentext-Test und liefert die Texte mit Lücken (ohne Lösungen).</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttemptResponse>> Start(int planId, StartDto dto)
    {
        var plan = await db.StudyPlans.Include(p => p.Items.OrderBy(i => i.Order)).ThenInclude(i => i.ClozeText)
            .FirstOrDefaultAsync(p => p.Id == planId);
        if (plan is null) return NotFound("Lehrplan nicht gefunden.");
        if (plan.Method != LearningMethod.Cloze) return BadRequest("Dieser Lehrplan ist kein Lückentext-Plan.");
        if (plan.Items.Count == 0) return BadRequest("Lehrplan enthält keine Lückentexte.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (dto.Day is { } dd && dd != today && !User.IsFather()) return Forbid();
        var day = dto.Day ?? today;
        // Nur der Vater darf die Stufe frei wählen; für den Sohn gilt zwingend die Fahrplan-Stufe des Tages.
        var stage = (User.IsFather() && dto.Stage is not null)
            ? dto.Stage.Value : (ClozeStage)StudyProgressService.StageForDay(plan, day);

        // Stundenplan-gesteuerte Auswahl (neuer Stoff vs. Wiederholung).
        var selection = await schedule.SelectAsync(plan, day);
        var texts = selection.Items.Select(i => i.ClozeText!).ToList();
        if (texts.Count == 0) return BadRequest("Für heute stehen keine Lückentexte an.");
        if (selection.Mode == LessonDayMode.New) await schedule.MarkIntroducedAsync(selection.Items, day);

        var results = new List<TestItemResult>();
        foreach (var t in texts)
            foreach (var gap in t.Gaps)
                results.Add(new TestItemResult { ContentId = t.Id, GapIndex = gap.Index, StageValue = (int)stage });

        var attempt = new TestAttempt
        {
            StudyPlanId = planId,
            Day = day,
            StageValue = (int)stage,
            Graded = StudyProgressService.IsTyped(stage),
            TotalItems = results.Count,
            Results = results,
        };
        db.TestAttempts.Add(attempt);
        await db.SaveChangesAsync();

        var presented = texts.Select(t => new ClozeTestText(t.Id, t.Title, t.Text,
            ShowTranslation(stage) ? t.Translation : null,
            t.Gaps.Select(g => g.Index).ToList(),
            ShowWordBank(stage) ? t.WordBank : null)).ToList();

        return CreatedAtAction(nameof(Get), new { planId, attemptId = attempt.Id },
            new AttemptResponse(attempt.Id, planId, day, stage, results.Count, presented));
    }

    public record GapResultDto(int ClozeTextId, int GapIndex, string? GivenAnswer, bool WasCorrect, int HintsUsed);
    public record AttemptDetail(int Id, int PlanId, DateOnly Day, ClozeStage Stage, DateTime StartedAt,
        DateTime? CompletedAt, int TotalGaps, int CorrectGaps, int ScorePercent, bool Passed,
        IReadOnlyList<GapResultDto> Gaps);

    /// <summary>Ein Lückentext-Testversuch samt Einzelergebnissen je Lücke.</summary>
    [HttpGet("{attemptId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttemptDetail>> Get(int planId, int attemptId)
    {
        var a = await attempts.LoadAttemptAsync(planId, attemptId);
        if (a is null) return NotFound();
        return new AttemptDetail(a.Id, a.StudyPlanId, a.Day, (ClozeStage)a.StageValue, a.StartedAt, a.CompletedAt,
            a.TotalItems, a.CorrectItems, a.ScorePercent, a.Passed,
            a.Results.Select(r => new GapResultDto(r.ContentId, r.GapIndex ?? 0, r.GivenAnswer, r.WasCorrect, r.HintsUsed)).ToList());
    }

    public record HintDto(int ClozeTextId, int GapIndex);
    public record HintResponse(int ClozeTextId, int GapIndex, int Index, char Letter, int HintsUsed);

    /// <summary>Freitext-Stufen (3/4): deckt einen zufälligen Buchstaben der Lücken-Lösung auf.</summary>
    [HttpPost("{attemptId:int}/hint")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HintResponse>> Hint(int planId, int attemptId, HintDto dto)
    {
        var attempt = await attempts.LoadAttemptAsync(planId, attemptId);
        if (attempt is null) return NotFound();
        if (attempt.CompletedAt is not null) return BadRequest("Test ist bereits abgeschlossen.");
        if (!StudyProgressService.IsTyped((ClozeStage)attempt.StageValue))
            return BadRequest("Buchstaben-Tipps gibt es nur in den Freitext-Stufen.");

        var result = attempt.Results.FirstOrDefault(r => r.ContentId == dto.ClozeTextId && r.GapIndex == dto.GapIndex);
        if (result is null) return NotFound("Lücke gehört nicht zum Test.");

        var cloze = await db.ClozeTexts.FindAsync(dto.ClozeTextId);
        var answer = cloze?.Gaps.FirstOrDefault(g => g.Index == dto.GapIndex)?.Answer;
        if (string.IsNullOrEmpty(answer)) return BadRequest("Keine Lösung hinterlegt.");

        var index = Random.Shared.Next(answer.Length);
        result.HintsUsed++;
        await db.SaveChangesAsync();
        return new HintResponse(dto.ClozeTextId, dto.GapIndex, index, answer[index], result.HintsUsed);
    }

    public record GapAnswerDto(int ClozeTextId, int GapIndex, string? GivenAnswer);
    public record SubmitDto(List<GapAnswerDto> Answers);
    public record GapOutcome(int ClozeTextId, int GapIndex, string ExpectedAnswer, string? GivenAnswer, bool WasCorrect, int HintsUsed);
    public record SubmitResponse(int AttemptId, ClozeStage Stage, int TotalGaps, int CorrectGaps,
        int ScorePercent, bool Passed, StudyProgressService.DayProgress DayProgress, IReadOnlyList<GapOutcome> Gaps);

    /// <summary>Bewertet die Lücken, speichert das Ergebnis und vergibt fällige Punkte.</summary>
    [HttpPost("{attemptId:int}/submit")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubmitResponse>> Submit(int planId, int attemptId, SubmitDto dto)
    {
        var attempt = await attempts.LoadAttemptAsync(planId, attemptId);
        if (attempt is null) return NotFound();
        if (attempt.CompletedAt is not null) return BadRequest("Test wurde bereits eingereicht.");

        var plan = (await attempts.GetPlanAsync(planId))!;
        var textIds = attempt.Results.Select(r => r.ContentId).Distinct().ToList();
        var texts = await db.ClozeTexts.Where(c => textIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c);
        var answers = dto.Answers.ToDictionary(a => (a.ClozeTextId, a.GapIndex));

        var outcomes = new List<GapOutcome>();
        foreach (var result in attempt.Results)
        {
            // Text/Lücke könnten seit Teststart geändert worden sein -> defensiv, kein Crash.
            var gap = texts.TryGetValue(result.ContentId, out var text)
                ? text.Gaps.FirstOrDefault(g => g.Index == result.GapIndex) : null;
            answers.TryGetValue((result.ContentId, result.GapIndex ?? 0), out var answer);
            var correct = gap is not null && GapCorrect(gap, answer?.GivenAnswer);

            result.GivenAnswer = answer?.GivenAnswer;
            result.WasCorrect = correct;
            outcomes.Add(new GapOutcome(result.ContentId, result.GapIndex ?? 0, gap?.Answer ?? "", answer?.GivenAnswer, correct, result.HintsUsed));
        }

        var dayProgress = await attempts.ScoreAndAwardAsync(attempt, plan);
        return new SubmitResponse(attempt.Id, (ClozeStage)attempt.StageValue, attempt.TotalItems,
            attempt.CorrectItems, attempt.ScorePercent, attempt.Passed, dayProgress, outcomes);
    }
}
