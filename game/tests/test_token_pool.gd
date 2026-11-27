extends TestSuite
## The run's words between pool, field and hand (B-822 E2).


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


func test_a_consumed_token_never_drops_again() -> void:
	var pool := TokenPool.new(["a"] as Array[String])
	var id := pool.draw()
	pool.consume(id)
	pool.give_back(id)
	equal("", pool.draw())


func test_returning_a_token_twice_does_not_double_it() -> void:
	var pool := TokenPool.new(["a"] as Array[String])
	var id := pool.draw()
	pool.give_back(id)
	pool.give_back(id)
	equal(id, pool.draw())
	equal("", pool.draw())
