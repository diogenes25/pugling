extends TestSuite
## The run's words between queue, field and hand (B-822 E2), and their order in an endless run.


func _ids(count: int) -> Array[String]:
	var ids: Array[String] = []
	for i: int in count:
		ids.append("t%02d" % i)
	return ids


func test_every_token_drops_once_until_it_comes_back() -> void:
	var random := RandomNumberGenerator.new()
	random.seed = 1
	var pool := TokenPool.new(["a", "b"] as Array[String], random)
	var first := pool.draw()
	var second := pool.draw()
	var both: Array[String] = [first, second]
	both.sort()
	equal(["a", "b"] as Array[String], both)
	equal("", pool.draw())
	pool.give_back(first)
	equal(first, pool.draw())


func test_returning_a_token_twice_does_not_double_it() -> void:
	var pool := TokenPool.new(["a"] as Array[String])
	var id := pool.draw()
	pool.give_back(id)
	pool.give_back(id)
	equal(id, pool.draw())
	equal("", pool.draw())


func test_drops_come_from_the_first_eight_of_the_queue() -> void:
	var random := RandomNumberGenerator.new()
	random.seed = 7
	var pool := TokenPool.new(_ids(40), random)
	for i: int in 30:
		var id := pool.draw()
		check(id <= "t07", "draw %d to come from the first eight, got %s" % [i, id])
		pool.give_back(id)


func test_a_wrongly_given_token_goes_back_to_its_old_place() -> void:
	var pool := TokenPool.new(["a", "b", "c"] as Array[String])
	for i: int in 3:
		pool.draw()
	pool.give_back("b")
	pool.give_back("c")
	pool.give_back("a")
	equal(["a", "b", "c"] as Array[String], pool.order())


func test_a_correctly_given_token_queues_up_again_behind_all_others() -> void:
	var pool := TokenPool.new(["a", "b", "c"] as Array[String])
	for i: int in 3:
		pool.draw()
	pool.consume("a")
	pool.give_back("c")
	pool.give_back("b")
	equal(["b", "c", "a"] as Array[String], pool.order())
