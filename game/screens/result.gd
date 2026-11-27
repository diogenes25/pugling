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

## The run's points, line by line.
var score: RunScore = RunScore.new()

## The last wave the run reached.
var wave: int = 0

## The highest level a tower reached.
var top_tower_level: int = 1

## The run beat the map's best score.
var record: bool = false

## The map's best score (this run included).
var best: int = 0

## Why the level ended.
var reason: LevelEnd.Reason = LevelEnd.Reason.GAVE_UP

## The server's answer to the end call.
var end: ApiResult


func _ready() -> void:
	set_title(_headline())
	add_text("Welle %d  ·  %d Punkte%s" % [wave, score.total(),
			"  ·  NEUER REKORD!" if record else "  ·  Rekord %d" % best], 34)
	for line: RunScore.Line in score.lines():
		add_text("%s: %d  →  %d Punkte" % [line.label, line.count, line.points], 24)
	add_text("%d von %d Vokabeln richtig  ·  bester Turm Stufe %d" % [correct, total, top_tower_level], 24)
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
	if record:
		return "Neuer Rekord!"
	match reason:
		LevelEnd.Reason.TIME_UP:
			return "Spielzeit für heute ist um."
		LevelEnd.Reason.GAVE_UP:
			return "Spiel beendet."
	return "Die Burg ist gefallen."


func _coins() -> String:
	var awarded := int(end.data.get("coinsAwarded", 0))
	var today := int(end.data.get("coinsToday", 0))
	var cap := int(end.data.get("dailyCoinCap", 0))
	if awarded > 0:
		return "+%d 🪙  (heute %d/%d)" % [awarded, today, cap]
	if end.data.get("won", false):
		return "Münzdeckel für heute erreicht (%d/%d 🪙)." % [today, cap]
	return "Münzen gibt es ab %d richtigen Vokabeln." % int(end.data.get("goal", 0))
