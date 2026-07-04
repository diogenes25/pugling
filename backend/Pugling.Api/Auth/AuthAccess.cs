using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Auth;

/// <summary>Rollen-Namen (werden als Rollen-Claim im JWT geführt).</summary>
public static class Roles
{
    public const string Vater = "Vater";
    public const string Sohn = "Sohn";
}

/// <summary>Zugriff auf Identität aus dem JWT.</summary>
public static class ClaimsPrincipalExtensions
{
    public static bool IsFather(this ClaimsPrincipal u) => u.IsInRole(Roles.Vater);
    public static bool IsChild(this ClaimsPrincipal u) => u.IsInRole(Roles.Sohn);
    public static int? FatherId(this ClaimsPrincipal u) => int.TryParse(u.FindFirstValue("fid"), out var v) ? v : null;
    public static int? ChildId(this ClaimsPrincipal u) => int.TryParse(u.FindFirstValue("cid"), out var v) ? v : null;
}

/// <summary>
/// Eigentums-Prüfungen: Vater darf nur seine eigenen Kinder/Pläne, Sohn nur seine eigenen.
/// </summary>
public class AuthAccess(PuglingDbContext db)
{
    /// <summary>Gehört der Plan dem angemeldeten Nutzer (Sohn = eigener Plan, Vater = Plan eines eigenen Kindes)?</summary>
    public async Task<bool> OwnsPlanAsync(ClaimsPrincipal user, StudyPlan plan)
    {
        if (user.IsChild()) return plan.ChildId == user.ChildId();
        if (user.IsFather())
        {
            var fid = user.FatherId();
            return fid is not null && await db.Children.AnyAsync(c => c.Id == plan.ChildId && c.FatherId == fid);
        }
        return false;
    }

    /// <summary>Gehört das Kind dem angemeldeten Vater?</summary>
    public async Task<bool> FatherOwnsChildAsync(ClaimsPrincipal user, int childId)
    {
        var fid = user.FatherId();
        return fid is not null && await db.Children.AnyAsync(c => c.Id == childId && c.FatherId == fid);
    }

    /// <summary>
    /// Darf der angemeldete Nutzer auf die kindbezogenen Daten dieses Kindes zugreifen?
    /// Sohn = nur sein eigenes Kind-Profil, Vater = jedes seiner Kinder.
    /// </summary>
    public async Task<bool> OwnsChildAsync(ClaimsPrincipal user, int childId)
    {
        if (user.IsChild()) return user.ChildId() == childId;
        if (user.IsFather()) return await FatherOwnsChildAsync(user, childId);
        return false;
    }
}
