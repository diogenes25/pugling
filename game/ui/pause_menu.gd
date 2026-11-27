class_name PauseMenu
extends ColorRect
## The pause menu over the level: a dark veil with "Weiter" and "Aufhören". The level decides what the buttons do.

## "Weiter" was pressed.
signal resume

## "Aufhören" was pressed.
signal give_up

@onready var _menu: VBoxContainer = %Menu


func _ready() -> void:
	_add("Weiter", UiKit.GO, resume)
	_add("Aufhören", Color(0.7, 0.25, 0.2), give_up)


func _add(text: String, tint: Color, pressed: Signal) -> void:
	var button := UiKit.button(text, tint)
	button.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	button.pressed.connect(pressed.emit)
	_menu.add_child(button)
