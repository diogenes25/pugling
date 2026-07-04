using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Models;
using Pugling.Api.Services;

namespace Pugling.Api.Controllers.Learn;

/// <summary>Lehrpläne (verfahrensneutral: Vokabeltraining oder Lückentext) – vom Vater erstellt und ausgewertet.</summary>
[ApiController]
[Route("api/study-plans")]
[Tags("Study – Plans")]
[Produces("application/json")]
[Authorize]
public class StudyPlansController(PuglingDbContext db, StudyProgressService progress, ScheduleService schedule, AuthAccess access) : ControllerBase
{
    public record PlanItemResponse(int Id, int Order, LearningMethod Kind, int ContentId,
        string Ref, string Label, string Detail, DateOnly? IntroducedAt, int Box, DateOnly? DueOn);
    public record PlanResponse(int Id, int ChildId, LearningMethod Method, string Title, int? SubjectId,
        int NewItemsPerLesson, DateOnly StartDate, DateOnly EndDate, int DailyMinutesRequired, bool DailyTestRequired,
        int DailyTestPassPercent, int DefaultStage, bool RequireTypedTest,
        IReadOnlyList<StageStep>? StageSchedule, bool UseLeitner, int MaxBox, IReadOnlyList<int>? BoxIntervalDays,
        int PointsMinutesMet, int PointsTestPassed,
        int PointsDayCompleteBonus, bool Active, IReadOnlyList<PlanItemResponse> Items);

    static PlanItemResponse MapItem(StudyPlanItem i) => i.ClozeTextId is not null
        ? new(i.Id, i.Order, LearningMethod.Cloze, i.ContentId, i.ClozeText!.Key, i.ClozeText!.Title, i.ClozeText!.Text, i.IntroducedAt, i.Box, i.DueOn)
        : new(i.Id, i.Order, LearningMethod.Vocabulary, i.ContentId, i.Vocabulary!.Key, i.Vocabulary!.Word, i.Vocabulary!.Translation, i.IntroducedAt, i.Box, i.DueOn);

    static PlanResponse Map(StudyPlan p) => new(p.Id, p.ChildId, p.Method, p.Title, p.SubjectId, p.NewItemsPerLesson,
        p.StartDate, p.EndDate, p.DailyMinutesRequired, p.DailyTestRequired, p.DailyTestPassPercent, p.DefaultStage, p.RequireTypedTest,
        p.StageSchedule, p.UseLeitner, p.MaxBox, p.BoxIntervalDays, p.PointsMinutesMet, p.PointsTestPassed, p.PointsDayCompleteBonus, p.Active,
        p.Items.OrderBy(i => i.Order).Select(MapItem).ToList());

    IQueryable<StudyPlan> WithItems() =>
        db.StudyPlans.Include(p => p.Items.OrderBy(i => i.Order)).ThenInclude(i => i.Vocabulary)
            .Include(p => p.Items).ThenInclude(i => i.ClozeText);

    async Task<PlanResponse?> Project(int planId) =>
        await WithItems().FirstOrDefaultAsync(p => p.Id == planId) is { } p ? Map(p) : null;

    /// <summary>Lehrpläne auflisten. Sohn sieht nur eigene, Vater nur die seiner Kinder.</summary>
    [HttpGet]
    public async Task<IEnumerable<PlanResponse>> List([FromQuery] int? childId = null)
    {
        var query = WithItems();
        if (User.IsChild())
        {
            query = query.Where(p => p.ChildId == User.ChildId());
        }
        else
        {
            var fid = User.FatherId();
            query = query.Where(p => db.Children.Any(c => c.Id == p.ChildId && c.FatherId == fid));
            if (childId is not null) query = query.Where(p => p.ChildId == childId);
        }
        return (await query.OrderByDescending(p => p.CreatedAt).ToListAsync()).Select(Map);
    }

