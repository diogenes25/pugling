extends Control
## The screen flow: login, pick the words, pick a map, play a level, see the result. Each screen is its own scene and
## reports through signals; this node swaps them and decides where the Android back button leads.

const LOGIN: PackedScene = preload("res://screens/login.tscn")
const POSITIONS: PackedScene = preload("res://screens/positions.tscn")
const MAPS: PackedScene = preload("res://screens/maps.tscn")
const RESULT: PackedScene = preload("res://screens/result.tscn")
const BACK_DEBOUNCE_MSEC: int = 400

## The server every screen talks to; a test sets a [FakeApi] before the node enters the tree.
var server: TowerServer

var _screen: Node
var _last_back: int = 0
## What the Android back button does on the current screen.
var _back: Callable = _nothing
var _last_position: int = 0
var _last_level: LevelDef = Catalog.LEVELS[0]


func _ready() -> void:
	if server == null:
		server = Api
	_show_login()


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


func _show_login() -> void:
	var login: LoginScreen = LOGIN.instantiate()
	login.server = server
	login.logged_in.connect(_show_positions)
	_replace(login)
	_back = _quit


func _show_positions() -> void:
	var positions: PositionsScreen = POSITIONS.instantiate()
	positions.server = server
	positions.chosen.connect(_show_maps)
	positions.logout.connect(_show_login)
	positions.quit_game.connect(_quit)
	_replace(positions)
	_back = _show_login


func _show_maps(plan_position_id: int) -> void:
	var maps: MapsScreen = MAPS.instantiate()
	maps.chosen.connect(_start.bind(plan_position_id))
	maps.other_words.connect(_show_positions)
	_replace(maps)
	_back = _show_positions


## Called with the map first: the map chooser emits it, the position is bound.
func _start(map: LevelDef, plan_position_id: int) -> void:
	_say("Level wird gebaut …")
	@warning_ignore("redundant_await")
	var started := await server.start_run(plan_position_id)
	if not started.ok:
		_say(started.error)
		return
	_last_position = plan_position_id
	_last_level = map
	_play(TowerRun.from_json(started.data), map)


func _play(run: TowerRun, _map: LevelDef) -> void:
	# The level scene follows in phase 4; until then a started run goes straight to its result.
	_show_result(run.run_id, 0, 0, LevelEnd.Reason.GAVE_UP)


func _show_result(run_id: int, correct: int, total: int, reason: LevelEnd.Reason) -> void:
	_back = _show_positions
	@warning_ignore("redundant_await")
	var end := await server.end_run(run_id)
	var result: ResultScreen = RESULT.instantiate()
	result.correct = correct
	result.total = total
	result.reason = reason
	result.end = end
	result.resend.connect(_show_result.bind(run_id, correct, total, reason))
	result.again.connect(_again)
	result.other_map.connect(_other_map)
	result.other_words.connect(_show_positions)
	_replace(result)


func _again() -> void:
	_start(_last_level, _last_position)


func _other_map() -> void:
	_show_maps(_last_position)
