extends Node
## Smoke run of the level on a [FakeApi]: a bot plays all three maps to their goal the way the C# reference run did
## (pause, a wrong word, lay down, the right word), then stages the other endings and checks the rules a player
## relies on. Every check is logged as "check <what>: ok" or "ABWEICHUNG"; exit code 0 only when none deviated.
## Run: godot --headless --path . res://tests/smoke_level.tscn
## With a window and "-- --shots=<folder>" it also saves pictures of the level.

const LEVEL: PackedScene = preload("res://level/level.tscn")
const WATCHDOG_SECONDS: float = 600.0
const ALL_SOUNDS: Array[String] = ["arrow", "fire_arrow", "cannon", "punch", "bones", "castle_hit", "pickup", "drop",
	"correct", "wrong", "victory", "defeat", "wave_start", "wave_clear", "level_up"]

var _level: Level
var _fake: FakeApi
var _failed: int = 0
var _ended: bool = false
var _end_reason: LevelEnd.Reason
var _sounds: Dictionary[String, int] = {}
var _spawned: Array[Enemy] = []
var _shots: Array[String] = []
var _messages: Array[String] = []
var _blasts: int = 0
var _pauses: Array[bool] = []
var _min_book_gap: float = INF
var _highest_boulder: float = 0.0
var _blinked: bool = false
var _boulders_landed: int = 0
var _last_hero_shot: float = -1.0
var _shortest_hero_gap: float = INF
var _fired: Dictionary[Tower, int] = {}
var _banners: Array[String] = []
var _hardest_hit: int = 0


func _ready() -> void:
	get_tree().create_timer(WATCHDOG_SECONDS, true, false, true).timeout.connect(_give_up)
	Engine.time_scale = 3.0 if UiDriver.arg("shots") != "" else 5.0
	DirAccess.remove_absolute(ProjectSettings.globalize_path(Records.FILE))
	for index: int in Catalog.LEVELS.size():
		await _play_to_goal(index)
	await _waves()
	await _time_up()
	await _run_closed()
	await _castle_falls()
	await _words_expire()
	await _catapult_and_fire()
	for sound: String in ALL_SOUNDS:
		_check("Sound %s gespielt (%d×)" % [sound, _sounds.get(sound, 0)], _sounds.get(sound, 0) > 0)
	Log.line("check fertig, %d Abweichungen" % _failed)
	get_tree().quit(1 if _failed > 0 else 0)


func _process(_delta: float) -> void:
	if _level == null or _ended:
		return
	var banner := _level.controls.banner()
	if banner != "" and not _banners.has(banner):
		_banners.append(banner)
	for a: WordBook in _level.words.all:
		if not a.placed and a.age > Balance.TOKEN_LIFETIME - Balance.TOKEN_BLINK_WARNING and not a.node.visible:
			_blinked = true
		for b: WordBook in _level.words.all:
			if a != b:
				_min_book_gap = minf(_min_book_gap, a.pos.distance_to(b.pos))


# --- scenarios ---------------------------------------------------------------------------------------------------

