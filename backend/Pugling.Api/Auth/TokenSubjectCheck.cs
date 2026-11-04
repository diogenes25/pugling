using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;

namespace Pugling.Api.Auth;

/// <summary>
/// Refuses a signed, unexpired token whose account no longer exists (B-245).
/// <para>
/// A token outlives <c>DELETE supervisor/adults/{id}</c>: the signature and the lifetime still check out, but the
/// adult behind <c>fid</c> is gone. Every writer that puts <c>fid</c> into a foreign key then ran into the constraint
/// and answered 500. Checking here, once per request, covers all of them - six and more writers would otherwise each
/// need the same guard. The price is one primary-key lookup per authenticated request.
/// </para>
/// <para>
/// The account is the right thing to look up: deleting a child removes every account whose profiles all point at
/// it (<c>ChildrenController.Delete</c>), deleting an adult removes every account whose profiles all point at the
/// adult or at the children orphaned with them (<c>AdultsController.Delete</c>, B-285). Because no creation path
/// builds an account with profiles of two different subjects, a live account means live <c>fid</c>/<c>cid</c>; a
/// mixed account that survives a deletion would carry a stale <c>fid</c> in a pre-issued token (see B-282).
/// </para>
/// </summary>
public static class TokenSubjectCheck
{
    /// <summary><c>OnTokenValidated</c> hook: fails authentication (401) when the token's account is gone.</summary>
    public static async Task ValidateAsync(TokenValidatedContext context)
    {
        var aid = context.Principal?.AccountId();
        if (aid is null)
        {
            context.Fail("Token without account.");
            return;
        }
        var db = context.HttpContext.RequestServices.GetRequiredService<PuglingDbContext>();
        // Deliberately not RequestAborted (B-372): JwtBearerHandler logs any exception from this hook as an error before
        // rethrowing, so a client hanging up would bypass the "abort ends as 499 without an error log" rule. A
        // primary-key lookup is too short to be worth cancelling.
        if (!await db.Accounts.AsNoTracking().AnyAsync(a => a.Id == aid, CancellationToken.None))
            context.Fail("The account behind this token no longer exists.");
    }
}
