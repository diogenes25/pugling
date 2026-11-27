class_name TouchStick
extends RefCounted
## A virtual joystick for the left thumb: it appears where the finger lands on the left part of the screen and
## reports a direction of length up to 1. The level asks it every frame; the overlay draws it.

## Pixels the knob travels for full speed.
const REACH: float = 90.0

const LEFT_SHARE: float = 0.45
const DEAD_ZONE: float = 10.0

## Where the thumb landed.
var center: Vector2 = Vector2.ZERO

## The steering direction, length 0..1 (screen axes: x right, y down).
var vector: Vector2 = Vector2.ZERO

var _finger: int = -1


## A finger is steering.
func held() -> bool:
	return _finger >= 0


## Feeds a touch event; only the first finger on the left part of the screen steers.
func handle(event: InputEvent, screen_width: float) -> void:
	var touch := event as InputEventScreenTouch
	var drag := event as InputEventScreenDrag
	if touch != null:
		if touch.pressed and _finger < 0 and touch.position.x < screen_width * LEFT_SHARE:
			_finger = touch.index
			center = touch.position
			vector = Vector2.ZERO
		elif not touch.pressed and touch.index == _finger:
			release()
	elif drag != null and drag.index == _finger:
		var offset := drag.position - center
		vector = Vector2.ZERO if offset.length() < DEAD_ZONE else offset.limit_length(REACH) / REACH


## Lets go of the stick (the level paused or ended).
func release() -> void:
	_finger = -1
	vector = Vector2.ZERO
