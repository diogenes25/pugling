namespace Pugling.Contracts.Creator;

// Vertrag des Medien-Stores. Zwei Ebenen, die nicht verschmelzen dürfen: das Asset ist eine
// *Darstellung* eines Motivs (Stil/Zielgruppe), die Variante eine *Auflösung* derselben Darstellung.

/// <summary>
/// An asset of the media store including its resolutions and tags. <c>Description</c> is at the same time
/// the alt text for accessibility, <c>Rating</c> the suitability (selection never returns an asset
/// above the child's clearance), <c>Placeholder</c> color/blur hash for smooth lazy-loading, and
/// <c>Tags</c> the slugs of the linked interest/style tags.
/// </summary>
public record MediaAssetResponse(int Id, string Key, string Description, MediaKind Kind,
    ContentRating Rating, string? License, string? Attribution, MediaOrigin Origin, string? Source,
    string? Placeholder, IReadOnlyList<MediaVariantResponse> Variants, IReadOnlyList<string> Tags,
    DateTime CreatedAt);

/// <summary>
/// Creating an asset. <c>Key</c> may stay empty – the server then forms a unique slug from
/// the description. <c>Tags</c> (slugs) are linked create-if-missing; <c>Variants</c> can be
/// supplied right away or added individually later.
/// </summary>
public record CreateMediaAssetDto(string Description, string? Key = null, MediaKind Kind = MediaKind.Image,
    ContentRating Rating = ContentRating.Everyone, string? License = null, string? Attribution = null,
    MediaOrigin Origin = MediaOrigin.Unknown, string? Source = null, string? Placeholder = null,
    List<string>? Tags = null, List<CreateMediaVariantDto>? Variants = null);

/// <summary>Only fields that are set are changed; <c>Tags</c> are appended (not replaced).</summary>
public record UpdateMediaAssetDto(string? Description = null, MediaKind? Kind = null,
    ContentRating? Rating = null, string? License = null, string? Attribution = null,
    MediaOrigin? Origin = null, string? Source = null, string? Placeholder = null, List<string>? Tags = null);

/// <summary>A resolution/format of an asset – addressed via its semantic purpose.</summary>
public record MediaVariantResponse(int Id, MediaPurpose Purpose, int Width, int Height,
    string Format, string Url, long? Bytes);

/// <summary>Creating a variant. (Purpose, format) is unique per asset.</summary>
public record CreateMediaVariantDto(MediaPurpose Purpose, string Url, int Width, int Height,
    string Format = "webp", long? Bytes = null);

/// <summary>Only fields that are set are changed.</summary>
public record UpdateMediaVariantDto(MediaPurpose? Purpose = null, string? Url = null, int? Width = null,
    int? Height = null, string? Format = null, long? Bytes = null);

/// <summary>Links an asset with tags of the shared taxonomy (slugs, create-if-missing).</summary>
public record TagMediaDto(List<string> Tags);

// ---- Zuordnung Bild ⇢ Träger ------------------------------------------------------------------------
// n:m in beide Richtungen: eine Vokabel trägt viele Darstellungen (damit je Kind gewählt werden kann),
// ein Asset dient vielen Vokabeln/Items. Deshalb eine eigene Ressource statt einer Spalte am Träger.

/// <summary>
/// An image assignment including the assigned asset (the list should be renderable without an extra fetch).
/// <c>Weight</c> is the editorial rank and only decides ties in the interest score.
/// </summary>
public record MediaLinkResponse(int Id, int Weight, MediaAssetResponse Asset);

/// <summary>
/// Assigns an asset – via <paramref name="MediaAssetId"/> or via <paramref name="Key"/> (handy
/// for agents building up the store via descriptive keys). Exactly one of the two is required.
/// </summary>
public record AddMediaLinkDto(int? MediaAssetId = null, string? Key = null, int Weight = 0);

/// <summary>Changes the editorial rank of an existing assignment.</summary>
public record UpdateMediaLinkDto(int Weight);

/// <summary>
/// Where an asset is assigned – the reverse direction of the assignment. <c>Carrier</c> is
/// <c>vocabulary</c> | <c>item</c> | <c>exercise</c>, <c>Label</c> a readable designation of the carrier.
/// </summary>
public record MediaUsage(string Carrier, int CarrierId, string Label, int Weight);
