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
    /// <summary>
    /// Konto (inkl. Profile) für einen Erwachsenen <b>mit</b> Betreuungsauftrag – Rollen Creator +
    /// Supervisor. Legt es idempotent an. Gegenstück: <see cref="EnsureForTeacherAsync"/>.
    /// </summary>
    public Task<Account> EnsureForAdultAsync(Adult adult, CancellationToken ct = default) =>
        EnsureAsync(adult, supervises: true, ct);

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

    private async Task<Account> EnsureAsync(Adult adult, bool supervises, CancellationToken ct)
    {
        var account = await db.Accounts.Include(a => a.Profiles)
            .FirstOrDefaultAsync(a => a.Profiles.Any(p => p.AdultId == adult.Id), ct);
        // Idempotent und **nicht** nachrüstend: ein bestehendes Konto behält seine Rollen. Sonst hätte ein
        // zweiter Registrierungs-Aufruf einem Lehrer stillschweigend den Betreuungsauftrag verliehen.
        if (account is not null) return account;

        account = new Account { DisplayName = adult.Name, Email = adult.Email, PinHash = adult.Pin, CreatedAt = adult.CreatedAt };
        account.Profiles.Add(new AccountProfile { Role = ProfileRole.Creator, AdultId = adult.Id });
        if (supervises) account.Profiles.Add(new AccountProfile { Role = ProfileRole.Supervisor, AdultId = adult.Id });
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

    /// <summary>
    /// Spiegelt Anzeigename, E-Mail und PIN-Hash des Erwachsenen auf sein Login-Konto. Die
    /// <see cref="Adult"/>-Zeile ist die <b>Quelle</b>, das Konto die Kopie – nie umgekehrt.
    /// <para>
    /// Warum es die Kopie gibt: der konto-zentrische Login (<c>POST auth/login</c>) kennt nur das Konto,
    /// und der Anzeigename wandert von dort als <c>ClaimTypes.Name</c> ins Token. Wer nur die fachliche
    /// Zeile ändert, benennt darum nichts um, was der Nutzer nach dem Anmelden sieht.
    /// </para>
    /// <para>
    /// Bei der <b>E-Mail</b> ist die Drift nicht kosmetisch: der gefilterte Unique-Index hängt an beiden
    /// Zeilen, und die Kollisionsprüfung läuft gegen das Konto. Blieb es stehen, hielt eine aufgegebene
    /// Adresse den Adressraum weiter besetzt, und eine belegte sah <i>frei</i> aus – die Prüfung ließ sie
    /// durch, der Index am <see cref="Adult"/> schlug zu, und aus dem fälligen 409 wurde ein 500 mit halb
    /// gespeichertem Zustand.
    /// </para>
    /// <para>
    /// Gespiegelt wird <b>unbedingt</b>, nicht nur das gerade geänderte Feld: „das Konto trägt, was die
    /// fachliche Zeile trägt" ist als Invariante prüfbar, „das Konto trägt, was der letzte PATCH mitschickte"
    /// nicht. Bestehende Drift heilt damit beim nächsten Schreibzugriff. Das Speichern bleibt beim Aufrufer,
    /// damit fachliche Änderung und Spiegelung in <b>einem</b> Commit landen.
    /// </para>
    /// </summary>
    public async Task MirrorAsync(Adult adult, CancellationToken ct)
    {
        var account = await EnsureForAdultAsync(adult, ct);
        account.DisplayName = adult.Name;
        account.Email = adult.Email;
        account.PinHash = adult.Pin;
    }

    /// <summary>
    /// Dasselbe für das Kind – ohne E-Mail, die hat es nicht (siehe <see cref="MirrorAsync(Adult, CancellationToken)"/>
    /// für die Begründung der Spiegelung).
    /// </summary>
    public async Task MirrorAsync(Child child, CancellationToken ct)
    {
        var account = await EnsureForChildAsync(child, ct);
        account.DisplayName = child.Name;
        account.PinHash = child.Pin;
    }

    /// <summary>Lädt ein Konto samt Profilen für die Token-Ausstellung (Login über Konto-Id).</summary>
    public Task<Account?> FindWithProfilesAsync(int accountId, CancellationToken ct = default) =>
        db.Accounts.Include(a => a.Profiles).FirstOrDefaultAsync(a => a.Id == accountId, ct);
}
