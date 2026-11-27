class_name Level
extends Node3D
## One level of "Vokabel-Türme" in 3D (B-853): skeletons walk fixed roads towards the castle, towers shoot them
## while they have ammo, a defeated skeleton drops a vocabulary book. The hero walks the map - larger than the
## screen, the camera keeps the hero in the centre - picks up a word and hands it to a tower; the server grades it.
## A correct word refills the tower, adds its value to the tower's level and gives it the next meaning; a wrong one
## is gone and costs the tower three shots. The run is endless: the skeletons come in waves that grow, small ones
## bring points and the bigger ones drop the words, and it ends when the castle falls, the day's play time is up or
## the player stops. The hero shoots too - weaker than a tower but without ammo - so a run with empty towers still
## has a way back; words he lays down stay where they are.
##
## The level only coordinates: each part is its own node of level.tscn (hero, enemies, towers, projectiles, word
## books, controls, overlay), the rules run here in one frame order, and everything worth a sound or an effect is
## emitted on [member events] afterwards.

## What the result screen shows of a finished run.
class Summary:
	extends RefCounted

	## The run's points.
	var score: RunScore = RunScore.new()

	## The last wave the run reached.
	var wave: int = 0

	## The highest level a tower reached.
	var top_tower_level: int = 1

	## The run beat the map's best score.
	var record: bool = false

	## The map's best score (this run included).
	var best: int = 0


## Emitted once when the level ends: run id, correct and total assignments, and why it ended.
signal finished(run_id: int, correct: int, total: int, reason: LevelEnd.Reason)

const CAMERA_OFFSET := Vector3(0, 15, 9.5)

## The hero's look is the one line to change for another figure (see [HeroLook]).
const HERO_LOOK: PackedScene = preload("res://heroes/knight_look.tscn")

## The level's events for sound and effects.
var events: GameEvents = GameEvents.new()

## The client's count of the run.
var tally: RunTally

## The run's points.
var score: RunScore = RunScore.new()

## The run beat the map's best score (known once the level is over).
var record: bool = false

var _session: RunSession
var _def: LevelDef
var _map: MapInfo
var _stick: TouchStick = TouchStick.new()
var _give_tower: Tower
var _clock: float = 0.0
var _busy: bool = false
var _over: bool = false
var _paused: bool = false
var _breaches: int = 0

@onready var hero: Hero = %Hero
@onready var enemies: Enemies = %Enemies
@onready var towers: Towers = %Towers
@onready var projectiles: Projectiles = %Projectiles
@onready var words: WordBooks = %Words
@onready var controls: LevelControls = %Controls
@onready var sounds: LevelSounds = %Sounds
@onready var _world: Node3D = %World
@onready var _camera: Camera3D = %Camera
@onready var _overlay: Overlay = %Overlay
@onready var _feedback: LevelFeedback = %Feedback


## Wires the level to a run and a map; call before adding it to the tree.
func setup(server: TowerServer, run: TowerRun, def: LevelDef) -> void:
	_session = RunSession.new(server, run)
	_def = def
	tally = RunTally.new(run.goal)


func _ready() -> void:
	Log.line("start Lauf %d Karte %s, Ziel %d" % [_session.run.run_id, _def.title, _session.run.goal])
	_map = MapBuilder.build(_def, _world)
	towers.setup(_def, _map, Catalog.TOWER_MIX, _session.next_target)
	hero.setup(_map, HERO_LOOK.instantiate() as HeroLook, _def.player_start)
	enemies.setup(_def)
	words.setup(_def, _map, _session.run.tokens)

	_camera.look_at_from_position(CAMERA_OFFSET, Vector3.ZERO)
	_camera.make_current()
	_follow_hero()

	_overlay.setup(_camera, words, enemies, towers, hero, tally, _stick, score)
	controls.give_pressed.connect(_assign)
	controls.drop_pressed.connect(_lay_down)
	controls.pause_pressed.connect(toggle_pause)
	controls.give_up_pressed.connect(_end.bind(LevelEnd.Reason.GAVE_UP))
	controls.book_picked.connect(_pick_up)
	_session.time_up.connect(_on_time_up)
	_feedback.setup(events, self, controls, towers)
	sounds.setup(events)


