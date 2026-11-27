class_name Towers
extends Node3D
## All towers of a level: places them on the map's tower spots and lets them shoot.


## A shot a tower just fired.
class Shot:
	extends RefCounted

	## The tower.
	var tower: Tower

	## The projectile on its way.
	var projectile: Projectile


## The towers, in the order of the map's tower spots.
var all: Array[Tower] = []


## Builds one tower per spot; the kinds take turns over the spots. [param next_target] hands out the meanings in the
## server's order (it returns a [TowerTarget] or null).
func setup(def: LevelDef, map: MapInfo, kinds: Array[TowerKind], next_target: Callable) -> void:
	for i: int in def.tower_spots.size():
		var tower := Tower.new()
		tower.kind = kinds[i % kinds.size()]
		tower.pos = def.tower_spots[i]
		tower.model = Models.scenery(tower.kind.models[i % tower.kind.models.size()]).instantiate() as Node3D
		tower.model.position = Geo.to3(tower.pos)
		tower.model.scale = Vector3.ONE * tower.kind.model_scale
		add_child(tower.model)
		add_child(Fx.ring(Geo.to3(tower.pos, 0.06), tower.kind.attack_range, Color(1, 1, 1, 0.13)))
		map.block(tower.pos, tower.kind.footprint)
		tower.target = next_target.call()
		all.append(tower)


## Lets every loaded tower with an enemy in range fire; returns the shots of this frame.
func shoot(dt: float, enemies: Enemies, projectiles: Projectiles) -> Array[Shot]:
	var shots: Array[Shot] = []
	for tower: Tower in all:
		if not tower.reload.tick(dt) or tower.ammo.count <= 0:
			continue
		var enemy := enemies.foremost(tower.pos, tower.kind.attack_range)
		if enemy == null:
			continue
		tower.reload.start(tower.kind.shot_interval)
		var shot := Shot.new()
		shot.tower = tower
		shot.projectile = projectiles.fire(tower.kind.strong_shot if tower.ammo.use() else tower.kind.shot,
				Projectile.Shooter.TOWER, Geo.to3(tower.pos, tower.kind.muzzle_height), enemy, tower.level.damage_bonus())
		shots.append(shot)
	return shots


## The nearest tower within reach that still asks for a word, or null.
func nearest_asking(from: Vector2, reach: float) -> Tower:
	var best: Tower = null
	var best_distance := INF
	for tower: Tower in all:
		var distance := tower.pos.distance_to(from)
		if tower.target != null and distance <= reach and distance < best_distance:
			best = tower
			best_distance = distance
	return best
