class_name GameProgress
extends RefCounted
## What the child collected in the game, as the server counts it ("progress" of the tower-game overview): trophies
## with their path, the game streak and today's game chest. An older server sends none of it; then everything stays
## at its defaults and [member known] is false, so the start screen can leave the parts out.

## The server sent the progress.
var known: bool = false

## Lifetime trophies.
var trophies: int = 0

## The trophy path.
var path: TrophyPath = TrophyPath.new()

## Days in a row with enough correct words in the game.
var streak: int = 0

## Correct words in the game today, and how many a day needs for the streak.
var words_today: int = 0
var streak_words: int = 0

## Today's game chest: correct words it holds and needs, whether it can be opened, whether it was opened.
var chest_words: int = 0
var chest_needed: int = 0
var chest_ready: bool = false
var chest_opened: bool = false


## Reads the tower-game overview; without "progress" the result is unknown.
static func from_json(overview: Dictionary) -> GameProgress:
	var result := GameProgress.new()
	var raw: Variant = overview.get("progress")
	if not raw is Dictionary:
		return result
	var json := raw as Dictionary
	result.known = true
	result.trophies = int(json.get("trophies", 0))
	for entry: Variant in json.get("trophyPath", []):
		if entry is Dictionary:
			result.path.add(int((entry as Dictionary).get("trophies", 0)), int((entry as Dictionary).get("gems", 0)))
	result.streak = int(json.get("streak", 0))
	result.words_today = int(json.get("wordsToday", 0))
	result.streak_words = int(json.get("streakWords", 0))
	var chest: Variant = json.get("chest")
	if chest is Dictionary:
		result.read_chest(chest as Dictionary)
	return result


## Takes over the state of the game chest (also from the answer of opening it).
func read_chest(chest: Dictionary) -> void:
	chest_words = int(chest.get("words", 0))
	chest_needed = int(chest.get("wordsNeeded", 0))
	chest_ready = bool(chest.get("ready", false))
	chest_opened = bool(chest.get("openedToday", false))
