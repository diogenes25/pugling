class_name TowerLevel
extends RefCounted
## A tower's growth over an endless run: every correct word adds its value - a new word is worth more than a well
## learned one - and past each threshold the tower gains a level and its shots hit harder.

## The tower's level, starting at 1.
var level: int = 1

## The summed value of all words the tower got.
var power: int = 0

## Correct words the tower got.
var words: int = 0


## The summed word value a tower needs to reach the level after [param current]: each level costs one step more
## than the one before (4, 12, 24, 40 ...).
@warning_ignore("integer_division")
static func needed(current: int) -> int:
	return Balance.LEVEL_STEP * current * (current + 1) / 2


## A correct word of the given value; returns whether the tower just gained a level.
func add(value: int) -> bool:
	var before := level
	words += 1
	power += value
	while power >= needed(level):
		level += 1
	return level > before


## Extra hit points every shot of the tower takes.
func damage_bonus() -> int:
	return level - 1


## How far the tower is on its way to the next level, 0 to 1.
func progress() -> float:
	var from := 0 if level == 1 else needed(level - 1)
	return float(power - from) / float(needed(level) - from)
