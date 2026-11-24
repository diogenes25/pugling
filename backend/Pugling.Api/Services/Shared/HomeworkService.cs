using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>Homework amounts from the configuration (section <c>Gamification:Homework</c>, B-760); a child may override both.</summary>
public class HomeworkOptions
{
    /// <summary>Configuration section.</summary>
    public const string SectionName = "Gamification:Homework";

    /// <summary>Coins an accepted homework books when the child sets no own amount.</summary>
    public int Coins { get; set; } = 20;

    /// <summary>Coins a homework without a hand-in by its due day costs when the child sets no own amount.</summary>
    public int PenaltyCoins { get; set; } = 2;
}

/// <summary>
/// A child's homework (B-760): set, hand in, accept or give back, and the lazy penalty. Handed in by the due day means done
/// for the penalty - the penalty comes only without any hand-in by then, and the supervisor's acceptance brings the coins
/// (B-687 decision 2). Nothing here touches positions, so streak, daily box, missions and the bonus-training lock never see a
/// homework (decision 1).
/// </summary>
public class HomeworkService(PuglingDbContext db, TimeProvider time, LearningDay learningDay, IOptions<HomeworkOptions> options)
{
    /// <summary>Upper bound of a homework's text - the length of its column.</summary>
    public const int MaxTextLength = 2000;

    /// <summary>Upper bound of a rework note: one line, the length of its column.</summary>
    public const int MaxNoteLength = 200;

    /// <summary>Result of a write: the homework as it is now, or the error with its detail.</summary>
    public record Outcome(HomeworkResponse? Value, ApiError? Error = null, string? Detail = null);

    private HomeworkOptions Options => options.Value;

    /// <summary>The child's homework, latest due day first; <paramref name="take"/> bounds the list (none = all).</summary>
    public async Task<IReadOnlyList<HomeworkResponse>> ListAsync(int childId, int? take, CancellationToken ct)
    {
        var child = await AmountsAsync(childId, ct);
        var query = Query().Where(h => h.ChildId == childId).OrderByDescending(h => h.DueOn).ThenByDescending(h => h.Id);
        var rows = await (take is { } n ? query.Take(n) : query).ToListAsync(ct);
        var charged = await ChargedAsync(rows.Select(h => h.Id).ToList(), ct);
        return [.. rows.Select(h => Project(h, child, charged))];
    }

    /// <summary>Sets a new homework for the child. The caller has checked text and subject.</summary>
    public async Task<HomeworkResponse> CreateAsync(int childId, int? subjectId, string text, DateOnly dueOn, CancellationToken ct)
    {
        var homework = new Homework
        {
            ChildId = childId,
            SubjectId = subjectId,
            Text = text,
            DueOn = dueOn,
            CreatedAt = time.GetUtcNow().UtcDateTime,
        };
        db.Homeworks.Add(homework);
        await db.SaveChangesAsync(ct);
        return (await OneAsync(childId, homework.Id, ct))!;
    }

    /// <summary>The child hands in. Allowed after the due day too - it then averts nothing - but not after acceptance.</summary>
    public async Task<Outcome> SubmitAsync(int childId, int homeworkId, CancellationToken ct)
    {
        var homework = await db.Homeworks.FirstOrDefaultAsync(h => h.Id == homeworkId && h.ChildId == childId, ct);
        if (homework is null) return NotFound();
        if (homework.Status == HomeworkStatus.Accepted) return AlreadyAccepted();

        db.HomeworkSubmissions.Add(new HomeworkSubmission { HomeworkId = homework.Id, SubmittedAt = time.GetUtcNow().UtcDateTime });
        homework.Status = HomeworkStatus.Submitted;
        await db.SaveChangesAsync(ct);
        return new Outcome(await OneAsync(childId, homeworkId, ct));
    }

