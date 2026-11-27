class_name Fx
extends Object
## Small scene effects and helper meshes the level builds in code.


## An unlit material in one colour.
static func unshaded(color: Color) -> StandardMaterial3D:
	var material := StandardMaterial3D.new()
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.albedo_color = color
	return material


## A flat ring on the ground (a tower's range, the hero's reach).
static func ring(at: Vector3, radius: float, color: Color) -> MeshInstance3D:
	var torus := TorusMesh.new()
	torus.inner_radius = radius - 0.12
	torus.outer_radius = radius
	torus.rings = 48
	torus.ring_segments = 4
	var material := unshaded(color)
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	var instance := MeshInstance3D.new()
	instance.mesh = torus
	instance.position = at
	instance.scale = Vector3(1, 0.05, 1)
	instance.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	instance.material_override = material
	return instance


## A trail of sparks that follows whatever it is added to (a burning projectile) and stays behind in the world.
static func trail(color: Color) -> CPUParticles3D:
	var particles := CPUParticles3D.new()
	particles.emitting = true
	particles.amount = 24
	particles.lifetime = 0.45
	particles.local_coords = false
	particles.mesh = _sphere(0.12, 6, 3)
	particles.material_override = unshaded(color)
	particles.direction = Vector3.UP
	particles.spread = 180.0
	particles.initial_velocity_min = 0.3
	particles.initial_velocity_max = 1.2
	particles.gravity = Vector3.ZERO
	particles.scale_amount_min = 0.4
	particles.scale_amount_max = 1.1
	particles.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	return particles


## A catapult's launch: a flash of fire and a puff of grey smoke that rises and thins out.
static func blast(parent: Node3D, at: Vector3) -> void:
	burst(parent, at, Color(1.0, 0.75, 0.2), 18, 0.7)
	burst(parent, at, Color(1.0, 0.35, 0.1), 10, 0.5)
	var material := StandardMaterial3D.new()
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.vertex_color_use_as_albedo = true
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	var fade := Gradient.new()
	fade.colors = PackedColorArray([Color(0.75, 0.73, 0.7, 0.8), Color(0.55, 0.55, 0.55, 0.0)])
	var smoke := CPUParticles3D.new()
	smoke.emitting = true
	smoke.one_shot = true
	smoke.explosiveness = 0.85
	smoke.amount = 14
	smoke.lifetime = 1.6
	smoke.mesh = _sphere(0.35, 8, 4)
	smoke.material_override = material
	smoke.color_ramp = fade
	smoke.direction = Vector3.UP
	smoke.spread = 35.0
	smoke.initial_velocity_min = 0.8
	smoke.initial_velocity_max = 2.0
	smoke.gravity = Vector3(0, 0.6, 0)
	smoke.scale_amount_min = 0.7
	smoke.scale_amount_max = 1.6
	smoke.position = at
	smoke.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	parent.add_child(smoke)
	parent.get_tree().create_timer(2.0).timeout.connect(smoke.queue_free)


## A short squash of a model, as if the shot had kicked it; it springs back by itself.
static func recoil(model: Node3D) -> void:
	var rest := model.scale
	var tween := model.create_tween()
	tween.tween_property(model, "scale", Vector3(rest.x * 1.06, rest.y * 0.9, rest.z * 1.06), 0.06)
	tween.tween_property(model, "scale", rest, 0.22).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)


## A short burst of sparks that removes itself.
static func burst(parent: Node3D, at: Vector3, color: Color, amount: int, size: float = 1.0) -> void:
	var particles := CPUParticles3D.new()
	particles.emitting = true
	particles.one_shot = true
	particles.explosiveness = 0.95
	particles.amount = amount
	particles.lifetime = 0.9
	particles.mesh = _sphere(0.09 * size, 6, 3)
	particles.material_override = unshaded(color)
	particles.direction = Vector3.UP
	particles.spread = 70.0
	particles.initial_velocity_min = 3.0 * size
	particles.initial_velocity_max = 7.0 * size
	particles.gravity = Vector3(0, -12, 0)
	particles.scale_amount_min = 0.6
	particles.scale_amount_max = 1.4
	particles.position = at
	particles.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	parent.add_child(particles)
	parent.get_tree().create_timer(1.3).timeout.connect(particles.queue_free)


static func _sphere(radius: float, segments: int, rings: int) -> SphereMesh:
	var sphere := SphereMesh.new()
	sphere.radius = radius
	sphere.height = radius * 2.0
	sphere.radial_segments = segments
	sphere.rings = rings
	return sphere
