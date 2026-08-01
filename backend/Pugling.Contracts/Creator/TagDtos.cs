namespace Pugling.Contracts.Creator;

// Contract of the two tag tiers:
//   * child-scoped tags on exercises AND vocabulary entries (Tag/TagResponse) - supervisor and child may tag,
//   * child-neutral vocabulary tags on the store (VocabTagResponse).

/// <summary>Tag in the response incl. count of tagged exercises and vocabulary entries.</summary>
public record TagResponse(int Id, int ChildId, string Name, string? Color, TaggedBy CreatedBy,
    int ExerciseCount, int VocabularyCount, DateTime CreatedAt);

/// <summary>Input for creating a child-scoped tag.</summary>
public record CreateTagDto(int ChildId, string Name, string? Color);

/// <summary>Partial change to a tag; empty fields remain unchanged.</summary>
public record UpdateTagDto(string? Name, string? Color);

/// <summary>Input for tagging exercises with a tag.</summary>
public record TagExercisesDto(List<int> ExerciseIds);

/// <summary>Lean vocabulary view for the tag assignment (without the child-neutral store details).</summary>
public record TaggedVocabularyDto(int Id, string Key, string Word, string Translation);

/// <summary>Input for tagging vocabulary entries with a child-scoped tag.</summary>
public record TagVocabularyDto(List<int> VocabularyIds);

/// <summary>Child-neutral vocabulary tag incl. count of linked vocabulary entries.</summary>
public record VocabTagResponse(int Id, string Name, string? Color, int VocabCount, DateTime CreatedAt);

/// <summary>Input for creating a child-neutral vocabulary tag.</summary>
public record CreateVocabTagDto(string Name, string? Color);

/// <summary>Partial change to a vocabulary tag; empty fields remain unchanged.</summary>
public record UpdateVocabTagDto(string? Name, string? Color);

/// <summary>Input for linking a vocabulary entry with tags (create-if-missing via the names).</summary>
public record TagVocabDto(List<string> Tags);
