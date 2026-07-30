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

/// <summary>PIN-Login; liefert ein JWT mit Konto-Subjekt und einer/mehreren Rollen.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.V1 + "/auth")]
[Tags("Auth")]
[Produces("application/json")]
public class AuthController(PuglingDbContext db, TokenService tokens, AccountService accounts,
    Services.Shared.PositionProgressService progress, Services.Shared.ObjectiveRewardService objectiveRewards) : ControllerBase
{
    /// <summary>
    /// Die <b>primäre Ebene</b> fürs UI-Routing – wohin der Nutzer nach dem Anmelden gehört.
    ///
    /// Rangfolge Supervisor → Creator → Student, weil sie von „darf am meisten steuern" nach „lernt selbst"
    /// verläuft: ein Vater trägt Creator <i>und</i> Supervisor und will in seine Betreuungs-Sicht, ein
    /// <b>Lehrer</b> hat nur Creator und gehört in die Werkstatt. Vorher stand hier
    /// <c>Any(p =&gt; p.Role != Student) ? Supervisor : Student</c> – das klappte Creator auf Supervisor
    /// zusammen und hätte einem Lehrer die Vater-Oberfläche vorgesetzt.
    /// </summary>
    private static string PrimaryRoleOf(IEnumerable<AccountProfile> profiles)
    {
        var roles = profiles.Select(p => p.Role).ToList();
        if (roles.Contains(ProfileRole.Supervisor)) return Roles.Supervisor;
        if (roles.Contains(ProfileRole.Creator)) return Roles.Creator;
        return Roles.Student;
    }

    /// <summary>
    /// Login per fachlicher Id + PIN. Löst das Konto auf und stellt ein Mehrrollen-Token aus.
    /// <para>
    /// Heißt <c>adult</c> und nicht <c>father</c>, weil derselbe Endpunkt ein <b>Lehrer-Konto</b> anmeldet:
    /// dessen Id ist ebenfalls eine <see cref="Adult"/>-Id, und die Antwort nennt dann <c>Creator</c> statt
    /// <c>Supervisor</c>. Ein bestehendes Konto bekommt dabei keine Rolle nachgereicht
    /// (siehe <see cref="AccountService"/>), ein Lehrer wird durch das Anmelden also nicht zum Betreuer.
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

        var account = await accounts.EnsureForFatherAsync(adult, ct);
        var (token, expires) = tokens.IssueForAccount(account, account.Profiles, isAdmin: adult.IsAdmin);
        return new LoginResponse(token, PrimaryRoleOf(account.Profiles), adult.Id, adult.Name, expires);
    }

    /// <summary>Sohn-Login per Id + PIN. Löst das Konto auf und stellt ein Rollen-Token aus.</summary>
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
    /// Kanonischer, konto-zentrischer Login: ein Token, das <b>alle</b> Rollen des Kontos trägt
    /// (z. B. Creator + Supervisor). <c>role</c> in der Antwort ist die primäre Ebene (Supervisor bzw. Student) fürs UI-Routing.
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

    /// <summary>Gibt die aktuelle Identität aus dem Token zurück (Konto, alle Rollen, fid/cid).</summary>
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
    /// Selbstverwaltung des eigenen Kontos: Anzeigename, E-Mail und PIN.
    ///
    /// <para>
    /// Liegt bei <c>auth/…</c> und nicht in einer Ebene, weil es zu keiner gehört – <b>derselbe Mensch</b>
    /// bedient es aus jeder Rolle (die dokumentierte Ausnahme in CLAUDE.md). Vorher ging das nur über
    /// <c>supervisor/adults/{id}</c>, und damit gar nicht für ein <b>Lehrer-Konto</b>: dem fehlt die
    /// Supervisor-Rolle, es konnte seine eigene PIN nicht ändern.
    /// </para>
    /// <para>
    /// Die Identität hängt an <b>zwei</b> Zeilen: die <see cref="Adult"/>-Zeile trägt den fachlichen Namen
    /// (er erscheint als Autor an den Übungen), das <see cref="Account"/> den Login. Geschrieben wird hier
    /// nur die fachliche; das Konto zieht <see cref="AccountService.MirrorAsync(Adult, CancellationToken)"/>
    /// nach – dieselbe eine Stelle, die auch <c>supervisor/adults/{id}</c> und
    /// <c>supervisor/children/{id}</c> benutzen.
    /// </para>
    /// <para>
    /// <b>Nur Erwachsene.</b> Ein Kind ändert seinen Namen und seine PIN nicht selbst: die PIN ist der
    /// Zugang, den der Vater vergibt, und ein Kind, das sie umstellt, hätte sich der Aufsicht entzogen.
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
