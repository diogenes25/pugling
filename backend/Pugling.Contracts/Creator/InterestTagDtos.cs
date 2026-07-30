namespace Pugling.Contracts.Creator;

// Vertrag der geteilten Interessen-/Stil-Taxonomie. Sie ist absichtlich EIN Vokabular für zwei
// Verbraucher: Bilder tragen sie als Eigenschaft, Kinder als Vorliebe/Abneigung. Nur deshalb lässt sich
// „welches Bild passt zu diesem Kind" überhaupt rechnen.

/// <summary>
/// A tag of the taxonomy including usage counts on both sides: <c>MediaCount</c> counts the
/// assets carrying it, <c>ChildCount</c> the children who list it as a like/dislike. <c>Slug</c>
/// is the stable reference name (lowercase, no diacritics), <c>Facet</c> distinguishes theme from
/// presentation style, <c>Synonyms</c> holds alternative spellings against duplicates from free text.
/// </summary>
public record InterestTagResponse(int Id, string Slug, string Label, InterestFacet Facet,
    IReadOnlyList<string> Synonyms, string? Color, int MediaCount, int ChildCount, DateTime CreatedAt);

/// <summary>
/// Creating a tag. The <c>Slug</c> may be omitted – it is then derived from the <c>Label</c>.
/// If the slug already exists, the endpoint returns the existing entry (idempotent).
/// </summary>
public record CreateInterestTagDto(string Label, string? Slug = null,
    InterestFacet Facet = InterestFacet.Other, List<string>? Synonyms = null, string? Color = null);

/// <summary>Only fields that are set are changed; <c>Synonyms</c> replaces the list entirely.</summary>
public record UpdateInterestTagDto(string? Label = null, InterestFacet? Facet = null,
    List<string>? Synonyms = null, string? Color = null);
