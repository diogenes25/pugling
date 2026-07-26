namespace Pugling.Contracts.Creator;

// Vertrag des Medien-Stores. Zwei Ebenen, die nicht verschmelzen dürfen: das Asset ist eine
// *Darstellung* eines Motivs (Stil/Zielgruppe), die Variante eine *Auflösung* derselben Darstellung.

/// <summary>
/// Ein Asset des Medien-Stores samt seiner Auflösungen und Schlagworte. <c>Description</c> ist zugleich
/// der Alt-Text für die Barrierefreiheit, <c>Rating</c> die Eignung (die Auswahl liefert nie ein Asset
/// über der Freigabe des Kindes), <c>Placeholder</c> Farbe/Blur-Hash fürs ruckelfreie Nachladen und
/// <c>Tags</c> die Slugs der verknüpften Interessen-/Stil-Schlagworte.
/// </summary>
public record MediaAssetResponse(int Id, string Key, string Description, MediaKind Kind,
    ContentRating Rating, string? License, string? Attribution, MediaOrigin Origin, string? Source,
    string? Placeholder, IReadOnlyList<MediaVariantResponse> Variants, IReadOnlyList<string> Tags,
    DateTime CreatedAt);

/// <summary>
/// Anlegen eines Assets. <c>Key</c> darf leer bleiben – der Server bildet dann einen eindeutigen Slug aus
/// der Beschreibung. <c>Tags</c> (Slugs) werden create-if-missing verknüpft; <c>Variants</c> lassen sich
/// gleich mitgeben oder später einzeln nachreichen.
/// </summary>
public record CreateMediaAssetDto(string Description, string? Key = null, MediaKind Kind = MediaKind.Image,
    ContentRating Rating = ContentRating.Everyone, string? License = null, string? Attribution = null,
    MediaOrigin Origin = MediaOrigin.Unknown, string? Source = null, string? Placeholder = null,
    List<string>? Tags = null, List<CreateMediaVariantDto>? Variants = null);

/// <summary>Nur gesetzte Felder werden geändert; <c>Tags</c> werden ergänzt (nicht ersetzt).</summary>
public record UpdateMediaAssetDto(string? Description = null, MediaKind? Kind = null,
    ContentRating? Rating = null, string? License = null, string? Attribution = null,
    MediaOrigin? Origin = null, string? Source = null, string? Placeholder = null, List<string>? Tags = null);

/// <summary>Eine Auflösung/ein Format eines Assets – adressiert über den semantischen Zweck.</summary>
public record MediaVariantResponse(int Id, MediaPurpose Purpose, int Width, int Height,
    string Format, string Url, long? Bytes);

/// <summary>Anlegen einer Variante. Je Asset ist (Zweck, Format) eindeutig.</summary>
public record CreateMediaVariantDto(MediaPurpose Purpose, string Url, int Width, int Height,
    string Format = "webp", long? Bytes = null);

/// <summary>Nur gesetzte Felder werden geändert.</summary>
public record UpdateMediaVariantDto(MediaPurpose? Purpose = null, string? Url = null, int? Width = null,
    int? Height = null, string? Format = null, long? Bytes = null);

/// <summary>Verknüpft ein Asset mit Schlagworten der geteilten Taxonomie (Slugs, create-if-missing).</summary>
public record TagMediaDto(List<string> Tags);

// ---- Zuordnung Bild ⇢ Träger ------------------------------------------------------------------------
// n:m in beide Richtungen: eine Vokabel trägt viele Darstellungen (damit je Kind gewählt werden kann),
// ein Asset dient vielen Vokabeln/Items. Deshalb eine eigene Ressource statt einer Spalte am Träger.

/// <summary>
/// Eine Bild-Zuordnung samt des zugeordneten Assets (die Liste soll ohne Nachladen darstellbar sein).
/// <c>Weight</c> ist der redaktionelle Rang und entscheidet erst bei Gleichstand der Interessens-Bewertung.
/// </summary>
public record MediaLinkResponse(int Id, int Weight, MediaAssetResponse Asset);

/// <summary>
/// Ordnet ein Asset zu – per <paramref name="MediaAssetId"/> oder per <paramref name="Key"/> (praktisch
/// für Agenten, die den Store über sprechende Keys aufbauen). Genau eines von beiden ist nötig.
/// </summary>
public record AddMediaLinkDto(int? MediaAssetId = null, string? Key = null, int Weight = 0);

/// <summary>Ändert den redaktionellen Rang einer bestehenden Zuordnung.</summary>
public record UpdateMediaLinkDto(int Weight);

/// <summary>
/// Wo ein Asset zugeordnet ist – die Rückrichtung zur Zuordnung. <c>Carrier</c> ist
/// <c>vocabulary</c> | <c>item</c> | <c>exercise</c>, <c>Label</c> eine lesbare Bezeichnung des Trägers.
/// </summary>
public record MediaUsage(string Carrier, int CarrierId, string Label, int Weight);
