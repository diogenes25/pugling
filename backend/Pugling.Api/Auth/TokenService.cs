using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Pugling.Api.Models;

namespace Pugling.Api.Auth;

/// <summary>Stellt signierte JWTs aus – mit Konto-Subjekt (<c>aid</c>) und einer/mehreren Rollen.</summary>
public class TokenService(IConfiguration config)
{
    private const int LifetimeHours = 12;

    /// <summary>Signierschlüssel aus Konfiguration (Dev-Fallback; in Prod über Jwt:Key setzen).</summary>
    private string Key => config["Jwt:Key"] ?? "pugling-dev-signing-key-change-me-please-0123456789";

    public SymmetricSecurityKey SigningKey => new(Encoding.UTF8.GetBytes(Key));

    /// <summary>
    /// Der kanonische Weg: Token aus einem Konto samt seiner Rollen-Profile. Trägt <c>aid</c> (Konto),
    /// je Rolle einen <see cref="ClaimTypes.Role"/>-Claim (Creator/Supervisor/Student) sowie <c>fid</c>
    /// (Father der Creator/Supervisor-Profile) und <c>cid</c> (Child des Student-Profils), soweit vorhanden.
    /// </summary>
    public (string token, DateTime expiresAt) IssueForAccount(Account account, IReadOnlyList<AccountProfile> profiles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new("aid", account.Id.ToString()),
            new(ClaimTypes.Name, account.DisplayName),
        };

        var roles = profiles.Select(p => p.Role).Distinct().ToList();
        if (roles.Contains(ProfileRole.Creator)) claims.Add(new(ClaimTypes.Role, Roles.Creator));
        if (roles.Contains(ProfileRole.Supervisor)) claims.Add(new(ClaimTypes.Role, Roles.Supervisor));
        if (roles.Contains(ProfileRole.Student)) claims.Add(new(ClaimTypes.Role, Roles.Student));

        var fid = profiles.FirstOrDefault(p => p.FatherId is not null)?.FatherId;
        if (fid is not null) claims.Add(new("fid", fid.Value.ToString()));
        var cid = profiles.FirstOrDefault(p => p.ChildId is not null)?.ChildId;
        if (cid is not null) claims.Add(new("cid", cid.Value.ToString()));

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
