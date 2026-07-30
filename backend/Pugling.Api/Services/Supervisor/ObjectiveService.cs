using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Supervisor;

/// <summary>
/// Verwaltet <see cref="Objective"/>s (große Ziele) und ihre <see cref="KeyResult"/>s (Etappen) und projiziert
/// sie – live ausgewertet über den <see cref="ObjectiveEvaluationService"/> – in DTOs. Ein Objective ist ein
/// Container über KeyResults (wie ein <see cref="StudyPlan"/> über <see cref="PlanPosition"/>s); der Scope einer
/// Etappe ist nach Anlage fix (zum Umhängen neu anlegen). Validiert Scope (Katalog + Hierarchie) und Zielwerte je
/// Metrik. Die Belohnung selbst bucht der <see cref="ObjectiveRewardService"/> (hier nur das <c>Rewarded</c>-Flag).
/// </summary>
public class ObjectiveService(PuglingDbContext db, ObjectiveEvaluationService evaluation, ExerciseTypeRegistry registry)
{
    // Die Vertrags-Records (KeyResultResponse/ObjectiveResponse/Create-/Update-Requests) leben im
    // Vertrags-Projekt (Pugling.Contracts.Supervisor); die Result-Paare bleiben hier (tragen ApiError).

    /// <summary>Ergebnis mit optionalem Fehler-Code; beide <c>null</c> = nicht gefunden.</summary>
    public record ObjectiveResult(ObjectiveResponse? Value, ApiError? Error);
    /// <summary>Ergebnis mit optionalem Fehler-Code; beide <c>null</c> = nicht gefunden.</summary>
    public record KeyResultResult(KeyResultResponse? Value, ApiError? Error);

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private static string KrScope(KeyResult k) =>
        k.ExerciseId is not null ? "exercise" : k.ChapterId is not null ? "chapter" : "subject";

    private static KeyResultResponse MapKr(ObjectiveEvaluationService.KeyResultEval e)
    {
        var k = e.KeyResult;
        return new KeyResultResponse(k.Id, k.ObjectiveId, k.SubjectId, k.ChapterId, k.ExerciseId,
            KrScope(k), k.Metric.ToString(), k.TargetValue, e.Current, e.ProgressPercent, e.Status, k.Title);
    }

    private static ObjectiveResponse MapObjective(ObjectiveEvaluationService.ObjectiveEval e, bool rewarded)
    {
        var o = e.Objective;
        return new ObjectiveResponse(o.Id, o.ChildId, o.Title, o.Motivation, o.Kind.ToString(),
            o.Start, o.DueDate, o.Active, o.RewardOnComplete, o.RewardPerKeyResult,
            e.AchievedCount, e.TotalCount, e.ProgressPercent, e.Status, rewarded,
            e.KeyResults.Select(MapKr).ToList(), o.CreatedAt);
    }

