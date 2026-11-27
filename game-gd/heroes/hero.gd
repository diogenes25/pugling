class_name Hero
extends Node3D
## The hero in the world: where he stands, how he walks around obstacles, the word he carries and his own shot. How
## he looks is the business of a [HeroLook]. The camera follows this node.

## His place on the ground.
var ground: Vector2

## The word he carries, or null.
var carried: TowerToken

## Reload timer of his own shot.
var reload: Cooldown = Cooldown.new()

## How he is shown.
var look: HeroLook

var _map: MapInfo
var _book: Node3D
var _yaw: float = 0.0


## Puts the hero on the map with the given look.
func setup(map: MapInfo, hero_look: HeroLook, start: Vector2) -> void:
	_map = map
	look = hero_look
	ground = start
	add_child(look)
	add_child(Fx.ring(Vector3(0, 0.07, 0), Balance.PICKUP_RADIUS, Color(1.0, 0.9, 0.4, 0.35)))
	_book = Models.character("spellbook_closed.gltf").instantiate() as Node3D
	_book.scale = Vector3.ONE * 1.4
	_book.visible = false
	add_child(_book)
	_place(0.0)


## Walks by the joystick vector ([param direction], length up to 1) and slides along obstacles; turns the look.
func move(dt: float, direction: Vector2, clock: float) -> void:
	var moving := direction.length() > 0.05
	if moving:
		var step := direction * Balance.HERO_SPEED * dt
		if _map.walkable(ground + step, Balance.HERO_RADIUS):
			ground += step
		elif _map.walkable(ground + Vector2(step.x, 0), Balance.HERO_RADIUS):
			ground.x += step.x
		elif _map.walkable(ground + Vector2(0, step.y), Balance.HERO_RADIUS):
			ground.y += step.y
		# He turns smoothly towards where the thumb points, so the direction of travel is always visible.
		_yaw = lerp_angle(_yaw, atan2(direction.x, direction.y), 1 - exp(-14 * dt))
	look.update(dt, moving, _yaw)
	_place(clock)


## Takes a word into the hand.
func carry(token: TowerToken) -> void:
	carried = token
	_book.visible = true


## Empties the hand; returns the word that was in it, or null.
func release() -> TowerToken:
	var token := carried
	carried = null
	_book.visible = false
	return token


func _place(clock: float) -> void:
	position = Geo.to3(ground)
	_book.position = Vector3(0, 2.5 + 0.12 * sin(clock * 4), 0)
	_book.rotation = Vector3(0, clock * 1.5, 0)
