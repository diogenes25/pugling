namespace Pugling.Contracts.Creator;

// Contract of rating shared exercises (B-12): stars plus an optional comment, one rating per creator.

/// <summary>Input for rating somebody else's exercise. Rating again overwrites the caller's own rating.</summary>
/// <param name="Stars">1 to 5.</param>
/// <param name="Comment">Optional short comment (at most 2000 characters); empty or blank = no comment.</param>
public record RateExerciseDto(int Stars, string? Comment);

/// <summary>
/// One creator's rating of an exercise. Visible to every creator, including the rater's name: the catalog
/// shows authors by name anyway, and a curation signal nobody can attribute is worth little.
/// </summary>
/// <param name="CreatorId">The rating creator.</param>
/// <param name="CreatorName">Display name of the rating creator.</param>
/// <param name="IsMine">Whether this is the requesting creator's own rating - the one they may change or delete.</param>
/// <param name="Stars">1 to 5.</param>
/// <param name="Comment">Optional comment.</param>
/// <param name="CreatedAt">When the creator first rated.</param>
/// <param name="UpdatedAt">When the creator last changed the rating.</param>
public record ExerciseRatingResponse(int CreatorId, string CreatorName, bool IsMine, int Stars, string? Comment,
    DateTime CreatedAt, DateTime UpdatedAt);
