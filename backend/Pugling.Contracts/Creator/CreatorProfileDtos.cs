namespace Pugling.Contracts.Creator;

// Vertrag der Creator-Profile (Route api/v1/creator/profiles): der „Fachlehrer" – Fach, Schulzweig,
// Klassenstufen, optional eine Buchreihe, dazu Persona und Didaktik für den KI-Creator.

/// <summary>
/// Ein Creator-Profil samt eigener Rechte-Sicht. <c>IsOwn</c> sagt, ob das aufrufende Konto das Profil
/// ändern darf; <c>DefaultTypes</c> sind die bevorzugten Übungstypen (Schlüssel aus dem Typ-Manifest).
/// </summary>
public record CreatorProfileResponse(int Id, string Name, int? OwnerFatherId, bool IsOwn,
    string? SubjectName, int? SubjectId, SchoolTypes SchoolTypes, int? GradeMin, int? GradeMax,
    int? SeriesId, string? SeriesName, string SourceLang, string TargetLang,
    string? Persona, string? Didactics, IReadOnlyList<string> DefaultTypes, bool Active, DateTime CreatedAt);

/// <summary>Eingabe zum Anlegen eines Profils. Nur <c>Name</c> ist Pflicht – alles andere verengt die Passung.</summary>
public record CreateCreatorProfileDto(string Name, string? SubjectName, int? SubjectId,
    SchoolTypes? SchoolTypes, int? GradeMin, int? GradeMax, int? SeriesId,
    string? SourceLang, string? TargetLang, string? Persona, string? Didactics,
    List<string>? DefaultTypes, bool? Active);

/// <summary>
/// Partielle Änderung eines Profils; weggelassene Felder bleiben unverändert.
/// <para>
/// Ein <c>null</c> bedeutet in einem PATCH „nicht angegeben" – es kann darum kein Feld <b>leeren</b>.
/// Dafür stehen die <c>Clear…</c>-Schalter (wie <c>ClearGrade</c> an der Klassenarbeit): erst mit ihnen
/// lässt sich ein Profil wieder fachneutral, werkunabhängig oder klassenstufen-offen machen.
/// </para>
/// </summary>
/// <para>
/// <c>ClearSubject</c> macht das Profil fachneutral (Fach-Id und -Name fallen weg), <c>ClearSeries</c>
/// werkunabhängig, <c>ClearGradeMin</c>/<c>ClearGradeMax</c> heben die jeweilige Klassenstufen-Grenze auf.
/// </para>
public record UpdateCreatorProfileDto(string? Name, string? SubjectName, int? SubjectId,
    SchoolTypes? SchoolTypes, int? GradeMin, int? GradeMax, int? SeriesId,
    string? SourceLang, string? TargetLang, string? Persona, string? Didactics,
    List<string>? DefaultTypes, bool? Active,
    bool ClearSubject = false, bool ClearSeries = false,
    bool ClearGradeMin = false, bool ClearGradeMax = false);

/// <summary>
/// Ein Treffer der Profil-Suche zu einem Kind. <paramref name="Score"/> ist deterministisch berechnet
/// (Reihen-Treffer wiegt am schwersten), <paramref name="Reasons"/> nennt die Gründe im Klartext –
/// damit eine Konsole oder ein UI die Wahl erklären kann, statt eine nackte Zahl zu zeigen.
/// </summary>
public record CreatorProfileMatch(CreatorProfileResponse Profile, int Score, IReadOnlyList<string> Reasons);