## The reference run: pause and resume, then until the goal: pick a word, give it (wrong), pick the next, lay it
## down, pick it again, give it (right).
func _play_to_goal(index: int) -> void:
	var def := Catalog.LEVELS[index]
	await _start(def)
	var hero := _level.hero
	var towers := _level.towers
	_check("%s: %d Türme, Bolzenturm und Katapult reihum" % [def.title, towers.all.size()],
			towers.all.size() == def.tower_spots.size() and towers.all[0].kind.id == "bolt-tower" and towers.all[1].kind.id == "catapult"
			and towers.all[2].kind.id == "bolt-tower")
	_check("%s: jeder Turm startet mit 16 Schuss und einer Bedeutung" % def.title,
			towers.all.all(func(t: Tower) -> bool: return t.ammo.count == Balance.START_AMMO and Balance.START_AMMO == 16 and t.target != null))
	_check("%s: Kamera folgt dem Helden, Abstand (0, 15, 9,5), Sichtwinkel 42" % def.title, _camera_follows())
	if index == 0:
		await _check_walking(def)
	if index == 1:
		_check_river(def)
	if index == 2:
		_check("Burgberg: zwei Wege, beide enden an der Burg", def.paths.size() == 2 and def.paths[0][def.paths[0].size() - 1] == def.paths[1][def.paths[1].size() - 1])

	await _seconds(2.5)
	_level.toggle_pause()
	var before := _level.tally.elapsed
	await _frames(20)
	_check("%s: Pause hält die Spielzeit an und zeigt das Menü" % def.title,
			_level.tally.elapsed == before and UiDriver.find_button(_level, "Aufhören") != null)
	await UiDriver.shot(get_tree(), "level-%d-pause" % index)
	_check("%s: Knopf „Weiter“ setzt fort" % def.title, UiDriver.press(_level, "Weiter"))
	await _frames(2)
	_check("%s: Pause-Knopf oben rechts ist da" % def.title, UiDriver.find_button(_level, "II") != null)

	var gives := 0
	while not _ended and _level.tally.correct < 3:
		var book := await _wait_for_book()
		if book == null:
			break
		await _stand_at(book.pos)
		_check_pick_list()
		_level.controls.book_picked.emit(book)
		await _frames(2)
		if gives == 0 and index == 0:
			var floating := hero.get("_book") as Node3D
			_check("Ritter: PickUp beim Aufheben, das Buch schwebt über ihm (Höhe %.1f)" % floating.position.y,
					_clip() == "PickUp" and floating.visible and floating.position.y > 2.3)
		if gives % 2 == 1:
			# Sorting: lay the word down, see that it stays as a placed book, and take it again.
			var word := hero.carried
			_check("%s: Knopf „„%s“ ablegen“" % [def.title, word.word], UiDriver.find_button(_level, "„%s“ ablegen" % word.word) != null)
			_level.controls.drop_pressed.emit()
			await _frames(2)
			var laid := _book_of(word)
			_check("%s: abgelegtes Wort liegt als bleibendes Buch" % def.title, hero.carried == null and laid != null and laid.placed)
			if gives == 1 and index == 0:
				await _check_swap(laid)
			else:
				_level.controls.book_picked.emit(laid)
				await _frames(2)
		var tower := towers.nearest_asking(hero.ground, 1000.0)
		if gives == 3 and index == 0:
			# One refill short of strong: this correct word must turn the tower and say so.
			tower.ammo.count = 27
			tower.level.power = 0
		if gives == 3 and index == 1:
			# One value point short of the second level: this correct word must lift the tower and say so.
			tower.level.power = 3
		await _stand_at(tower.pos + Vector2(0, 2.0))
		await _frames(2)
		if gives == 0:
			await UiDriver.shot(get_tree(), "level-%d-geben" % index)
		_check("%s: grüner Geben-Knopf am Turm" % def.title, UiDriver.find_button(_level, "geben") != null)
		await _give_and_check(tower, def.title)
		gives += 1
	await _seconds(1.0)
	_check("%s: das Wortziel beendet den Level nicht mehr (endlos)" % def.title, not _ended and _level.tally.goal_reached())
	var points := _level.score.total()
	var lines := 0
	for line: RunScore.Line in _level.score.lines():
		lines += line.points
	_check("%s: Punkte aus Monstern, Wörtern und Turm-Aufstiegen (%d), Zeilen ergeben die Summe" % [def.title, points],
			points >= 3 * Balance.WORD_POINTS and lines == points and _level.score.words == 3)
	_level.toggle_pause()
	await _frames(2)
	UiDriver.press(_level, "Aufhören")
	await _until_finished()
	_check("%s: Aufhören beendet den Lauf (%d Richtige von %d)" % [def.title, _level.tally.correct, _level.tally.total],
			_end_reason == LevelEnd.Reason.GAVE_UP and _level.tally.correct == 3 and _level.tally.total == 6)
	_check("%s: erster Lauf auf der Karte ist ein Rekord (%d), Ritter jubelt (Cheer)" % [def.title, Records.best(def.title)],
			_level.record and Records.best(def.title) == _level.score.total() and _clip() == "Cheer"
			and _level.summary().best == _level.score.total())
	_check("%s: Pause, Weiter und Pause gemeldet" % def.title, _pauses == ([true, false, true] as Array[bool]))
	_check("%s: Heartbeat etwa alle 15 s (%d in %.0f s)" % [def.title, _fake.beats(), _level.tally.elapsed],
			absi(_fake.beats() - int(_level.tally.elapsed / 15.0)) <= 1)
	_check_enemies(def)
	if index == 0:
		_check("Lichtung: Held schießt selbst (%d Schüsse, alle hero-bolt als punch)" % _level.tally.hero_shots,
				_level.tally.hero_shots > 0 and _shots.has("hero-bolt"))
		_check("Lichtung: Heldenschuss mit Reichweite 5,5, Takt 1,0 s (kürzester Abstand %.2f s) und 1 Schaden" % _shortest_hero_gap,
				Balance.HERO_RANGE == 5.5 and Balance.HERO_SHOT_INTERVAL == 1.0 and Catalog.HERO_BOLT.damage == 1
				and _shortest_hero_gap >= 1.0 and _shortest_hero_gap < 100.0)
		_check("Lichtung: starker Turm meldet sich und schießt Feuer (%s)" % ", ".join(_unique(_shots)),
				_messages.has("Richtig! Der Turm schießt jetzt Feuerpfeile ★") or _messages.has("Richtig! Der Turm schießt jetzt Feuerkugeln ★"))
	await _stop()


