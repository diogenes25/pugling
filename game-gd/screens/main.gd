extends Control
## The screen flow: login, pick the words, pick a map, play a level, see the result. Each screen is its own scene and
## reports through signals; this node swaps them and decides where the Android back button leads.

const LOGIN: PackedScene = preload("res://screens/login.tscn")
const BACK_DEBOUNCE_MSEC: int = 400

## The server every screen talks to; a test sets a [FakeApi] before the node enters the tree.
var server: TowerServer

var _screen: Node
var _last_back: int = 0
## What the Android back button does on the current screen.
var _back: Callable = _nothing


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


func _replace(screen: Node) -> void:
	if _screen != null:
		_screen.queue_free()
	_screen = screen
	add_child(screen)


func _show_login() -> void:
	var login: Page = LOGIN.instantiate()
	login.set("server", server)
	login.connect("logged_in", _show_positions)
	_replace(login)
	_back = get_tree().quit


func _show_positions() -> void:
	Log.line("angemeldet als %s" % server.child_name)
