class_name Icon
extends Control
## A small drawn symbol (coin, gem, trophy ...), so the screens need no words for what a number counts and no
## picture files. Everything is drawn from a few shapes in a square of [member custom_minimum_size].

## What the icon shows.
enum Kind {
	COIN,
	GEM,
	TROPHY,
	FLAME,
	CHEST,
	CLOCK,
	GEAR,
	BOOK,
	MAP,
	STAR,
	SWORDS,
	TOWER,
	CHECK,
	RING,
}

## The German word for each kind: what a screen reader - and the smoke runs - read in place of the picture.
const CAPTIONS: Dictionary[Kind, String] = {
	Kind.COIN: "Münzen", Kind.GEM: "Gems", Kind.TROPHY: "Trophäen", Kind.FLAME: "Serie", Kind.CHEST: "Truhe",
	Kind.CLOCK: "Spielzeit", Kind.GEAR: "Einstellungen", Kind.BOOK: "Wörter", Kind.MAP: "Karte", Kind.STAR: "Rekord",
	Kind.SWORDS: "Spielen", Kind.TOWER: "Turm", Kind.CHECK: "erledigt", Kind.RING: "offen",
}

const OUTLINE := Color(0.1, 0.07, 0.05)

## The symbol.
var kind: Kind = Kind.COIN:
	set(value):
		kind = value
		queue_redraw()

## The word the picture stands for.
var caption: String:
	get:
		return CAPTIONS[kind]


## An icon of [param edge] pixels.
static func make(what: Kind, edge: float) -> Icon:
	var icon := Icon.new()
	icon.kind = what
	icon.custom_minimum_size = Vector2(edge, edge)
	icon.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	icon.mouse_filter = Control.MOUSE_FILTER_IGNORE
	return icon


