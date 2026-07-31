namespace Pugling.Contracts.Supervisor;

// Vertrag der gewichteten Kind-Interessen. Der Supervisor pflegt hier, was sein Kind mag – und vor
// allem, was es NICHT mag: negative Gewichte sind Abneigungen und schließen passende Bilder hart aus.

/// <summary>
/// A weighted interest of the child in a taxonomy tag. <c>Weight</c> ranges from
/// -3 (strong dislike) through 0 (neutral) to +3 (favorite topic).
/// </summary>
public record ChildInterestResponse(int TagId, string Slug, string Label, InterestFacet Facet,
    int Weight, DateTime CreatedAt);

/// <summary>
/// An interest to set. The tag is referenced either by <paramref name="TagId"/>
/// or named via <paramref name="Slug"/>/<paramref name="Label"/> – the latter creates it if needed
/// (create-if-missing), so the supervisor can type freely in the UI without maintaining the catalog first.
/// </summary>
public record ChildInterestInput(int Weight, int? TagId = null, string? Slug = null,
    string? Label = null, InterestFacet? Facet = null);

/// <summary>
/// Replaces the child's interests entirely with the given list (empty list = remove all).
/// Deliberately a full replacement rather than additive, because the UI edits the set as a whole.
/// </summary>
public record SetChildInterestsDto(List<ChildInterestInput> Interests);

/// <summary>Sets/changes the weight of a single tag (upsert).</summary>
public record SetChildInterestWeightDto(int Weight);
