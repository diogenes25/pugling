using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;

namespace Pugling.Api.Controllers;

/// <summary>PIN-Login für Vater und Sohn; liefert ein JWT mit Rollen-Claim.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.V1 + "/auth")]
[Tags("Auth")]
[Produces("application/json")]
public class AuthController(PuglingDbContext db, TokenService tokens) : ControllerBase
{
    public record LoginResponse(string Token, string Role, int Id, string Name, DateTime ExpiresAt);

    public record FatherLoginDto(int FatherId, string Pin);

    /// <summary>Vater-Login per Id + PIN.</summary>
    [HttpPost("father")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginResponse>> LoginFather(FatherLoginDto dto)
    {
        var father = await db.Fathers.FirstOrDefaultAsync(f => f.Id == dto.FatherId);
        if (father is null || !PinHasher.Verify(dto.Pin, father.Pin)) return this.ProblemWithCode(ApiErrors.InvalidCredentials, "Invalid father ID or PIN.");

        var (token, expires) = tokens.IssueForFather(father.Id, father.Name);
        return new LoginResponse(token, Roles.Vater, father.Id, father.Name, expires);
    }

    public record ChildLoginDto(int ChildId, string Pin);

    /// <summary>Sohn-Login per Id + PIN.</summary>
    [HttpPost("child")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginResponse>> LoginChild(ChildLoginDto dto)
    {
        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == dto.ChildId);
        if (child is null || !PinHasher.Verify(dto.Pin, child.Pin)) return this.ProblemWithCode(ApiErrors.InvalidCredentials, "Invalid child ID or PIN.");

        var (token, expires) = tokens.IssueForChild(child.Id, child.FatherId, child.Name);
        return new LoginResponse(token, Roles.Sohn, child.Id, child.Name, expires);
    }

    /// <summary>Gibt die aktuelle Identität aus dem Token zurück (Debug/Selbstauskunft).</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<object> Me() => new
    {
        Role = User.IsFather() ? Roles.Vater : User.IsChild() ? Roles.Sohn : "?",
        FatherId = User.FatherId(),
        ChildId = User.ChildId(),
        Name = User.Identity?.Name,
    };
}
