using Microsoft.EntityFrameworkCore;

namespace Pugling.Api.Controllers;

/// <summary>Shared offset paging for list endpoints (skip/take + <c>X-Total-Count</c> header).</summary>
public static class PagingExtensions
{
    /// <summary>Default page size when the caller specifies no take.</summary>
    public const int DefaultTake = 100;

    /// <summary>Upper bound per page (protection against full scans).</summary>
    public const int MaxTake = 500;

    /// <summary>
    /// Executes a filtered, sorted query page by page: first sets the total hit count
    /// in the <c>X-Total-Count</c> header (before the body!), then applies skip/take.
    /// <paramref name="take"/> is clamped to 0..<see cref="MaxTake"/> (<c>0</c> = count only, load no rows –
    /// useful for pure metrics), <paramref name="skip"/> to &gt;= 0.
    /// Expects a query that already has <c>OrderBy</c> applied, so the window is deterministic.
    /// </summary>
    public static async Task<List<T>> ToPagedListAsync<T>(
        this IQueryable<T> query, HttpResponse response, int skip, int take, CancellationToken ct = default)
    {
        response.Headers["X-Total-Count"] = (await query.CountAsync(ct)).ToString();
        return await query.Skip(Math.Max(skip, 0)).Take(Math.Clamp(take, 0, MaxTake)).ToListAsync(ct);
    }

    /// <summary>
    /// Additive paging for top-level collections that historically returned everything (B-122): with a
    /// <paramref name="take"/> it behaves exactly like <see cref="ToPagedListAsync{T}"/>; without one it
    /// applies no window at all, so an existing caller expecting the full list never silently gets a slice.
    /// <c>X-Total-Count</c> is set in both branches, so a client can read the header uniformly.
    /// Expects a query with a unique ordering (tie-break on <c>Id</c>), so a window is deterministic.
    /// </summary>
    public static async Task<List<T>> ToOptionallyPagedListAsync<T>(
        this IQueryable<T> query, HttpResponse response, int skip, int? take, CancellationToken ct)
    {
        if (take is { } window) return await query.ToPagedListAsync(response, skip, window, ct);
        response.Headers["X-Total-Count"] = (await query.CountAsync(ct)).ToString();
        return await query.Skip(Math.Max(skip, 0)).ToListAsync(ct);
    }

    /// <summary>
    /// In-memory variant of <see cref="ToPagedListAsync{T}"/> for already materialized, sorted
    /// lists (e.g. metrics from services without <c>IQueryable</c>): writes the total count into
    /// <c>X-Total-Count</c> (before the body!), then applies skip/take. <paramref name="take"/> is clamped to
    /// 0..<see cref="MaxTake"/>, <paramref name="skip"/> to &gt;= 0. Expects an already
    /// sorted source, so the window is deterministic.
    /// </summary>
    public static List<T> ToPagedList<T>(this IEnumerable<T> source, HttpResponse response, int skip, int take)
    {
        var all = source as IReadOnlyCollection<T> ?? source.ToList();
        response.Headers["X-Total-Count"] = all.Count.ToString();
        return all.Skip(Math.Max(skip, 0)).Take(Math.Clamp(take, 0, MaxTake)).ToList();
    }
}
