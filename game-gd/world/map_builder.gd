class_name MapBuilder
extends Object
## Builds the 3D scenery of a map from its [LevelDef]: a hex field (grass, or water where the river runs), the roads
## of the monster paths, bridges, the castle, mountains, and trees and rocks scattered by the map's seed. Whatever
## repeats is drawn as a MultiMesh - a few draw calls instead of thousands of nodes. The order of the random draws
## decides how a map looks, so keep it when changing this file.

# KayKit hexes are pointy-top: 2 units wide, 2.3 deep, so rows sit 0.75 * 2.3 apart.
const HEX_WIDTH: float = 2.0
const HEX_ROW: float = 1.725
const BORDER: float = 16.0

# Cells whose centre is this close to the river line become water (a little wider than the blocking band).
const RIVER_HALF_CELLS: float = 2.2

const TREES: Array[String] = ["trees_A_large", "trees_A_medium", "trees_B_large", "trees_B_medium", "tree_single_A", "tree_single_B"]
const ROCKS: Array[String] = ["rock_single_A", "rock_single_B", "rock_single_C"]
const MOUNTAINS: Array[String] = ["mountain_A_grass_trees", "mountain_B_grass_trees", "hills_A_trees"]
const CAMP: Dictionary[Vector2, String] = {
	Vector2(-2.6, 2.2): "flag_blue",
	Vector2(2.7, 2.0): "flag_blue",
	Vector2(3.2, -1.4): "barrel",
	Vector2(-3.1, -1.2): "crate_A_big",
	Vector2(0, 3.4): "sack",
}


## What is being built: the copies of each model and the circles scenery keeps clear of.
class Draft:
	extends RefCounted

	var def: LevelDef
	var placed: Dictionary[String, Array] = {}
	## x and y are the centre, z the radius.
	var blocked: Array[Vector3] = []

	func put(model: String, at: Vector2, size: float, angle: float, y: float = 0.0) -> void:
		if not placed.has(model):
			placed[model] = []
		placed[model].append(Transform3D(Basis.from_euler(Vector3(0, angle, 0)).scaled(Vector3.ONE * size), Geo.to3(at, y)))

	func keep_clear(at: Vector2, radius: float) -> void:
		blocked.append(Vector3(at.x, at.y, radius))

	## Whether scenery may stand at a point: off the roads, out of the river, away from everything marked.
	func clear(at: Vector2, road_gap: float) -> bool:
		for path: PackedVector2Array in def.paths:
			if Geo.dist_to_polyline(at, path) <= road_gap:
				return false
		if def.river.size() >= 2 and Geo.dist_to_polyline(at, def.river) <= MapInfo.RIVER_HALF_WIDTH + 0.8:
			return false
		for circle: Vector3 in blocked:
			if Vector2(circle.x, circle.y).distance_to(at) <= circle.z:
				return false
		return true


## Adds the scenery to [param parent] and returns the map's walkability.
static func build(def: LevelDef, parent: Node3D) -> MapInfo:
	var map := MapInfo.new(def)
	var draft := Draft.new()
	draft.def = def
	var rng := RandomNumberGenerator.new()
	rng.seed = def.map_seed

	# Ground: one hex per cell, water where the river runs.
	var row := 0
	while row * HEX_ROW < def.size.y + 2 * BORDER:
		var col := 0
		while col * HEX_WIDTH < def.size.x + 2 * BORDER + HEX_WIDTH:
			var at := Vector2(col * HEX_WIDTH + (row % 2) * HEX_WIDTH / 2 - BORDER, row * HEX_ROW - BORDER)
			var water := def.river.size() > 1 and Geo.dist_to_polyline(at, def.river) < RIVER_HALF_CELLS
			draft.put("hex_water" if water else "hex_grass", at, 1.0, 0.0)
			if water and rng.randf() < 0.12 and _far_from_bridges(def, at):
				var plant := "waterlily_A" if rng.randf() < 0.5 else "waterplant_A"
				draft.put(plant, at, 1.0, rng.randf() * TAU, -0.2)
			col += 1
		row += 1

	for bridge: Vector2 in def.bridges:
		# The bridge runs along the road that crosses here.
		var along := _road_direction(def, bridge)
		draft.put("building_bridge_A", bridge, 1.9, atan2(along.x, along.y))

	_add_roads(def, parent)

	# Castle at the end of the paths, a little camp around it.
	var castle := Models.scenery("building_castle_blue").instantiate() as Node3D
	castle.position = Geo.to3(def.castle())
	castle.scale = Vector3.ONE * 1.4
	parent.add_child(castle)
	map.block(def.castle(), 2.0)
	draft.keep_clear(def.castle(), 4.0)
	for offset: Vector2 in CAMP:
		draft.put(CAMP[offset], def.castle() + offset, 1.4, rng.randf() * TAU)

	for spot: Vector2 in def.tower_spots:
		draft.keep_clear(spot, 2.8)
	draft.keep_clear(def.player_start, 2.5)

	for spot: Vector2 in def.mountains:
		var mountain := MOUNTAINS[rng.randi_range(0, MOUNTAINS.size() - 1)]
		draft.put(mountain, spot, 2.4, rng.randf() * TAU)
		map.block(spot, 2.0)
		draft.keep_clear(spot, 3.2)

	# Inside: loose trees and rocks the hero walks around.
	var inside := int(def.size.x * def.size.y / 16)
	for i: int in inside:
		var at := Vector2(rng.randf_range(1, def.size.x - 1), rng.randf_range(1, def.size.y - 1))
		if not draft.clear(at, 2.6):
			continue
		var rock := rng.randf() < 0.25
		var size := rng.randf_range(0.9, 1.3)
		var model := ROCKS[rng.randi_range(0, ROCKS.size() - 1)] if rock else TREES[rng.randi_range(0, TREES.size() - 1)]
		draft.put(model, at, size, rng.randf() * TAU)
		map.block(at, (0.45 if rock else 0.7) * size)
		draft.keep_clear(at, 1.7)

	# Beyond the edge: a dense forest with the odd mountain, so the world does not end on screen.
	var z := -BORDER
	while z < def.size.y + BORDER:
		var x := -BORDER
		while x < def.size.x + BORDER:
			var at := Vector2(x + rng.randf_range(-0.5, 0.5), z + rng.randf_range(-0.5, 0.5))
			var outside := at.x < -0.5 or at.y < -0.5 or at.x > def.size.x + 0.5 or at.y > def.size.y + 0.5
			if outside and draft.clear(at, 2.4):
				var roll := rng.randf()
				if roll < 0.04:
					var mountain := MOUNTAINS[rng.randi_range(0, MOUNTAINS.size() - 1)]
					var size := rng.randf_range(2.0, 3.0)
					draft.put(mountain, at, size, rng.randf() * TAU)
				elif roll < 0.62:
					var tree := TREES[rng.randi_range(0, TREES.size() - 1)]
					var size := rng.randf_range(1.0, 1.5)
					draft.put(tree, at, size, rng.randf() * TAU)
				elif roll < 0.68:
					draft.put(ROCKS[rng.randi_range(0, ROCKS.size() - 1)], at, 1.2, rng.randf() * TAU)
			x += 1.6
		z += 1.6

	for model: String in draft.placed:
		_add_multi(parent, model, draft.placed[model])
	return map


