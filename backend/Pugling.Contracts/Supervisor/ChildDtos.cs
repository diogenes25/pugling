namespace Pugling.Contracts.Supervisor;

// Vertrag rund um das betreute Kind: Stammdaten inkl. übungsunabhängigem Profil, die Ko-Supervisoren
// (ein Student kann mehrere haben) und das gemeinsame Wallet samt manueller Vater-Buchung.

/// <summary>
/// Ein betreutes Kind samt Profil und Kontostand beider Währungen. <c>Interests</c> ist der <b>freie</b>
/// Teil des Profils (Sprache des KI-Creators); die gewichteten, referenzierten Interessen für die
/// Bildauswahl stehen unter <c>children/{id}/interests</c>. <c>AllowedContentRating</c> ist die
/// Obergrenze der Bild-Eignung – nur der Supervisor darf sie heben.
/// </summary>
public record ChildResponse(int Id, string Name, int? BirthYear, int? Grade,
    SchoolTypes SchoolType, Gender Gender, IReadOnlyList<string> Interests, string? ProfileNotes,
    ContentRating AllowedContentRating, DateTime CreatedAt, int Coins, int Gems);

/// <summary>Eingabe zum Anlegen eines Kindes.</summary>
public record CreateChildDto(string Name, int? BirthYear, int? Grade, SchoolTypes? SchoolType, string? Pin,
    Gender? Gender = null, List<string>? Interests = null, string? ProfileNotes = null,
    ContentRating? AllowedContentRating = null);

/// <summary>
/// Partielle Änderung eines Kindes; weggelassene Felder bleiben unverändert. <c>null</c> heißt „nicht
/// angegeben" und kann darum nichts <b>leeren</b> – dafür stehen die <c>Clear…</c>-Schalter (vgl.
/// <c>ClearGrade</c> an der Klassenarbeit).
/// </summary>
/// <param name="ClearBirthYear">Entfernt das Geburtsjahr.</param>
/// <param name="ClearGrade">Entfernt die Klassenstufe (das Kind wird damit aus Klassen-Filtern herausgenommen).</param>
public record UpdateChildDto(string? Name, int? BirthYear, int? Grade, SchoolTypes? SchoolType, string? Pin,
    Gender? Gender = null, List<string>? Interests = null, string? ProfileNotes = null,
    ContentRating? AllowedContentRating = null,
    bool ClearBirthYear = false, bool ClearGrade = false);

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
