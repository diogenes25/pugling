class_name LoginScreen
extends Page
## Login with the child's number and PIN. Digits come only from the on-screen keypad, as in the web login: the phone's
## keyboard would pop up over the form and hide the keypad. A hardware keyboard still types into the focused field.

## The child is logged in on [member server].
signal logged_in

## Servers to pick with one tap: the emulator reaches the throwaway API via "adb reverse", the phone the home server
## in the WLAN.
const SERVERS: Dictionary[String, String] = {
	"Emulator": "http://127.0.0.1:5280",
	"Heimnetz": "http://192.168.178.55:5200",
}
const KEYS: Array[String] = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "⌫", "0", "C"]
const DIMMED := Color(1, 1, 1, 0.55)

## The server the login goes to; set before the screen enters the tree.
var server: TowerServer

## The field the keypad writes into.
var _target: LineEdit

@onready var _form: VBoxContainer = %Form
@onready var _server: LineEdit = %Server
@onready var _presets: HBoxContainer = %Presets
@onready var _child: LineEdit = %Child
@onready var _pin: LineEdit = %Pin
@onready var _submit: Button = %Submit
@onready var _keypad: GridContainer = %Keypad


func _ready() -> void:
	set_title("Vokabel-Türme")
	# The emulator is the only x86_64 target; a real phone (arm64) starts on the home server.
	_server.text = SERVERS["Emulator" if OS.has_feature("x86_64") else "Heimnetz"]
	for label: String in SERVERS:
		var preset := UiKit.action(label)
		preset.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		preset.pressed.connect(_use_server.bind(SERVERS[label]))
		_presets.add_child(preset)
	for key: String in KEYS:
		var button := UiKit.action(key)
		button.custom_minimum_size = Vector2(110, 80)
		button.pressed.connect(_type.bind(key))
		_keypad.add_child(button)
	_child.focus_entered.connect(_select.bind(_child))
	_pin.focus_entered.connect(_select.bind(_pin))
	_submit.pressed.connect(_log_in)
	_form.add_child(status)
	_select(_pin)


func _use_server(url: String) -> void:
	_server.text = url


func _select(field: LineEdit) -> void:
	_target = field
	# The field the keypad writes into is the bright one.
	_child.modulate = Color.WHITE if field == _child else DIMMED
	_pin.modulate = Color.WHITE if field == _pin else DIMMED


func _type(key: String) -> void:
	match key:
		"⌫":
			_target.text = _target.text.left(-1)
		"C":
			_target.text = ""
		_:
			_target.text += key


func _log_in() -> void:
	if not _child.text.is_valid_int():
		status.text = "Kind-Nr. muss eine Zahl sein."
		return
	status.text = "Anmelden …"
	@warning_ignore("redundant_await")
	var result := await server.login(_server.text, _child.text.to_int(), _pin.text)
	if not is_inside_tree():
		return
	if result.ok:
		logged_in.emit()
	else:
		status.text = result.error