func _time_up() -> void:
	await _start(Catalog.LEVELS[0], func(fake: FakeApi) -> void: fake.time_up_at_beat = 1)
	await _until_finished()
	_check("Zeit um: Heartbeat nach 15 s beendet den Level (%.0f s)" % _level.tally.elapsed,
			_end_reason == LevelEnd.Reason.TIME_UP and _level.tally.elapsed >= 15.0 and _level.tally.elapsed < 17.0)
	_check("Zeit um: Rückmeldezeile", _messages.has("Die Spielzeit für heute ist um."))
	await _stop()


func _run_closed() -> void:
	await _start(Catalog.LEVELS[0], func(fake: FakeApi) -> void: fake.closed_at_assign = 1)
	var book := await _wait_for_book()
	await _stand_at(book.pos)
	_level.controls.book_picked.emit(book)
	await _frames(2)
	await _stand_at(_level.towers.all[0].pos + Vector2(0, 2.0))
	await _frames(2)
	_level.controls.give_pressed.emit()
	await _until_finished()
	_check("Lauf geschlossen: „Das Level ist vorbei.“ beendet den Level",
			_end_reason == LevelEnd.Reason.TIME_UP and _messages.has("Das Level ist vorbei."))
	await _stop()


func _castle_falls() -> void:
	await _start(Catalog.LEVELS[0])
	for tower: Tower in _level.towers.all:
		tower.ammo.count = 0
	# Far from the road, so the hero's own shot does not hold the skeletons.
	_level.hero.ground = Vector2(2, 24)
	await _until_finished()
	_check("Burg: nach 10 Skeletten fällt sie (Leben %d, Welle %d)" % [_level.tally.lives, _level.enemies.waves.wave],
			_end_reason == LevelEnd.Reason.CASTLE_FALLEN and _level.tally.lives == 0 and _level.enemies.waves.wave == 2)
	_check("Burg: eine Welle mit Durchbruch ist nur überstanden und repariert nichts", _banners.has("Welle 1 überstanden")
			and not _banners.has("Welle 1 geschafft!"))
	_check("Burg: kein Rekord, der Lauf endet als Niederlage", not _level.record)
	_check("Burg: Rückmeldezeile", _messages.has("Ein Skelett hat die Burg erreicht!"))
	_check("Burg: Ritter fällt (Death_A)", _clip() == "Death_A")
	_check("Burg: leere Türme schießen nicht", not _shots.has("bolt") and not _shots.has("boulder"))
	await _stop()


