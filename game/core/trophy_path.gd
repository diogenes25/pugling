class_name TrophyPath
extends RefCounted
## The trophy path as the start screen shows it: where the child stands between the last step and the next one. The
## server counts the trophies and pays the steps; this only reads its numbers.


## One step of the path.
class Step:
	extends RefCounted

	## Lifetime trophies that reach the step.
	var trophies: int

	## Gems the step pays once.
	var gems: int

	func _init(at: int, pays: int) -> void:
		trophies = at
		gems = pays


## The steps, lowest first.
var steps: Array[Step] = []


## Adds a step; the path keeps itself sorted by threshold.
func add(at: int, pays: int) -> void:
	steps.append(Step.new(at, pays))
	steps.sort_custom(func(a: Step, b: Step) -> bool: return a.trophies < b.trophies)


## The first step the total has not reached yet, or null when the path is done (or empty).
func next(total: int) -> Step:
	for step: Step in steps:
		if total < step.trophies:
			return step
	return null


## Steps the total has reached.
func reached(total: int) -> int:
	var count := 0
	for step: Step in steps:
		if total >= step.trophies:
			count += 1
	return count


## How far the total is from the last reached step to the next one, 0 to 1; 1 when the path is done.
func progress(total: int) -> float:
	var goal := next(total)
	if goal == null:
		return 1.0
	var done := reached(total)
	var from := 0 if done == 0 else steps[done - 1].trophies
	return float(total - from) / float(goal.trophies - from)
