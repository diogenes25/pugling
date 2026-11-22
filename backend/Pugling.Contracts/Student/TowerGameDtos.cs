namespace Pugling.Contracts.Student;

// Contract of "Vokabel-Türme" (B-851 core): a native game that assigns vocabulary tokens to towers. The server picks the
// material and grades every assignment; tower and token ids are opaque, so the run payload never reveals which token
// belongs to which tower.

/// <summary>The child's tower game: the plan positions whose vocabulary a run can be built from.</summary>
public record TowerGameOverview(IReadOnlyList<TowerGamePosition> Positions);

/// <summary>
/// A vocabulary position of a playable plan. <paramref name="WordCount"/> is the number of words in the exercise; a run
/// needs at least five.
/// </summary>
public record TowerGamePosition(int PlanPositionId, string PlanTitle, string? SubjectName, string ExerciseTitle,
    int WordCount);

/// <summary>Starts a tower-game run from the vocabulary of one plan position.</summary>
public record StartTowerRunDto(int PlanPositionId);

/// <summary>
/// A tower-game run. The client shows <paramref name="TowerSlots"/> towers at a time in the order of <paramref
/// name="Towers"/> and refills a slot after a correct assignment; <paramref name="Goal"/> correct assignments win the
/// level. <paramref name="Tokens"/> are shuffled and hold the words of all towers, so a token whose tower is not on the
/// field yet acts as a distractor.
/// </summary>
public record TowerRunResponse(int PlanPositionId, int Goal, int TowerSlots, IReadOnlyList<TowerTarget> Towers,
    IReadOnlyList<TowerToken> Tokens);

/// <summary>A tower: an opaque id and the meaning (German) it asks for.</summary>
public record TowerTarget(string TowerId, string Prompt);

/// <summary>A dropped vocabulary token: an opaque id and the foreign word it carries.</summary>
public record TowerToken(string TokenId, string Word);

/// <summary>Assigns a token to a tower; both ids come from the same run of <paramref name="PlanPositionId"/>.</summary>
public record AssignTowerTokenDto(int PlanPositionId, string TokenId, string TowerId);

/// <summary>The graded assignment. <paramref name="Expected"/> is the word the tower asked for (revealed after the answer).</summary>
public record TowerAssignResult(bool Correct, string Expected);