func _words_expire() -> void:
	await _start(Catalog.LEVELS[0])
	var first := await _wait_for_book()
	_check("Wörter: Buch fällt neben den Weg (Abstand %.1f)" % Geo.dist_to_polyline(first.pos, Catalog.LEVELS[0].paths[0]),
			Geo.dist_to_polyline(first.pos, Catalog.LEVELS[0].paths[0]) > 1.0 and not first.placed)
	# A laid-down word next to it: it must outlive the dropped one.
	var second := await _wait_for_book(first)
	await _stand_at(second.pos)
	_level.controls.book_picked.emit(second)
	await _frames(2)
	var word := _level.hero.carried
	_level.controls.drop_pressed.emit()
	await _frames(2)
	_level.hero.ground = Vector2(2, 24)
	while _level.words.all.has(first) and not _ended:
		await get_tree().process_frame
	_check("Wörter: Buch verfällt nach 30 s (%.1f s) und blinkt vorher" % first.age,
			first.age >= Balance.TOKEN_LIFETIME and first.age < Balance.TOKEN_LIFETIME + 1.0 and _blinked)
	await _seconds(8.0)
	# Seven words fall on the same spot: they must spread out, and the list offers the nearest five.
	var fell := Geo.point_at(Catalog.LEVELS[0].paths[0], 30.0)
	for i: int in 7:
		_level.words.drop_beside(fell.pos, fell.dir)
	var smallest := INF
	for a: WordBook in _level.words.all:
		for b: WordBook in _level.words.all:
			if a != b:
				smallest = minf(smallest, a.pos.distance_to(b.pos))
	_check("Wörter: %d Bücher halten Abstand 2,4 (kleinster %.2f)" % [_level.words.all.size(), smallest],
			_level.words.all.size() >= 8 and smallest >= WordBooks.GAP - 0.001)
	# Seven laid-down words in reach at once (a centre and six around it): the list still offers only five.
	var spot := _open_ground(_level.get("_map") as MapInfo)
	for i: int in 7:
		var offset := Vector2.ZERO if i == 0 else Vector2.from_angle(TAU * i / 6.0) * 2.45
		_level.words.lay_down(TowerToken.new("extra%d" % i, "wort%d" % i), spot + offset)
	await _stand_at(spot)
	var in_reach := _level.words.near(_level.hero.ground, Balance.PICKUP_RADIUS, 99).size()
	var offered := _level.words.near(_level.hero.ground, Balance.PICKUP_RADIUS, 5)
	_check("Wörter: Aufheben-Liste bietet höchstens 5 von %d in Reichweite, das nächste zuerst" % in_reach,
			in_reach == 7 and offered.size() == 5 and offered[0].data.word == "wort0")
	_check_pick_list()
	await UiDriver.shot(get_tree(), "level-aufheben-liste")
	_level.hero.ground = Vector2(2, 24)
	var laid := _book_of(word)
	_check("Wörter: abgelegtes Buch bleibt liegen", laid != null and laid.placed)
	_level.toggle_pause()
	await _frames(2)
	UiDriver.press(_level, "Aufhören")
	await _until_finished()
	_check("Aufhören beendet den Level", _end_reason == LevelEnd.Reason.GAVE_UP)
	await _stop()


## Only the catapult may shoot first, then both towers are stocked beyond thirty: stone in a bow with fire, smoke and
## recoil, then fire arrows and fire boulders.
func _catapult_and_fire() -> void:
	await _start(Catalog.LEVELS[0])
	var towers := _level.towers.all
	towers[0].ammo.count = 0
	towers[2].ammo.count = 0
	# Next to the catapult, away from the road: the hero must not finish the skeletons before the stone lands.
	_level.hero.ground = Vector2(16, 19.5)
	# Measured once a stone has landed: its bow peaks halfway, long after the launch effects.
	while (_blasts == 0 or _boulders_landed == 0) and not _ended:
		await get_tree().process_frame
	_check("Katapult: wirft im Bogen (höchster Punkt %.1f über Mündung 3,4) mit Feuer, Rauch und Rückstoß" % _highest_boulder,
			_highest_boulder > 4.5 and _blasts > 0 and _shots.has("boulder"))
	towers[0].ammo.count = 40
	towers[1].ammo.count = 40
	await _frames(2)
	_check("Stark: mehr als 30 Schuss zeigen ★ am Turmschild", towers[0].ammo.is_strong() and towers[1].ammo.is_strong())
	await UiDriver.shot(get_tree(), "level-stark")
	while not (_shots.has("fire-arrow") and _shots.has("fire-boulder")) and not _ended:
		await get_tree().process_frame
	_check("Stark: Bolzenturm schießt Feuerpfeile, Katapult Feuerkugeln (Schaden 2 und 3, mit Funkenschweif)",
			_shots.has("fire-arrow") and _shots.has("fire-boulder") and towers[0].kind.strong_shot.damage == 2
			and towers[1].kind.strong_shot.damage == 3 and towers[1].kind.strong_shot.burning)
	towers[0].ammo.count = 3
	await _frames(2)
	_check("Wenig Munition: drei Schuss oder weniger gelten als knapp (rot)", towers[0].ammo.is_low())
	# The tower at the far end of the map is off screen from here: its badge sits on the edge.
	await UiDriver.shot(get_tree(), "level-randmarker")
	_level.toggle_pause()
	await _frames(2)
	UiDriver.press(_level, "Aufhören")
	await _until_finished()
	await _stop()