    // Zielwert-/Scope-Regeln je Metrik; null = ok. ClassTestGrade: nur Fach-Scope, Note 1,0..6,0 (×10 = 10..60).
    private async Task<ApiError?> ValidateKeyResultAsync(int subjectId, int? chapterId, int? exerciseId,
        KeyResultMetric metric, int targetValue, CancellationToken ct)
    {
        if (!Enum.IsDefined(metric))
            return ApiErrors.ValidationError;

        if (metric == KeyResultMetric.ClassTestGrade)
        {
            if (chapterId is not null || exerciseId is not null)
                return ApiErrors.ValidationError; // Noten hängen am Fach, nicht an Kapitel/Übung
            if (targetValue is < 10 or > 60)
                return ApiErrors.ValidationError;
        }
        else
        {
            // MaxWeakItems ist ein „≤"-Ziel: 0 („gar keine schwachen Wörter") ist ein legitimer Zielwert.
            // Die „≥"-Metriken (AvgMastery/MasteredPercent) brauchen dagegen eine echte Untergrenze > 0 –
            // sonst wäre ein Zielwert 0 sofort vakuär erfüllt und löste eine Belohnung ohne Lernleistung aus.
            var (min, max) = metric == KeyResultMetric.MaxWeakItems ? (0, int.MaxValue) : (1, 100);
            if (targetValue < min || targetValue > max)
                return ApiErrors.ValidationError;
        }

        if (!await db.Subjects.AsNoTracking().AnyAsync(s => s.Id == subjectId, ct))
            return ApiErrors.InvalidReference;
        if (chapterId is { } chId && !await db.Chapters.AsNoTracking().AnyAsync(c => c.Id == chId && c.SubjectId == subjectId, ct))
            return ApiErrors.InvalidReference;
        if (exerciseId is { } exId)
        {
            if (chapterId is null)
                return ApiErrors.ValidationError; // Übungs-Scope setzt ein Kapitel voraus
            var type = await db.Exercises.AsNoTracking()
                .Where(e => e.Id == exId && e.ChapterId == chapterId).Select(e => e.Type).FirstOrDefaultAsync(ct);
            if (type is null || registry.ByKey(type)?.SupportsObjectives != true)
                return ApiErrors.InvalidReference; // nur item-getrackte Typen (heute Vokabeln)
        }
        return null;
    }

    private static ApiError? ValidateObjectiveFields(string title, ObjectiveKind kind, int rewardOnComplete, int rewardPerKeyResult) =>
        string.IsNullOrWhiteSpace(title) || !Enum.IsDefined(kind) || rewardOnComplete < 0 || rewardPerKeyResult < 0
            ? ApiErrors.ValidationError : null;

    // Ob der große Abschluss-Batzen bereits gebucht wurde (fürs Rewarded-Flag der Antwort). Die
    // Abschluss-Buchung ist die ohne Etappe – `PaidKeyResultId is null` ist ihr Diskriminator.
    private async Task<bool> IsRewardedAsync(int objectiveId, CancellationToken ct) =>
        await db.ObjectiveRewards.AsNoTracking()
            .AnyAsync(r => r.ObjectiveId == objectiveId && r.PaidKeyResultId == null, ct);

