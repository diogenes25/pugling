extends TestSuite
## When the skeletons come and what they are.

const MINION: EnemyKind = preload("res://data/minion.tres")
const WARRIOR: EnemyKind = preload("res://data/warrior.tres")


func _schedule(interval: float, speed: float, paths: int) -> SpawnSchedule:
	return SpawnSchedule.new(interval, speed, paths, MINION, WARRIOR)


func test_the_first_skeleton_comes_after_two_seconds() -> void:
	var schedule := _schedule(5.0, 1.6, 1)
	check(schedule.tick(1.9, 1.9) == null, "no skeleton before two seconds")
	check(schedule.tick(0.2, 2.1) != null, "a skeleton after two seconds")


func test_every_fourth_skeleton_is_a_slower_warrior() -> void:
	var schedule := _schedule(5.0, 2.0, 1)
	var spawns: Array[SpawnSchedule.Spawn] = []
	for i: int in 8:
		spawns.append(schedule.tick(10.0, 0.0))
	for i: int in 8:
		var expected := WARRIOR if (i + 1) % 4 == 0 else MINION
		check(spawns[i].kind == expected, "skeleton %d to be a %s" % [i + 1, expected.model])
	near(1.6, spawns[3].speed)
	near(2.0, spawns[0].speed)


func test_several_paths_take_turns() -> void:
	var schedule := _schedule(5.0, 2.0, 2)
	var paths: Array[int] = []
	for i: int in 4:
		paths.append(schedule.tick(10.0, 0.0).path)
	equal([1, 0, 1, 0] as Array[int], paths)


func test_the_interval_shrinks_with_play_time_down_to_its_floor() -> void:
	var schedule := _schedule(5.0, 2.0, 1)
	schedule.tick(2.0, 100.0)  # next interval: 5 - 1.2 = 3.8
	check(schedule.tick(3.7, 100.0) == null, "no skeleton 3.7 s into an interval of 3.8")
	check(schedule.tick(0.2, 100.0) != null, "a skeleton once 3.8 s have passed")
	schedule.tick(10.0, 1000.0)  # 5 - 12 is below the floor of 2.75
	check(schedule.tick(2.7, 1000.0) == null, "no skeleton 2.7 s into the floor interval of 2.75")
	check(schedule.tick(0.1, 1000.0) != null, "a skeleton once the floor interval has passed")


func test_skeletons_speed_up_by_at_most_point_eight() -> void:
	var schedule := _schedule(5.0, 2.0, 1)
	near(2.4, schedule.tick(10.0, 100.0).speed)
	near(2.8, schedule.tick(10.0, 10000.0).speed)
