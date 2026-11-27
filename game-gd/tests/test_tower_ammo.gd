extends TestSuite
## The tower's ammunition economy (decisions E2 and of 2026-11-26).


func test_a_correct_word_adds_eight_shots_and_a_wrong_one_costs_three() -> void:
	var ammo := TowerAmmo.new()
	equal(10, ammo.count)
	ammo.refill()
	equal(18, ammo.count)
	ammo.penalize()
	equal(15, ammo.count)


func test_a_penalty_never_drives_the_ammo_below_zero() -> void:
	var ammo := TowerAmmo.new()
	for i: int in 9:
		ammo.use()
	ammo.penalize()
	equal(0, ammo.count)


func test_more_than_thirty_shots_make_a_tower_strong_and_the_turn_is_reported_once() -> void:
	var ammo := TowerAmmo.new()
	check(not ammo.refill(), "18 shots not to turn the tower strong")
	check(not ammo.refill(), "26 shots not to turn the tower strong")
	check(ammo.refill(), "34 shots to turn the tower strong")
	check(ammo.is_strong(), "the tower to be strong")
	check(not ammo.refill(), "an already strong tower not to report the turn again")


func test_five_refills_keep_a_tower_strong_even_when_its_ammo_runs_low() -> void:
	var ammo := TowerAmmo.new()
	for i: int in 5:
		ammo.refill()
		for k: int in 8:
			ammo.use()
	equal(10, ammo.count)
	check(ammo.is_strong(), "five refills to make the tower strong")


func test_a_shot_reports_the_strength_it_was_fired_with() -> void:
	var ammo := TowerAmmo.new()
	for i: int in 3:
		ammo.refill()  # 34: strong
	for i: int in 3:
		check(ammo.use(), "shots 34, 33, 32 to be strong")
	check(ammo.use(), "shot 31 to be strong: more than thirty when fired")
	check(not ammo.use(), "shot 30 to be plain: no longer more than thirty")


func test_three_shots_or_fewer_count_as_low() -> void:
	var ammo := TowerAmmo.new()
	for i: int in 6:
		ammo.use()
	check(not ammo.is_low(), "4 shots not to be low")
	ammo.use()
	check(ammo.is_low(), "3 shots to be low")
