namespace Pugling.Contracts.Auth;

// Contract of the login tier (api/v1/auth/…). The records are pure transport shapes: no behavior,
// no dependency on entities - so a client can use them without the API assembly.

/// <summary>Response of all login endpoints: JWT plus primary tier for UI routing.</summary>
/// <param name="Token">The issued JWT (Bearer).</param>
/// <param name="Role">
/// Primary tier for UI routing: <c>Supervisor</c>, <c>Creator</c>, or <c>Student</c>. Ranked in this
/// order – a father holds Creator <i>and</i> Supervisor and belongs in the supervision view, a
/// <b>teacher</b> only holds Creator and belongs in the workshop. The token itself carries <i>all</i>
/// roles of the account; this field only says where the UI should start.
/// </param>
/// <param name="Id">Domain id of the logged-in profile (adult or child id; account id for account login).</param>
/// <param name="Name">Display name.</param>
/// <param name="ExpiresAt">Expiry of the token (UTC).</param>
public record LoginResponse(string Token, string Role, int Id, string Name, DateTime ExpiresAt);

/// <summary>
/// The caller's own identity from the token (<c>GET auth/me</c>) – account, all roles, and the domain ids.
/// </summary>
/// <param name="AccountId">Account id (subject of the token); <c>null</c> for a legacy token without <c>aid</c>.</param>
/// <param name="Role">Primary tier for routing – see <see cref="LoginResponse"/>.</param>
/// <param name="Roles">All roles of the token. For a teacher account, exactly <c>["Creator"]</c>.</param>
/// <param name="AdultId">Domain id of the adult (Creator/Supervisor), otherwise <c>null</c>.</param>
/// <param name="ChildId">Domain id of the child (Student), otherwise <c>null</c>.</param>
/// <param name="Name">Display name.</param>
public record MeResponse(int? AccountId, string Role, IReadOnlyList<string> Roles,
    int? AdultId, int? ChildId, string? Name);

/// <summary>
/// Self-service management of the caller's own account (<c>PATCH auth/me</c>) – for <b>every</b> adult
/// role, including a teacher account that has no access to the supervisor endpoints.
///
/// <para>
/// <b>PATCH semantics:</b> <c>null</c> means "not specified" (the value stays). Email is the only
/// clearable field and needs <see cref="ClearEmail"/> for that – without the switch, a form with an
/// empty field would report "saved" while the old address stayed in place.
/// </para>
/// </summary>
/// <param name="Name">New display name; also appears as the author on the account's own exercises.</param>
/// <param name="Email">New email. Must be unique account-wide.</param>
/// <param name="ClearEmail">Remove the email. Wins over <paramref name="Email"/> if both are sent.</param>
/// <param name="Pin">New login PIN (will be hashed). Empty string = remove the PIN, so an account can
/// be deliberately deactivated; <c>null</c> = unchanged.</param>
public record UpdateMyAccountDto(string? Name, string? Email, bool ClearEmail = false, string? Pin = null);

/// <summary>Login of an adult (father or teacher) via domain adult id + PIN.</summary>
public record AdultLoginDto(int AdultId, string Pin);

/// <summary>Child login via domain child id + PIN.</summary>
public record ChildLoginDto(int ChildId, string Pin);

/// <summary>Account-centric login: one token across all roles of the account.</summary>
public record AccountLoginDto(int AccountId, string Pin);
