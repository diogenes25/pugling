namespace Pugling.Api.Models;

// Identity tier: ONE login account (Account) can carry SEVERAL roles (creator/supervisor/student),
// decoupled from the domain profiles Adult/Child. That way an adult is creator and supervisor at once, and
// eventually one person can be supervisor in one household and student in another.
// The ids of Adult/Child stay untouched (every domain FK hangs on them); the account sits above.
// See docs/grundprinzip.md.

// ProfileRole lives in the contract project (Pugling.Contracts).

/// <summary>
/// Login account: holds a person's credentials (PIN hash). Through <see cref="Profiles"/> it carries one or
/// more roles. The roles point at the domain profiles <see cref="Adult"/>/<see cref="Child"/>.
/// </summary>
public class Account
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = "";
    /// <summary>Optional (children have no e-mail today). Unique when set.</summary>
    public string? Email { get; set; }
    /// <summary>PIN hash in the format of <see cref="Auth.PinHasher"/> (accepts legacy plaintext on verify).</summary>
    public string PinHash { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<AccountProfile> Profiles { get; set; } = new();
}

/// <summary>
/// One role membership of an account: (account, role) → domain profile. Exactly one of
/// <see cref="AdultId"/>/<see cref="ChildId"/> is set (Creator/Supervisor → Adult, Student → Child).
/// More roles = more rows; that is why the multi-supervisor extension needs no schema reshape.
/// </summary>
public class AccountProfile
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public Account? Account { get; set; }
    public ProfileRole Role { get; set; }
    /// <summary>Set for <see cref="ProfileRole.Creator"/>/<see cref="ProfileRole.Supervisor"/>.</summary>
    public int? AdultId { get; set; }
    public Adult? Adult { get; set; }
    /// <summary>Set for <see cref="ProfileRole.Student"/>.</summary>
    public int? ChildId { get; set; }
    public Child? Child { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
