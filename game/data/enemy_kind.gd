class_name EnemyKind
extends Resource
## One kind of enemy as data (minion.tres, warrior.tres). A new enemy is a new resource plus a rule in
## [SpawnSchedule] for when it comes; the scene side only reads these fields.

## Model file in the character pack (see [method Models.character]).
@export var model: String = ""

## Only the model's meshes with this prefix stay visible (the packs carry spare weapons).
@export var mesh_prefix: String = ""

## Model scale.
@export var model_scale: float = 1.0

## Hit points.
@export var hp: int = 1

## Multiplies the map's skeleton speed.
@export var speed_factor: float = 1.0