## Well stocked towers clear the first wave without a breach: banner, jingle, bonus, repair, then the second wave
## comes with a bigger troop.
func _waves() -> void:
	await _start(Catalog.LEVELS[0])
	for tower: Tower in _level.towers.all:
		tower.ammo.count = 500
		tower.level.add(TowerLevel.needed(3))
	_level.tally.lives = 8
	# Far from the road: the towers do the work, and no word is picked up.
	_level.hero.ground = Vector2(2, 24)
	var cleared: Array[int] = []
	var bonuses: Array[int] = []
	_level.events.wave_cleared.connect(func(number: int, bonus: int, _flawless: bool) -> void:
		cleared.append(number)
		bonuses.append(bonus))
	var started: Array[int] = []
	_level.events.wave_started.connect(func(number: int) -> void: started.append(number))
	await _seconds(2.5)
	_check("Wellen: die erste Welle wird groß angekündigt", _banners.has("Welle 1") and started == ([1] as Array[int]))
	while cleared.is_empty() and not _ended:
		await get_tree().process_frame
	var kills := _level.score.kills
	_check("Wellen: Welle 1 geschafft nach %d besiegten Monstern, Bonus %d" % [kills, bonuses[0]],
			cleared == ([1] as Array[int]) and kills == 9 and bonuses[0] == 100 and _level.score.waves == 1)
	_check("Wellen: Punkte = 5 kleine × 10 + 4 Krieger × 30 + 100 Bonus (%d)" % _level.score.total(), _level.score.total() == 270)
	_check("Wellen: eine Welle ohne Durchbruch repariert die Burg um 1 (Leben %d)" % _level.tally.lives, _level.tally.lives == 9)
	await _frames(3)
	_check("Wellen: Banner „Welle 1 geschafft!“ und der Ritter jubelt mitten im Lauf", _banners.has("Welle 1 geschafft!") and _clip() == "Cheer")
	await UiDriver.shot(get_tree(), "level-welle-geschafft")
	var words_dropped := _level.words.all.size()
	_check("Wellen: vier Krieger haben vier Wörter fallen lassen (%d)" % words_dropped, words_dropped == 4)
	var before := _spawned.size()
	await _seconds(Balance.WAVE_REST - 1.0)
	_check("Wellen: während der Pause von 5 s kommt niemand", _spawned.size() == before and _level.enemies.waves.wave == 1)
	await _seconds(2.0)
	_check("Wellen: danach beginnt Welle 2", started == ([1, 2] as Array[int]) and _level.enemies.waves.wave == 2
			and _banners.has("Welle 2"))
	_check("Wellen: stärkere Türme treffen härter (Stufe %d, +%d Schaden)" % [_level.towers.all[0].level.level, _level.towers.all[0].level.damage_bonus()],
			_level.towers.all[0].level.level == 4 and _level.towers.all[0].level.damage_bonus() == 3
			and _hardest_hit == _level.towers.all[0].kind.strong_shot.damage + 3)
	_level.toggle_pause()
	await _frames(2)
	UiDriver.press(_level, "Aufhören")
	await _until_finished()
	await _stop()


# --- checks ------------------------------------------------------------------------------------------------------

func _camera_follows() -> bool:
	var camera := _level.get_viewport().get_camera_3d()
	return camera.position.is_equal_approx(_level.hero.position + Vector3(0, 15.8, 9.5)) and is_equal_approx(camera.fov, 42.0)


## Keyboard, joystick and sliding along an obstacle.
func _check_walking(def: LevelDef) -> void:
	var hero := _level.hero
	var start := hero.ground
	var player := Models.animator(hero.look)
	var has_clips := true
	for clip: String in ["Idle", "Running_A", "PickUp", "Interact", "Cheer", "Death_A"]:
		has_clips = has_clips and player != null and player.has_animation(clip)
	_check("Ritter: Modell mit den Clips Idle, Running_A, PickUp, Interact, Cheer, Death_A", has_clips)
	Input.action_press("ui_right")
	await _seconds(0.4)
	var running := _clip()
	Input.action_release("ui_right")
	_check("Laufen: Pfeiltaste rechts bewegt den Helden nach rechts", hero.ground.x > start.x + 1.0 and is_equal_approx(hero.ground.y, start.y))
	_check("Laufen: Kamera bleibt beim Helden", _camera_follows())
	await _seconds(0.3)
	_check("Ritter: läuft mit Running_A, steht mit Idle, schaut in Laufrichtung (%.2f rad)" % hero.look.rotation.y,
			running == "Running_A" and _clip() == "Idle" and hero.look.rotation.y > 1.0)

	# The left thumb lands anywhere on the left part of the screen and drags up.
	var from := hero.ground
	var touch := InputEventScreenTouch.new()
	touch.index = 0
	touch.pressed = true
	touch.position = Vector2(300, 400)
	_touch(touch)
	var drag := InputEventScreenDrag.new()
	drag.index = 0
	drag.position = Vector2(300, 250)
	_touch(drag)
	await _seconds(0.4)
	await UiDriver.shot(get_tree(), "level-0-joystick")
	var lifted := InputEventScreenTouch.new()
	lifted.index = 0
	lifted.pressed = false
	lifted.position = Vector2(300, 250)
	_touch(lifted)
	await _frames(2)
	_check("Laufen: Joystick links (frei aufgesetzt) bewegt den Helden nach oben", hero.ground.y < from.y - 1.0)
	var rest := hero.ground
	await _frames(5)
	_check("Laufen: losgelassener Joystick hält an", hero.ground == rest)

	# Diagonally into a tower: the blocked axis stops, the free one keeps going.
	var tower := def.tower_spots[0]
	hero.ground = tower + Vector2(-1.5, 0)
	var at := hero.ground
	hero.move(0.02, Vector2(1, 1).normalized(), 0.0)
	_check("Laufen: Gleiten am Turm (x bleibt, y läuft weiter)", is_equal_approx(hero.ground.x, at.x) and hero.ground.y > at.y)
	hero.ground = Vector2(0.5, 5)
	hero.move(0.02, Vector2.LEFT, 0.0)
	_check("Laufen: Kartenrand hält den Helden", hero.ground.x >= Balance.HERO_RADIUS)
	hero.ground = def.player_start


