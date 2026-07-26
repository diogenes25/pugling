namespace Pugling.Contracts.Creator;

// Vertrag der Creator-Profile (Route api/v1/creator/profiles): der „Fachlehrer" – Fach, Schulzweig,
// Klassenstufen, optional eine Buchreihe, dazu Persona und Didaktik für den KI-Creator.

/// <summary>Ein Creator-Profil samt eigener Rechte-Sicht.</summary>
/// <param name="IsOwn">Ob das aufrufende Konto das Profil ändern darf.</param>
/// <param name="DefaultTypes">Bevorzugte Übungstypen (Schlüssel aus dem Typ-Manifest).</param>
public record CreatorProfileResponse(int Id, string Name, int? OwnerFatherId, bool IsOwn,
    string? SubjectName, int? SubjectId, SchoolTypes SchoolTypes, int? GradeMin, int? GradeMax,
    int? SeriesId, string? SeriesName, string SourceLang, string TargetLang,
    string? Persona, string? Didactics, IReadOnlyList<string> DefaultTypes, bool Active, DateTime CreatedAt);

/// <summary>Eingabe zum Anlegen eines Profils. Nur <c>Name</c> ist Pflicht – alles andere verengt die Passung.</summary>
public record CreateCreatorProfileDto(string Name, string? SubjectName, int? SubjectId,
    SchoolTypes? SchoolTypes, int? GradeMin, int? GradeMax, int? SeriesId,
    string? SourceLang, string? TargetLang, string? Persona, string? Didactics,
    List<string>? DefaultTypes, bool? Active);

/// <summary>Partielle Änderung eines Profils; weggelassene Felder bleiben unverändert.</summary>
public record UpdateCreatorProfileDto(string? Name, string? SubjectName, int? SubjectId,
    SchoolTypes? SchoolTypes, int? GradeMin, int? GradeMax, int? SeriesId,
    string? SourceLang, string? TargetLang, string? Persona, string? Didactics,
    List<string>? DefaultTypes, bool? Active);

/// <summary>
/// Ein Treffer der Profil-Suche zu einem Kind. <paramref name="Score"/> ist deterministisch berechnet
/// (Reihen-Treffer wiegt am schwersten), <paramref name="Reasons"/> nennt die Gründe im Klartext –
/// damit eine Konsole oder ein UI die Wahl erklären kann, statt eine nackte Zahl zu zeigen.
/// </summary>
public record CreatorProfileMatch(CreatorProfileResponse Profile, int Score, IReadOnlyList<string> Reasons);
