class_name LevelControls
extends Control
## The level's controls on screen: the list of words to pick up, the give and lay-down buttons, the pause button
## with its menu, and the feedback line. It reports presses as signals; the rules stay in the level.

## The give button was pressed.
signal give_pressed

## The lay-down button was pressed.
signal drop_pressed

## The pause button or the menu's "Weiter" was pressed.
signal pause_pressed

## The pause menu's "Aufhören" was pressed.
signal give_up_pressed

## A word of the pick-up list was pressed.
signal book_picked(book: WordBook)

const PAUSE_MENU: PackedScene = preload("res://ui/pause_menu.tscn")

var _pick_panel: VBoxContainer = VBoxContainer.new()
var _give: Button = UiKit.button("", UiKit.GO, 34)
var _drop: Button = UiKit.button("", Color(0.3, 0.4, 0.6), 26)
var _pause: Button = UiKit.button("II", UiKit.NIGHT, 30)
var _feedback: Label = Label.new()
var _pause_menu: PauseMenu = PAUSE_MENU.instantiate()
# The pick list is rebuilt only when the books in reach change, so a finger on a button is not cut off.
var _pick_signature: String = ""


func _ready() -> void:
	_pick_panel.add_theme_constant_override("separation", 10)
	add_child(_pick_panel)

	_give.custom_minimum_size = Vector2(300, 110)
	_give.visible = false
	_give.pressed.connect(give_pressed.emit)
	add_child(_give)

	_drop.custom_minimum_size = Vector2(240, 70)
	_drop.visible = false
	_drop.pressed.connect(drop_pressed.emit)
	add_child(_drop)

	_pause.custom_minimum_size = Vector2(84, 72)
	_pause.pressed.connect(pause_pressed.emit)
	add_child(_pause)

	_feedback.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_feedback.modulate = Color.TRANSPARENT
	_feedback.add_theme_font_size_override("font_size", 36)
	_feedback.add_theme_color_override("font_outline_color", Color.BLACK)
	_feedback.add_theme_constant_override("outline_size", 10)
	add_child(_feedback)

	_pause_menu.resume.connect(pause_pressed.emit)
	_pause_menu.give_up.connect(give_up_pressed.emit)
	add_child(_pause_menu)


## Lists the books in reach, nearest first.
func show_nearby(near: Array[WordBook]) -> void:
	var ids := PackedStringArray()
	for book: WordBook in near:
		ids.append(book.data.token_id)
	var signature := "|".join(ids)
	if signature == _pick_signature:
		return
	_pick_signature = signature
	_clear_picks()
	if not near.is_empty():
		_pick_panel.add_child(UiKit.caption("Aufheben:"))
	for book: WordBook in near:
		var button := UiKit.button(book.data.word, Color(0.85, 0.6, 0.12))
		button.pressed.connect(book_picked.emit.bind(book))
		_pick_panel.add_child(button)


## Forces the pick list to be rebuilt on the next [method show_nearby].
func refresh_nearby() -> void:
	_pick_signature = ""


## Shows the give button for a word (or hides it with "") and the lay-down button for the carried word.
func show_carry(give_word: String, drop_word: String) -> void:
	_give.visible = give_word != ""
	if give_word != "":
		_give.text = "„%s“\ngeben" % give_word
	_drop.visible = drop_word != ""
	if drop_word != "":
		_drop.text = "„%s“ ablegen" % drop_word


## Hides the give button while the server grades.
func hide_give() -> void:
	_give.visible = false


## Shows or hides the pause menu.
func show_pause(paused: bool) -> void:
	_pause_menu.visible = paused


## Takes every control away when the level is over.
func close() -> void:
	_pause_menu.visible = false
	_pause.visible = false
	_give.visible = false
	_drop.visible = false
	_clear_picks()


## Shows a feedback line for a few seconds.
func say(text: String, color: Color) -> void:
	_feedback.text = text
	_feedback.add_theme_color_override("font_color", color)
	var tween := _feedback.create_tween()
	_feedback.modulate = Color.WHITE
	_feedback.pivot_offset = _feedback.size / 2
	_feedback.scale = Vector2.ONE * 1.25
	tween.tween_property(_feedback, "scale", Vector2.ONE, 0.25)
	tween.tween_interval(2.6)
	tween.tween_property(_feedback, "modulate", Color.TRANSPARENT, 0.5)


## Places the controls for the current screen size; call every frame.
func layout(screen: Vector2) -> void:
	_pick_panel.position = Vector2(screen.x - _pick_panel.size.x - 28, 110)
	_give.position = screen - _give.size - Vector2(28, 28)
	_drop.position = Vector2(screen.x - _drop.size.x - 28, (_give.position.y if _give.visible else screen.y) - _drop.size.y - 18)
	_pause.position = Vector2(screen.x - _pause.size.x - 20, 14)
	_feedback.size = Vector2(screen.x, 60)
	_feedback.position = Vector2(0, screen.y * 0.16)


func _clear_picks() -> void:
	for child: Node in _pick_panel.get_children():
		child.queue_free()
