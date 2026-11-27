class_name Catalog
extends Object
## The lists the game picks from. A new map, tower type or enemy is a resource under data/ plus its entry here.

## The maps, in the order of the map chooser.
const LEVELS: Array[LevelDef] = [
	preload("res://data/lichtung.tres"),
	preload("res://data/flussfurt.tres"),
	preload("res://data/burgberg.tres"),
]

## The tower types a map's tower spots take in turn.
const TOWER_MIX: Array[TowerKind] = [
	preload("res://data/bolt_tower.tres"),
	preload("res://data/catapult.tres"),
]

## The hero's own shot: a quick blue bolt, weaker than a tower's.
const HERO_BOLT: ProjectileKind = preload("res://data/hero_bolt.tres")

## The common skeleton: quick and weak, worth points only.
const MINION: EnemyKind = preload("res://data/minion.tres")

## The armoured skeleton: slower, takes twice the hits, and carries a word.
const WARRIOR: EnemyKind = preload("res://data/warrior.tres")

## The giant that closes every fifth wave: slow, very tough, drops three words.
const BOSS: EnemyKind = preload("res://data/boss.tres")
