class_name Models
extends Object
## Loads the KayKit models (CC0, see assets/kaykit) and hands out scenes, meshes and animation players.

const HEX: String = "res://assets/kaykit/hex/"
const CHARS: String = "res://assets/kaykit/chars/"


## One mesh of a scenery model with its placement inside the model.
class Part:
	extends RefCounted

	var mesh: Mesh
	var local: Transform3D


static var _scenes: Dictionary[String, PackedScene] = {}
static var _parts: Dictionary[String, Array] = {}


## A scenery model from the hexagon pack, by file name without extension.
static func scenery(model: String) -> PackedScene:
	return _load(HEX + model + ".gltf")


## A character or prop from the adventurers and skeletons packs, by file name with extension.
static func character(file: String) -> PackedScene:
	return _load(CHARS + file)


## The meshes of a scenery model with their local transforms, for drawing many copies in one MultiMesh.
static func parts(model: String) -> Array[Part]:
	if not _parts.has(model):
		var found: Array[Part] = []
		var root := scenery(model).instantiate() as Node3D
		_collect(root, Transform3D.IDENTITY, found)
		root.free()
		_parts[model] = found
	var cached: Array[Part] = []
	cached.assign(_parts[model])
	return cached


## The first animation player below [param root], or null.
static func animator(root: Node) -> AnimationPlayer:
	var players := root.find_children("*", "AnimationPlayer", true, false)
	return players[0] as AnimationPlayer if not players.is_empty() else null


## Plays the first of the given clips the model has; a looping clip is set to loop.
static func play(player: AnimationPlayer, loop: bool, clips: Array[String]) -> void:
	if player == null:
		return
	for clip: String in clips:
		if not player.has_animation(clip):
			continue
		if loop:
			player.get_animation(clip).loop_mode = Animation.LOOP_LINEAR
		if player.current_animation != clip:
			player.play(clip, 0.15)
		return


## Shows only the meshes whose names are listed; a name ending in "_" matches as a prefix. The character models carry
## every weapon at once.
static func keep_only(root: Node, keep: Array[String]) -> void:
	for node: Node in root.find_children("*", "MeshInstance3D", true, false):
		var mesh := node as MeshInstance3D
		var mesh_name := String(mesh.name)
		var shown := false
		for wanted: String in keep:
			if mesh_name.begins_with(wanted) if wanted.ends_with("_") else mesh_name == wanted:
				shown = true
		mesh.visible = shown


static func _load(path: String) -> PackedScene:
	if not _scenes.has(path):
		_scenes[path] = load(path) as PackedScene
	return _scenes[path]


static func _collect(node: Node, parent: Transform3D, found: Array[Part]) -> void:
	for child: Node in node.get_children():
		var spatial := child as Node3D
		if spatial == null:
			continue
		var placed := parent * spatial.transform
		var instance := spatial as MeshInstance3D
		if instance != null and instance.mesh != null:
			var part := Part.new()
			part.mesh = instance.mesh
			part.local = placed
			found.append(part)
		_collect(spatial, placed, found)