func _check_river(def: LevelDef) -> void:
	var map := MapInfo.new(def)
	_check("Flussfurt: drei Brücken, der Fluss sperrt, die Brücke trägt",
			def.bridges.size() == 3 and not map.walkable(Vector2(24.5, 11.5), Balance.HERO_RADIUS)
			and map.walkable(def.bridges[1], Balance.HERO_RADIUS))


func _check_pick_list() -> void:
	var near := _level.words.near(_level.hero.ground, Balance.PICKUP_RADIUS, 5)
	var listed := 0
	for book: WordBook in near:
		if UiDriver.find_button(_level, book.data.word) != null:
			listed += 1
	_check("Aufheben-Liste zeigt die %d Wörter in Reichweite (höchstens 5)" % near.size(),
			listed == near.size() and near.size() <= 5 and UiDriver.texts(_level).has("Aufheben:"))


## Carrying nothing next to a placed book and a second book: picking one, then the other, swaps them.
func _check_swap(laid: WordBook) -> void:
	var hero := _level.hero
	var first := laid.data
	_level.controls.book_picked.emit(laid)
	await _frames(2)
	var other := await _wait_for_book()
	if other == null:
		return
	await _stand_at(other.pos)
	_level.controls.book_picked.emit(other)
	await _frames(2)
	var back := _book_of(first)
	_check("Tauschen: neues Wort in der Hand, das getragene liegt als bleibendes Buch",
			hero.carried == other.data and back != null and back.placed)


func _give_and_check(tower: Tower, map: String) -> void:
	var word := _level.hero.carried
	var target := tower.target
	var ammo := tower.ammo.count
	var strong := tower.ammo.is_strong()
	var power := tower.level.power
	var level := tower.level.level
	var shots_before := _shot_count_of(tower)
	_level.controls.give_pressed.emit()
	while _level.hero.carried == word and not _ended:
		await get_tree().process_frame
	_note_feedback()
	var spent := _shot_count_of(tower) - shots_before
	var graded_right := _level.tally.correct * 2 == _level.tally.total
	if graded_right:
		# A word's value decides the refill: 10, 12 or 14 shots.
		var refill := 8 + 2 * target.value
		_check("%s: richtig gibt +%d Schuss (Wert %d) und die nächste Bedeutung" % [map, refill, target.value],
				tower.ammo.count == ammo + refill - spent and tower.target != target)
		_check("%s: der Turm bekommt den Wert des Wortes (%d) als Kraft" % [map, target.value],
				tower.level.power == power + target.value and target.value >= 1 and target.value <= 3)
		if tower.level.level > level:
			_check("%s: der Turm steigt auf Stufe %d, meldet es und schießt mit +%d Schaden" % [map, tower.level.level, tower.level.damage_bonus()],
					tower.level.level == 2 and tower.level.damage_bonus() == 1
					and _messages.has("Turm steigt auf Stufe 2!  +1 Schaden") and _level.score.tower_levels == 1)
		elif not strong and tower.ammo.is_strong():
			_check("%s: mehr als 30 Schuss machen den Turm stark (★ im Schild)" % map, tower.ammo.count > 30)
		else:
			_check("%s: Rückmeldezeile richtig" % map,
					_messages.has("Richtig! „%s“ = „%s“  +%d Schuss  +%d Kraft" % [word.word, target.prompt, 8 + 2 * target.value, target.value]))
	else:
		_check("%s: falsch kostet 3 Schuss, die Bedeutung bleibt" % map,
				tower.ammo.count == maxi(0, ammo - spent - 3) and tower.target == target)
		_check("%s: Rückmeldezeile falsch" % map, _messages.has("Falsch – „%s“ heißt „x“  −3 Schuss" % target.prompt))
		_check("%s: falsches Wort ist aus der Hand und wieder im Pool" % map,
				_level.hero.carried == null and _book_of(word) == null)


