namespace Pugling.Contracts;

/// <summary>Die drei fachlichen Ebenen als Rolle – unabhängig vom Login.</summary>
public enum ProfileRole
{
    /// <summary>Erstellt Inhalte/Übungen (heute: an ein <c>Father</c>-Profil gebunden).</summary>
    Creator = 0,
    /// <summary>Steuert: Lehrpläne, Ziele/Punkte, Shop (heute: <c>Father</c>-Profil).</summary>
    Supervisor = 1,
    /// <summary>Lernt, verdient, kauft/aktiviert (heute: <c>Child</c>-Profil).</summary>
    Student = 2,
}
