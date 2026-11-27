class_name Enemy
extends RefCounted
## One enemy on the map.

## What it is.
var kind: EnemyKind

## Index of the path it walks.
var path: int

## Walking speed in units per second.
var speed: float

## Its model in the scene.
var node: Node3D

## The model's animation player, if it has one.
var anim: AnimationPlayer

## Distance walked along the path.
var distance: float = 0.0

## Distance still to go to the castle; the towers shoot the one that is furthest along.
var remaining: float = 0.0

## Hit points left.
var hp: int = 0

## Hit points it entered the map with (its kind's, grown with the wave).
var max_hp: int = 1

## Defeated and playing its death clip; no longer a target.
var dying: bool = false

## Seconds until the dying enemy is removed.
var dying_time: float = 0.0


## Its place on the ground.
func ground() -> Vector2:
	return Geo.flat(node.position)
