extends Control
## The screen flow: the app logs in by itself with what the device remembers and opens the start screen; from there
## a run starts with one tap. Settings (server, child, PIN, sound) and the choice of words are their own screens.
## Each screen is its own scene and reports through signals; this node swaps them and decides where the Android back
## button leads.

const LOGIN: PackedScene = preload("res://screens/login.tscn")
const HOME: PackedScene = preload("res://screens/home.tscn")
const POSITIONS: PackedScene = preload("res://screens/positions.tscn")
const RESULT: PackedScene = preload("res://screens/result.tscn")
const SKINS: PackedScene = preload("res://screens/skins.tscn")
const LEVEL: PackedScene = preload("res://level/level.tscn")
const PAGE: PackedScene = preload("res://screens/page.tscn")
const BACK_DEBOUNCE_MSEC: int = 400

## The server every screen talks to; a test sets a [FakeApi] before the node enters the tree.
var server: TowerServer

var _screen: Node
var _last_back: int = 0
## What the Android back button does on the current screen.
var _back: Callable = _nothing
var _last_position: int = 0
var _last_level: LevelDef = Catalog.LEVELS[0]
var _summary: Level.Summary = Level.Summary.new()
## A session that expired is renewed once with the stored PIN before the settings open.
var _renewed: bool = false
## The trophies the start screen last read from the server: the hero's level of the next run.
var _trophies: int = 0
## A run is being started on the server.
var _starting: bool = false


func _ready() -> void:
	if server == null:
		server = Api
	Settings.apply_sound()
	_last_position = Settings.position()
	if Settings.pin() == "":
		_show_settings("")
	else:
		_auto_login()


func _notification(what: int) -> void:
	# project.godot keeps the app open on "back" (quit_on_go_back=false); each screen decides where back leads.
	# One press arrives more than once on Android; without the debounce it would skip a screen.
	if what != NOTIFICATION_WM_GO_BACK_REQUEST or Time.get_ticks_msec() - _last_back < BACK_DEBOUNCE_MSEC:
		return
	_last_back = Time.get_ticks_msec()
	_back.call()


func _nothing() -> void:
	pass


func _quit() -> void:
	get_tree().quit()


func _replace(screen: Node) -> void:
	if _screen != null:
		_screen.queue_free()
	_screen = screen
	add_child(screen)


func _say(text: String) -> void:
	var page := _screen as Page
	if page != null:
		page.status.text = text
	var home := _screen as HomeScreen
	if home != null:
		home.status.text = text


## Logs in with the remembered server, child and PIN. A wrong PIN or an unreachable server opens the settings with
## the reason; the PIN stays stored, so a server that is back needs only "Anmelden".
func _auto_login() -> void:
	var splash: Page = PAGE.instantiate()
	_replace(splash)
	splash.set_title("Vokabel-Türme")
	splash.add_status()
	splash.status.text = "Anmelden …"
	_back = _quit
	@warning_ignore("redundant_await")
	var result := await server.login(Settings.server(), Settings.child(), Settings.pin())
	if result.ok:
		_show_home()
	else:
		_show_settings(result.error)


func _show_settings(notice: String) -> void:
	var settings: LoginScreen = LOGIN.instantiate()
	settings.server = server
	settings.notice = notice
	settings.logged_in_already = server.child_name != "" and notice == ""
	settings.logged_in.connect(_on_logged_in)
	settings.back.connect(_show_home)
	settings.logged_out.connect(_on_logged_out)
	_replace(settings)
	_back = _show_home if settings.logged_in_already else _quit


func _on_logged_in() -> void:
	_renewed = false
	_show_home()


func _on_logged_out() -> void:
	server.child_name = ""
	_show_settings("")


func _show_home() -> void:
	var home: HomeScreen = HOME.instantiate()
	home.server = server
	home.plan_position_id = _last_position
	home.play.connect(_start)
	home.pick_words.connect(_show_positions)
	home.open_settings.connect(_show_settings.bind(""))
	home.open_hero.connect(_show_skins)
	home.trophies_known.connect(func(total: int) -> void: _trophies = total)
	home.load_failed.connect(_on_home_failed)
	_replace(home)
	_back = _quit


## The start screen got no answer. A login token runs out after a while: one fresh login with the stored PIN, and
## only if that fails too the settings open with the reason.
func _on_home_failed(error: String) -> void:
	if _renewed or Settings.pin() == "":
		_show_settings(error)
		return
	_renewed = true
	_auto_login()


func _show_skins() -> void:
	var skins: SkinsScreen = SKINS.instantiate()
	skins.server = server
	skins.back.connect(_show_home)
	_replace(skins)
	_back = _show_home


func _show_positions() -> void:
	var positions: PositionsScreen = POSITIONS.instantiate()
	positions.server = server
	positions.chosen.connect(_on_words_chosen)
	positions.back.connect(_show_home)
	_replace(positions)
	_back = _show_home


func _on_words_chosen(plan_position_id: int) -> void:
	_last_position = plan_position_id
	Settings.set_position(plan_position_id)
	_show_home()


func _start(map: LevelDef, plan_position_id: int) -> void:
	# A second tap while the server builds the run would start a second run and leave the first one open.
	if _starting:
		return
	_starting = true
	_say("Level wird gebaut …")
	@warning_ignore("redundant_await")
	var started := await server.start_run(plan_position_id)
	_starting = false
	if not started.ok:
		_say(started.error)
		return
	_last_position = plan_position_id
	_last_level = map
	Settings.set_position(plan_position_id)
	_play(TowerRun.from_json(started.data), map)


func _play(run: TowerRun, map: LevelDef) -> void:
	var level: Level = LEVEL.instantiate()
	level.setup(server, run, map, HeroLevel.of(_trophies), Catalog.skin(Settings.skin()))
	level.finished.connect(_show_result)
	_replace(level)
	_back = level.toggle_pause


func _show_result(run_id: int, correct: int, total: int, reason: LevelEnd.Reason) -> void:
	_back = _show_home
	# The level is still on screen when it reports: its score goes along to the result (and to a resend).
	var level := _screen as Level
	if level != null:
		_summary = level.summary()
	@warning_ignore("redundant_await")
	var end := await server.end_run(run_id, _summary.wave)
	var result: ResultScreen = RESULT.instantiate()
	result.correct = correct
	result.total = total
	result.reason = reason
	result.score = _summary.score
	result.wave = _summary.wave
	result.top_tower_level = _summary.top_tower_level
	result.record = _summary.record
	result.best = _summary.best
	result.end = end
	result.trophies_before = _trophies
	if end.ok and end.data.has("trophies"):
		_trophies = int(end.data["trophies"])
	result.resend.connect(_show_result.bind(run_id, correct, total, reason))
	result.again.connect(_again)
	result.home.connect(_show_home)
	_replace(result)


func _again() -> void:
	_start(_last_level, _last_position)
