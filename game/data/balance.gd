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

## The hero's lasting level: the trophies its second level needs (see [method HeroLevel.needed]), the share of his
## reload time each level takes off down to the floor, and how many levels add one damage to his shot.
const HERO_LEVEL_STEP: int = 30
const HERO_RELOAD_GAIN: float = 0.05
const HERO_RELOAD_FLOOR: float = 0.5
const HERO_DAMAGE_EVERY: int = 4

## Shots a tower starts with: enough for the first wave, not for the second - without words a run tips soon.
const START_AMMO: int = 16

## Shots a correct word adds, plus REFILL_PER_VALUE for every value point of the word: a new or weak word (value 3)
## stocks a tower better than a well learned one (value 1).
const REFILL_AMMO: int = 8
const REFILL_PER_VALUE: int = 2

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

## Seconds before the first wave.
const FIRST_SPAWN_DELAY: float = 2.0

## Seconds between a cleared wave and the next one.
const WAVE_REST: float = 5.0

## Small enemies of the first wave, and how many more each further wave brings, up to MAX_MINIONS.
const WAVE_MINIONS: int = 5
const WAVE_MINIONS_GROWTH: int = 2
const MAX_MINIONS: int = 30

## Word carriers of the first wave; every second wave brings one more, up to MAX_CARRIERS. Each drops a word, so
## they are what a run lives on: their words have to outweigh the shots the wave costs.
const WAVE_CARRIERS: int = 4
const MAX_CARRIERS: int = 10

## The special enemies join later, so the first waves stay simple. From RUNNER_FROM on every RUNNER_EVERY-th small
## enemy is a runner, from SHIELD_FROM on every SHIELD_EVERY-th carrier bears a shield, and from HEALER_FROM on a
## healer marches in the middle - one more every HEALER_MORE_EVERY waves, at most MAX_HEALERS.
const RUNNER_FROM: int = 3
const RUNNER_EVERY: int = 3
const SHIELD_FROM: int = 4
const SHIELD_EVERY: int = 3
const HEALER_FROM: int = 6
const HEALER_MORE_EVERY: int = 6
const MAX_HEALERS: int = 3

## Every n-th wave ends with a boss.
const BOSS_EVERY: int = 5

## Within a wave the enemies follow each other at the map's interval times WAVE_INTERVAL_START, shrinking by
## WAVE_INTERVAL_RAMP per wave down to the floor factor.
const WAVE_INTERVAL_START: float = 0.6
const WAVE_INTERVAL_RAMP: float = 0.03
const WAVE_INTERVAL_FLOOR: float = 0.25

## Enemies get faster by this per wave, by at most MAX_SPEED_RAMP.
const WAVE_SPEED_RAMP: float = 0.06
const MAX_SPEED_RAMP: float = 0.8

## Drops come from this many tokens at the front of the pool's queue.
const DROP_WINDOW: int = 8

## The summed word value a tower needs for its second level; see [method TowerLevel.needed].
const LEVEL_STEP: int = 4

## The value of a word the server did not rate (an older server): the middle of 1 (well learned) to 3 (new).
const DEFAULT_WORD_VALUE: int = 2

## Points: per value point of a correct word, per level a tower reaches (times the level), per cleared wave (times
## its number).
const WORD_POINTS: int = 50
const LEVEL_POINTS: int = 100
const WAVE_POINTS: int = 100

## A cleared wave repairs this many of the castle's lives.
const WAVE_REPAIR: int = 1

## The app reports the foreground play time about this often (B-851: the server clamps each beat).
const BEAT_INTERVAL: float = 15.0