## Opens or closes the pause menu (pause button, Android back button).
func toggle_pause() -> void:
	if _over:
		return
	_paused = not _paused
	controls.show_pause(_paused)
	Log.line("pause" if _paused else "weiter")
	_stick.release()
	events.pause_changed.emit(_paused)


func _input(event: InputEvent) -> void:
	if not _paused:
		_stick.handle(event, get_viewport().get_visible_rect().size.x)


func _process(delta: float) -> void:
	_clock += delta
	_overlay.clock = _clock
	_overlay.chosen = _give_tower
	if _paused:
		_overlay.queue_redraw()
		return
	if not _over:
		tally.elapsed += delta
		_session.tick(delta)
		_move_hero(delta)
		_spawn_enemies(delta)
		_move_enemies(delta)
		_shoot_towers(delta)
		_shoot_hero(delta)
		_update_context()
	_move_projectiles(delta)
	words.animate(delta, _clock, _over)
	controls.layout(get_viewport().get_visible_rect().size)
	_overlay.chosen = _give_tower
	_overlay.queue_redraw()


func _move_hero(dt: float) -> void:
	# Keyboard arrows as a desktop fallback for the left thumb. Screen up is world -Z (the camera looks north).
	var keys := Input.get_vector("ui_left", "ui_right", "ui_up", "ui_down")
	hero.move(dt, _stick.vector if _stick.vector != Vector2.ZERO else keys, _clock)
	_follow_hero()


## The camera keeps the hero in the centre: the map moves, the hero stays.
func _follow_hero() -> void:
	_camera.position = hero.position + Vector3(0, 0.8, 0) + CAMERA_OFFSET


func _spawn_enemies(dt: float) -> void:
	var enemy := enemies.spawn(dt)
	var cleared := enemies.waves.take_cleared()
	if cleared > 0:
		# Only a wave that never reached the castle repairs it.
		var flawless := _breaches == 0
		if flawless:
			tally.repair(Balance.WAVE_REPAIR)
		_breaches = 0
		var bonus := score.wave(cleared)
		Log.line("welle %d %s, Punkte %d" % [cleared, "geschafft" if flawless else "überstanden", score.total()])
		hero.look.play(HeroLook.Action.CHEER)
		events.wave_cleared.emit(cleared, bonus, flawless)
	var started := enemies.waves.take_started()
	if started > 0:
		events.wave_started.emit(started)
	if enemy != null:
		events.enemy_spawned.emit(enemy)


func _move_enemies(dt: float) -> void:
	for i: int in enemies.move(dt):
		_breaches += 1
		events.castle_hit.emit(Geo.to3(_def.castle(), 2.5))
		if tally.castle_hit():
			_end(LevelEnd.Reason.CASTLE_FALLEN)


func _shoot_towers(dt: float) -> void:
	for shot: Towers.Shot in towers.shoot(dt, enemies, projectiles):
		events.shot_fired.emit(shot.projectile)
		events.tower_fired.emit(shot)


## Plan B: the hero shoots the nearest skeleton in reach by himself; no ammo, so empty towers never end a run alone.
func _shoot_hero(dt: float) -> void:
	if not hero.reload.tick(dt):
		return
	var target := enemies.nearest(hero.ground, Balance.HERO_RANGE)
	if target == null:
		return
	hero.reload.start(Balance.HERO_SHOT_INTERVAL)
	tally.hero_shot()
	hero.look.play(HeroLook.Action.SHOOT)
	events.shot_fired.emit(projectiles.fire(Catalog.HERO_BOLT, Projectile.Shooter.HERO, Geo.to3(hero.ground, 1.4), target))


func _move_projectiles(dt: float) -> void:
	for hit: Projectiles.Hit in projectiles.move(dt):
		events.projectile_hit.emit(hit.projectile, hit.at)
		if not hit.killed:
			continue
		var enemy := hit.projectile.target
		if hit.projectile.from == Projectile.Shooter.HERO:
			tally.hero_kill()
		score.kill(enemy.kind)
		events.enemy_defeated.emit(enemy, hit.projectile.from)
		# Small enemies bring points only; the bigger ones carry the words.
		var fell := enemies.path_point(enemy)
		for i: int in enemy.kind.drops:
			var book := words.drop_beside(fell.pos, fell.dir)
			if book != null:
				events.word_appeared.emit(book)


