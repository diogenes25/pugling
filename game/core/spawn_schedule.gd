class_name SpawnSchedule
extends RefCounted
## When the next enemy comes and what it is. The level gets harder the longer it runs: the interval shrinks and the
## skeletons speed up (both capped), every [constant Balance.WARRIOR_EVERY]-th one is a warrior, and several paths
## take turns.


## One enemy to put on the map: what it is, which path it takes, how fast it walks.
class Spawn:
	extends RefCounted

	## The enemy kind.
	var kind: EnemyKind

	## Index into the map's paths.
	var path: int

	## Walking speed in units per second.
	var speed: float


## Enemies spawned so far.
var spawned: int = 0

var _interval: float
var _speed: float
var _paths: int
var _common: EnemyKind
var _warrior: EnemyKind
var _timer: float = Balance.FIRST_SPAWN_DELAY


## [param interval] and [param speed] are the map's start values, [param paths] its number of paths; [param common]
## is the usual enemy and [param warrior] the one that comes every n-th time.
func _init(interval: float, speed: float, paths: int, common: EnemyKind, warrior: EnemyKind) -> void:
	_interval = interval
	_speed = speed
	_paths = paths
	_common = common
	_warrior = warrior


## Advances the timer by [param dt]; returns the enemy to spawn now, or null. [param elapsed] is the run's played
## seconds.
func tick(dt: float, elapsed: float) -> Spawn:
	_timer -= dt
	if _timer > 0.0:
		return null
	_timer = maxf(_interval * Balance.SPAWN_INTERVAL_FLOOR, _interval - elapsed * Balance.SPAWN_INTERVAL_RAMP)
	spawned += 1
	var spawn := Spawn.new()
	spawn.kind = _warrior if spawned % Balance.WARRIOR_EVERY == 0 else _common
	spawn.path = spawned % _paths
	spawn.speed = (_speed + minf(Balance.MAX_SPEED_RAMP, elapsed * Balance.SPEED_RAMP)) * spawn.kind.speed_factor
	return spawn
