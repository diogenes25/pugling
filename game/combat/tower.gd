class_name Tower
extends RefCounted
## A tower on the map: it shoots while it has ammo and asks for the word of one meaning.

## What it is.
var kind: TowerKind

## Its place on the ground.
var pos: Vector2

## The meaning the tower asks for, or null when the run has no more.
var target: TowerTarget

## Its ammunition.
var ammo: TowerAmmo = TowerAmmo.new()

## Its level, grown by the value of the words it got.
var level: TowerLevel = TowerLevel.new()

## Its reload timer.
var reload: Cooldown = Cooldown.new()

## The tower's model in the scene (launch effects move it).
var model: Node3D
