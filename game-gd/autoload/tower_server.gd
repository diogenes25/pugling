class_name TowerServer
extends Node
## What the game asks of a server. [code]Api[/code] (autoload/api.gd) talks to the real Pugling API; [FakeApi] answers
## from memory, so a level runs without a server (headless smoke runs, tests). Every call is awaited and returns an
## [ApiResult].

## The logged-in child's id, set by [method login].
var child_id: int = 0

## The logged-in child's display name.
var child_name: String = ""


## Logs the child in with its PIN.
@warning_ignore("unused_parameter")
func login(server: String, child: int, pin: String) -> ApiResult:
	return ApiResult.failure("")


## The vocabulary positions a run can be built from, with today's play time and coins.
func overview() -> ApiResult:
	return ApiResult.failure("")


## Starts a run from one plan position.
@warning_ignore("unused_parameter")
func start_run(plan_position_id: int) -> ApiResult:
	return ApiResult.failure("")


## Lets the server grade a token-to-tower assignment.
@warning_ignore("unused_parameter")
func assign(run_id: int, token_id: String, tower_id: String) -> ApiResult:
	return ApiResult.failure("")


## Reports that the level is still in the foreground; the server counts the play time.
@warning_ignore("unused_parameter")
func heartbeat(run_id: int) -> ApiResult:
	return ApiResult.failure("")


## Ends the level; the server decides by its own count whether it is won and pays the coins.
@warning_ignore("unused_parameter")
func end_run(run_id: int) -> ApiResult:
	return ApiResult.failure("")
