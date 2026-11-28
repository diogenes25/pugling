class_name Page
extends ScrollContainer
## The frame every menu screen shares (page.tscn): a scrolling column with a title on top. Each screen is a scene
## that inherits it and fills the column.

## The line for progress and error messages; each screen adds it where it belongs.
var status: Label = UiKit.text_line("", 24)

@onready var _box: VBoxContainer = %Box
@onready var _title: Label = %Title


## Sets the screen's headline.
func set_title(text: String) -> void:
	_title.text = text


## Appends a menu button that calls [param on_pressed].
func add_action(text: String, on_pressed: Callable) -> Button:
	var button := UiKit.action(text)
	button.pressed.connect(on_pressed)
	_box.add_child(button)
	return button


## Appends a line of text.
func add_text(text: String, font_size: int) -> Label:
	var label := UiKit.text_line(text, font_size)
	_box.add_child(label)
	return label


## Appends any control to the column.
func add_node(node: Control) -> void:
	_box.add_child(node)


## Appends the status line at the current end of the column.
func add_status() -> void:
	_box.add_child(status)
