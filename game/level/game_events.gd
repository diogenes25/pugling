class_name GameEvents
extends RefCounted
## Everything noteworthy that happens in a level, emitted by the [Level] after the rules have run. Sound, effects
## and feedback texts connect here instead of being called from the game logic - so a new sound is one handler, not
## a call in five places. Handlers only present; they never change the game state.


## A word the server graded at a tower.
class Assignment:
	extends RefCounted

	## The tower it was given to.
	var tower: Tower

	## The word.
	var word: TowerToken

	## The meaning the tower asked for.
	var target: TowerTarget

	## The server's verdict.
	var correct: bool

	## The right word for the tower's meaning (shown after a wrong one).
	var expected: String

	## The refill just turned the tower strong.
	var became_strong: bool

	## What the word was worth for the tower (0 for a wrong one).
	var value: int

	## The word just lifted the tower to a new level.
	var leveled: bool


## A wave of enemies starts.
@warning_ignore("unused_signal")
signal wave_started(number: int)

## The last enemy of a wave is gone; the bonus is what the wave paid in points, and a flawless wave (no enemy
## reached the castle) repaired the castle.
@warning_ignore("unused_signal")
signal wave_cleared(number: int, bonus: int, flawless: bool)

## A skeleton entered the map.
@warning_ignore("unused_signal")
signal enemy_spawned(enemy: Enemy)

## A tower or the hero fired.
@warning_ignore("unused_signal")
signal shot_fired(projectile: Projectile)

## A tower fired (in addition to [signal shot_fired]): for the tower's own launch effects.
@warning_ignore("unused_signal")
signal tower_fired(shot: Towers.Shot)

## A projectile reached its living target, at the given point.
@warning_ignore("unused_signal")
signal projectile_hit(projectile: Projectile, at: Vector3)

## A skeleton was defeated; the shooter says who finished it off.
@warning_ignore("unused_signal")
signal enemy_defeated(enemy: Enemy, by: Projectile.Shooter)

## A healer mended this enemy.
@warning_ignore("unused_signal")
signal enemy_healed(enemy: Enemy)

## A skeleton reached the castle at the given point.
@warning_ignore("unused_signal")
signal castle_hit(at: Vector3)

## A book appeared on the field (dropped by a skeleton or laid down by the hero).
@warning_ignore("unused_signal")
signal word_appeared(book: WordBook)

## A book went back into the pool unpicked.
@warning_ignore("unused_signal")
signal word_expired(token: TowerToken)

## The hero picked a word up.
@warning_ignore("unused_signal")
signal word_picked_up(token: TowerToken)

## The hero laid his word down.
@warning_ignore("unused_signal")
signal word_laid_down(token: TowerToken)

## The hero handed a word to a tower; the server's verdict is still out.
@warning_ignore("unused_signal")
signal word_given(tower: Tower, token: TowerToken)

## The server graded a word.
@warning_ignore("unused_signal")
signal word_assigned(assignment: Assignment)

## A call to the server failed; the text is ready for the screen.
@warning_ignore("unused_signal")
signal server_message(text: String)

## Today's play time is used up.
@warning_ignore("unused_signal")
signal time_up

## The level was paused (true) or resumed (false).
@warning_ignore("unused_signal")
signal pause_changed(paused: bool)

## The level ended; the flag says whether the run set a new high score.
@warning_ignore("unused_signal")
signal level_ended(reason: LevelEnd.Reason, record: bool)
