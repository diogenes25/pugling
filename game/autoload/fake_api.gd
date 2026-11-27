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

## Coins a won level pays, up to the daily cap.
var coins_per_win: int = 5

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
	return ApiResult.success({"token": "fake", "role": "Student", "id": child, "name": child_name})


func overview() -> ApiResult:
	await _hop()
	return ApiResult.success({
		"positions": [
			{"planPositionId": 1.0, "planTitle": "Testplan", "subjectName": "Englisch", "exerciseTitle": "Unit 1 · Wörter", "wordCount": float(word_count)},
			{"planPositionId": 2.0, "planTitle": "Testplan", "subjectName": null, "exerciseTitle": "Ohne Fach", "wordCount": float(word_count)},
		],
		"playedSecondsToday": float(_played_seconds),
		"limitSeconds": 1800.0,
		"coinsToday": float(_coins_today),
		"dailyCoinCap": 10.0,
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


func end_run(_run_id: int) -> ApiResult:
	await _hop()
	if failing_ends > 0:
		failing_ends -= 1
		return ApiResult.failure("Server nicht erreichbar (keine Verbindung).")
	var won := _solved >= goal
	var awarded := mini(coins_per_win, 10 - _coins_today) if won else 0
	_coins_today += awarded
	return ApiResult.success({
		"won": won, "solved": float(_solved), "goal": float(goal), "coinsAwarded": float(awarded),
		"coinsToday": float(_coins_today), "dailyCoinCap": 10.0,
	})


## Heartbeats the current run has received.
func beats() -> int:
	return _beats


## An answer never arrives in the frame that asked for it.
func _hop() -> void:
	await Engine.get_main_loop().process_frame
