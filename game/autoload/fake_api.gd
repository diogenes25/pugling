class_name FakeApi
extends TowerServer
## A server from memory with the real one's answers, for headless smoke runs and tests. It grades every second word
## as correct (first wrong, then right), so one run shows both outcomes; the switches below stage the rare endings.

## Correct words a run needs.
var goal: int = 3

## Words (and tower meanings) a run carries.
var word_count: int = 12

## The heartbeat with this number reports that today's play time is used up (0: never).
var time_up_at_beat: int = 0

## Assignments from this number on are refused because the run is closed (0: never).
var closed_at_assign: int = 0

## This many calls of [method end_run] fail before one answers.
var failing_ends: int = 0

## This many calls of [method overview] fail as an expired session before one answers.
var expired_overviews: int = 0

## Logins that worked.
var logins: int = 0

## Coins a won level pays, up to the daily cap.
var coins_per_win: int = 5

## Daily chests waiting to be opened.
var chests_waiting: int = 1

## Correct words in the game today, as the game chest and the game streak count them.
var words_today: int = 15

## Lifetime trophies.
var trophies: int = 48

## The wave the last ended run reported.
var reported_wave: int = -1

var _game_chest_opened: bool = false
var _gems: int = 7
var _assigns: int = 0
var _beats: int = 0
var _solved: int = 0
var _coins_today: int = 0
var _played_seconds: int = 120


func login(_server: String, child: int, pin: String) -> ApiResult:
	await _hop()
	if pin != "4711":
		return ApiResult.failure("PIN falsch oder Anmeldung abgelaufen.")
	child_id = child
	child_name = "Testkind"
	logins += 1
	return ApiResult.success({"token": "fake", "role": "Student", "id": child, "name": child_name})


func overview() -> ApiResult:
	await _hop()
	if expired_overviews > 0:
		expired_overviews -= 1
		return ApiResult.failure("PIN falsch oder Anmeldung abgelaufen.")
	return ApiResult.success({
		"positions": [
			{"planPositionId": 1.0, "planTitle": "Testplan", "subjectName": "Englisch", "exerciseTitle": "Unit 1 · Wörter", "wordCount": float(word_count)},
			{"planPositionId": 2.0, "planTitle": "Testplan", "subjectName": null, "exerciseTitle": "Ohne Fach", "wordCount": float(word_count)},
			{"planPositionId": 3.0, "planTitle": "Testplan", "subjectName": "Französisch", "exerciseTitle": "Unité 5 · Vocabulaire Volet 2 – Texte A und B", "wordCount": float(word_count)},
		],
		"playedSecondsToday": float(_played_seconds),
		"limitSeconds": 1800.0,
		"coinsToday": float(_coins_today),
		"dailyCoinCap": 10.0,
		"progress": {
			"trophies": float(trophies),
			"trophyPath": [
				{"trophies": 20.0, "gems": 1.0, "reached": trophies >= 20},
				{"trophies": 60.0, "gems": 2.0, "reached": trophies >= 60},
				{"trophies": 120.0, "gems": 3.0, "reached": trophies >= 120},
			],
			"streak": 2.0, "wordsToday": float(words_today), "streakWords": 10.0,
			"chest": _game_chest(),
		},
	})


func home() -> ApiResult:
	await _hop()
	return ApiResult.success({
		"wallet": {"childId": float(child_id), "coins": 120.0, "gems": float(_gems)},
		"today": {
			"goalsTotal": 4.0, "goalsMet": 1.0, "currentStreak": 3.0, "dutyDone": false,
			"outstanding": ["Unit 1 · Wörter Part A (Tagesziel) offen", "Unité 5 · Vokabeln (Wochenziel) offen",
				"Grammatik: simple past (Tagesziel) offen"],
			"dailyBox": {"claimedToday": false, "openedToday": false, "waiting": float(chests_waiting), "streakAtClaim": 0.0},
		},
		"missions": [
			{"id": 1.0, "title": "Fertig-Mission", "target": 5.0, "current": 5.0, "completed": true, "rewardPoints": 10.0},
			{"id": 2.0, "title": "20 Vokabeln richtig", "target": 20.0, "current": 8.0, "completed": false, "rewardPoints": 25.0},
		],
	})


