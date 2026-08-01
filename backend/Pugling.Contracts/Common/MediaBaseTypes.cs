namespace Pugling.Contracts;

// Shared base types of the media store and the interest taxonomy (tier-neutral: the creator maintains
// assets/tags, the supervisor the child's interests, the student later receives the image).

/// <summary>Media kind of an asset. Today only images are delivered; the store is deliberately open-ended.</summary>
public enum MediaKind
{
    /// <summary>Image – the only type delivered today (see docs/medien-bilder.md).</summary>
    Image = 0,
    /// <summary>Audio recording (e.g. pronunciation). Planned in the store, not yet delivered.</summary>
    Audio = 1,
    /// <summary>Video. Planned in the store, not yet delivered.</summary>
    Video = 2,
}

/// <summary>
/// Suitability of an asset for an age group – the load-bearing axis of target-audience differentiation.
/// Only this makes <b>a shared store for all target audiences</b> viable: the selection filters hard
/// against <c>Child.AllowedContentRating</c> before even sorting by interests.
/// <para>
/// The values are <b>ordered ascending</b> and compared numerically (<c>Rating &lt;= Allowed</c>).
/// That's why they are stored as <c>int</c> in the DB (not as a string like the other enums) and must
/// <b>never be renumbered</b> – new levels are only appended at the end.
/// </para>
/// </summary>
public enum ContentRating
{
    /// <summary>Suitable for everyone. Default for new assets <i>and</i> new children.</summary>
    Everyone = 0,
    /// <summary>From about age 12: milder horror/conflict motifs, teen themes.</summary>
    Teen = 1,
    /// <summary>Adults only (explicit content, graphic depiction). For a child profile only after explicit approval by the supervisor.</summary>
    Mature = 2,
}

/// <summary>
/// Semantic delivery slot of a variant. The client asks for the <i>purpose</i>, not for
/// pixel dimensions – this keeps the resolution policy changeable server-side without breaking the contract.
/// </summary>
public enum MediaPurpose
{
    /// <summary>Tiny preview image in lists/result lists.</summary>
    Thumb = 0,
    /// <summary>Standard size on the exercise card – the common case while learning.</summary>
    Card = 1,
    /// <summary>Large view (preview/zoom).</summary>
    Full = 2,
    /// <summary>Wide header format (chapter/exercise header).</summary>
    Hero = 3,
}

/// <summary>Origin of an asset – makes generated and third-party content distinguishable in the catalog.</summary>
public enum MediaOrigin
{
    /// <summary>Origin not recorded – default for legacy assets.</summary>
    Unknown = 0,
    /// <summary>Uploaded/provided by the creator themselves.</summary>
    Upload = 1,
    /// <summary>Taken from an external image source (maintain license/attribution!).</summary>
    Stock = 2,
    /// <summary>AI-generated; the generating prompt/model belongs in <c>Source</c>.</summary>
    Generated = 3,
}

/// <summary>
/// Facet of an interest tag. It groups the taxonomy by domain without splitting it:
/// topic (<see cref="Franchise"/>, <see cref="Sport"/> …) and <see cref="Style"/> deliberately live in
/// <b>the same</b> table because they behave identically during image selection – only the weighting
/// differs. Extensions are purely additive.
/// </summary>
public enum InterestFacet
{
    /// <summary>Cannot be assigned to any of the other facets. Default when creating a tag.</summary>
    Other = 0,
    /// <summary>Brand/series/game ("Pokémon", "Brawl Stars", "Star Wars").</summary>
    Franchise = 1,
    /// <summary>Sport or club ("football", "skateboarding").</summary>
    Sport = 2,
    /// <summary>Animal or animal group ("horse", "dinosaur").</summary>
    Animal = 3,
    /// <summary>Vehicle ("tractor", "fire truck", "rocket").</summary>
    Vehicle = 4,
    /// <summary>Music – genre, band, or instrument.</summary>
    Music = 5,
    /// <summary>Leisure/activity ("cooking", "fishing", "programming").</summary>
    Hobby = 6,
    /// <summary>Nature and landscape ("forest", "sea", "space").</summary>
    Nature = 7,
    /// <summary>Visual style ("comic", "photo", "pixel art") – orthogonal to the topic.</summary>
    Style = 8,
}
