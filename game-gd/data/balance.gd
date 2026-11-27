class_name Balance
extends Object
## The game's tuning knobs in one place. Numbers that belong to one kind of thing (a tower type, an enemy type, a
## projectile, a map) live in that kind's resource under data/; everything here is a rule of the whole game.

## The hero's walking speed in units per second.
const HERO_SPEED: float = 6.5

## The hero's body radius against obstacles.
const HERO_RADIUS: float = 0.45

## Books closer than this to the hero are offered for pick-up.
const PICKUP_RADIUS: float = 2.6

## A tower closer than this to the hero accepts the carried word.
const TOWER_REACH: float = 3.6

## The hero's own shot: shorter range and slower than a tower and only ever one damage, so the towers stay the main
## defence - but without ammo, so empty towers never end a run alone.
const HERO_RANGE: float = 5.5
const HERO_SHOT_INTERVAL: float = 1.0

## Shots a tower starts with.
const START_AMMO: int = 10

## Shots a correct word adds.
const REFILL_AMMO: int = 8

## Shots a wrong word costs (user decision of 2026-11-26; it was 1 before).
const WRONG_PENALTY: int = 3

## A tower with more shots than STRONG_AMMO, or refilled MASTER_REFILLS times, shoots its strong projectile.
const STRONG_AMMO: int = 30
const MASTER_REFILLS: int = 5

## Skeletons that may reach the castle before it falls.
const CASTLE_LIVES: int = 10

## Seconds a dropped book lies on the field before it goes back into the pool; laid-down books stay.
const TOKEN_LIFETIME: float = 30.0

## A book blinks during its last seconds on the field.
const TOKEN_BLINK_WARNING: float = 5.0

## Seconds before the first skeleton.
const FIRST_SPAWN_DELAY: float = 2.0

## The spawn interval shrinks by this many seconds per played second, but never below the floor factor.
const SPAWN_INTERVAL_RAMP: float = 0.012
const SPAWN_INTERVAL_FLOOR: float = 0.55

## Skeletons get faster by this per played second, by at most MAX_SPEED_RAMP.
const SPEED_RAMP: float = 0.004
const MAX_SPEED_RAMP: float = 0.8

## Every n-th skeleton is a warrior.
const WARRIOR_EVERY: int = 4

## The app reports the foreground play time about this often (B-851: the server clamps each beat).
const BEAT_INTERVAL: float = 15.0
