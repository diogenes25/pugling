using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;

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
    /// <summary>Vater-Login per Id + PIN. Löst das Konto auf und stellt ein Mehrrollen-Token aus.</summary>
    [HttpPost("father")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginResponse>> LoginFather(FatherLoginDto dto)
    {
        var father = await db.Fathers.FirstOrDefaultAsync(f => f.Id == dto.FatherId);
        if (father is null || !PinHasher.Verify(dto.Pin, father.Pin)) return this.ProblemWithCode(ApiErrors.InvalidCredentials, "Invalid father ID or PIN.");

        var account = await accounts.EnsureForFatherAsync(father);
        var (token, expires) = tokens.IssueForAccount(account, account.Profiles, isAdmin: father.IsAdmin);
        return new LoginResponse(token, Roles.Supervisor, father.Id, father.Name, expires);
    }

    /// <summary>Sohn-Login per Id + PIN. Löst das Konto auf und stellt ein Rollen-Token aus.</summary>
    [HttpPost("child")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginResponse>> LoginChild(ChildLoginDto dto)
    {
        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == dto.ChildId);
        if (child is null || !PinHasher.Verify(dto.Pin, child.Pin)) return this.ProblemWithCode(ApiErrors.InvalidCredentials, "Invalid child ID or PIN.");

        var account = await accounts.EnsureForChildAsync(child);
        // Beim Einloggen offene Pflicht-Perioden nachrechnen: ein Malus fürs Nicht-Lernen landet so, bevor
        // der Sohn seinen Kontostand sieht oder etwas ausgibt (es gibt keinen Scheduler; idempotent).
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await progress.SettleClosedPeriodsAsync(child.Id, today);
        // Ebenso die verdienten Belohnungen erreichter „großer Ziele" idempotent gutschreiben (Carrot),
        // damit der Sohn sie direkt beim Login auf dem Konto hat.
        await objectiveRewards.SettleAsync(child.Id, today);
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
    public async Task<ActionResult<LoginResponse>> Login(AccountLoginDto dto)
    {
        var account = await accounts.FindWithProfilesAsync(dto.AccountId);
        if (account is null || !PinHasher.Verify(dto.Pin, account.PinHash)) return this.ProblemWithCode(ApiErrors.InvalidCredentials, "Invalid account ID or PIN.");

        // Break-Glass-Admin: gilt, wenn ein an das Konto gebundener Vater als Admin markiert ist.
        var fatherIds = account.Profiles.Where(p => p.FatherId is not null).Select(p => p.FatherId!.Value).ToList();
        var isAdmin = fatherIds.Count > 0 && await db.Fathers.AnyAsync(f => fatherIds.Contains(f.Id) && f.IsAdmin);
        var (token, expires) = tokens.IssueForAccount(account, account.Profiles, isAdmin);
        var primaryRole = account.Profiles.Any(p => p.Role != ProfileRole.Student) ? Roles.Supervisor : Roles.Student;
        return new LoginResponse(token, primaryRole, account.Id, account.DisplayName, expires);
    }

    /// <summary>Gibt die aktuelle Identität aus dem Token zurück (Konto, alle Rollen, fid/cid).</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<object> Me() => new
    {
        AccountId = int.TryParse(User.FindFirstValue("aid"), out var aid) ? aid : (int?)null,
        // Primäre Ebene fürs UI-Routing: Student → Student, jeder Erwachsene (auch reiner Creator) → Supervisor.
        Role = User.IsStudent() ? Roles.Student : User.IsSupervisor() || User.IsCreator() ? Roles.Supervisor : "?",
        Roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToArray(),
        FatherId = User.FatherId(),
        ChildId = User.ChildId(),
        Name = User.Identity?.Name,
    };
}
