namespace Pugling.Contracts.Creator;

// Contract of the publisher vocabulary (route api/v1/creator/publishers) - a shared, slug-idempotent
// list like the vocabulary store's tags. No ownership: naming a publisher is not authorship.

/// <summary>A publisher ("Cornelsen"). <c>SeriesCount</c> shows whether it is still in use.</summary>
public record PublisherResponse(int Id, string Name, string Slug, int SeriesCount, DateTime CreatedAt);

/// <summary>
/// Input for creating a publisher. If the slug is already taken, the existing entry comes back
/// (idempotent) - so an agent can safely repeat the same catalog setup.
/// </summary>
public record CreatePublisherDto(string Name);

/// <summary>Changes the display name. The slug stays fixed.</summary>
public record UpdatePublisherDto(string? Name);
