namespace Pugling.Contracts.Student;

// Contract of "Vokabel-Türme" (B-851): a native game that assigns vocabulary tokens to towers. The server picks the
// material, grades every assignment, counts the play time and pays the coins; tower and token ids are opaque, so the run
// payload never reveals which token belongs to which tower.

/// <summary>
/// The child's tower game: the plan positions whose vocabulary a run can be built from, today's play time against the
/// limit (<paramref name="LimitSeconds"/> 0 = the game is off) and today's tower-game coins against their own cap.
/// </summary>
public record TowerGameOverview(IReadOnlyList<TowerGamePosition> Positions, int PlayedSecondsToday, int LimitSeconds,
    int CoinsToday, int DailyCoinCap);

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
/// name="Towers"/> and refills a slot after a correct assignment; <paramref name="Goal"/> solved towers win the level
/// (and pay its coins), but the run goes on for as long as the castle stands. <paramref name="Tokens"/> hold the words
/// of all towers; they follow the order of the towers only roughly (shuffled within small groups), so the client can
/// drop words the towers on the field are likely to ask for without learning which token fits which tower. A token
/// whose tower is not on the field yet acts as a distractor. The app sends a heartbeat about every 15 seconds while the
/// level is in the foreground.
/// </summary>
public record TowerRunResponse(int RunId, int PlanPositionId, int Goal, int TowerSlots, IReadOnlyList<TowerTarget> Towers,
    IReadOnlyList<TowerToken> Tokens);

/// <summary>
/// A tower: an opaque id and the meaning (German) it asks for. <paramref name="Value"/> is what the word is worth for
/// the tower in the game, by the child's learning state: 3 for a new or weak word, 2 for one on its way, 1 for a well
/// learned one - the game pays more for the words that need the practice.
/// </summary>
public record TowerTarget(string TowerId, string Prompt, int Value);

/// <summary>A dropped vocabulary token: an opaque id and the foreign word it carries.</summary>
public record TowerToken(string TokenId, string Word);

/// <summary>Assigns a token to a tower; both ids come from the run <paramref name="RunId"/>.</summary>
public record AssignTowerTokenDto(int RunId, string TokenId, string TowerId);

/// <summary>The graded assignment. <paramref name="Expected"/> is the word the tower asked for (revealed after the answer).</summary>
public record TowerAssignResult(bool Correct, string Expected);

/// <summary>
/// Play time after a heartbeat. <paramref name="TimeUp"/> is set once the daily limit plus the grace time for the running
/// level is used up: the run takes no more assignments and the app ends the level.
/// </summary>
public record TowerHeartbeatResult(int PlayedSecondsToday, int LimitSeconds, bool TimeUp);

/// <summary>
/// The ended level, by the server's own count. <paramref name="CoinsAwarded"/> is cut to what is left of today's cap;
/// ending a run twice returns the same result and pays nothing more.
/// </summary>
public record TowerRunEndResult(bool Won, int Solved, int Goal, int CoinsAwarded, int CoinsToday, int DailyCoinCap);