static func _far_from_bridges(def: LevelDef, at: Vector2) -> bool:
	for bridge: Vector2 in def.bridges:
		if bridge.distance_to(at) <= 3.0:
			return false
	return true


## The direction of the road segment that passes closest to a point.
static func _road_direction(def: LevelDef, p: Vector2) -> Vector2:
	var best := INF
	var along := Vector2.RIGHT
	for path: PackedVector2Array in def.paths:
		for i: int in range(1, path.size()):
			var distance := Geo.dist_to_segment(p, path[i - 1], path[i])
			if distance < best:
				best = distance
				along = (path[i] - path[i - 1]).normalized()
	return along


static func _add_multi(parent: Node3D, model: String, transforms: Array) -> void:
	for part: Models.Part in Models.parts(model):
		var multi := MultiMesh.new()
		multi.transform_format = MultiMesh.TRANSFORM_3D
		multi.mesh = part.mesh
		multi.instance_count = transforms.size()
		for i: int in transforms.size():
			var placed: Transform3D = transforms[i]
			multi.set_instance_transform(i, placed * part.local)
		var instance := MultiMeshInstance3D.new()
		instance.multimesh = multi
		# KayKit's grass is a light lime that reads as yellow under a bright sun; tint it towards green.
		var grass := part.mesh.surface_get_material(0) as StandardMaterial3D
		if model == "hex_grass" and grass != null:
			var tinted := grass.duplicate() as StandardMaterial3D
			tinted.albedo_color = Color(0.6, 0.85, 0.55)
			instance.material_override = tinted
		parent.add_child(instance)


## Roads are their own meshes: a dark rim with a light track on top, following the monster paths exactly.
static func _add_roads(def: LevelDef, parent: Node3D) -> void:
	var rim := StandardMaterial3D.new()
	rim.albedo_color = Color(0.42, 0.31, 0.2)
	var track := StandardMaterial3D.new()
	track.albedo_color = Color(0.78, 0.62, 0.42)
	for material: StandardMaterial3D in [rim, track]:
		material.roughness = 1.0
		material.cull_mode = BaseMaterial3D.CULL_DISABLED
	for path: PackedVector2Array in def.paths:
		_ribbon(parent, path, 2.7, 0.02, rim)
		_ribbon(parent, path, 2.1, 0.04, track)


static func _ribbon(parent: Node3D, path: PackedVector2Array, width: float, y: float, material: Material) -> void:
	var surface := SurfaceTool.new()
	surface.begin(Mesh.PRIMITIVE_TRIANGLES)
	surface.set_normal(Vector3.UP)
	for i: int in range(1, path.size()):
		var a := path[i - 1]
		var b := path[i]
		var side := (b - a).normalized().orthogonal() * width / 2
		var quad: Array[Vector3] = [Geo.to3(a - side, y), Geo.to3(a + side, y), Geo.to3(b + side, y), Geo.to3(b - side, y)]
		# Godot treats clockwise triangles as front faces; seen from above this order is clockwise.
		for corner: int in [0, 1, 2, 0, 2, 3]:
			surface.add_vertex(quad[corner])
	var band := MeshInstance3D.new()
	band.mesh = surface.commit()
	band.material_override = material
	band.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	parent.add_child(band)

	# A disc on every corner closes the gap two straight bands leave.
	var disc := CylinderMesh.new()
	disc.top_radius = width / 2
	disc.bottom_radius = width / 2
	disc.height = 0.02
	disc.radial_segments = 24
	for corner: Vector2 in path:
		var joint := MeshInstance3D.new()
		joint.mesh = disc
		joint.material_override = material
		joint.position = Geo.to3(corner, y - 0.005)
		joint.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
		parent.add_child(joint)
