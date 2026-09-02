namespace Pugling.Contracts.Creator;

// Contract of the authoring catalog: subject → (exercise) category. Exercises themselves hang off the
// textbook catalog (TextbookSeries → SeriesUnit, see TextbookSeriesDtos.cs) since B-106.
// Pure transport shapes without any entity reference; projecting from the entities stays in the API.

/// <summary>
/// A school subject in the shared catalog. Every creator may read it; <paramref name="IsMine"/> says
/// whether this caller may also rename or delete it, so a client can show the difference instead of
/// letting the user find out through a 403.
/// </summary>
/// <param name="Id">Subject id.</param>
/// <param name="Name">Display name (e.g. "English").</param>
/// <param name="CreatedAt">When the subject was created (UTC).</param>
/// <param name="CategoriesCount">Number of exercise categories below it.</param>
/// <param name="OwnerAdultId">The creator who opened the subject; <c>null</c> = seeded, owned by nobody.</param>
/// <param name="IsMine">True if the calling creator is the owner (an ownerless subject is nobody's).</param>
public record SubjectResponse(int Id, string Name, DateTime CreatedAt, int CategoriesCount,
    int? OwnerAdultId, bool IsMine);

/// <summary>Input for creating a subject.</summary>
public record CreateSubjectDto(string Name);

/// <summary>Partial change to a subject; empty fields remain unchanged.</summary>
public record UpdateSubjectDto(string? Name);

/// <summary>
/// An exercise category within a subject. Every creator may read it and add new ones; IsOwn says whether
/// this caller may also rename or delete it, so a client can show the difference per row instead of
/// letting the user find out through a 403.
/// </summary>
/// <param name="Id">Category id.</param>
/// <param name="SubjectId">The subject it belongs to.</param>
/// <param name="Name">Display name (e.g. "Grammar").</param>
/// <param name="CreatedAt">When the category was created (UTC).</param>
/// <param name="IsOwn">
/// Whether the caller may rename or delete this category: its creator, the owner of its subject, or a
/// platform admin (B-170). Not the same question as "did I create it" - the right has two bearers, and a
/// client that needs to draw a button needs the answer, not one of its halves. Named IsOwn rather than
/// IsMine because the contract says IsOwn seven times against one (B-156), and there it already carries
/// exactly this "may write (owner or grant)" meaning.
/// </param>
public record CategoryResponse(int Id, int SubjectId, string Name, DateTime CreatedAt, bool IsOwn);

/// <summary>Input for creating a category.</summary>
public record CreateCategoryDto(string Name);

/// <summary>Partial change to a category; empty fields remain unchanged.</summary>
public record UpdateCategoryDto(string? Name);
