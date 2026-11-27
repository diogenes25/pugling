extends TestSuite
## The client's count of a run and its log line.


func test_only_correct_words_count_towards_the_goal() -> void:
	var tally := RunTally.new(2)
	tally.assigned(false)
	tally.assigned(true)
	check(not tally.goal_reached(), "one correct word of two not to reach the goal")
	tally.assigned(true)
	check(tally.goal_reached(), "two correct words to reach the goal")
	equal(3, tally.total)


func test_the_castle_falls_with_the_tenth_skeleton() -> void:
	var tally := RunTally.new(10)
	for i: int in 9:
		check(not tally.castle_hit(), "the castle to stand after skeleton %d" % (i + 1))
	check(tally.castle_hit(), "the castle to fall with the tenth skeleton")


func test_the_summary_keeps_the_words_the_run_reports_are_read_by() -> void:
	var tally := RunTally.new(10)
	tally.elapsed = 97.4
	tally.assigned(true)
	tally.assigned(false)
	tally.castle_hit()
	tally.hero_shot()
	tally.hero_shot()
	tally.hero_kill()
	equal("ende GoalReached 1/2 richtig, 97s, Burg 9/10, Heldenschüsse 2, Heldentreffer 1", tally.summary("GoalReached"))
