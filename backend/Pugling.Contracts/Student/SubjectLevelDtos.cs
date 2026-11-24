namespace Pugling.Contracts.Student;

/// <summary>
/// The grade level of a child in one vocabulary subject (B-742): <paramref name="Level"/> 1 (best) to 6 and the best
/// level ever reached. The server sends the number for every level; the wording is the client's business - the child
/// app names only levels 1 to 3 and otherwise shows the next goal (B-743). Never sent for several children at once.
/// </summary>
/// <param name="SubjectId">The subject.</param>
/// <param name="SubjectName">Its name - the child's day view groups its positions by this name.</param>
/// <param name="Level">The current level, evaluated once per learning day; 1 is the best.</param>
/// <param name="BestLevel">The best level ever reached; never gets worse.</param>
/// <param name="WordsToNextLevel">
/// Further mastered words of the material that lift the share to the next better level, counted live; 0 = reached, the
/// level follows with the next day's evaluation; <c>null</c> on the best level.
/// </param>
public record SubjectLevelResponse(int SubjectId, string SubjectName, int Level, int BestLevel, int? WordsToNextLevel);
