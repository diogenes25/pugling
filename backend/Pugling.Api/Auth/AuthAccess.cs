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

    /// <summary>
    /// Ob der Principal Autor der Übung ist und sie daher ändern/löschen darf. Die eine Stelle,
    /// an der die Autorschafts-Regel lebt (Anzeige-<c>IsOwn</c> wie serverseitige Durchsetzung),
    /// damit beide nicht auseinanderdriften.
    /// </summary>
    public static bool Owns(this ClaimsPrincipal u, Exercise exercise) =>
        IsOwnedBy(exercise.AuthorFatherId, u.FatherId());

    /// <summary>
    /// Reiner Eigentums-Vergleich (für Hot-Paths/Projektionen, wo der <c>fid</c> einmal ermittelt wird):
    /// Eine Übung gehört einem Vater nur, wenn sie einen Autor hat <b>und</b> dieser der Vater ist.
    /// Fehlt der Autor (geseedete System-Übung) oder der <c>fid</c>, ist das Ergebnis <c>false</c>
    /// (fail-closed) – sonst würde ein fehlender Claim System-Übungen fälschlich freigeben.
    /// </summary>
    public static bool IsOwnedBy(int? authorFatherId, int? fatherId) =>
        authorFatherId is { } author && author == fatherId;
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
