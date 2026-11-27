@abstract
class_name HeroLook
extends Node3D
## How the hero is shown - the one seam between the game and the figure on screen. [Hero] owns position, speed and
## the carried word; a look only turns that into something visible. Another hero (a flat billboard figure with
## sprite frames, another 3D model) is a new scene whose root script extends this class.

## A moment the hero's look may react to with a gesture. Looks ignore what they have no clip for.
enum Action {
	## Picked a word up.
	PICK_UP,
	## Laid a word down.
	LAY_DOWN,
	## Handed a word to a tower.
	GIVE,
	## Fired his own shot.
	SHOOT,
	## The server graded his word as correct.
	CORRECT,
	## The server graded his word as wrong.
	WRONG,
	## A wave is cleared: a cheer in the middle of the run.
	CHEER,
	## The run is over and ended well.
	VICTORY,
	## The castle fell or the time is up.
	DEFEAT,
}


## Called every frame while the level runs: whether the hero walks, and the direction he faces (rotation around the
## y axis in radians, 0 looks towards the viewer). A flat figure may mirror by it or ignore it.
@abstract
func update(dt: float, moving: bool, yaw: float) -> void


## Plays the gesture for a moment.
@abstract
func play(action: Action) -> void