func _update_context() -> void:
	# Every book within reach is offered on the right, nearest first.
	controls.show_nearby(words.near(hero.ground, Balance.PICKUP_RADIUS, 5))
	# The give button stays up as long as the hero carries a word next to a tower that still asks for one.
	var carried: TowerToken = null if _busy else hero.carried
	_give_tower = towers.nearest_asking(hero.ground, Balance.TOWER_REACH) if carried != null else null
	controls.show_carry(carried.word if _give_tower != null else "", carried.word if carried != null else "")


func _pick_up(book: WordBook) -> void:
	if _over or not words.take(book):
		return
	# One word at a time (B-822): picking another one puts the carried one down here.
	var previous := hero.release()
	if previous != null:
		events.word_appeared.emit(words.lay_down(previous, hero.ground))
	hero.carry(book.data)
	hero.look.play(HeroLook.Action.PICK_UP)
	events.word_picked_up.emit(book.data)
	controls.refresh_nearby()


## Sorting words: the carried word goes down next to the hero and stays there for later.
func _lay_down() -> void:
	var token := hero.carried
	if _over or _busy or token == null:
		return
	events.word_appeared.emit(words.lay_down(token, hero.ground))
	Log.line("abgelegt '%s'" % token.word)
	hero.release()
	hero.look.play(HeroLook.Action.LAY_DOWN)
	events.word_laid_down.emit(token)
	controls.refresh_nearby()


func _assign() -> void:
	var tower := _give_tower
	var token := hero.carried
	if _busy or _over or tower == null or tower.target == null or token == null:
		return
	var target := tower.target
	_busy = true
	controls.hide_give()
	hero.look.play(HeroLook.Action.GIVE)
	events.word_given.emit(tower, token)
	var result := await _session.assign(token, target)
	_busy = false
	if not is_inside_tree():
		return
	if not result.ok:
		events.server_message.emit(result.error)
		if result.run_closed:
			_end(LevelEnd.Reason.TIME_UP)
		return
	var graded := GameEvents.Assignment.new()
	graded.tower = tower
	graded.word = token
	graded.target = target
	graded.correct = result.data.get("correct", false)
	graded.expected = str(result.data.get("expected", ""))
	tally.assigned(graded.correct)
	hero.release()
	Log.line("assign '%s' -> '%s': %s" % [token.word, target.prompt,
		"richtig" if graded.correct else "falsch, erwartet " + graded.expected])
	if graded.correct:
		words.pool.consume(token.token_id)
		graded.value = target.value
		graded.became_strong = tower.ammo.refill()
		graded.leveled = tower.level.add(target.value)
		score.word(target.value)
		if graded.leveled:
			score.tower_level(tower.level.level)
			Log.line("turm '%s' Stufe %d" % [tower.kind.id, tower.level.level])
		tower.target = _session.next_target()
	else:
		# The word is gone from the hand; it goes back into the pool and drops again later.
		words.pool.give_back(token.token_id)
		tower.ammo.penalize()
	hero.look.play(HeroLook.Action.CORRECT if graded.correct else HeroLook.Action.WRONG)
	events.word_assigned.emit(graded)


## The run for the result screen.
func summary() -> Summary:
	var result := Summary.new()
	result.score = score
	result.wave = enemies.waves.wave
	for tower: Tower in towers.all:
		result.top_tower_level = maxi(result.top_tower_level, tower.level.level)
	result.record = record
	result.best = Records.best(_def.title)
	return result


func _on_time_up() -> void:
	events.time_up.emit()
	_end(LevelEnd.Reason.TIME_UP)


## The server pays by its own count of solved towers; the client only says the run is over and keeps the score.
func _end(reason: LevelEnd.Reason) -> void:
	if _over:
		return
	_over = true
	_paused = false
	controls.close()
	record = score.total() > 0 and Records.submit(_def.title, score.total())
	hero.look.play(HeroLook.Action.DEFEAT if reason == LevelEnd.Reason.CASTLE_FALLEN else HeroLook.Action.VICTORY)
	Log.line("%s, Welle %d, Punkte %d" % [tally.summary(LevelEnd.LOG_NAMES[reason]), enemies.waves.wave, score.total()])
	events.level_ended.emit(reason, record)
	# A short moment to see the hero cheer (or fall) before the result screen.
	await get_tree().create_timer(0.3 if reason == LevelEnd.Reason.GAVE_UP else 1.6).timeout
	finished.emit(_session.run.run_id, tally.correct, tally.total, reason)
