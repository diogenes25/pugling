using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Auth;

/// <summary>
/// Rollen-Namen (als Rollen-Claim im JWT) – die drei fachlichen Ebenen. Ein Konto kann mehrere
/// Rollen tragen (ein Vater ist zugleich Creator und Supervisor). Die <c>[Authorize(Roles=…)]</c>
/// gaten direkt auf diese Ebenen; das frühere Vater/Sohn-Alias wurde entfernt.
/// </summary>
public static class Roles
{
    /// <summary>Ebenen-Rolle für Inhalte: erstellt/verwaltet den Lern-Katalog (Fächer, Kapitel, Übungen).</summary>
    public const string Creator = "Creator";
    /// <summary>Ebenen-Rolle für Steuerung: verwaltet Kinder, Lehrpläne und Belohnungen.</summary>
    public const string Supervisor = "Supervisor";
    /// <summary>Ebenen-Rolle fürs Lernen: spielt Übungen und Tests.</summary>
    public const string Student = "Student";
    /// <summary>Plattform-Superuser (Break-Glass). Umgeht die RWX-Rechteprüfung auf Übungen – z. B. um
    /// verwaiste (ownerlose) Übungen im Notfall zu bearbeiten. Wird nicht per API vergeben, sondern über
    /// das Flag <see cref="Adult.IsAdmin"/> (DB/Seed) gesetzt und beim Login als Rollen-Claim ausgestellt.</summary>
    public const string Admin = "Admin";
}

/// <summary>Zugriff auf Identität aus dem JWT.</summary>
public static class ClaimsPrincipalExtensions
{
    // Entität-IDs aus dem Token: fid trägt sowohl das Creator- als auch das Supervisor-Profil
    // (ein Erwachsener = ein Adult); cid trägt das Student-Profil. (Adult/Child sind die Fach-Entitäten,
    // nicht die Rollen – die Rollen heißen Creator/Supervisor/Student.)
    //
    // Der Claim heißt weiterhin `fid`, obwohl die Entität `Adult` heißt: er steht in bereits ausgestellten
    // Tokens. Ihn umzubenennen würde jede offene Sitzung ungültig machen – für einen Namen, den niemand
    // sieht. Der Zugriff heißt `AdultId()`, damit der Code die richtige Sprache spricht.
    /// <summary>Die <c>Adult</c>-Id aus dem Claim <c>fid</c> (Creator-/Supervisor-Profil), sofern vorhanden.</summary>
    public static int? AdultId(this ClaimsPrincipal u) => int.TryParse(u.FindFirstValue("fid"), out var v) ? v : null;
    /// <summary>Die <c>Child</c>-Id aus dem Claim <c>cid</c> (Student-Profil), sofern vorhanden.</summary>
    public static int? ChildId(this ClaimsPrincipal u) => int.TryParse(u.FindFirstValue("cid"), out var v) ? v : null;

    /// <summary>
    /// Das Konto selbst (Claim <c>aid</c>) – rollenunabhängig. Nötig überall dort, wo nicht die Ebene
    /// zählt, sondern die Person: etwa die Autorschaft einer Anmerkung, die derselbe Mensch mal als
    /// Supervisor und mal als Student erfasst.
    /// </summary>
    public static int? AccountId(this ClaimsPrincipal u) => int.TryParse(u.FindFirstValue("aid"), out var v) ? v : null;

    // Ebenen-Rollen und ihre Ziel-IDs.
    /// <summary>Trägt der Principal die Ebenen-Rolle <see cref="Roles.Creator"/>?</summary>
    public static bool IsCreator(this ClaimsPrincipal u) => u.IsInRole(Roles.Creator);
    /// <summary>Trägt der Principal die Ebenen-Rolle <see cref="Roles.Supervisor"/>?</summary>
    public static bool IsSupervisor(this ClaimsPrincipal u) => u.IsInRole(Roles.Supervisor);
    /// <summary>Trägt der Principal die Ebenen-Rolle <see cref="Roles.Student"/>?</summary>
    public static bool IsStudent(this ClaimsPrincipal u) => u.IsInRole(Roles.Student);
    /// <summary>Plattform-Superuser (Break-Glass, siehe <see cref="Roles.Admin"/>).</summary>
    public static bool IsAdmin(this ClaimsPrincipal u) => u.IsInRole(Roles.Admin);
    /// <summary>Die <c>Adult</c>-Id des Supervisor-Profils (identisch zu <see cref="AdultId"/>).</summary>
    public static int? SupervisorId(this ClaimsPrincipal u) => u.AdultId();
    /// <summary>Die <c>Adult</c>-Id des Creator-Profils (identisch zu <see cref="AdultId"/>).</summary>
    public static int? CreatorId(this ClaimsPrincipal u) => u.AdultId();
    /// <summary>Die <c>Child</c>-Id des Student-Profils (identisch zu <see cref="ChildId"/>).</summary>
    public static int? StudentId(this ClaimsPrincipal u) => u.ChildId();

    // `Owns(this ClaimsPrincipal, Exercise)` wurde entfernt: Es behauptete, „die eine Stelle" der
    // Autorschafts-Regel zu sein, war aber nach dem RWX-Umbau von niemandem mehr aufgerufen –
    // durchgesetzt wird ausschließlich über die Grants (siehe ExercisePermissionService). Ein toter
    // Helfer mit genau diesem Kommentar ist schlimmer als keiner: er lädt dazu ein, die Rechteprüfung
    // an ihm statt an den Grants festzumachen.

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
    public async Task<bool> OwnsPlanAsync(ClaimsPrincipal user, StudyPlan plan, CancellationToken ct = default)
    {
        if (user.IsStudent() && plan.ChildId == user.StudentId()) return true;
        var fid = user.SupervisorId();
        return user.IsSupervisor() && fid is not null
            && await db.SupervisorLinks.AnyAsync(l => l.StudentId == plan.ChildId && l.SupervisorId == fid, ct);
    }

    /// <summary>Betreut der angemeldete Supervisor dieses Kind (Mitgliedschaft über <see cref="SupervisorLink"/>)?</summary>
    public async Task<bool> FatherOwnsChildAsync(ClaimsPrincipal user, int childId, CancellationToken ct = default)
    {
        var fid = user.SupervisorId();
        return fid is not null && await db.SupervisorLinks.AnyAsync(l => l.StudentId == childId && l.SupervisorId == fid, ct);
    }

    /// <summary>
    /// Darf der angemeldete Nutzer auf die kindbezogenen Daten dieses Kindes zugreifen?
    /// Student = nur sein eigenes Profil, Supervisor = jedes von ihm betreute Kind.
    /// </summary>
    public async Task<bool> OwnsChildAsync(ClaimsPrincipal user, int childId, CancellationToken ct = default)
    {
        if (user.IsStudent() && user.StudentId() == childId) return true;
        return user.IsSupervisor() && await FatherOwnsChildAsync(user, childId, ct);
    }
}