func start_run(plan_position_id: int) -> ApiResult:
	await _hop()
	_assigns = 0
	_beats = 0
	_solved = 0
	var towers: Array[Dictionary] = []
	var tokens: Array[Dictionary] = []
	for i: int in range(1, word_count + 1):
		towers.append({"towerId": "T%d" % i, "prompt": "Bedeutung %d" % i, "value": float(i % 3 + 1)})
		tokens.append({"tokenId": "t%d" % i, "word": "word%d" % i})
	return ApiResult.success({
		"runId": 1.0, "planPositionId": float(plan_position_id), "goal": float(goal), "towerSlots": 6.0,
		"towers": towers, "tokens": tokens,
	})


func assign(_run_id: int, _token_id: String, _tower_id: String) -> ApiResult:
	await _hop()
	_assigns += 1
	if closed_at_assign > 0 and _assigns >= closed_at_assign:
		return ApiResult.failure("Das Level ist vorbei.", true)
	var correct := _assigns % 2 == 0
	if correct:
		_solved += 1
	return ApiResult.success({"correct": correct, "expected": "x"})


func heartbeat(_run_id: int) -> ApiResult:
	await _hop()
	_beats += 1
	_played_seconds += 15
	return ApiResult.success({
		"playedSecondsToday": float(_played_seconds), "limitSeconds": 1800.0,
		"timeUp": time_up_at_beat > 0 and _beats >= time_up_at_beat,
	})


func end_run(_run_id: int, wave: int) -> ApiResult:
	await _hop()
	if failing_ends > 0:
		failing_ends -= 1
		return ApiResult.failure("Server nicht erreichbar (keine Verbindung).")
	reported_wave = wave
	var won := _solved >= goal
	var awarded := mini(coins_per_win, 10 - _coins_today) if won else 0
	_coins_today += awarded
	# Like the server: three trophies per solved word and one per cleared wave, never more waves than words.
	var earned := 3 * _solved + mini(maxi(0, wave - 1), _solved)
	var gems := 2 if trophies < 60 and trophies + earned >= 60 else 0
	trophies += earned
	_gems += gems
	return ApiResult.success({
		"won": won, "solved": float(_solved), "goal": float(goal), "coinsAwarded": float(awarded),
		"coinsToday": float(_coins_today), "dailyCoinCap": 10.0,
		"wave": float(wave), "trophiesAwarded": float(earned), "trophies": float(trophies), "gemsAwarded": float(gems),
	})


func open_daily_chest() -> ApiResult:
	await _hop()
	if chests_waiting <= 0:
		return ApiResult.failure("Hier ist gerade keine Truhe zu öffnen.")
	chests_waiting -= 1
	_gems += 1
	return ApiResult.success({"coins": 20.0, "gems": 1.0, "streakAtClaim": 3.0, "day": "2026-11-27", "waitingAfter": float(chests_waiting)})


func open_game_chest() -> ApiResult:
	await _hop()
	if _game_chest_opened or words_today < 15:
		return ApiResult.failure("Hier ist gerade keine Truhe zu öffnen.")
	_game_chest_opened = true
	_gems += 2
	return ApiResult.success({"gemsAwarded": 2.0, "chest": _game_chest()})


func _game_chest() -> Dictionary:
	return {"words": float(mini(words_today, 15)), "wordsNeeded": 15.0,
		"ready": words_today >= 15 and not _game_chest_opened, "openedToday": _game_chest_opened}


## Heartbeats the current run has received.
func beats() -> int:
	return _beats


## An answer never arrives in the frame that asked for it.
func _hop() -> void:
	await Engine.get_main_loop().process_frame
