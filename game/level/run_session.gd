class_name RunSession
extends RefCounted
## The level's line to the server for one run: the queue of tower meanings, grading a word, and the heartbeat that
## counts the play time. The server keeps which word fits which tower; the client never knows.

## The server says today's play time is used up.
signal time_up

## The run as the server started it.
var run: TowerRun

var _server: TowerServer
var _targets: Array[TowerTarget] = []
var _since_beat: float = 0.0
var _beating: bool = false


func _init(server: TowerServer, started: TowerRun) -> void:
	_server = server
	run = started
	_targets.assign(started.towers)


## The next meaning a tower asks for. The run is endless: a meaning that was handed out queues up again at the end,
## so every word comes back once all are through. Null only for a run without towers.
func next_target() -> TowerTarget:
	if _targets.is_empty():
		return null
	var target: TowerTarget = _targets.pop_front()
	_targets.append(target)
	return target


## Lets the server grade a word at a tower.
func assign(token: TowerToken, target: TowerTarget) -> ApiResult:
	@warning_ignore("redundant_await")
	return await _server.assign(run.run_id, token.token_id, target.tower_id)


## Counts played time and sends a heartbeat every [constant Balance.BEAT_INTERVAL] seconds. Call only while the
## level runs, not while it is paused.
func tick(delta: float) -> void:
	_since_beat += delta
	if _since_beat >= Balance.BEAT_INTERVAL and not _beating:
		_beat()


func _beat() -> void:
	_beating = true
	_since_beat = 0.0
	@warning_ignore("redundant_await")
	var beat := await _server.heartbeat(run.run_id)
	_beating = false
	# A lost beat costs only its seconds; the next one goes out in 15 s.
	if beat.ok and beat.data.get("timeUp", false):
		time_up.emit()
