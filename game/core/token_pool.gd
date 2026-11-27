class_name TokenPool
extends RefCounted
## Bookkeeping of a run's vocabulary tokens: which are waiting in the queue, lying on the field or carried. The
## client cannot know which token fits which tower - the server keeps that - but the server sends the tokens roughly
## in the order of its towers, so drops come from the front of the queue: among many words the ones the towers ask
## for now stay likely. A wrongly assigned token goes back to its old place (B-822 E2); a correctly assigned one goes
## to the very end, because an endless run asks for every word again once all are through.

var _queue: Array[String] = []
var _rank: Dictionary[String, int] = {}
var _out: Dictionary[String, bool] = {}
var _next_rank: int = 0
var _random: RandomNumberGenerator


## [param token_ids] are the run's tokens in the server's order; a seeded [param random] makes the draws repeatable.
func _init(token_ids: Array[String], random: RandomNumberGenerator = null) -> void:
	for id: String in token_ids:
		_queue.append(id)
		_rank[id] = _next_rank
		_next_rank += 1
	_random = random if random != null else RandomNumberGenerator.new()


## Takes a random token from the front [constant Balance.DROP_WINDOW] of the queue for a drop, or "" when every
## token is out.
func draw() -> String:
	if _queue.is_empty():
		return ""
	var id: String = _queue.pop_at(_random.randi_range(0, mini(Balance.DROP_WINDOW, _queue.size()) - 1))
	_out[id] = true
	return id


## Puts a token back where it stood (it expired on the field, or was assigned wrongly).
func give_back(id: String) -> void:
	if not _out.erase(id):
		return
	var at := 0
	while at < _queue.size() and _rank[_queue[at]] < _rank[id]:
		at += 1
	_queue.insert(at, id)


## A correct assignment used the token: it queues up again behind all others.
func consume(id: String) -> void:
	if not _out.erase(id):
		return
	_rank[id] = _next_rank
	_next_rank += 1
	_queue.append(id)


## The waiting tokens, front first.
func order() -> Array[String]:
	return _queue.duplicate()
