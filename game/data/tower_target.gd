class_name TowerTarget
extends RefCounted
## A meaning a tower asks for, as the server names it. Which word fits is the server's secret.

## The server's id of the tower slot.
var tower_id: String

## The meaning shown on the tower.
var prompt: String

## What the word is worth for the tower: 3 for a new word down to 1 for a well learned one.
var value: int


func _init(id: String, meaning: String, worth: int = Balance.DEFAULT_WORD_VALUE) -> void:
	tower_id = id
	prompt = meaning
	value = worth
