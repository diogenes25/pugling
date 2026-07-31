using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers;

/// <summary>PIN login; returns a JWT with account subject and one or more roles.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.V1 + "/auth")]
[Tags("Auth")]
[Produces("application/json")]
public class AuthController(PuglingDbContext db, TokenService tokens, AccountService accounts,
    Services.Shared.PositionProgressService progress, Services.Shared.ObjectiveRewardService objectiveRewards) : ControllerBase
{
    /// <summary>
    /// The <b>primary tier</b> for UI routing – where the user belongs after logging in.
    ///
    /// Precedence Supervisor → Creator → Student, because it runs from "gets to control the most" to
    /// "learns on their own": a father carries Creator <i>and</i> Supervisor and wants their supervision
    /// view, a <b>teacher</b> has only Creator and belongs in the workshop. This used to read
    /// <c>Any(p =&gt; p.Role != Student) ? Supervisor : Student</c> – that collapsed Creator into Supervisor
    /// and would have presented a teacher with the father's UI.
    /// </summary>
    private static string PrimaryRoleOf(IEnumerable<AccountProfile> profiles)
    {
        var roles = profiles.Select(p => p.Role).ToList();
        if (roles.Contains(ProfileRole.Supervisor)) return Roles.Supervisor;
        if (roles.Contains(ProfileRole.Creator)) return Roles.Creator;
        return Roles.Student;
    }

    /// <summary>
    /// Login via domain id + PIN. Resolves the account and issues a multi-role token.
    /// <para>
    /// Named <c>adult</c> and not <c>father</c> because the same endpoint logs in a <b>teacher account</b>:
    /// its id is likewise an <see cref="Adult"/> id, and the response then names <c>Creator</c> instead of
    /// <c>Supervisor</c>. An existing account is not granted an additional role in the process
    /// (see <see cref="AccountService"/>), so a teacher does not become a supervisor by logging in.
    /// </para>
    /// </summary>
    [HttpPost("adult")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginResponse>> LoginAdult(AdultLoginDto dto, CancellationToken ct = default)
    {
        var adult = await db.Adults.FirstOrDefaultAsync(a => a.Id == dto.AdultId, ct);
        if (adult is null || !PinHasher.Verify(dto.Pin, adult.Pin)) return this.ProblemWithCode(ApiErrors.InvalidCredentials, "Invalid adult ID or PIN.");

        var account = await accounts.EnsureForAdultAsync(adult, ct);
        var (token, expires) = tokens.IssueForAccount(account, account.Profiles, isAdmin: adult.IsAdmin);
        return new LoginResponse(token, PrimaryRoleOf(account.Profiles), adult.Id, adult.Name, expires);
    }

    /// <summary>Child login via id + PIN. Resolves the account and issues a role token.</summary>
    [HttpPost("child")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginResponse>> LoginChild(ChildLoginDto dto, CancellationToken ct = default)
    {
        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == dto.ChildId, ct);
        if (child is null || !PinHasher.Verify(dto.Pin, child.Pin)) return this.ProblemWithCode(ApiErrors.InvalidCredentials, "Invalid child ID or PIN.");

        var account = await accounts.EnsureForChildAsync(child, ct);
        // Beim Einloggen offene Pflicht-Perioden nachrechnen: ein Malus fürs Nicht-Lernen landet so, bevor
        // der Sohn seinen Kontostand sieht oder etwas ausgibt (es gibt keinen Scheduler; idempotent).
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await progress.SettleClosedPeriodsAsync(child.Id, today, ct);
        // Ebenso die verdienten Belohnungen erreichter „großer Ziele" idempotent gutschreiben (Carrot),
        // damit der Sohn sie direkt beim Login auf dem Konto hat.
        await objectiveRewards.SettleAsync(child.Id, today, ct);
        var (token, expires) = tokens.IssueForAccount(account, account.Profiles);
        return new LoginResponse(token, Roles.Student, child.Id, child.Name, expires);
    }

    /// <summary>
    /// Canonical, account-centric login: a single token that carries <b>all</b> roles of the account
    /// (e.g. Creator + Supervisor). <c>role</c> in the response is the primary tier (Supervisor or Student) for UI routing.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginResponse>> Login(AccountLoginDto dto, CancellationToken ct = default)
    {
        var account = await accounts.FindWithProfilesAsync(dto.AccountId, ct);
        if (account is null || !PinHasher.Verify(dto.Pin, account.PinHash)) return this.ProblemWithCode(ApiErrors.InvalidCredentials, "Invalid account ID or PIN.");

        // Break-Glass-Admin: gilt, wenn ein an das Konto gebundener Vater als Admin markiert ist.
        var adultIds = account.Profiles.Where(p => p.AdultId is not null).Select(p => p.AdultId!.Value).ToList();
        var isAdmin = adultIds.Count > 0 && await db.Adults.AnyAsync(a => adultIds.Contains(a.Id) && a.IsAdmin, ct);
        var (token, expires) = tokens.IssueForAccount(account, account.Profiles, isAdmin);
        return new LoginResponse(token, PrimaryRoleOf(account.Profiles), account.Id, account.DisplayName, expires);
    }

    /// <summary>Returns the current identity from the token (account, all roles, fid/cid).</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<MeResponse> Me() => new MeResponse(
        AccountId: int.TryParse(User.FindFirstValue("aid"), out var aid) ? aid : null,
        // Primäre Ebene fürs UI-Routing – dieselbe Rangfolge wie beim Login (Supervisor → Creator → Student).
        // Vorher stand hier „jeder Erwachsene (auch reiner Creator) → Supervisor": ein Lehrer, der die Seite
        // neu lädt, hätte damit trotz Creator-Token die Vater-Oberfläche bekommen.
        Role: User.IsSupervisor() ? Roles.Supervisor
            : User.IsCreator() ? Roles.Creator
            : User.IsStudent() ? Roles.Student : "?",
        Roles: User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList(),
        AdultId: User.AdultId(),
        ChildId: User.ChildId(),
        Name: User.Identity?.Name);

    /// <summary>
    /// Self-management of the account itself: display name, email and PIN.
    ///
    /// <para>
    /// Lives under <c>auth/…</c> and not under a tier because it belongs to none – <b>the same person</b>
    /// operates it from any role (the documented exception in CLAUDE.md). Previously this only worked via
    /// <c>supervisor/adults/{id}</c>, and thus not at all for a <b>teacher account</b>: it lacks the
    /// Supervisor role, so it couldn't change its own PIN.
    /// </para>
    /// <para>
    /// Identity hangs off <b>two</b> rows: the <see cref="Adult"/> row carries the domain name (it appears
    /// as the author on exercises), the <see cref="Account"/> the login. Only the domain row is written
    /// here; <see cref="AccountService.MirrorAsync(Adult, CancellationToken)"/> pulls the account along –
    /// the same single place that <c>supervisor/adults/{id}</c> and <c>supervisor/children/{id}</c> use.
    /// </para>
    /// <para>
    /// <b>Adults only.</b> A child does not change its own name and PIN: the PIN is the
    /// access the father grants, and a child that changed it would have escaped supervision.
    /// </para>
    /// </summary>
    [HttpPatch("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MeResponse>> UpdateMe(UpdateMyAccountDto dto, CancellationToken ct)
    {
        if (User.AdultId() is not int fid)
            return this.ProblemWithCode(ApiErrors.Forbidden,
                "Only a grown-up account can manage itself; a child's name and PIN are set by its supervisor.");
        if (!int.TryParse(User.FindFirstValue("aid"), out var accountId))
            return this.ProblemWithCode(ApiErrors.Unauthorized, "The token carries no account.");

        var account = await db.Accounts.Include(a => a.Profiles).FirstOrDefaultAsync(a => a.Id == accountId, ct);
        var adult = await db.Adults.FirstOrDefaultAsync(a => a.Id == fid, ct);
        if (account is null || adult is null) return NotFound();

        if (dto.Name is not null)
        {
            var name = dto.Name.Trim();
            if (name.Length == 0) return this.ProblemWithCode(ApiErrors.ValidationError, "Name must not be empty.");
            adult.Name = name;
        }

        // Erst der Wert, dann der Schalter – so gewinnt „leeren", wenn ein Formular beides schickt.
        if (dto.Email is not null)
        {
            var email = dto.Email.Trim();
            if (email.Length > 0 && await db.Accounts.AnyAsync(a => a.Id != accountId && a.Email == email, ct))
                return this.ProblemWithCode(ApiErrors.Conflict, "This e-mail is already used by another account.");
            adult.Email = email.Length == 0 ? null : email;
        }
        if (dto.ClearEmail) adult.Email = null;

        if (dto.Pin is not null) adult.Pin = dto.Pin.Length == 0 ? "" : PinHasher.Hash(dto.Pin);

        // Geschrieben wird nur die fachliche Zeile; das Konto ist ihre Spiegelung (AccountService.MirrorAsync)
        // – dieselbe eine Stelle, die auch die beiden Supervisor-PATCHes benutzen. `account` ist dieselbe
        // verfolgte Instanz, die Antwort unten sieht den gespiegelten Stand also schon.
        await accounts.MirrorAsync(adult, ct);
        await db.SaveChangesAsync(ct);

        // Der Name im Token ist jetzt veraltet – die Antwort nennt darum den **gespeicherten** Stand, damit
        // die Oberfläche ihn ohne neues Token anzeigen kann.
        return new MeResponse(account.Id, PrimaryRoleOf(account.Profiles),
            account.Profiles.Select(p => p.Role.ToString()).Distinct().ToList(),
            fid, User.ChildId(), account.DisplayName);
    }
}