func _check_enemies(def: LevelDef) -> void:
	var troop := _level.enemies.waves.troop(1)
	var wrong_kind := 0
	for i: int in mini(troop.size(), _spawned.size()):
		if _spawned[i].kind != troop[i]:
			wrong_kind += 1
	_check("%s: die erste Welle marschiert wie geplant (5 kleine, 4 Krieger mit 4 HP, langsamer; %d gekommen)" % [def.title, _spawned.size()],
			wrong_kind == 0 and _spawned.size() >= 4 and troop.count(Catalog.MINION) == 5 and troop.count(Catalog.WARRIOR) == 4
			and Catalog.WARRIOR.hp == 4 and Catalog.MINION.hp == 2 and _spawned[1].speed < _spawned[0].speed
			and is_equal_approx(_spawned[0].speed, def.monster_speed))
	_check("%s: nur die Krieger tragen Wörter, die kleinen bringen Punkte" % def.title,
			Catalog.MINION.drops == 0 and Catalog.WARRIOR.drops == 1 and Catalog.BOSS.drops == 3
			and Catalog.MINION.points < Catalog.WARRIOR.points and Catalog.WARRIOR.points < Catalog.BOSS.points)
	var paths: Dictionary[int, bool] = {}
	for enemy: Enemy in _spawned:
		paths[enemy.path] = true
	_check("%s: Skelette nutzen alle %d Wege" % [def.title, def.paths.size()], paths.size() == def.paths.size())


## A spot where a book and six more around it (2.45 apart) all find free, walkable ground.
func _open_ground(map: MapInfo) -> Vector2:
	for x: int in range(4, int(map.def.size.x) - 4):
		for y: int in range(4, int(map.def.size.y) - 4):
			var centre := Vector2(x, y)
			var free := map.walkable(centre, 0.3)
			for i: int in 6:
				free = free and map.walkable(centre + Vector2.from_angle(TAU * i / 6.0) * 2.45, 0.3)
			for book: WordBook in _level.words.all:
				free = free and book.pos.distance_to(centre) > 2.45 + WordBooks.GAP
			if free:
				return centre
	return map.def.player_start


# --- plumbing ----------------------------------------------------------------------------------------------------

func _start(def: LevelDef, prepare: Callable = Callable()) -> void:
	_fake = FakeApi.new()
	add_child(_fake)
	if prepare.is_valid():
		prepare.call(_fake)
	var started := await _fake.start_run(1)
	_ended = false
	_spawned.clear()
	_shots.clear()
	_messages.clear()
	_pauses.clear()
	_blasts = 0
	_min_book_gap = INF
	_highest_boulder = 0.0
	_blinked = false
	_boulders_landed = 0
	_last_hero_shot = -1.0
	_shortest_hero_gap = INF
	_fired.clear()
	_banners.clear()
	_hardest_hit = 0
	_level = LEVEL.instantiate()
	_level.setup(_fake, TowerRun.from_json(started.data), def)
	_level.finished.connect(_on_finished)
	add_child(_level)
	_level.sounds.played.connect(func(sound: String) -> void: _sounds[sound] = _sounds.get(sound, 0) + 1)
	var events := _level.events
	events.enemy_spawned.connect(func(enemy: Enemy) -> void: _spawned.append(enemy))
	events.shot_fired.connect(_on_shot)
	events.projectile_hit.connect(func(projectile: Projectile, _at: Vector3) -> void:
		if projectile.kind.id == "fire-arrow":
			_hardest_hit = maxi(_hardest_hit, projectile.damage))
	events.tower_fired.connect(_on_tower_fired)
	events.pause_changed.connect(func(paused: bool) -> void: _pauses.append(paused))
	events.server_message.connect(func(text: String) -> void: _messages.append(text))
	await _frames(2)
	await UiDriver.shot(get_tree(), "level-start-%s" % def.title)


