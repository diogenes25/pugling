class_name WordBooks
extends Node3D
## The words of a run on the field: books drop where skeletons fall, the hero lays words down to sort them, dropped
## books expire. Which word is in the pool, out or used is the business of the [TokenPool].

## Books lie at least this far apart, so their labels on screen do not cover each other.
const GAP: float = 2.4

## The books on the field.
var all: Array[WordBook] = []

## The run's token bookkeeping.
var pool: TokenPool

var _def: LevelDef
var _map: MapInfo
var _by_id: Dictionary[String, TowerToken] = {}


## Binds the books to a map and the run's words.
func setup(def: LevelDef, map: MapInfo, tokens: Array[TowerToken]) -> void:
	_def = def
	_map = map
	var ids: Array[String] = []
	for token: TowerToken in tokens:
		_by_id[token.token_id] = token
		ids.append(token.token_id)
	pool = TokenPool.new(ids)


## Drops a random word of the pool beside the road at a path point; null when the pool is empty.
func drop_beside(at: Vector2, dir: Vector2) -> WordBook:
	var id := pool.draw()
	if id == "":
		return null
	# The book lands beside the road, inside the map, where the hero can reach it.
	var side := dir.orthogonal() * (1.6 if randf() < 0.5 else -1.6)
	return _drop(_by_id[id], _free_spot(at + side), false)


## Lays a word down near a point; it stays there.
func lay_down(token: TowerToken, near_to: Vector2) -> WordBook:
	return _drop(token, _free_spot(near_to), true)


## Takes a book off the field; false when it is no longer there.
func take(book: WordBook) -> bool:
	var index := all.find(book)
	if index < 0:
		return false
	all.remove_at(index)
	book.node.queue_free()
	return true


## The books within reach of a point, nearest first, at most [param limit].
func near(from: Vector2, radius: float, limit: int) -> Array[WordBook]:
	var found: Array[WordBook] = []
	for book: WordBook in all:
		if book.pos.distance_to(from) <= radius:
			found.append(book)
	found.sort_custom(func(a: WordBook, b: WordBook) -> bool: return a.pos.distance_to(from) < b.pos.distance_to(from))
	return found.slice(0, limit)


## Ages and animates the books; dropped books that expire go back into the pool. A frozen field (level over) only
## animates.
func animate(dt: float, clock: float, frozen: bool) -> void:
	for i: int in range(all.size() - 1, -1, -1):
		var book := all[i]
		# Expired books go back to the pool, so the field never fills up with words nobody picks.
		if not frozen and not book.placed:
			book.age += dt
			if book.age >= Balance.TOKEN_LIFETIME:
				pool.give_back(book.data.token_id)
				book.node.queue_free()
				all.remove_at(i)
				continue
		var blink := (not book.placed and book.age > Balance.TOKEN_LIFETIME - Balance.TOKEN_BLINK_WARNING
				and fposmod(clock, 0.5) < 0.2)
		book.node.visible = not blink
		book.node.position = Geo.to3(book.pos, 0.6 + 0.15 * sin(clock * 3 + i))
		book.node.rotation = Vector3(0, clock * 1.2 + i, 0)


func _drop(data: TowerToken, at: Vector2, placed: bool) -> WordBook:
	var book := WordBook.new()
	book.data = data
	book.pos = at
	book.placed = placed
	book.node = Models.character("spellbook_closed.gltf").instantiate() as Node3D
	book.node.scale = Vector3.ONE * 1.6
	add_child(book.node)
	# A white ring marks a word the hero laid down himself: it stays.
	book.node.add_child(Fx.ring(Vector3(0, -0.3, 0), 0.55, Color(1, 1, 1, 0.85) if placed else Color(UiKit.GOLD, 0.7)))
	all.append(book)
	return book


## A walkable spot near a point that keeps [constant GAP] to every book.
func _free_spot(near_to: Vector2) -> Vector2:
	var lo := Vector2(1, 1)
	var hi := _def.size - Vector2(1, 1)
	for ring: int in range(0, 7):
		var steps := 1 if ring == 0 else ring * 8
		for k: int in steps:
			var angle := TAU * k / steps
			var spot := (near_to + Vector2(cos(angle), sin(angle)) * ring * 1.1).clamp(lo, hi)
			if _map.walkable(spot, 0.3) and _keeps_gap(spot):
				return spot
	return near_to.clamp(lo, hi)


func _keeps_gap(spot: Vector2) -> bool:
	for book: WordBook in all:
		if book.pos.distance_to(spot) < GAP:
			return false
	return true
