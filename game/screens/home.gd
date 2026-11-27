class_name HomeScreen
extends Control
## The start screen: the hero in the middle, the big play button bottom right with the map and the words next to it,
## on top the wallet, the streak and the best score, on the left today's tasks and on the right the chest. Everything
## about the day comes from the server ([method TowerServer.home], [method TowerServer.overview]); the screen only
## shows it, so a task is never ticked off by the game itself.

## "SPIELEN" was pressed: this map with these words.
signal play(level: LevelDef, plan_position_id: int)

## The words card was pressed.
signal pick_words

## The settings button was pressed.
signal open_settings

## The server did not answer; the text is ready for the screen. Main tries a fresh login once.
signal load_failed(error: String)

const HERO_LOOK: PackedScene = preload("res://heroes/knight_look.tscn")
const SKY := Color(0.16, 0.22, 0.42)
const PANEL := Color(0.06, 0.08, 0.18, 0.78)
const DONE := Color(0.5, 0.95, 0.55)
const CHEER_EVERY: float = 9.0

## The server; set before the screen enters the tree.
var server: TowerServer

## The words to play; 0 picks the first position the server offers.
var plan_position_id: int = 0

var _map_index: int = 0
var _positions: Array = []
var _hero: HeroLook
var _since_cheer: float = 0.0
var _wallet: Label = UiKit.caption("")
var _tasks: VBoxContainer = VBoxContainer.new()
var _chest: Label = UiKit.text_line("", 22)
var _game_day: Label = UiKit.text_line("", 22)
var _map_card: Button = UiKit.button("", Color(0.25, 0.35, 0.6), 26)
var _words_card: Button = UiKit.button("", Color(0.25, 0.35, 0.6), 24)
var _play: Button = UiKit.button("SPIELEN", Color(0.95, 0.72, 0.1), 56)

## The line for progress and error messages.
var status: Label = UiKit.caption("")


func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_map_index = clampi(Settings.map(), 0, Catalog.LEVELS.size() - 1)
	_build_stage()
	_build_top()
	_build_left()
	_build_right()
	_build_bottom()
	_show_choice()
	_load()


func _process(delta: float) -> void:
	# The hero idles and cheers now and then, so the screen is alive.
	_since_cheer += delta
	if _since_cheer >= CHEER_EVERY:
		_since_cheer = 0.0
		_hero.play(HeroLook.Action.CHEER)
	_hero.update(delta, false, 0.35 * sin(Time.get_ticks_msec() / 1400.0))


## The hero on a small stage of his own world, behind the controls.
func _build_stage() -> void:
	var back := ColorRect.new()
	back.color = SKY
	back.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(back)
	var container := SubViewportContainer.new()
	container.stretch = true
	container.set_anchors_preset(Control.PRESET_FULL_RECT)
	container.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(container)
	var stage := SubViewport.new()
	stage.own_world_3d = true
	stage.transparent_bg = true
	container.add_child(stage)
	var sun := DirectionalLight3D.new()
	sun.rotation_degrees = Vector3(-50, 35, 0)
	stage.add_child(sun)
	var fill := OmniLight3D.new()
	fill.position = Vector3(-2, 2.5, 3)
	fill.light_energy = 1.2
	stage.add_child(fill)
	var camera := Camera3D.new()
	camera.fov = 36
	stage.add_child(camera)
	camera.look_at_from_position(Vector3(0, 1.7, 7.0), Vector3(0, 0.75, 0))
	var ground := MeshInstance3D.new()
	var disc := CylinderMesh.new()
	disc.top_radius = 1.5
	disc.bottom_radius = 1.7
	disc.height = 0.16
	ground.mesh = disc
	ground.position = Vector3(0, -0.08, 0)
	ground.material_override = Fx.unshaded(Color(0.35, 0.72, 0.3))
	stage.add_child(ground)
	_hero = HERO_LOOK.instantiate() as HeroLook
	stage.add_child(_hero)


