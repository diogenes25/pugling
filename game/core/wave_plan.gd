class_name WavePlan
extends RefCounted
## The endless run as attack waves: which enemy comes when. A wave is a fixed troop that grows with its number -
## small ones for points, carriers that drop a word, and a boss every [constant Balance.BOSS_EVERY]-th wave. The next
## wave starts a short rest after the last enemy of the current one is gone, so a cleared wave is a moment to cheer.


## One enemy to put on the map: what it is, which path it takes, how fast it walks and how much it takes.
class Spawn:
	extends RefCounted

	## The enemy kind.
	var kind: EnemyKind

	## Index into the map's paths.
	var path: int

	## Walking speed in units per second.
	var speed: float

	## Hit points, grown with the wave.
	var hp: int


## The running wave, 1-based; 0 before the first one.
var wave: int = 0

## Enemies spawned so far, over all waves.
var spawned: int = 0

## The special kinds; a plan without one of them simply never sends it.
var runner: EnemyKind
var shield: EnemyKind
var healer: EnemyKind

var _interval: float
var _speed: float
var _paths: int
var _minion: EnemyKind
var _carrier: EnemyKind
var _boss: EnemyKind
var _queue: Array[EnemyKind] = []
var _timer: float = 0.0
var _rest: float = Balance.FIRST_SPAWN_DELAY
var _started: int = 0
var _cleared: int = 0
var _cleared_note: int = 0


## [param interval] and [param speed] are the map's start values, [param paths] its number of paths. [param minion]
## is the small enemy, [param carrier] the one that drops a word, [param boss] closes every fifth wave.
func _init(interval: float, speed: float, paths: int, minion: EnemyKind, carrier: EnemyKind, boss: EnemyKind) -> void:
	_interval = interval
	_speed = speed
	_paths = paths
	_minion = minion
	_carrier = carrier
	_boss = boss


@warning_ignore("integer_division")
## The troop of a wave in marching order: carriers spread evenly among the minions, the boss last.
func troop(number: int) -> Array[EnemyKind]:
	var minions := mini(Balance.MAX_MINIONS, Balance.WAVE_MINIONS + Balance.WAVE_MINIONS_GROWTH * (number - 1))
	var carriers := mini(Balance.MAX_CARRIERS, Balance.WAVE_CARRIERS + (number - 1) / 2)
	var result: Array[EnemyKind] = []
	var every := maxi(1, (minions + carriers) / carriers)
	var carriers_left := carriers
	var minions_left := minions
	while carriers_left + minions_left > 0:
		var carrier_turn := (result.size() + 1) % every == 0 or minions_left == 0
		if carrier_turn and carriers_left > 0:
			result.append(_carrier)
			carriers_left -= 1
		else:
			result.append(_minion)
			minions_left -= 1
	_send_specials(result, number)
	if number % Balance.BOSS_EVERY == 0:
		result.append(_boss)
	return result


## Swaps some of a troop's enemies for the special kinds and puts the healers in its middle; the troop keeps its
## number of small enemies and of word carriers.
@warning_ignore("integer_division")
func _send_specials(result: Array[EnemyKind], number: int) -> void:
	var minions_seen := 0
	var carriers_seen := 0
	for i: int in result.size():
		if result[i] == _minion:
			minions_seen += 1
			if runner != null and number >= Balance.RUNNER_FROM and minions_seen % Balance.RUNNER_EVERY == 0:
				result[i] = runner
		elif result[i] == _carrier:
			carriers_seen += 1
			if shield != null and number >= Balance.SHIELD_FROM and carriers_seen % Balance.SHIELD_EVERY == 0:
				result[i] = shield
	if healer == null or number < Balance.HEALER_FROM:
		return
	var healers := mini(Balance.MAX_HEALERS, 1 + (number - Balance.HEALER_FROM) / Balance.HEALER_MORE_EVERY)
	for i: int in healers:
		result.insert(result.size() / 2, healer)


## Advances the plan by [param dt]; [param alive] is the number of enemies still on the map. Returns the enemy to
## spawn now, or null.
func tick(dt: float, alive: int) -> Spawn:
	if _queue.is_empty():
		if alive > 0:
			return null
		if wave > _cleared:
			_cleared = wave
			_cleared_note = wave
			_rest = Balance.WAVE_REST
			return null
		_rest -= dt
		if _rest > 0.0:
			return null
		wave += 1
		_started = wave
		_queue = troop(wave)
		_timer = 0.0
	_timer -= dt
	if _timer > 0.0:
		return null
	_timer = maxf(_interval * Balance.WAVE_INTERVAL_FLOOR,
			_interval * (Balance.WAVE_INTERVAL_START - Balance.WAVE_INTERVAL_RAMP * (wave - 1)))
	spawned += 1
	var spawn := Spawn.new()
	spawn.kind = _queue.pop_front()
	spawn.path = spawned % _paths
	spawn.speed = (_speed + minf(Balance.MAX_SPEED_RAMP, Balance.WAVE_SPEED_RAMP * (wave - 1))) * spawn.kind.speed_factor
	spawn.hp = spawn.kind.hp + int(spawn.kind.hp_per_wave * (wave - 1))
	return spawn


## The wave that just started, once; 0 otherwise.
func take_started() -> int:
	var number := _started
	_started = 0
	return number


## The wave whose last enemy just went, once; 0 otherwise.
func take_cleared() -> int:
	var number := _cleared_note
	_cleared_note = 0
	return number
