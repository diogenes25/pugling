using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;

namespace Pugling.Api.Controllers;

/// <summary>PIN-Login für Vater und Sohn; liefert ein JWT mit Rollen-Claim.</summary>
[ApiController]
[Route("api/auth")]
[Tags("Auth")]
[Produces("application/json")]
public class AuthController(PuglingDbContext db, TokenService tokens) : ControllerBase
{
    public record LoginResponse(string Token, string Role, int Id, string Name, DateTime ExpiresAt);

    public record FatherLoginDto(int FatherId, string Pin);

    /// <summary>Vater-Login per Id + PIN.</summary>
    [HttpPost("father")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> LoginFather(FatherLoginDto dto)
    {
        var father = await db.Fathers.FirstOrDefaultAsync(f => f.Id == dto.FatherId);
        if (father is null || father.Pin != dto.Pin) return Unauthorized("Vater-Id oder PIN falsch.");

        var (token, expires) = tokens.IssueForFather(father.Id, father.Name);
        return new LoginResponse(token, Roles.Vater, father.Id, father.Name, expires);
    }

    public record ChildLoginDto(int ChildId, string Pin);

    /// <summary>Sohn-Login per Id + PIN.</summary>
    [HttpPost("child")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> LoginChild(ChildLoginDto dto)
    {
        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == dto.ChildId);
        if (child is null || child.Pin != dto.Pin) return Unauthorized("Kind-Id oder PIN falsch.");

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