func _build_top() -> void:
	var bar := HBoxContainer.new()
	_place(bar, 0, 0, 1, 0, 20, 16, -20, 84)
	bar.add_theme_constant_override("separation", 18)
	add_child(bar)
	var player := UiKit.caption(server.child_name)
	player.add_theme_font_size_override("font_size", 38)
	bar.add_child(player)
	_wallet.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_wallet.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	_wallet.add_theme_font_size_override("font_size", 30)
	bar.add_child(_wallet)
	var settings := UiKit.button("Einstellungen", UiKit.NIGHT, 22)
	settings.custom_minimum_size = Vector2(190, 60)
	settings.pressed.connect(open_settings.emit)
	bar.add_child(settings)


func _build_left() -> void:
	var panel := _panel("Tagesaufgaben")
	_place(panel, 0, 0, 0, 1, 20, 100, 420, -170)
	_tasks.add_theme_constant_override("separation", 8)
	(panel.get_child(0) as VBoxContainer).add_child(_tasks)
	add_child(panel)


func _build_right() -> void:
	var panel := _panel("Truhe & Spiel")
	_place(panel, 1, 0, 1, 1, -380, 100, -20, -170)
	var box := panel.get_child(0) as VBoxContainer
	box.add_child(_chest)
	box.add_child(_game_day)
	add_child(panel)


func _build_bottom() -> void:
	var row := HBoxContainer.new()
	_place(row, 0, 1, 1, 1, 20, -150, -20, -20)
	row.alignment = BoxContainer.ALIGNMENT_END
	row.add_theme_constant_override("separation", 18)
	add_child(row)
	status.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	status.horizontal_alignment = HORIZONTAL_ALIGNMENT_LEFT
	status.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	row.add_child(status)
	# A long exercise title wraps inside the card and the card grows upwards; it is never cut off.
	_words_card.custom_minimum_size = Vector2(420, 110)
	_words_card.size_flags_vertical = Control.SIZE_SHRINK_END
	_words_card.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_words_card.pressed.connect(pick_words.emit)
	row.add_child(_words_card)
	_map_card.custom_minimum_size = Vector2(230, 110)
	_map_card.size_flags_vertical = Control.SIZE_SHRINK_END
	_map_card.pressed.connect(_next_map)
	row.add_child(_map_card)
	_play.custom_minimum_size = Vector2(320, 130)
	_play.pressed.connect(_on_play)
	row.add_child(_play)


## Pins a control to the screen: the anchors as fractions of the screen, the offsets in pixels from them.
static func _place(control: Control, left: float, top: float, right: float, bottom: float,
		from_left: float, from_top: float, from_right: float, from_bottom: float) -> void:
	control.anchor_left = left
	control.anchor_top = top
	control.anchor_right = right
	control.anchor_bottom = bottom
	control.offset_left = from_left
	control.offset_top = from_top
	control.offset_right = from_right
	control.offset_bottom = from_bottom


## A dark rounded panel with a headline; its first child is the column to fill.
func _panel(headline: String) -> PanelContainer:
	var panel := PanelContainer.new()
	var box := StyleBoxFlat.new()
	box.bg_color = PANEL
	box.set_corner_radius_all(20)
	box.set_content_margin_all(18)
	panel.add_theme_stylebox_override("panel", box)
	var column := VBoxContainer.new()
	column.add_theme_constant_override("separation", 10)
	panel.add_child(column)
	var title := UiKit.text_line(headline, 30)
	title.add_theme_color_override("font_color", UiKit.GOLD)
	column.add_child(title)
	return panel


func _load() -> void:
	status.text = "Lade …"
	@warning_ignore("redundant_await")
	var overview := await server.overview()
	if not is_inside_tree():
		return
	if not overview.ok:
		status.text = overview.error
		load_failed.emit(overview.error)
		return
	_positions = overview.data.get("positions", [])
	_show_game_day(overview.data)
	_show_choice()
	@warning_ignore("redundant_await")
	var day := await server.home()
	if not is_inside_tree():
		return
	status.text = "" if not _positions.is_empty() else "Kein aktiver Plan mit genug Wörtern."
	if day.ok:
		_show_day(HomeInfo.from_json(day.data.get("wallet", {}), day.data.get("today", {}), day.data.get("missions", [])))


