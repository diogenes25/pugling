class_name Chip
extends PanelContainer
## A dark pill with a symbol and a value (the wallet line of the start screen): the symbol says what is counted, so
## no word is needed. A value that changes makes the chip hop once.

var _icon: Icon
var _label: Label = Label.new()


## A chip for [param kind] showing [param value].
static func make(kind: Icon.Kind, shown: String, font_size: int = 28) -> Chip:
	var chip := Chip.new()
	chip._build(kind, shown, font_size)
	return chip


## The value the chip shows.
var value: String:
	get:
		return _label.text
	set(text):
		if _label.text != "" and text != _label.text and is_inside_tree():
			_hop()
		_label.text = text


func _build(kind: Icon.Kind, text: String, font_size: int) -> void:
	var box := StyleBoxFlat.new()
	box.bg_color = Color(0.05, 0.07, 0.16, 0.72)
	box.border_color = Color(1, 1, 1, 0.16)
	box.set_border_width_all(2)
	box.set_corner_radius_all(22)
	box.content_margin_left = 10
	box.content_margin_right = 16
	box.content_margin_top = 4
	box.content_margin_bottom = 4
	add_theme_stylebox_override("panel", box)
	size_flags_vertical = Control.SIZE_SHRINK_CENTER
	var row := HBoxContainer.new()
	row.add_theme_constant_override("separation", 8)
	add_child(row)
	_icon = Icon.make(kind, font_size + 8)
	row.add_child(_icon)
	_label.text = text
	_label.add_theme_font_size_override("font_size", font_size)
	_label.add_theme_color_override("font_outline_color", Color.BLACK)
	_label.add_theme_constant_override("outline_size", 6)
	row.add_child(_label)


func _hop() -> void:
	pivot_offset = size / 2.0
	var tween := create_tween()
	tween.tween_property(self, "scale", Vector2(1.25, 1.25), 0.12).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	tween.tween_property(self, "scale", Vector2.ONE, 0.2)
