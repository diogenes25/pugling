using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Pugling.Api.Auth;

/// <summary>Stellt signierte JWTs für Vater/Sohn aus.</summary>
public class TokenService(IConfiguration config)
{
    private const int LifetimeHours = 12;

    /// <summary>Signierschlüssel aus Konfiguration (Dev-Fallback; in Prod über Jwt:Key setzen).</summary>
    private string Key => config["Jwt:Key"] ?? "pugling-dev-signing-key-change-me-please-0123456789";

    public SymmetricSecurityKey SigningKey => new(Encoding.UTF8.GetBytes(Key));

    public (string token, DateTime expiresAt) IssueForFather(int fatherId, string name)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, fatherId.ToString()),
            new(ClaimTypes.Name, name),
            new(ClaimTypes.Role, Roles.Vater),
            new("fid", fatherId.ToString()),
        };
        return Issue(claims);
    }

    public (string token, DateTime expiresAt) IssueForChild(int childId, int fatherId, string name)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, childId.ToString()),
            new(ClaimTypes.Name, name),
            new(ClaimTypes.Role, Roles.Sohn),
            new("cid", childId.ToString()),
            new("fid", fatherId.ToString()),
        };
        return Issue(claims);
    }

    private (string, DateTime) Issue(List<Claim> claims)
    {
        var expires = DateTime.UtcNow.AddHours(LifetimeHours);
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256),
        };
        return (new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }
}