    /// <summary>Alle Objectives des Kindes, live ausgewertet; optional nach Status/Art gefiltert.</summary>
    public async Task<List<ObjectiveResponse>> ListAsync(int childId, string? status, ObjectiveKind? kind, CancellationToken ct = default)
    {
        var evals = await evaluation.EvaluateAllAsync(childId, Today, ct: ct);
        if (evals.Count == 0) return [];

        // Ids vorab materialisieren: eine stabile lokale Liste für die IN-Klausel (statt einer lazy
        // Projektion im Query-Ausdruck), die EF eindeutig parametrisiert.
        var objectiveIds = evals.Select(e => e.Objective.Id).ToList();
        var rewardedIds = (await db.ObjectiveRewards.AsNoTracking()
            .Where(r => r.PaidKeyResultId == null && objectiveIds.Contains(r.ObjectiveId))
            .Select(r => r.ObjectiveId).ToListAsync(ct)).ToHashSet();

        IEnumerable<ObjectiveResponse> mapped = evals.Select(e => MapObjective(e, rewardedIds.Contains(e.Objective.Id)));
        if (kind is { } k) mapped = mapped.Where(o => o.Kind.Equals(k.ToString(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(status))
            mapped = mapped.Where(o => o.Status.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase));
        return mapped.ToList();
    }

    /// <summary>Ein Objective live ausgewertet; <c>null</c>, wenn es (für dieses Kind) nicht existiert.</summary>
    public async Task<ObjectiveResponse?> GetAsync(int childId, int objectiveId, CancellationToken ct = default)
    {
        var e = await evaluation.EvaluateOneAsync(childId, objectiveId, Today, ct);
        return e is null ? null : MapObjective(e, await IsRewardedAsync(objectiveId, ct));
    }

    /// <summary>Legt ein Objective (optional mit inline-Etappen) an und liefert es ausgewertet zurück.</summary>
    public async Task<ObjectiveResult> CreateAsync(int childId, CreateObjectiveRequest req, CancellationToken ct = default)
    {
        if (ValidateObjectiveFields(req.Title, req.Kind, req.RewardOnComplete, req.RewardPerKeyResult) is { } fieldErr)
            return new ObjectiveResult(null, fieldErr);

        var keyResults = new List<KeyResult>();
        foreach (var kr in req.KeyResults ?? [])
        {
            if (await ValidateKeyResultAsync(kr.SubjectId, kr.ChapterId, kr.ExerciseId, kr.Metric, kr.TargetValue, ct) is { } err)
                return new ObjectiveResult(null, err);
            keyResults.Add(new KeyResult
            {
                SubjectId = kr.SubjectId,
                ChapterId = kr.ChapterId,
                ExerciseId = kr.ExerciseId,
                Metric = kr.Metric,
                TargetValue = kr.TargetValue,
                Title = string.IsNullOrWhiteSpace(kr.Title) ? null : kr.Title.Trim(),
            });
        }

        var objective = new Objective
        {
            ChildId = childId,
            Title = req.Title.Trim(),
            Motivation = string.IsNullOrWhiteSpace(req.Motivation) ? null : req.Motivation.Trim(),
            Kind = req.Kind,
            Start = req.Start,
            DueDate = req.DueDate,
            RewardOnComplete = req.RewardOnComplete,
            RewardPerKeyResult = req.RewardPerKeyResult,
            KeyResults = keyResults,
        };
        db.Objectives.Add(objective);
        await db.SaveChangesAsync(ct);

        var eval = await evaluation.EvaluateOneAsync(childId, objective.Id, Today, ct);
        return new ObjectiveResult(MapObjective(eval!, false), null);
    }

    /// <summary>Ändert Kopf-Felder eines Objectives (Etappen separat). Not-found = beide null.</summary>
    public async Task<ObjectiveResult> UpdateAsync(int childId, int objectiveId, UpdateObjectiveRequest req, CancellationToken ct = default)
    {
        var objective = await db.Objectives.FirstOrDefaultAsync(o => o.Id == objectiveId && o.ChildId == childId, ct);
        if (objective is null) return new ObjectiveResult(null, null);

        var title = req.Title is null ? objective.Title : req.Title.Trim();
        var kind = req.Kind ?? objective.Kind;
        var rewardOnComplete = req.RewardOnComplete ?? objective.RewardOnComplete;
        var rewardPerKeyResult = req.RewardPerKeyResult ?? objective.RewardPerKeyResult;
        if (ValidateObjectiveFields(title, kind, rewardOnComplete, rewardPerKeyResult) is { } err)
            return new ObjectiveResult(null, err);

        objective.Title = title;
        objective.Kind = kind;
        objective.RewardOnComplete = rewardOnComplete;
        objective.RewardPerKeyResult = rewardPerKeyResult;
        if (req.Motivation is not null) objective.Motivation = string.IsNullOrWhiteSpace(req.Motivation) ? null : req.Motivation.Trim();
        if (req.Start is not null) objective.Start = req.Start;
        if (req.DueDate is not null) objective.DueDate = req.DueDate;
        if (req.Active is { } active) objective.Active = active;
        await db.SaveChangesAsync(ct);

        var eval = await evaluation.EvaluateOneAsync(childId, objectiveId, Today, ct);
        return new ObjectiveResult(MapObjective(eval!, await IsRewardedAsync(objectiveId, ct)), null);
    }

    /// <summary>Löscht ein Objective (samt Etappen/Belohnungs-Log per Cascade). <c>false</c> = nicht gefunden.</summary>
    public async Task<bool> DeleteAsync(int childId, int objectiveId, CancellationToken ct = default)
    {
        var objective = await db.Objectives.FirstOrDefaultAsync(o => o.Id == objectiveId && o.ChildId == childId, ct);
        if (objective is null) return false;
        db.Objectives.Remove(objective);
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Fügt einem Objective eine Etappe hinzu und liefert sie ausgewertet zurück. Not-found = beide null.</summary>
    public async Task<KeyResultResult> AddKeyResultAsync(int childId, int objectiveId, CreateKeyResultRequest req, CancellationToken ct = default)
    {
        var objective = await db.Objectives.FirstOrDefaultAsync(o => o.Id == objectiveId && o.ChildId == childId, ct);
        if (objective is null) return new KeyResultResult(null, null);

        if (await ValidateKeyResultAsync(req.SubjectId, req.ChapterId, req.ExerciseId, req.Metric, req.TargetValue, ct) is { } err)
            return new KeyResultResult(null, err);

        var kr = new KeyResult
        {
            ObjectiveId = objectiveId,
            SubjectId = req.SubjectId,
            ChapterId = req.ChapterId,
            ExerciseId = req.ExerciseId,
            Metric = req.Metric,
            TargetValue = req.TargetValue,
            Title = string.IsNullOrWhiteSpace(req.Title) ? null : req.Title.Trim(),
        };
        db.KeyResults.Add(kr);
        await db.SaveChangesAsync(ct);
        return new KeyResultResult(await EvaluatedKrAsync(childId, objectiveId, kr.Id, ct), null);
    }

    /// <summary>Ändert Metrik/Zielwert/Titel einer Etappe (Scope bleibt fix). Not-found = beide null.</summary>
    public async Task<KeyResultResult> UpdateKeyResultAsync(int childId, int objectiveId, int keyResultId, UpdateKeyResultRequest req, CancellationToken ct = default)
    {
        var kr = await db.KeyResults.FirstOrDefaultAsync(
            k => k.Id == keyResultId && k.ObjectiveId == objectiveId && k.Objective!.ChildId == childId, ct);
        if (kr is null) return new KeyResultResult(null, null);

        var metric = req.Metric ?? kr.Metric;
        var target = req.TargetValue ?? kr.TargetValue;
        // Nur Metrik/Zielwert (bereichsabhängig) neu prüfen – der Scope bleibt unverändert gültig.
        if (await ValidateKeyResultAsync(kr.SubjectId, kr.ChapterId, kr.ExerciseId, metric, target, ct) is { } err)
            return new KeyResultResult(null, err);

        kr.Metric = metric;
        kr.TargetValue = target;
        if (req.Title is not null) kr.Title = string.IsNullOrWhiteSpace(req.Title) ? null : req.Title.Trim();
        await db.SaveChangesAsync(ct);
        return new KeyResultResult(await EvaluatedKrAsync(childId, objectiveId, keyResultId, ct), null);
    }

    /// <summary>Löscht eine Etappe eines Objectives. <c>false</c> = nicht gefunden.</summary>
    public async Task<bool> DeleteKeyResultAsync(int childId, int objectiveId, int keyResultId, CancellationToken ct = default)
    {
        var kr = await db.KeyResults.FirstOrDefaultAsync(
            k => k.Id == keyResultId && k.ObjectiveId == objectiveId && k.Objective!.ChildId == childId, ct);
        if (kr is null) return false;
        db.KeyResults.Remove(kr);
        await db.SaveChangesAsync(ct);
        return true;
    }

    // Wertet das Objective aus und pickt die eine Etappe heraus (für die KR-level Antworten).
    private async Task<KeyResultResponse?> EvaluatedKrAsync(int childId, int objectiveId, int keyResultId, CancellationToken ct)
    {
        var eval = await evaluation.EvaluateOneAsync(childId, objectiveId, Today, ct);
        var kr = eval?.KeyResults.FirstOrDefault(k => k.KeyResult.Id == keyResultId);
        return kr is null ? null : MapKr(kr);
    }
}
