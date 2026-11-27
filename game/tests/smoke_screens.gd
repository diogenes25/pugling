extends Node
## Smoke run of the whole screen flow on a [FakeApi]: presses what a player presses, including the Android back
## button on every screen, and logs what each screen says. Exit code 0 only when every expectation held and the back
## button on the login closed the app.
## Run: godot --headless --path . res://tests/smoke_screens.tscn
## With a window and "-- --shots=<folder>" it also saves a picture of every screen.

const MAIN: PackedScene = preload("res://Main.tscn")
const WATCHDOG_SECONDS: float = 120.0

var _main: Control
var _fake := FakeApi.new()
var _failed: int = 0


func _ready() -> void:
	get_tree().create_timer(WATCHDOG_SECONDS, true, false, true).timeout.connect(_give_up)
	add_child(_fake)
	_fake.failing_ends = 1
	_main = MAIN.instantiate()
	_main.set("server", _fake)
	add_child(_main)
	await _frames(2)

	_expect("login", ["Vokabel-Türme", "[Emulator]", "[Heimnetz]", "[Anmelden]", "[⌫]"])
	_expect_field("Child", "3")
	_expect_field("Server", "http://127.0.0.1:5280" if OS.has_feature("x86_64") else "http://192.168.178.55:5200")
	_check((_main.find_child("Pin", true, false) as LineEdit).virtual_keyboard_enabled == false, "PIN-Feld ohne Bildschirmtastatur")
	_check((_main.find_child("Child", true, false) as LineEdit).virtual_keyboard_enabled == false, "Kind-Feld ohne Bildschirmtastatur")
	_press("Heimnetz")
	_expect_field("Server", "http://192.168.178.55:5200")
	_press("Emulator")
	_expect_field("Server", "http://127.0.0.1:5280")
	# The keypad writes into the chosen field: first the child number, then the PIN.
	(_main.find_child("Child", true, false) as LineEdit).grab_focus()
	_press("C")
	_press("7")
	_press("⌫")
	_press("3")
	_expect_field("Child", "3")
	(_main.find_child("Pin", true, false) as LineEdit).grab_focus()
	_type("0000")
	await UiDriver.shot(get_tree(), "1-login")
	_press("Anmelden")
	await _frames(4)
	_expect("login falsche PIN", ["PIN falsch oder Anmeldung abgelaufen."])
	_press("C")
	_type("4711")
	_press("Anmelden")
	await _frames(6)

	_expect("wortwahl", ["Hallo Testkind! Welche Wörter?", "Spielzeit heute noch 28 Min.  ·  Münzen heute 0/10 🪙",
		"[Englisch · Unit 1 · Wörter (12)]", "[ · Ohne Fach (12)]", "[Abmelden]", "[Spiel beenden]"])
	await UiDriver.shot(get_tree(), "2-wortwahl")
	await _back("wortwahl")
	_expect("zurück von wortwahl", ["Vokabel-Türme", "[Anmelden]"])
	(_main.find_child("Pin", true, false) as LineEdit).grab_focus()
	_type("4711")
	_press("Anmelden")
	await _frames(6)
	_press("Englisch · Unit 1")
	await _frames(2)

	_expect("kartenwahl", ["Welche Karte?", "[Lichtung]", "Ein langer Weg, drei Türme nah beieinander – zum Eingewöhnen.",
		"[Flussfurt]", "Ein Fluss teilt die Karte. Nur über die Brücken kommst du auf die andere Seite.",
		"[Burgberg]", "Zwei Eingänge, ein Weg zur Burg. Sechs Türme wollen versorgt werden.", "[Andere Wörter]"])
	await UiDriver.shot(get_tree(), "3-kartenwahl")
	await _back("kartenwahl")
	_expect("zurück von kartenwahl", ["Hallo Testkind! Welche Wörter?"])
	_press("Englisch · Unit 1")
	await _frames(2)
	_press("Andere Wörter")
	await _frames(4)
	_expect("andere wörter", ["Hallo Testkind! Welche Wörter?"])
	_press("Englisch · Unit 1")
	await _frames(2)
	_press("Lichtung")
	await _play_until_result()

	# The first end call fails (failing_ends): the result is not known yet and can be sent again.
	_expect("ergebnis ungespeichert", ["Ergebnis noch nicht gespeichert.", "[Ergebnis nochmal senden]", "[Noch einmal]",
		"[Andere Karte]", "[Andere Wörter]", "Server nicht erreichbar (keine Verbindung)."])
	await UiDriver.shot(get_tree(), "5-ergebnis-ungespeichert")
	_press("Ergebnis nochmal senden")
	await _frames(6)
	_expect("ergebnis", ["Spiel beendet.", "Zuweisungen richtig.", "Keine Münzen diesmal.", "[Noch einmal]", "[Andere Karte]",
		"[Andere Wörter]"])
	_check(UiDriver.find_button(_main, "Ergebnis nochmal senden") == null, "kein Nochmal-senden nach gespeichertem Ergebnis")
	await UiDriver.shot(get_tree(), "6-ergebnis")
	_press("Andere Karte")
	await _frames(2)
	_expect("andere karte", ["Welche Karte?"])
	_press("Flussfurt")
	await _play_until_result()
	_expect("ergebnis 2", ["Spiel beendet."])
	_press("Noch einmal")
	await _play_until_result()
	_expect("noch einmal", ["Spiel beendet."])
	await _back("ergebnis")
	_expect("zurück von ergebnis", ["Hallo Testkind! Welche Wörter?"])
	_press("Abmelden")
	await _frames(2)
	_expect("abmelden", ["Vokabel-Türme", "[Anmelden]"])

	Log.line("bild fertig, %d Abweichungen; Zurück auf dem Login beendet die App" % _failed)
	if _failed > 0:
		get_tree().quit(1)
		return
	# The last expectation is the exit itself: main quits with code 0, otherwise the watchdog ends the run with 1.
	await get_tree().create_timer(0.5).timeout
	_main.notification(NOTIFICATION_WM_GO_BACK_REQUEST)


