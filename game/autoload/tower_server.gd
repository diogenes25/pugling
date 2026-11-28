@abstract
class_name TowerServer
extends Node
## What the game asks of a server. [code]Api[/code] (autoload/api.gd) talks to the real Pugling API; [FakeApi] answers
## from memory, so a level runs without a server (headless smoke runs, tests). Every call is awaited and returns an
## [ApiResult]. The analyzer cannot see that the implementations are coroutines, so callers mark their await with
## @warning_ignore("redundant_await").

## The logged-in child's id, set by [method login].
var child_id: int = 0

## The logged-in child's display name.
var child_name: String = ""


## Logs the child in with its PIN.
@abstract
func login(server: String, child: int, pin: String) -> ApiResult


## The vocabulary positions a run can be built from, with today's play time and coins.
@abstract
func overview() -> ApiResult


## The child's day for the start screen: the data holds "wallet", "today" (dictionaries) and "missions" (an array),
## each empty when its call failed. Fails only when none of them answered.
@abstract
func home() -> ApiResult


## Starts a run from one plan position.
@abstract
func start_run(plan_position_id: int) -> ApiResult


## Lets the server grade a token-to-tower assignment.
@abstract
func assign(run_id: int, token_id: String, tower_id: String) -> ApiResult


## Reports that the level is still in the foreground; the server counts the play time.
@abstract
func heartbeat(run_id: int) -> ApiResult


## Ends the level and reports the wave it reached; the server decides by its own count whether it is won, pays the
## coins and books the trophies.
@abstract
func end_run(run_id: int, wave: int) -> ApiResult


## Opens the oldest waiting daily chest (earned in the learning part); the answer holds its coins and gems.
@abstract
func open_daily_chest() -> ApiResult


## Opens today's game chest once it is full; the answer holds its gems and the chest afterwards.
@abstract
func open_game_chest() -> ApiResult


## The hero skins with their prices in gems and what the child owns, plus its gem balance.
@abstract
func skins() -> ApiResult


## Buys a hero skin for gems; the answer is the list of [method skins] afterwards.
@abstract
func buy_skin(skin_id: String) -> ApiResult
