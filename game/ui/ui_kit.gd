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
	var shades: Dictionary[String, float] = {"normal": 0.0, "hover": 0.1, "pressed": -0.2, "focus": 0.0}
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
