extends TestSuite
## The attack waves of an endless run: who comes, when, and how strong.

const MINION: EnemyKind = preload("res://data/minion.tres")
const WARRIOR: EnemyKind = preload("res://data/warrior.tres")
const BOSS: EnemyKind = preload("res://data/boss.tres")


func _plan(interval: float, speed: float, paths: int) -> WavePlan:
	return WavePlan.new(interval, speed, paths, MINION, WARRIOR, BOSS)


func _count(troop: Array[EnemyKind], kind: EnemyKind) -> int:
	return troop.count(kind)


## Spawns the rest of the running wave with nobody left alive in between.
func _drain(plan: WavePlan) -> Array[WavePlan.Spawn]:
	var spawns: Array[WavePlan.Spawn] = []
	var next := plan.tick(100.0, 1)
	while next != null:
		spawns.append(next)
		next = plan.tick(100.0, 1)
	return spawns


func test_the_first_wave_starts_after_two_seconds() -> void:
	var plan := _plan(5.0, 1.6, 1)
	check(plan.tick(1.9, 0) == null, "no enemy before two seconds")
	equal(0, plan.take_started())
	check(plan.tick(0.2, 0) != null, "an enemy after two seconds")
	equal(1, plan.take_started())
	equal(0, plan.take_started())


func test_a_wave_grows_by_two_minions_and_a_carrier_every_second_wave() -> void:
	var plan := _plan(5.0, 2.0, 1)
	equal(5, _count(plan.troop(1), MINION))
	equal(4, _count(plan.troop(1), WARRIOR))
	equal(7, _count(plan.troop(2), MINION))
	equal(4, _count(plan.troop(2), WARRIOR))
	equal(9, _count(plan.troop(3), MINION))
	equal(5, _count(plan.troop(3), WARRIOR))
	equal(30, _count(plan.troop(40), MINION))
	equal(10, _count(plan.troop(40), WARRIOR))


func test_carriers_march_spread_among_the_minions() -> void:
	var troop := _plan(5.0, 2.0, 1).troop(1)
	var places: Array[int] = []
	for i: int in troop.size():
		if troop[i] == WARRIOR:
			places.append(i)
	equal([1, 3, 5, 7] as Array[int], places)


func test_every_fifth_wave_ends_with_a_boss() -> void:
	var plan := _plan(5.0, 2.0, 1)
	equal(0, _count(plan.troop(4), BOSS))
	equal(1, _count(plan.troop(5), BOSS))
	check(plan.troop(5).back() == BOSS, "the boss to march last")
	equal(1, _count(plan.troop(10), BOSS))


func test_the_next_wave_waits_until_the_field_is_clear_and_rests_five_seconds() -> void:
	var plan := _plan(5.0, 2.0, 1)
	plan.tick(2.0, 0)
	equal(8, _drain(plan).size())
	check(plan.tick(100.0, 1) == null, "no new wave while an enemy is alive")
	equal(0, plan.take_cleared())
	check(plan.tick(0.1, 0) == null, "the cleared wave to be noticed first")
	equal(1, plan.take_cleared())
	equal(0, plan.take_cleared())
	check(plan.tick(4.9, 0) == null, "no enemy 4.9 s into the rest of 5")
	var first := plan.tick(0.2, 0)
	check(first != null, "the next wave after the rest")
	equal(2, plan.wave)
	equal(2, plan.take_started())


func test_enemies_follow_closer_from_wave_to_wave_down_to_a_floor() -> void:
	var plan := _plan(5.0, 2.0, 1)
	plan.tick(2.0, 0)  # wave 1: interval 5 * 0.6 = 3.0
	check(plan.tick(2.9, 1) == null, "no enemy 2.9 s into an interval of 3.0")
	check(plan.tick(0.2, 1) != null, "an enemy once 3.0 s have passed")
	plan.wave = 40  # 0.6 - 39 * 0.03 is below the floor of 0.25: interval 1.25
	plan.tick(100.0, 1)
	check(plan.tick(1.2, 1) == null, "no enemy 1.2 s into the floor interval of 1.25")
	check(plan.tick(0.1, 1) != null, "an enemy once the floor interval has passed")


func test_enemies_get_faster_and_tougher_with_the_wave() -> void:
	var plan := _plan(5.0, 2.0, 1)
	var first := plan.tick(2.0, 0)
	near(2.0, first.speed)
	equal(MINION.hp, first.hp)
	plan.wave = 4
	var later := _drain(plan)  # the rest of the troop marches carrier, minion, carrier ...
	near(2.18 * WARRIOR.speed_factor, later[0].speed)
	equal(WARRIOR.hp + 3, later[0].hp)
	near(2.18, later[1].speed)
	equal(MINION.hp + 1, later[1].hp)
	var late := _plan(5.0, 2.0, 1)
	late.wave = 99
	late.tick(0.0, 0)
	near(2.8, late.tick(5.0, 0).speed)


func test_several_paths_take_turns() -> void:
	var plan := _plan(5.0, 2.0, 2)
	var paths: Array[int] = [plan.tick(2.0, 0).path]
	for spawn: WavePlan.Spawn in _drain(plan).slice(0, 3):
		paths.append(spawn.path)
	equal([1, 0, 1, 0] as Array[int], paths)
