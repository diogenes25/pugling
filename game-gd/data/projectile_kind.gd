class_name ProjectileKind
extends Resource
## One kind of projectile as data (bolt.tres, fire_arrow.tres, boulder.tres, fire_boulder.tres, hero_bolt.tres).

## Name for logs and for picking the sound.
@export var id: String = ""

## Colour of the projectile and its hit sparks.
@export var color: Color = Color.WHITE

## Scale of the projectile.
@export var size: float = 1.0

## Flight speed in units per second.
@export var speed: float = 20.0

## Hit points one hit takes.
@export var damage: int = 1

## A model from the scenery pack instead of the plain sphere ("" for the sphere).
@export var model: String = ""

## Height of the bow the projectile flies, in units; 0 flies straight (arrows), more is a lobbed stone.
@export var arc: float = 0.0

## Leaves a trail of sparks while it flies.
@export var burning: bool = false
