using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Auth;

/// <summary>
/// Ensures that a login account with the matching roles exists for every domain profile
/// (<see cref="Adult"/>/<see cref="Child"/>) – idempotent. Used during the startup backfill, when
/// creating new adults/children, and at login (as a safety net), so that a freshly created user
/// immediately gets a token with all of their roles. PIN hashes are taken over from the Adult/Child on creation.
/// </summary>
public class AccountService(PuglingDbContext db)
{
    /// <summary>Account (incl. profiles) for the adult – Creator + Supervisor roles. Creates it idempotently.</summary>
    public Task<Account> EnsureForFatherAsync(Adult father, CancellationToken ct = default) =>
        EnsureAsync(father, supervises: true, ct);

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
    public Task<Account> EnsureForTeacherAsync(Adult teacher, CancellationToken ct = default) =>
        EnsureAsync(teacher, supervises: false, ct);

    private async Task<Account> EnsureAsync(Adult father, bool supervises, CancellationToken ct)
    {
        var account = await db.Accounts.Include(a => a.Profiles)
            .FirstOrDefaultAsync(a => a.Profiles.Any(p => p.AdultId == father.Id), ct);
        // Idempotent und **nicht** nachrüstend: ein bestehendes Konto behält seine Rollen. Sonst hätte ein
        // zweiter Registrierungs-Aufruf einem Lehrer stillschweigend den Betreuungsauftrag verliehen.
        if (account is not null) return account;

        account = new Account { DisplayName = father.Name, Email = father.Email, PinHash = father.Pin, CreatedAt = father.CreatedAt };
        account.Profiles.Add(new AccountProfile { Role = ProfileRole.Creator, AdultId = father.Id });
        if (supervises) account.Profiles.Add(new AccountProfile { Role = ProfileRole.Supervisor, AdultId = father.Id });
        db.Accounts.Add(account);
        await db.SaveChangesAsync(ct);
        return account;
    }

    /// <summary>Account (incl. profile) for the child – Student role. Creates it idempotently.</summary>
    public async Task<Account> EnsureForChildAsync(Child child, CancellationToken ct = default)
    {
        var account = await db.Accounts.Include(a => a.Profiles)
            .FirstOrDefaultAsync(a => a.Profiles.Any(p => p.ChildId == child.Id), ct);
        if (account is not null) return account;

        account = new Account { DisplayName = child.Name, Email = null, PinHash = child.Pin, CreatedAt = child.CreatedAt };
        account.Profiles.Add(new AccountProfile { Role = ProfileRole.Student, ChildId = child.Id });
        db.Accounts.Add(account);
        await db.SaveChangesAsync(ct);
        return account;
    }

    /// <summary>Loads an account with its profiles for token issuance (login via account id).</summary>
    public Task<Account?> FindWithProfilesAsync(int accountId, CancellationToken ct = default) =>
        db.Accounts.Include(a => a.Profiles).FirstOrDefaultAsync(a => a.Id == accountId, ct);
}
