class_name LevelDef
extends Resource
## One map as data (lichtung.tres, flussfurt.tres, burgberg.tres), in ground coordinates: x to the right, y towards
## the viewer (world z). The hero may walk inside (0,0)..size; monster paths may start outside it. A new map is a new
## resource plus an entry in [constant Catalog.LEVELS].

## Display name.
@export var title: String = ""

## One line for the map chooser.
@export var description: String = ""

## Walkable area.
@export var size: Vector2 = Vector2.ZERO

## Roads the monsters follow; each ends at the castle.
@export var paths: Array[PackedVector2Array] = []

## Where towers stand; each run fills them in order.
@export var tower_spots: PackedVector2Array = []

## Where the hero starts.
@export var player_start: Vector2 = Vector2.ZERO

## A river as a polyline (empty: none); it blocks the hero except at the bridges.
@export var river: PackedVector2Array = []

## Points on the river where a bridge lets the hero (and a path) cross.
@export var bridges: PackedVector2Array = []

## Spots for large mountains inside the map; they block the hero.
@export var mountains: PackedVector2Array = []

## Seed for the scattered scenery, so a map looks the same every time.
@export var map_seed: int = 0

## Monster speed in units per second at the start.
@export var monster_speed: float = 1.0

## Seconds between two monsters at the start.
@export var spawn_interval: float = 5.0


## The castle the monsters walk to.
func castle() -> Vector2:
	var first := paths[0]
	return first[first.size() - 1]
