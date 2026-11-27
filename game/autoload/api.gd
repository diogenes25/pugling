extends TowerServer
## The game's thin HTTP layer against the Pugling API: child login and the tower-game endpoints (B-851). The server
## rejects unknown fields with 400, so every body carries exactly the contract's field names.

const TIMEOUT_SECONDS: float = 10.0

var _root: String = ""
var _token: String = ""


func login(server: String, child: int, pin: String) -> ApiResult:
	_root = server.strip_edges().trim_suffix("/") + "/api/v1/"
	_token = ""
	var result := await _send(HTTPClient.METHOD_POST, "auth/child", {"childId": child, "pin": pin})
	if result.ok:
		_token = str(result.data.get("token", ""))
		child_id = child
		child_name = str(result.data.get("name", ""))
	return result


func overview() -> ApiResult:
	return await _send(HTTPClient.METHOD_GET, _game())


func home() -> ApiResult:
	var wallet := await _send(HTTPClient.METHOD_GET, "student/me/points")
	var today := await _send(HTTPClient.METHOD_GET, "student/children/%d/today" % child_id)
	var missions := await _send(HTTPClient.METHOD_GET, "student/me/missions")
	if not wallet.ok and not today.ok and not missions.ok:
		return wallet
	return ApiResult.success({"wallet": wallet.data, "today": today.data, "missions": missions.data.get("items", [])})


func start_run(plan_position_id: int) -> ApiResult:
	return await _send(HTTPClient.METHOD_POST, _game() + "/runs", {"planPositionId": plan_position_id})


func assign(run_id: int, token_id: String, tower_id: String) -> ApiResult:
	return await _send(HTTPClient.METHOD_POST, _game() + "/assign",
			{"runId": run_id, "tokenId": token_id, "towerId": tower_id})


func heartbeat(run_id: int) -> ApiResult:
	return await _send(HTTPClient.METHOD_POST, "%s/runs/%d/heartbeat" % [_game(), run_id])


func end_run(run_id: int) -> ApiResult:
	return await _send(HTTPClient.METHOD_POST, "%s/runs/%d/end" % [_game(), run_id])


func _game() -> String:
	return "student/children/%d/tower-game" % child_id


## One request per call on its own node, so a heartbeat and an assignment can be under way at the same time.
func _send(method: HTTPClient.Method, path: String, body: Dictionary = {}) -> ApiResult:
	var http := HTTPRequest.new()
	http.timeout = TIMEOUT_SECONDS
	add_child(http)
	var headers := PackedStringArray(["Accept: application/json"])
	if _token != "":
		headers.append("Authorization: Bearer " + _token)
	var payload := ""
	if not body.is_empty():
		headers.append("Content-Type: application/json")
		payload = JSON.stringify(body)
	var started := http.request(_root + path, headers, method, payload)
	if started != OK:
		http.queue_free()
		return ApiResult.failure("Server nicht erreichbar (%s)." % error_string(started))
	var answer: Array = await http.request_completed
	http.queue_free()
	var outcome: int = answer[0]
	var status: int = answer[1]
	var raw: PackedByteArray = answer[3]
	var text := raw.get_string_from_utf8()
	if outcome != HTTPRequest.RESULT_SUCCESS:
		return ApiResult.failure("Server nicht erreichbar (%s)." % _outcome_name(outcome))
	if status < 200 or status >= 300:
		return _problem(status, text)
	# A 200 that is not the expected JSON (wrong host, a router page) must reach the screen like any failure.
	var json: Variant = JSON.parse_string(text)
	if json is Dictionary:
		return ApiResult.success(json as Dictionary)
	# A list endpoint answers with a bare array; it travels as "items".
	if json is Array:
		return ApiResult.success({"items": json})
	return ApiResult.failure("Unerwartete Antwort vom Server.")


## The ProblemDetails body carries a stable code; only the 409 cases need telling apart.
static func _problem(status: int, text: String) -> ApiResult:
	var closed := text.contains("tower_game_run_closed")
	match status:
		401:
			return ApiResult.failure("PIN falsch oder Anmeldung abgelaufen.")
		404:
			return ApiResult.failure("Nicht gefunden – ist der Plan aktiv?")
		409:
			if text.contains("tower_game_time_limit"):
				return ApiResult.failure("Deine Spielzeit für heute ist um. Morgen geht's weiter!")
			if closed:
				return ApiResult.failure("Das Level ist vorbei.", true)
			return ApiResult.failure("Gerade kam etwas dazwischen – bitte nochmal.")
		429:
			return ApiResult.failure("Zu viele Versuche, kurz warten.")
	return ApiResult.failure("Fehler vom Server (HTTP %d)." % status)


static func _outcome_name(outcome: int) -> String:
	match outcome:
		HTTPRequest.RESULT_CANT_CONNECT:
			return "keine Verbindung"
		HTTPRequest.RESULT_CANT_RESOLVE:
			return "Adresse unbekannt"
		HTTPRequest.RESULT_TIMEOUT:
			return "Zeitüberschreitung"
		HTTPRequest.RESULT_CONNECTION_ERROR, HTTPRequest.RESULT_NO_RESPONSE:
			return "Verbindung abgebrochen"
	return "Fehler %d" % outcome
