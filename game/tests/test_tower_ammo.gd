extends TestSuite
## The tower's ammunition economy (decisions E2 and of 2026-11-26; refill by word value since 2026-11-28).


func test_a_tower_starts_with_sixteen_shots_and_a_wrong_word_costs_three() -> void:
	var ammo := TowerAmmo.new()
	equal(16, ammo.count)
	ammo.penalize()
	equal(13, ammo.count)


func test_a_correct_word_adds_eight_shots_plus_two_per_value_point() -> void:
	equal(10, TowerAmmo.refill_size(1))
	equal(12, TowerAmmo.refill_size(2))
	equal(14, TowerAmmo.refill_size(3))
	var ammo := TowerAmmo.new()
	ammo.refill(3)
	equal(30, ammo.count)
	ammo.refill(1)
	equal(40, ammo.count)
	equal(2, ammo.refills)


func test_a_penalty_never_drives_the_ammo_below_zero() -> void:
	var ammo := TowerAmmo.new()
	for i: int in 15:
		ammo.use()
	ammo.penalize()
	equal(0, ammo.count)


func test_more_than_thirty_shots_make_a_tower_strong_and_the_turn_is_reported_once() -> void:
	var ammo := TowerAmmo.new()
	check(not ammo.refill(3), "30 shots not to turn the tower strong")
	check(ammo.refill(1), "40 shots to turn the tower strong")
	check(ammo.is_strong(), "the tower to be strong")
	check(not ammo.refill(1), "an already strong tower not to report the turn again")


func test_five_refills_keep_a_tower_strong_even_when_its_ammo_runs_low() -> void:
	var ammo := TowerAmmo.new()
	for i: int in 5:
		ammo.refill(1)
		for k: int in 10:
			ammo.use()
	equal(16, ammo.count)
	check(ammo.is_strong(), "five refills to make the tower strong")


func test_a_shot_reports_the_strength_it_was_fired_with() -> void:
	var ammo := TowerAmmo.new()
	ammo.refill(3)
	ammo.refill(-2)  # 16 + 14 + 4 = 34: strong
	for i: int in 3:
		check(ammo.use(), "shots 34, 33, 32 to be strong")
	check(ammo.use(), "shot 31 to be strong: more than thirty when fired")
	check(not ammo.use(), "shot 30 to be plain: no longer more than thirty")


func test_three_shots_or_fewer_count_as_low() -> void:
	var ammo := TowerAmmo.new()
	for i: int in 12:
		ammo.use()
	check(not ammo.is_low(), "4 shots not to be low")
	ammo.use()
	check(ammo.is_low(), "3 shots to be low")
