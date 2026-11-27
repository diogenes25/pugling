class_name TowerAmmo
extends RefCounted
## A tower's ammunition economy: it shoots while it has shots, a correct word refills it, a wrong one costs shots. A
## tower that is well stocked or often refilled turns strong and shoots stronger projectiles.

## Shots left.
var count: int = Balance.START_AMMO

## How often a correct word refilled this tower.
var refills: int = 0


## Shoots the strong projectile.
func is_strong() -> bool:
	return count > Balance.STRONG_AMMO or refills >= Balance.MASTER_REFILLS


## One more wrong word would empty the tower.
func is_low() -> bool:
	return count <= Balance.WRONG_PENALTY


## Uses one shot; returns whether it was a strong one.
func use() -> bool:
	var strong := is_strong()
	count -= 1
	return strong


## Shots a correct word of the given value adds.
static func refill_size(value: int) -> int:
	return Balance.REFILL_AMMO + Balance.REFILL_PER_VALUE * value


## A correct word of the given value: adds [method refill_size] shots; returns whether the tower just turned strong.
func refill(value: int) -> bool:
	var was_strong := is_strong()
	count += refill_size(value)
	refills += 1
	return not was_strong and is_strong()


## A wrong word: costs [constant Balance.WRONG_PENALTY] shots, never below zero.
func penalize() -> void:
	count = maxi(0, count - Balance.WRONG_PENALTY)
