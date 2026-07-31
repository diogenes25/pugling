using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Pugling.Api.Models;

namespace Pugling.Api.Auth;

/// <summary>Issues signed JWTs – with an account subject (<c>aid</c>) and one or more roles.</summary>
public class TokenService(IConfiguration config)
{
    private const int LifetimeHours = 12;

    /// <summary>Signing key from configuration (dev fallback; set via Jwt:Key in prod).</summary>
    private string Key => config["Jwt:Key"] ?? "pugling-dev-signing-key-change-me-please-0123456789";

    /// <summary>The symmetric key used to sign issued tokens (derived from <see cref="Key"/>).</summary>
    public SymmetricSecurityKey SigningKey => new(Encoding.UTF8.GetBytes(Key));

    /// <summary>
    /// The canonical way: token from an account together with its role profiles. Carries <c>aid</c> (account),
    /// one <see cref="ClaimTypes.Role"/> claim per role (Creator/Supervisor/Student), as well as <c>fid</c>
    /// (adult of the Creator/Supervisor profiles) and <c>cid</c> (child of the Student profile), where present.
    /// </summary>
    /// <param name="account">The login account for which the token is issued.</param>
    /// <param name="profiles">The account's role profiles – one tier claim per profile.</param>
    /// <param name="isAdmin">Additionally sets the <see cref="Roles.Admin"/> claim (break-glass superuser, from <see cref="Adult.IsAdmin"/>).</param>
    public (string token, DateTime expiresAt) IssueForAccount(Account account, IReadOnlyList<AccountProfile> profiles, bool isAdmin = false)
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
        if (isAdmin) claims.Add(new(ClaimTypes.Role, Roles.Admin));

        var fid = profiles.FirstOrDefault(p => p.AdultId is not null)?.AdultId;
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
