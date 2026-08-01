using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Auth;

/// <summary>
/// The one place for the RWX permission rule for exercises (replaces the former single-author <c>Owns</c> as enforcement).
/// A permission hangs off an <see cref="ExerciseGrant"/> for the requesting creator (<c>fid</c>):
/// <list type="bullet">
/// <item><b>Write</b> (modify) = owner or write grant.</item>
/// <item><b>Administer</b> (delete, grant permissions, toggle visibility) = owner grant.</item>
/// <item><b>Execute</b> (assign) = <see cref="Exercise.ExecutePublic"/> OR any grant.</item>
/// </list>
/// Read is deliberately not checked – the catalog stays readable for everyone. If <c>fid</c> is missing,
/// everything is fail-closed <c>false</c> (seeded system exercises without an owner remain unmanageable).
/// </summary>
public class ExercisePermissionService(PuglingDbContext db)
{
    /// <summary>May the requesting creator modify the exercise content (owner or write grant, or admin)?</summary>
    public async Task<bool> CanWriteAsync(ClaimsPrincipal user, int exerciseId, CancellationToken ct = default)
    {
        if (user.IsAdmin()) return true;
        var fid = user.AdultId();
        return fid is not null && await db.ExerciseGrants.AnyAsync(g =>
            g.ExerciseId == exerciseId && g.CreatorId == fid
            && (g.Permission == GrantPermission.Owner || g.Permission == GrantPermission.Write), ct);
    }

    /// <summary>May the requesting creator administer the exercise – delete, grant/revoke permissions, toggle visibility (owner grant, or admin)?</summary>
    public async Task<bool> CanAdministerAsync(ClaimsPrincipal user, int exerciseId, CancellationToken ct = default)
    {
        if (user.IsAdmin()) return true;
        var fid = user.AdultId();
        return fid is not null && await db.ExerciseGrants.AnyAsync(g =>
            g.ExerciseId == exerciseId && g.CreatorId == fid && g.Permission == GrantPermission.Owner, ct);
    }

    /// <summary>
    /// May the requesting creator assign the exercise (add it to a study plan/class test)? Publicly
    /// executable exercises (default) may be assigned by anyone; otherwise only with an owner/write/execute grant (or admin).
    /// </summary>
    public async Task<bool> CanExecuteAsync(ClaimsPrincipal user, Exercise exercise, CancellationToken ct = default)
    {
        if (exercise.ExecutePublic || user.IsAdmin()) return true;
        var fid = user.AdultId();
        return fid is not null && await db.ExerciseGrants.AnyAsync(g =>
            g.ExerciseId == exercise.Id && g.CreatorId == fid, ct);
    }

    // ── In-memory variants for projections with grants already loaded (no DB round trip per row) ─────────
    // The admin bypass is passed in through the isAdmin flag (the static method does not know the principal).

    /// <summary>Write rule on a loaded grant set (for list/detail projections).</summary>
    public static bool CanWrite(IEnumerable<ExerciseGrant> grants, int? fid, bool isAdmin = false) =>
        isAdmin || (fid is int f && grants.Any(g => g.CreatorId == f
            && (g.Permission is GrantPermission.Owner or GrantPermission.Write)));

    /// <summary>Owner rule on a loaded grant set (for list/detail projections).</summary>
    public static bool CanAdminister(IEnumerable<ExerciseGrant> grants, int? fid, bool isAdmin = false) =>
        isAdmin || (fid is int f && grants.Any(g => g.CreatorId == f && g.Permission == GrantPermission.Owner));
}
