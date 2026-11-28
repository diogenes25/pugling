class_name UiKit
extends Object
## The game's own controls, so every screen and the level look alike.

## The colour of everything that carries a word.
const GOLD := Color(1.0, 0.82, 0.25)

## Panels over the 3D scene.
const NIGHT := Color(0.08, 0.1, 0.2, 0.82)

## Buttons that move the game forward.
const GO := Color(0.18, 0.62, 0.32)


## A plain, wide menu button; connect its [signal BaseButton.pressed].
static func action(text: String) -> Button:
	var plain := Button.new()
	plain.text = text
	plain.custom_minimum_size = Vector2(0, 72)
	plain.add_theme_font_size_override("font_size", 30)
	return plain


## A wrapping line of text for the menus.
static func text_line(content: String, font_size: int) -> Label:
	var label := Label.new()
	label.text = content
	label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	label.add_theme_font_size_override("font_size", font_size)
	return label


## A rounded, coloured button with a drop shadow, big enough for a thumb. It never takes the focus, so a keyboard's
## arrow keys keep moving the hero.
static func button(text_on_it: String, color: Color, font_size: int = 30) -> Button:
	var styled := Button.new()
	styled.text = text_on_it
	styled.focus_mode = Control.FOCUS_NONE
	styled.custom_minimum_size = Vector2(260, 76)
	styled.add_theme_font_size_override("font_size", font_size)
	var shades: Dictionary[String, float] = {"normal": 0.0, "hover": 0.1, "pressed": -0.2, "focus": 0.0, "disabled": -0.45}
	for state: String in shades:
		var shade := shades[state]
		var box := StyleBoxFlat.new()
		box.bg_color = color.lightened(shade) if shade >= 0.0 else color.darkened(-shade)
		box.border_color = color.darkened(0.45)
		box.shadow_color = Color(0, 0, 0, 0.35)
		box.shadow_size = 6
		box.shadow_offset = Vector2(0, 4)
		box.anti_aliasing = true
		box.set_corner_radius_all(24)
		box.set_border_width_all(4)
		box.set_content_margin_all(12)
		styled.add_theme_stylebox_override(state, box)
	styled.add_theme_color_override("font_color", Color.WHITE)
	styled.add_theme_color_override("font_outline_color", Color(0, 0, 0, 0.6))
	styled.add_theme_constant_override("outline_size", 6)
	springy(styled)
	return styled


## A small outlined caption.
static func caption(content: String) -> Label:
	var label := Label.new()
	label.text = content
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label.add_theme_font_size_override("font_size", 24)
	label.add_theme_color_override("font_outline_color", Color.BLACK)
	label.add_theme_constant_override("outline_size", 8)
	return label


## A [method button] with a symbol on its left: the cards of the start screen and its play button.
static func card(kind: Icon.Kind, color: Color, font_size: int) -> Button:
	var styled := button("", color, font_size)
	var edge := font_size + 22.0
	for state: String in ["normal", "hover", "pressed", "focus", "disabled"]:
		var box := styled.get_theme_stylebox(state) as StyleBoxFlat
		if box != null and styled.has_theme_stylebox_override(state):
			box.content_margin_left = edge + 26.0
	var icon := Icon.make(kind, edge)
	icon.anchor_top = 0.5
	icon.anchor_bottom = 0.5
	icon.offset_left = 16.0
	icon.offset_right = 16.0 + edge
	icon.offset_top = -edge / 2.0
	icon.offset_bottom = edge / 2.0
	styled.add_child(icon)
	return styled


## A square [method button] that shows only a symbol. Its tooltip carries the symbol's word, which is also what the
## smoke runs press it by.
static func icon_button(kind: Icon.Kind, color: Color, edge: float) -> Button:
	var styled := button("", color, 20)
	styled.custom_minimum_size = Vector2(edge, edge)
	styled.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	var icon := Icon.make(kind, edge * 0.62)
	icon.set_anchors_preset(Control.PRESET_CENTER)
	icon.offset_left = -edge * 0.31
	icon.offset_right = edge * 0.31
	icon.offset_top = -edge * 0.31
	icon.offset_bottom = edge * 0.31
	styled.add_child(icon)
	styled.tooltip_text = icon.caption
	return styled


## Lets a control breathe: it grows and shrinks a little, forever. For the one thing on a screen that wants a tap.
static func pulse(control: Control) -> void:
	control.resized.connect(func() -> void: control.pivot_offset = control.size / 2.0)
	control.tree_entered.connect(func() -> void:
		control.pivot_offset = control.size / 2.0
		var tween := control.create_tween().set_loops()
		tween.tween_property(control, "scale", Vector2(1.04, 1.04), 0.7).set_trans(Tween.TRANS_SINE)
		tween.tween_property(control, "scale", Vector2.ONE, 0.7).set_trans(Tween.TRANS_SINE))


## Makes a button give way under the thumb: it shrinks while held and springs back when let go.
static func springy(styled: Button) -> void:
	styled.button_down.connect(func() -> void:
		styled.pivot_offset = styled.size / 2.0
		styled.create_tween().tween_property(styled, "scale", Vector2(0.93, 0.93), 0.06))
	styled.button_up.connect(func() -> void:
		styled.create_tween().tween_property(styled, "scale", Vector2.ONE, 0.18).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT))
