extends TestSuite
## The trophy path on the start screen: the next step and the way to it.


func _path() -> TrophyPath:
	var path := TrophyPath.new()
	# Added out of order: the path sorts itself.
	path.add(60, 2)
	path.add(20, 1)
	path.add(120, 3)
	return path


func test_the_next_step_is_the_first_one_not_reached_yet() -> void:
	var path := _path()
	equal(20, path.next(0).trophies)
	equal(20, path.next(19).trophies)
	equal(60, path.next(20).trophies)
	equal(2, path.next(20).gems)
	equal(120, path.next(119).trophies)
	check(path.next(120) == null, "no next step once the path is done")
	check(TrophyPath.new().next(5) == null, "no next step on an empty path")


func test_reached_counts_the_steps_at_or_below_the_total() -> void:
	var path := _path()
	equal(0, path.reached(19))
	equal(1, path.reached(20))
	equal(2, path.reached(119))
	equal(3, path.reached(500))


func test_the_progress_runs_from_the_last_reached_step_to_the_next() -> void:
	var path := _path()
	near(0.5, path.progress(10))
	near(0.0, path.progress(20))
	near(0.25, path.progress(30))
	near(0.5, path.progress(90))
	near(1.0, path.progress(120))
	near(1.0, TrophyPath.new().progress(7))
