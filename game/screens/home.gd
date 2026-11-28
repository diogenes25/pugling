class_name HomeScreen
extends Control
## The start screen: the hero in front of his castle, the big play button bottom right with the map and the words
## next to it, on top what the child has collected (streak, trophies, coins, gems, best score), on the left today's
## tasks and on the right the chests, the trophy path and the game streak. Everything about the day comes from the
## server ([method TowerServer.home], [method TowerServer.overview]); the screen only shows it, so a task is never
## ticked off by the game itself. The two chests are opened here, and the server books what they hold.

## "SPIELEN" was pressed: this map with these words.
signal play(level: LevelDef, plan_position_id: int)

## The words card was pressed.
signal pick_words

## The settings button was pressed.
signal open_settings

## The server did not answer; the text is ready for the screen. Main tries a fresh login once.
signal load_failed(error: String)

const HERO_LOOK: PackedScene = preload("res://heroes/knight_look.tscn")
const PANEL := Color(0.05, 0.07, 0.17, 0.74)
const DONE := Color(0.55, 0.95, 0.6)
const CARD := Color(0.22, 0.33, 0.62)

## The server; set before the screen enters the tree.
var server: TowerServer

## The words to play; 0 picks the first position the server offers.
var plan_position_id: int = 0

## What the child collected in the game, as last read from the server.
var game: GameProgress = GameProgress.new()

var _map_index: int = 0
var _positions: Array = []
var _busy: bool = false
var _stage: HomeStage = HomeStage.new()
var _streak: Chip = Chip.make(Icon.Kind.FLAME, "0")
var _trophies: Chip = Chip.make(Icon.Kind.TROPHY, "0")
var _coins: Chip = Chip.make(Icon.Kind.COIN, "0")
var _gems: Chip = Chip.make(Icon.Kind.GEM, "0")
var _record: Chip = Chip.make(Icon.Kind.STAR, "0")
var _tasks: VBoxContainer = VBoxContainer.new()
var _collect: VBoxContainer = VBoxContainer.new()
var _game_day: VBoxContainer = VBoxContainer.new()
var _map_card: Button = UiKit.card(Icon.Kind.MAP, CARD, 26)
var _words_card: Button = UiKit.card(Icon.Kind.BOOK, CARD, 24)
var _play: Button = UiKit.card(Icon.Kind.SWORDS, Color(0.95, 0.66, 0.08), 54)

## The line for progress and error messages.
var status: Label = UiKit.caption("")


func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_map_index = clampi(Settings.map(), 0, Catalog.LEVELS.size() - 1)
	add_child(_stage)
	_stage.show_hero(HERO_LOOK.instantiate() as HeroLook)
	_build_top()
	_build_left()
	_build_right()
	_build_bottom()
	_show_choice()
	_load()


func _build_top() -> void:
	var bar := HBoxContainer.new()
	_place(bar, 0, 0, 1, 0, 20, 14, -20, 78)
	bar.add_theme_constant_override("separation", 12)
	add_child(bar)
	var player := UiKit.caption(server.child_name)
	player.add_theme_font_size_override("font_size", 38)
	player.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	player.horizontal_alignment = HORIZONTAL_ALIGNMENT_LEFT
	bar.add_child(player)
	for chip: Chip in [_streak, _trophies, _coins, _gems, _record]:
		bar.add_child(chip)
	var settings := UiKit.icon_button(Icon.Kind.GEAR, UiKit.NIGHT, 60)
	settings.pressed.connect(open_settings.emit)
	bar.add_child(settings)


func _build_left() -> void:
	var panel := _panel("Tagesaufgaben")
	_place(panel, 0, 0, 0, 1, 20, 92, 420, -170)
	_tasks.add_theme_constant_override("separation", 6)
	(panel.get_child(0) as VBoxContainer).add_child(_tasks)
	add_child(panel)


