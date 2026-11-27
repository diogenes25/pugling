extends TestSuite
## The balance of an endless run as arithmetic on the smallest map (three towers): a run fed with every word its
## carriers drop keeps its towers stocked for many waves, and a run without words runs dry in the second wave. The
## model is on the cautious side: every tower shoots its plain projectile of one damage, a tenth of the shots is
## wasted on enemies that already fall, and the hero's own shot does not count.

const MINION: EnemyKind = preload("res://data/minion.tres")
const WARRIOR: EnemyKind = preload("res://data/warrior.tres")
const BOSS: EnemyKind = preload("res://data/boss.tres")
const TOWERS: int = 3


## Plays [param waves] waves; returns the first wave in which the towers ran dry, or 0. With [param fed] every
## dropped word reaches a tower as a correct word of middle value, in turn.
func _first_dry_wave(waves: int, fed: bool) -> int:
	var plan := WavePlan.new(5.0, 1.6, 1, MINION, WARRIOR, BOSS)
	var ammo: Array[TowerAmmo] = []
	var level: Array[TowerLevel] = []
	for i: int in TOWERS:
		ammo.append(TowerAmmo.new())
		level.append(TowerLevel.new())
	var next := 0
	for number: int in range(1, waves + 1):
		var words := 0
		for kind: EnemyKind in plan.troop(number):
			var hp := kind.hp + int(kind.hp_per_wave * (number - 1))
			while hp > 0:
				var tower := next % TOWERS
				next += 1
				if ammo[tower].count <= 0:
					return number
				ammo[tower].use()
				hp -= 1 + level[tower].damage_bonus()
			words += kind.drops
		# A tenth of a wave's shots hits an enemy that is already falling.
		var wasted := plan.troop(number).size() / 10 + 1
		for i: int in wasted:
			ammo[i % TOWERS].count = maxi(0, ammo[i % TOWERS].count - 1)
		if fed:
			for i: int in words:
				ammo[i % TOWERS].refill(Balance.DEFAULT_WORD_VALUE)
				level[i % TOWERS].add(Balance.DEFAULT_WORD_VALUE)
	return 0


func test_a_run_fed_with_every_word_keeps_its_towers_stocked_for_twenty_waves() -> void:
	equal(0, _first_dry_wave(20, true))


func test_a_run_without_words_clears_the_first_wave_and_runs_dry_in_the_second() -> void:
	equal(2, _first_dry_wave(20, false))


func test_the_words_of_the_first_wave_outweigh_the_shots_of_the_second() -> void:
	var plan := WavePlan.new(5.0, 1.6, 1, MINION, WARRIOR, BOSS)
	var supply := 0
	for kind: EnemyKind in plan.troop(1):
		supply += kind.drops * TowerAmmo.refill_size(1)
	var demand := 0
	for kind: EnemyKind in plan.troop(2):
		demand += kind.hp + int(kind.hp_per_wave)
	check(supply >= demand, "the first wave's words (%d shots, even well learned ones) to cover the second wave (%d hits)" % [supply, demand])
