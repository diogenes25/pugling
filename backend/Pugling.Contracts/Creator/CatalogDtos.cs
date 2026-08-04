namespace Pugling.Contracts.Creator;

// Contract of the authoring catalog: subject → (exercise) category. Exercises themselves hang off the
// textbook catalog (TextbookSeries → SeriesUnit, see TextbookSeriesDtos.cs) since B-106.
// Pure transport shapes without any entity reference; projecting from the entities stays in the API.

/// <summary>A school subject in the shared catalog.</summary>
public record SubjectResponse(int Id, string Name, DateTime CreatedAt, int CategoriesCount);

/// <summary>Input for creating a subject.</summary>
public record CreateSubjectDto(string Name);

/// <summary>Partial change to a subject; empty fields remain unchanged.</summary>
public record UpdateSubjectDto(string? Name);

/// <summary>An exercise category within a subject.</summary>
public record CategoryResponse(int Id, int SubjectId, string Name, DateTime CreatedAt);

/// <summary>Input for creating a category.</summary>
public record CreateCategoryDto(string Name);

/// <summary>Partial change to a category; empty fields remain unchanged.</summary>
public record UpdateCategoryDto(string? Name);