func _draw() -> void:
	var edge := minf(size.x, size.y)
	draw_set_transform((size - Vector2(edge, edge)) / 2.0, 0.0, Vector2(edge, edge))
	match kind:
		Kind.COIN:
			_disc(Vector2(0.5, 0.5), 0.46, OUTLINE)
			_disc(Vector2(0.5, 0.5), 0.4, Color(1.0, 0.78, 0.15))
			_disc(Vector2(0.5, 0.5), 0.28, Color(1.0, 0.9, 0.45))
			_shape([Vector2(0.44, 0.3), Vector2(0.56, 0.3), Vector2(0.56, 0.7), Vector2(0.44, 0.7)], Color(0.85, 0.55, 0.05))
		Kind.GEM:
			_shape([Vector2(0.5, 0.04), Vector2(0.94, 0.4), Vector2(0.5, 0.97), Vector2(0.06, 0.4)], OUTLINE)
			_shape([Vector2(0.5, 0.1), Vector2(0.86, 0.4), Vector2(0.5, 0.88), Vector2(0.14, 0.4)], Color(0.3, 0.75, 1.0))
			_shape([Vector2(0.5, 0.1), Vector2(0.86, 0.4), Vector2(0.5, 0.4)], Color(0.6, 0.9, 1.0))
			_shape([Vector2(0.14, 0.4), Vector2(0.5, 0.4), Vector2(0.5, 0.88)], Color(0.15, 0.55, 0.9))
		Kind.TROPHY:
			_shape([Vector2(0.2, 0.1), Vector2(0.8, 0.1), Vector2(0.72, 0.5), Vector2(0.56, 0.62), Vector2(0.56, 0.76),
				Vector2(0.72, 0.8), Vector2(0.72, 0.92), Vector2(0.28, 0.92), Vector2(0.28, 0.8), Vector2(0.44, 0.76),
				Vector2(0.44, 0.62), Vector2(0.28, 0.5)], Color(1.0, 0.76, 0.12))
			_shape([Vector2(0.3, 0.16), Vector2(0.44, 0.16), Vector2(0.44, 0.5), Vector2(0.36, 0.44)], Color(1.0, 0.92, 0.55))
			_arc(Vector2(0.2, 0.28), 0.14, PI * 0.5, PI * 1.5, Color(1.0, 0.76, 0.12))
			_arc(Vector2(0.8, 0.28), 0.14, -PI * 0.5, PI * 0.5, Color(1.0, 0.76, 0.12))
		Kind.FLAME:
			_shape([Vector2(0.5, 0.03), Vector2(0.78, 0.4), Vector2(0.86, 0.66), Vector2(0.7, 0.9), Vector2(0.5, 0.97),
				Vector2(0.3, 0.9), Vector2(0.14, 0.66), Vector2(0.3, 0.42), Vector2(0.36, 0.56)], Color(1.0, 0.42, 0.08))
			_shape([Vector2(0.5, 0.42), Vector2(0.66, 0.68), Vector2(0.6, 0.88), Vector2(0.5, 0.93), Vector2(0.4, 0.88),
				Vector2(0.34, 0.7)], Color(1.0, 0.85, 0.25))
		Kind.CHEST:
			_shape([Vector2(0.06, 0.4), Vector2(0.16, 0.18), Vector2(0.84, 0.18), Vector2(0.94, 0.4), Vector2(0.94, 0.9),
				Vector2(0.06, 0.9)], OUTLINE)
			_shape([Vector2(0.12, 0.42), Vector2(0.2, 0.24), Vector2(0.8, 0.24), Vector2(0.88, 0.42)], Color(0.72, 0.42, 0.16))
			_shape([Vector2(0.12, 0.48), Vector2(0.88, 0.48), Vector2(0.88, 0.84), Vector2(0.12, 0.84)], Color(0.58, 0.32, 0.12))
			_shape([Vector2(0.12, 0.42), Vector2(0.88, 0.42), Vector2(0.88, 0.5), Vector2(0.12, 0.5)], Color(1.0, 0.8, 0.2))
			_shape([Vector2(0.43, 0.4), Vector2(0.57, 0.4), Vector2(0.57, 0.64), Vector2(0.43, 0.64)], Color(1.0, 0.86, 0.3))
		Kind.CLOCK:
			_disc(Vector2(0.5, 0.52), 0.44, OUTLINE)
			_disc(Vector2(0.5, 0.52), 0.37, Color(0.95, 0.95, 0.98))
			draw_line(Vector2(0.5, 0.52), Vector2(0.5, 0.26), OUTLINE, 0.07)
			draw_line(Vector2(0.5, 0.52), Vector2(0.68, 0.6), OUTLINE, 0.07)
		Kind.GEAR:
			for i: int in 8:
				var out := Vector2.from_angle(TAU * i / 8.0)
				_shape([Vector2(0.5, 0.5) + out * 0.46 + out.orthogonal() * 0.1, Vector2(0.5, 0.5) + out * 0.46 - out.orthogonal() * 0.1,
					Vector2(0.5, 0.5) - out.orthogonal() * 0.13, Vector2(0.5, 0.5) + out.orthogonal() * 0.13], Color(0.85, 0.87, 0.92))
			_disc(Vector2(0.5, 0.5), 0.33, Color(0.85, 0.87, 0.92))
			_disc(Vector2(0.5, 0.5), 0.14, Color(0.2, 0.24, 0.36))
		Kind.BOOK:
			_shape([Vector2(0.08, 0.2), Vector2(0.5, 0.28), Vector2(0.92, 0.2), Vector2(0.92, 0.82), Vector2(0.5, 0.9),
				Vector2(0.08, 0.82)], OUTLINE)
			_shape([Vector2(0.13, 0.26), Vector2(0.47, 0.33), Vector2(0.47, 0.83), Vector2(0.13, 0.77)], Color(0.98, 0.95, 0.85))
			_shape([Vector2(0.53, 0.33), Vector2(0.87, 0.26), Vector2(0.87, 0.77), Vector2(0.53, 0.83)], Color(0.98, 0.95, 0.85))
			for row: int in 3:
				draw_line(Vector2(0.19, 0.42 + row * 0.12), Vector2(0.41, 0.46 + row * 0.12), Color(0.5, 0.45, 0.4), 0.035)
				draw_line(Vector2(0.59, 0.46 + row * 0.12), Vector2(0.81, 0.42 + row * 0.12), Color(0.5, 0.45, 0.4), 0.035)
		Kind.MAP:
			_shape([Vector2(0.06, 0.2), Vector2(0.36, 0.1), Vector2(0.64, 0.2), Vector2(0.94, 0.1), Vector2(0.94, 0.8),
				Vector2(0.64, 0.9), Vector2(0.36, 0.8), Vector2(0.06, 0.9)], Color(0.45, 0.78, 0.4))
			_shape([Vector2(0.36, 0.1), Vector2(0.64, 0.2), Vector2(0.64, 0.9), Vector2(0.36, 0.8)], Color(0.6, 0.86, 0.5))
			draw_polyline(PackedVector2Array([Vector2(0.14, 0.72), Vector2(0.34, 0.5), Vector2(0.56, 0.62), Vector2(0.8, 0.34)]),
					Color(0.85, 0.7, 0.4), 0.07)
			_disc(Vector2(0.8, 0.34), 0.08, Color(0.9, 0.2, 0.2))
		Kind.STAR:
			var points: Array[Vector2] = []
			for i: int in 10:
				points.append(Vector2(0.5, 0.52) + Vector2.from_angle(-PI / 2.0 + TAU * i / 10.0) * (0.47 if i % 2 == 0 else 0.2))
			_shape(points, Color(1.0, 0.85, 0.2))
		Kind.SWORDS:
			draw_line(Vector2(0.14, 0.86), Vector2(0.82, 0.14), Color(0.92, 0.94, 1.0), 0.1)
			draw_line(Vector2(0.86, 0.86), Vector2(0.18, 0.14), Color(0.92, 0.94, 1.0), 0.1)
			draw_line(Vector2(0.1, 0.66), Vector2(0.34, 0.9), Color(0.75, 0.5, 0.15), 0.09)
			draw_line(Vector2(0.9, 0.66), Vector2(0.66, 0.9), Color(0.75, 0.5, 0.15), 0.09)
		Kind.TOWER:
			_shape([Vector2(0.22, 0.95), Vector2(0.28, 0.36), Vector2(0.2, 0.36), Vector2(0.2, 0.1), Vector2(0.34, 0.1),
				Vector2(0.34, 0.2), Vector2(0.43, 0.2), Vector2(0.43, 0.1), Vector2(0.57, 0.1), Vector2(0.57, 0.2),
				Vector2(0.66, 0.2), Vector2(0.66, 0.1), Vector2(0.8, 0.1), Vector2(0.8, 0.36), Vector2(0.72, 0.36),
				Vector2(0.78, 0.95)], Color(0.8, 0.82, 0.9))
			_shape([Vector2(0.43, 0.95), Vector2(0.43, 0.72), Vector2(0.5, 0.64), Vector2(0.57, 0.72), Vector2(0.57, 0.95)], OUTLINE)
		Kind.CHECK:
			_disc(Vector2(0.5, 0.5), 0.46, Color(0.3, 0.75, 0.38))
			draw_polyline(PackedVector2Array([Vector2(0.26, 0.52), Vector2(0.44, 0.7), Vector2(0.76, 0.32)]), Color.WHITE, 0.1)
		Kind.RING:
			draw_arc(Vector2(0.5, 0.5), 0.36, 0.0, TAU, 32, Color(1, 1, 1, 0.7), 0.08, true)


func _disc(centre: Vector2, radius: float, color: Color) -> void:
	draw_circle(centre, radius, color)


func _shape(points: Array[Vector2], color: Color) -> void:
	draw_colored_polygon(PackedVector2Array(points), color)


func _arc(centre: Vector2, radius: float, from: float, to: float, color: Color) -> void:
	draw_arc(centre, radius, from, to, 12, color, 0.07, true)
