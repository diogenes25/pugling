class_name ResultScreen
extends Page
## The result of a level. Without the server's answer nobody knows whether the level is won: the screen says so and
## offers the end call again (it is idempotent), instead of showing a won level as lost and never booking its coins.

## "Noch einmal": the same words on the same map.
signal again

## "Andere Karte" was pressed.
signal other_map

## "Andere Wörter" was pressed.
signal other_words

## "Ergebnis nochmal senden" was pressed.
signal resend

## Correct assignments of the level, as the client counted them.
var correct: int = 0

## All graded assignments of the level.
var total: int = 0

## Why the level ended.
var reason: LevelEnd.Reason = LevelEnd.Reason.GAVE_UP

## The server's answer to the end call.
var end: ApiResult


func _ready() -> void:
	set_title(_headline())
	add_text("%d von %d Zuweisungen richtig." % [correct, total], 30)
	if end.ok:
		add_text(_coins(), 30)
	else:
		add_action("Ergebnis nochmal senden", resend.emit)
	add_action("Noch einmal", again.emit)
	add_action("Andere Karte", other_map.emit)
	add_action("Andere Wörter", other_words.emit)
	add_status()
	status.text = end.error


func _headline() -> String:
	if not end.ok:
		return "Ergebnis noch nicht gespeichert."
	if end.data.get("won", false):
		return "Level geschafft!"
	match reason:
		LevelEnd.Reason.TIME_UP:
			return "Spielzeit für heute ist um."
		LevelEnd.Reason.GAVE_UP:
			return "Spiel beendet."
	return "Die Monster sind durchgebrochen."


func _coins() -> String:
	var awarded := int(end.data.get("coinsAwarded", 0))
	var today := int(end.data.get("coinsToday", 0))
	var cap := int(end.data.get("dailyCoinCap", 0))
	if awarded > 0:
		return "+%d 🪙  (heute %d/%d)" % [awarded, today, cap]
	if end.data.get("won", false):
		return "Münzdeckel für heute erreicht (%d/%d 🪙)." % [today, cap]
	return "Keine Münzen diesmal."
