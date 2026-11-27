class_name TowerKind
extends Resource
## One kind of tower as data (bolt_tower.tres, catapult.tres). A new tower type is a new resource plus its entry in
## [constant Catalog.TOWER_MIX].

## Name for logs.
@export var id: String = ""

## Model variants from the scenery pack; towers of one kind take turns through them.
@export var models: PackedStringArray = []

## Model scale.
@export var model_scale: float = 1.0

## How far the tower shoots.
@export var attack_range: float = 7.5

## Seconds between two shots.
@export var shot_interval: float = 0.8

## Height the projectile leaves the tower at.
@export var muzzle_height: float = 3.6

## The normal projectile.
@export var shot: ProjectileKind

## The projectile of a strong tower.
@export var strong_shot: ProjectileKind

## Radius the hero cannot walk into.
@export var footprint: float = 1.0

## What the feedback line calls the strong shot.
@export var strong_shot_name: String = "Feuerpfeile"

## A shot comes with a muzzle flash, smoke and a recoil of the model (see level/feedback.gd).
@export var blast: bool = false
