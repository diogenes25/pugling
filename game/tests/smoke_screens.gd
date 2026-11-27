extends Node
## Smoke run of the whole screen flow on a [FakeApi]: presses what a player presses, including the Android back
## button on every screen, and logs what each screen says. It starts on a device that remembers nothing (settings
## with the login), then checks the start screen, the choices that are remembered, a run with its result, a restart
## that logs in by itself, a session that expired, and a stored PIN that no longer fits. Exit code 0 only when every
## expectation held and the back button on the login closed the app.
## Run: godot --headless --path . res://tests/smoke_screens.tscn
## With a window and "-- --shots=<folder>" it also saves a picture of every screen.

const MAIN: PackedScene = preload("res://Main.tscn")
const WATCHDOG_SECONDS: float = 120.0
const SETTINGS_FILE: String = "user://settings-smoke.cfg"

var _main: Control
var _fake := FakeApi.new()
var _failed: int = 0


func _ready() -> void:
	get_tree().create_timer(WATCHDOG_SECONDS, true, false, true).timeout.connect(_give_up)
	Settings.file = SETTINGS_FILE
	DirAccess.remove_absolute(ProjectSettings.globalize_path(SETTINGS_FILE))
	add_child(_fake)
	_fake.failing_ends = 1
	await _restart()

	# A device that remembers nothing opens the settings with the login.
	_expect("login", ["Vokabel-Türme", "[Emulator]", "[Heimnetz]", "[Anmelden]", "[⌫]", "[Ton: an]"])
	_check(UiDriver.find_button(_main, "Abmelden") == null and UiDriver.find_button(_main, "Zurück") == null,
			"ohne Anmeldung kein Abmelden und kein Zurück")
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
	_check(Settings.pin() == "", "eine falsche PIN wird nicht gespeichert")
	_press("C")
	_type("4711")
	_press("Anmelden")
	await _frames(8)

	_expect("startseite", ["Testkind", "[SPIELEN]", "Tagesaufgaben", "○ Lernziele heute  1/4", "  Unit 1 · Wörter Part A (Tagesziel) offen",
		"○ 20 Vokabeln richtig  8/20  +25", "✔ Fertig-Mission  5/5  +10", "1 Truhe wartet – öffne sie im Lern-Teil!",
		"Spielzeit heute noch 28 Min.", "Spiel-Münzen heute 0/10", "Serie 3   ·   Münzen 120   ·   Gems 7   ·   Rekord",
		"[Wörter\nEnglisch · Unit 1 · Wörter  ▸]", "[Karte\nLichtung  ▸]", "[Einstellungen]"])
	_check(Settings.pin() == "4711" and Settings.server() == "http://127.0.0.1:5280" and Settings.child() == 3,
			"die Anmeldung ist auf dem Gerät gespeichert")
	var texts := " | ".join(UiDriver.texts(_main))
	_check(texts.find("○ 20 Vokabeln") < texts.find("✔ Fertig-Mission"), "offene Aufgaben stehen vor erledigten")
	await UiDriver.shot(get_tree(), "2-startseite")

	# The map card steps through the maps and the choice is remembered.
	_press("Karte")
	_expect("karte 2", ["[Karte\nFlussfurt  ▸]"])
	_check(Settings.map() == 1, "die Karte ist gemerkt")
	_press("Karte")
	_press("Karte")
	_expect("karte wieder 1", ["[Karte\nLichtung  ▸]"])

	# The words card opens the list; back leads to the start screen, a pick is remembered.
	_press("Wörter")
	await _frames(6)
	_expect("wortwahl", ["Hallo Testkind! Welche Wörter?", "Spielzeit heute noch 28 Min.  ·  Münzen heute 0/10 🪙",
		"[Englisch · Unit 1 · Wörter (12)]", "[ · Ohne Fach (12)]", "[Zurück]"])
	await UiDriver.shot(get_tree(), "3-wortwahl")
	await _back("wortwahl")
	_expect("zurück von wortwahl", ["[SPIELEN]", "[Wörter\nEnglisch · Unit 1 · Wörter  ▸]"])
	_press("Wörter")
	await _frames(6)
	_press("Ohne Fach")
	await _frames(8)
	_expect("wörter gewählt", ["[SPIELEN]", "[Wörter\nOhne Fach  ▸]"])
	_check(Settings.position() == 2, "die Wörter sind gemerkt")
	# A long title wraps inside the card instead of being cut off (seen on the phone on 2026-11-27).
	_press("Wörter")
	await _frames(6)
	_press("Französisch")
	await _frames(8)
	_expect("langer titel", ["[Wörter\nFranzösisch · Unité 5 · Vocabulaire Volet 2 – Texte A und B  ▸]"])
	var card := UiDriver.find_button(_main, "Wörter")
	var needed := card.get_theme_font("font").get_multiline_string_size(card.text, HORIZONTAL_ALIGNMENT_CENTER,
			card.size.x - 32, card.get_theme_font_size("font_size")).y
	_check(not card.clip_text and card.autowrap_mode != TextServer.AUTOWRAP_OFF and needed <= card.size.y,
			"der lange Titel passt umbrochen in die Wörter-Karte (braucht %d von %d Höhe)" % [needed, card.size.y])
	await UiDriver.shot(get_tree(), "2b-langer-titel")
	_press("Wörter")
	await _frames(6)
	_press("Ohne Fach")
	await _frames(8)

	# The settings from the start screen: the sound switch, and the way back.
	_press("Einstellungen")
	await _frames(4)
	_expect("einstellungen", ["Einstellungen", "[Anmelden]", "[Ton: an]", "[Abmelden]", "[Zurück]"])
	_press("Ton: an")
	_expect("ton aus", ["[Ton: aus]"])
	_check(AudioServer.is_bus_mute(0) and not Settings.sound(), "Ton aus schaltet stumm und ist gemerkt")
	_press("Ton: aus")
	_check(not AudioServer.is_bus_mute(0) and Settings.sound(), "Ton an schaltet wieder ein")
	await UiDriver.shot(get_tree(), "4-einstellungen")
	await _back("einstellungen")
	_expect("zurück von einstellungen", ["[SPIELEN]"])

	# One tap starts a run on the chosen map with the chosen words.
	_press("SPIELEN")
	await _play_until_result()
	# The first end call fails (failing_ends): the result is not known yet and can be sent again.
	_expect("ergebnis ungespeichert", ["Ergebnis noch nicht gespeichert.", "[Ergebnis nochmal senden]", "[Noch einmal]",
		"[Startseite]", "Server nicht erreichbar (keine Verbindung)."])
	await UiDriver.shot(get_tree(), "6-ergebnis-ungespeichert")
	_press("Ergebnis nochmal senden")
	await _frames(6)
	_expect("ergebnis", ["Spiel beendet.", "Welle 0  ·  0 Punkte", "Monster besiegt: 0  →  0 Punkte", "0 von 0 Vokabeln richtig",
		"Münzen gibt es ab 3 richtigen Vokabeln.", "[Noch einmal]", "[Startseite]"])
	_check(UiDriver.find_button(_main, "Ergebnis nochmal senden") == null, "kein Nochmal-senden nach gespeichertem Ergebnis")
	await UiDriver.shot(get_tree(), "7-ergebnis")
	_press("Noch einmal")
	await _play_until_result()
	_expect("noch einmal", ["Spiel beendet."])
	await _back("ergebnis")
	_expect("zurück von ergebnis", ["[SPIELEN]"])
	_press("SPIELEN")
	await _play_until_result()
	_press("Startseite")
	await _frames(8)
	_expect("startseite nach dem lauf", ["[SPIELEN]", "Tagesaufgaben"])

	# A restart logs in by itself and lands on the start screen with the remembered choices.
	var logins := _fake.logins
	await _restart()
	await _frames(8)
	_expect("neustart", ["Testkind", "[SPIELEN]", "[Wörter\nOhne Fach  ▸]"])
	_check(_fake.logins == logins + 1 and UiDriver.find_button(_main, "Anmelden") == null, "Neustart meldet selbst an, ohne Login-Bildschirm")

	# A session that ran out is renewed once with the stored PIN, without bothering the player.
	_fake.expired_overviews = 1
	logins = _fake.logins
	_press("Einstellungen")
	await _frames(4)
	_press("Zurück")
	await _frames(14)
	_expect("abgelaufene sitzung", ["[SPIELEN]", "Tagesaufgaben"])
	_check(_fake.logins == logins + 1, "abgelaufene Sitzung: genau eine neue Anmeldung")

	# A stored PIN that no longer fits opens the settings and says why.
	Settings.save_login(Settings.server(), 3, "9999")
	await _restart()
	await _frames(8)
	_expect("gespeicherte PIN passt nicht mehr", ["Vokabel-Türme", "[Anmelden]", "PIN falsch oder Anmeldung abgelaufen."])
	(_main.find_child("Pin", true, false) as LineEdit).grab_focus()
	_type("4711")
	_press("Anmelden")
	await _frames(8)
	_expect("wieder angemeldet", ["[SPIELEN]"])

	# "Abmelden" forgets the PIN; the next start would ask again.
	_press("Einstellungen")
	await _frames(4)
	_press("Abmelden")
	await _frames(4)
	_expect("abmelden", ["Vokabel-Türme", "[Anmelden]"])
	_check(Settings.pin() == "" and UiDriver.find_button(_main, "Zurück") == null, "Abmelden vergisst die PIN")

	Log.line("bild fertig, %d Abweichungen; Zurück auf dem Login beendet die App" % _failed)
	if _failed > 0:
		get_tree().quit(1)
		return
	# The last expectation is the exit itself: main quits with code 0, otherwise the watchdog ends the run with 1.
	await get_tree().create_timer(0.5).timeout
	_main.notification(NOTIFICATION_WM_GO_BACK_REQUEST)


## Starts the app anew on the same server and the same device memory.
func _restart() -> void:
	if _main != null:
		_main.queue_free()
		await _frames(2)
	_main = MAIN.instantiate()
	_main.set("server", _fake)
	add_child(_main)
	await _frames(2)


## From the pressed map to the result screen. In a level it opens the pause menu with the back button and stops.
func _play_until_result() -> void:
	await _frames(6)
	var level := _main.find_child("Level", true, false)
	if level != null:
		await get_tree().create_timer(1.0).timeout
		await UiDriver.shot(get_tree(), "5-level")
		await _back("level")
		_expect("pause", ["Pause", "[Weiter]", "[Aufhören]"])
		await UiDriver.shot(get_tree(), "5-pause")
		_press("Aufhören")
		await get_tree().create_timer(0.8).timeout
	await _frames(6)


func _back(from: String) -> void:
	# After a deviation the app may be on another screen than expected - on the login, back would close the app with
	# exit code 0 and the run would look green. So a run that already deviated stops here.
	if _failed > 0:
		Log.line("bild ABBRUCH vor der Zurück-Taste auf %s: %d Abweichungen" % [from, _failed])
		get_tree().quit(1)
		await get_tree().create_timer(5.0).timeout
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
