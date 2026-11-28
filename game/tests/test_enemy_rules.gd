extends TestSuite
## Armour and healing of the special enemies.


func test_armour_swallows_its_share_of_every_hit_but_never_all_of_it() -> void:
	equal(3, EnemyRules.damage_taken(3, 0))
	equal(2, EnemyRules.damage_taken(3, 1))
	equal(1, EnemyRules.damage_taken(1, 1))
	equal(1, EnemyRules.damage_taken(2, 5))


func test_a_healer_never_mends_beyond_the_hit_points_an_enemy_came_with() -> void:
	equal(4, EnemyRules.healed(3, 6, 1))
	equal(6, EnemyRules.healed(5, 6, 2))
	equal(6, EnemyRules.healed(6, 6, 2))
