class_name EnemyRules
extends RefCounted
## What the special enemies do to the numbers: armour takes the edge off every hit, a healer mends the ones around
## it. Both are bounded, so a wave stays beatable.


## Hit points a hit of [param damage] takes from an enemy with [param armor]: the armour swallows that much of every
## hit, but a hit always takes at least one - weak towers still wear a shield bearer down, strong ones barely notice.
static func damage_taken(damage: int, armor: int) -> int:
	return maxi(1, damage - armor)


## The hit points after a healer's pulse of [param amount]: never more than the enemy entered the map with.
static func healed(hp: int, max_hp: int, amount: int) -> int:
	return mini(max_hp, hp + amount)
