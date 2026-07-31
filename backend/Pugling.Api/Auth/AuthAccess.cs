using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Auth;

/// <summary>
/// Role names (as a role claim in the JWT) – the three domain tiers. An account can carry several
/// roles (an adult is simultaneously Creator and Supervisor). The <c>[Authorize(Roles=…)]</c>
/// gates directly on these tiers; the former father/child alias has been removed.
/// </summary>
public static class Roles
{
    /// <summary>Tier role for content: creates/manages the learning catalog (subjects, chapters, exercises).</summary>
    public const string Creator = "Creator";
    /// <summary>Tier role for control: manages children, study plans, and rewards.</summary>
    public const string Supervisor = "Supervisor";
    /// <summary>Tier role for learning: plays exercises and tests.</summary>
    public const string Student = "Student";
    /// <summary>Platform superuser (break-glass). Bypasses the RWX permission check on exercises – e.g. to
    /// edit orphaned (ownerless) exercises in an emergency. Not granted via the API but set through
    /// the <see cref="Adult.IsAdmin"/> flag (DB/seed) and issued as a role claim at login.</summary>
    public const string Admin = "Admin";
}

/// <summary>Access to identity from the JWT.</summary>
public static class ClaimsPrincipalExtensions
{
    // Entität-IDs aus dem Token: fid trägt sowohl das Creator- als auch das Supervisor-Profil
    // (ein Erwachsener = ein Adult); cid trägt das Student-Profil. (Adult/Child sind die Fach-Entitäten,
    // nicht die Rollen – die Rollen heißen Creator/Supervisor/Student.)
    //
    // Der Claim heißt weiterhin `fid`, obwohl die Entität `Adult` heißt: er steht in bereits ausgestellten
    // Tokens. Ihn umzubenennen würde jede offene Sitzung ungültig machen – für einen Namen, den niemand
    // sieht. Der Zugriff heißt `AdultId()`, damit der Code die richtige Sprache spricht.
    /// <summary>The <c>Adult</c> id from the <c>fid</c> claim (Creator/Supervisor profile), if present.</summary>
    public static int? AdultId(this ClaimsPrincipal u) => int.TryParse(u.FindFirstValue("fid"), out var v) ? v : null;
    /// <summary>The <c>Child</c> id from the <c>cid</c> claim (Student profile), if present.</summary>
    public static int? ChildId(this ClaimsPrincipal u) => int.TryParse(u.FindFirstValue("cid"), out var v) ? v : null;

    /// <summary>
    /// The account itself (claim <c>aid</c>) – role-independent. Needed wherever what matters is not
    /// the tier but the person: for instance the authorship of a remark, which the same human records
    /// sometimes as a supervisor and sometimes as a student.
    /// </summary>
    public static int? AccountId(this ClaimsPrincipal u) => int.TryParse(u.FindFirstValue("aid"), out var v) ? v : null;

    // Ebenen-Rollen und ihre Ziel-IDs.
    /// <summary>Does the principal carry the tier role <see cref="Roles.Creator"/>?</summary>
    public static bool IsCreator(this ClaimsPrincipal u) => u.IsInRole(Roles.Creator);
    /// <summary>Does the principal carry the tier role <see cref="Roles.Supervisor"/>?</summary>
    public static bool IsSupervisor(this ClaimsPrincipal u) => u.IsInRole(Roles.Supervisor);
    /// <summary>Does the principal carry the tier role <see cref="Roles.Student"/>?</summary>
    public static bool IsStudent(this ClaimsPrincipal u) => u.IsInRole(Roles.Student);
    /// <summary>Platform superuser (break-glass, see <see cref="Roles.Admin"/>).</summary>
    public static bool IsAdmin(this ClaimsPrincipal u) => u.IsInRole(Roles.Admin);
    /// <summary>The <c>Adult</c> id of the Supervisor profile (identical to <see cref="AdultId"/>).</summary>
    public static int? SupervisorId(this ClaimsPrincipal u) => u.AdultId();
    /// <summary>The <c>Adult</c> id of the Creator profile (identical to <see cref="AdultId"/>).</summary>
    public static int? CreatorId(this ClaimsPrincipal u) => u.AdultId();
    /// <summary>The <c>Child</c> id of the Student profile (identical to <see cref="ChildId"/>).</summary>
    public static int? StudentId(this ClaimsPrincipal u) => u.ChildId();

    // `Owns(this ClaimsPrincipal, Exercise)` wurde entfernt: Es behauptete, „die eine Stelle" der
    // Autorschafts-Regel zu sein, war aber nach dem RWX-Umbau von niemandem mehr aufgerufen –
    // durchgesetzt wird ausschließlich über die Grants (siehe ExercisePermissionService). Ein toter
    // Helfer mit genau diesem Kommentar ist schlimmer als keiner: er lädt dazu ein, die Rechteprüfung
    // an ihm statt an den Grants festzumachen.

    /// <summary>
    /// Pure ownership comparison (for hot paths/projections where the <c>fid</c> is determined once):
    /// An exercise belongs to an adult only if it has an author <b>and</b> that author is the adult.
    /// If the author is missing (seeded system exercise) or the <c>fid</c> is missing, the result is
    /// <c>false</c> (fail-closed) – otherwise a missing claim would wrongly unlock system exercises.
    /// </summary>
    public static bool IsOwnedBy(int? authorFatherId, int? supervisorId) =>
        authorFatherId is { } author && author == supervisorId;
}

/// <summary>
/// Ownership checks: an adult may only access their own children/plans, a child only their own.
/// </summary>
public class AuthAccess(PuglingDbContext db)
{
    // OR-basiert statt if/else: ein Konto kann Student UND Supervisor sein (perspektivisch in verschiedenen
    // Haushalten). Jede Rolle wird eigenständig geprüft; erfüllt eine, ist der Zugriff erlaubt.

    /// <summary>Does the plan belong to the logged-in user (student = own plan, supervisor = plan of a supervised child)?</summary>
    public async Task<bool> OwnsPlanAsync(ClaimsPrincipal user, StudyPlan plan, CancellationToken ct = default)
    {
        if (user.IsStudent() && plan.ChildId == user.StudentId()) return true;
        var fid = user.SupervisorId();
        return user.IsSupervisor() && fid is not null
            && await db.SupervisorLinks.AnyAsync(l => l.StudentId == plan.ChildId && l.SupervisorId == fid, ct);
    }

    /// <summary>Does the logged-in supervisor supervise this child (membership via <see cref="SupervisorLink"/>)?</summary>
    public async Task<bool> SupervisorOwnsChildAsync(ClaimsPrincipal user, int childId, CancellationToken ct = default)
    {
        var fid = user.SupervisorId();
        return fid is not null && await db.SupervisorLinks.AnyAsync(l => l.StudentId == childId && l.SupervisorId == fid, ct);
    }

    /// <summary>
    /// May the logged-in user access this child's child-related data?
    /// Student = only their own profile, supervisor = any child they supervise.
    /// </summary>
    public async Task<bool> OwnsChildAsync(ClaimsPrincipal user, int childId, CancellationToken ct = default)
    {
        if (user.IsStudent() && user.StudentId() == childId) return true;
        return user.IsSupervisor() && await SupervisorOwnsChildAsync(user, childId, ct);
    }
}