    /// <summary>Ein Lehrplan (nur eigener).</summary>
    [HttpGet("{planId:int}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlanResponse>> Get(int planId)
    {
        var plan = await WithItems().FirstOrDefaultAsync(p => p.Id == planId);
        if (plan is null) return NotFound();
        if (!await access.OwnsPlanAsync(User, plan)) return Forbid();
        return Map(plan);
    }

    public record CreatePlanDto(int ChildId, string Title, LearningMethod? Method, int? SubjectId,
        int? NewItemsPerLesson, DateOnly? StartDate, int DurationDays, List<string>? ContentKeys,
        int? DailyMinutesRequired, int? DailyTestPassPercent, int? DefaultStage, bool? RequireTypedTest,
        List<StageStep>? StageSchedule, bool? UseLeitner, int? MaxBox, List<int>? BoxIntervalDays,
        int? PointsMinutesMet, int? PointsTestPassed, int? PointsDayCompleteBonus);

    /// <summary>Erstellt einen Lehrplan (nur Vater, nur für eigene Kinder) und verknüpft die Inhalte.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Vater)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlanResponse>> Create(CreatePlanDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title)) return BadRequest("Title ist erforderlich.");
        // Eigentums-Prüfung zuerst: gibt für "existiert nicht" und "nicht mein Kind" einheitlich 404 zurück (kein Enumerieren fremder Kind-Ids).
        if (!await access.FatherOwnsChildAsync(User, dto.ChildId)) return NotFound("Kind nicht gefunden.");
        if (dto.SubjectId is { } sid && !await db.Subjects.AnyAsync(s => s.Id == sid)) return BadRequest("Fach nicht gefunden.");

        var method = dto.Method ?? LearningMethod.Vocabulary;
        var start = dto.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var duration = dto.DurationDays > 0 ? dto.DurationDays : 10;

        var plan = new StudyPlan
        {
            ChildId = dto.ChildId,
            Method = method,
            Title = dto.Title.Trim(),
            SubjectId = dto.SubjectId,
            NewItemsPerLesson = dto.NewItemsPerLesson is > 0 ? dto.NewItemsPerLesson.Value : 5,
            StartDate = start,
            EndDate = start.AddDays(duration - 1),
            DailyMinutesRequired = dto.DailyMinutesRequired ?? 20,
            DailyTestPassPercent = dto.DailyTestPassPercent ?? 80,
            DefaultStage = dto.DefaultStage ?? method switch
            {
                LearningMethod.Cloze => (int)ClozeStage.TranslationWordBank,
                LearningMethod.Matching => (int)MatchStage.Direct,
                _ => (int)TestStage.SelfAssess,
            },
            RequireTypedTest = dto.RequireTypedTest ?? false,
            StageSchedule = dto.StageSchedule,
            UseLeitner = dto.UseLeitner ?? false,
            MaxBox = dto.MaxBox is > 0 ? dto.MaxBox.Value : 5,
            BoxIntervalDays = dto.BoxIntervalDays,
            PointsMinutesMet = dto.PointsMinutesMet ?? 10,
            PointsTestPassed = dto.PointsTestPassed ?? 20,
            PointsDayCompleteBonus = dto.PointsDayCompleteBonus ?? 10,
        };

        if (dto.ContentKeys is { Count: > 0 })
        {
            var (ids, error) = await ResolveContentKeysAsync(method, dto.ContentKeys);
            if (error is not null) return BadRequest(error);
            var order = 0;
            foreach (var id in ids) plan.Items.Add(NewItem(method, id, order++));
        }

        db.StudyPlans.Add(plan);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { planId = plan.Id }, (await Project(plan.Id))!);
    }

    public record UpdatePlanDto(string? Title, int? SubjectId, int? NewItemsPerLesson, DateOnly? StartDate, DateOnly? EndDate,
        int? DailyMinutesRequired, bool? DailyTestRequired, int? DailyTestPassPercent,
        int? DefaultStage, bool? RequireTypedTest, List<StageStep>? StageSchedule,
        bool? UseLeitner, int? MaxBox, List<int>? BoxIntervalDays,
        int? PointsMinutesMet, int? PointsTestPassed, int? PointsDayCompleteBonus, bool? Active);

