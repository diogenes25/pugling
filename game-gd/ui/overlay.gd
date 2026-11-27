class_name Overlay
extends Control
## Draws everything that has to stay readable on a phone flat over the 3D scene: the words on the books, the
## skeletons' health bars, the tower panels with meaning and ammo, the badges pointing to towers off screen, the HUD
## and the joystick. It only reads the game state; the level asks for a redraw every frame.

const FULL_AMMO_BAR: int = 20
const INK := Color(0.2, 0.12, 0.0)

## The level's clock, for pulsing.
var clock: float = 0.0

## The tower the give button currently points at, or null.
var chosen: Tower

var _camera: Camera3D
var _words: WordBooks
var _enemies: Enemies
var _towers: Towers
var _hero: Hero
var _tally: RunTally
var _stick: TouchStick
var _boxes: Dictionary[String, StyleBoxFlat] = {}
var _font: Font = ThemeDB.fallback_font


## Hands the overlay what it draws.
func setup(camera: Camera3D, words: WordBooks, enemies: Enemies, towers: Towers, hero: Hero, tally: RunTally, stick: TouchStick) -> void:
	_camera = camera
	_words = words
	_enemies = enemies
	_towers = towers
	_hero = hero
	_tally = tally
	_stick = stick


func _draw() -> void:
	if _camera == null:
		return
	var screen := get_viewport_rect().size

	for book: WordBook in _words.all:
		var at := _project(book.node.position + Vector3(0, 1.1, 0), screen)
		if book.node.visible and _inside(at, screen, 0.0):
			_pill(at, book.data.word, 24, Color(UiKit.GOLD, 0.95), INK)

	for enemy: Enemy in _enemies.all:
		var at := _project(enemy.node.position + Vector3(0, 2.1, 0), screen)
		if enemy.dying or not _inside(at, screen, 0.0):
			continue
		var bar := Rect2(at - Vector2(26, 5), Vector2(52, 10))
		_box(bar.grow(2), Color(0, 0, 0, 0.6), Color.TRANSPARENT, 0, 5)
		var healthy := enemy.hp * 2 > enemy.kind.hp
		_box(Rect2(bar.position, Vector2(bar.size.x * enemy.hp / enemy.kind.hp, bar.size.y)),
				Color(0.4, 0.9, 0.3) if healthy else Color(0.95, 0.35, 0.25), Color.TRANSPARENT, 0, 4)

	for tower: Tower in _towers.all:
		_draw_tower(tower, screen)

	if _hero.carried != null:
		var head := _project(_hero.position + Vector3(0, 3.4, 0), screen)
		if _inside(head, screen, 0.0):
			_pill(head, _hero.carried.word, 26, UiKit.GOLD, INK, Color.WHITE, 3)

	_draw_hud()

	# Joystick: a hint where the left thumb goes, and the stick while it is held.
	var center := _stick.center if _stick.held() else Vector2(150, screen.y - 150)
	draw_circle(center, 95, Color(1, 1, 1, 0.12))
	draw_arc(center, 95, 0, TAU, 48, Color(1, 1, 1, 0.35), 3, true)
	draw_circle(center + _stick.vector * TouchStick.REACH, 40, Color(1, 1, 1, 0.45))


## Hits towards the goal and the castle's lives.
func _draw_hud() -> void:
	var hud := Rect2(16, 14, 330, 64)
	_box(hud, UiKit.NIGHT, Color(1, 1, 1, 0.25), 2, 18)
	_text(Vector2(hud.position.x + 82, hud.get_center().y), "%d/%d" % [_tally.correct, _tally.goal], 30, UiKit.GOLD)
	_text(Vector2(hud.position.x + 82, hud.get_center().y + 25), "Treffer", 14, Color.WHITE, 0)
	for i: int in Balance.CASTLE_LIVES:
		_box(Rect2(hud.position.x + 160 + i * 16, hud.get_center().y - 9, 12, 18),
				Color(0.95, 0.3, 0.35) if i < _tally.lives else Color(1, 1, 1, 0.15), Color.TRANSPARENT, 0, 4)
	_text(Vector2(hud.position.x + 238, hud.get_center().y + 25), "Burg", 14, Color.WHITE, 0)


