class_name LevelFeedback
extends Node
## The level's visible reactions to its [GameEvents]: spark bursts in the scene and the feedback line on screen. It
## only listens; [LevelSounds] hangs onto the same events the same way.

const SPARK := Color(1.0, 0.95, 0.8)
const GOOD := Color(0.4, 1.0, 0.5)
const BAD := Color(1.0, 0.25, 0.2)

var _scene: Node3D
var _controls: LevelControls
var _towers: Towers


## Connects the reactions: [param scene] holds the bursts, [param controls] shows the feedback line and the
## banners, [param towers] cheer along when a wave is cleared.
func setup(events: GameEvents, scene: Node3D, controls: LevelControls, towers: Towers) -> void:
	_scene = scene
	_towers = towers
	_controls = controls
	events.projectile_hit.connect(_on_projectile_hit)
	events.tower_fired.connect(_on_tower_fired)
	events.word_appeared.connect(_on_word_appeared)
	events.castle_hit.connect(_on_castle_hit)
	events.word_assigned.connect(_on_word_assigned)
	events.server_message.connect(_on_server_message)
	events.time_up.connect(_on_time_up)
	events.wave_started.connect(_on_wave_started)
	events.wave_cleared.connect(_on_wave_cleared)
	events.level_ended.connect(_on_level_ended)


func _on_projectile_hit(projectile: Projectile, at: Vector3) -> void:
	var kind := projectile.kind
	if kind.arc > 0.0:
		Fx.burst(_scene, at, kind.color.lightened(0.3), 22, 0.7)
	else:
		Fx.burst(_scene, at, SPARK, 10, 0.4)


func _on_tower_fired(shot: Towers.Shot) -> void:
	var tower := shot.tower
	if not tower.kind.blast:
		return
	Fx.blast(_scene, Geo.to3(tower.pos, tower.kind.muzzle_height))
	Fx.recoil(tower.model)


func _on_word_appeared(book: WordBook) -> void:
	Fx.burst(_scene, Geo.to3(book.pos, 0.8), UiKit.GOLD, 14, 0.5)


func _on_castle_hit(at: Vector3) -> void:
	Fx.burst(_scene, at, Color(1, 0.3, 0.2), 30)
	_controls.say("Ein Skelett hat die Burg erreicht!", Color.SALMON)


func _on_word_assigned(graded: GameEvents.Assignment) -> void:
	Fx.burst(_scene, Geo.to3(graded.tower.pos, 3.2), GOOD if graded.correct else BAD, 40)
	if not graded.correct:
		_controls.say("Falsch – „%s“ heißt „%s“  −%d Schuss" % [graded.target.prompt, graded.expected, Balance.WRONG_PENALTY], Color.SALMON)
	elif graded.leveled:
		Fx.burst(_scene, Geo.to3(graded.tower.pos, 4.4), UiKit.GOLD, 70, 1.4)
		_controls.say("Turm steigt auf Stufe %d!  +%d Schaden" % [graded.tower.level.level, graded.tower.level.damage_bonus()], UiKit.GOLD)
	elif graded.became_strong:
		_controls.say("Richtig! Der Turm schießt jetzt %s ★" % graded.tower.kind.strong_shot_name, Color.LIGHT_GREEN)
	else:
		_controls.say("Richtig! „%s“ = „%s“  +%d Schuss  +%d Kraft" % [graded.word.word, graded.target.prompt, Balance.REFILL_AMMO, graded.value], Color.LIGHT_GREEN)


func _on_wave_started(number: int) -> void:
	_controls.announce("Welle %d" % number, "Boss-Welle!" if number % Balance.BOSS_EVERY == 0 else "Die Skelette kommen!", Color.WHITE)


func _on_wave_cleared(number: int, bonus: int, flawless: bool) -> void:
	if flawless:
		_controls.announce("Welle %d geschafft!" % number, "+%d Punkte  ·  Burg repariert" % bonus, UiKit.GOLD)
	else:
		_controls.announce("Welle %d überstanden" % number, "+%d Punkte" % bonus, Color.WHITE)
	for tower: Tower in _towers.all:
		Fx.burst(_scene, Geo.to3(tower.pos, 4.2), UiKit.GOLD, 24, 0.9)


func _on_level_ended(_reason: LevelEnd.Reason, record: bool) -> void:
	if record:
		_controls.announce("Neuer Rekord!", "", UiKit.GOLD)


func _on_server_message(text: String) -> void:
	_controls.say(text, Color.ORANGE)


func _on_time_up() -> void:
	_controls.say("Die Spielzeit für heute ist um.", Color.ORANGE)
