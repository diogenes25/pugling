class_name Enemies
extends Node3D
## All enemies of a level: spawns them by the [SpawnSchedule], walks them along the paths, and answers the towers'
## and the hero's question "whom do I shoot at".

const DEATH_SECONDS: float = 1.3
const WALK_CLIPS: Array[String] = ["Walking_A", "Walking_B", "Running_A"]
const DEATH_CLIPS: Array[String] = ["Death_A", "Death_B"]

## The enemies on the map, the dying ones included.
var all: Array[Enemy] = []

var _def: LevelDef
var _schedule: SpawnSchedule
var _path_lengths: PackedFloat32Array = []


## Binds the enemies to a map; call before the first frame.
func setup(def: LevelDef) -> void:
	_def = def
	_schedule = SpawnSchedule.new(def.spawn_interval, def.monster_speed, def.paths.size(), Catalog.MINION, Catalog.WARRIOR)
	for path: PackedVector2Array in def.paths:
		_path_lengths.append(Geo.length(path))


## Lets the schedule decide whether an enemy comes now; returns the new one, or null.
func spawn(dt: float, elapsed: float) -> Enemy:
	var next := _schedule.tick(dt, elapsed)
	if next == null:
		return null
	var enemy := Enemy.new()
	enemy.kind = next.kind
	enemy.path = next.path
	enemy.speed = next.speed
	enemy.hp = next.kind.hp
	enemy.node = Models.character(next.kind.model).instantiate() as Node3D
	enemy.node.scale = Vector3.ONE * next.kind.model_scale
	Models.keep_only(enemy.node, [next.kind.mesh_prefix])
	enemy.anim = Models.animator(enemy.node)
	add_child(enemy.node)
	Models.play(enemy.anim, true, WALK_CLIPS)
	all.append(enemy)
	_place(enemy)
	return enemy


## Walks every enemy; returns how many reached the castle this frame (they are gone afterwards).
func move(dt: float) -> int:
	var arrived := 0
	for i: int in range(all.size() - 1, -1, -1):
		var enemy := all[i]
		if enemy.dying:
			enemy.dying_time -= dt
			if enemy.dying_time > 0.0:
				continue
			enemy.node.queue_free()
			all.remove_at(i)
			continue
		enemy.distance += enemy.speed * dt
		if enemy.distance < _path_lengths[enemy.path]:
			_place(enemy)
			continue
		enemy.node.queue_free()
		all.remove_at(i)
		arrived += 1
	return arrived


## Marks an enemy as defeated: it plays its death clip and is removed a moment later.
static func defeat(enemy: Enemy) -> void:
	enemy.dying = true
	enemy.dying_time = DEATH_SECONDS
	Models.play(enemy.anim, false, DEATH_CLIPS)


## Where an enemy is on its path, and its direction of travel.
func path_point(enemy: Enemy) -> Geo.PathPoint:
	return Geo.point_at(_def.paths[enemy.path], enemy.distance)


## The living enemy in reach that is closest to the castle (what a tower shoots at), or null.
func foremost(from: Vector2, reach: float) -> Enemy:
	var best: Enemy = null
	for enemy: Enemy in all:
		if not enemy.dying and enemy.ground().distance_to(from) <= reach and (best == null or enemy.remaining < best.remaining):
			best = enemy
	return best


## The living enemy in reach that is closest to a point (what the hero shoots at), or null.
func nearest(from: Vector2, reach: float) -> Enemy:
	var best: Enemy = null
	var best_distance := INF
	for enemy: Enemy in all:
		var distance := enemy.ground().distance_to(from)
		if not enemy.dying and distance <= reach and distance < best_distance:
			best = enemy
			best_distance = distance
	return best


func _place(enemy: Enemy) -> void:
	var point := path_point(enemy)
	enemy.node.position = Geo.to3(point.pos, 0.05)
	enemy.node.rotation = Vector3(0, atan2(point.dir.x, point.dir.y), 0)
	enemy.remaining = _path_lengths[enemy.path] - enemy.distance
