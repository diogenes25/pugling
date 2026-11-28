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
/// <para>
/// What a child collects in the game follows the same rule: trophies, the trophy path, the game chest and the game
/// streak are all counted from the server's own record of correct assignments. The one number taken from the app is
/// the wave a run reached, and it counts only as far as the run's play time covers it. Nothing here takes anything
/// away for a day without play - the duty and its penalty live in the learning part.
/// </para>
/// </summary>
public class TowerGameService(PuglingDbContext db, IDataProtectionProvider protection, ItemProgressService progress,
    LearningDay learningDay, IOptions<TowerGameOptions> options, TimeProvider time, GamificationService gamification,
    DutyCalendar calendar, WalletService wallet)
{
    /// <summary>The tower value of a new or weak word (<see cref="WordValue"/>) - what "a new word" means in the game.</summary>
    public const int NewWordValue = 3;
    /// <summary>How far back the game streak is counted; a longer streak shows as this many days.</summary>
    private const int StreakLookbackDays = 120;

    /// <summary>Solved towers that win a level (B-822).</summary>
    public const int Goal = 10;
    /// <summary>Towers on the field at the same time (B-853: one map, five towers).</summary>
    public const int TowerSlots = 5;
    /// <summary>A run needs at least as many words as towers on the field, or there is nothing to tell apart.</summary>
    public const int MinWords = TowerSlots;
    /// <summary>Upper bound of towers per run. A run is endless (waves), so it carries far more words than the goal.</summary>
    private const int MaxTowers = 40;
    /// <summary>
    /// Tokens are shuffled only within groups of this size along the tower order. The client cannot know which token
    /// fits which tower, so it drops from the front of the token list; the grouping keeps the share of dropped words
    /// that fit a tower on the field high without giving the pairing away.
    /// </summary>
    private const int TokenGroup = 6;

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
                && p.StudyPlan.StartDate <= today && (p.StudyPlan.EndDate == null || today <= p.StudyPlan.EndDate))
            .Select(p => new
            {
                p.Id,
                PlanTitle = p.StudyPlan!.Title,
                SubjectName = p.StudyPlan.Subject != null ? p.StudyPlan.Subject.Name : null,
                ExerciseTitle = p.Exercise!.Title,
                // The same words a run is built from (WordsAsync): an entry without word or meaning is no tower, and a
                // position listed by its raw item count would answer the start with 404.
                Words = db.ExerciseItems.Count(i => i.ExerciseId == p.ExerciseId
                    && db.Vocabularies.Any(v => v.Id == i.VocabularyId && v.Word.Trim() != "" && v.Translation.Trim() != "")),
                p.StudyPlanId,
                p.Order,
            })
            .ToListAsync(ct);
        return new TowerGameOverview([.. rows.Where(r => r.Words >= MinWords)
            .OrderBy(r => r.SubjectName).ThenBy(r => r.StudyPlanId).ThenBy(r => r.Order)
            .Select(r => new TowerGamePosition(r.Id, r.PlanTitle, r.SubjectName, r.ExerciseTitle, r.Words))],
            await PlayedSecondsAsync(childId, today, ct), await LimitSecondsAsync(childId, ct),
            await CoinsTodayAsync(childId, today, ct), opts.DailyCoinCap, await ProgressAsync(childId, today, ct));
    }

    /// <summary>Trophies, trophy path, game streak and today's game chest of the child - a pure read.</summary>
    private async Task<TowerGameProgress> ProgressAsync(int childId, DateOnly today, CancellationToken ct)
    {
        var paid = await db.TowerTrophyRewards.AsNoTracking().Where(r => r.ChildId == childId)
            .Select(r => r.Trophies).ToListAsync(ct);
        var perDay = await CorrectPerDayAsync(childId, today.AddDays(-StreakLookbackDays), today, ct);
        var wordsToday = perDay.GetValueOrDefault(today);
        return new TowerGameProgress(await TrophiesAsync(childId, ct),
            [.. opts.TrophyPath.OrderBy(s => s.Trophies).Select(s => new TowerTrophyStep(s.Trophies, s.Gems, paid.Contains(s.Trophies)))],
            Streak(perDay, await calendar.ForChildAsync(childId, ct), today, opts.StreakWords, StreakLookbackDays),
            wordsToday, opts.StreakWords, await ChestStateAsync(childId, today, wordsToday, ct));
    }

    /// <summary>
    /// Days in a row, back from <paramref name="today"/>, with at least <paramref name="needed"/> correct assignments.
    /// Today is still open: it counts once it is met and breaks nothing before. A day without duty (Sunday, a pause)
    /// counts when it is met and is stepped over when not, like in the learning streak.
    /// </summary>
    public static int Streak(IReadOnlyDictionary<DateOnly, int> correctPerDay, ChildDutyDays dutyDays, DateOnly today,
        int needed, int lookbackDays)
    {
        var streak = 0;
        for (var day = today; day > today.AddDays(-lookbackDays); day = day.AddDays(-1))
        {
            if (correctPerDay.GetValueOrDefault(day) >= needed)
            {
                streak++;
                continue;
            }
            if (day == today || !dutyDays.IsDutyDay(day)) continue;
            break;
        }
        return streak;
    }

    /// <summary>
    /// What a run earns in trophies: the value of every solved word plus one per cleared wave. The waves are capped
    /// twice: by the play time the server counted (a forged wave number buys nothing) and by the number of solved
    /// words (waves cleared without learning pay nothing).
    /// </summary>
    public static int RunTrophies(IEnumerable<int> solvedValues, int acceptedWave)
    {
        var values = solvedValues.ToList();
        return values.Sum() + Math.Min(Math.Max(0, acceptedWave - 1), values.Count);
    }

    /// <summary>The reported wave as far as the counted play time covers it: one wave per <see cref="TowerGameOptions.MinSecondsPerWave"/>, the running one included.</summary>
    public static int AcceptedWave(int reportedWave, int playedSeconds, int minSecondsPerWave) =>
        Math.Clamp(reportedWave, 0, 1 + playedSeconds / Math.Max(1, minSecondsPerWave));

    /// <summary>
    /// Opens today's game chest: full after <see cref="TowerGameOptions.ChestWords"/> correct assignments, once per
    /// day, paid in gems. Fails with <c>tower_game_chest_none</c> when it is not full or already opened - also for the
    /// loser of two parallel taps, which the unique index on (child, day) stops.
    /// </summary>
    public async Task<Outcome<TowerGameChestOpened>> OpenChestAsync(int childId, CancellationToken ct)
    {
        var today = learningDay.Today;
        var words = (await CorrectPerDayAsync(childId, today, today, ct)).GetValueOrDefault(today);
        if (!(await ChestStateAsync(childId, today, words, ct)).Ready) return NoChest();

        var child = await db.Children.FirstAsync(c => c.Id == childId, ct);
        var now = time.GetUtcNow().UtcDateTime;
        var gems = Random.Shared.Next(Math.Max(0, opts.ChestMinGems), Math.Max(opts.ChestMinGems, opts.ChestMaxGems) + 1);
        db.TowerGameChests.Add(new TowerGameChest { ChildId = childId, Day = today, Gems = gems, OpenedAt = now });
        if (gems > 0)
            db.ChildPointsEntries.Add(new ChildPointsEntry
            {
                ChildId = childId,
                Kind = PointKind.TowerChestGems,
                Amount = gems,
                Reason = "Vokabel-Türme: Spiel-Truhe",
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
            return Outcome<TowerGameChestOpened>.Fail(ApiErrors.ConcurrencyConflict,
                "Another booking was saved at the same time. Please try again.");
        }
        catch (DbUpdateException)
        {
            // Only the parallel tap is benign: the chest of today is there now. Anything else is a real error.
            db.ChangeTracker.Clear();
            if (!await db.TowerGameChests.AnyAsync(c => c.ChildId == childId && c.Day == today, ct)) throw;
            return NoChest();
        }
        return new TowerGameChestOpened(gems, new TowerGameChestState(words, opts.ChestWords, false, true));
    }

    /// <summary>The hero skins with their prices, what the child owns of them, and its gem balance - a pure read.</summary>
    public async Task<TowerSkinState> SkinsAsync(int childId, CancellationToken ct)
    {
        var owned = await db.Children.AsNoTracking().Where(c => c.Id == childId).Select(c => c.OwnedSkins).FirstAsync(ct);
        return new TowerSkinState(await wallet.GemsAsync(childId, ct),
            [.. opts.Skins.Select(s => new TowerSkinOffer(s.Id, s.Gems, s.Gems == 0 || owned.Contains(s.Id)))]);
    }

    /// <summary>
    /// Buys a hero skin for gems. Price and ownership are the server's: fails with <c>not_found</c> for an unknown id,
    /// <c>skin_already_unlocked</c> for a skin the child owns (a free one included) and <c>insufficient_gems</c> when
    /// the balance does not cover it. The debit bumps the child's concurrency stamp, so two parallel purchases cannot
    /// both pass the balance check; the loser gets <c>concurrency_conflict</c>.
    /// </summary>
    public async Task<Outcome<TowerSkinState>> BuySkinAsync(int childId, string skinId, CancellationToken ct)
    {
        var skin = opts.Skins.FirstOrDefault(s => s.Id == skinId);
        if (skin is null) return Outcome<TowerSkinState>.Fail(ApiErrors.NotFound, $"Unknown tower-game skin '{skinId}'.");
        var child = await db.Children.FirstAsync(c => c.Id == childId, ct);
        if (skin.Gems == 0 || child.OwnedSkins.Contains(skinId))
            return Outcome<TowerSkinState>.Fail(ApiErrors.SkinAlreadyUnlocked, "This skin is already unlocked.");
        var gems = await wallet.GemsAsync(childId, ct);
        if (gems < skin.Gems)
            return Outcome<TowerSkinState>.Fail(ApiErrors.InsufficientGems, $"Not enough gems: {gems}/{skin.Gems} for '{skinId}'.");

        db.ChildPointsEntries.Add(new ChildPointsEntry
        {
            ChildId = childId,
            Amount = -skin.Gems,
            Kind = PointKind.SkinPurchase,
            Reason = $"Vokabel-Türme: Skin freigeschaltet ({skinId})",
            CreatedAt = time.GetUtcNow().UtcDateTime,
        });
        // Reassigned, not mutated in place: the list is a JSON column.
        child.OwnedSkins = [.. child.OwnedSkins, skinId];
        child.ConcurrencyStamp = Guid.NewGuid();
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            return Outcome<TowerSkinState>.Fail(ApiErrors.ConcurrencyConflict,
                "Another booking was saved at the same time. Please try again.");
        }
        return await SkinsAsync(childId, ct);
    }

    private static Outcome<TowerGameChestOpened> NoChest() => Outcome<TowerGameChestOpened>.Fail(
        ApiErrors.TowerGameChestNone, "The game chest is not full yet, or today's chest is already opened.");

    private async Task<TowerGameChestState> ChestStateAsync(int childId, DateOnly today, int wordsToday, CancellationToken ct)
    {
        var opened = await db.TowerGameChests.AsNoTracking().AnyAsync(c => c.ChildId == childId && c.Day == today, ct);
        return new TowerGameChestState(Math.Min(wordsToday, opts.ChestWords), opts.ChestWords,
            !opened && wordsToday >= opts.ChestWords, opened);
    }

    /// <summary>Correct tower-game assignments per learning day within [<paramref name="from"/>, <paramref name="to"/>].</summary>
    private async Task<Dictionary<DateOnly, int>> CorrectPerDayAsync(int childId, DateOnly from, DateOnly to, CancellationToken ct) =>
        await db.ItemReviewEvents.AsNoTracking()
            .Where(e => e.ChildId == childId && e.Source == ItemReviewSource.Game && e.WasCorrect && e.Day >= from && e.Day <= to)
            .GroupBy(e => e.Day)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Day, g => g.Count, ct);

    private Task<int> TrophiesAsync(int childId, CancellationToken ct) =>
        db.TowerGameRuns.Where(r => r.ChildId == childId).SumAsync(r => r.Trophies, ct);

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
        var tokenOrder = towerOrder.Chunk(TokenGroup).SelectMany(g => g.OrderBy(_ => Random.Shared.Next())).ToList();

        var now = time.GetUtcNow().UtcDateTime;
        var run = new TowerGameRun
        {
            ChildId = childId,
            PlanPositionId = planPositionId,
            Day = today,
            StartedAt = now,
            LastBeatAt = now,
            Items = [.. towerOrder.Select((w, i) => new TowerGameRunItem
            {
                ExerciseItemId = w.ItemId,
                Order = i,
                Value = WordValue(boxes.TryGetValue(w.ItemId, out var box) ? box : null),
            })],
        };
        db.TowerGameRuns.Add(run);
        await db.SaveChangesAsync(ct);

        return new TowerRunResponse(run.Id, planPositionId, Math.Min(Goal, towerOrder.Count), TowerSlots,
            [.. towerOrder.Select(w => new TowerTarget(Seal(childId, run.Id, w.ItemId, 'T'), w.Meaning,
                WordValue(boxes.TryGetValue(w.ItemId, out var box) ? box : null)))],
            [.. tokenOrder.Select(w => new TowerToken(Seal(childId, run.Id, w.ItemId, 'K'), w.Foreign))]);
    }

    /// <summary>
    /// Grades one assignment and records it as a <see cref="ItemReviewSource.Game"/> answer for the tower's word; a right
    /// one marks the tower solved. A token also counts as right when it carries the same word or the same meaning as the
    /// tower's own one - the child cannot tell two such words apart. The Leitner box rises only with the first hit per
    /// word and day (B-822 E3), a miss resets it like in practice. Fails with <c>not_found</c> for a forged or foreign id
    /// and with <c>tower_game_run_closed</c> once the run ended, its day is over or its grace time is over. Every
    /// assignment also counts as play time, clamped like a heartbeat.
    /// </summary>
    public async Task<Outcome<TowerAssignResult>> AssignAsync(int childId, AssignTowerTokenDto dto, CancellationToken ct)
    {
        if (Open(dto.TowerId, childId, dto.RunId, 'T') is not { } towerItem
            || Open(dto.TokenId, childId, dto.RunId, 'K') is not { } tokenItem)
            return Outcome<TowerAssignResult>.NotFound;
        var run = await db.TowerGameRuns.Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == dto.RunId && r.ChildId == childId, ct);
        if (run is null) return Outcome<TowerAssignResult>.NotFound;
        // A run of an earlier day is over: its time counts against the day it started on, so playing it on would
        // run outside today's minute limit.
        if (run.EndedAt is not null || run.Day != learningDay.Today || await TimeUpAsync(childId, ct))
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
            run.PlanPositionId, today, countsForMastery, accentSlip: false,
            // B-732: a tower is a pick among the words on the board, never typed - no error kind.
            errorKind: null, ct);
        if (correct) runItem.SolvedAt ??= time.GetUtcNow().UtcDateTime;
        // Playing is play time, with or without heartbeats: a client that never beats must not escape the minute limit.
        Beat(run);
        await db.SaveChangesAsync(ct);

        return new TowerAssignResult(correct, tower.Foreign);
    }

    /// <summary>
    /// Adds the time since the last beat to the run, clamped to <see cref="TowerGameOptions.MaxSecondsPerBeat"/>, and
    /// reports today's play time. An ended run adds nothing; neither does a run of an earlier day, which is reported as
    /// time up. <c>null</c> when the run is not the child's.
    /// </summary>
    public async Task<TowerHeartbeatResult?> HeartbeatAsync(int childId, int runId, CancellationToken ct)
    {
        var run = await db.TowerGameRuns.FirstOrDefaultAsync(r => r.Id == runId && r.ChildId == childId, ct);
        if (run is null) return null;
        var today = learningDay.Today;
        // A run of an earlier day adds nothing and is reported as over (see AssignAsync).
        var stale = run.Day != today;
        if (run.EndedAt is null && !stale)
        {
            Beat(run);
            await db.SaveChangesAsync(ct);
        }
        var played = await PlayedSecondsAsync(childId, today, ct);
        var limit = await LimitSecondsAsync(childId, ct);
        return new TowerHeartbeatResult(played, limit, stale || played >= limit + opts.GraceMinutes * 60);
    }

    /// <summary>
    /// Ends the run and pays a won level (enough solved towers by the server's count) in coins, cut to what is left of
    /// today's cap. It also books the run's trophies (<see cref="RunTrophies"/>) and the gems of every trophy-path step
    /// the child's total reaches with them, and lets the missions see the run. Ending twice returns the stored result.
    /// The booking bumps the child's concurrency stamp like every wallet path, so two parallel ends can neither both
    /// pass the cap nor both pay a path step; the loser gets <c>concurrency_conflict</c>.
    /// </summary>
    public async Task<Outcome<TowerRunEndResult>> EndAsync(int childId, int runId, int reportedWave, CancellationToken ct)
    {
        var run = await db.TowerGameRuns.Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == runId && r.ChildId == childId, ct);
        if (run is null) return Outcome<TowerRunEndResult>.NotFound;

        var today = learningDay.Today;
        var solved = run.Items.Count(i => i.SolvedAt is not null);
        var goal = Math.Min(Goal, run.Items.Count);
        if (run.EndedAt is { } endedAt)
            return new TowerRunEndResult(run.Won, solved, goal, run.CoinsAwarded, await CoinsTodayAsync(childId, today, ct),
                opts.DailyCoinCap, run.Wave, run.Trophies, await TrophiesAsync(childId, ct),
                // The steps this run reached carry its end as their time stamp.
                await db.TowerTrophyRewards.Where(r => r.ChildId == childId && r.AwardedAt == endedAt).SumAsync(r => r.Gems, ct));

        var child = await db.Children.FirstAsync(c => c.Id == childId, ct);
        var coinsBefore = await CoinsTodayAsync(childId, today, ct);
        var trophiesBefore = await TrophiesAsync(childId, ct);
        var now = time.GetUtcNow().UtcDateTime;
        Beat(run);
        run.EndedAt = now;
        run.Won = solved >= goal;
        run.CoinsAwarded = run.Won ? Math.Clamp(opts.DailyCoinCap - coinsBefore, 0, opts.CoinsPerWin) : 0;
        run.Wave = AcceptedWave(reportedWave, run.PlayedSeconds, opts.MinSecondsPerWave);
        run.Trophies = RunTrophies(run.Items.Where(i => i.SolvedAt is not null).Select(i => i.Value), run.Wave);
        var trophies = trophiesBefore + run.Trophies;
        var paid = await db.TowerTrophyRewards.Where(r => r.ChildId == childId).Select(r => r.Trophies).ToListAsync(ct);
        var gems = 0;
        foreach (var step in opts.TrophyPath.Where(s => s.Trophies <= trophies && !paid.Contains(s.Trophies)))
        {
            gems += step.Gems;
            db.TowerTrophyRewards.Add(new TowerTrophyReward { ChildId = childId, Trophies = step.Trophies, Gems = step.Gems, AwardedAt = now });
            if (step.Gems > 0)
                db.ChildPointsEntries.Add(new ChildPointsEntry
                {
                    ChildId = childId,
                    Kind = PointKind.TowerTrophyGems,
                    Amount = step.Gems,
                    Reason = $"Vokabel-Türme: {step.Trophies} Trophäen erreicht",
                    CreatedAt = now,
                });
        }
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
        // The run is on record now: missions on tower metrics (words, wave, new words) are checked at this seam.
        await gamification.EvaluateAndAwardAsync(childId, today, ct);
        return new TowerRunEndResult(run.Won, solved, goal, run.CoinsAwarded, coinsBefore + run.CoinsAwarded,
            opts.DailyCoinCap, run.Wave, run.Trophies, trophies, gems);
    }

    /// <summary>
    /// What a word is worth for its tower, by the child's Leitner box across plans: a word never met or still in the
    /// lower boxes pays most, a well learned one least - the reward follows the need to practise.
    /// </summary>
    public static int WordValue(int? box) => box switch
    {
        null or <= 2 => 3,
        3 => 2,
        _ => 1,
    };

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
            && p.StudyPlan.StartDate <= today && (p.StudyPlan.EndDate == null || today <= p.StudyPlan.EndDate), ct);
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
