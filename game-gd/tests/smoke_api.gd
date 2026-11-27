extends Node
## Contract check of all six calls against a real (throwaway) API - the server rejects unknown fields, so a wrong
## field name shows up here as a 400. Never point it at the home server: it starts and ends a real run.
## Run: godot --headless --path . res://tests/smoke_api.tscn -- --server=http://127.0.0.1:5281 --child=3 --pin=4711
## Exit code 0 when every call answered as the contract says.

var _failed: int = 0


func _ready() -> void:
	get_tree().create_timer(60.0, true, false, true).timeout.connect(get_tree().quit.bind(1))
	var server := UiDriver.arg("server")
	var wrong := await Api.login(server, UiDriver.arg("child", "3").to_int(), "0000")
	_note("login falsche PIN", not wrong.ok and wrong.error == "PIN falsch oder Anmeldung abgelaufen.", wrong.error)
	var login := await Api.login(server, UiDriver.arg("child", "3").to_int(), UiDriver.arg("pin"))
	_note("login", login.ok and Api.child_name != "", Api.child_name + login.error)
	var overview := await Api.overview()
	var positions: Array = overview.data.get("positions", [])
	_note("overview", overview.ok and not positions.is_empty() and overview.data.has("limitSeconds"),
			"%d Positionen %s" % [positions.size(), overview.error])
	if positions.is_empty():
		get_tree().quit(1)
		return
	var first: Dictionary = positions[0]
	var started := await Api.start_run(int(first["planPositionId"]))
	var run := TowerRun.from_json(started.data)
	_note("start_run", started.ok and run.run_id > 0 and run.goal > 0 and not run.towers.is_empty() and not run.tokens.is_empty(),
			"Lauf %d, Ziel %d, %d Bedeutungen, %d Wörter %s" % [run.run_id, run.goal, run.towers.size(), run.tokens.size(), started.error])
	# Try the first tower's meaning with every word until the server says "correct": proves both verdicts.
	var verdicts: Dictionary[bool, int] = {true: 0, false: 0}
	for token: TowerToken in run.tokens:
		var graded := await Api.assign(run.run_id, token.token_id, run.towers[0].tower_id)
		if not graded.ok or not graded.data.has("correct") or not graded.data.has("expected"):
			_note("assign", false, graded.error)
			break
		verdicts[graded.data["correct"] as bool] += 1
		if graded.data["correct"]:
			break
	_note("assign", verdicts[true] == 1, "%d falsch, %d richtig" % [verdicts[false], verdicts[true]])
	var beat := await Api.heartbeat(run.run_id)
	_note("heartbeat", beat.ok and beat.data.has("timeUp"), str(beat.data) + beat.error)
	var end := await Api.end_run(run.run_id)
	_note("end_run", end.ok and end.data.has("won") and end.data.has("coinsAwarded"), str(end.data) + end.error)
	var late := await Api.assign(run.run_id, run.tokens[0].token_id, run.towers[0].tower_id)
	_note("assign nach dem Ende", not late.ok and late.run_closed and late.error == "Das Level ist vorbei.", late.error)
	var missing := await Api.start_run(999999)
	_note("start_run unbekannte Position", not missing.ok, missing.error)
	get_tree().quit(1 if _failed > 0 else 0)


func _note(what: String, ok: bool, detail: String) -> void:
	if not ok:
		_failed += 1
	Log.line("api %s: %s (%s)" % [what, "ok" if ok else "ABWEICHUNG", detail])
