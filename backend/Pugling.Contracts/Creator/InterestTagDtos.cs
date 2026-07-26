namespace Pugling.Contracts.Creator;

// Vertrag der geteilten Interessen-/Stil-Taxonomie. Sie ist absichtlich EIN Vokabular für zwei
// Verbraucher: Bilder tragen sie als Eigenschaft, Kinder als Vorliebe/Abneigung. Nur deshalb lässt sich
// „welches Bild passt zu diesem Kind" überhaupt rechnen.

/// <summary>
/// Ein Schlagwort der Taxonomie samt Nutzungszahlen auf beiden Seiten: <c>MediaCount</c> zählt die
/// Assets, die es tragen, <c>ChildCount</c> die Kinder, die es als Vorliebe/Abneigung führen. <c>Slug</c>
/// ist der stabile Referenzname (kleingeschrieben, ohne Diakritika), <c>Facet</c> unterscheidet Thema von
/// Darstellungsstil, <c>Synonyms</c> hält alternative Schreibweisen gegen Dubletten aus Freitext.
/// </summary>
public record InterestTagResponse(int Id, string Slug, string Label, InterestFacet Facet,
    IReadOnlyList<string> Synonyms, string? Color, int MediaCount, int ChildCount, DateTime CreatedAt);

/// <summary>
/// Anlegen eines Schlagworts. Der <c>Slug</c> darf entfallen – er wird dann aus dem <c>Label</c>
/// abgeleitet. Existiert der Slug bereits, liefert der Endpunkt den bestehenden Eintrag (idempotent).
/// </summary>
public record CreateInterestTagDto(string Label, string? Slug = null,
    InterestFacet Facet = InterestFacet.Other, List<string>? Synonyms = null, string? Color = null);

/// <summary>Nur gesetzte Felder werden geändert; <c>Synonyms</c> ersetzt die Liste vollständig.</summary>
public record UpdateInterestTagDto(string? Label = null, InterestFacet? Facet = null,
    List<string>? Synonyms = null, string? Color = null);
