class_name Catalog
extends Object
## The lists the game picks from. A new map, tower type or enemy is a resource under data/ plus its entry here.

## The maps, in the order of the map chooser.
const LEVELS: Array[LevelDef] = [
	preload("res://data/lichtung.tres"),
	preload("res://data/flussfurt.tres"),
	preload("res://data/burgberg.tres"),
]
