class_name LevelSounds
extends Node3D
## The level's sound, hung onto its [GameEvents] like [LevelFeedback]. Shots, hits and the castle sound where they
## happen (3D, heard from the camera, so a tower far off screen is quieter); everything about words and the end of
## the level plays flat at full volume, because that is what the player is waiting for. The effects are generated,
## not downloaded (assets/sfx, made by tools/sfx.py).

## How many sounds may overlap in the scene; the oldest is cut off when all are busy.
const VOICES: int = 10

## The same sound again within this many seconds is skipped: six towers firing at once is noise, not news.
const MIN_GAP: float = 0.09

## Emitted for every sound that actually starts; nothing in the game listens, the smoke runs count by it.
signal played(sound: String)

var _streams: Dictionary[String, AudioStream] = {}
var _last_played: Dictionary[String, float] = {}
var _voices: Array[AudioStreamPlayer3D] = []
var _flat: AudioStreamPlayer = AudioStreamPlayer.new()
# The end fanfare gets its own player so the last "correct" is not cut off by it.
var _fanfare: AudioStreamPlayer = AudioStreamPlayer.new()
var _next: int = 0


func _ready() -> void:
	add_child(_flat)
	add_child(_fanfare)
	for i: int in VOICES:
		var voice := AudioStreamPlayer3D.new()
		# The camera hangs about 18 units above the hero: sounds around him play at full volume.
		voice.unit_size = 18.0
		voice.max_distance = 60.0
		voice.attenuation_filter_cutoff_hz = 20500.0
		add_child(voice)
		_voices.append(voice)


## Connects the sounds to the level's events.
func setup(events: GameEvents) -> void:
	events.shot_fired.connect(_on_shot_fired)
	events.enemy_defeated.connect(_on_enemy_defeated)
	events.castle_hit.connect(_on_castle_hit)
	events.word_picked_up.connect(_on_word_picked_up)
	events.word_laid_down.connect(_on_word_laid_down)
	events.word_assigned.connect(_on_word_assigned)
	events.level_ended.connect(_on_level_ended)
	events.wave_started.connect(_on_wave_started)
	events.wave_cleared.connect(_on_wave_cleared)


## The sound of a shot, by what was fired.
static func shot_sound(kind: ProjectileKind) -> String:
	match kind.id:
		"hero-bolt":
			return "punch"
		"boulder", "fire-boulder":
			return "cannon"
		"fire-arrow":
			return "fire_arrow"
	return "arrow"


func _on_shot_fired(projectile: Projectile) -> void:
	_at(shot_sound(projectile.kind), projectile.node.position, -4.0 if projectile.from == Projectile.Shooter.HERO else -9.0)


func _on_enemy_defeated(enemy: Enemy, _by: Projectile.Shooter) -> void:
	_at("bones", enemy.node.position, -6.0)


func _on_castle_hit(at: Vector3) -> void:
	_at("castle_hit", at, 0.0)


func _on_word_picked_up(_token: TowerToken) -> void:
	_play_flat(_flat, "pickup", -4.0)


func _on_word_laid_down(_token: TowerToken) -> void:
	_play_flat(_flat, "drop", -4.0)


func _on_word_assigned(graded: GameEvents.Assignment) -> void:
	_play_flat(_flat, "correct" if graded.correct else "wrong", -2.0)
	if graded.leveled:
		_play_flat(_fanfare, "level_up", 0.0)


func _on_wave_started(_number: int) -> void:
	_play_flat(_fanfare, "wave_start", -3.0)


func _on_wave_cleared(_number: int, _bonus: int, _flawless: bool) -> void:
	_play_flat(_fanfare, "wave_clear", 0.0)


## A fallen castle sounds like a defeat - unless the run still set a record.
func _on_level_ended(reason: LevelEnd.Reason, record: bool) -> void:
	_play_flat(_fanfare, "defeat" if reason == LevelEnd.Reason.CASTLE_FALLEN and not record else "victory", 0.0)


func _at(sound: String, at: Vector3, volume_db: float) -> void:
	if not is_inside_tree() or _voices.is_empty() or not _due(sound):
		return
	var voice := _voices[_next]
	_next = (_next + 1) % _voices.size()
	voice.stream = _stream(sound)
	voice.volume_db = volume_db
	voice.global_position = at
	voice.play()
	played.emit(sound)


func _play_flat(player: AudioStreamPlayer, sound: String, volume_db: float) -> void:
	if not is_inside_tree() or not _due(sound):
		return
	player.stream = _stream(sound)
	player.volume_db = volume_db
	player.play()
	played.emit(sound)


func _due(sound: String) -> bool:
	var now := Time.get_ticks_msec() / 1000.0
	if _last_played.has(sound) and now - _last_played[sound] < MIN_GAP:
		return false
	_last_played[sound] = now
	return true


func _stream(sound: String) -> AudioStream:
	if not _streams.has(sound):
		_streams[sound] = load("res://assets/sfx/%s.wav" % sound) as AudioStream
	return _streams[sound]
