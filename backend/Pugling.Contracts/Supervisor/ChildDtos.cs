namespace Pugling.Contracts.Supervisor;

// Vertrag rund um das betreute Kind: Stammdaten inkl. übungsunabhängigem Profil, die Ko-Supervisoren
// (ein Student kann mehrere haben) und das gemeinsame Wallet samt manueller Vater-Buchung.

/// <summary>Ein betreutes Kind samt Profil und Kontostand beider Währungen.</summary>
public record ChildResponse(int Id, string Name, int? BirthYear, int? Grade,
    SchoolTypes SchoolType, Gender Gender, IReadOnlyList<string> Interests, string? ProfileNotes,
    DateTime CreatedAt, int Coins, int Gems);

/// <summary>Eingabe zum Anlegen eines Kindes.</summary>
public record CreateChildDto(string Name, int? BirthYear, int? Grade, SchoolTypes? SchoolType, string? Pin,
    Gender? Gender = null, List<string>? Interests = null, string? ProfileNotes = null);

/// <summary>Partielle Änderung eines Kindes; weggelassene Felder bleiben unverändert.</summary>
public record UpdateChildDto(string? Name, int? BirthYear, int? Grade, SchoolTypes? SchoolType, string? Pin,
    Gender? Gender = null, List<string>? Interests = null, string? ProfileNotes = null);

/// <summary>Eine Betreuungs-Beziehung: welcher Supervisor betreut den Studenten seit wann.</summary>
public record SupervisorLinkResponse(int SupervisorId, string SupervisorName, SupervisorRelation Relation, DateTime CreatedAt);

/// <summary>Eingabe zum Hinzufügen eines weiteren Supervisors (z. B. Mutter/Oma).</summary>
public record AddSupervisorDto(int SupervisorId, SupervisorRelation Relation = SupervisorRelation.Other);

/// <summary>Eine Punkte-Buchung im Ledger des Kindes.</summary>
public record PointsEntryResponse(int Id, int ChildId, int Amount, PointKind Kind, string Reason, DateTime CreatedAt);

/// <summary>Kontostand des Kindes (beide Währungen) mit einer Seite seiner Buchungen.</summary>
public record ChildPointsResponse(int ChildId, int Coins, int Gems, IEnumerable<PointsEntryResponse> Entries);

/// <summary>
/// Manuelle Vater-Buchung: positiver Betrag = gutschreiben/verschenken, negativ = abziehen.
/// Über die Währung kann der Vater neben Münzen auch <b>Gems verschenken</b>
/// (Belohnung außerhalb der App, Schulden-Erlass).
/// </summary>
/// <param name="Amount">Betrag; positiv = gutschreiben/verschenken, negativ = abziehen.</param>
/// <param name="Reason">Freitext-Begründung fürs Ledger.</param>
/// <param name="Currency">Zielwährung der Buchung (Default Münzen).</param>
public record PointsEntryDto(int Amount, string Reason, Currency Currency = Currency.Coins);
