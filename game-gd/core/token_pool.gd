class_name TokenPool
extends RefCounted
## Bookkeeping of a run's vocabulary tokens: which are still in the pool, lying on the field or carried, and which
## are used up by a correct assignment. A wrongly assigned token goes back into the pool and drops again later
## (B-822 E2). The client cannot know which token fits which tower - the server keeps that - so drops are random.

var _pool: Array[String] = []
var _out: Dictionary[String, bool] = {}
var _random: RandomNumberGenerator


## [param token_ids] are the run's tokens; a seeded [param random] makes the draws repeatable.
func _init(token_ids: Array[String], random: RandomNumberGenerator = null) -> void:
	_pool.assign(token_ids)
	_random = random if random != null else RandomNumberGenerator.new()


## Takes a random token out of the pool for a drop, or "" when every token is out or used.
func draw() -> String:
	if _pool.is_empty():
		return ""
	var id: String = _pool.pop_at(_random.randi_range(0, _pool.size() - 1))
	_out[id] = true
	return id


## Puts a token back (it expired on the field, or was assigned wrongly).
func give_back(id: String) -> void:
	if _out.erase(id):
		_pool.append(id)


## Marks a token as used up by a correct assignment; it never drops again.
func consume(id: String) -> void:
	_out.erase(id)
