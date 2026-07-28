using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Auth;

/// <summary>
/// Sorgt dafür, dass zu jedem fachlichen Profil (<see cref="Adult"/>/<see cref="Child"/>) ein Login-Konto
/// mit den passenden Rollen existiert – idempotent. Genutzt beim Start-Backfill, beim Anlegen neuer
/// Väter/Kinder und beim Login (als Sicherheitsnetz), damit ein frisch angelegter Nutzer sofort ein
/// Token mit allen seinen Rollen erhält. PIN-Hashes werden beim Anlegen vom Adult/Child übernommen.
/// </summary>
public class AccountService(PuglingDbContext db)
{
    /// <summary>Konto (inkl. Profile) für den Vater – Rollen Creator + Supervisor. Legt es idempotent an.</summary>
    public Task<Account> EnsureForFatherAsync(Adult father, CancellationToken ct = default) =>
        EnsureAsync(father, supervises: true, ct);

    /// <summary>
    /// Konto für einen <b>Lehrer</b>: Rolle <see cref="ProfileRole.Creator"/> – und <b>keine</b>
    /// Supervisor-Rolle. Damit trägt sein Token keinen Supervisor-Claim, und alle Betreuungs-Endpunkte
    /// (<c>[Authorize(Roles = Roles.Supervisor)]</c>) weisen ihn ab, ohne dass irgendwo eine Sonderregel nötig wäre.
    /// <para>
    /// Fachlich hängt er weiter an einer <see cref="Adult"/>-Zeile – daran hängen Autorschaft
    /// (<c>Exercise.AuthorAdultId</c>) und die RWX-Rechte (<c>ExerciseGrant.CreatorId</c>). Ein Lehrer ist
    /// also kein neuer Entitätstyp, sondern <b>ein Erwachsener ohne Betreuungsauftrag</b>. Die Rollen sind
    /// vom Login entkoppelt (siehe docs/grundprinzip.md); genau diese Entkopplung wird hier zum ersten Mal
    /// ausgenutzt, statt sie mit einer parallelen Identität zu umgehen.
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
