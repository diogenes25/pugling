using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Student;

/// <summary>
/// "Vokabel-Türme" (B-851): builds tower-game runs from the vocabulary of a plan position, grades every assignment of a
/// token to a tower, counts the play time from heartbeats and pays a won level in coins under its own daily cap.
/// <para>
/// A run freezes its words as <see cref="TowerGameRunItem"/>s, so the server decides which towers a level holds and
/// how many were solved; the client's own count never pays. Tower and token ids are encrypted references (child, run,
/// item, role, nonce): the client cannot read which token belongs to which tower, cannot reuse an id for another child
/// or run, and the same item gets unrelated ids as tower and as token.
/// </para>
/// </summary>
public class TowerGameService(PuglingDbContext db, IDataProtectionProvider protection, ItemProgressService progress,
    LearningDay learningDay, IOptions<TowerGameOptions> options, TimeProvider time)
{
    /// <summary>Solved towers that win a level (B-822).</summary>
    public const int Goal = 10;
    /// <summary>Towers on the field at the same time (B-853: one map, five towers).</summary>
    public const int TowerSlots = 5;
    /// <summary>A run needs at least as many words as towers on the field, or there is nothing to tell apart.</summary>
    public const int MinWords = TowerSlots;
    /// <summary>Upper bound of towers per run: the goal plus two spare. The client drops tokens at random (it cannot know the mapping), so a small pool keeps the share of tokens that fit a tower on the field high.</summary>
    private const int MaxTowers = 12;

    private readonly IDataProtector protector = protection.CreateProtector("Pugling.TowerGame.v1");
    private readonly TowerGameOptions opts = options.Value;

    /// <summary>Outcome of a call that can fail with a business error: either the value or the error to answer with.</summary>
    public record Outcome<T>(T? Value, ApiError? Error = null, string? Detail = null) where T : class
    {
        /// <summary>A failed outcome answered with <paramref name="error"/>.</summary>
        public static Outcome<T> Fail(ApiError error, string detail) => new(null, error, detail);

        /// <summary>The run or an id does not belong to the child.</summary>
        public static Outcome<T> NotFound => Fail(ApiErrors.NotFound, "Tower-game run not found for this child.");

        /// <summary>Wraps a successful value.</summary>
        public static implicit operator Outcome<T>(T value) => new(value);
    }

    private sealed record Word(int ItemId, int ExerciseId, int VocabularyId, string Foreign, string Meaning);

    /// <summary>The vocabulary positions of the child's playable plans with enough words, plus today's time and coins.</summary>
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
            .Select(r => new TowerGamePosition(r.Id, r.PlanTitle, r.SubjectName, r.ExerciseTitle, r.Words))],
            await PlayedSecondsAsync(childId, today, ct), await LimitSecondsAsync(childId, ct),
            await CoinsTodayAsync(childId, today, ct), opts.DailyCoinCap);
    }

    /// <summary>
    /// A new run from the position's words. Fails with <c>not_found</c> when the position is not a playable vocabulary
    /// position of the child, and with <c>tower_game_time_limit</c> once today's minutes are used up. Words the child
    /// already met come first, weakest first (the game consolidates, B-822 decision 7); only when there are too few of
    /// them do unseen words fill the run.
    /// </summary>
    public async Task<Outcome<TowerRunResponse>> StartAsync(int childId, int planPositionId, CancellationToken ct)
    {
        if (!await PlayableAsync(childId, planPositionId, ct)) return NoPosition<TowerRunResponse>();
        var words = await WordsAsync(planPositionId, ct);
        if (words.Count < MinWords) return NoPosition<TowerRunResponse>();

        var today = learningDay.Today;
        var limit = await LimitSecondsAsync(childId, ct);
        if (await PlayedSecondsAsync(childId, today, ct) >= limit)
            return Outcome<TowerRunResponse>.Fail(ApiErrors.TowerGameTimeLimit,
                "Today's tower-game minutes are used up. Come back tomorrow.");

        var itemIds = words.Select(w => w.ItemId).ToList();
        var boxes = await db.ItemProgress.AsNoTracking()
            .Where(p => p.ChildId == childId && itemIds.Contains(p.ItemId))
            .ToDictionaryAsync(p => p.ItemId, p => p.Box, ct);

        var towerOrder = words
            .OrderBy(w => boxes.ContainsKey(w.ItemId) ? 0 : 1)
            .ThenBy(w => boxes.GetValueOrDefault(w.ItemId))
            .ThenBy(_ => Random.Shared.Next())
            .Take(MaxTowers)
            .OrderBy(_ => Random.Shared.Next())
            .ToList();
        var tokenOrder = towerOrder.OrderBy(_ => Random.Shared.Next()).ToList();

        var now = time.GetUtcNow().UtcDateTime;
        var run = new TowerGameRun
        {
            ChildId = childId,
            PlanPositionId = planPositionId,
            Day = today,
            StartedAt = now,
            LastBeatAt = now,
            Items = [.. towerOrder.Select((w, i) => new TowerGameRunItem { ExerciseItemId = w.ItemId, Order = i })],
        };
        db.TowerGameRuns.Add(run);
        await db.SaveChangesAsync(ct);

        return new TowerRunResponse(run.Id, planPositionId, Math.Min(Goal, towerOrder.Count), TowerSlots,
            [.. towerOrder.Select(w => new TowerTarget(Seal(childId, run.Id, w.ItemId, 'T'), w.Meaning))],
            [.. tokenOrder.Select(w => new TowerToken(Seal(childId, run.Id, w.ItemId, 'K'), w.Foreign))]);
    }

    /// <summary>
    /// Grades one assignment and records it as a <see cref="ItemReviewSource.Game"/> answer for the tower's word; a right
    /// one marks the tower solved. A token also counts as right when it carries the same word or the same meaning as the
    /// tower's own one - the child cannot tell two such words apart. The Leitner box rises only with the first hit per
    /// word and day (B-822 E3), a miss resets it like in practice. Fails with <c>not_found</c> for a forged or foreign id
    /// and with <c>tower_game_run_closed</c> once the run ended or its grace time is over.
    /// </summary>
    public async Task<Outcome<TowerAssignResult>> AssignAsync(int childId, AssignTowerTokenDto dto, CancellationToken ct)
    {
        if (Open(dto.TowerId, childId, dto.RunId, 'T') is not { } towerItem
            || Open(dto.TokenId, childId, dto.RunId, 'K') is not { } tokenItem)
            return Outcome<TowerAssignResult>.NotFound;
        var run = await db.TowerGameRuns.Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == dto.RunId && r.ChildId == childId, ct);
        if (run is null) return Outcome<TowerAssignResult>.NotFound;
        if (run.EndedAt is not null || await TimeUpAsync(childId, ct))
            return Outcome<TowerAssignResult>.Fail(ApiErrors.TowerGameRunClosed, "This level is over.");
        if (!await PlayableAsync(childId, run.PlanPositionId, ct)) return Outcome<TowerAssignResult>.NotFound;

        var words = await WordsAsync(run.PlanPositionId, ct);
        var tower = words.FirstOrDefault(w => w.ItemId == towerItem);
        var token = words.FirstOrDefault(w => w.ItemId == tokenItem);
        var runItem = run.Items.FirstOrDefault(i => i.ExerciseItemId == towerItem);
        if (tower is null || token is null || runItem is null) return Outcome<TowerAssignResult>.NotFound;

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
            run.PlanPositionId, today, countsForMastery, ct);
        if (correct) runItem.SolvedAt ??= time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        return new TowerAssignResult(correct, tower.Foreign);
    }

    /// <summary>
    /// Adds the time since the last beat to the run, clamped to <see cref="TowerGameOptions.MaxSecondsPerBeat"/>, and
    /// reports today's play time. An ended run adds nothing. <c>null</c> when the run is not the child's.
    /// </summary>
    public async Task<TowerHeartbeatResult?> HeartbeatAsync(int childId, int runId, CancellationToken ct)
    {
        var run = await db.TowerGameRuns.FirstOrDefaultAsync(r => r.Id == runId && r.ChildId == childId, ct);
        if (run is null) return null;
        if (run.EndedAt is null)
        {
            Beat(run);
            await db.SaveChangesAsync(ct);
        }
        var played = await PlayedSecondsAsync(childId, learningDay.Today, ct);
        var limit = await LimitSecondsAsync(childId, ct);
        return new TowerHeartbeatResult(played, limit, played >= limit + opts.GraceMinutes * 60);
    }

    /// <summary>
    /// Ends the run and pays a won level (enough solved towers by the server's count) in coins, cut to what is left of
    /// today's cap. Ending twice returns the stored result. The booking bumps the child's concurrency stamp like every
    /// wallet path, so two parallel ends cannot both pass the cap; the loser gets <c>concurrency_conflict</c>.
    /// </summary>
    public async Task<Outcome<TowerRunEndResult>> EndAsync(int childId, int runId, CancellationToken ct)
    {
        var run = await db.TowerGameRuns.Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == runId && r.ChildId == childId, ct);
        if (run is null) return Outcome<TowerRunEndResult>.NotFound;

        var today = learningDay.Today;
        var solved = run.Items.Count(i => i.SolvedAt is not null);
        var goal = Math.Min(Goal, run.Items.Count);
        if (run.EndedAt is not null)
            return new TowerRunEndResult(run.Won, solved, goal, run.CoinsAwarded, await CoinsTodayAsync(childId, today, ct),
                opts.DailyCoinCap);

        var child = await db.Children.FirstAsync(c => c.Id == childId, ct);
        var coinsBefore = await CoinsTodayAsync(childId, today, ct);
        var now = time.GetUtcNow().UtcDateTime;
        Beat(run);
        run.EndedAt = now;
        run.Won = solved >= goal;
        run.CoinsAwarded = run.Won ? Math.Clamp(opts.DailyCoinCap - coinsBefore, 0, opts.CoinsPerWin) : 0;
        if (run.CoinsAwarded > 0)
            db.ChildPointsEntries.Add(new ChildPointsEntry
            {
                ChildId = childId,
                Kind = PointKind.TowerGameCoins,
                Amount = run.CoinsAwarded,
                Reason = "Vokabel-Türme: Level geschafft",
                CreatedAt = now,
            });

        child.ConcurrencyStamp = Guid.NewGuid();
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            return Outcome<TowerRunEndResult>.Fail(ApiErrors.ConcurrencyConflict,
                "Another booking was saved at the same time. Please try again.");
        }
        return new TowerRunEndResult(run.Won, solved, goal, run.CoinsAwarded, coinsBefore + run.CoinsAwarded,
            opts.DailyCoinCap);
    }

    private void Beat(TowerGameRun run)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var seconds = (int)Math.Clamp((now - run.LastBeatAt).TotalSeconds, 0, opts.MaxSecondsPerBeat);
        run.PlayedSeconds += seconds;
        run.LastBeatAt = now;
    }

    private static Outcome<T> NoPosition<T>() where T : class =>
        Outcome<T>.Fail(ApiErrors.NotFound, "No playable vocabulary position with at least five words.");

    private async Task<bool> TimeUpAsync(int childId, CancellationToken ct) =>
        await PlayedSecondsAsync(childId, learningDay.Today, ct)
            >= await LimitSecondsAsync(childId, ct) + opts.GraceMinutes * 60;

    private Task<int> PlayedSecondsAsync(int childId, DateOnly day, CancellationToken ct) =>
        db.TowerGameRuns.Where(r => r.ChildId == childId && r.Day == day).SumAsync(r => r.PlayedSeconds, ct);

    private async Task<int> LimitSecondsAsync(int childId, CancellationToken ct)
    {
        var own = await db.Children.AsNoTracking().Where(c => c.Id == childId)
            .Select(c => c.TowerGameMinutesPerDay).FirstAsync(ct);
        return (own ?? opts.MinutesPerDay) * 60;
    }

    private async Task<int> CoinsTodayAsync(int childId, DateOnly today, CancellationToken ct)
    {
        var from = learningDay.StartUtc(today);
        var to = learningDay.StartUtc(today.AddDays(1));
        return await db.ChildPointsEntries.Where(e => e.ChildId == childId && e.Kind == PointKind.TowerGameCoins
            && e.CreatedAt >= from && e.CreatedAt < to).SumAsync(e => e.Amount, ct);
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

    private string Seal(int childId, int runId, int itemId, char role)
    {
        // The nonce makes every id unique, so nothing can be matched by equality between tower and token ids.
        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(6));
        return protector.Protect($"{childId}|{runId}|{itemId}|{role}|{nonce}");
    }

    private int? Open(string id, int childId, int runId, char role)
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
        if (parts.Length != 5 || parts[0] != childId.ToString() || parts[1] != runId.ToString()
            || parts[3] != role.ToString() || !int.TryParse(parts[2], out var itemId))
            return null;
        return itemId;
    }
}