## The clip the knight plays (or played last).
func _clip() -> String:
	var player := Models.animator(_level.hero.look)
	return player.assigned_animation if player != null else ""


func _on_shot(projectile: Projectile) -> void:
	_shots.append(projectile.kind.id)
	if projectile.from == Projectile.Shooter.HERO:
		if _last_hero_shot >= 0.0:
			_shortest_hero_gap = minf(_shortest_hero_gap, _level.tally.elapsed - _last_hero_shot)
		_last_hero_shot = _level.tally.elapsed
	_check_sound_matches(projectile)
	if projectile.kind.arc > 0.0:
		_follow_boulder(projectile)


func _check_sound_matches(projectile: Projectile) -> void:
	var wanted: Dictionary[String, String] = {"bolt": "arrow", "fire-arrow": "fire_arrow", "boulder": "cannon",
		"fire-boulder": "cannon", "hero-bolt": "punch"}
	if LevelSounds.shot_sound(projectile.kind) != wanted[projectile.kind.id]:
		_check("Sound zum Geschoss %s" % projectile.kind.id, false)


func _follow_boulder(projectile: Projectile) -> void:
	while is_instance_valid(projectile.node):
		_highest_boulder = maxf(_highest_boulder, projectile.node.position.y)
		await get_tree().process_frame
	_boulders_landed += 1


## A catapult's launch must leave fire, smoke and a squashed model behind.
func _on_tower_fired(shot: Towers.Shot) -> void:
	_fired[shot.tower] = _fired.get(shot.tower, 0) + 1
	if not shot.tower.kind.blast:
		return
	var rest := shot.tower.model.scale
	await _frames(2)
	if not is_instance_valid(_level) or not is_instance_valid(shot.tower.model):
		return
	var particles := _level.find_children("*", "CPUParticles3D", false, false).size()
	if particles >= 3 and not shot.tower.model.scale.is_equal_approx(rest):
		_blasts += 1
		if _blasts == 1:
			await UiDriver.shot(get_tree(), "level-katapult")


func _on_finished(_run_id: int, _correct: int, _total: int, reason: LevelEnd.Reason) -> void:
	_ended = true
	_end_reason = reason


func _until_finished() -> void:
	while not _ended:
		_note_feedback()
		await get_tree().process_frame
	_note_feedback()


func _stop() -> void:
	await UiDriver.shot(get_tree(), "level-ende-%s" % LevelEnd.LOG_NAMES[_end_reason])
	_level.queue_free()
	_fake.queue_free()
	_level = null
	await _frames(3)


## Remembers every line the feedback label shows.
func _note_feedback() -> void:
	if not is_instance_valid(_level):
		return
	for text: String in UiDriver.texts(_level):
		if not _messages.has(text):
			_messages.append(text)


func _wait_for_book(other_than: WordBook = null) -> WordBook:
	while not _ended:
		_note_feedback()
		for book: WordBook in _level.words.all:
			if book != other_than and book.age < Balance.TOKEN_LIFETIME - 8.0:
				return book
		await get_tree().process_frame
	return null


func _stand_at(spot: Vector2) -> void:
	_level.hero.ground = spot
	await _frames(2)
	_note_feedback()


func _book_of(token: TowerToken) -> WordBook:
	for book: WordBook in _level.words.all:
		if book.data == token:
			return book
	return null


## Shots a tower has fired in this level.
func _shot_count_of(tower: Tower) -> int:
	return _fired.get(tower, 0)


## Feeds a touch in viewport coordinates. Headless has no window, so the way through Input would scale the position
## by a window of size zero.
func _touch(event: InputEvent) -> void:
	get_viewport().push_input(event, true)


func _frames(count: int) -> void:
	for i: int in count:
		await get_tree().process_frame


## Waits game seconds (the run is sped up).
func _seconds(seconds: float) -> void:
	await get_tree().create_timer(seconds, false).timeout


func _unique(values: Array[String]) -> Array[String]:
	var seen: Array[String] = []
	for value: String in values:
		if not seen.has(value):
			seen.append(value)
	return seen


func _check(what: String, ok: bool) -> void:
	if not ok:
		_failed += 1
	Log.line("check %s: %s" % [what, "ok" if ok else "ABWEICHUNG"])


func _give_up() -> void:
	Log.line("check ABBRUCH: Zeit abgelaufen")
	get_tree().quit(1)
