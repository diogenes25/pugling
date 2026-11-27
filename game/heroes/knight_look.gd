extends HeroLook
## The hero as the KayKit knight (CC0): a rigged 3D model that turns where he walks and plays the pack's clips.
## knight_look.tscn inherits the model's scene and puts this script on its root.

const KEEP: Array[String] = ["Knight_", "1H_Sword", "Round_Shield"]

var _anim: AnimationPlayer
# While a one-shot clip plays, walking and standing must not cut it off.
var _action_time: float = 0.0


func _ready() -> void:
	scale = Vector3.ONE * 0.8
	Models.keep_only(self, KEEP)
	_anim = Models.animator(self)


func update(dt: float, moving: bool, yaw: float) -> void:
	_action_time -= dt
	if _action_time <= 0.0:
		Models.play(_anim, true, ["Running_A" if moving else "Idle"])
	rotation = Vector3(0, yaw, 0)


func play(action: Action) -> void:
	match action:
		Action.PICK_UP:
			_act(0.5, ["PickUp", "Interact"])
		Action.LAY_DOWN:
			_act(0.4, ["PickUp", "Interact"])
		Action.GIVE:
			_act(0.6, ["Interact", "PickUp"])
		Action.VICTORY:
			Models.play(_anim, false, ["Cheer"])
		Action.DEFEAT:
			Models.play(_anim, false, ["Death_A"])
		# The knight has no clips for shooting or for a graded word; his sword arm stays as it is.


func _act(seconds: float, clips: Array[String]) -> void:
	_action_time = seconds
	Models.play(_anim, false, clips)
