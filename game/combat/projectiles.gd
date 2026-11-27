class_name Projectiles
extends Node3D
## All projectiles in flight - the towers' and the hero's. They home in on their target: straight like an arrow, or
## in a bow like a catapult stone (see [member ProjectileKind.arc]).


## A projectile that reached its living target.
class Hit:
	extends RefCounted

	## The projectile.
	var projectile: Projectile

	## Where it struck.
	var at: Vector3

	## The hit took the target's last hit points.
	var killed: bool


var _flying: Array[Projectile] = []
var _sphere: SphereMesh = SphereMesh.new()
var _materials: Dictionary[ProjectileKind, StandardMaterial3D] = {}


func _init() -> void:
	_sphere.radius = 0.16
	_sphere.height = 0.32
	_sphere.radial_segments = 8
	_sphere.rings = 4


## Launches a projectile from a point at a target; [param damage_bonus] is added to the kind's damage.
func fire(kind: ProjectileKind, shooter: Projectile.Shooter, from: Vector3, target: Enemy, damage_bonus: int = 0) -> Projectile:
	var projectile := Projectile.new()
	projectile.kind = kind
	projectile.damage = kind.damage + damage_bonus
	projectile.from = shooter
	projectile.target = target
	projectile.track = from
	projectile.node = _body(kind)
	projectile.node.position = from
	projectile.node.scale = Vector3.ONE * kind.size
	add_child(projectile.node)
	if kind.burning:
		projectile.node.add_child(Fx.trail(kind.color))
	_flying.append(projectile)
	return projectile


## Flies every projectile; returns the ones that struck a living target this frame. An enemy killed by a hit is
## already marked as defeated when this returns.
func move(dt: float) -> Array[Hit]:
	var hits: Array[Hit] = []
	for i: int in range(_flying.size() - 1, -1, -1):
		var projectile := _flying[i]
		var target := projectile.target
		# The skeleton may be gone already (freed after its death); the projectile then just fizzles where it is.
		var alive := is_instance_valid(target.node) and not target.dying
		var dest := target.node.position + Vector3(0, 1, 0) if alive else projectile.track
		var to := dest - projectile.track
		var step := projectile.kind.speed * dt
		if to.length() > step:
			projectile.track += to.normalized() * step
			projectile.flown += step
			# The bow: highest halfway between launch and target, measured on what is still to fly.
			var share := projectile.flown / (projectile.flown + to.length())
			projectile.node.position = projectile.track + Vector3.UP * (projectile.kind.arc * 4 * share * (1 - share))
			if projectile.kind.arc > 0.0:
				projectile.node.rotate_x(6 * dt)
			continue
		projectile.node.queue_free()
		_flying.remove_at(i)
		if not alive:
			continue
		target.hp -= projectile.damage
		var hit := Hit.new()
		hit.projectile = projectile
		hit.at = dest
		hit.killed = target.hp <= 0
		# Dying right away: a second projectile arriving in the same frame must fizzle, not kill twice.
		if hit.killed:
			Enemies.defeat(target)
		hits.append(hit)
	return hits


func _body(kind: ProjectileKind) -> Node3D:
	if kind.model != "":
		return Models.scenery(kind.model).instantiate() as Node3D
	if not _materials.has(kind):
		_materials[kind] = Fx.unshaded(kind.color)
	var ball := MeshInstance3D.new()
	ball.mesh = _sphere
	ball.material_override = _materials[kind]
	ball.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	return ball
