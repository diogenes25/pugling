class_name Cooldown
extends RefCounted
## A reload timer: ticks down every frame and reports when a new shot may go.

var _left: float = 0.0


## Counts down by [param dt]; true once the timer has run out.
func tick(dt: float) -> bool:
	_left -= dt
	return _left <= 0.0


## Starts the next wait.
func start(seconds: float) -> void:
	_left = seconds
