class_name Catalog
extends Object
## The lists the game picks from. A new map, tower type or enemy is a resource under data/ plus its entry here.

## The maps, in the order of the map chooser.
const LEVELS: Array[LevelDef] = [
	preload("res://data/lichtung.tres"),
	preload("res://data/flussfurt.tres"),
	preload("res://data/burgberg.tres"),
	preload("res://data/schlucht.tres"),
]

## The tower types a map's tower spots take in turn.
const TOWER_MIX: Array[TowerKind] = [
	preload("res://data/bolt_tower.tres"),
	preload("res://data/catapult.tres"),
]

## The hero's skins; the first one is the free knight every child has. The server keeps what a skin costs and who
## owns it, by [member HeroSkin.id].
const SKINS: Array[HeroSkin] = [
	preload("res://data/skin_knight.tres"),
	preload("res://data/skin_red.tres"),
	preload("res://data/skin_gold.tres"),
	preload("res://data/skin_shadow.tres"),
]

## The hero's own shot: a quick blue bolt, weaker than a tower's.
const HERO_BOLT: ProjectileKind = preload("res://data/hero_bolt.tres")

## The common skeleton: quick and weak, worth points only.
const MINION: EnemyKind = preload("res://data/minion.tres")

## The armoured skeleton: slower, takes twice the hits, and carries a word.
const WARRIOR: EnemyKind = preload("res://data/warrior.tres")

## The runner: tiny, very quick and gone with one hit - it tests whether the towers still have shots.
const RUNNER: EnemyKind = preload("res://data/runner.tres")

## The shield bearer: a word carrier whose armour takes one off every hit, so strong towers matter.
const SHIELD: EnemyKind = preload("res://data/shield.tres")

## The healer: mends the enemies around it, so it is worth shooting first.
const HEALER: EnemyKind = preload("res://data/healer.tres")

## The giant that closes every fifth wave: slow, very tough, drops three words.
const BOSS: EnemyKind = preload("res://data/boss.tres")


## The skin with the given id, or the free knight when the id is unknown. (It lives here and not on [HeroSkin]: a
## resource script that names the catalog which preloads its own resources does not finish loading.)
static func skin(id: String) -> HeroSkin:
	for candidate: HeroSkin in SKINS:
		if candidate.id == id:
			return candidate
	return SKINS[0]
