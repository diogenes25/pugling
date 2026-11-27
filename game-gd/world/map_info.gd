class_name MapInfo
extends RefCounted
## The ground truth a built map hands to the level: where the hero may walk. The towers add their footprints here.

## Half the width of the band of water that blocks the hero.
const RIVER_HALF_WIDTH: float = 2.1

## How close to a bridge point the river can be crossed.
const BRIDGE_REACH: float = 2.3

## The map this was built from.
var def: LevelDef

## Circles the hero cannot walk into (trees, rocks, towers, the castle): x and y are the centre, z the radius.
var obstacles: Array[Vector3] = []


func _init(map: LevelDef) -> void:
	def = map


## Adds a circle the hero cannot walk into.
func block(at: Vector2, radius: float) -> void:
	obstacles.append(Vector3(at.x, at.y, radius))


## Whether a point lies in the river and not on a bridge.
func is_water(p: Vector2) -> bool:
	if def.river.size() < 2 or Geo.dist_to_polyline(p, def.river) >= RIVER_HALF_WIDTH:
		return false
	for bridge: Vector2 in def.bridges:
		if bridge.distance_to(p) < BRIDGE_REACH:
			return false
	return true


## Whether a body of the given radius can stand at [param p]: inside the map, not in the water, clear of obstacles.
func walkable(p: Vector2, radius: float) -> bool:
	if p.x < radius or p.y < radius or p.x > def.size.x - radius or p.y > def.size.y - radius or is_water(p):
		return false
	for obstacle: Vector3 in obstacles:
		if Vector2(obstacle.x, obstacle.y).distance_to(p) < obstacle.z + radius:
			return false
	return true
