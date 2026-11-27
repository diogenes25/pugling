class_name RunTally
extends RefCounted
## The client's count of one run: words given, the castle's lives, the hero's shots. Only for the screen and the log -
## whether the level is won is the server's own count (B-851).

## Correct words the server pays the run for (it counts them itself).
var goal: int

## Correct assignments so far.
var correct: int = 0

## All graded assignments so far.
var total: int = 0

## Skeletons the castle can still take.
var lives: int = Balance.CASTLE_LIVES

## Played seconds, without pauses.
var elapsed: float = 0.0

## Shots the hero fired himself.
var hero_shots: int = 0

## Skeletons the hero's own shots finished off.
var hero_kills: int = 0


func _init(words_needed: int) -> void:
	goal = words_needed


## The goal of correct words is reached.
func goal_reached() -> bool:
	return correct >= goal


## Counts a graded assignment.
func assigned(was_correct: bool) -> void:
	total += 1
	if was_correct:
		correct += 1


## Repairs the castle by some lives, never above the full count.
func repair(amount: int) -> void:
	lives = mini(Balance.CASTLE_LIVES, lives + amount)


## A skeleton reached the castle; returns whether the castle has fallen.
func castle_hit() -> bool:
	lives -= 1
	return lives <= 0


## Counts one shot of the hero.
func hero_shot() -> void:
	hero_shots += 1


## Counts one skeleton the hero finished off.
func hero_kill() -> void:
	hero_kills += 1


## The run's last log line; the reports on the phone are read from it, so keep its words stable.
func summary(reason: String) -> String:
	return "ende %s %d/%d richtig, %.0fs, Burg %d/%d, Heldenschüsse %d, Heldentreffer %d" % [
		reason, correct, total, elapsed, lives, Balance.CASTLE_LIVES, hero_shots, hero_kills]
