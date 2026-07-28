namespace Pugling.Api.Models;

// Identitäts-Ebene: EIN Login-Konto (Account) kann MEHRERE Rollen tragen (Creator/Supervisor/Student),
// entkoppelt von den fachlichen Profilen Adult/Child. So ist ein Vater zugleich Creator und Supervisor,
// und ein Mensch kann perspektivisch in einem Haushalt Supervisor und in einem anderen Student sein.
// Die IDs von Adult/Child bleiben unangetastet (jede Fach-FK hängt daran); der Account sitzt darüber.
// Siehe docs/grundprinzip.md.

// ProfileRole lebt im Vertrags-Projekt (Pugling.Contracts).

/// <summary>
/// Login-Konto: hält die Zugangsdaten (PIN-Hash) einer Person. Über <see cref="Profiles"/> trägt es
/// eine oder mehrere Rollen. Die Rollen zeigen auf die fachlichen Profile <see cref="Adult"/>/<see cref="Child"/>.
/// </summary>
public class Account
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = "";
    /// <summary>Optional (Kinder haben heute keine E-Mail). Wenn gesetzt, eindeutig.</summary>
    public string? Email { get; set; }
    /// <summary>PIN-Hash im Format von <see cref="Auth.PinHasher"/> (akzeptiert Alt-Klartext beim Verify).</summary>
    public string PinHash { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<AccountProfile> Profiles { get; set; } = new();
}

/// <summary>
/// Eine Rollen-Mitgliedschaft eines Kontos: (Konto, Rolle) → fachliches Profil. Genau eines von
/// <see cref="AdultId"/>/<see cref="ChildId"/> ist gesetzt (Creator/Supervisor → Adult, Student → Child).
/// Mehr Rollen = mehr Zeilen; die Multi-Supervisor-Erweiterung braucht dadurch kein Schema-Reshape.
/// </summary>
public class AccountProfile
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public Account? Account { get; set; }
    public ProfileRole Role { get; set; }
    /// <summary>Gesetzt für <see cref="ProfileRole.Creator"/>/<see cref="ProfileRole.Supervisor"/>.</summary>
    public int? AdultId { get; set; }
    public Adult? Adult { get; set; }
    /// <summary>Gesetzt für <see cref="ProfileRole.Student"/>.</summary>
    public int? ChildId { get; set; }
    public Child? Child { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