    /// <summary>Ändert einen Lehrplan (partiell, nur Vater/eigener). Das Lernverfahren ist nicht änderbar.</summary>
    [HttpPatch("{planId:int}")]
    [Authorize(Roles = Roles.Vater)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlanResponse>> Update(int planId, UpdatePlanDto dto)
    {
        var plan = await db.StudyPlans.FirstOrDefaultAsync(p => p.Id == planId);
        if (plan is null) return NotFound();
        if (!await access.OwnsPlanAsync(User, plan)) return Forbid();

        if (dto.Title is not null) plan.Title = dto.Title.Trim();
        if (dto.SubjectId is { } sid)
        {
            if (!await db.Subjects.AnyAsync(s => s.Id == sid)) return BadRequest("Fach nicht gefunden.");
            plan.SubjectId = sid;
        }
        if (dto.NewItemsPerLesson is > 0) plan.NewItemsPerLesson = dto.NewItemsPerLesson.Value;
        if (dto.StartDate is not null) plan.StartDate = dto.StartDate.Value;
        if (dto.EndDate is not null) plan.EndDate = dto.EndDate.Value;
        if (dto.DailyMinutesRequired is not null) plan.DailyMinutesRequired = dto.DailyMinutesRequired.Value;
        if (dto.DailyTestRequired is not null) plan.DailyTestRequired = dto.DailyTestRequired.Value;
        if (dto.DailyTestPassPercent is not null) plan.DailyTestPassPercent = dto.DailyTestPassPercent.Value;
        if (dto.DefaultStage is not null) plan.DefaultStage = dto.DefaultStage.Value;
        if (dto.RequireTypedTest is not null) plan.RequireTypedTest = dto.RequireTypedTest.Value;
        if (dto.StageSchedule is not null) plan.StageSchedule = dto.StageSchedule;
        if (dto.UseLeitner is not null) plan.UseLeitner = dto.UseLeitner.Value;
        if (dto.MaxBox is > 0) plan.MaxBox = dto.MaxBox.Value;
        if (dto.BoxIntervalDays is not null) plan.BoxIntervalDays = dto.BoxIntervalDays;
        if (dto.PointsMinutesMet is not null) plan.PointsMinutesMet = dto.PointsMinutesMet.Value;
        if (dto.PointsTestPassed is not null) plan.PointsTestPassed = dto.PointsTestPassed.Value;
        if (dto.PointsDayCompleteBonus is not null) plan.PointsDayCompleteBonus = dto.PointsDayCompleteBonus.Value;
        if (dto.Active is not null) plan.Active = dto.Active.Value;
        await db.SaveChangesAsync();
        return (await Project(planId))!;
    }

    public record AddItemDto(string ContentKey);

    /// <summary>Fügt dem Lehrplan einen Inhalt hinzu (nur Vater/eigener; Key je nach Verfahren).</summary>
    [HttpPost("{planId:int}/items")]
    [Authorize(Roles = Roles.Vater)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlanResponse>> AddItem(int planId, AddItemDto dto)
    {
        var plan = await db.StudyPlans.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == planId);
        if (plan is null) return NotFound();
        if (!await access.OwnsPlanAsync(User, plan)) return Forbid();

        var (ids, error) = await ResolveContentKeysAsync(plan.Method, new() { dto.ContentKey });
        if (error is not null) return BadRequest(error);
        var contentId = ids[0];
        if (plan.Items.Any(i => i.ContentId == contentId)) return BadRequest("Inhalt ist bereits im Plan.");

        var nextOrder = plan.Items.Count == 0 ? 0 : plan.Items.Max(i => i.Order) + 1;
        plan.Items.Add(NewItem(plan.Method, contentId, nextOrder));
        await db.SaveChangesAsync();
        return (await Project(planId))!;
    }

    /// <summary>Entfernt einen Inhalt aus dem Lehrplan (nur Vater/eigener).</summary>
    [HttpDelete("{planId:int}/items/{itemId:int}")]
    [Authorize(Roles = Roles.Vater)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveItem(int planId, int itemId)
    {
        var plan = await db.StudyPlans.FirstOrDefaultAsync(p => p.Id == planId);
        if (plan is null) return NotFound();
        if (!await access.OwnsPlanAsync(User, plan)) return Forbid();
        var item = await db.StudyPlanItems.FirstOrDefaultAsync(i => i.Id == itemId && i.StudyPlanId == planId);
        if (item is null) return NotFound();
        db.StudyPlanItems.Remove(item);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ---- Auswertung für den Vater ----

    public record ProgressResponse(int PlanId, DateOnly StartDate, DateOnly EndDate, int DaysComplete,
        int TotalDays, int TotalPoints, int CurrentStreak, IReadOnlyList<StudyProgressService.DayProgress> Days);

    /// <summary>Tag-für-Tag-Fortschritt über die gesamte Laufzeit (Zeit, Test, Punkte, offene Punkte).</summary>
    [HttpGet("{planId:int}/progress")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProgressResponse>> Progress(int planId)
    {
        var plan = await db.StudyPlans.FirstOrDefaultAsync(p => p.Id == planId);
        if (plan is null) return NotFound();
        if (!await access.OwnsPlanAsync(User, plan)) return Forbid();

        var days = new List<StudyProgressService.DayProgress>();
        for (var d = plan.StartDate; d <= plan.EndDate; d = d.AddDays(1))
            days.Add(await progress.ComputeDayAsync(plan, d));

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new ProgressResponse(planId, plan.StartDate, plan.EndDate,
            days.Count(x => x.DayComplete), days.Count, days.Sum(x => x.PointsAwarded),
            StreakUntil(days, today), days);
    }

    public record TodayResponse(int PlanId, LearningMethod Method, DateOnly Day, bool DutyDone,
        int RecommendedStage, LessonDayMode? Mode, bool IsPreparationDay, string ScheduleReason,
        int CurrentStreak, StudyProgressService.DayProgress Progress,
        IReadOnlyList<PlanItemResponse> DueItems, IReadOnlyList<ItemStat> WeakItems);

    /// <summary>Ein-Blick-Status für heute: Pflicht erfüllt? Modus (neu/Wiederholung) laut Stundenplan? fällige Inhalte? Streak? Schwache Inhalte?</summary>
    [HttpGet("{planId:int}/today")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TodayResponse>> Today(int planId)
    {
        var plan = await WithItems().FirstOrDefaultAsync(p => p.Id == planId);
        if (plan is null) return NotFound();
        if (!await access.OwnsPlanAsync(User, plan)) return Forbid();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var days = new List<StudyProgressService.DayProgress>();
        for (var d = plan.StartDate; d <= today && d <= plan.EndDate; d = d.AddDays(1))
            days.Add(await progress.ComputeDayAsync(plan, d));
        var todayProgress = days.LastOrDefault(x => x.Day == today) ?? await progress.ComputeDayAsync(plan, today);

        var selection = await schedule.SelectAsync(plan, today);
        var stats = await ComputeItemStatsAsync(plan);
        var weak = stats.Where(s => s.MasteryPercent < plan.DailyTestPassPercent)
            .OrderBy(s => s.MasteryPercent).ToList();

        return new TodayResponse(planId, plan.Method, today, todayProgress.DayComplete,
            StudyProgressService.StageForDay(plan, today), selection.Mode, selection.IsPreparationDay, selection.Reason,
            StreakUntil(days, today), todayProgress, selection.Items.Select(MapItem).ToList(), weak);
    }

    public record ItemStat(int ContentId, string Ref, string Label, string Detail,
        int TimesReviewed, int ReviewCorrect, int TimesTested, int TestCorrect, int MasteryPercent, DateTime? LastSeen,
        int Box, DateOnly? DueOn);
    public record TestHistoryEntry(int AttemptId, DateOnly Day, int Stage, bool Graded, int ScorePercent, bool Passed, DateTime? CompletedAt);
    public record RatingInfo(int ContentId, string Label, ExerciseFeedback Feedback, string? Comment, DateTime CreatedAt);
    public record ReportResponse(int PlanId, LearningMethod Method, IReadOnlyList<ItemStat> Items,
        IReadOnlyList<TestHistoryEntry> TestHistory, IReadOnlyList<RatingInfo> Ratings);

    /// <summary>Detaillierte Lern-Doku: was und wie hat das Kind gelernt, wie liefen die Tests, wie bewertet der Sohn die Inhalte.</summary>
    [HttpGet("{planId:int}/report")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReportResponse>> Report(int planId)
    {
        var plan = await WithItems().FirstOrDefaultAsync(p => p.Id == planId);
        if (plan is null) return NotFound();
        if (!await access.OwnsPlanAsync(User, plan)) return Forbid();

        var stats = await ComputeItemStatsAsync(plan);
        var history = await db.TestAttempts
            .Where(t => t.StudyPlanId == planId && t.CompletedAt != null)
            .OrderBy(t => t.CompletedAt)
            .Select(t => new TestHistoryEntry(t.Id, t.Day, t.StageValue, t.Graded, t.ScorePercent, t.Passed, t.CompletedAt))
            .ToListAsync();

        var labels = plan.Items.ToDictionary(i => i.ContentId, i => MapItem(i).Label);
        var ratings = (await db.ContentRatings
                .Where(r => r.StudyPlanId == planId)
                .OrderByDescending(r => r.CreatedAt).Take(200).ToListAsync())
            .Select(r => new RatingInfo(r.ContentId, labels.GetValueOrDefault(r.ContentId, "?"), r.Feedback, r.Comment, r.CreatedAt))
            .ToList();

        return new ReportResponse(planId, plan.Method, stats, history, ratings);
    }

    // ---- Helfer ----

    private static StudyPlanItem NewItem(LearningMethod method, int contentId, int order) => method switch
    {
        LearningMethod.Cloze => new StudyPlanItem { ClozeTextId = contentId, Order = order },
        _ => new StudyPlanItem { VocabularyId = contentId, Order = order },
    };

    private async Task<(List<int> ids, string? error)> ResolveContentKeysAsync(LearningMethod method, List<string> keys)
    {
        List<(int Id, string Key)> found = method == LearningMethod.Cloze
            ? (await db.ClozeTexts.Where(c => keys.Contains(c.Key)).Select(c => new { c.Id, c.Key }).ToListAsync())
                .Select(x => (x.Id, x.Key)).ToList()
            : (await db.Vocabulary.Where(v => keys.Contains(v.Key)).Select(v => new { v.Id, v.Key }).ToListAsync())
                .Select(x => (x.Id, x.Key)).ToList();

        var missing = keys.Except(found.Select(f => f.Key)).ToList();
        if (missing.Count > 0) return (new(), $"Unbekannte Keys: {string.Join(", ", missing)}");
        var byKey = found.ToDictionary(f => f.Key, f => f.Id);
        return (keys.Select(k => byKey[k]).ToList(), null);
    }

    private static int StreakUntil(IEnumerable<StudyProgressService.DayProgress> days, DateOnly today)
    {
        var streak = 0;
        foreach (var x in days.Where(x => x.Day <= today).Reverse())
        {
            if (x.DayComplete) streak++; else break;
        }
        return streak;
    }

    /// <summary>Aggregiert pro Plan-Inhalt: Wiederholungen, Test-Treffer und eine Beherrschungs-Quote (Mastery).</summary>
    private async Task<List<ItemStat>> ComputeItemStatsAsync(StudyPlan plan)
    {
        var reviews = await db.ReviewEvents
            .Where(r => r.PracticeSession!.StudyPlanId == plan.Id)
            .Select(r => new { r.ContentId, r.WasCorrect, r.At }).ToListAsync();
        var testResults = await db.TestItemResults
            .Where(r => r.TestAttempt!.StudyPlanId == plan.Id && r.TestAttempt!.CompletedAt != null)
            .Select(r => new { r.ContentId, r.WasCorrect, At = r.TestAttempt!.CompletedAt }).ToListAsync();

        return plan.Items.OrderBy(i => i.Order).Select(i =>
        {
            var item = MapItem(i);
            var rv = reviews.Where(r => r.ContentId == i.ContentId).ToList();
            var tr = testResults.Where(r => r.ContentId == i.ContentId).ToList();
            DateTime? last = rv.Select(x => (DateTime?)x.At).Concat(tr.Select(x => x.At)).Max();
            var mastery = tr.Count == 0 ? 0 : (int)Math.Round(100.0 * tr.Count(x => x.WasCorrect) / tr.Count);
            return new ItemStat(i.ContentId, item.Ref, item.Label, item.Detail,
                rv.Count, rv.Count(x => x.WasCorrect), tr.Count, tr.Count(x => x.WasCorrect), mastery, last, i.Box, i.DueOn);
        }).ToList();
    }
}