func _build_right() -> void:
	var panel := _panel("Truhen & Trophäen")
	_place(panel, 1, 0, 1, 1, -400, 92, -20, -170)
	var box := panel.get_child(0) as VBoxContainer
	_collect.add_theme_constant_override("separation", 6)
	box.add_child(_collect)
	_game_day.add_theme_constant_override("separation", 4)
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
	_words_card.custom_minimum_size = Vector2(430, 110)
	_words_card.size_flags_vertical = Control.SIZE_SHRINK_END
	_words_card.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_words_card.pressed.connect(pick_words.emit)
	row.add_child(_words_card)
	_map_card.custom_minimum_size = Vector2(250, 110)
	_map_card.size_flags_vertical = Control.SIZE_SHRINK_END
	_map_card.pressed.connect(_next_map)
	row.add_child(_map_card)
	_play.custom_minimum_size = Vector2(340, 130)
	_play.text = "SPIELEN"
	_play.pressed.connect(_on_play)
	row.add_child(_play)
	UiKit.pulse(_play)


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
	box.border_color = Color(1, 1, 1, 0.14)
	box.set_border_width_all(2)
	box.set_corner_radius_all(20)
	box.set_content_margin_all(16)
	panel.add_theme_stylebox_override("panel", box)
	var column := VBoxContainer.new()
	column.add_theme_constant_override("separation", 8)
	panel.add_child(column)
	var title := UiKit.text_line(headline, 28)
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
	game = GameProgress.from_json(overview.data)
	_show_choice()
	@warning_ignore("redundant_await")
	var day := await server.home()
	if not is_inside_tree():
		return
	status.text = "" if not _positions.is_empty() else "Kein aktiver Plan mit genug Wörtern."
	var info := HomeInfo.new()
	if day.ok:
		info = HomeInfo.from_json(day.data.get("wallet", {}), day.data.get("today", {}), day.data.get("missions", []))
	_show_day(info)
	_show_collect(info)
	_show_game_day(overview.data)


func _show_game_day(overview: Dictionary) -> void:
	_clear(_game_day)
	var seconds_left := maxi(0, int(overview.get("limitSeconds", 0)) - int(overview.get("playedSecondsToday", 0)))
	@warning_ignore("integer_division")
	var minutes_left := seconds_left / 60
	_game_day.add_child(_line(Icon.Kind.CLOCK, "Spielzeit heute noch %d Min." % minutes_left))
	_game_day.add_child(_line(Icon.Kind.COIN, "Spiel-Münzen heute %d/%d" % [
		int(overview.get("coinsToday", 0)), int(overview.get("dailyCoinCap", 0))]))


func _show_day(info: HomeInfo) -> void:
	_streak.value = str(info.streak)
	_trophies.value = str(game.trophies)
	_coins.value = str(info.coins)
	_gems.value = str(info.gems)
	_record.value = str(_best())
	_trophies.visible = game.known
	_clear(_tasks)
	if info.goals_total > 0:
		_task("Lernziele heute", info.goals_met, info.goals_total, info.goals_met >= info.goals_total, 0)
		for title: String in info.outstanding.slice(0, 3):
			# The server words each open goal itself ("... (Tagesziel) offen").
			var open := UiKit.text_line("  " + title, 19)
			open.modulate = Color(1, 1, 1, 0.75)
			_tasks.add_child(open)
	for mission: HomeInfo.Mission in info.missions.slice(0, 5):
		_task(mission.title, mission.current, mission.target, mission.completed, mission.reward)
	if _tasks.get_child_count() == 0:
		_tasks.add_child(UiKit.text_line("Heute stehen keine Aufgaben an.", 22))


## The right panel: the daily chest, the game chest, the trophy path and the game streak.
func _show_collect(info: HomeInfo) -> void:
	_clear(_collect)
	if info.chests_waiting > 0:
		var open := UiKit.button("Truhe öffnen" if info.chests_waiting == 1 else "Truhe öffnen (%d)" % info.chests_waiting,
				Color(0.8, 0.5, 0.1), 24)
		open.custom_minimum_size = Vector2(0, 58)
		open.pressed.connect(_open_daily_chest)
		UiKit.pulse(open)
		_collect.add_child(open)
	elif info.chest_earned_today:
		_collect.add_child(_line(Icon.Kind.CHEST, "Die Truhe von heute hast du schon."))
	else:
		_collect.add_child(_line(Icon.Kind.CHEST, "Schaffe deine Lernziele, dann gibt es eine Truhe."))
	if not game.known:
		return
	if game.chest_ready:
		var open_game := UiKit.button("Spiel-Truhe öffnen", Color(0.2, 0.55, 0.8), 24)
		open_game.custom_minimum_size = Vector2(0, 58)
		open_game.pressed.connect(_open_game_chest)
		UiKit.pulse(open_game)
		_collect.add_child(open_game)
	elif game.chest_opened:
		_collect.add_child(_line(Icon.Kind.CHEST, "Spiel-Truhe für heute geöffnet."))
	else:
		_collect.add_child(_line(Icon.Kind.CHEST, "Spiel-Truhe  %d/%d Vokabeln" % [game.chest_words, game.chest_needed]))
		_collect.add_child(_bar(game.chest_words, game.chest_needed))
	var step := game.path.next(game.trophies)
	if step != null:
		_collect.add_child(_line(Icon.Kind.TROPHY, "noch %d bis +%d Gems" % [step.trophies - game.trophies, step.gems]))
		var path := _bar(0, 1)
		path.max_value = 1.0
		path.step = 0.001
		path.value = game.path.progress(game.trophies)
		_collect.add_child(path)
	elif not game.path.steps.is_empty():
		_collect.add_child(_line(Icon.Kind.TROPHY, "Trophäen-Pfad geschafft!"))
	_collect.add_child(_line(Icon.Kind.FLAME, "Spiel-Serie %d  ·  heute %d/%d Vokabeln" % [
		game.streak, mini(game.words_today, game.streak_words), game.streak_words]))


