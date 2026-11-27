extends Node
## Smoke run of the login against a real (throwaway) API: types the PIN on the on-screen keypad like a player.
## Run: godot --headless --path . res://tests/smoke_login.tscn -- --server=http://127.0.0.1:5281 --pin=4711
## Exit code 0 when the child is logged in.

const MAIN: PackedScene = preload("res://Main.tscn")


func _ready() -> void:
	var main: Control = MAIN.instantiate()
	add_child(main)
	await get_tree().process_frame
	var server: LineEdit = main.find_child("Server", true, false)
	Log.line("login Server-Vorgabe %s, Kind-Nr. %s" % [server.text, (main.find_child("Child", true, false) as LineEdit).text])
	server.text = UiDriver.arg("server", server.text)
	# A wrong PIN first: the screen must say so and stay.
	for digit: String in "0000".split(""):
		UiDriver.press(main, digit)
	UiDriver.press(main, "Anmelden")
	await _settled(main)
	Log.line("login falsche PIN: %s" % _status(main))
	UiDriver.press(main, "C")
	for digit: String in UiDriver.arg("pin").split(""):
		UiDriver.press(main, digit)
	UiDriver.press(main, "Anmelden")
	await _settled(main)
	await UiDriver.shot(get_tree(), "login")
	var ok := Api.child_name != ""
	Log.line("login %s" % ("ok als %s" % Api.child_name if ok else "gescheitert: %s" % _status(main)))
	get_tree().quit(0 if ok else 1)


func _status(main: Node) -> String:
	var page := main.get_child(main.get_child_count() - 1) as Page
	return page.status.text if page != null else ""


## Waits until the pending call has answered: the status line no longer says "Anmelden …".
func _settled(main: Node) -> void:
	for i: int in 600:
		await get_tree().process_frame
		if _status(main) != "Anmelden …" or Api.child_name != "":
			return
