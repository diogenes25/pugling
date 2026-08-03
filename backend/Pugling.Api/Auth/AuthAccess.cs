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
    /// <summary>
    /// Both adult tiers for one <c>[Authorize(Roles = …)]</c> – "any adult, but no student". Comma-separated
    /// because the attribute reads its value that way (OR), and a single attribute is the only form that
    /// works: two <c>[Authorize]</c> attributes would be AND-ed and lock out a teacher account, which
    /// carries <see cref="Creator"/> alone. Use it where an endpoint hands out material a student must not
    /// see, yet sits below a route prefix that a student legitimately calls for other actions.
    /// <para>
    /// Do <b>not</b> "simplify" this to <see cref="Creator"/>: today every adult account gets a Creator
    /// profile and a Supervisor one only in addition (<c>AccountService.EnsureForAdultAsync</c>), so the
    /// second half happens to be redundant – but the rule being expressed is "any adult", and a
    /// supervisor-only account would silently lose access the day it exists.
    /// </para>
    /// </summary>
    public const string AnyAdult = Creator + "," + Supervisor;
    /// <summary>Platform superuser (break-glass). Bypasses the RWX permission check on exercises – e.g. to
    /// edit orphaned (ownerless) exercises in an emergency. Not granted via the API but set through
    /// the <see cref="Adult.IsAdmin"/> flag (DB/seed) and issued as a role claim at login.</summary>
    public const string Admin = "Admin";
}

/// <summary>Access to identity from the JWT.</summary>
public static class ClaimsPrincipalExtensions
{
    // Entity ids from the token: fid carries both the creator and the supervisor profile (one adult = one
    // Adult); cid carries the student profile. (Adult/Child are the domain entities, not the roles - the
    // roles are called Creator/Supervisor/Student.)
    //
    // The claim is still called `fid` although the entity is called `Adult`: it sits in already issued
    // tokens. Renaming it would invalidate every open session - for a name nobody sees. The accessor is
    // called `AdultId()` so that the code speaks the right language.
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

    // Tier roles and their target ids.
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

    // `Owns(this ClaimsPrincipal, Exercise)` was removed: it claimed to be "the one place" of the authorship
    // rule but after the RWX rebuild nobody called it - enforcement runs exclusively through the grants (see
    // ExercisePermissionService). A dead helper with exactly that comment is worse than none: it invites you
    // to pin the rights check on it instead of on the grants.

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
    // OR-based instead of if/else: an account can be student AND supervisor (in different households,
    // eventually). Every role is checked on its own; if one holds, access is granted.

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