func _show_game_day(overview: Dictionary) -> void:
	var seconds_left := maxi(0, int(overview.get("limitSeconds", 0)) - int(overview.get("playedSecondsToday", 0)))
	@warning_ignore("integer_division")
	var minutes_left := seconds_left / 60
	_game_day.text = "Spielzeit heute noch %d Min.\nSpiel-Münzen heute %d/%d" % [
		minutes_left, int(overview.get("coinsToday", 0)), int(overview.get("dailyCoinCap", 0))]


func _show_day(info: HomeInfo) -> void:
	_wallet.text = "Serie %d   ·   Münzen %d   ·   Gems %d   ·   Rekord %d" % [info.streak, info.coins, info.gems, _best()]
	for child: Node in _tasks.get_children():
		child.queue_free()
	if info.goals_total > 0:
		_task("Lernziele heute", info.goals_met, info.goals_total, info.goals_met >= info.goals_total, "")
		for title: String in info.outstanding.slice(0, 3):
			# The server words each open goal itself ("... (Tagesziel) offen").
			var open := UiKit.text_line("  " + title, 20)
			open.modulate = Color(1, 1, 1, 0.75)
			_tasks.add_child(open)
	for mission: HomeInfo.Mission in info.missions.slice(0, 4):
		_task(mission.title, mission.current, mission.target, mission.completed, "+%d" % mission.reward)
	if _tasks.get_child_count() == 0:
		_tasks.add_child(UiKit.text_line("Heute stehen keine Aufgaben an.", 22))
	if info.chests_waiting > 0:
		_chest.text = "%d Truhe%s wartet – öffne sie im Lern-Teil!" % [info.chests_waiting, "" if info.chests_waiting == 1 else "n"]
		_chest.add_theme_color_override("font_color", UiKit.GOLD)
	elif info.chest_earned_today:
		_chest.text = "Die Truhe von heute hast du schon."
	else:
		_chest.text = "Schaffe deine Lernziele, dann gibt es heute eine Truhe."


## One task line: title, progress and a bar; a finished one turns green.
func _task(title: String, current: int, target: int, done: bool, reward: String) -> void:
	var line := UiKit.text_line("%s %s  %d/%d  %s" % ["✔" if done else "○", title, mini(current, target), target, reward], 22)
	if done:
		line.add_theme_color_override("font_color", DONE)
	_tasks.add_child(line)
	var bar := ProgressBar.new()
	bar.show_percentage = false
	bar.custom_minimum_size = Vector2(0, 10)
	bar.max_value = maxi(1, target)
	bar.value = mini(current, target)
	_tasks.add_child(bar)


func _show_choice() -> void:
	_map_card.text = "Karte\n%s  ▸" % Catalog.LEVELS[_map_index].title
	var chosen := _position()
	_words_card.text = "Wörter\n%s  ▸" % (_position_title(chosen) if not chosen.is_empty() else "wählen")
	_play.disabled = chosen.is_empty()


func _next_map() -> void:
	_map_index = (_map_index + 1) % Catalog.LEVELS.size()
	Settings.set_map(_map_index)
	_show_choice()


func _on_play() -> void:
	var chosen := _position()
	if not chosen.is_empty():
		play.emit(Catalog.LEVELS[_map_index], int(chosen.get("planPositionId", 0)))


## The position to play: the remembered one if the server still offers it, else the first.
func _position() -> Dictionary:
	for entry: Variant in _positions:
		if entry is Dictionary and int((entry as Dictionary).get("planPositionId", 0)) == plan_position_id:
			return entry as Dictionary
	return _positions[0] as Dictionary if not _positions.is_empty() and _positions[0] is Dictionary else {}


static func _position_title(entry: Dictionary) -> String:
	var subject: Variant = entry.get("subjectName")
	var title := str(entry.get("exerciseTitle", ""))
	return "%s · %s" % [subject, title] if subject is String else title


## The best score over all maps.
static func _best() -> int:
	var best := 0
	for level: LevelDef in Catalog.LEVELS:
		best = maxi(best, Records.best(level.title))
	return best
