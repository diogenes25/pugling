using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Student;

/// <summary>
/// "Vokabel-Türme" (B-851 core): builds tower-game runs from the vocabulary of a plan position and grades every
/// assignment of a token to a tower.
/// <para>
/// The run is stateless. Tower and token ids are encrypted references (child, position, item, role, nonce): the client
/// cannot read which token belongs to which tower, cannot reuse an id for another child or position, and the same item
/// gets unrelated ids as tower and as token. Coins, the daily cap, heartbeat and the minute limit are not part of this
/// core (see the backlog story).
/// </para>
/// </summary>
public class TowerGameService(PuglingDbContext db, IDataProtectionProvider protection, ItemProgressService progress,
    LearningDay learningDay)
{
    /// <summary>Correct assignments that win a level (B-822).</summary>
    public const int Goal = 10;
    /// <summary>Towers on the field at the same time (B-853: one map, five towers).</summary>
    public const int TowerSlots = 5;
    /// <summary>A run needs at least as many words as towers on the field, or there is nothing to tell apart.</summary>
    public const int MinWords = TowerSlots;
    /// <summary>Upper bound of towers per run: the goal plus two spare. The client drops tokens at random (it cannot know the mapping), so a small pool keeps the share of tokens that fit a tower on the field high.</summary>
    private const int MaxTowers = 12;

    private readonly IDataProtector protector = protection.CreateProtector("Pugling.TowerGame.v1");

    private sealed record Word(int ItemId, int ExerciseId, int VocabularyId, string Foreign, string Meaning);

    /// <summary>The vocabulary positions of the child's playable plans with enough words for a run.</summary>
    public async Task<TowerGameOverview> OverviewAsync(int childId, CancellationToken ct)
    {
        var today = learningDay.Today;
        var rows = await db.PlanPositions.AsNoTracking()
            .Where(p => p.StudyPlan!.ChildId == childId && p.StudyPlan.Active
                && p.StudyPlan.StartDate <= today && today <= p.StudyPlan.EndDate)
            .Select(p => new
            {
                p.Id,
                PlanTitle = p.StudyPlan!.Title,
                SubjectName = p.StudyPlan.Subject != null ? p.StudyPlan.Subject.Name : null,
                ExerciseTitle = p.Exercise!.Title,
                Words = db.ExerciseItems.Count(i => i.ExerciseId == p.ExerciseId),
                p.StudyPlanId,
                p.Order,
            })
            .ToListAsync(ct);
        return new TowerGameOverview([.. rows.Where(r => r.Words >= MinWords)
            .OrderBy(r => r.SubjectName).ThenBy(r => r.StudyPlanId).ThenBy(r => r.Order)
            .Select(r => new TowerGamePosition(r.Id, r.PlanTitle, r.SubjectName, r.ExerciseTitle, r.Words))]);
    }

    /// <summary>
    /// A new run from the position's words, or <c>null</c> when the position is not a playable vocabulary position of
    /// the child. Words the child already met come first, weakest first (the game consolidates, B-822 decision 7); only
    /// when there are too few of them do unseen words fill the run.
    /// </summary>
    public async Task<TowerRunResponse?> StartAsync(int childId, int planPositionId, CancellationToken ct)
    {
        if (!await PlayableAsync(childId, planPositionId, ct)) return null;
        var words = await WordsAsync(planPositionId, ct);
        if (words.Count < MinWords) return null;

        var itemIds = words.Select(w => w.ItemId).ToList();
        var boxes = await db.ItemProgress.AsNoTracking()
            .Where(p => p.ChildId == childId && itemIds.Contains(p.ItemId))
            .ToDictionaryAsync(p => p.ItemId, p => p.Box, ct);

        var picked = words
            .OrderBy(w => boxes.ContainsKey(w.ItemId) ? 0 : 1)
            .ThenBy(w => boxes.GetValueOrDefault(w.ItemId))
            .ThenBy(_ => Random.Shared.Next())
            .Take(MaxTowers)
            .ToList();
        var towerOrder = picked.OrderBy(_ => Random.Shared.Next()).ToList();
        var tokenOrder = picked.OrderBy(_ => Random.Shared.Next()).ToList();

        return new TowerRunResponse(planPositionId, Goal, TowerSlots,
            [.. towerOrder.Select(w => new TowerTarget(Seal(childId, planPositionId, w.ItemId, 'T'), w.Meaning))],
            [.. tokenOrder.Select(w => new TowerToken(Seal(childId, planPositionId, w.ItemId, 'K'), w.Foreign))]);
    }

    /// <summary>
    /// Grades one assignment and records it as a <see cref="ItemReviewSource.Game"/> answer for the tower's word.
    /// <c>null</c> when an id is forged, belongs to another child or position, or the position is no longer playable.
    /// A token also counts as right when it carries the same word or the same meaning as the tower's own one - the child
    /// cannot tell two such words apart. The Leitner box rises only with the first hit per word and day (B-822 E3), a
    /// miss resets it like in practice.
    /// </summary>
    public async Task<TowerAssignResult?> AssignAsync(int childId, AssignTowerTokenDto dto, CancellationToken ct)
    {
        if (Open(dto.TowerId, childId, dto.PlanPositionId, 'T') is not { } towerItem
            || Open(dto.TokenId, childId, dto.PlanPositionId, 'K') is not { } tokenItem)
            return null;
        if (!await PlayableAsync(childId, dto.PlanPositionId, ct)) return null;

        var words = await WordsAsync(dto.PlanPositionId, ct);
        var tower = words.FirstOrDefault(w => w.ItemId == towerItem);
        var token = words.FirstOrDefault(w => w.ItemId == tokenItem);
        if (tower is null || token is null) return null;

        var correct = tower.ItemId == token.ItemId
            || string.Equals(tower.Foreign.Trim(), token.Foreign.Trim(), StringComparison.OrdinalIgnoreCase)
            || string.Equals(tower.Meaning.Trim(), token.Meaning.Trim(), StringComparison.OrdinalIgnoreCase);

        var today = learningDay.Today;
        var dayStart = learningDay.StartUtc(today);
        var countsForMastery = !correct || !await db.ItemReviewEvents.AnyAsync(e => e.ChildId == childId
            && e.ItemId == tower.ItemId && e.Source == ItemReviewSource.Game && e.WasCorrect && e.At >= dayStart, ct);

        var item = new ContentItem(0, tower.Meaning, tower.Foreign, [tower.Foreign], ItemId: tower.ItemId,
            VocabularyId: tower.VocabularyId);
        await progress.RecordAsync(childId, tower.ExerciseId, item, correct, 0, token.Foreign, ItemReviewSource.Game,
            dto.PlanPositionId, today, countsForMastery, ct);
        await db.SaveChangesAsync(ct);

        return new TowerAssignResult(correct, tower.Foreign);
    }

    private async Task<bool> PlayableAsync(int childId, int planPositionId, CancellationToken ct)
    {
        var today = learningDay.Today;
        return await db.PlanPositions.AsNoTracking().AnyAsync(p => p.Id == planPositionId
            && p.StudyPlan!.ChildId == childId && p.StudyPlan.Active
            && p.StudyPlan.StartDate <= today && today <= p.StudyPlan.EndDate, ct);
    }

    private async Task<List<Word>> WordsAsync(int planPositionId, CancellationToken ct)
    {
        var exerciseId = await db.PlanPositions.AsNoTracking().Where(p => p.Id == planPositionId)
            .Select(p => p.ExerciseId).FirstAsync(ct);
        var rows = await db.ExerciseItems.AsNoTracking()
            .Where(i => i.ExerciseId == exerciseId)
            .Join(db.Vocabularies.AsNoTracking(), i => i.VocabularyId, v => v.Id,
                (i, v) => new Word(i.Id, i.ExerciseId, v.Id, v.Word, v.Translation))
            .ToListAsync(ct);
        return [.. rows.Where(w => !string.IsNullOrWhiteSpace(w.Foreign) && !string.IsNullOrWhiteSpace(w.Meaning))];
    }

    private string Seal(int childId, int planPositionId, int itemId, char role)
    {
        // The nonce makes every id unique, so two runs never share ids and nothing can be matched by equality.
        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(6));
        return protector.Protect($"{childId}|{planPositionId}|{itemId}|{role}|{nonce}");
    }

    private int? Open(string id, int childId, int planPositionId, char role)
    {
        string plain;
        try
        {
            plain = protector.Unprotect(id);
        }
        catch (CryptographicException)
        {
            return null;
        }
        var parts = plain.Split('|');
        if (parts.Length != 5 || parts[0] != childId.ToString() || parts[1] != planPositionId.ToString()
            || parts[3] != role.ToString() || !int.TryParse(parts[2], out var itemId))
            return null;
        return itemId;
    }
}
