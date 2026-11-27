class_name RunScore
extends RefCounted
## The points of an endless run: defeated enemies, correct words by their value, tower levels and cleared waves. Only
## for the screen and the high score - coins stay the server's own count.


## One line of the summary: what it counts, how often, and the points it brought.
class Line:
	extends RefCounted

	## What the line counts.
	var label: String

	## How often it happened.
	var count: int

	## The points it brought.
	var points: int

	func _init(what: String, times: int, sum: int) -> void:
		label = what
		count = times
		points = sum


## Defeated enemies.
var kills: int = 0

## Correct words.
var words: int = 0

## Level gains of all towers.
var tower_levels: int = 0

## Cleared waves.
var waves: int = 0

var _kill_points: int = 0
var _word_points: int = 0
var _level_points: int = 0
var _wave_points: int = 0


## An enemy went down; its kind says what it is worth.
func kill(kind: EnemyKind) -> void:
	kills += 1
	_kill_points += kind.points


## A correct word of the given value.
func word(value: int) -> void:
	words += 1
	_word_points += Balance.WORD_POINTS * value


## A tower reached the given level.
func tower_level(level: int) -> void:
	tower_levels += 1
	_level_points += Balance.LEVEL_POINTS * level


## The wave with the given number is cleared; returns the bonus it paid.
func wave(number: int) -> int:
	waves = maxi(waves, number)
	var bonus := Balance.WAVE_POINTS * number
	_wave_points += bonus
	return bonus


## All points of the run.
func total() -> int:
	return _kill_points + _word_points + _level_points + _wave_points


## The summary, one line per source of points.
func lines() -> Array[Line]:
	return [
		Line.new("Wellen geschafft", waves, _wave_points),
		Line.new("Monster besiegt", kills, _kill_points),
		Line.new("Vokabeln richtig", words, _word_points),
		Line.new("Turm-Aufstiege", tower_levels, _level_points),
	]
