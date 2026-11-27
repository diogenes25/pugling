class_name HomeInfo
extends RefCounted
## What the start screen shows of the child's day, read from the answers of three read-only calls: the wallet, today's
## duties with streak and chest, and the missions. Every part is optional - an older server or a failed call leaves
## its fields at their defaults.


## A mission with its progress.
class Mission:
	extends RefCounted

	## What the mission asks for.
	var title: String

	## The value reached so far.
	var current: int

	## The value to reach.
	var target: int

	## The mission is done.
	var completed: bool

	## Points it pays.
	var reward: int


## Coins in the wallet.
var coins: int = 0

## Gems in the wallet.
var gems: int = 0

## Days in a row with the duties done.
var streak: int = 0

## Today's learning goals: met and all.
var goals_met: int = 0
var goals_total: int = 0

## Today's goals that are still open, by title.
var outstanding: Array[String] = []

## Chests waiting to be opened.
var chests_waiting: int = 0

## Today's chest is already earned.
var chest_earned_today: bool = false

## The missions, open ones first.
var missions: Array[Mission] = []


## Reads the answers of "me/points", "children/{id}/today" and "me/missions"; an empty dictionary skips a part.
static func from_json(wallet: Dictionary, today: Dictionary, mission_list: Array) -> HomeInfo:
	var info := HomeInfo.new()
	info.coins = int(wallet.get("coins", 0))
	info.gems = int(wallet.get("gems", 0))
	info.streak = int(today.get("currentStreak", 0))
	info.goals_met = int(today.get("goalsMet", 0))
	info.goals_total = int(today.get("goalsTotal", 0))
	for title: Variant in today.get("outstanding", []):
		info.outstanding.append(str(title))
	var box: Variant = today.get("dailyBox")
	if box is Dictionary:
		info.chests_waiting = int((box as Dictionary).get("waiting", 0))
		info.chest_earned_today = bool((box as Dictionary).get("claimedToday", false))
	for entry: Variant in mission_list:
		if not entry is Dictionary:
			continue
		var raw := entry as Dictionary
		var mission := Mission.new()
		mission.title = str(raw.get("title", ""))
		mission.current = int(raw.get("current", 0))
		mission.target = int(raw.get("target", 0))
		mission.completed = bool(raw.get("completed", false))
		mission.reward = int(raw.get("rewardPoints", 0))
		info.missions.append(mission)
	info.missions.sort_custom(func(a: Mission, b: Mission) -> bool: return not a.completed and b.completed)
	return info
