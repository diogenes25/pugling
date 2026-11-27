class_name Settings
extends Object
## What the app remembers on the device between starts: where the server is, who plays, the PIN for the automatic
## login (father's decision of 2026-11-27: log in once, then play right away), the sound switch and the last choice
## of words and map. The file is encrypted with a key from the device id - no protection against someone who owns
## the device, but the PIN is not readable in a file browser or a backup.

const SECTION: String = "app"
const HOME_SERVER: String = "http://192.168.178.55:5200"
const EMULATOR_SERVER: String = "http://127.0.0.1:5280"

## The settings file; a test points this at its own file.
static var file: String = "user://settings.cfg"


## The server's address; the emulator (the only x86_64 target) starts on the throwaway API, a phone on the home server.
static func server() -> String:
	return str(_read("server", EMULATOR_SERVER if OS.has_feature("x86_64") else HOME_SERVER))


## The child's number.
static func child() -> int:
	return int(_read("child", 3))


## The stored PIN, "" when nobody is logged in on this device.
static func pin() -> String:
	return str(_read("pin", ""))


## Whether the game plays sound.
static func sound() -> bool:
	return bool(_read("sound", true))


## The plan position played last, 0 for none.
static func position() -> int:
	return int(_read("position", 0))


## The index of the map played last.
static func map() -> int:
	return int(_read("map", 0))


## Remembers a login that just worked.
static func save_login(address: String, child_number: int, secret: String) -> void:
	_write({"server": address, "child": child_number, "pin": secret})


## Forgets the PIN: the next start asks for it again.
static func forget_pin() -> void:
	_write({"pin": ""})


## Switches the sound on or off, now and for the next starts.
static func set_sound(on: bool) -> void:
	_write({"sound": on})
	apply_sound()


## Remembers the words to play.
static func set_position(plan_position_id: int) -> void:
	_write({"position": plan_position_id})


## Remembers the map to play.
static func set_map(index: int) -> void:
	_write({"map": index})


## Mutes or unmutes the game by the stored switch; call once at start.
static func apply_sound() -> void:
	AudioServer.set_bus_mute(0, not sound())


static func _key() -> String:
	var id := OS.get_unique_id()
	return "pugling-tower-" + (id if id != "" else "device")


static func _load() -> ConfigFile:
	var config := ConfigFile.new()
	config.load_encrypted_pass(file, _key())
	return config


static func _read(key: String, fallback: Variant) -> Variant:
	return _load().get_value(SECTION, key, fallback)


static func _write(values: Dictionary[String, Variant]) -> void:
	var config := _load()
	for key: String in values:
		config.set_value(SECTION, key, values[key])
	config.save_encrypted_pass(file, _key())
