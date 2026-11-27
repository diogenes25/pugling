class_name TowerRun
extends RefCounted
## A run as the server started it: the goal, the queue of tower meanings and the words that can drop.

## The server's id of the run.
var run_id: int

## The plan position the words come from.
var plan_position_id: int

## Correct words needed for the level.
var goal: int

## Meanings in the order the towers ask for them.
var towers: Array[TowerTarget] = []

## The words that drop as books.
var tokens: Array[TowerToken] = []


## Reads the answer of "POST tower-game/runs". JSON numbers arrive as float, hence the int().
static func from_json(json: Dictionary) -> TowerRun:
	var run := TowerRun.new()
	run.run_id = int(json.get("runId", 0))
	run.plan_position_id = int(json.get("planPositionId", 0))
	run.goal = int(json.get("goal", 0))
	for tower: Dictionary in json.get("towers", []):
		run.towers.append(TowerTarget.new(str(tower.get("towerId", "")), str(tower.get("prompt", ""))))
	for token: Dictionary in json.get("tokens", []):
		run.tokens.append(TowerToken.new(str(token.get("tokenId", "")), str(token.get("word", ""))))
	return run
