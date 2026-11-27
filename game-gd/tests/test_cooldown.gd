extends TestSuite
## The reload timer.


func test_a_started_cooldown_holds_until_its_time_has_passed() -> void:
	var cooldown := Cooldown.new()
	check(cooldown.tick(0.1), "a fresh cooldown to be ready")
	cooldown.start(1.0)
	check(not cooldown.tick(0.6), "the cooldown to hold after 0.6 of 1 s")
	check(cooldown.tick(0.5), "the cooldown to be over after 1.1 s")
