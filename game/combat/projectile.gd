class_name Projectile
extends RefCounted
## One projectile in flight.

## Who fired a projectile: decides what a kill counts for and how it sounds.
enum Shooter {
	## A tower.
	TOWER,
	## The hero himself.
	HERO,
}

## What it is.
var kind: ProjectileKind

## Who fired it.
var from: Shooter

## The enemy it flies at.
var target: Enemy

## Its node in the scene.
var node: Node3D

## Where the projectile is on its straight line to the target; the node is lifted off it by the arc.
var track: Vector3

## Distance flown along the track.
var flown: float = 0.0
