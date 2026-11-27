class_name LoginScreen
extends Page
## The settings: where the server is and who plays, with the login. A login that works is remembered on the device
## ([Settings]), so the app starts on the start screen from then on; "Abmelden" forgets the PIN again.
## Login with the child's number and PIN. Digits come only from the on-screen keypad, as in the web login: the phone's
## keyboard would pop up over the form and hide the keypad. A hardware keyboard still types into the focused field.

## The child is logged in on [member server].
signal logged_in

## "Zurück" was pressed (only offered while a child is logged in).
signal back

## "Abmelden" was pressed and the PIN is forgotten.
signal logged_out

## Servers to pick with one tap: the emulator reaches the throwaway API via "adb reverse", the phone the home server
## in the WLAN.
const SERVERS: Dictionary[String, String] = {
	"Emulator": Settings.EMULATOR_SERVER,
	"Heimnetz": Settings.HOME_SERVER,
}
const KEYS: Array[String] = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "⌫", "0", "C"]
const DIMMED := Color(1, 1, 1, 0.55)

## The server the login goes to; set before the screen enters the tree.
var server: TowerServer

## A child is logged in already: the screen was opened from the start screen and offers the way back.
var logged_in_already: bool = false

## A message to show right away (why the automatic login failed).
var notice: String = ""

## The field the keypad writes into.
var _target: LineEdit
var _sound: Button

@onready var _form: VBoxContainer = %Form
@onready var _server: LineEdit = %Server
@onready var _presets: HBoxContainer = %Presets
@onready var _child: LineEdit = %Child
@onready var _pin: LineEdit = %Pin
@onready var _submit: Button = %Submit
@onready var _keypad: GridContainer = %Keypad


func _ready() -> void:
	set_title("Einstellungen" if logged_in_already else "Vokabel-Türme")
	_server.text = Settings.server()
	_child.text = str(Settings.child())
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
	status.text = notice
	_sound = add_action("", _toggle_sound)
	_show_sound()
	if logged_in_already:
		add_action("Abmelden", _log_out)
		add_action("Zurück", back.emit)
	_select(_pin)


func _toggle_sound() -> void:
	Settings.set_sound(not Settings.sound())
	_show_sound()


func _show_sound() -> void:
	_sound.text = "Ton: an" if Settings.sound() else "Ton: aus"


func _log_out() -> void:
	Settings.forget_pin()
	logged_out.emit()


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
		Settings.save_login(_server.text, _child.text.to_int(), _pin.text)
		logged_in.emit()
	else:
		status.text = result.error
