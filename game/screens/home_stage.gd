class_name HomeStage
extends SubViewportContainer
## The picture behind the start screen: the hero on a meadow in front of his castle, with two towers, trees and
## hills, under an evening sky. It is a small 3D world of its own, built from the same models as the maps. Clouds
## drift, the flags of the menu are the buttons - the stage itself takes no input.

const SKY_TOP := Color(0.13, 0.2, 0.46)
const SKY_LOW := Color(0.95, 0.66, 0.42)
const CHEER_EVERY: float = 9.0
const CLOUDS: int = 5

## The hero standing in front; the start screen may swap his look.
var hero: HeroLook

var _stage: SubViewport = SubViewport.new()
var _clouds: Array[MeshInstance3D] = []
var _since_cheer: float = 0.0
var _clock: float = 0.0


func _init() -> void:
	stretch = true
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	set_anchors_preset(Control.PRESET_FULL_RECT)


func _ready() -> void:
	_stage.own_world_3d = true
	add_child(_stage)
	var world := WorldEnvironment.new()
	var env := Environment.new()
	env.background_mode = Environment.BG_SKY
	var sky := Sky.new()
	var gradient := ProceduralSkyMaterial.new()
	gradient.sky_top_color = SKY_TOP
	gradient.sky_horizon_color = SKY_LOW
	gradient.ground_horizon_color = SKY_LOW
	gradient.ground_bottom_color = Color(0.3, 0.5, 0.3)
	gradient.sun_angle_max = 0.0
	sky.sky_material = gradient
	env.sky = sky
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color(0.85, 0.85, 1.0)
	env.ambient_light_energy = 0.55
	world.environment = env
	_stage.add_child(world)
	var sun := DirectionalLight3D.new()
	sun.rotation_degrees = Vector3(-42, 38, 0)
	sun.light_color = Color(1.0, 0.93, 0.82)
	_stage.add_child(sun)
	var camera := Camera3D.new()
	camera.fov = 38
	_stage.add_child(camera)
	camera.look_at_from_position(Vector3(0, 2.2, 8.2), Vector3(0, 1.25, 0))

	# The meadow: a few rows of hexes, wide enough to fill the picture's lower half.
	for row: int in range(-9, 3):
		for col: int in range(-9, 10):
			_put("hex_grass", Vector3(col * MapBuilder.HEX_WIDTH + (absi(row) % 2) * MapBuilder.HEX_WIDTH / 2.0, -0.02,
					row * MapBuilder.HEX_ROW), 1.0, 0.0)
	_put("building_castle_blue", Vector3(0, 0, -7.5), 2.3, 0.0)
	_put("building_tower_A_blue", Vector3(-5.6, 0, -3.6), 1.7, 0.3)
	_put("building_tower_catapult_blue", Vector3(5.8, 0, -3.8), 1.7, -0.4)
	_put("flag_blue", Vector3(-2.3, 0, -0.6), 1.5, 0.4)
	_put("flag_blue", Vector3(2.3, 0, -0.6), 1.5, -0.4)
	_put("mountain_A_grass_trees", Vector3(-11, 0, -11), 3.2, 0.6)
	_put("mountain_B_grass_trees", Vector3(11.5, 0, -12), 3.4, 2.1)
	_put("hills_A_trees", Vector3(-6.5, 0, -10.5), 2.6, 1.2)
	_put("hills_A_trees", Vector3(6.2, 0, -10.8), 2.4, 4.0)
	var spots: Array[Vector3] = [Vector3(-8.2, 0, -5.4), Vector3(-9.6, 0, -1.6), Vector3(-7.4, 0, 0.6), Vector3(8.4, 0, -6.0),
		Vector3(9.8, 0, -2.2), Vector3(7.6, 0, 0.4), Vector3(-3.6, 0, -6.6), Vector3(3.8, 0, -6.4)]
	for i: int in spots.size():
		_put(MapBuilder.TREES[i % MapBuilder.TREES.size()], spots[i], 1.5, i * 1.3)
	_put("rock_single_B", Vector3(-3.0, 0, 1.4), 1.2, 0.7)
	_put("barrel", Vector3(3.1, 0, 1.2), 1.3, 0.0)

	for i: int in CLOUDS:
		var cloud := MeshInstance3D.new()
		var puff := SphereMesh.new()
		puff.radius = 1.0
		puff.height = 1.1
		puff.radial_segments = 12
		puff.rings = 6
		cloud.mesh = puff
		cloud.material_override = Fx.unshaded(Color(1, 0.94, 0.88, 0.9))
		cloud.scale = Vector3(2.6 + i % 3, 0.7, 1.0)
		cloud.position = Vector3(-18.0 + i * 8.5, 7.5 + (i % 2) * 1.6, -16.0 - (i % 3) * 2.0)
		_stage.add_child(cloud)
		_clouds.append(cloud)


## Puts the hero's look on the stage (replacing the one before).
func show_hero(look: HeroLook) -> void:
	if hero != null:
		hero.queue_free()
	hero = look
	hero.position = Vector3(0, 0, 1.6)
	_stage.add_child(hero)


func _process(delta: float) -> void:
	_clock += delta
	for cloud: MeshInstance3D in _clouds:
		cloud.position.x += delta * 0.35
		if cloud.position.x > 24.0:
			cloud.position.x = -24.0
	if hero == null:
		return
	# The hero idles and cheers now and then, so the screen is alive.
	_since_cheer += delta
	if _since_cheer >= CHEER_EVERY:
		_since_cheer = 0.0
		hero.play(HeroLook.Action.CHEER)
	hero.update(delta, false, 0.35 * sin(_clock * 0.7))


## Lets the hero cheer right now (a chest was opened, a reward came in).
func cheer() -> void:
	_since_cheer = 0.0
	if hero != null:
		hero.play(HeroLook.Action.CHEER)


func _put(model: String, at: Vector3, model_scale: float, angle: float) -> void:
	var node := Models.scenery(model).instantiate() as Node3D
	node.position = at
	node.scale = Vector3.ONE * model_scale
	node.rotation = Vector3(0, angle, 0)
	_stage.add_child(node)
