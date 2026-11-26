using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// The praise channel (B-377): a supervisor sends a child a short plain text, the child sees it once. Praise books
/// nothing - no ledger entry, no stamp - so the wallet invariant is not touched (decision 1).
/// </summary>
public class PraiseService(PuglingDbContext db)
{
    /// <summary>Upper bound of a praise after trimming (decision 6): short enough for one overlay line.</summary>
    public const int MaxTextLength = 140;

    /// <summary>How many unread praises the overlay shows at once (decision 5); the rest follow on the next open.</summary>
    public const int OverlayLimit = 5;

    /// <summary>Stores a praise from <paramref name="supervisorId"/> to <paramref name="childId"/>; the text is trimmed.</summary>
    public async Task<Praise> SendAsync(int childId, int supervisorId, string text, DateTime nowUtc, CancellationToken ct)
    {
        var praise = new Praise { ChildId = childId, SupervisorId = supervisorId, Text = text.Trim(), CreatedAt = nowUtc };
        db.Praises.Add(praise);
        await db.SaveChangesAsync(ct);
        return praise;
    }

    /// <summary>The sender's own praise to one child, newest first - another supervisor's never shows (decision 7).</summary>
    public IQueryable<PraiseResponse> SentBy(int childId, int supervisorId) =>
        db.Praises.AsNoTracking()
            .Where(p => p.ChildId == childId && p.SupervisorId == supervisorId)
            .OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
            .Select(p => new PraiseResponse(p.Id, p.ChildId, p.Text, p.CreatedAt, p.SeenAt));

    /// <summary>The child's unread praise, newest first, at most <see cref="OverlayLimit"/>.</summary>
    public Task<List<MyPraiseResponse>> UnreadAsync(int childId, CancellationToken ct) =>
        db.Praises.AsNoTracking()
            .Where(p => p.ChildId == childId && p.SeenAt == null)
            .OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
            .Take(OverlayLimit)
            .Select(p => new MyPraiseResponse(p.Id, p.Supervisor!.Name, p.Text, p.CreatedAt))
            .ToListAsync(ct);

    /// <summary>
    /// Stamps the given ids seen - only the child's own and only unread ones, so a foreign id is ignored and a second
    /// call keeps the first stamp.
    /// </summary>
    public Task MarkSeenAsync(int childId, IReadOnlyCollection<int> ids, DateTime nowUtc, CancellationToken ct) =>
        db.Praises
            .Where(p => p.ChildId == childId && p.SeenAt == null && ids.Contains(p.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.SeenAt, nowUtc), ct);
}
