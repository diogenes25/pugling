namespace Pugling.Api.Models;

// Media store: one motif, many images. Two axes that must stay strictly separate -
//   MediaAsset   = one *rendition*            ("running unicorn, comic")  → content-wise (style/audience)
//   MediaVariant = one *technical form* of that same rendition            → resolution/format
// As in the vocabulary store, no bytes live in the DB, only URLs (cf. Vocabulary.PronunciationAudioUrl).
//
// There is deliberately NO "motif" entity: the set "all images meaning *to run*" is exactly the set of
// MediaLinks on the same vocabulary entry - the carrier is the motif.
//
// MediaKind/ContentRating/MediaPurpose/MediaOrigin live in the contract project (Pugling.Contracts).

/// <summary>
/// One concrete asset of a motif – not "the image for running" but "the running unicorn in comic style".
/// It carries meaning, style (through <see cref="TagLinks"/>) and suitability (<see cref="Rating"/>);
/// the files themselves hang off it as <see cref="Variants"/>.
/// </summary>
public class MediaAsset
{
    public int Id { get; set; }

    /// <summary>Stable, globally unique reference key (e.g. "run_unicorn_comic") – like <see cref="Vocabulary.Key"/>.</summary>
    public string Key { get; set; } = "";

    /// <summary>
    /// What can be seen. Double duty: <b>alt text</b> for accessibility (it later goes to the client with
    /// the card) and search text for creators and AI agents.
    /// </summary>
    public string Description { get; set; } = "";

    public MediaKind Kind { get; set; } = MediaKind.Image;

    /// <summary>Suitability. The selection later filters hard against it, before it even sorts by interests.</summary>
    public ContentRating Rating { get; set; } = ContentRating.Everyone;

    /// <summary>Short license identifier (e.g. "CC-BY-4.0") – mandatory for third-party sources.</summary>
    public string? License { get; set; }

    /// <summary>Naming of the author, where the license requires it.</summary>
    public string? Attribution { get; set; }

    public MediaOrigin Origin { get; set; } = MediaOrigin.Unknown;

    /// <summary>Provenance detail: URL of the third-party source, or model + prompt for <see cref="MediaOrigin.Generated"/>.</summary>
    public string? Source { get; set; }

    /// <summary>Dominant color (hex) or a tiny blur hash – allows stutter-free lazy loading in the client.</summary>
    public string? Placeholder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>The same asset in several resolutions/formats.</summary>
    public List<MediaVariant> Variants { get; set; } = [];

    /// <summary>Topic and style keywords from the shared taxonomy (<see cref="InterestTag"/>).</summary>
    public List<MediaTagLink> TagLinks { get; set; } = [];

    /// <summary>Where this image is assigned (vocabulary entries, exercise items, exercises).</summary>
    public List<MediaLink> Links { get; set; } = [];
}

/// <summary>
/// A technical rendition of a <see cref="MediaAsset"/> – the same asset, different bytes.
/// It is addressed through the semantic <see cref="Purpose"/>, not through pixel dimensions: that way
/// delivery can later switch to other sizes without breaking the contract.
/// </summary>
public class MediaVariant
{
    public int Id { get; set; }

    public int MediaAssetId { get; set; }
    public MediaAsset? MediaAsset { get; set; }

    public MediaPurpose Purpose { get; set; }

    public int Width { get; set; }
    public int Height { get; set; }

    /// <summary>File format ("webp", "avif", "png", "jpg"). Several formats per purpose allow <c>&lt;picture&gt;</c>/srcset.</summary>
    public string Format { get; set; } = "webp";

    /// <summary>URL of the file – no base64 in the payload (the same rule as for the pronunciation audio source).</summary>
    public string Url { get; set; } = "";

    /// <summary>File size in bytes, if known (budget decisions in the client).</summary>
    public long? Bytes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Links a <see cref="MediaAsset"/> to an <see cref="InterestTag"/> (n:m).</summary>
public class MediaTagLink
{
    public int Id { get; set; }

    public int MediaAssetId { get; set; }
    public MediaAsset? MediaAsset { get; set; }

    public int InterestTagId { get; set; }
    public InterestTag? InterestTag { get; set; }
}

/// <summary>
/// Assignment of a <see cref="MediaAsset"/> to whatever it illustrates – <b>n:m in both directions</b>.
/// One vocabulary entry carries many assets (that is exactly the point: the child gets the fitting one), and
/// one asset serves many vocabulary entries: "run" (en→de) and "laufen" (de→en) are separate store rows, and
/// the running unicorn should serve both. A column on the carrier (like
/// <see cref="Vocabulary.PronunciationAudioUrl"/>) could not do that – audio is 1:1 because there is one
/// correct pronunciation; with images the variety is the requirement.
/// <para>
/// Exactly <b>one</b> of the three carrier FKs is set (check constraint). The three form a specificity
/// cascade that the resolver later reads from the bottom up:
/// <see cref="ExerciseItemId"/> (this exercise only) beats <see cref="VocabularyId"/> (applies everywhere);
/// <see cref="ExerciseId"/> is the title image of a text/reading exercise and stands beside them.
/// </para>
/// </summary>
public class MediaLink
{
    public int Id { get; set; }

    public int MediaAssetId { get; set; }
    public MediaAsset? MediaAsset { get; set; }

    /// <summary>Store assignment: applies in <b>all</b> exercises using this vocabulary entry (the normal case).</summary>
    public int? VocabularyId { get; set; }
    public Vocabulary? Vocabulary { get; set; }

    /// <summary>Exercise-local override: applies to this one item only, without bending the store.</summary>
    public int? ExerciseItemId { get; set; }
    public ExerciseItem? ExerciseItem { get; set; }

    /// <summary>Title image of an exercise (text/sentence/reading) – no item relation.</summary>
    public int? ExerciseId { get; set; }
    public Exercise? Exercise { get; set; }

    /// <summary>
    /// Editorial rank. It only decides <b>on a tie</b> of the interest scoring – with it the creator can pull
    /// a favorite image forward without overriding the per-child selection.
    /// </summary>
    public int Weight { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// A child's <b>frozen</b> image choice for one carrier. The non-obvious part of the whole design: when
/// learning vocabulary, image constancy is <i>wanted</i> – the child should see the same image on every
/// repetition, recognition <b>is</b> the retention effect. If the selection recomputed on every request, an
/// image added later would destroy exactly that. The same pattern as the frozen play-out order of an
/// exercise session.
/// <para>
/// One row per <b>candidate</b>, not per carrier: the active choice is the row with <see cref="Rejected"/> =
/// <c>false</c>, rejected images remain as a row and are never drawn again. That makes "another image" at the
/// same time the cheapest feedback signal we can get.
/// </para>
/// </summary>
public class ChildMediaPick
{
    public int Id { get; set; }

    public int ChildId { get; set; }
    public Child? Child { get; set; }

    /// <summary>Carrier of the choice – exactly one of the two is set (as with <see cref="MediaLink"/>).</summary>
    public int? VocabularyId { get; set; }
    public Vocabulary? Vocabulary { get; set; }

    public int? ExerciseItemId { get; set; }
    public ExerciseItem? ExerciseItem { get; set; }

    public int MediaAssetId { get; set; }
    public MediaAsset? MediaAsset { get; set; }

    /// <summary>Rejected by the child/supervisor ("another image") – never drawn again for this carrier.</summary>
    public bool Rejected { get; set; }

    public DateTime PickedAt { get; set; } = DateTime.UtcNow;
}