func _draw_tower(tower: Tower, screen: Vector2) -> void:
	var ammo := tower.ammo
	var asking := tower.target != null
	var low := asking and ammo.is_low()
	var is_chosen := tower == chosen
	var border := UiKit.GOLD if is_chosen else Color(1.0, 0.3, 0.25) if low else Color(1, 1, 1, 0.3)
	var label := "fertig"
	if asking:
		label = "★ " + tower.target.prompt if ammo.is_strong() else tower.target.prompt
	var at := _project(Geo.to3(tower.pos, 4.6), screen)
	if _inside(at, screen, 40.0):
		var width := maxf(150, _font.get_string_size(label, HORIZONTAL_ALIGNMENT_LEFT, -1, 26).x + 34)
		var panel := Rect2(at - Vector2(width / 2, 40), Vector2(width, 66))
		var pulse := 1 + 0.06 * sin(clock * 8) if is_chosen else 1.0
		panel = panel.grow((pulse - 1) * 40)
		var fill_color := UiKit.NIGHT
		if not asking:
			fill_color = Color(0.2, 0.2, 0.2, 0.7)
		elif ammo.is_strong():
			fill_color = Color(0.45, 0.18, 0.05, 0.88)
		_box(panel, fill_color, border, 5 if is_chosen else 2, 16)
		_text(Vector2(panel.get_center().x, panel.position.y + 22), label, 26, Color.WHITE if asking else Color(1, 1, 1, 0.6))
		if not asking:
			return
		var bar := Rect2(panel.position.x + 14, panel.end.y - 22, panel.size.x - 64, 12)
		_box(bar, Color(0, 0, 0, 0.55), Color.TRANSPARENT, 0, 6)
		var fill := clampf(float(ammo.count) / FULL_AMMO_BAR, 0.0, 1.0)
		if fill > 0.0:
			_box(Rect2(bar.position, Vector2(maxf(12, bar.size.x * fill), bar.size.y)), _ammo_color(ammo.count), Color.TRANSPARENT, 0, 6)
		_text(Vector2(panel.end.x - 26, bar.get_center().y), str(ammo.count), 20, _ammo_color(ammo.count), 4)
		return

	if not asking:
		return
	# Off screen: a badge on the edge points the way, red when the tower is running dry.
	var mid := screen / 2
	var dir := (at - mid).normalized()
	var inset := Vector2(70, 70)
	var stretch := minf((mid.x - inset.x) / maxf(0.001, absf(dir.x)), (mid.y - inset.y) / maxf(0.001, absf(dir.y)))
	var edge := mid + dir * stretch
	var tip := edge + dir * 46
	var side := dir.orthogonal() * 16
	draw_colored_polygon(PackedVector2Array([tip, edge + dir * 24 + side, edge + dir * 24 - side]), border)
	_box(Rect2(edge - Vector2(34, 34), Vector2(68, 68)), Color(0.85, 0.2, 0.2, 0.92) if low else UiKit.NIGHT, border, 3, 34)
	_text(edge, str(ammo.count), 26, _ammo_color(ammo.count))
	_pill(edge + Vector2(0, -54 if dir.y > 0 else 54), label, 18, UiKit.NIGHT, Color.WHITE)


static func _ammo_color(ammo: int) -> Color:
	if ammo <= Balance.WRONG_PENALTY:
		return Color(1.0, 0.35, 0.3)
	return Color(1.0, 0.8, 0.3) if ammo <= 6 else Color(0.45, 0.95, 0.45)


## Where a world point lands on the screen; a point behind the camera is mirrored so its direction stays right.
func _project(world: Vector3, screen: Vector2) -> Vector2:
	var at := _camera.unproject_position(world)
	return screen - at if _camera.is_position_behind(world) else at


static func _inside(at: Vector2, screen: Vector2, margin: float) -> bool:
	return at.x >= margin and at.y >= margin and at.x <= screen.x - margin and at.y <= screen.y - margin


## A rounded panel; the style boxes are reused per look, because this runs every frame.
func _box(rect: Rect2, fill: Color, border: Color, border_width: int, radius: int) -> void:
	var key := "%s|%s|%d|%d" % [fill.to_html(), border.to_html(), border_width, radius]
	if not _boxes.has(key):
		var box := StyleBoxFlat.new()
		box.bg_color = fill
		box.border_color = border
		box.anti_aliasing = true
		box.set_corner_radius_all(radius)
		box.set_border_width_all(border_width)
		_boxes[key] = box
	_boxes[key].draw(get_canvas_item(), rect)


## Text centred on a point, with a dark outline so it reads on any ground.
func _text(center: Vector2, content: String, font_size: int, color: Color, outline: int = 6) -> void:
	var width := _font.get_string_size(content, HORIZONTAL_ALIGNMENT_LEFT, -1, font_size).x
	var at := center + Vector2(-width / 2, (_font.get_ascent(font_size) - _font.get_descent(font_size)) / 2)
	if outline > 0:
		draw_string_outline(_font, at, content, HORIZONTAL_ALIGNMENT_LEFT, -1, font_size, outline, Color(0, 0, 0, 0.85 * color.a))
	draw_string(_font, at, content, HORIZONTAL_ALIGNMENT_LEFT, -1, font_size, color)


## A word on a rounded badge, centred on a point.
func _pill(center: Vector2, content: String, font_size: int, fill: Color, text_color: Color,
		border: Color = Color.TRANSPARENT, border_width: int = 0) -> void:
	var width := _font.get_string_size(content, HORIZONTAL_ALIGNMENT_LEFT, -1, font_size).x + 26
	var rect := Rect2(center - Vector2(width / 2, font_size * 0.75 + 6), Vector2(width, font_size * 1.5 + 12))
	_box(rect, fill, border, border_width, int(rect.size.y / 2))
	_text(center, content, font_size, text_color, 0)
