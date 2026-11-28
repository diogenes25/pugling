extends Node
## Contract check of all calls against a real (throwaway) API - the server rejects unknown fields, so a wrong
## field name shows up here as a 400. Never point it at the home server: it starts and ends a real run.
## Run: godot --headless --path . res://tests/smoke_api.tscn -- --server=http://127.0.0.1:5281 --child=3 --pin=4711
## Add --rate-limit to also run into the login limiter (blocks logins for a minute), or --time-limit when the
## copy has today's play time used up.
## Exit code 0 when every call answered as the contract says.

var _failed: int = 0


func _ready() -> void:
	get_tree().create_timer(60.0, true, false, true).timeout.connect(get_tree().quit.bind(1))
	var server := UiDriver.arg("server")
	var nowhere := await Api.login("http://127.0.0.1:9", 3, "0000")
	_note("kein Server", not nowhere.ok and nowhere.error.begins_with("Server nicht erreichbar ("), nowhere.error)
	var wrong := await Api.login(server, UiDriver.arg("child", "3").to_int(), "0000")
	_note("login falsche PIN", not wrong.ok and wrong.error == "PIN falsch oder Anmeldung abgelaufen.", wrong.error)
	var login := await Api.login(server, UiDriver.arg("child", "3").to_int(), UiDriver.arg("pin"))
	_note("login", login.ok and Api.child_name != "", Api.child_name + login.error)
	var overview := await Api.overview()
	var positions: Array = overview.data.get("positions", [])
	_note("overview", overview.ok and not positions.is_empty() and overview.data.has("limitSeconds"),
			"%d Positionen %s" % [positions.size(), overview.error])
	var day := await Api.home()
	var wallet: Dictionary = day.data.get("wallet", {})
	var today: Dictionary = day.data.get("today", {})
	_note("home", day.ok and wallet.has("coins") and wallet.has("gems") and today.has("goalsTotal") and today.has("currentStreak")
			and today.has("dailyBox") and day.data.get("missions") is Array,
			"Münzen %s, Gems %s, Ziele %s/%s, Serie %s, %d Missionen %s" % [wallet.get("coins"), wallet.get("gems"),
			today.get("goalsMet"), today.get("goalsTotal"), today.get("currentStreak"), (day.data.get("missions", []) as Array).size(), day.error])
	if positions.is_empty():
		get_tree().quit(1)
		return
	var first: Dictionary = positions[0]
	var started := await Api.start_run(int(first["planPositionId"]))
	if UiDriver.arg("time-limit") != "":
		# Staged on the copy: today's play time is used up, so no run may start.
		_note("start_run nach dem Zeitlimit", not started.ok and started.error == "Deine Spielzeit für heute ist um. Morgen geht's weiter!", started.error)
		get_tree().quit(1 if _failed > 0 else 0)
		return
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
	var end := await Api.end_run(run.run_id, 7)
	# One correct word and a few seconds of play: the reported wave 7 must come back cut to what the time covers.
	_note("end_run", end.ok and end.data.has("won") and end.data.has("coinsAwarded") and end.data.has("trophies")
			and int(end.data.get("trophiesAwarded", 0)) >= 1 and int(end.data.get("wave", 9)) <= 2, str(end.data) + end.error)
	var after := GameProgress.from_json((await Api.overview()).data)
	_note("overview progress", after.known and after.trophies == int(end.data.get("trophies", -1)) and not after.path.steps.is_empty()
			and after.streak_words > 0 and after.chest_needed > 0,
			"Trophäen %d, %d Pfad-Stufen, Serie %d, heute %d/%d, Truhe %d/%d" % [after.trophies, after.path.steps.size(), after.streak,
			after.words_today, after.streak_words, after.chest_words, after.chest_needed])
	var chest := await Api.open_game_chest()
	_note("open_game_chest", chest.ok == after.chest_ready and (chest.ok or chest.error == "Hier ist gerade keine Truhe zu öffnen."),
			str(chest.data) + chest.error)
	var box := await Api.open_daily_chest()
	_note("open_daily_chest", (box.ok and box.data.has("coins") and box.data.has("gems"))
			or box.error == "Hier ist gerade keine Truhe zu öffnen.", str(box.data) + box.error)
	var late := await Api.assign(run.run_id, run.tokens[0].token_id, run.towers[0].tower_id)
	_note("assign nach dem Ende", not late.ok and late.run_closed and late.error == "Das Level ist vorbei.", late.error)
	var missing := await Api.start_run(999999)
	_note("start_run unbekannte Position", not missing.ok and missing.error == "Nicht gefunden – ist der Plan aktiv?", missing.error)
	if UiDriver.arg("rate-limit") != "":
		# The server slows the PIN login down; afterwards it refuses logins for about a minute.
		var limited := ""
		for i: int in 15:
			limited = (await Api.login(server, 3, "0000")).error
			if limited != "PIN falsch oder Anmeldung abgelaufen.":
				break
		_note("login gebremst", limited == "Zu viele Versuche, kurz warten.", limited)
	get_tree().quit(1 if _failed > 0 else 0)


func _note(what: String, ok: bool, detail: String) -> void:
	if not ok:
		_failed += 1
	Log.line("api %s: %s (%s)" % [what, "ok" if ok else "ABWEICHUNG", detail])
