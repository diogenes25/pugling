namespace Pugling.Contracts.Creator;

// Contract of the shared interest/style taxonomy. It is deliberately ONE vocabulary for two consumers:
// images carry it as a property, children as a preference/dislike. Only that makes "which image fits
// this child" computable at all.

/// <summary>
/// A tag of the taxonomy including usage counts on <b>all three</b> sides: <c>MediaCount</c> counts the
/// assets carrying it, <c>ChildCount</c> the children who list it as a like/dislike, and
/// <c>ExerciseCount</c> the exercises dressed in it (B-46). <c>Slug</c>
/// is the stable reference name (lowercase, no diacritics), <c>Facet</c> distinguishes theme from
/// presentation style, <c>Synonyms</c> holds alternative spellings against duplicates from free text.
///
/// The third count is not decoration: the counts are what says whether a tag is dead, and a tag that
/// dresses exercises but no image and no child would have looked dead without it.
///
/// <c>ImagePrompt</c> is the brand-neutral English scene fragment the image generator uses for this tag;
/// <c>null</c> means images are never generated for it (B-485). <c>VisibleToChildren</c> says whether a child may
/// pick it in its own interest picker – a creator's release (B-207); new keywords start hidden.
/// </summary>
public record InterestTagResponse(int Id, string Slug, string Label, InterestFacet Facet,
    IReadOnlyList<string> Synonyms, string? Color, int MediaCount, int ChildCount, DateTime CreatedAt,
    int ExerciseCount, string? ImagePrompt, bool VisibleToChildren);

/// <summary>
/// Creating a tag. The <c>Slug</c> may be omitted – it is then derived from the <c>Label</c>.
/// If the slug already exists, the endpoint returns the existing entry (idempotent).
/// </summary>
public record CreateInterestTagDto(string Label, string? Slug = null,
    InterestFacet Facet = InterestFacet.Other, List<string>? Synonyms = null, string? Color = null,
    string? ImagePrompt = null, bool VisibleToChildren = false);

/// <summary>
/// Only fields that are set are changed; <c>Synonyms</c> replaces the list entirely. <c>ClearImagePrompt</c>
/// removes the image prompt and wins over a value sent with it; an <c>ImagePrompt</c> of <c>null</c> means "unchanged".
/// <c>VisibleToChildren</c> releases or withdraws the tag for children's pickers (B-207); <c>null</c> leaves it as is.
/// </summary>
public record UpdateInterestTagDto(string? Label = null, InterestFacet? Facet = null,
    List<string>? Synonyms = null, string? Color = null, string? ImagePrompt = null, bool ClearImagePrompt = false,
    bool? VisibleToChildren = null);
