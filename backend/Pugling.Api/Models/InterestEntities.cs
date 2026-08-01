namespace Pugling.Api.Models;

// Interest taxonomy: ONE controlled vocabulary shared by the child profile and the media store. This is the
// pivot of the individualized image selection - only because both sides draw from the same table is "which
// image fits this child" more than a string comparison.
// The free-form Child.Interests stays next to it: the AI creator lives on free text (it dresses the subject
// matter in language), the image selection needs exact references instead.
//
// InterestFacet lives in the contract project (Pugling.Contracts).

/// <summary>
/// An interest/style keyword of the shared vocabulary ("pokemon", "fussball", "comic").
/// Global and child-neutral like the vocabulary store: maintained by the creator, referenced by children
/// (<see cref="ChildInterest"/>) <b>and</b> by images (<see cref="MediaTagLink"/>).
/// </summary>
public class InterestTag
{
    public int Id { get; set; }

    /// <summary>Stable, globally unique reference slug (lower case, without diacritics).</summary>
    public string Slug { get; set; } = "";

    /// <summary>Display name for the UI ("Pokémon") – may carry capitals/special characters.</summary>
    public string Label { get; set; } = "";

    /// <summary>Domain facet (topic vs. presentation style); drives the weighting of the selection.</summary>
    public InterestFacet Facet { get; set; } = InterestFacet.Other;

    /// <summary>
    /// Alternative spellings ("Poke", "Pikachu"). They serve the free-text backfill and the creator search
    /// so the same interest does not end up as several separate tags. Stored as a JSON list
    /// (reassign in the controller, no in-place mutation – a missing ValueComparer is a pitfall otherwise).
    /// </summary>
    public List<string> Synonyms { get; set; } = [];

    /// <summary>Optional display color (hex) for the UI – as with <see cref="VocabTag"/>.</summary>
    public string? Color { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Images carrying this keyword (counterpart to <see cref="ChildInterests"/>).</summary>
    public List<MediaTagLink> MediaLinks { get; set; } = [];

    /// <summary>Children who like this keyword (or dislike it – see <see cref="ChildInterest.Weight"/>).</summary>
    public List<ChildInterest> ChildInterests { get; set; } = [];
}

/// <summary>
/// A child's weighted interest in an <see cref="InterestTag"/>. The sign carries the main domain statement:
/// <b>negative weights are dislikes</b> ("no spiders") and later exclude matching images hard – they matter
/// more for a good result than the preferences do, because a repellent image inverts the learning effect.
/// </summary>
public class ChildInterest
{
    public int Id { get; set; }

    public int ChildId { get; set; }
    public Child? Child { get; set; }

    public int InterestTagId { get; set; }
    public InterestTag? InterestTag { get; set; }

    /// <summary>
    /// <see cref="MinWeight"/> (strong dislike) … 0 (neutral) … <see cref="MaxWeight"/> (favorite topic).
    /// The controller clamps to this range; the scale is deliberately coarse because a human maintains it.
    /// </summary>
    public int Weight { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Lower bound of the weight scale (dislike).</summary>
    public const int MinWeight = -3;

    /// <summary>Upper bound of the weight scale (favorite topic).</summary>
    public const int MaxWeight = 3;
}
