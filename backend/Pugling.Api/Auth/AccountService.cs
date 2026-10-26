using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Auth;

/// <summary>
/// Ensures that a login account with the matching roles exists for every domain profile
/// (<see cref="Adult"/>/<see cref="Child"/>) – idempotent. Used during the startup backfill, when
/// creating new adults/children, so that a freshly created user immediately gets a token with all of their roles.
/// <para>
/// The account is the only home of the PIN (B-230): the plaintext PIN goes in on creation and is hashed here, and
/// <see cref="SetPin"/> changes it later. The profile rows carry no copy, so no write path can forget a mirror.
/// </para>
/// </summary>
public class AccountService(PuglingDbContext db)
{
    /// <summary>
    /// Account (incl. profiles) for an adult <b>with</b> a supervision mandate – Creator + Supervisor
    /// roles. Creates it idempotently. Counterpart: <see cref="EnsureForTeacherAsync"/>.
    /// </summary>
    public Task<Account> EnsureForAdultAsync(Adult adult, string? pin, CancellationToken ct = default) =>
        EnsureAsync(adult, pin, supervises: true, ct);

    /// <summary>
    /// Account for a <b>teacher</b>: role <see cref="ProfileRole.Creator"/> – and <b>no</b>
    /// Supervisor role. So their token carries no Supervisor claim, and all supervision endpoints
    /// (<c>[Authorize(Roles = Roles.Supervisor)]</c>) reject them without needing any special-case rule anywhere.
    /// <para>
    /// Domain-wise they still hang off an <see cref="Adult"/> row – authorship
    /// (<c>Exercise.AuthorAdultId</c>) and the RWX permissions (<c>ExerciseGrant.CreatorId</c>) attach to that.
    /// A teacher is thus not a new entity type, but <b>an adult without a supervision mandate</b>. The roles
    /// are decoupled from the login (see docs/grundprinzip.md); this exact decoupling is exploited here for
    /// the first time, instead of working around it with a parallel identity.
    /// </para>
    /// </summary>
    public Task<Account> EnsureForTeacherAsync(Adult teacher, string? pin, CancellationToken ct = default) =>
        EnsureAsync(teacher, pin, supervises: false, ct);

    private async Task<Account> EnsureAsync(Adult adult, string? pin, bool supervises, CancellationToken ct)
    {
        var account = await db.Accounts.Include(a => a.Profiles)
            .FirstOrDefaultAsync(a => a.Profiles.Any(p => p.AdultId == adult.Id), ct);
        // Idempotent and **not** retrofitting: an existing account keeps its roles and its PIN. Otherwise a second
        // registration call would silently grant a teacher a supervision assignment.
        if (account is not null) return account;

        account = new Account { DisplayName = adult.Name, Email = adult.Email, PinHash = HashOrEmpty(pin), CreatedAt = adult.CreatedAt };
        account.Profiles.Add(new AccountProfile { Role = ProfileRole.Creator, AdultId = adult.Id });
        if (supervises) account.Profiles.Add(new AccountProfile { Role = ProfileRole.Supervisor, AdultId = adult.Id });
        db.Accounts.Add(account);
        await db.SaveChangesAsync(ct);
        return account;
    }

    /// <summary>Account (incl. profile) for the child – Student role. Creates it idempotently.</summary>
    public async Task<Account> EnsureForChildAsync(Child child, string? pin, CancellationToken ct = default)
    {
        var account = await db.Accounts.Include(a => a.Profiles)
            .FirstOrDefaultAsync(a => a.Profiles.Any(p => p.ChildId == child.Id), ct);
        if (account is not null) return account;

        account = new Account { DisplayName = child.Name, Email = null, PinHash = HashOrEmpty(pin), CreatedAt = child.CreatedAt };
        account.Profiles.Add(new AccountProfile { Role = ProfileRole.Student, ChildId = child.Id });
        db.Accounts.Add(account);
        await db.SaveChangesAsync(ct);
        return account;
    }

    /// <summary>
    /// Sets the account's PIN: hashed, or empty to disable the login. Saving stays with the caller, so that the
    /// change lands in the same commit as whatever else the request writes.
    /// </summary>
    public static void SetPin(Account account, string pin) => account.PinHash = HashOrEmpty(pin);

    private static string HashOrEmpty(string? pin) => string.IsNullOrEmpty(pin) ? "" : PinHasher.Hash(pin);

    /// <summary>The login account of an adult (null if there is none).</summary>
    public Task<Account?> FindForAdultAsync(int adultId, CancellationToken ct) =>
        db.Accounts.Include(a => a.Profiles).FirstOrDefaultAsync(a => a.Profiles.Any(p => p.AdultId == adultId), ct);

    /// <summary>The login account of a child (null if there is none).</summary>
    public Task<Account?> FindForChildAsync(int childId, CancellationToken ct) =>
        db.Accounts.Include(a => a.Profiles).FirstOrDefaultAsync(a => a.Profiles.Any(p => p.ChildId == childId), ct);

    /// <summary>
    /// Mirrors the adult's display name and e-mail onto their login account. The
    /// <see cref="Adult"/> row is the <b>source</b>, the account the copy – never the other way round.
    /// <para>
    /// Why the copy exists: the account-centric login (<c>POST auth/login</c>) only knows the account,
    /// and the display name travels from there into the token as <c>ClaimTypes.Name</c>. Changing only the
    /// domain row therefore renames nothing that the user sees after signing in.
    /// </para>
    /// <para>
    /// For the <b>e-mail</b> the drift is not cosmetic: the filtered unique index hangs off both rows, and
    /// the collision check runs against the account. If it went stale, an abandoned address kept occupying
    /// the address space, and a taken one looked <i>free</i> – the check let it through, the index on the
    /// <see cref="Adult"/> struck, and the 409 that was due became a 500 with half-saved state.
    /// </para>
    /// <para>
    /// Mirroring is <b>unconditional</b>, not limited to the field just changed: "the account carries what
    /// the domain row carries" is checkable as an invariant, "the account carries whatever the last PATCH
    /// sent along" is not. Existing drift therefore heals on the next write. Saving stays with the caller,
    /// so that the domain change and the mirroring land in <b>one</b> commit.
    /// </para>
    /// </summary>
    public async Task<Account> MirrorAsync(Adult adult, CancellationToken ct)
    {
        var account = await EnsureForAdultAsync(adult, pin: null, ct);
        account.DisplayName = adult.Name;
        account.Email = adult.Email;
        return account;
    }

    /// <summary>
    /// The same for the child – without an e-mail, which it does not have (see
    /// <see cref="MirrorAsync(Adult, CancellationToken)"/> for why the mirroring exists).
    /// </summary>
    public async Task<Account> MirrorAsync(Child child, CancellationToken ct)
    {
        var account = await EnsureForChildAsync(child, pin: null, ct);
        account.DisplayName = child.Name;
        return account;
    }

    /// <summary>Loads an account with its profiles for token issuance (login via account id).</summary>
    public Task<Account?> FindWithProfilesAsync(int accountId, CancellationToken ct = default) =>
        db.Accounts.Include(a => a.Profiles).FirstOrDefaultAsync(a => a.Id == accountId, ct);
}