    /// <summary>
    /// The supervisor accepts a hand-in: the child's amount is booked once as <see cref="PointKind.HomeworkCoins"/>. A second
    /// acceptance is refused, and two at the same moment collide on the child's stamp, so the coins come once. Only a homework
    /// waiting with a hand-in is accepted: one given back for rework waits for the next hand-in - accepting it as well would let
    /// a hand-in arriving at the same moment overwrite "accepted" and open the way to a second booking.
    /// </summary>
    public async Task<Outcome> AcceptAsync(int childId, int homeworkId, CancellationToken ct)
    {
        var homework = await db.Homeworks.Include(h => h.Subject).Include(h => h.Submissions)
            .FirstOrDefaultAsync(h => h.Id == homeworkId && h.ChildId == childId, ct);
        if (homework is null) return NotFound();
        if (homework.Status == HomeworkStatus.Accepted) return AlreadyAccepted();
        if (homework.Status != HomeworkStatus.Submitted) return NotSubmitted();
        var child = await db.Children.FirstAsync(c => c.Id == childId, ct);

        var now = time.GetUtcNow().UtcDateTime;
        var coins = child.HomeworkCoins ?? Options.Coins;
        homework.Status = HomeworkStatus.Accepted;
        homework.AwardedCoins = coins;
        homework.ReviewedAt = now;
        if (coins > 0)
            db.ChildPointsEntries.Add(new ChildPointsEntry
            {
                ChildId = childId,
                Kind = PointKind.HomeworkCoins,
                Amount = coins,
                Reason = $"{Label(homework)} angenommen",
                CreatedAt = now,
            });
        // Bumped although this only books coins in (B-627 decision 6): the stamp makes a second acceptance at the same
        // moment fail instead of booking twice.
        child.ConcurrencyStamp = Guid.NewGuid();
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            return new Outcome(null, ApiErrors.ConcurrencyConflict, "The child's coins were changed at the same time. Please try again.");
        }
        return new Outcome(await OneAsync(childId, homeworkId, ct));
    }

    /// <summary>
    /// The supervisor gives a hand-in back with a note (B-687 decision 6): no partial coins, and the due day does not move -
    /// the hand-in already averted the penalty.
    /// </summary>
    public async Task<Outcome> ReworkAsync(int childId, int homeworkId, string note, CancellationToken ct)
    {
        var homework = await db.Homeworks.Include(h => h.Submissions)
            .FirstOrDefaultAsync(h => h.Id == homeworkId && h.ChildId == childId, ct);
        if (homework is null) return NotFound();
        if (homework.Status == HomeworkStatus.Accepted) return AlreadyAccepted();
        if (homework.Submissions.Count == 0) return NotSubmitted();

        homework.Status = HomeworkStatus.Rework;
        homework.ReworkNote = note;
        homework.ReviewedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);
        return new Outcome(await OneAsync(childId, homeworkId, ct));
    }

    /// <summary>Removes a homework; coins or a penalty already booked stay in the ledger.</summary>
    public async Task<bool> DeleteAsync(int childId, int homeworkId, CancellationToken ct) =>
        await db.Homeworks.Where(h => h.Id == homeworkId && h.ChildId == childId).ExecuteDeleteAsync(ct) > 0;

    /// <summary>
    /// Books the penalty once for every homework whose due day has passed without a hand-in by its end, as a negative
    /// <see cref="PointKind.GoalPenalty"/> - at most <see cref="PositionProgressService.MaxSettleLookbackDays"/> days back, and
    /// only for a homework set before its due day was over (the child could not have seen a later one in time). Called at the
    /// same seams as the duty's settlement (login, shop purchase), idempotent through the unique key in
    /// <see cref="HomeworkPenalty"/>. A later hand-in does not take a booked penalty back; that is the supervisor's manual gift.
    /// </summary>
    /// <returns>Sum of the coins deducted in this run.</returns>
    public async Task<int> SettleAsync(int childId, DateOnly today, CancellationToken ct)
    {
        var floor = today.AddDays(-PositionProgressService.MaxSettleLookbackDays);
        var due = await db.Homeworks.Include(h => h.Subject).Include(h => h.Submissions)
            .Where(h => h.ChildId == childId && h.DueOn < today && h.DueOn >= floor
                && !db.HomeworkPenalties.Any(p => p.HomeworkId == h.Id))
            .ToListAsync(ct);
        // Days on the learning-day calendar (B-730), so in memory: "set in time" and "handed in in time" both end with the
        // due day in the family's zone, not in UTC.
        var missed = due.Where(h => learningDay.DayOf(h.CreatedAt) <= h.DueOn
            && !h.Submissions.Any(s => InTime(h, s))).ToList();
        if (missed.Count == 0) return 0;

        var child = await db.Children.FirstAsync(c => c.Id == childId, ct);
        var amount = child.HomeworkPenaltyCoins ?? Options.PenaltyCoins;
        if (amount <= 0) return 0;

        var now = time.GetUtcNow().UtcDateTime;
        foreach (var homework in missed)
        {
            db.HomeworkPenalties.Add(new HomeworkPenalty { HomeworkId = homework.Id, Points = amount, AppliedAt = now });
            db.ChildPointsEntries.Add(new ChildPointsEntry
            {
                ChildId = childId,
                Kind = PointKind.GoalPenalty,
                Amount = -amount,
                Reason = $"{Label(homework)} bis {homework.DueOn.Day}.{homework.DueOn.Month}. nicht abgegeben",
                CreatedAt = now,
            });
        }
        // Wallet invariant: a debiting path bumps the serialization point of the shared balance.
        child.ConcurrencyStamp = Guid.NewGuid();
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // A parallel settlement hit the unique key or the child's stamp - benign: the penalty is there already, or the
            // next run books it.
            db.ChangeTracker.Clear();
            return 0;
        }
        return amount * missed.Count;
    }

    private bool InTime(Homework homework, HomeworkSubmission submission) => learningDay.DayOf(submission.SubmittedAt) <= homework.DueOn;

    /// <summary>The homework as the ledger names it: "[Hausaufgabe Mathe] S. 12 Nr. 3".</summary>
    private static string Label(Homework homework)
    {
        var text = homework.Text.Length > 60 ? Truncate(homework.Text, 60) + "…" : homework.Text;
        return homework.Subject is { } subject ? $"[Hausaufgabe {subject.Name}] {text}" : $"[Hausaufgabe] {text}";
    }

    // Cut by code units, but never between the two halves of a surrogate pair: a lone half would land in the ledger as an
    // unreadable character.
    internal static string Truncate(string text, int max) =>
        char.IsHighSurrogate(text[max - 1]) ? text[..(max - 1)] : text[..max];

    private IQueryable<Homework> Query() =>
        db.Homeworks.AsNoTracking().Include(h => h.Subject).Include(h => h.Submissions.OrderBy(s => s.SubmittedAt).ThenBy(s => s.Id));

    private async Task<HomeworkResponse?> OneAsync(int childId, int homeworkId, CancellationToken ct)
    {
        var homework = await Query().FirstOrDefaultAsync(h => h.Id == homeworkId && h.ChildId == childId, ct);
        if (homework is null) return null;
        return Project(homework, await AmountsAsync(childId, ct), await ChargedAsync([homework.Id], ct));
    }

    private async Task<(int? Coins, int? Penalty)> AmountsAsync(int childId, CancellationToken ct)
    {
        // A token can outlive its child; the list of a deleted child is then simply empty, not a 500.
        var child = await db.Children.AsNoTracking().Where(c => c.Id == childId)
            .Select(c => new { c.HomeworkCoins, c.HomeworkPenaltyCoins }).FirstOrDefaultAsync(ct);
        return (child?.HomeworkCoins, child?.HomeworkPenaltyCoins);
    }

    private async Task<Dictionary<int, int>> ChargedAsync(List<int> homeworkIds, CancellationToken ct) =>
        await db.HomeworkPenalties.AsNoTracking().Where(p => homeworkIds.Contains(p.HomeworkId))
            .ToDictionaryAsync(p => p.HomeworkId, p => p.Points, ct);

    private HomeworkResponse Project(Homework h, (int? Coins, int? Penalty) child, Dictionary<int, int> charged)
    {
        var isCharged = charged.TryGetValue(h.Id, out var booked);
        return new HomeworkResponse(h.Id, h.ChildId, h.SubjectId, h.Subject?.Name, h.Text, h.DueOn, h.CreatedAt, h.Status,
            h.ReworkNote, h.ReviewedAt,
            h.AwardedCoins ?? child.Coins ?? Options.Coins,
            isCharged ? booked : child.Penalty ?? Options.PenaltyCoins,
            isCharged,
            [.. h.Submissions.Select(s => new HomeworkSubmissionResponse(s.Id, s.SubmittedAt, InTime(h, s)))]);
    }

    private static Outcome NotFound() => new(null, ApiErrors.NotFound, "Homework not found.");

    private static Outcome AlreadyAccepted() =>
        new(null, ApiErrors.HomeworkAccepted, "This homework is already accepted and its coins are booked.");

    private static Outcome NotSubmitted() =>
        new(null, ApiErrors.HomeworkNotSubmitted,
            "This homework has no new hand-in; accept or give it back once the child has handed in.");
}
