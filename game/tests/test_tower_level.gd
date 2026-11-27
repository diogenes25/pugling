extends TestSuite
## A tower's level over an endless run.


func test_a_tower_gains_its_second_level_at_four_value_points() -> void:
	var level := TowerLevel.new()
	check(not level.add(3), "three points not to lift the tower")
	equal(1, level.level)
	check(level.add(1), "the fourth point to lift the tower")
	equal(2, level.level)
	equal(2, level.words)


func test_every_level_costs_one_step_more_than_the_one_before() -> void:
	equal(4, TowerLevel.needed(1))
	equal(12, TowerLevel.needed(2))
	equal(24, TowerLevel.needed(3))
	var level := TowerLevel.new()
	level.add(11)
	equal(2, level.level)
	level.add(1)
	equal(3, level.level)


func test_a_new_word_lifts_a_tower_faster_than_a_well_learned_one() -> void:
	var fed_new := TowerLevel.new()
	var fed_known := TowerLevel.new()
	for i: int in 4:
		fed_new.add(3)
		fed_known.add(1)
	equal(3, fed_new.level)
	equal(2, fed_known.level)


func test_one_big_word_can_lift_a_tower_two_levels_and_reports_it_once() -> void:
	var level := TowerLevel.new()
	check(level.add(12), "twelve points to lift the tower")
	equal(3, level.level)


func test_every_level_adds_one_damage() -> void:
	var level := TowerLevel.new()
	equal(0, level.damage_bonus())
	level.add(12)
	equal(2, level.damage_bonus())


func test_the_progress_runs_from_the_last_threshold_to_the_next() -> void:
	var level := TowerLevel.new()
	level.add(2)
	near(0.5, level.progress())
	level.add(6)  # 8 of the way from 4 to 12
	near(0.5, level.progress())