func _open_daily_chest() -> void:
	if _busy:
		return
	_busy = true
	@warning_ignore("redundant_await")
	var opened := await server.open_daily_chest()
	_busy = false
	if not is_inside_tree():
		return
	if opened.ok:
		_stage.cheer()
		Log.line("truhe geöffnet: +%d Münzen, +%d Gems" % [int(opened.data.get("coins", 0)), int(opened.data.get("gems", 0))])
	await _load()
	status.text = _chest_text(int(opened.data.get("coins", 0)), int(opened.data.get("gems", 0))) if opened.ok else opened.error


func _open_game_chest() -> void:
	if _busy:
		return
	_busy = true
	@warning_ignore("redundant_await")
	var opened := await server.open_game_chest()
	_busy = false
	if not is_inside_tree():
		return
	if opened.ok:
		_stage.cheer()
		Log.line("spiel-truhe geöffnet: +%d Gems" % int(opened.data.get("gemsAwarded", 0)))
	await _load()
	status.text = _chest_text(0, int(opened.data.get("gemsAwarded", 0))) if opened.ok else opened.error


## What a chest held, as one line.
static func _chest_text(coins: int, gems: int) -> String:
	var parts: Array[String] = []
	if coins > 0:
		parts.append("+%d Münzen" % coins)
	if gems > 0:
		parts.append("+%d Gem%s" % [gems, "" if gems == 1 else "s"])
	return "In der Truhe: %s!" % " und ".join(parts) if not parts.is_empty() else "Die Truhe war leer."


## One task line: a tick or a ring, the title with its progress, the reward in gems and a bar; a finished one is green.
func _task(title: String, current: int, target: int, done: bool, reward: int) -> void:
	var row := HBoxContainer.new()
	row.add_theme_constant_override("separation", 8)
	row.add_child(Icon.make(Icon.Kind.CHECK if done else Icon.Kind.RING, 24))
	var line := UiKit.text_line("%s  %d/%d" % [title, mini(current, target), target], 21)
	line.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	if done:
		line.add_theme_color_override("font_color", DONE)
	row.add_child(line)
	if reward > 0:
		var pays := UiKit.text_line("+%d" % reward, 21)
		pays.autowrap_mode = TextServer.AUTOWRAP_OFF
		row.add_child(pays)
		row.add_child(Icon.make(Icon.Kind.GEM, 22))
	_tasks.add_child(row)
	_tasks.add_child(_bar(current, target))


## A line of the right panel: a symbol and what it says.
static func _line(kind: Icon.Kind, text: String) -> HBoxContainer:
	var row := HBoxContainer.new()
	row.add_theme_constant_override("separation", 8)
	row.add_child(Icon.make(kind, 26))
	var label := UiKit.text_line(text, 20)
	label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row.add_child(label)
	return row


static func _bar(current: int, target: int) -> ProgressBar:
	var bar := ProgressBar.new()
	bar.show_percentage = false
	bar.custom_minimum_size = Vector2(0, 10)
	bar.max_value = maxi(1, target)
	bar.value = mini(current, target)
	return bar


static func _clear(box: Container) -> void:
	for child: Node in box.get_children():
		box.remove_child(child)
		child.queue_free()


func _show_choice() -> void:
	_map_card.text = "Karte\n%s  ›" % Catalog.LEVELS[_map_index].title
	var chosen := _position()
	_words_card.text = "Wörter\n%s  ›" % (_position_title(chosen) if not chosen.is_empty() else "wählen")
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
