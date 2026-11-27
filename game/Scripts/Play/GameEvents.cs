using Godot;
using Pugling.Contracts.Student;
using PuglingTower.Combat;
using PuglingTower.Words;

namespace PuglingTower.Play;

/// <summary>A word the server graded at a tower.</summary>
/// <param name="Tower">The tower it was given to.</param>
/// <param name="Word">The word.</param>
/// <param name="Target">The meaning the tower asked for.</param>
/// <param name="Correct">The server's verdict.</param>
/// <param name="Expected">The right word for the tower's meaning (shown after a wrong one).</param>
/// <param name="BecameStrong">The refill just turned the tower strong.</param>
public sealed record Assignment(Tower Tower, TowerToken Word, TowerTarget Target, bool Correct, string Expected, bool BecameStrong);

/// <summary>
/// Everything noteworthy that happens in a level, raised by the <see cref="Level"/> after the rules have run. Sound,
/// effects and feedback texts subscribe here instead of being called from the game logic - so a new sound is one
/// handler, not a call in five places. Handlers only present; they never change the game state.
/// </summary>
public sealed class GameEvents
{
    /// <summary>A skeleton entered the map.</summary>
    public event Action<Enemy>? EnemySpawned;

    /// <summary>A tower or the hero fired.</summary>
    public event Action<Projectile>? ShotFired;

    /// <summary>A tower fired (in addition to <see cref="ShotFired"/>): for the tower's own launch effects.</summary>
    public event Action<TowerShot>? TowerFired;

    /// <summary>A projectile reached its living target, at the given point.</summary>
    public event Action<Projectile, Vector3>? ProjectileHit;

    /// <summary>A skeleton was defeated; the shooter says who finished it off.</summary>
    public event Action<Enemy, Shooter>? EnemyDefeated;

    /// <summary>A skeleton reached the castle at the given point.</summary>
    public event Action<Vector3>? CastleHit;

    /// <summary>A book appeared on the field (dropped by a skeleton or laid down by the hero).</summary>
    public event Action<WordBook>? WordAppeared;

    /// <summary>A book went back into the pool unpicked.</summary>
    public event Action<TowerToken>? WordExpired;

    /// <summary>The hero picked a word up.</summary>
    public event Action<TowerToken>? WordPickedUp;

    /// <summary>The hero laid his word down.</summary>
    public event Action<TowerToken>? WordLaidDown;

    /// <summary>The hero handed a word to a tower; the server's verdict is still out.</summary>
    public event Action<Tower, TowerToken>? WordGiven;

    /// <summary>The server graded a word.</summary>
    public event Action<Assignment>? WordAssigned;

    /// <summary>A call to the server failed; the text is ready for the screen.</summary>
    public event Action<string>? ServerMessage;

    /// <summary>Today's play time is used up.</summary>
    public event Action? TimeUp;

    /// <summary>The level was paused (true) or resumed (false).</summary>
    public event Action<bool>? PauseChanged;

    /// <summary>The level ended; the flag says whether the goal of correct words was reached.</summary>
    public event Action<LevelEnd, bool>? LevelEnded;

    /// <summary>Raises <see cref="EnemySpawned"/>.</summary>
    public void RaiseEnemySpawned(Enemy enemy) => EnemySpawned?.Invoke(enemy);

    /// <summary>Raises <see cref="ShotFired"/>.</summary>
    public void RaiseShotFired(Projectile projectile) => ShotFired?.Invoke(projectile);

    /// <summary>Raises <see cref="TowerFired"/>.</summary>
    public void RaiseTowerFired(TowerShot shot) => TowerFired?.Invoke(shot);

    /// <summary>Raises <see cref="ProjectileHit"/>.</summary>
    public void RaiseProjectileHit(Projectile projectile, Vector3 at) => ProjectileHit?.Invoke(projectile, at);

    /// <summary>Raises <see cref="EnemyDefeated"/>.</summary>
    public void RaiseEnemyDefeated(Enemy enemy, Shooter by) => EnemyDefeated?.Invoke(enemy, by);

    /// <summary>Raises <see cref="CastleHit"/>.</summary>
    public void RaiseCastleHit(Vector3 at) => CastleHit?.Invoke(at);

    /// <summary>Raises <see cref="WordAppeared"/>.</summary>
    public void RaiseWordAppeared(WordBook book) => WordAppeared?.Invoke(book);

    /// <summary>Raises <see cref="WordExpired"/>.</summary>
    public void RaiseWordExpired(TowerToken token) => WordExpired?.Invoke(token);

    /// <summary>Raises <see cref="WordPickedUp"/>.</summary>
    public void RaiseWordPickedUp(TowerToken token) => WordPickedUp?.Invoke(token);

    /// <summary>Raises <see cref="WordLaidDown"/>.</summary>
    public void RaiseWordLaidDown(TowerToken token) => WordLaidDown?.Invoke(token);

    /// <summary>Raises <see cref="WordGiven"/>.</summary>
    public void RaiseWordGiven(Tower tower, TowerToken token) => WordGiven?.Invoke(tower, token);

    /// <summary>Raises <see cref="WordAssigned"/>.</summary>
    public void RaiseWordAssigned(Assignment assignment) => WordAssigned?.Invoke(assignment);

    /// <summary>Raises <see cref="ServerMessage"/>.</summary>
    public void RaiseServerMessage(string text) => ServerMessage?.Invoke(text);

    /// <summary>Raises <see cref="TimeUp"/>.</summary>
    public void RaiseTimeUp() => TimeUp?.Invoke();

    /// <summary>Raises <see cref="PauseChanged"/>.</summary>
    public void RaisePauseChanged(bool paused) => PauseChanged?.Invoke(paused);

    /// <summary>Raises <see cref="LevelEnded"/>.</summary>
    public void RaiseLevelEnded(LevelEnd reason, bool goalReached) => LevelEnded?.Invoke(reason, goalReached);
}
