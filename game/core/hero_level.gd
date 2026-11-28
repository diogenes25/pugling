class_name HeroLevel
extends RefCounted
## The hero's lasting level. It follows the trophies the server counted over all runs, so it grows only with
## correct words. A higher level makes the hero's own shot a little quicker and, every few levels, harder - he stays
## the towers' plan B and never replaces them.


## The trophies that reach [param level]: each level costs one step more than the one before (0, 30, 90, 180 ...).
@warning_ignore("integer_division")
static func needed(level: int) -> int:
	return Balance.HERO_LEVEL_STEP * (level - 1) * level / 2


## The level a trophy total stands for, starting at 1.
static func of(trophies: int) -> int:
	var level := 1
	while trophies >= needed(level + 1):
		level += 1
	return level


## How far the total is on its way from its level to the next, 0 to 1.
static func progress(trophies: int) -> float:
	var level := of(trophies)
	return float(trophies - needed(level)) / float(needed(level + 1) - needed(level))


## Seconds between two shots of a hero of that level, never below [constant Balance.HERO_RELOAD_FLOOR].
static func shot_interval(level: int) -> float:
	return maxf(Balance.HERO_RELOAD_FLOOR, Balance.HERO_SHOT_INTERVAL * (1.0 - Balance.HERO_RELOAD_GAIN * (level - 1)))


## Extra hit points the hero's shot takes: one more every [constant Balance.HERO_DAMAGE_EVERY] levels.
@warning_ignore("integer_division")
static func damage_bonus(level: int) -> int:
	return (level - 1) / Balance.HERO_DAMAGE_EVERY