## From the pressed map to the result screen. In a level it opens the pause menu with the back button and stops.
func _play_until_result() -> void:
	await _frames(6)
	var level := _main.find_child("Level", true, false)
	if level != null:
		await get_tree().create_timer(1.0).timeout
		await UiDriver.shot(get_tree(), "4-level")
		await _back("level")
		_expect("pause", ["Pause", "[Weiter]", "[Aufhören]"])
		await UiDriver.shot(get_tree(), "4-pause")
		_press("Aufhören")
		await get_tree().create_timer(0.8).timeout
	await _frames(6)


func _back(from: String) -> void:
	# Longer than the debounce, or the press would be swallowed as a repeat.
	await get_tree().create_timer(0.5).timeout
	Log.line("bild Zurück-Taste auf %s" % from)
	_main.notification(NOTIFICATION_WM_GO_BACK_REQUEST)
	await _frames(6)


func _frames(count: int) -> void:
	for i: int in count:
		await get_tree().process_frame


func _press(text: String) -> void:
	_check(UiDriver.press(_main, text), "Knopf „%s“ ist da" % text)


func _type(digits: String) -> void:
	for digit: String in digits.split(""):
		_press(digit)


func _expect(screen: String, wanted: Array[String]) -> void:
	var shown := UiDriver.texts(_main)
	var all := " | ".join(shown)
	Log.line("bild %s: %s" % [screen, all])
	for text: String in wanted:
		_check(all.contains(text), "%s zeigt „%s“" % [screen, text])


func _expect_field(field: String, wanted: String) -> void:
	var edit := _main.find_child(field, true, false) as LineEdit
	_check(edit != null and edit.text == wanted, "Feld %s = %s" % [field, wanted])


func _check(condition: bool, what: String) -> void:
	if not condition:
		_failed += 1
		Log.line("bild ABWEICHUNG: %s" % what)


func _give_up() -> void:
	Log.line("bild ABBRUCH: Zeit abgelaufen")
	get_tree().quit(1)
