class_name PositionsScreen
extends Page
## Pick the words: the plan positions a run can be built from, with what is left of today's play time and coins.

## A position was picked.
signal chosen(plan_position_id: int)

## "Zurück" was pressed.
signal back

## The server the overview comes from; set before the screen enters the tree.
var server: TowerServer


func _ready() -> void:
	set_title("Hallo %s! Welche Wörter?" % server.child_name)
	add_status()
	@warning_ignore("redundant_await")
	var result := await server.overview()
	if not is_inside_tree():
		return
	if result.ok:
		_list(result.data)
	else:
		status.text = result.error
	add_action("Zurück", back.emit)


func _list(overview: Dictionary) -> void:
	var seconds_left := maxi(0, int(overview.get("limitSeconds", 0)) - int(overview.get("playedSecondsToday", 0)))
	@warning_ignore("integer_division")
	var minutes_left := seconds_left / 60
	add_text("Spielzeit heute noch %d Min.  ·  Münzen heute %d/%d 🪙" % [
		minutes_left, int(overview.get("coinsToday", 0)), int(overview.get("dailyCoinCap", 0))], 26)
	var positions: Array = overview.get("positions", [])
	if positions.is_empty():
		status.text = "Kein aktiver Plan mit genug Wörtern."
	for entry: Dictionary in positions:
		# The subject is optional in the contract; JSON null must not show up as text.
		var subject: Variant = entry.get("subjectName")
		add_action("%s · %s (%d)" % [
			subject if subject is String else "", entry.get("exerciseTitle", ""), int(entry.get("wordCount", 0))],
			chosen.emit.bind(int(entry.get("planPositionId", 0))))
