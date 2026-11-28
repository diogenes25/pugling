extends TestSuite
## The hero's lasting level from the trophies the server counted.


func test_every_hero_level_costs_one_step_more_than_the_one_before() -> void:
	equal(0, HeroLevel.needed(1))
	equal(30, HeroLevel.needed(2))
	equal(90, HeroLevel.needed(3))
	equal(180, HeroLevel.needed(4))


func test_the_level_follows_the_trophies() -> void:
	equal(1, HeroLevel.of(0))
	equal(1, HeroLevel.of(29))
	equal(2, HeroLevel.of(30))
	equal(2, HeroLevel.of(89))
	equal(3, HeroLevel.of(90))
	equal(5, HeroLevel.of(300))


func test_the_way_to_the_next_level_runs_from_zero_to_one() -> void:
	near(0.0, HeroLevel.progress(0))
	near(0.5, HeroLevel.progress(15))
	near(0.0, HeroLevel.progress(30))
	near(0.5, HeroLevel.progress(60))


func test_a_higher_level_shoots_a_little_quicker_down_to_a_floor() -> void:
	near(1.0, HeroLevel.shot_interval(1))
	near(0.95, HeroLevel.shot_interval(2))
	near(0.8, HeroLevel.shot_interval(5))
	near(0.5, HeroLevel.shot_interval(11))
	near(0.5, HeroLevel.shot_interval(40))


func test_every_fourth_level_adds_one_damage_to_the_heros_shot() -> void:
	equal(0, HeroLevel.damage_bonus(1))
	equal(0, HeroLevel.damage_bonus(4))
	equal(1, HeroLevel.damage_bonus(5))
	equal(2, HeroLevel.damage_bonus(9))
