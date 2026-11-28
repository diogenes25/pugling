class_name EnemyKind
extends Resource
## One kind of enemy as data (minion.tres, warrior.tres, runner.tres ...). A new enemy is a new resource plus a rule
## in [WavePlan] for when it comes; the scene side only reads these fields.

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

## Hit points it gains per wave after the first (the fraction adds up over the waves).
@export var hp_per_wave: float = 0.0

## Points for defeating it.
@export var points: int = 10

## Books it drops when defeated: small enemies bring points, the bigger ones bring words.
@export var drops: int = 0

## Hit points its armour takes off every hit (a hit always takes at least one, see [method EnemyRules.damage_taken]).
@export var armor: int = 0

## A healer mends every other enemy within [member heal_radius] by this many hit points every
## [member heal_interval] seconds; 0 heals nobody.
@export var heal_amount: int = 0
@export var heal_interval: float = 0.0
@export var heal_radius: float = 0.0

## Colours the model, so kinds that share a model can be told apart at a glance.
@export var tint: Color = Color.WHITE

## What the kind is called on screen.
@export var title: String = ""
