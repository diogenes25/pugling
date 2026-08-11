namespace Pugling.Contracts.Creator;

// Contract of the publisher vocabulary (route api/v1/creator/publishers) - a shared, slug-idempotent
// list like the vocabulary store's tags. No ownership: naming a publisher is not authorship.

/// <summary>
/// A publisher ("Cornelsen"). <c>SeriesCount</c> shows whether it is still in use.
/// <para>
/// <c>ForeignSeriesCount</c> is the subset of those series that do <b>not</b> belong to the caller - a
/// foreign account's, or an ownerless one from the shared catalog, which counts as foreign rather than as
/// free. It is what decides the delete: greater than zero means DELETE answers 409
/// <c>publisher_in_use</c>. Without it a UI can only offer the delete and let the caller run into the
/// lock, because <c>SeriesCount</c> counts foreign rows too and says nothing about who owns them.
/// </para>
/// </summary>
public record PublisherResponse(int Id, string Name, string Slug, int SeriesCount, int ForeignSeriesCount, DateTime CreatedAt);

/// <summary>
/// Input for creating a publisher. If the slug is already taken, the existing entry comes back
/// (idempotent) - so an agent can safely repeat the same catalog setup.
/// </summary>
public record CreatePublisherDto(string Name);

/// <summary>Changes the display name. The slug stays fixed.</summary>
public record UpdatePublisherDto(string? Name);
