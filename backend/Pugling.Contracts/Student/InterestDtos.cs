namespace Pugling.Contracts.Student;

// Contract of the child's self-description (B-50). Deliberately narrower than the supervisor's contract
// next to it: the child picks FROM the shared taxonomy, it never extends it. A tag invented by a child
// would match no image at all, and the taxonomy is exactly the axis that makes image selection computable.

/// <summary>
/// A selectable keyword of the shared taxonomy, as the child sees it. Deliberately without the usage
/// counts of <c>InterestTagResponse</c>: they would tell one child how many others picked a keyword.
/// </summary>
public record InterestTagOptionResponse(int Id, string Slug, string Label, InterestFacet Facet);

/// <summary>
/// One interest the child sets on itself. Only <paramref name="TagId"/> – naming a tag by text would
/// create it (create-if-missing), and creating taxonomy is not a child's action.
/// <c>Weight</c> ranges from -3 (strong dislike) through 0 (neutral) to +3 (favorite topic).
/// </summary>
public record MyInterestInput(int TagId, int Weight);

/// <summary>
/// Replaces the child's own interests entirely (empty list = remove all) – the guided process edits
/// the set as a whole, the same way the supervisor's editor does.
/// </summary>
public record SetMyInterestsDto(List<MyInterestInput> Interests);

/// <summary>
/// A free-text wish outside the taxonomy ("Sonstiges: …"). It lands in the child's free-form
/// <c>Interests</c> list, visible to the supervisor – no hidden channel; an adult can later turn it into
/// a real tag via the backfill funnel.
/// </summary>
public record AddInterestWishDto(string Text);

/// <summary>
/// One of the child's own weighted interests. Slimmer than the supervisor's view of the same row: the
/// child has no use for the creation timestamp.
/// </summary>
public record MyInterestResponse(int TagId, string Slug, string Label, InterestFacet Facet, int Weight);

/// <summary>
/// The child's own interest profile in one read: the weighted taxonomy entries plus the free-text
/// wishes. <paramref name="NeedsIntro"/> is the server's answer to "has this child ever described
/// itself?" – it is derived from an empty profile, so no extra timestamp column is needed.
/// </summary>
public record MyInterestProfileResponse(IReadOnlyList<MyInterestResponse> Interests,
    IReadOnlyList<string> Wishes, bool NeedsIntro);
