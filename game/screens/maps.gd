class_name MapsScreen
extends Page
## Pick the map for the run.

## A map was picked.
signal chosen(level: LevelDef)

## "Andere Wörter" was pressed.
signal other_words


func _ready() -> void:
	set_title("Welche Karte?")
	for level: LevelDef in Catalog.LEVELS:
		add_action(level.title, chosen.emit.bind(level))
		add_text(level.description, 22)
	add_action("Andere Wörter", other_words.emit)
	add_status()
