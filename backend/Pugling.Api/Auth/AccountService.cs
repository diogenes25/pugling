using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Auth;

/// <summary>
/// Sorgt dafür, dass zu jedem fachlichen Profil (<see cref="Father"/>/<see cref="Child"/>) ein Login-Konto
/// mit den passenden Rollen existiert – idempotent. Genutzt beim Start-Backfill, beim Anlegen neuer
/// Väter/Kinder und beim Login (als Sicherheitsnetz), damit ein frisch angelegter Nutzer sofort ein
/// Token mit allen seinen Rollen erhält. PIN-Hashes werden beim Anlegen vom Father/Child übernommen.
/// </summary>
public class AccountService(PuglingDbContext db)
{
    /// <summary>Konto (inkl. Profile) für den Vater – Rollen Creator + Supervisor. Legt es idempotent an.</summary>
    public async Task<Account> EnsureForFatherAsync(Father father, CancellationToken ct = default)
    {
        var account = await db.Accounts.Include(a => a.Profiles)
            .FirstOrDefaultAsync(a => a.Profiles.Any(p => p.FatherId == father.Id), ct);
        if (account is not null) return account;

        account = new Account { DisplayName = father.Name, Email = father.Email, PinHash = father.Pin, CreatedAt = father.CreatedAt };
        account.Profiles.Add(new AccountProfile { Role = ProfileRole.Creator, FatherId = father.Id });
        account.Profiles.Add(new AccountProfile { Role = ProfileRole.Supervisor, FatherId = father.Id });
        db.Accounts.Add(account);
        await db.SaveChangesAsync(ct);
        return account;
    }

    /// <summary>Konto (inkl. Profil) für das Kind – Rolle Student. Legt es idempotent an.</summary>
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

    /// <summary>Lädt ein Konto samt Profilen für die Token-Ausstellung (Login über Konto-Id).</summary>
    public Task<Account?> FindWithProfilesAsync(int accountId, CancellationToken ct = default) =>
        db.Accounts.Include(a => a.Profiles).FirstOrDefaultAsync(a => a.Id == accountId, ct);
}
