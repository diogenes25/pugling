class_name TowerTarget
extends RefCounted
## A meaning a tower asks for, as the server names it. Which word fits is the server's secret.

## The server's id of the tower slot.
var tower_id: String

## The meaning shown on the tower.
var prompt: String


func _init(id: String, meaning: String) -> void:
	tower_id = id
	prompt = meaning
