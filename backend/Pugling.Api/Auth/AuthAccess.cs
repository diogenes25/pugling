using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Auth;

/// <summary>
/// Rollen-Namen (als Rollen-Claim im JWT). Die drei fachlichen Ebenen (Creator/Supervisor/Student)
/// sind die neuen Rollen; <see cref="Vater"/>/<see cref="Sohn"/> bleiben als Alias erhalten, damit die
/// bestehenden <c>[Authorize(Roles=…)]</c> unverändert greifen. Ein Konto kann mehrere Rollen tragen.
/// </summary>
public static class Roles
{
    public const string Vater = "Vater";
    public const string Sohn = "Sohn";
    public const string Creator = "Creator";
    public const string Supervisor = "Supervisor";
    public const string Student = "Student";
}

/// <summary>Zugriff auf Identität aus dem JWT.</summary>
public static class ClaimsPrincipalExtensions
{
    // Alt-Vokabular (bewusst erhalten – hält bestehende Inline-Checks/Force-Unwraps ohne Edits):
    public static bool IsFather(this ClaimsPrincipal u) => u.IsInRole(Roles.Vater);
    public static bool IsChild(this ClaimsPrincipal u) => u.IsInRole(Roles.Sohn);
    public static int? FatherId(this ClaimsPrincipal u) => int.TryParse(u.FindFirstValue("fid"), out var v) ? v : null;
    public static int? ChildId(this ClaimsPrincipal u) => int.TryParse(u.FindFirstValue("cid"), out var v) ? v : null;

    // Ebenen-Vokabular (neuer Code): Rollen und ihre Ziel-IDs. fid trägt heute sowohl das Creator- als
    // auch das Supervisor-Profil (ein Haushalt = ein Father); cid trägt das Student-Profil.
    public static bool IsCreator(this ClaimsPrincipal u) => u.IsInRole(Roles.Creator);
    public static bool IsSupervisor(this ClaimsPrincipal u) => u.IsInRole(Roles.Supervisor);
    public static bool IsStudent(this ClaimsPrincipal u) => u.IsInRole(Roles.Student);
    public static int? SupervisorId(this ClaimsPrincipal u) => u.FatherId();
    public static int? CreatorId(this ClaimsPrincipal u) => u.FatherId();
    public static int? StudentId(this ClaimsPrincipal u) => u.ChildId();

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
    // OR-basiert statt if/else: ein Konto kann Student UND Supervisor sein (perspektivisch in verschiedenen
    // Haushalten). Jede Rolle wird eigenständig geprüft; erfüllt eine, ist der Zugriff erlaubt.

    /// <summary>Gehört der Plan dem angemeldeten Nutzer (Student = eigener Plan, Supervisor = Plan eines betreuten Kindes)?</summary>
    public async Task<bool> OwnsPlanAsync(ClaimsPrincipal user, StudyPlan plan)
    {
        if (user.IsStudent() && plan.ChildId == user.StudentId()) return true;
        var fid = user.SupervisorId();
        return user.IsSupervisor() && fid is not null
            && await db.Children.AnyAsync(c => c.Id == plan.ChildId && c.FatherId == fid);
    }

    /// <summary>Betreut der angemeldete Supervisor dieses Kind?</summary>
    public async Task<bool> FatherOwnsChildAsync(ClaimsPrincipal user, int childId)
    {
        var fid = user.SupervisorId();
        return fid is not null && await db.Children.AnyAsync(c => c.Id == childId && c.FatherId == fid);
    }

    /// <summary>
    /// Darf der angemeldete Nutzer auf die kindbezogenen Daten dieses Kindes zugreifen?
    /// Student = nur sein eigenes Profil, Supervisor = jedes von ihm betreute Kind.
    /// </summary>
    public async Task<bool> OwnsChildAsync(ClaimsPrincipal user, int childId)
    {
        if (user.IsStudent() && user.StudentId() == childId) return true;
        return user.IsSupervisor() && await FatherOwnsChildAsync(user, childId);
    }
}
