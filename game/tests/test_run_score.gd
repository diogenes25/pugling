extends TestSuite
## The points of an endless run.

const MINION: EnemyKind = preload("res://data/minion.tres")
const WARRIOR: EnemyKind = preload("res://data/warrior.tres")
const BOSS: EnemyKind = preload("res://data/boss.tres")


func test_a_kill_pays_what_the_enemy_is_worth() -> void:
	var score := RunScore.new()
	score.kill(MINION)
	equal(10, score.total())
	score.kill(WARRIOR)
	equal(40, score.total())
	score.kill(BOSS)
	equal(340, score.total())
	equal(3, score.kills)


func test_a_new_word_pays_three_times_a_well_learned_one() -> void:
	var score := RunScore.new()
	score.word(1)
	equal(50, score.total())
	score.word(3)
	equal(200, score.total())
	equal(2, score.words)


func test_a_tower_level_pays_a_hundred_times_the_level() -> void:
	var score := RunScore.new()
	score.tower_level(2)
	score.tower_level(3)
	equal(500, score.total())
	equal(2, score.tower_levels)


func test_a_cleared_wave_pays_a_hundred_times_its_number_and_reports_the_bonus() -> void:
	var score := RunScore.new()
	equal(100, score.wave(1))
	equal(200, score.wave(2))
	equal(300, score.total())
	equal(2, score.waves)


func test_the_summary_lines_add_up_to_the_total() -> void:
	var score := RunScore.new()
	score.kill(MINION)
	score.word(2)
	score.tower_level(2)
	score.wave(1)
	var sum := 0
	var counts: Array[int] = []
	for line: RunScore.Line in score.lines():
		sum += line.points
		counts.append(line.count)
	equal(score.total(), sum)
	equal(410, sum)
	equal([1, 1, 1, 1] as Array[int], counts)
