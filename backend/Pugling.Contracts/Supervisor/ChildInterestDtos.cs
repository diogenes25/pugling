namespace Pugling.Contracts.Supervisor;

// Vertrag der gewichteten Kind-Interessen. Der Supervisor pflegt hier, was sein Kind mag – und vor
// allem, was es NICHT mag: negative Gewichte sind Abneigungen und schließen passende Bilder hart aus.

/// <summary>
/// Ein gewichtetes Interesse des Kindes an einem Taxonomie-Schlagwort. <c>Weight</c> läuft von
/// -3 (starke Abneigung) über 0 (neutral) bis +3 (Lieblingsthema).
/// </summary>
public record ChildInterestResponse(int TagId, string Slug, string Label, InterestFacet Facet,
    int Weight, DateTime CreatedAt);

/// <summary>
/// Ein zu setzendes Interesse. Das Schlagwort wird entweder per <paramref name="TagId"/> referenziert
/// oder per <paramref name="Slug"/>/<paramref name="Label"/> benannt – Letzteres legt es bei Bedarf an
/// (create-if-missing), damit der Vater im UI frei tippen kann, ohne vorher den Katalog zu pflegen.
/// </summary>
public record ChildInterestInput(int Weight, int? TagId = null, string? Slug = null,
    string? Label = null, InterestFacet? Facet = null);

/// <summary>
/// Ersetzt die Interessen des Kindes vollständig durch die übergebene Liste (leere Liste = alle
/// entfernen). Bewusst ersetzend statt ergänzend, weil das UI die Menge als Ganzes bearbeitet.
/// </summary>
public record SetChildInterestsDto(List<ChildInterestInput> Interests);

/// <summary>Setzt/ändert das Gewicht eines einzelnen Schlagworts (Upsert).</summary>
public record SetChildInterestWeightDto(int Weight);
